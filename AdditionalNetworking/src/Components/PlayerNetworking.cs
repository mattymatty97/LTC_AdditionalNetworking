using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class PlayerNetworking: NetworkBehaviour
    {
        public static PlayerNetworking Instance { get; private set; }
        public bool Enabled { get; private set; }
        internal HashSet<ulong> ValidClientIDs = [];
        
        
        /// <summary>
        ///  Set the Instance
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }
        
        /// <summary>
        ///  signal that we have the mod installed.
        /// </summary>
        private void Start()
        {
            onConnectServerRpc();
        }

        /// <summary>
        ///  track clients with the mod installed.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void onConnectServerRpc(ServerRpcParams serverRpcParams = default)
        {                
            ClientRpcParams senderClientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            AdditionalNetworking.Log.LogInfo($"{serverRpcParams.Receive.SenderClientId} registered on {nameof(PlayerNetworking)}");
            ValidClientIDs.Add(serverRpcParams.Receive.SenderClientId);
            ackConnectClientRpc(senderClientRpcParams);
        }
        
        /// <summary>
        ///  server ack.
        /// </summary>
        [ClientRpc]
        private void ackConnectClientRpc(ClientRpcParams clientRpcParams = default)
        {
            Enabled = true;
            AdditionalNetworking.Log.LogInfo($"host has {nameof(PlayerNetworking)}");
        }

       
        /// <summary>
        ///  broadcast new inventory order.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncInventoryServerRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] inventory, int[] slots, ServerRpcParams serverRpcParams = default)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            
            AdditionalNetworking.Log.LogDebug($"syncInventoryServerRpc was called for {controllerReference.NetworkObjectId}!");
            var _controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            //limit the list to the max slots of the server
            List<NetworkObjectReference> valid = new List<NetworkObjectReference>();
            List<int> valid_ids = new List<int>();
            
            List<NetworkObjectReference> extra = new List<NetworkObjectReference>();

            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index] < _controllerB.ItemSlots.Length)
                {
                    valid.Add(inventory[index]);
                    valid_ids.Add(slots[index]);
                }
                else
                {
                    extra.Add(inventory[index]);
                }
            }
            
            syncInventoryClientRpc(controllerReference,valid.ToArray(), valid_ids.ToArray(), clientRpcParams);
            if (extra.Count > 0)
            {
                ClientRpcParams senderClientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                    }
                };
                throwExtraItemsClientRpc(controllerReference, extra.ToArray(), senderClientRpcParams);
            }
        }
        
        /// <summary>
        ///  align the inventory.
        ///  if we're the owner do not update the current inventory but check if the server has truncated the request
        /// </summary>
        [ClientRpc]
        private void syncInventoryClientRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] inventory, int[] slots, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"syncInventoryClientRpc was called for {controllerReference.NetworkObjectId}!");
            var _controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            HashSet<GrabbableObject> missingObjects = new HashSet<GrabbableObject>(_controllerB.ItemSlots.Where(g=>g!=null));
            if (!_controllerB.IsOwner)
                //flush the inventory
                _controllerB.ItemSlots = new GrabbableObject[_controllerB.ItemSlots.Length];
            for (var index = 0; index < inventory.Length; index++ )
            {
                var slot = slots[index];
                var networkObjectReference = inventory[index];
                if (slot < _controllerB.ItemSlots.Length)
                {
                    if (networkObjectReference.TryGet(out var networkObject) &&
                        networkObject.TryGetComponent<GrabbableObject>(out var grabbableObject))
                    {
                        missingObjects.Remove(grabbableObject);
                        if (!_controllerB.IsOwner)
                            _controllerB.ItemSlots[slot] = grabbableObject;
                    }
                    else
                    {
                        if (!_controllerB.IsOwner)
                            _controllerB.ItemSlots[slot] = null;
                    }
                }
                else
                {
                    //TODO: handle too many slots
                }
            }

            if (IsServer && missingObjects.Count > 0)
            {
                //should never happen but it's a good idea to handle that case
                ClientRpcParams clientRpcParams2 = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{_controllerB.OwnerClientId}
                    }
                };
                throwExtraItemsClientRpc(controllerReference, missingObjects.Select(g => (NetworkObjectReference)g.NetworkObject).ToArray(),clientRpcParams2);
            }
        }

        /// <summary>
        ///  safely drop requested items.
        /// </summary>
        [ClientRpc]
        private void throwExtraItemsClientRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] objectsToThrow, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"throwExtraItemsClientRpc was called for {controllerReference.NetworkObjectId}!");
            var _controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            if (!_controllerB.IsOwner)
                return;

            foreach (var networkObjectReference in objectsToThrow)
            {
                if (networkObjectReference.TryGet(out var networkObject) &&
                    networkObject.TryGetComponent<GrabbableObject>(out var grabbableObject))
                {
                    _controllerB.ThrowObjectServerRpc(networkObjectReference, _controllerB.isInElevator, _controllerB.isInHangarShipRoom, default, default);
                }
            }
        }
        
        /// <summary>
        ///  broadcast new held item.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncSelectedSlotServerRpc(NetworkObjectReference controllerReference, int selectedSlot)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"syncSelectedSlotServerRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot}");
            syncSelectedSlotClientRpc(controllerReference, selectedSlot, clientRpcParams);
        }        
                
        /// <summary>
        ///  align new held item.
        /// </summary>
        [ClientRpc]
        private void syncSelectedSlotClientRpc(NetworkObjectReference controllerReference, int selectedSlot, ClientRpcParams clientRpcParams = default)
        {
            var _controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            AdditionalNetworking.Log.LogDebug($"syncSelectedSlotClientRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot} was:{_controllerB.currentItemSlot}");
            if (_controllerB.IsOwner)
                return;
            
            if (_controllerB.currentItemSlot != selectedSlot)
                _controllerB.SwitchToItemSlot(selectedSlot);
        }

        /// <summary>
        ///  broadcast name change.
        ///  ( de-sync typically in lateJoin cases )
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncUsernameServerRpc(NetworkObjectReference controllerReference, string username)
        {            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"syncUsernameServerRpc was called for {controllerReference.NetworkObjectId}!");
            syncUsernameClientRpc(controllerReference, username, clientRpcParams);
        }
        
        /// <summary>
        ///  align player name.
        /// </summary>
        [ClientRpc]
        public void syncUsernameClientRpc(NetworkObjectReference controllerReference, string username, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"syncUsernameClientRpc was called for {controllerReference.NetworkObjectId}!");
            var _controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            if (_controllerB.IsOwner)
                return;
            _controllerB.playerUsername = username;
            _controllerB.usernameBillboardText.text = username;
            //TODO update spectating boxes and radar
        }
        
        
        /// <summary>
        ///  request server username value.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void requestSyncUsernameServerRpc(NetworkObjectReference controllerReference, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"requestSyncUsernameServerRpc was called for {controllerReference.NetworkObjectId} by {serverRpcParams.Receive.SenderClientId}!");
            var _controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            //only send update if player is connected!
            if (_controllerB.IsOwnedByServer)
                return;
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            syncUsernameClientRpc(controllerReference, _controllerB.playerUsername, clientRpcParams);
        }
        
    }
}