using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class GrabbableNetworking: NetworkBehaviour
    {
        public static GrabbableNetworking Instance { get; private set; }
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
            AdditionalNetworking.Log.LogInfo($"{serverRpcParams.Receive.SenderClientId} registered on {nameof(GrabbableNetworking)}");
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
            AdditionalNetworking.Log.LogInfo($"host has {nameof(GrabbableNetworking)}");
        }
        
        /// <summary>
        ///  align scrap values.
        /// </summary>
        [ClientRpc]
        private void SyncValuesClientRpc(NetworkObjectReference grabbableReference, int scrapValue, int dataValue, ClientRpcParams clientRpcParams = default)
        {
            var grabbableObject = ((GameObject)grabbableReference).GetComponent<GrabbableObject>();
            AdditionalNetworking.Log.LogDebug($"{nameof(GrabbableNetworking)}.SyncValuesClientRpc was called for {grabbableReference.NetworkObjectId}! scrap: {scrapValue}, data: {dataValue}");

            if (grabbableObject.itemProperties.saveItemVariable)
            {
                grabbableObject.LoadItemSaveData(dataValue);
            }
            
            grabbableObject.SetScrapValue(scrapValue);

            grabbableObject.AdditionalNetworking_hasRequestedSync = false;
        }
    
        
        /// <summary>
        ///  request server values for scrap and data.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestValuesServerRpc(NetworkObjectReference grabbableReference, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"{nameof(GrabbableNetworking)}.RequestValuesServerRpc was called for {grabbableReference.NetworkObjectId} by {serverRpcParams.Receive.SenderClientId}!");
            var grabbableObject = ((GameObject)grabbableReference).GetComponent<GrabbableObject>();
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            };
            SyncValuesClientRpc(grabbableReference, grabbableObject.scrapValue, grabbableObject.itemProperties.saveItemVariable?grabbableObject.GetItemDataToSave():0, clientRpcParams);
        }
        
    }
}