using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class ParentNetworking: NetworkBehaviour
    {
        public static ParentNetworking Instance { get; private set; }
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
            AdditionalNetworking.Log.LogInfo($"{serverRpcParams.Receive.SenderClientId} registered on {nameof(ParentNetworking)}");
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
            AdditionalNetworking.Log.LogInfo($"host has {nameof(ParentNetworking)}");
        }
        
        /// <summary>
        ///  broadcast new parent.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncParentServerRpc(NetworkObjectReference networkObjectReference, NetworkObjectReference parentReference, bool worldPositionStays, ServerRpcParams serverRpcParams = default)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"{nameof(ShotgunNetworking)}.SyncParentServerRpc was called for {networkObjectReference.NetworkObjectId}! parent: {parentReference.NetworkObjectId} worldPositionStays: {worldPositionStays}");
            SyncParentClientRpc(networkObjectReference, parentReference, worldPositionStays, clientRpcParams);
        }        
        
        /// <summary>
        ///  broadcast remove parent.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RemoveParentServerRpc(NetworkObjectReference networkObjectReference, bool worldPositionStays, ServerRpcParams serverRpcParams = default)
        {
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"{nameof(ShotgunNetworking)}.RemoveParentServerRpc was called for {networkObjectReference.NetworkObjectId}! worldPositionStays: {worldPositionStays}");
            RemoveParentClientRpc(networkObjectReference, worldPositionStays, clientRpcParams);
        }
        
        /// <summary>
        ///  align new parent.
        /// </summary>
        [ClientRpc]
        public void SyncParentClientRpc(NetworkObjectReference networkObjectReference, NetworkObjectReference parentReference, bool worldPositionStays, ClientRpcParams clientRpcParams = default)
        {
            var networkObject = (NetworkObject)networkObjectReference;
            var parentObject = (NetworkObject)parentReference;
            
            AdditionalNetworking.Log.LogDebug($"{nameof(ShotgunNetworking)}.SyncParentClientRpc was called for {networkObjectReference.NetworkObjectId}! parent: {parentReference.NetworkObjectId} worldPositionStays: {worldPositionStays}");
            networkObject.transform.SetParent(parentObject.transform, worldPositionStays);
        }        
        
        /// <summary>
        ///  align remove parent.
        /// </summary>
        [ClientRpc]
        public void RemoveParentClientRpc(NetworkObjectReference networkObjectReference, bool worldPositionStays, ClientRpcParams clientRpcParams = default)
        {
            var networkObject = (NetworkObject)networkObjectReference;
            
            AdditionalNetworking.Log.LogDebug($"{nameof(ShotgunNetworking)}.RemoveParentClientRpc was called for {networkObjectReference.NetworkObjectId}! worldPositionStays: {worldPositionStays}");
            networkObject.transform.SetParent(null, worldPositionStays);
        }
        
        /// <summary>
        ///  request server parent.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestParentServerRpc(NetworkObjectReference networkObjectReference, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"{nameof(ShotgunNetworking)}.RequestParentServerRpc was called for {networkObjectReference.NetworkObjectId} by {serverRpcParams.Receive.SenderClientId}!");
            var networkObject = (NetworkObject)networkObjectReference;
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[]{serverRpcParams.Receive.SenderClientId}
                }
            };
            if (networkObject.transform.parent)
                SyncParentClientRpc(networkObjectReference, networkObject.transform.parent.gameObject, false);
            else
                RemoveParentClientRpc(networkObjectReference, false);
        }
        
    }
}