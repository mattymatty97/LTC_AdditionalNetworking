using System;
using Unity.Netcode;

namespace AdditionalNetworking.Components
{
    public class ShotgunNetworking: NetworkBehaviour
    {
        private ShotgunItem _shotgunItem;
        
        /// <summary>
        ///  Grab the associated VanillaComponent.
        /// </summary>
        private void Awake()
        {
            _shotgunItem = gameObject.GetComponent<ShotgunItem>();
            if (_shotgunItem == null)
                AdditionalNetworking.Log.LogError($"{nameof(ShotgunNetworking)}#{GetInstanceID()} did not find associated ShotgunItem");
        }
        
        /// <summary>
        ///  request shotgun values upon creation.
        /// </summary>
        private void Start()
        {
            if (!IsServer)
            {
                //if not server request shotgun info
                requestSyncServerRpc();
            }
        }
        
        /// <summary>
        ///  broadcast new ammo count.
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        public void syncAmmoServerRpc(int ammoCount)
        {
            syncAmmoClientRpc(ammoCount);
        }
        
        /// <summary>
        ///  align new ammo count.
        /// </summary>
        [ClientRpc]
        private void syncAmmoClientRpc(int ammoCount, ClientRpcParams clientRpcParams = default)
        {
            _shotgunItem.shellsLoaded = ammoCount;
        }
                
        /// <summary>
        ///  broadcast new safety status.
        /// </summary>
        [ServerRpc(RequireOwnership = true)]
        public void syncSafetyServerRpc(bool safety)
        {
            syncSafetyClientRpc(safety);
        }
                        
        /// <summary>
        ///  align new safety status.
        /// </summary>
        [ClientRpc]
        private void syncSafetyClientRpc(bool safety, ClientRpcParams clientRpcParams = default)
        {
            _shotgunItem.safetyOn = safety;
        }
        
        
        /// <summary>
        ///  request server values for ammo and safety.
        /// </summary>
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