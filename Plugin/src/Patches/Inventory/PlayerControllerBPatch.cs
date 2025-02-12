using System;
using System.Collections.Generic;
using AdditionalNetworking.Utils;
using HarmonyLib;
using Unity.Netcode;
using PlayerControllerB = AdditionalNetworking.Networking.PlayerControllerB;

namespace AdditionalNetworking.Patches.Inventory;

[HarmonyPatch]
internal class PlayerControllerBPatch
{
    internal static readonly Dictionary<GameNetcodeStuff.PlayerControllerB, bool> DirtySlots = [];
    internal static readonly Dictionary<GameNetcodeStuff.PlayerControllerB, bool> DirtyInventory = [];

    /// <summary>
    ///     Request the username.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), nameof(GameNetcodeStuff.PlayerControllerB.Start))]
    private static void OnStart(GameNetcodeStuff.PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Misc.Username.Value)
            return;

        if (!__instance.IsServer)
            PlayerControllerB.RequestSyncUsernameServerRpc(__instance.NetworkObject);
    }

    /// <summary>
    ///     mark changed held slot.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB),
        nameof(GameNetcodeStuff.PlayerControllerB.SwitchToItemSlot))]
    private static void OnSlotChange(GameNetcodeStuff.PlayerControllerB __instance, int slot)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.SlotChange.Value)
            return;

        DirtySlots[__instance] = true;
    }


    /// <summary>
    ///     mark new inventory status on item grab.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB),
        nameof(GameNetcodeStuff.PlayerControllerB.GrabObjectClientRpc))]
    private static void OnItemGrabbed(GameNetcodeStuff.PlayerControllerB __instance, bool grabValidated)
    {
        var networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client ||
            (!networkManager.IsClient && !networkManager.IsHost))
            return;

        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        if (!grabValidated)
            return;

        DirtyInventory[__instance] = true;
    }

    /// <summary>
    ///     mark new inventory status on item discarded.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB),
        nameof(GameNetcodeStuff.PlayerControllerB.DiscardHeldObject))]
    private static void OnDiscardItem(GameNetcodeStuff.PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        DirtyInventory[__instance] = true;
    }

    /// <summary>
    ///     mark new inventory status.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB),
        nameof(GameNetcodeStuff.PlayerControllerB.DropAllHeldItems))]
    private static void OnDropItem(GameNetcodeStuff.PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;

        DirtyInventory[__instance] = true;
    }

    /// <summary>
    ///     broadcast username change.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB),
        nameof(GameNetcodeStuff.PlayerControllerB.ConnectClientToPlayerObject))]
    private static void OnPlayerConnected(GameNetcodeStuff.PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Misc.Username.Value)
            return;

        if (!__instance.IsServer && __instance.IsOwner)
            try
            {
                PlayerControllerB.SyncUsernameServerRpc(__instance.NetworkObject, __instance.playerUsername);
            }
            catch (Exception ex)
            {
                AdditionalNetworking.Log.LogFatal(
                    $"Exception syncing name of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
            }
    }


    /// <summary>
    ///     broadcast changed data.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), nameof(GameNetcodeStuff.PlayerControllerB.LateUpdate))]
    private static void OnLateUpdate(GameNetcodeStuff.PlayerControllerB __instance)
    {
        if (DirtySlots.TryGetValue(__instance, out var value) && value)
        {
            DirtySlots[__instance] = false;

            if (__instance.IsOwner)
                try
                {
                    PlayerControllerB.SyncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
                }
                catch (Exception ex)
                {
                    AdditionalNetworking.Log.LogFatal(
                        $"Exception syncing slots of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
                }
        }

        if (!DirtyInventory.TryGetValue(__instance, out var value2) || !value2)
            return;

        DirtyInventory[__instance] = false;

        if (!__instance.IsOwner)
            return;

        var networkObjects = new List<NetworkObjectReference>();
        var slots = new List<int>();
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
            PlayerControllerB.SyncInventoryServerRpc(__instance.NetworkObject, networkObjects.ToArray(),
                slots.ToArray());
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogFatal(
                $"Exception syncing inventory of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
        }
    }

    /// <summary>
    ///     clear entries on Destroy.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetcodeStuff.PlayerControllerB), nameof(GameNetcodeStuff.PlayerControllerB.OnDestroy))]
    private static void OnDestroy(GameNetcodeStuff.PlayerControllerB __instance)
    {
        DirtyInventory.Remove(__instance);
        DirtySlots.Remove(__instance);
    }
}