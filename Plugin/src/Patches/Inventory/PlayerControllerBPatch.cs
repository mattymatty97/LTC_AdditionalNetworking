using System;
using AdditionalNetworking.Interfaces;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Utils;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine.Pool;

namespace AdditionalNetworking.Patches.Inventory;

[HarmonyPatch]
internal class PlayerControllerBPatch
{
    /// <summary>
    ///     Request the username.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Start))]
    private static void OnStart(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Misc.Username.Value)
            return;

        if (__instance.IsServer)
            return;

        try
        {
            PlayerController.RequestSyncUsernameServerRpc(__instance.NetworkObject);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogError(
                $"Exception during networking of {__instance.playerUsername}({__instance.NetworkObjectId}): {ex}");
        }
    }

    /// <summary>
    ///     mark changed held slot.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),
        nameof(PlayerControllerB.SwitchToItemSlot))]
    private static void OnSlotChange(PlayerControllerB __instance, int slot)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.SlotChange.Value)
            return;

        if (!__instance.IsOwner)
            return;

        ((INetworkPlayerControllerB)__instance).AdditionalNetworking_SlotChanged = true;
    }


    /// <summary>
    ///     mark new inventory status on item grab.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),
        nameof(PlayerControllerB.GrabObjectClientRpc))]
    private static void OnItemGrabbed(PlayerControllerB __instance, bool grabValidated)
    {
        if (!__instance.IsRPCClientStage())
            return;

        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        if (!__instance.IsOwner)
            return;

        if (!grabValidated)
            return;

        ((INetworkPlayerControllerB)__instance).AdditionalNetworking_InventoryChanged = true;
    }

    /// <summary>
    ///     mark new inventory status on item discarded.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),
        nameof(PlayerControllerB.DiscardHeldObject))]
    private static void OnDiscardItem(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        if (!__instance.IsOwner)
            return;

        ((INetworkPlayerControllerB)__instance).AdditionalNetworking_InventoryChanged = true;
    }

    /// <summary>
    ///     mark new inventory status.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),
        nameof(PlayerControllerB.DropAllHeldItems))]
    private static void OnDropItem(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        if (!__instance.IsOwner)
            return;

        ((INetworkPlayerControllerB)__instance).AdditionalNetworking_InventoryChanged = true;
    }

    /// <summary>
    ///     broadcast username change.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB),
        nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    private static void OnPlayerConnected(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Misc.Username.Value)
            return;

        if (__instance.IsServer || !__instance.IsOwner)
            return;

        try
        {
            PlayerController.SyncUsernameServerRpc(__instance.NetworkObject, __instance.playerUsername);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogFatal(
                $"Exception during networking of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
        }
    }


    /// <summary>
    ///     broadcast changed data.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LateUpdate))]
    private static void OnLateUpdate(PlayerControllerB __instance)
    {
        if (((INetworkPlayerControllerB)__instance).AdditionalNetworking_SlotChanged)
        {
            ((INetworkPlayerControllerB)__instance).AdditionalNetworking_SlotChanged = false;

            if (__instance.IsOwner)
            {
                try
                {
                    PlayerController.SyncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
                }
                catch (Exception ex)
                {
                    AdditionalNetworking.Log.LogFatal(
                        $"Exception during networking of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
                }
            }
        }

        if (((INetworkPlayerControllerB)__instance).AdditionalNetworking_InventoryChanged)
        {
            ((INetworkPlayerControllerB)__instance).AdditionalNetworking_InventoryChanged = false;

            if (__instance.IsOwner)
            {
                using var pooledObject1 = ListPool<NetworkObjectReference>.Get(out var networkObjects);
                using var pooledObject2 = ListPool<int>.Get(out var slots);

                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];

                    if (slot == null || slot.NetworkObject == null)
                        continue;

                    if (!slot.NetworkObject.IsSpawned)
                    {
                        var itemTag = ItemCategory.GetKeyForItem(slot.itemProperties);
                        AdditionalNetworking.Log.LogFatal(
                            $"{itemTag}({slot.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
                        continue;
                    }

                    networkObjects.Add(slot.NetworkObject);
                    slots.Add(i);
                }

                try
                {
                    PlayerController.SyncInventoryServerRpc(
                        __instance.NetworkObject,
                        __instance.ItemOnlySlot ? __instance.ItemOnlySlot.NetworkObject : null,
                        networkObjects.ToArray(), slots.ToArray());
                }
                catch (Exception ex)
                {
                    AdditionalNetworking.Log.LogFatal(
                        $"Exception during networking of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
                }
            }
        }
    }
}