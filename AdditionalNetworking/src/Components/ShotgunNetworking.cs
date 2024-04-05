using System;
using Unity.Netcode;

namespace AdditionalNetworking.Components
{
    public class ShotgunNetworking: NetworkBehaviour
    {
        private ShotgunItem _shotgunItem;

        private void Awake()
        {
            _shotgunItem = gameObject.GetComponent<ShotgunItem>();
            if (_shotgunItem == null)
                AdditionalNetworking.Log.LogError($"{nameof(ShotgunNetworking)}#{GetInstanceID()} did not find associated ShotgunItem");
        }

        private void Start()
        {
            if (!IsServer)
            {
                //if not server request shotgun info
                requestSyncServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = true)]
        public void syncAmmoServerRpc(int ammoCount)
        {
            syncAmmoClientRpc(ammoCount);
        }
        
        [ClientRpc]
        private void syncAmmoClientRpc(int ammoCount, ClientRpcParams clientRpcParams = default)
        {
            _shotgunItem.shellsLoaded = ammoCount;
        }
        
        [ServerRpc(RequireOwnership = true)]
        public void syncSafetyServerRpc(bool safety)
        {
            syncSafetyClientRpc(safety);
        }
        
        [ClientRpc]
        private void syncSafetyClientRpc(bool safety, ClientRpcParams clientRpcParams = default)
        {
            _shotgunItem.safetyOn = safety;
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void requestSyncServerRpc(ServerRpcParams serverRpcParams = default)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            syncAmmoClientRpc(_shotgunItem.shellsLoaded, clientRpcParams);
            syncSafetyClientRpc(_shotgunItem.safetyOn, clientRpcParams);
        }
        
    }
}