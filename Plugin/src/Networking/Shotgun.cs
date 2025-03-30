using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace AdditionalNetworking.Networking;

public static class Shotgun
{
    private static readonly string BaseName = typeof(Shotgun).FullName;
    private static readonly string SyncAmmoClientRpcMessage = $"{BaseName}|SyncAmmoClientRpc";
    private static readonly string SyncAmmoServerRpcMessage = $"{BaseName}|SyncAmmoServerRpc";
    private static readonly string SyncSafetyClientRpcMessage = $"{BaseName}|SyncSafetyClientRpc";
    private static readonly string SyncSafetyServerRpcMessage = $"{BaseName}|SyncSafetyServerRpc";
    private static readonly string RequestSyncServerRpcMessage = $"{BaseName}|RequestSyncServerRpc";

    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncAmmoServerRpcMessage,
            OnSyncAmmoServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncAmmoClientRpcMessage,
            OnSyncAmmoClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSafetyServerRpcMessage,
            OnSyncSafetyServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSafetyClientRpcMessage,
            OnSyncSafetyClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncServerRpcMessage,
            OnRequestSyncServerRpc);
    }

    internal static void UnregisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncAmmoServerRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncAmmoClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSafetyServerRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSafetyClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(RequestSyncServerRpcMessage);
    }

    public static void SyncAmmoServerRpc(NetworkObjectReference shotgunReference, int ammoCount)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        buffer.WriteValue(ammoCount);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncAmmoServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncAmmoServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);
            data.ReadValue(out int ammoCount);

            if (!shotgunReference.TryGet(out var networkObject) ||
                (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Shotgun)}.SyncAmmoServerRpc was called for {shotgunReference.NetworkObjectId}! ammo: {ammoCount}");

            SyncAmmoClientRpc(shotgunReference, ammoCount);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }


    private static void SyncAmmoClientRpc(NetworkObjectReference shotgunReference, int ammoCount,
        ulong[] targets = null)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        buffer.WriteValue(ammoCount);
        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncAmmoClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncAmmoClientRpcMessage, targets, buffer);
    }

    private static void OnSyncAmmoClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);
            data.ReadValue(out int ammoCount);

            if (!shotgunReference.TryGet(out _))
                return;

            var shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Shotgun)}.SyncAmmoClientRpc was called for {shotgunReference.NetworkObjectId}! ammo: {ammoCount} was: {shotgunItem.shellsLoaded}");

            if (shotgunItem.IsOwner)
                return;

            shotgunItem.shellsLoaded = ammoCount;
            switch (ammoCount)
            {
                case 0:
                    shotgunItem.shotgunShellLeft.enabled = false;
                    shotgunItem.shotgunShellRight.enabled = false;
                    break;
                case 1:
                    shotgunItem.shotgunShellLeft.enabled = true;
                    shotgunItem.shotgunShellRight.enabled = false;
                    break;
                default:
                    shotgunItem.shotgunShellLeft.enabled = true;
                    shotgunItem.shotgunShellRight.enabled = true;
                    break;
            }

            var localController = GameNetworkManager.Instance.localPlayerController;
            if (!localController || localController.currentlyHeldObjectServer != shotgunItem)
                return;

            HUDManager.Instance.ClearControlTips();
            shotgunItem.SetControlTipsForItem();
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    public static void SyncSafetyServerRpc(NetworkObjectReference shotgunReference, bool safety)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        buffer.WriteValue(safety);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSafetyServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncSafetyServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);
            data.ReadValue(out bool safety);

            if (!shotgunReference.TryGet(out var networkObject) ||
                (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Shotgun)}.SyncSafetyServerRpc was called for {shotgunReference.NetworkObjectId}! safety:{(safety ? "on" : "off")}");

            SyncSafetyClientRpc(shotgunReference, safety);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }


    private static void SyncSafetyClientRpc(NetworkObjectReference shotgunReference, bool safety,
        ulong[] targets = null)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(shotgunReference);
        buffer.WriteValue(safety);
        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSafetyClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSafetyClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncSafetyClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);
            data.ReadValue(out bool safety);

            if (!shotgunReference.TryGet(out _))
                return;

            var shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Shotgun)}.SyncSafetyClientRpc was called for {shotgunReference.NetworkObjectId}! safety:{(safety ? "on" : "off")} was: {(shotgunItem.safetyOn ? "on" : "off")}");

            if (shotgunItem.IsOwner)
                return;

            shotgunItem.safetyOn = safety;

            var localController = GameNetworkManager.Instance.localPlayerController;
            if (!localController || localController.currentlyHeldObjectServer != shotgunItem)
                return;

            HUDManager.Instance.ClearControlTips();
            shotgunItem.SetControlTipsForItem();
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
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
        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference shotgunReference);

            if (!shotgunReference.TryGet(out _))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Shotgun)}.RequestSyncServerRpc was called for {shotgunReference.NetworkObjectId} by {senderId}!");

            var shotgunItem = ((GameObject)shotgunReference).GetComponent<ShotgunItem>();

            SyncAmmoClientRpc(shotgunReference, shotgunItem.shellsLoaded, [senderId]);
            SyncSafetyClientRpc(shotgunReference, shotgunItem.safetyOn, [senderId]);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }
}
