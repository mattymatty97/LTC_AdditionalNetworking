using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AdditionalNetworking.Components;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        private static readonly ConditionalWeakTable<PlayerControllerB, PlayerNetworking> networkingTable =
            new ConditionalWeakTable<PlayerControllerB, PlayerNetworking>();
        
        /// <summary>
        ///  Grab the associated NetworkingComponent.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.Start))]
        private static void onStart(PlayerControllerB __instance)
        {
            var networkingComponent = __instance.gameObject.GetComponent<PlayerNetworking>();
            if (networkingComponent == null)
                AdditionalNetworking.Log.LogError($"{nameof(PlayerControllerB)}#{__instance.GetInstanceID()} did not find associated PlayerNetworking");
            else
                networkingTable.Add(__instance, networkingComponent);
        }
        
        /// <summary>
        ///  broadcast changed held slot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.SwitchToItemSlot))]
        private static void onSlotChange(PlayerControllerB __instance, int slot)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
            {
                playerNetworking.syncSelectedSlotServerRpc(slot);
            }
        }
        
        /// <summary>
        ///  broadcast new inventory status on item grab.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
        private static void onItemGrabbed(PlayerControllerB __instance, bool grabValidated)
        {
            if (!grabValidated)
                return;
            
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
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
                playerNetworking.syncInventoryServerRpc(networkObjects.ToArray(),slots.ToArray());
            }
        }        
        
        /// <summary>
        ///  broadcast new inventory status on item discarded.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DiscardHeldObject))]
        private static void onDiscardItem(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
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
                playerNetworking.syncInventoryServerRpc(networkObjects.ToArray(),slots.ToArray());
            }
        }        
        
        /// <summary>
        ///  broadcast new inventory status.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DropAllHeldItems))]
        private static void onDropItem(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
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
                playerNetworking.syncInventoryServerRpc(networkObjects.ToArray(),slots.ToArray());
            }
        }
        
        /// <summary>
        ///  broadcast username change.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        private static void onPlayerConnected(PlayerControllerB __instance)
        {
            if (!__instance.IsServer && __instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
            {
                playerNetworking.syncUsernameServerRpc(__instance.playerUsername);
            }
        }
        
    }
}