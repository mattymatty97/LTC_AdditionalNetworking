using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Networking;

public static class Shotgun
{
    private static readonly string BaseName = typeof(Shotgun).FullName;
    private static readonly string SyncAmmoMessage = $"{BaseName}|SyncAmmo";
    private static readonly string SyncSafetyMessage = $"{BaseName}|SyncSafety";
    private static readonly string RequestSyncServerRpcMessage = $"{BaseName}|RequestSyncServerRpc";

    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncAmmoMessage, OnSyncAmmo);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSafetyMessage, OnSyncSafety);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncServerRpcMessage,
            OnRequestSyncServerRpc);
    }

    public static void SyncAmmo(NetworkObjectReference shotgunReference, int ammoCount, ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        buffer.WriteValue(ammoCount);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncAmmoMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncAmmoMessage, targets, buffer);
    }

    private static void OnSyncAmmo(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);
        data.ReadValue(out int ammoCount);

        if (!shotgunReference.TryGet(out var networkObject) ||
            (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
            return;

        var shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();
        if (AdditionalNetworking.PluginConfig.Debug.Verbose.Value)
            AdditionalNetworking.Log.LogDebug(
                $"{nameof(Shotgun)}.SyncAmmo was called for {shotgunReference.NetworkObjectId}! ammo: {ammoCount} was: {shotgunItem.shellsLoaded}");
        if (shotgunItem.IsOwner)
            return;
        shotgunItem.shellsLoaded = ammoCount;
    }


    public static void SyncSafety(NetworkObjectReference shotgunReference, bool safety, ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        buffer.WriteValue(safety);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSafetyMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSafetyMessage, targets, buffer);
    }

    private static void OnSyncSafety(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);
        data.ReadValue(out bool safety);

        if (!shotgunReference.TryGet(out var networkObject) ||
            (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
            return;

        var shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();
        if (AdditionalNetworking.PluginConfig.Debug.Verbose.Value)
            AdditionalNetworking.Log.LogDebug(
                $"{nameof(Shotgun)}.SyncSafety was called for {shotgunReference.NetworkObjectId}! safety:{(safety ? "on" : "off")} was: {(shotgunItem.safetyOn ? "on" : "off")}");
        if (shotgunItem.IsOwner)
            return;
        shotgunItem.safetyOn = safety;
    }

    public static void RequestSyncServerRpc(NetworkObjectReference shotgunReference)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestSyncServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnRequestSyncServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);

        if (!shotgunReference.TryGet(out _))
            return;

        if (AdditionalNetworking.PluginConfig.Debug.Verbose.Value)
            AdditionalNetworking.Log.LogDebug(
                $"{nameof(Shotgun)}.RequestSyncServerRpc was called for {shotgunReference.NetworkObjectId} by {senderId}!");

        var shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();

        SyncAmmo(shotgunReference, shotgunItem.shellsLoaded, [senderId]);
        SyncSafety(shotgunReference, shotgunItem.safetyOn, [senderId]);
    }
}