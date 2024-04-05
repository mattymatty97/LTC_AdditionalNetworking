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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.Start))]
        private static void onStart(PlayerControllerB __instance)
        {
            var networkingComponent = __instance.gameObject.GetComponent<PlayerNetworking>();
            if (networkingComponent == null)
                AdditionalNetworking.Log.LogError($"{nameof(PlayerControllerB)}#{__instance.GetInstanceID()} did not find associated PlayerNetworking");
            else
                networkingTable.Add(__instance, networkingComponent);
        }
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.SwitchToItemSlot))]
        private static void onSlotChange(PlayerControllerB __instance, int slot)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
            {
                playerNetworking.syncSelectedSlotServerRpc(slot);
            }
        }
        
                
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
        private static void onSlotChange(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
            {
                var networkObjectArray =
                    __instance.ItemSlots.Select(g => (NetworkObjectReference)(g!=null?g.NetworkObject:null)).ToArray();
                playerNetworking.syncInventoryServerRpc(networkObjectArray);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        private static void onPlayerConnected(PlayerControllerB __instance)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var playerNetworking))
            {
                playerNetworking.syncUsernameServerRpc(__instance.playerUsername);
            }
        }
        
    }
}