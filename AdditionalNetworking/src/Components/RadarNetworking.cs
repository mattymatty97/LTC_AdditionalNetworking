using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class RadarNetworking: NetworkBehaviour
    {
        public static RadarNetworking Instance { get; private set; }
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
            AdditionalNetworking.Log.LogInfo($"{serverRpcParams.Receive.SenderClientId} registered on {nameof(RadarNetworking)}");
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
            AdditionalNetworking.Log.LogInfo($"host has {nameof(RadarNetworking)}");
            //align radar targets
            requestSyncRadarRpc();
        }
        
        /// <summary>
        ///  broadcast new ammo count.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void syncRadarServerRpc()
        {            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"syncRadarServerRpc was called!");
            var targetList = StartOfRound.Instance.mapScreen.radarTargets;
            syncRadarClientRpc(
                targetList.Select(tn=>(NetworkObjectReference)tn.transform.gameObject.GetComponent<NetworkObject>()).ToArray(),
                targetList.Select(tn=>tn.name).ToArray(),
                clientRpcParams);
        }
        
        /// <summary>
        ///  align new ammo count.
        /// </summary>
        [ClientRpc]
        private void  syncRadarClientRpc(NetworkObjectReference[] radarTargets, string[] radarNames, ClientRpcParams clientRpcParams = default)
        {
            if (IsServer)
                return;
            AdditionalNetworking.Log.LogDebug($"syncRadarClientRpc was called!");
            var targetList = StartOfRound.Instance.mapScreen.radarTargets;
            targetList.Clear();
            for (var i = 0; i < radarTargets.Length; i++)
            {
                var currentTarget = radarTargets[i];
                var currentName = radarNames[i];
                if (currentTarget.TryGet(out var networkObject))
                {
                    targetList.Add(new TransformAndName(networkObject.transform, currentName, !networkObject.TryGetComponent<PlayerControllerB>(out _)));
                }
            }
        }

        
        /// <summary>
        ///  request server values for ammo and safety.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void requestSyncRadarRpc(ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"requestSyncRadarRpc was called by {serverRpcParams.Receive.SenderClientId}!");
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            var targetList = StartOfRound.Instance.mapScreen.radarTargets;
            syncRadarClientRpc(
                targetList.Select(tn=>(NetworkObjectReference)tn.transform.gameObject.GetComponent<NetworkObject>()).ToArray(),
                targetList.Select(tn=>tn.name).ToArray(),
                clientRpcParams);
        }
        
    }
}