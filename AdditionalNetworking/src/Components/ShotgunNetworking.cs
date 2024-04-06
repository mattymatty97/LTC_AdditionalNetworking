using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class ShotgunNetworking: NetworkBehaviour
    {
        public static ShotgunNetworking Instance { get; private set; }
        
        /// <summary>
        ///  Set the Instance
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }
        
        /// <summary>
        ///  broadcast new ammo count.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncAmmoServerRpc(NetworkObjectReference shotgunReference, int ammoCount)
        {
            syncAmmoClientRpc(shotgunReference, ammoCount);
        }
        
        /// <summary>
        ///  align new ammo count.
        /// </summary>
        [ClientRpc]
        private void syncAmmoClientRpc(NetworkObjectReference shotgunReference, int ammoCount, ClientRpcParams clientRpcParams = default)
        {
            var _shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();
            _shotgunItem.shellsLoaded = ammoCount;
        }
                
        /// <summary>
        ///  broadcast new safety status.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncSafetyServerRpc(NetworkObjectReference shotgunReference, bool safety)
        {
            syncSafetyClientRpc(shotgunReference, safety);
        }
                        
        /// <summary>
        ///  align new safety status.
        /// </summary>
        [ClientRpc]
        private void syncSafetyClientRpc(NetworkObjectReference shotgunReference, bool safety, ClientRpcParams clientRpcParams = default)
        {
            var _shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();
            _shotgunItem.safetyOn = safety;
        }
        
        
        /// <summary>
        ///  request server values for ammo and safety.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void requestSyncServerRpc(NetworkObjectReference shotgunReference, ServerRpcParams serverRpcParams = default)
        {
            var _shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            syncAmmoClientRpc(shotgunReference, _shotgunItem.shellsLoaded, clientRpcParams);
            syncSafetyClientRpc(shotgunReference, _shotgunItem.safetyOn, clientRpcParams);
        }
        
    }
}