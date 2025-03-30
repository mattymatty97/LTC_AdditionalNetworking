using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace AdditionalNetworking.Networking;

public static class AnimatedObject
{
    private static readonly string BaseName = typeof(AnimatedObject).FullName;
    private static readonly string SyncAudioStateServerRpcMessage = $"{BaseName}|SyncAudioStateServerRpc";
    private static readonly string SyncAudioStateClientRpcMessage = $"{BaseName}|SyncAudioStateClientRpc";
    private static readonly string RequestSyncServerRpcMessage = $"{BaseName}|RequestSyncServerRpc";


    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncAudioStateServerRpcMessage,
            OnSyncAudioStateServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncAudioStateClientRpcMessage,
            OnSyncAudioStateClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncServerRpcMessage,
            OnRequestSyncServerRpc);
    }

    public static void SyncStateServerRpc(NetworkObjectReference itemReference, bool playing)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(itemReference);
        buffer.WriteValue(playing);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncAudioStateServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncAudioStateServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference itemReference);
            data.ReadValue(out bool playing);

            if (!itemReference.TryGet(out var networkObject) ||
                (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(AnimatedObject)}.SyncStateServerRpc was called for {itemReference.NetworkObjectId}! playing: {playing}");

            SyncStateClientRpc(itemReference, playing);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    private static void SyncStateClientRpc(NetworkObjectReference itemReference, bool playing,
        ulong[] targets = null)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(itemReference);
        buffer.WriteValue(playing);
        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncAudioStateClientRpcMessage,
                buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncAudioStateClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncAudioStateClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;
        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference itemReference);
            data.ReadValue(out bool playing);

            if (!itemReference.TryGet(out _))
                return;

            var animatedItem = ((GameObject)itemReference).GetComponent<AnimatedItem>();
            var oldState = animatedItem.itemAudio.isPlaying;
            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Boombox)}.SyncStateClientRpc was called for {itemReference.NetworkObjectId}! playing: {playing} was: {oldState}");

            if (animatedItem.IsOwner)
                return;

            if (playing == oldState)
                return;

            var itemAudio = animatedItem.itemAudio;

            if (playing)
            {
                itemAudio.clip = animatedItem.grabAudio;
                itemAudio.loop = animatedItem.loopGrabAudio;
                itemAudio.Play();
            }
            else
                itemAudio.Stop();
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }


    public static void RequestSyncServerRpc(NetworkObjectReference itemReference)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(itemReference);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestSyncServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnRequestSyncServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference itemReference);

            if (!itemReference.TryGet(out _))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Boombox)}.RequestSyncServerRpc was called for {itemReference.NetworkObjectId} by {senderId}!");

            var animatedItem = ((GameObject)itemReference).GetComponent<AnimatedItem>();

            var state = animatedItem.itemAudio.isPlaying;
            SyncStateClientRpc(itemReference, state, [senderId]);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }
}