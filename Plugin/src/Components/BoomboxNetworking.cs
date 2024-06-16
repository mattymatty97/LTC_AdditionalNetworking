using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Components
{
    public class BoomboxNetworking: NetworkBehaviour
    {
        public static BoomboxNetworking Instance { get; private set; }
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
            AdditionalNetworking.Log.LogInfo($"{serverRpcParams.Receive.SenderClientId} registered on {nameof(BoomboxNetworking)}");
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
            AdditionalNetworking.Log.LogInfo($"host has {nameof(BoomboxNetworking)}");
        }
        
        /// <summary>
        ///  broadcast new boombox state.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SyncStateServerRpc(NetworkObjectReference boomboxReference, bool playing, int track, ServerRpcParams serverRpcParams = default)
        {            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ValidClientIDs.ToArray()
                }
            };
            AdditionalNetworking.Log.LogDebug($"{nameof(BoomboxNetworking)}.SyncStateServerRpc was called for {boomboxReference.NetworkObjectId}! track: {track}, playing: {playing}");
            SyncStateClientRpc(boomboxReference, playing, track, clientRpcParams);
        }
        
        /// <summary>
        ///  align new boombox state.
        /// </summary>
        [ClientRpc]
        private void SyncStateClientRpc(NetworkObjectReference boomboxReference, bool playing, int track, ClientRpcParams clientRpcParams = default)
        {
            var boomboxItem = ((GameObject)boomboxReference).GetComponent<BoomboxItem>();
            var oldTrack = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
            var oldState = boomboxItem.isPlayingMusic;
            AdditionalNetworking.Log.LogDebug($"{nameof(BoomboxNetworking)}.SyncStateClientRpc was called for {boomboxReference.NetworkObjectId}! track: {track}, playing: {playing} was track: {oldTrack}, playing: {oldState}");
            
            if (boomboxItem.IsOwner)
                return;
            
            //if we need to stop playing
            if (!playing)
            {
                //if it was already off do nothing
                if (oldState)
                    boomboxItem.StartMusic(false,false);
                return;
            }
            
            //if all is fine do nothing
            if (track == -1 || (oldState && oldTrack == track))
                return;
            
            //make sure we play the right track!
            boomboxItem.isPlayingMusic = true;
            boomboxItem.isBeingUsed = true;
            boomboxItem.boomboxAudio.Stop();
            boomboxItem.boomboxAudio.clip = boomboxItem.musicAudios[track];
            boomboxItem.boomboxAudio.pitch = 1f;
            boomboxItem.boomboxAudio.Play();
        }
    
        
        /// <summary>
        ///  request server boombox state.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestSyncServerRpc(NetworkObjectReference boomboxReference, ServerRpcParams serverRpcParams = default)
        {
            AdditionalNetworking.Log.LogDebug($"{nameof(BoomboxNetworking)}.requestSyncServerRpc was called for {boomboxReference.NetworkObjectId} by {serverRpcParams.Receive.SenderClientId}!");
            var boomboxItem = ((GameObject)boomboxReference).GetComponent<BoomboxItem>();
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { serverRpcParams.Receive.SenderClientId }
                }
            };
            var track = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
            var state = boomboxItem.isPlayingMusic;
            SyncStateClientRpc(boomboxReference, state, track, clientRpcParams);
        }
        
    }
}