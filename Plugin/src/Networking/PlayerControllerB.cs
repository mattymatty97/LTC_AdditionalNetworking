using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace AdditionalNetworking.Networking;

public static class PlayerControllerB
{
    private static readonly string BaseName = typeof(PlayerControllerB).FullName;
    private static readonly string SyncInventoryServerRpcMessage = $"{BaseName}|SyncInventoryServerRpc";
    private static readonly string SyncInventoryClientRpcMessage = $"{BaseName}|SyncInventoryClientRpc";
    private static readonly string ThrowExtraItemsClientRpcMessage = $"{BaseName}|ThrowExtraItemsClientRpc";
    private static readonly string SyncSelectedSlotServerRpcMessage = $"{BaseName}|SyncSelectedSlotServerRpc";
    private static readonly string SyncSelectedSlotClientRpcMessage = $"{BaseName}|SyncSelectedSlotClientRpc";
    private static readonly string SyncUsernameServerRpcMessage = $"{BaseName}|SyncUsernameServerRpc";
    private static readonly string SyncUsernameClientRpcMessage = $"{BaseName}|SyncUsernameClientRpc";
    private static readonly string RequestSyncUsernameServerRpcMessage = $"{BaseName}|RequestSyncUsernameServerRpc";

    internal static void RegisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncInventoryServerRpcMessage,
            OnSyncInventoryServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncInventoryClientRpcMessage,
            OnSyncInventoryClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(ThrowExtraItemsClientRpcMessage,
            OnThrowExtraItemsClientRpc);

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSelectedSlotServerRpcMessage,
            OnSyncSelectedSlotServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncSelectedSlotClientRpcMessage,
            OnSyncSelectedSlotClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncUsernameServerRpcMessage,
            OnSyncUsernameServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncUsernameClientRpcMessage,
            OnSyncUsernameClientRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncUsernameServerRpcMessage,
            OnRequestSyncUsernameServerRpc);
    }


    public static void SyncInventoryServerRpc(NetworkObjectReference controllerReference,
        NetworkObjectReference[] inventory, int[] slots)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteNetworkSerializable(inventory);
        buffer.WriteValue(slots);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncInventoryServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncInventoryServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadNetworkSerializable(out NetworkObjectReference[] inventory);
        data.ReadValue(out int[] slots);

        if (!controllerReference.TryGet(out var networkObject) || networkObject.OwnerClientId != senderId)
            return;

        AdditionalNetworking.VerboseLog(LogLevel.Debug, () => $"{nameof(PlayerControllerB)}.syncInventoryServerRpc was called for {controllerReference.NetworkObjectId}!");
        
        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();
        //limit the list to the max slots of the server
        var valid = new List<NetworkObjectReference>();
        var validIds = new List<int>();

        var extra = new List<NetworkObjectReference>();

        for (var index = 0; index < slots.Length; index++)
            if (slots[index] < controllerB.ItemSlots.Length)
            {
                valid.Add(inventory[index]);
                validIds.Add(slots[index]);
            }
            else
            {
                extra.Add(inventory[index]);
            }

        SyncInventoryClientRpc(controllerReference, valid.ToArray(), validIds.ToArray());
        if (extra.Count > 0) ThrowExtraItemsClientRpc(controllerReference, extra.ToArray(), [senderId]);
    }

    private static void SyncInventoryClientRpc(NetworkObjectReference controllerReference,
        NetworkObjectReference[] inventory,
        int[] slots, ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteNetworkSerializable(inventory);
        buffer.WriteValue(slots);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncInventoryClientRpcMessage,
                buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncInventoryClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncInventoryClientRpc(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadNetworkSerializable(out NetworkObjectReference[] inventory);
        data.ReadValue(out int[] slots);

        if (!controllerReference.TryGet(out _) || senderId != NetworkManager.ServerClientId)
            return;

        AdditionalNetworking.VerboseLog(LogLevel.Debug, () => $"{nameof(PlayerControllerB)}.syncInventoryClientRpc was called for {controllerReference.NetworkObjectId}!");
        
        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();

        if (controllerB.IsOwner)
            return;

        //flush the inventory
        controllerB.ItemSlots = new global::GrabbableObject[controllerB.ItemSlots.Length];

        for (var index = 0; index < inventory.Length; index++)
        {
            var slot = slots[index];
            var networkObjectReference = inventory[index];
            if (slot < controllerB.ItemSlots.Length)
            {
                if (networkObjectReference.TryGet(out var networkObject) &&
                    networkObject.TryGetComponent<global::GrabbableObject>(out var grabbableObject))
                    controllerB.ItemSlots[slot] = grabbableObject;
                else
                    controllerB.ItemSlots[slot] = null;
            }
        }

        controllerB.SwitchToItemSlot(controllerB.currentItemSlot);
    }


    private static void ThrowExtraItemsClientRpc(NetworkObjectReference controllerReference,
        NetworkObjectReference[] objectsToThrow, ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteNetworkSerializable(objectsToThrow);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(ThrowExtraItemsClientRpcMessage,
                buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(ThrowExtraItemsClientRpcMessage, targets,
                buffer);
    }

    private static void OnThrowExtraItemsClientRpc(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadNetworkSerializable(out NetworkObjectReference[] objectsToThrow);

        if (!controllerReference.TryGet(out _) || senderId != NetworkManager.ServerClientId)
            return;

        AdditionalNetworking.Log.LogWarning(
            $"{nameof(PlayerControllerB)}.throwExtraItemsClientRpc was called for {controllerReference.NetworkObjectId}!");
        
        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();

        if (!controllerB.IsOwner)
            return;

        foreach (var networkObjectReference in objectsToThrow)
            if (networkObjectReference.TryGet(out var networkObject) &&
                networkObject.TryGetComponent<global::GrabbableObject>(out _))
                controllerB.ThrowObjectServerRpc(networkObjectReference, controllerB.isInElevator,
                    controllerB.isInHangarShipRoom, controllerB.transform.position,
                    (int)controllerB.transform.eulerAngles.y);
    }

    public static void SyncSelectedSlotServerRpc(NetworkObjectReference controllerReference, int selectedSlot)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(selectedSlot);

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSelectedSlotServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }


    private static void OnSyncSelectedSlotServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadValue(out int selectedSlot);

        if (!controllerReference.TryGet(out var networkObject) || networkObject.OwnerClientId != senderId)
            return;

        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();
        if (selectedSlot < 0 || selectedSlot >= controllerB.ItemSlots.Length)
        {
            AdditionalNetworking.Log.LogWarning(
                $"Invalid {nameof(PlayerControllerB)}.syncSelectedSlotServerRpc was called for {controllerReference.NetworkObjectId}, Ignored! slot:{selectedSlot}");
            return;
        }

        AdditionalNetworking.VerboseLog(LogLevel.Debug, () => $"{nameof(PlayerControllerB)}.syncSelectedSlotServerRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot}");

        SyncSelectedSlotClientRpc(controllerReference, selectedSlot);
    }


    private static void SyncSelectedSlotClientRpc(NetworkObjectReference controllerReference, int selectedSlot,
        ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(selectedSlot);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSelectedSlotClientRpcMessage,
                buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSelectedSlotClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncSelectedSlotClientRpc(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadValue(out int selectedSlot);

        if (!controllerReference.TryGet(out _) || NetworkManager.ServerClientId != senderId)
            return;

        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();
        AdditionalNetworking.VerboseLog(LogLevel.Debug, () =>  $"{nameof(PlayerControllerB)}.syncSelectedSlotClientRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot} was:{controllerB.currentItemSlot}");

        if (controllerB.IsOwner)
            return;

        if (controllerB.currentItemSlot != selectedSlot)
            controllerB.SwitchToItemSlot(selectedSlot);
    }

    public static void SyncUsernameServerRpc(NetworkObjectReference controllerReference, string username)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(username);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncUsernameServerRpcMessage, NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncUsernameServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadValue(out string username);

        if (!controllerReference.TryGet(out var networkObject) ||
            (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
            return;

        AdditionalNetworking.Log.LogDebug(
            $"{nameof(PlayerControllerB)}.syncUsernameServerRpc was called for {controllerReference.NetworkObjectId}!");
        
        SyncUsernameClientRpc(controllerReference, username);
    }
    
    
    private static void SyncUsernameClientRpc(NetworkObjectReference controllerReference, string username,
        ulong[] targets = default)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(username);
        if (targets == default)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncUsernameClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncUsernameClientRpcMessage, targets, buffer);
    }

    private static void OnSyncUsernameClientRpc(ulong senderId, FastBufferReader data)
    {
        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
        data.ReadValue(out string username);

        if (!controllerReference.TryGet(out _) || senderId != NetworkManager.ServerClientId)
            return;

        AdditionalNetworking.Log.LogDebug(
            $"{nameof(PlayerControllerB)}.syncUsernameClientRpc was called for {controllerReference.NetworkObjectId}!");
        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();
        if (controllerB.IsOwner)
            return;
        controllerB.playerUsername = username;
        controllerB.usernameBillboardText.text = username;

        StartOfRound.Instance.mapScreen.ChangeNameOfTargetTransform(controllerB.transform, username);

        if (HUDManager.Instance.spectatingPlayerBoxes.ContainsValue(controllerB))
        {
            var spectatorBox = HUDManager.Instance.spectatingPlayerBoxes.First(x => x.Value == controllerB)
                .Key.gameObject;
            spectatorBox.GetComponentInChildren<TextMeshProUGUI>().text = username;
        }
    }

    public static void RequestSyncUsernameServerRpc(NetworkObjectReference controllerReference)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(RequestSyncUsernameServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnRequestSyncUsernameServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);

        if (!controllerReference.TryGet(out _))
            return;

        AdditionalNetworking.Log.LogDebug(
            $"{nameof(PlayerControllerB)}.requestSyncUsernameServerRpc was called for {controllerReference.NetworkObjectId} by {senderId}!");
        var controllerB = ((GameObject)controllerReference).GetComponent<GameNetcodeStuff.PlayerControllerB>();
        //only send update if player is connected!

        if (controllerB.IsOwnedByServer)
            return;

        SyncUsernameClientRpc(controllerReference, controllerB.playerUsername, [senderId]);
    }
}