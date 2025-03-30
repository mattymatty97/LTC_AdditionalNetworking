using System;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace AdditionalNetworking.Networking;

public static class PlayerController
{
    private static readonly string BaseName = typeof(PlayerController).FullName;
    private static readonly string SyncInventoryServerRpcMessage = $"{BaseName}|SyncInventoryServerRpc";
    private static readonly string SyncInventoryClientRpcMessage = $"{BaseName}|SyncInventoryClientRpc";
    private static readonly string ThrowExtraItemsClientRpcMessage = $"{BaseName}|ThrowExtraItemsClientRpc";
    private static readonly string SyncSelectedSlotServerRpcMessage = $"{BaseName}|SyncSelectedSlotServerRpc";
    private static readonly string SyncSelectedSlotClientRpcMessage = $"{BaseName}|SyncSelectedSlotClientRpc";
    private static readonly string SyncCrouchServerRpcMessage = $"{BaseName}|SyncCrouchServerRpc";
    private static readonly string SyncCrouchClientRpcMessage = $"{BaseName}|SyncCrouchClientRpc";
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

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncCrouchServerRpcMessage,
            OnSyncCrouchServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncCrouchClientRpcMessage,
            OnSyncCrouchClientRpc);

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncUsernameServerRpcMessage,
            OnSyncUsernameServerRpc);
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(SyncUsernameClientRpcMessage,
            OnSyncUsernameClientRpc);

        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(RequestSyncUsernameServerRpcMessage,
            OnRequestSyncUsernameServerRpc);
    }

    internal static void UnregisterMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncInventoryServerRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncInventoryClientRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(ThrowExtraItemsClientRpcMessage);

        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSelectedSlotServerRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncSelectedSlotClientRpcMessage);

        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncCrouchServerRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncCrouchClientRpcMessage);

        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncUsernameServerRpcMessage);
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(SyncUsernameClientRpcMessage);

        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
            RequestSyncUsernameServerRpcMessage);
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
        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadNetworkSerializable(out NetworkObjectReference[] inventory);
            data.ReadValue(out int[] slots);

            if (!controllerReference.TryGet(out var networkObject) || networkObject.OwnerClientId != senderId)
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(PlayerController)}.syncInventoryServerRpc was called for {controllerReference.NetworkObjectId}!");

            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
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
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
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
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadNetworkSerializable(out NetworkObjectReference[] inventory);
            data.ReadValue(out int[] slots);

            if (!controllerReference.TryGet(out _))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(PlayerController)}.syncInventoryClientRpc was called for {controllerReference.NetworkObjectId}!");

            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();

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
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
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
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadNetworkSerializable(out NetworkObjectReference[] objectsToThrow);

            if (!controllerReference.TryGet(out _))
                return;

            AdditionalNetworking.Log.LogWarning(
                $"{nameof(PlayerController)}.throwExtraItemsClientRpc was called for {controllerReference.NetworkObjectId}!");

            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();

            if (!controllerB.IsOwner)
                return;

            foreach (var networkObjectReference in objectsToThrow)
                if (networkObjectReference.TryGet(out var networkObject) &&
                    networkObject.TryGetComponent<global::GrabbableObject>(out _))
                    controllerB.ThrowObjectServerRpc(networkObjectReference, controllerB.isInElevator,
                        controllerB.isInHangarShipRoom, controllerB.transform.position,
                        (int)controllerB.transform.eulerAngles.y);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
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

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadValue(out int selectedSlot);

            if (!controllerReference.TryGet(out var networkObject) || networkObject.OwnerClientId != senderId)
                return;

            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            if (selectedSlot < 0 || selectedSlot >= controllerB.ItemSlots.Length)
            {
                AdditionalNetworking.Log.LogWarning(
                    $"Invalid {nameof(PlayerController)}.syncSelectedSlotServerRpc was called for {controllerReference.NetworkObjectId}, Ignored! slot:{selectedSlot}");
                return;
            }

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(PlayerController)}.syncSelectedSlotServerRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot}");

            SyncSelectedSlotClientRpc(controllerReference, selectedSlot);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }


    private static void SyncSelectedSlotClientRpc(NetworkObjectReference controllerReference, int selectedSlot,
        ulong[] targets = null)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(selectedSlot);
        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncSelectedSlotClientRpcMessage,
                buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncSelectedSlotClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncSelectedSlotClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadValue(out int selectedSlot);

            if (!controllerReference.TryGet(out _))
                return;

            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(PlayerController)}.syncSelectedSlotClientRpc was called for {controllerReference.NetworkObjectId}! slot:{selectedSlot} was:{controllerB.currentItemSlot}");

            if (controllerB.IsOwner)
                return;

            if (controllerB.currentItemSlot != selectedSlot)
                controllerB.SwitchToItemSlot(selectedSlot);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    public static void SyncCrouchServerRpc(NetworkObjectReference controllerReference, bool safety)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(safety);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncCrouchServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncCrouchServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadValue(out bool crouched);

            if (!controllerReference.TryGet(out var networkObject) ||
                (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
                return;

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(PlayerController)}.SyncCrouchServerRpc was called for {controllerReference.NetworkObjectId}! crouched:{(crouched ? "yes" : "no")}");

            SyncCrouchClientRpc(controllerReference, crouched);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }


    private static void SyncCrouchClientRpc(NetworkObjectReference controllerReference, bool crouched,
        ulong[] targets = null)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(crouched);
        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(SyncCrouchClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncCrouchClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncCrouchClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadValue(out bool crouched);

            if (!controllerReference.TryGet(out _))
                return;

            var controller = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();

            AdditionalNetworking.VerboseLog(LogLevel.Debug,
                () =>
                    $"{nameof(Shotgun)}.SyncCrouchClientRpc was called for {controllerReference.NetworkObjectId}! safety:{(crouched ? "yes" : "no")} was: {(controller.isCrouching ? "yes" : "no")}");

            if (controller.IsOwner)
                return;

            controller.isCrouching = crouched;
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }

    public static void SyncUsernameServerRpc(NetworkObjectReference controllerReference, string username)
    {
        var buffer = new FastBufferWriter(1024, Allocator.Temp);
        buffer.WriteNetworkSerializable(controllerReference);
        buffer.WriteValue(username);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncUsernameServerRpcMessage,
            NetworkManager.ServerClientId, buffer);
    }

    private static void OnSyncUsernameServerRpc(ulong senderId, FastBufferReader data)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadValue(out string username);

            if (!controllerReference.TryGet(out var networkObject) ||
                (senderId != NetworkManager.ServerClientId && networkObject.OwnerClientId != senderId))
                return;

            AdditionalNetworking.Log.LogDebug(
                $"{nameof(PlayerController)}.syncUsernameServerRpc was called for {controllerReference.NetworkObjectId}!");

            SyncUsernameClientRpc(controllerReference, username);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
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
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(SyncUsernameClientRpcMessage, targets,
                buffer);
    }

    private static void OnSyncUsernameClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);
            data.ReadValue(out string username);

            if (!controllerReference.TryGet(out _))
                return;

            AdditionalNetworking.Log.LogDebug(
                $"{nameof(PlayerController)}.syncUsernameClientRpc was called for {controllerReference.NetworkObjectId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
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
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
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

        try
        {
            data.ReadNetworkSerializable(out NetworkObjectReference controllerReference);

            if (!controllerReference.TryGet(out _))
                return;

            AdditionalNetworking.Log.LogDebug(
                $"{nameof(PlayerController)}.requestSyncUsernameServerRpc was called for {controllerReference.NetworkObjectId} by {senderId}!");
            var controllerB = ((GameObject)controllerReference).GetComponent<PlayerControllerB>();
            //only send update if player is connected!

            if (controllerB.IsOwnedByServer)
                return;

            SyncUsernameClientRpc(controllerReference, controllerB.playerUsername, [senderId]);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError($"Exception during networking: {ex}");
        }
    }
}