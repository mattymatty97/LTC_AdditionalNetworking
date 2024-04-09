using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
// ReSharper disable MemberCanBeMadeStatic.Local

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
            OnConnectServerRpc();
        }

        /// <summary>
        ///  track clients with the mod installed.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void OnConnectServerRpc(ServerRpcParams serverRpcParams = default)
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
            AckConnectClientRpc(senderClientRpcParams);
        }
        
        /// <summary>
        ///  server ack.
        /// </summary>
        [ClientRpc]
        private void AckConnectClientRpc(ClientRpcParams clientRpcParams = default)
        {
            Enabled = true;
            AdditionalNetworking.Log.LogInfo($"host has {nameof(PlayerNetworking)}");
        }

       
        /// <summary>
        ///  broadcast new inventory order.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncInventoryServerRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] inventory, int[] slots, ServerRpcParams serverRpcParams = default)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            
            AdditionalNetworking.Log.LogDebug($"syncInventoryServerRpc was called for {controllerReference.NetworkObjectId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            //limit the list to the max slots of the server
            List<NetworkObjectReference> valid = new List<NetworkObjectReference>();
            List<int> validIds = new List<int>();
            
            List<NetworkObjectReference> extra = new List<NetworkObjectReference>();

            for (var index = 0; index < slots.Length; index++)
            {
                if (slots[index] < controllerB.ItemSlots.Length)
                {
                    valid.Add(inventory[index]);
                    validIds.Add(slots[index]);
                }
                else
                {
                    extra.Add(inventory[index]);
                }
            }
            
            controllerB.SwitchToItemSlot(controllerB.currentItemSlot);
            
            SyncInventoryClientRpc(controllerReference,valid.ToArray(), validIds.ToArray(), clientRpcParams);
            if (extra.Count > 0)
            {
                ClientRpcParams senderClientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                    }
                };
                ThrowExtraItemsClientRpc(controllerReference, extra.ToArray(), senderClientRpcParams);
            }
        }
        
        /// <summary>
        ///  align the inventory.
        ///  if we're the owner do not update the current inventory but check if the server has truncated the request
        /// </summary>
        [ClientRpc]
        private void SyncInventoryClientRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] inventory, int[] slots, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"syncInventoryClientRpc was called for {controllerReference.NetworkObjectId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            if (!controllerB.IsOwner)
                //flush the inventory
                controllerB.ItemSlots = new GrabbableObject[controllerB.ItemSlots.Length];
            for (var index = 0; index < inventory.Length; index++ )
            {
                var slot = slots[index];
                var networkObjectReference = inventory[index];
                if (slot < controllerB.ItemSlots.Length)
                {
                    if (networkObjectReference.TryGet(out var networkObject) &&
                        networkObject.TryGetComponent<GrabbableObject>(out var grabbableObject))
                    {
                        if (!controllerB.IsOwner)
                            controllerB.ItemSlots[slot] = grabbableObject;
                    }
                    else
                    {
                        if (!controllerB.IsOwner)
                            controllerB.ItemSlots[slot] = null;
                    }
                }
                else
                {
                    //should never happen but better be safe
                    //TODO: handle too many slots
                }
            }
        }

        /// <summary>
        ///  safely drop requested items.
        /// </summary>
        [ClientRpc]
        private void ThrowExtraItemsClientRpc(NetworkObjectReference controllerReference, NetworkObjectReference[] objectsToThrow, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"throwExtraItemsClientRpc was called for {controllerReference.NetworkObjectId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            if (!controllerB.IsOwner)
                return;

            foreach (var networkObjectReference in objectsToThrow)
            {
                if (networkObjectReference.TryGet(out var networkObject) &&
                    networkObject.TryGetComponent<GrabbableObject>(out var grabbableObject))
                {
                    controllerB.ThrowObjectServerRpc(networkObjectReference, controllerB.isInElevator, controllerB.isInHangarShipRoom, default, default);
                }
            }
        }
        
        /// <summary>
        ///  broadcast new held item.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncSelectedSlotServerRpc(NetworkObjectReference controllerReference, int selectedSlot)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"syncSelectedSlotServerRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot}");
            SyncSelectedSlotClientRpc(controllerReference, selectedSlot, clientRpcParams);
        }        
                
        /// <summary>
        ///  align new held item.
        /// </summary>
        [ClientRpc]
        private void SyncSelectedSlotClientRpc(NetworkObjectReference controllerReference, int selectedSlot, ClientRpcParams clientRpcParams = default)
        {
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            AdditionalNetworking.Log.LogDebug($"syncSelectedSlotClientRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot} was:{controllerB.currentItemSlot}");
            if (controllerB.IsOwner)
                return;
            
            if (controllerB.currentItemSlot != selectedSlot)
                controllerB.SwitchToItemSlot(selectedSlot);
        }

        /// <summary>
        ///  broadcast name change.
        ///  ( de-sync typically in lateJoin cases )
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncUsernameServerRpc(NetworkObjectReference controllerReference, string username)
        {            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"syncUsernameServerRpc was called for {controllerReference.NetworkObjectId}!");
            SyncUsernameClientRpc(controllerReference, username, clientRpcParams);
        }
        
        /// <summary>
        ///  align player name.
        /// </summary>
        [ClientRpc]
        public void SyncUsernameClientRpc(NetworkObjectReference controllerReference, string username, ClientRpcParams clientRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"syncUsernameClientRpc was called for {controllerReference.NetworkObjectId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            if (controllerB.IsOwner)
                return;
            controllerB.playerUsername = username;
            controllerB.usernameBillboardText.text = username;
            //TODO update spectating boxes and radar
        }
        
        
        /// <summary>
        ///  request server username value.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncUsernameServerRpc(NetworkObjectReference controllerReference, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"requestSyncUsernameServerRpc was called for {controllerReference.NetworkObjectId} by {serverRpcParams.Receive.SenderClientId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            //only send update if player is connected!
            if (controllerB.IsOwnedByServer)
                return;
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            SyncUsernameClientRpc(controllerReference, controllerB.playerUsername, clientRpcParams);
        }
        
    }
}