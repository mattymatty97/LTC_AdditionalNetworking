using System;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Preloader;
using AdditionalNetworking.Utils;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch]
internal class ShotgunItemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.Start))]
    private static void OnStart(ShotgunItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Shotgun.Value)
            return;

        if (StartOfRound.Instance.IsServer)
            return;

        if (!__instance.NetworkObject.IsSpawned)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"{itemTag}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        try
        {
            Shotgun.RequestSyncServerRpc(__instance.NetworkObject);
        }
        catch (Exception ex)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogError(
                $"Exception during networking of {itemTag}({__instance.NetworkObjectId}): {ex}");
        }
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsServerRpc))]
    private static void OnAmmoReload(ShotgunItem __instance, bool start)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Shotgun.Value)
            return;

        if (start || !__instance.IsOwner)
            return;

        __instance.SetDirtyAmmo(true);
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
    private static void OnShot(ShotgunItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Shotgun.Value)
            return;

        if (!__instance.IsOwner)
            return;

        __instance.SetDirtyAmmo(true);
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ItemInteractLeftRight))]
    private static void OnSafetyToggle(ShotgunItem __instance, bool right)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Shotgun.Value)
            return;

        if (!__instance.IsOwner)
            return;

        __instance.SetDirtySafety(true);
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    private static void OnLateUpdate(GrabbableObject __instance)
    {
        var shotgunItem = __instance as ShotgunItem;
        if (shotgunItem == null)
            return;

        if (!__instance.NetworkObject.IsSpawned)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"{itemTag}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        if (shotgunItem.GetDirtyAmmo())
        {
            shotgunItem.SetDirtyAmmo(false);

            if (__instance.IsOwner)
            {
                try
                {
                    Shotgun.SyncAmmoServerRpc(__instance.NetworkObject, shotgunItem.shellsLoaded);
                }
                catch (Exception ex)
                {
                    var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
                    AdditionalNetworking.Log.LogError(
                        $"Exception during networking of {itemTag}({__instance.NetworkObjectId}): {ex}");
                }
            }
        }

        if (shotgunItem.GetDirtySafety())
        {
            shotgunItem.SetDirtySafety(false);

            if (__instance.IsOwner)
            {
                try
                {
                    Shotgun.SyncSafetyServerRpc(__instance.NetworkObject, shotgunItem.safetyOn);
                }
                catch (Exception ex)
                {
                    var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
                    AdditionalNetworking.Log.LogError(
                        $"Exception during networking of {itemTag}({__instance.NetworkObjectId}): {ex}");
                }
            }
        }
    }
}