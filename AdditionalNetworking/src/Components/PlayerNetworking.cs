using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;

namespace AdditionalNetworking.Components
{
    public class PlayerNetworking: NetworkBehaviour
    {
        private PlayerControllerB _controllerB;
        
        /// <summary>
        ///  Grab the associated VanillaComponent.
        /// </summary>
        private void Awake()
        {
            _controllerB = gameObject.GetComponent<PlayerControllerB>();
            if (_controllerB == null)
                AdditionalNetworking.Log.LogError($"{nameof(PlayerNetworking)}#{GetInstanceID()} did not find associated PlayerControllerB");
        }

        /// <summary>
        ///  Request usernames after connecting.
        /// </summary>
        private void Start()
        {
            if (!IsServer)
            {
                //only run if we're a client
                requestSyncUsernameServerRpc();
            }
        }
        
        /// <summary>
        ///  broadcast new inventory order.
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        public void syncInventoryServerRpc(NetworkObjectReference[] inventory, int[] slots, ServerRpcParams serverRpcParams = default)
        {
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
            
            syncInventoryClientRpc(valid.ToArray(), valid_ids.ToArray());
            if (extra.Count > 0)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                    }
                };
                throwExtraItemsClientRpc(extra.ToArray(), clientRpcParams);
            }
        }
        
        /// <summary>
        ///  align the inventory.
        ///  if we're the owner do not update the current inventory but check if the server has truncated the request
        /// </summary>
        [ClientRpc]
        private void syncInventoryClientRpc(NetworkObjectReference[] inventory, int[] slots)
        {
            HashSet<GrabbableObject> missingObjects = new HashSet<GrabbableObject>(_controllerB.ItemSlots.Where(g=>g!=null));
            if (!IsOwner)
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
                        if (!IsOwner)
                            _controllerB.ItemSlots[slot] = grabbableObject;
                    }
                    else
                    {
                        if (!IsOwner)
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
                throwExtraItemsClientRpc(missingObjects.Select(g => (NetworkObjectReference)g.NetworkObject).ToArray(),clientRpcParams);
            }
            
            if (_controllerB.currentlyHeldObjectServer != _controllerB.ItemSlots[_controllerB.currentItemSlot])
                _controllerB.SwitchToItemSlot(_controllerB.currentItemSlot);
        }

        /// <summary>
        ///  safely drop requested items.
        /// </summary>
        [ClientRpc]
        private void throwExtraItemsClientRpc(NetworkObjectReference[] objectsToThrow, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner)
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
        [ServerRpc(RequireOwnership = true)]
        public void syncSelectedSlotServerRpc(int selectedSlot)
        {
            syncSelectedSlotClientRpc(selectedSlot);
        }        
                
        /// <summary>
        ///  align new held item.
        /// </summary>
        [ClientRpc]
        private void syncSelectedSlotClientRpc(int selectedSlot)
        {
            if (IsOwner)
                return;
            
            if (_controllerB.currentItemSlot != selectedSlot)
                _controllerB.SwitchToItemSlot(_controllerB.currentItemSlot);
        }

        /// <summary>
        ///  broadcast name change.
        ///  ( de-sync typically in lateJoin cases )
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        public void syncUsernameServerRpc(string username)
        {
            syncUsernameClientRpc(username);
        }
        
        /// <summary>
        ///  align player name.
        /// </summary>
        [ClientRpc]
        public void syncUsernameClientRpc(string username, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
                return;
            _controllerB.playerUsername = username;
            _controllerB.usernameBillboardText.text = username;
            //TODO update spectating boxes and radar
        }
        
        
        /// <summary>
        ///  request server username value.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void requestSyncUsernameServerRpc(ServerRpcParams serverRpcParams = default)
        {
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
            syncUsernameClientRpc(_controllerB.playerUsername, clientRpcParams);
        }
    }
}