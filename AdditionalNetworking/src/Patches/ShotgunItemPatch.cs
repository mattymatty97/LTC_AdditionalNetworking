using System.Collections.Generic;
using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class ShotgunItemPatch
    {
        
        internal static readonly Dictionary<ShotgunItem, bool> DirtyAmmo = [];
        internal static readonly Dictionary<ShotgunItem, bool> DirtySafety = [];
        
        /// <summary>
        ///  Sync on Creation
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.Start))]
        private static void OnStart(ShotgunItem __instance)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsServer)
            {
                ShotgunNetworking.Instance.RequestSyncServerRpc(__instance.NetworkObject);
            }
        }
        
        /// <summary>
        ///  broadcast the new ammo count after a reload animation.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ReloadGunEffectsServerRpc))]
        private static void OnAmmoReload(ShotgunItem __instance, bool start)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;

            if (start || !__instance.IsOwner)
                return;
            
            DirtyAmmo[__instance] = true;
        }        
        
        /// <summary>
        ///  broadcast the new ammo count after a shot.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ShootGun))]
        private static void OnShot(ShotgunItem __instance)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;

            if (!__instance.IsOwner)
                return;

            DirtyAmmo[__instance] = true;
        }
        
        /// <summary>
        ///  broadcast the new safety value.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ItemInteractLeftRight))]
        private static void OnSafetyToggle(ShotgunItem __instance, bool right)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsOwner)
                return;

            DirtySafety[__instance] = true;
        }
        
                
                
        /// <summary>
        ///  broadcast changed data.
        /// </summary>>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.LateUpdate))]
        private static void OnLateUpdate(ShotgunItem __instance)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (DirtyAmmo.TryGetValue(__instance, out var value) && value)
            {
                DirtyAmmo[__instance] = false;
                
                if (__instance.IsOwner)
                {
                    ShotgunNetworking.Instance.SyncAmmoServerRpc(__instance.NetworkObject, __instance.shellsLoaded);
                }
            }
            
            if (DirtySafety.TryGetValue(__instance, out var value2) && value2)
            {
                DirtySafety[__instance] = false;
                
                if (__instance.IsOwner)
                {
                    ShotgunNetworking.Instance.SyncSafetyServerRpc(__instance.NetworkObject,__instance.safetyOn);
                }
            }
        }
        
        /// <summary>
        ///  clear entries on Destroy.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.OnDestroy))]
        private static void OnDestroy(ShotgunItem __instance)
        {
            DirtySafety.Remove(__instance);
            DirtyAmmo.Remove(__instance);
        }
        
    }
}