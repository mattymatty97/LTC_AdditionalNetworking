using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;

namespace AdditionalNetworking.Components
{
    public class PlayerNetworking: NetworkBehaviour
    {
        private PlayerControllerB _controllerB;
        private void Awake()
        {
            _controllerB = gameObject.GetComponent<PlayerControllerB>();
            if (_controllerB == null)
                AdditionalNetworking.Log.LogError($"{nameof(PlayerNetworking)}#{GetInstanceID()} did not find associated PlayerControllerB");
        }

        private void Start()
        {
            if (!IsServer)
            {
                requestSyncUsernameServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = true)]
        public void syncInventoryServerRpc(NetworkObjectReference[] inventory)
        {
            //limit the list to the max slots of the server
            syncInventoryClientRpc(inventory.Take(_controllerB.ItemSlots.Length).ToArray());
        }
        
        [ClientRpc]
        private void syncInventoryClientRpc(NetworkObjectReference[] inventory)
        {
            HashSet<GrabbableObject> missingObjects = new HashSet<GrabbableObject>(_controllerB.ItemSlots.Where(g=>g!=null));
            var index = 0;
            foreach (var networkObjectReference in inventory)
            {
                if (networkObjectReference.TryGet(out var networkObject) && networkObject.TryGetComponent<GrabbableObject>(out var grabbableObject))
                {
                    missingObjects.Remove(grabbableObject);
                    if (!IsOwner)
                        _controllerB.ItemSlots[index] = grabbableObject;
                }
                else
                {
                    if (!IsOwner)
                        _controllerB.ItemSlots[index] = null;
                }

                index++;
                if (index > _controllerB.ItemSlots.Length)
                {
                    //TODO: handle too many slots
                    break;
                }
            }

            if (IsOwner && missingObjects.Count > 0)
            {
                //should never happen but it's a good idea to handle that case
                foreach (var grabbableObject in missingObjects)
                {
                    _controllerB.ThrowObjectServerRpc((NetworkObjectReference)grabbableObject.NetworkObject, _controllerB.isInElevator, _controllerB.isInHangarShipRoom, default, default);
                }
            }
            
            if (_controllerB.currentlyHeldObjectServer != _controllerB.ItemSlots[_controllerB.currentItemSlot])
                _controllerB.SwitchToItemSlot(_controllerB.currentItemSlot);
        }
        
        [ServerRpc(RequireOwnership = true)]
        public void syncSelectedSlotServerRpc(int selectedSlot)
        {
            syncSelectedSlotClientRpc(selectedSlot);
        }        
        
        [ClientRpc]
        private void syncSelectedSlotClientRpc(int selectedSlot)
        {
            if (IsOwner)
                return;
            
            if (_controllerB.currentItemSlot != selectedSlot)
                _controllerB.SwitchToItemSlot(_controllerB.currentItemSlot);
        }

        [ServerRpc(RequireOwnership = true)]
        public void syncUsernameServerRpc(string username)
        {
            syncUsernameClientRpc(username);
        }
        
        [ClientRpc]
        public void syncUsernameClientRpc(string username, ClientRpcParams clientRpcParams = default)
        {
            if (IsOwner)
                return;
            _controllerB.playerUsername = username;
            _controllerB.usernameBillboardText.text = username;
        }
        
                
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