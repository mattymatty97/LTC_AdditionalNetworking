using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.State
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
            if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
                return;
            
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!StartOfRound.Instance.IsServer)
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
            if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
                return;
            
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;

            if (start || !__instance.IsOwner)
                return;
            
            __instance.AdditionalNetworking_dirtyAmmo = true;
        }        
        
        /// <summary>
        ///  broadcast the new ammo count after a shot.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ShootGun))]
        private static void OnShot(ShotgunItem __instance)
        {
            if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
                return;
            
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;

            if (!__instance.IsOwner)
                return;

            __instance.AdditionalNetworking_dirtyAmmo = true;
        }
        
        /// <summary>
        ///  broadcast the new safety value.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(ShotgunItem),nameof(ShotgunItem.ItemInteractLeftRight))]
        private static void OnSafetyToggle(ShotgunItem __instance, bool right)
        {
            if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
                return;
            
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsOwner)
                return;

            __instance.AdditionalNetworking_dirtySafety = true;
        }
        
                
                
        /// <summary>
        ///  broadcast changed data.
        /// </summary>>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GrabbableObject),nameof(GrabbableObject.LateUpdate))]
        private static void OnLateUpdate(GrabbableObject __instance)
        {
            var shotgunItem = __instance as ShotgunItem;
            if ( shotgunItem == null)
                return;
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (shotgunItem.AdditionalNetworking_dirtyAmmo)
            {
                shotgunItem.AdditionalNetworking_dirtyAmmo = false;
                
                if (__instance.IsOwner)
                {
                    ShotgunNetworking.Instance.SyncAmmoServerRpc(__instance.NetworkObject, shotgunItem.shellsLoaded);
                }
            }
            
            if (shotgunItem.AdditionalNetworking_dirtySafety)
            {
                shotgunItem.AdditionalNetworking_dirtySafety = false;
                
                if (__instance.IsOwner)
                {
                    ShotgunNetworking.Instance.SyncSafetyServerRpc(__instance.NetworkObject,shotgunItem.safetyOn);
                }
            }
        }
    }
}