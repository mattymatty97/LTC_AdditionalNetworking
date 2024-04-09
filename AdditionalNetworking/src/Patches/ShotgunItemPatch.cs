using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class ShotgunItemPatch
    {
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
            
            ShotgunNetworking.Instance.SyncAmmoServerRpc(__instance.NetworkObject, __instance.shellsLoaded);
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

            ShotgunNetworking.Instance.SyncAmmoServerRpc(__instance.NetworkObject, __instance.shellsLoaded);

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
            
            ShotgunNetworking.Instance.SyncSafetyServerRpc(__instance.NetworkObject, __instance.safetyOn);
        }
        
    }
}