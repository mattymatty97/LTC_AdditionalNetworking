using System;
using Unity.Netcode;

namespace AdditionalNetworking.Components
{
    public class RadarNetworking: NetworkBehaviour
    {
        private ManualCameraRenderer _radarCamera;
        private void Awake()
        {
            _radarCamera = gameObject.GetComponent<ManualCameraRenderer>();
            if (_radarCamera == null)
                AdditionalNetworking.Log.LogError($"{nameof(RadarNetworking)}#{GetInstanceID()} did not find associated ManualCameraRenderer (Radar)");
        }

        private void Start()
        {
            if (!IsServer)
            {
                //if not server request current radar list
            }
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
        }
        
    }
}