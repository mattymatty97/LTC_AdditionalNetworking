using System;
using System.Collections.Generic;
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

        __instance.AdditionalNetworking_dirtySlots = true;
    }


    /// <summary>
    ///     mark new inventory status on item grab.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),
        nameof(PlayerControllerB.GrabObjectClientRpc))]
    private static void OnItemGrabbed(PlayerControllerB __instance, bool grabValidated)
    {
        var networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client ||
            (!networkManager.IsClient && !networkManager.IsHost))
            return;

        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        if (!__instance.IsOwner)
            return;

        if (!grabValidated)
            return;

        __instance.AdditionalNetworking_dirtyInventory = true;
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

        __instance.AdditionalNetworking_dirtyInventory = true;
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

        __instance.AdditionalNetworking_dirtyInventory = true;
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
        if (__instance.AdditionalNetworking_dirtySlots)
        {
            __instance.AdditionalNetworking_dirtySlots = false;

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

        if (__instance.AdditionalNetworking_dirtyInventory)
        {
            __instance.AdditionalNetworking_dirtyInventory = false;

            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects;
                List<int> slots;

                using var pooledObject1 = ListPool<NetworkObjectReference>.Get(out networkObjects);
                using var pooledObject2 = ListPool<int>.Get(out slots);

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
                    PlayerController.SyncInventoryServerRpc(__instance.NetworkObject, networkObjects.ToArray(),
                        slots.ToArray());
                }
                catch (Exception ex)
                {
                    AdditionalNetworking.Log.LogFatal(
                        $"Exception during networking of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
                }
            }
        }

        if (__instance.AdditionalNetworking_lastCrouchState != __instance.isCrouching)
        {
            __instance.AdditionalNetworking_lastCrouchState = __instance.isCrouching;

            if (AdditionalNetworking.PluginConfig.PlayerState.Crouching.Value && __instance.IsOwner)
            {
                try
                {
                    PlayerController.SyncCrouchServerRpc(__instance.NetworkObject, __instance.isCrouching);
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