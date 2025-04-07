using System;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Preloader;
using AdditionalNetworking.Utils;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch(typeof(AnimatedItem))]
internal static class AnimatedItemPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(AnimatedItem.Start))]
    private static void OnStart(AnimatedItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Animated.Value)
            return;

        //host does not need to sync!
        if (__instance.IsServer)
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
            AnimatedObject.RequestSyncServerRpc(__instance.NetworkObject);
        }
        catch (Exception ex)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogError(
                $"Exception during networking of {itemTag}({__instance.NetworkObjectId}): {ex}");
        }
    }


    [HarmonyFinalizer]
    [HarmonyPatch(nameof(AnimatedItem.EquipItem))]
    private static void OnEquipItem(AnimatedItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Animated.Value)
            return;

        if (!__instance.IsOwner)
            return;

        __instance.SetDirtyStatus(true);
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    private static void OnLateUpdate(GrabbableObject __instance)
    {
        if (!AdditionalNetworking.PluginConfig.ItemState.Animated.Value)
            return;

        var animatedItem = __instance as AnimatedItem;
        if (animatedItem == null)
            return;

        if (!animatedItem.GetDirtyStatus())
            return;

        animatedItem.SetDirtyStatus(false);

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
            var itemAudio = animatedItem.itemAudio;
            AnimatedObject.SyncStateServerRpc(__instance.NetworkObject, itemAudio.isPlaying);
        }
        catch (Exception ex)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"Exception syncing animator state of {itemTag}({__instance.NetworkObjectId}):\n{ex}");
        }
    }
}