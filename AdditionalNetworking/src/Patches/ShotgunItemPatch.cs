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
        private static void onStart(ShotgunItem __instance)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsServer)
            {
                ShotgunNetworking.Instance.requestSyncServerRpc(__instance.NetworkObject);
            }
        }
        
        /// <summary>
        ///  broadcast the new ammo count after a reload animation.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ReloadGunEffectsServerRpc))]
        private static void onAmmoReload(ShotgunItem __instance, bool start)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;

            if (start || !__instance.IsOwner)
                return;
            
            ShotgunNetworking.Instance.syncAmmoServerRpc(__instance.NetworkObject, __instance.shellsLoaded);
        }        
        
        /// <summary>
        ///  broadcast the new ammo count after a shot.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ShootGun))]
        private static void onShot(ShotgunItem __instance)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;

            if (!__instance.IsOwner)
                return;

            ShotgunNetworking.Instance.syncAmmoServerRpc(__instance.NetworkObject, __instance.shellsLoaded);

        }
        
        /// <summary>
        ///  broadcast the new safety value.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ItemInteractLeftRight))]
        private static void onSafetyToggle(ShotgunItem __instance, bool right)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsOwner)
                return;
            
            ShotgunNetworking.Instance.syncSafetyServerRpc(__instance.NetworkObject, __instance.safetyOn);
        }
        
    }
}