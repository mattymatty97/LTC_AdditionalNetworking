using System.Collections.Generic;
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
    ///     broadcast new inventory status on item grab.
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
    ///     broadcast new inventory status on item discarded.
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
    ///     broadcast new inventory status.
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
            PlayerControllerB.SyncUsername(__instance.NetworkObject, __instance.playerUsername);
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
                PlayerControllerB.SyncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
        }

        if (DirtyInventory.TryGetValue(__instance, out var value2) && value2)
        {
            DirtyInventory[__instance] = false;

            if (__instance.IsOwner)
            {
                var networkObjects = new List<NetworkObjectReference>();
                var slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null && slot.NetworkObject != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }

                PlayerControllerB.SyncInventoryServerRpc(__instance.NetworkObject, networkObjects.ToArray(),
                    slots.ToArray());
            }
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