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

    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncValuesClientRpcMessage,
            OnSyncValuesClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncServerRpcMessage,
            OnRequestSyncServerRpc);
    }

    private static void SyncValuesClientRpc(NetworkObjectReference grabbableReference, int scrapValue, int dataValue,
        ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(grabbableReference);
        buffer.WriteValue(scrapValue);
        buffer.WriteValue(dataValue);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncValuesClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncValuesClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncValuesClientRpc(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference grabbableReference);
        data.ReadValue(out int scrapValue);
        data.ReadValue(out int dataValue);

        if (!grabbableReference.TryGet(out _) || senderId != NetworkManager.ServerClientId)
            return;

        var grabbableObject = ((GameObject)grabbableReference).GetComponent<global::GrabbableObject>();

        AdditionalNetworking.VerboseLog(LogLevel.Debug,
            () =>
                $"{nameof(GrabbableObject)}.SyncValuesClientRpc was called for {grabbableReference.NetworkObjectId}! scrap: {scrapValue}, data: {dataValue}");

        if (grabbableObject.itemProperties.saveItemVariable) grabbableObject.LoadItemSaveData(dataValue);

        var itemTag = ItemCategory.GetKeyForItem(grabbableObject.itemProperties);

        AdditionalNetworking.VerboseLog(LogLevel.Debug,
            () =>
                $"{nameof(GrabbableObject)}.SyncValuesClientRpc was called for {grabbableReference.NetworkObjectId}! ItemTag: {itemTag}");


        if (AdditionalNetworking.PluginConfig.Value.IgnoreScanNodesList.Contains(itemTag))
        {
            grabbableObject.scrapValue = scrapValue;
            grabbableObject.AdditionalNetworking_isInitialized = true;
        }
        else
            grabbableObject.SetScrapValue(scrapValue);

        grabbableObject.AdditionalNetworking_hasRequestedSync = false;
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

        data.ReadNetworkSerializable(out NetworkObjectReference grabbableReference);

        if (!grabbableReference.TryGet(out _))
            return;

        AdditionalNetworking.VerboseLog(LogLevel.Debug,
            () =>
                $"{nameof(GrabbableObject)}.RequestValuesServerRpc was called for {grabbableReference.NetworkObjectId} by {senderId}!");

        var grabbableObject = ((GameObject)grabbableReference).GetComponent<global::GrabbableObject>();

        SyncValuesClientRpc(grabbableReference, grabbableObject.scrapValue,
            grabbableObject.itemProperties.saveItemVariable ? grabbableObject.GetItemDataToSave() : 0, [senderId]);
    }
}