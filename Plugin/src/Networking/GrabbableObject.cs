using System;
using AdditionalNetworking.Preloader;
using AdditionalNetworking.Utils;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace AdditionalNetworking.Networking;

public static class GrabbableObject
{
    private static readonly string BaseName = typeof(GrabbableObject).FullName;
    private static readonly string SyncValuesClientRpcMessage = $"{BaseName}|SyncValuesClientRpc";
    private static readonly string RequestSyncServerRpcMessage = $"{BaseName}|RequestSyncServerRpc";
    private static readonly string SyncMultipleValuesClientRpcMessage = $"{BaseName}|SyncMultipleValuesClientRpc";
    private static readonly string MarkValuablesSyncedClientRpcMessage = $"{BaseName}|MarkValuablesSyncedClientRpc";

    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncValuesClientRpcMessage,
            OnSyncValuesClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncServerRpcMessage,
            OnRequestSyncServerRpc);

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncMultipleValuesClientRpcMessage,
            OnSyncMultipleValuesClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(MarkValuablesSyncedClientRpcMessage,
            OnMarkValuablesSyncedClientRpc);
    }

    internal static void UnregisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncValuesClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(RequestSyncServerRpcMessage);

        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
            SyncMultipleValuesClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
            MarkValuablesSyncedClientRpcMessage);
    }

    private static void SyncValuesClientRpc(in GrabbableDataHolder grabbableItem, ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteGrabbableDataHolder(grabbableItem);

        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncValuesClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncValuesClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncValuesClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadGrabbableDataHolder(out var dataHolder);

            var (grabbableReference, scrapValue, dataValue) = dataHolder;

            if (!grabbableReference.TryGet(out _))
                return;

            var grabbableObject = ((GameObject)grabbableReference).GetComponent<global::GrabbableObject>();

            var itemTag = ItemCategory.GetKeyForItem(grabbableObject.itemProperties);

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(GrabbableObject)}.OnSyncValuesClientRpc was called for {grabbableReference.NetworkObjectId}! ItemTag: {itemTag} scrap: {scrapValue}, data: {dataValue}");

            SyncSingleItem(grabbableObject, itemTag, scrapValue, dataValue);

            grabbableObject.SetHasRequestedSync(false);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    private static void SyncSingleItem(in global::GrabbableObject grabbableObject, in string itemTag, in int scrapValue,
        in int dataValue)
    {
        if (grabbableObject.itemProperties.saveItemVariable) grabbableObject.LoadItemSaveData(dataValue);

        if (!grabbableObject.itemProperties.isScrap)
            return;

        if (AdditionalNetworking.PluginConfig.Value.IgnoreScanNodesList.Contains(itemTag))
        {
            grabbableObject.scrapValue = scrapValue;
            grabbableObject.SetIsInitialized(true);
        }
        else
            grabbableObject.SetScrapValue(scrapValue);
    }


    public static void RequestSyncServerRpc(NetworkObjectReference grabbableReference)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(grabbableReference);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestSyncServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnRequestSyncServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference grabbableReference);

            if (!grabbableReference.TryGet(out _))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(GrabbableObject)}.RequestValuesServerRpc was called for {grabbableReference.NetworkObjectId} by {senderId}!");

            var grabbableObject = ((GameObject)grabbableReference).GetComponent<global::GrabbableObject>();

            SyncValuesClientRpc(new GrabbableDataHolder(grabbableObject), [senderId]);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    internal static void SyncMultipleValuesClientRpc(in GrabbableDataHolder[] grabbableItems, ulong[] targets = null)
    {
        // Calculate required buffer size
        var singleItemSize = GrabbableDataHolderSerializer.GrabbableDataHolderSize;
        var requiredSize = FastBufferWriter.GetWriteSize<int>() + (singleItemSize * grabbableItems.Length);

        if (requiredSize > NetworkManager.Singleton.MaximumFragmentedMessageSize)
        {
            AdditionalNetworking.Log.LogFatal(
                $"SyncMultipleValuesClientRpc: {nameof(grabbableItems)} array is too big!");
            return;
        }

        // Create a buffer with the calculated size
        var buffer = new FastBufferWriter(requiredSize, Allocator.Temp);
        // Write the data
        buffer.WriteGrabbableDataHolderArray(grabbableItems);

        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncMultipleValuesClientRpcMessage,
                buffer, NetworkDelivery.ReliableFragmentedSequenced);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncMultipleValuesClientRpcMessage,
                targets,
                buffer, NetworkDelivery.ReliableFragmentedSequenced);
    }

    private static void OnSyncMultipleValuesClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadGrabbableDataHolderArray(out var grabbableItems);

            foreach (var (grabbableReference, scrapValue, dataValue) in grabbableItems)
            {
                if (!grabbableReference.TryGet(out var networkObject))
                    return;

                var grabbableObject = networkObject.GetComponent<global::GrabbableObject>();

                var itemTag = ItemCategory.GetKeyForItem(grabbableObject.itemProperties);

                AdditionalNetworking.VerboseLog(LogLevel.Debug,
                    () =>
                        $"{nameof(GrabbableObject)}.SyncMultipleValuesClientRpc was called for {grabbableReference.NetworkObjectId}! ItemTag: {itemTag} isScrap:{grabbableObject.itemProperties.isScrap}, value: {scrapValue}, hasData:{grabbableObject.itemProperties.saveItemVariable}, data: {dataValue}");

                SyncSingleItem(grabbableObject, itemTag, scrapValue, dataValue);
            }
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    internal static void MarkValuablesSyncedClientRpc(ulong[] targets = null)
    {
        // Create a buffer with the calculated size
        var buffer = new FastBufferWriter(0, Allocator.Temp);

        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(MarkValuablesSyncedClientRpcMessage,
                buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(MarkValuablesSyncedClientRpcMessage,
                targets, buffer);
    }

    private static void OnMarkValuablesSyncedClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(GrabbableObject)}.MarkValuablesSyncedClientRpc was called!");

            StartOfRound.Instance.SetValuablesSynced(true);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }
}
