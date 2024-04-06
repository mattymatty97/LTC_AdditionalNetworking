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
        
        /// <summary>
        ///  Grab the associated NetworkingComponent.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPriority(Priority.VeryLow)]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.Start))]
        private static void onStart(ShotgunItem __instance)
        {
            var networkingComponent = __instance.gameObject.GetComponent<ShotgunNetworking>();
            if (networkingComponent == null)
                AdditionalNetworking.Log.LogError($"{nameof(ShotgunItem)}#{__instance.GetInstanceID()} did not find associated PlayerNetworking");
            else
                networkingTable.Add(__instance, networkingComponent);
        }
        
        /// <summary>
        ///  broadcast the new ammo count after a reload animation.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ReloadGunEffectsServerRpc))]
        private static void onAmmoReload(ShotgunItem __instance, bool start)
        {
            if (start || !__instance.IsOwner)
                return;
            
            if (networkingTable.TryGetValue(__instance, out var shotgunNetworking))
            {
                shotgunNetworking.syncAmmoServerRpc(__instance.shellsLoaded);
            }
        }        
        
        /// <summary>
        ///  broadcast the new ammo count after a shot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ShootGun))]
        private static void onShot(ShotgunItem __instance)
        {
            //if the gun is dropped allow the server to broadcast
            if (!__instance.IsOwner)
                return;
            
            if (networkingTable.TryGetValue(__instance, out var shotgunNetworking))
            {
                shotgunNetworking.syncAmmoServerRpc(__instance.shellsLoaded);
            }
        }
        
        /// <summary>
        ///  broadcast the new safety value.
        /// </summary>
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