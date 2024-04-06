using System.Collections.Generic;
using AdditionalNetworking.Components;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        /// <summary>
        ///  Request the username fr.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.Start))]
        private static void onStart(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null)
                return;
            
            if (!__instance.IsServer)
                PlayerNetworking.Instance.requestSyncUsernameServerRpc(__instance.NetworkObject);
        }
        
        /// <summary>
        ///  broadcast changed held slot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.SwitchToItemSlot))]
        private static void onSlotChange(PlayerControllerB __instance, int slot)
        {
            if (PlayerNetworking.Instance == null)
                return;
            
            if (__instance.IsOwner)
            {
                PlayerNetworking.Instance.syncSelectedSlotServerRpc(__instance.NetworkObject, slot);
            }
        }
        
        /// <summary>
        ///  broadcast new inventory status on item grab.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
        private static void onItemGrabbed(PlayerControllerB __instance, bool grabValidated)
        {
            if (PlayerNetworking.Instance == null)
                return;
            
            if (!grabValidated)
                return;
            
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.syncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
                PlayerNetworking.Instance.syncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
            }
        }        
        
        /// <summary>
        ///  broadcast new inventory status on item discarded.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DiscardHeldObject))]
        private static void onDiscardItem(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null)
                return;
            
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.syncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
                PlayerNetworking.Instance.syncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
            }
        }        
        
        /// <summary>
        ///  broadcast new inventory status.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DropAllHeldItems))]
        private static void onDropItem(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null)
                return;
            
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.syncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
                PlayerNetworking.Instance.syncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
            }
        }
        
        /// <summary>
        ///  broadcast username change.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        private static void onPlayerConnected(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null)
                return;
            
            if (!__instance.IsServer && __instance.IsOwner)
            {
                PlayerNetworking.Instance.syncUsernameServerRpc(__instance.NetworkObject, __instance.playerUsername);
                PlayerNetworking.Instance.syncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
            }
        }
        
    }
}