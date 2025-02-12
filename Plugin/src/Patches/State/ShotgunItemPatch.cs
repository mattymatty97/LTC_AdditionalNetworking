using System;
using AdditionalNetworking.Networking;
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
        if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
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
            AdditionalNetworking.Log.LogFatal(
                $"Exception syncing status of {itemTag}({__instance.NetworkObjectId}):\n{ex}");
        }
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsServerRpc))]
    private static void OnAmmoReload(ShotgunItem __instance, bool start)
    {
        if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
            return;

        if (start || !__instance.IsOwner)
            return;

        __instance.AdditionalNetworking_dirtyAmmo = true;
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ShootGun))]
    private static void OnShot(ShotgunItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
            return;

        if (!__instance.IsOwner)
            return;

        __instance.AdditionalNetworking_dirtyAmmo = true;
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ItemInteractLeftRight))]
    private static void OnSafetyToggle(ShotgunItem __instance, bool right)
    {
        if (!AdditionalNetworking.PluginConfig.State.Shotgun.Value)
            return;

        if (!__instance.IsOwner)
            return;

        __instance.AdditionalNetworking_dirtySafety = true;
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    private static void OnLateUpdate(GrabbableObject __instance)
    {
        var shotgunItem = __instance as ShotgunItem;
        if (shotgunItem == null)
            return;

        if (shotgunItem.AdditionalNetworking_dirtyAmmo)
        {
            shotgunItem.AdditionalNetworking_dirtyAmmo = false;

            if (!__instance.NetworkObject.IsSpawned)
            {
                var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
                AdditionalNetworking.Log.LogFatal(
                    $"{itemTag}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
                return;
            }

            if (!__instance.IsOwner)
                return;
            try
            {
                Shotgun.SyncAmmoServerRpc(__instance.NetworkObject, shotgunItem.shellsLoaded);
            }
            catch (Exception ex)
            {
                var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
                AdditionalNetworking.Log.LogFatal(
                    $"Exception syncing ammo of {itemTag}({__instance.NetworkObjectId}):\n{ex}");
            }
        }

        if (shotgunItem.AdditionalNetworking_dirtySafety)
        {
            shotgunItem.AdditionalNetworking_dirtySafety = false;

            if (!__instance.NetworkObject.IsSpawned)
            {
                var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
                AdditionalNetworking.Log.LogFatal(
                    $"{itemTag}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
                return;
            }

            if (!__instance.IsOwner)
                return;
            try
            {
                Shotgun.SyncSafetyServerRpc(__instance.NetworkObject, shotgunItem.safetyOn);
            }
            catch (Exception ex)
            {
                var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
                AdditionalNetworking.Log.LogFatal(
                    $"Exception syncing safety of {itemTag}({__instance.NetworkObjectId}):\n{ex}");
            }
        }
    }
}