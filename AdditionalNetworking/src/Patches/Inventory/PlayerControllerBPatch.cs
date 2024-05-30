using System.Collections.Generic;
using AdditionalNetworking.Components;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.Inventory;

[HarmonyPatch]
internal class PlayerControllerBPatch
{

    internal static readonly Dictionary<PlayerControllerB, bool> DirtySlots = [];
    internal static readonly Dictionary<PlayerControllerB, bool> DirtyInventory = [];
        
    /// <summary>
    ///  Request the username.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.Start))]
    private static void OnStart(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Misc.Username.Value)
            return;
            
        if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
            return;
            
        if (!__instance.IsServer)
            PlayerNetworking.Instance.RequestSyncUsernameServerRpc(__instance.NetworkObject);
    }
        
    /// <summary>
    ///  mark changed held slot.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.SwitchToItemSlot))]
    private static void OnSlotChange(PlayerControllerB __instance, int slot)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.SlotChange.Value)
            return;
            
        DirtySlots[__instance] = true;
    }


        
    /// <summary>
    ///  broadcast new inventory status on item grab.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
    private static void OnItemGrabbed(PlayerControllerB __instance, bool grabValidated)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            return;
            
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;
            
        if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
            return;
            
        if (!grabValidated)
            return;

        DirtyInventory[__instance] = true;
    }        
        
    /// <summary>
    ///  broadcast new inventory status on item discarded.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DiscardHeldObject))]
    private static void OnDiscardItem(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;
            
        if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
            return;
            
        DirtyInventory[__instance] = true;
    }        
        
    /// <summary>
    ///  broadcast new inventory status.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DropAllHeldItems))]
    private static void OnDropItem(PlayerControllerB __instance)
    {            
        if (!AdditionalNetworking.PluginConfig.Inventory.InventoryChange.Value)
            return;
            
        if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
            return;
            
        DirtyInventory[__instance] = true;
    }
        
    /// <summary>
    ///  broadcast username change.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.ConnectClientToPlayerObject))]
    private static void OnPlayerConnected(PlayerControllerB __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Misc.Username.Value)
            return;
            
        if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
            return;
            
        if (!__instance.IsServer && __instance.IsOwner)
        {
            PlayerNetworking.Instance.SyncUsernameServerRpc(__instance.NetworkObject, __instance.playerUsername);
        }
    }
        
                
    /// <summary>
    ///  broadcast changed data.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.LateUpdate))]
    private static void OnLateUpdate(PlayerControllerB __instance)
    {
        if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
            return;
            
        if (DirtySlots.TryGetValue(__instance, out var value) && value)
        {
            DirtySlots[__instance] = false;
                
            if (__instance.IsOwner)
            {
                PlayerNetworking.Instance.SyncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
            }
        }
            
        if (DirtyInventory.TryGetValue(__instance, out var value2) && value2)
        {
            DirtyInventory[__instance] = false;
                
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null && slot.NetworkObject != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.SyncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
            }
        }
    }
        
    /// <summary>
    ///  clear entries on Destroy.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.OnDestroy))]
    private static void OnDestroy(PlayerControllerB __instance)
    {
        DirtyInventory.Remove(__instance);
        DirtySlots.Remove(__instance);
    }
        
}