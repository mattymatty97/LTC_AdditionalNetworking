using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace AdditionalNetworking.Networking;

public static class Boombox
{
    private static readonly string BaseName = typeof(Boombox).FullName;
    private static readonly string SyncStateServerRpcMessage = $"{BaseName}|SyncStateServerRpc";
    private static readonly string SyncStateClientRpcMessage = $"{BaseName}|SyncStateClientRpc";
    private static readonly string RequestSyncServerRpcMessage = $"{BaseName}|RequestSyncServerRpc";

    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncStateServerRpcMessage, OnSyncStateServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncStateClientRpcMessage, OnSyncStateClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncServerRpcMessage, OnRequestSyncServerRpc);
    }
    
    public static void SyncStateServerRpc(NetworkObjectReference boomboxReference, bool playing, int track)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(boomboxReference);
        buffer.WriteValue(playing);
        buffer.WriteValue(track);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncStateServerRpcMessage, NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncStateServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        data.ReadNetworkSerializable(out NetworkObjectReference boomboxReference);
        data.ReadValue(out bool playing);
        data.ReadValue(out int track);

        if (!boomboxReference.TryGet(out var networkObject) ||
            (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
            return;
        
        AdditionalNetworking.VerboseLog(LogLevel.Debug, () => $"{nameof(Boombox)}.SyncStateServerRpc was called for {boomboxReference.NetworkObjectId}! track: {track}, playing: {playing}");

        SyncStateClientRpc(boomboxReference, playing, track);
    }

    private static void SyncStateClientRpc(NetworkObjectReference boomboxReference, bool playing, int track,
        ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(boomboxReference);
        buffer.WriteValue(playing);
        buffer.WriteValue(track);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncStateClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncStateClientRpcMessage, targets, buffer);
    }

    private static void OnSyncStateClientRpc(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference boomboxReference);
        data.ReadValue(out bool playing);
        data.ReadValue(out int track);

        if (!boomboxReference.TryGet(out _) || senderId != NetworkManager.ServerClientId)
            return;

        var boomboxItem = ((GameObject)boomboxReference).GetComponent<BoomboxItem>();
        var oldTrack = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
        var oldState = boomboxItem.isPlayingMusic;
        AdditionalNetworking.VerboseLog(LogLevel.Debug, () => $"{nameof(Boombox)}.SyncStateClientRpc was called for {boomboxReference.NetworkObjectId}! track: {track}, playing: {playing} was track: {oldTrack}, playing: {oldState}");

        if (boomboxItem.IsOwner)
            return;

        //if we need to stop playing
        if (!playing)
        {
            //if it was already off do nothing
            if (oldState)
                boomboxItem.StartMusic(false);
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


    public static void RequestSyncServerRpc(NetworkObjectReference boomboxReference)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(boomboxReference);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestSyncServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnRequestSyncServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        data.ReadNetworkSerializable(out NetworkObjectReference boomboxReference);

        if (!boomboxReference.TryGet(out _))
            return;

        AdditionalNetworking.VerboseLog(LogLevel.Debug, () => $"{nameof(Boombox)}.RequestSyncServerRpc was called for {boomboxReference.NetworkObjectId} by {senderId}!");
        
        var boomboxItem = ((GameObject)boomboxReference).GetComponent<BoomboxItem>();

        var track = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
        var state = boomboxItem.isPlayingMusic;
        SyncStateClientRpc(boomboxReference, state, track, [senderId]);
    }
}