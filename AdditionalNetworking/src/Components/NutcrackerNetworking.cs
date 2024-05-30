using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class NutcrackerNetworking: NetworkBehaviour
    {
        public static NutcrackerNetworking Instance { get; private set; }
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
            AdditionalNetworking.Log.LogInfo($"{serverRpcParams.Receive.SenderClientId} registered on {nameof(NutcrackerNetworking)}");
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
            AdditionalNetworking.Log.LogInfo($"host has {nameof(NutcrackerNetworking)}");
        }
        
        /// <summary>
        ///  broadcast torsoTarget.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncTorsoServerRpc(NetworkObjectReference nutcrackerEnemy, int targetTorsoDegrees, ServerRpcParams serverRpcParams = default)
        {
           
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            
            AdditionalNetworking.Log.LogDebug($"{nameof(NutcrackerNetworking)}.SyncTorsoServerRpc was called for {nutcrackerEnemy.NetworkObjectId}! Degrees: {targetTorsoDegrees}");
            SyncTorsoClientRpc(nutcrackerEnemy, targetTorsoDegrees, clientRpcParams);
        }
        
        /// <summary>
        ///  align new ammo count.
        /// </summary>
        [ClientRpc]
        private void SyncTorsoClientRpc(NetworkObjectReference nutcrackerEnemy, int targetTorsoDegrees, ClientRpcParams clientRpcParams = default)
        {
            var nutcrackerEnemyAI = ((GameObject)nutcrackerEnemy).GetComponent<NutcrackerEnemyAI>();
            AdditionalNetworking.Log.LogDebug($"{nameof(NutcrackerNetworking)}.SyncTorsoClientRpc was called for {nutcrackerEnemy.NetworkObjectId}! Degrees: {targetTorsoDegrees} was: {nutcrackerEnemyAI.targetTorsoDegrees}");
            if (nutcrackerEnemyAI.IsOwner)
                return;
            nutcrackerEnemyAI.targetTorsoDegrees = targetTorsoDegrees;
        }
        
    }
}