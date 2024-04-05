using System.Runtime.CompilerServices;
using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class ShotgunItemPatch
    {
        private static readonly ConditionalWeakTable<ShotgunItem, ShotgunNetworking> networkingTable =
            new ConditionalWeakTable<ShotgunItem, ShotgunNetworking>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.Start))]
        private static void onStart(ShotgunItem __instance)
        {
            var networkingComponent = __instance.gameObject.GetComponent<ShotgunNetworking>();
            if (networkingComponent == null)
                AdditionalNetworking.Log.LogError($"{nameof(ShotgunItem)}#{__instance.GetInstanceID()} did not find associated PlayerNetworking");
            else
                networkingTable.Add(__instance, networkingComponent);
        }
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.reloadGunAnimation))]
        private static void onAmmoReload(ShotgunItem __instance)
        {
            if (__instance.IsOwner && networkingTable.TryGetValue(__instance, out var shotgunNetworking))
            {
                shotgunNetworking.syncAmmoServerRpc(__instance.shellsLoaded);
            }
        }   
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ItemInteractLeftRight))]
        private static void onSafetyToggle(ShotgunItem __instance, bool right)
        {
            if (__instance.IsOwner && !right && networkingTable.TryGetValue(__instance, out var shotgunNetworking))
            {
                shotgunNetworking.syncSafetyServerRpc(__instance.safetyOn);
            }
        }
        
    }
}