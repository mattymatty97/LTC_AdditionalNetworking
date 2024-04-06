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
        
        /// <summary>
        ///  Set the Instance
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

       
        /// <summary>
        ///  broadcast new inventory order.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncInventoryServerRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] inventory, int[] slots, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogInfo($"syncInventoryServerRpc was called!");
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
            
            syncInventoryClientRpc(controllerReference,valid.ToArray(), valid_ids.ToArray());
            if (extra.Count > 0)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                    }
                };
                throwExtraItemsClientRpc(controllerReference, extra.ToArray(), clientRpcParams);
            }
        }
        
        /// <summary>
        ///  align the inventory.
        ///  if we're the owner do not update the current inventory but check if the server has truncated the request
        /// </summary>
        [ClientRpc]
        private void syncInventoryClientRpc(NetworkObjectReference controllerObject, NetworkObjectReference[] inventory, int[] slots)
        {
            AdditionalNetworking.Log.LogInfo($"syncInventoryClientRpc was called!");
            var _controllerB = ((GameObject)controllerObject).GetComponent<PlayerControllerB>();
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
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{_controllerB.OwnerClientId}
                    }
                };
                throwExtraItemsClientRpc(controllerObject, missingObjects.Select(g => (NetworkObjectReference)g.NetworkObject).ToArray(),clientRpcParams);
            }
        }

        /// <summary>
        ///  safely drop requested items.
        /// </summary>
        [ClientRpc]
        private void throwExtraItemsClientRpc(NetworkObjectReference controllerObject, NetworkObjectReference[] objectsToThrow, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogInfo($"throwExtraItemsClientRpc was called!");
            var _controllerB = ((GameObject)controllerObject).GetComponent<PlayerControllerB>();
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
        public void syncSelectedSlotServerRpc(NetworkObjectReference controllerObject, int selectedSlot)
        {
            AdditionalNetworking.Log.LogInfo($"syncSelectedSlotServerRpc was called!");
            syncSelectedSlotClientRpc(controllerObject, selectedSlot);
        }        
                
        /// <summary>
        ///  align new held item.
        /// </summary>
        [ClientRpc]
        private void syncSelectedSlotClientRpc(NetworkObjectReference controllerObject, int selectedSlot)
        {
            AdditionalNetworking.Log.LogInfo($"syncSelectedSlotClientRpc was called!");
            var _controllerB = ((GameObject)controllerObject).GetComponent<PlayerControllerB>();
            if (_controllerB.IsOwner)
                return;
            
            if (_controllerB.currentItemSlot != selectedSlot)
                _controllerB.SwitchToItemSlot(_controllerB.currentItemSlot);
        }

        /// <summary>
        ///  broadcast name change.
        ///  ( de-sync typically in lateJoin cases )
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncUsernameServerRpc(NetworkObjectReference controllerObject, string username)
        {
            AdditionalNetworking.Log.LogInfo($"syncUsernameServerRpc was called!");
            syncUsernameClientRpc(controllerObject, username);
        }
        
        /// <summary>
        ///  align player name.
        /// </summary>
        [ClientRpc]
        public void syncUsernameClientRpc(NetworkObjectReference controllerObject, string username, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogInfo($"syncUsernameClientRpc was called!");
            var _controllerB = ((GameObject)controllerObject).GetComponent<PlayerControllerB>();
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
        public void requestSyncUsernameServerRpc(NetworkObjectReference controllerObject, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogInfo($"requestSyncUsernameServerRpc was called!");
            var _controllerB = ((GameObject)controllerObject).GetComponent<PlayerControllerB>();
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
            syncUsernameClientRpc(controllerObject, _controllerB.playerUsername, clientRpcParams);
        }
        
    }
}