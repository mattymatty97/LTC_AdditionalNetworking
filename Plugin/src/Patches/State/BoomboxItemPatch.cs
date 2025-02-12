using System;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Utils;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch]
internal class BoomboxItemPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BoomboxItem), nameof(BoomboxItem.Start))]
    private static void OnStart(BoomboxItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.State.Boombox.Value)
            return;

        if (!StartOfRound.Instance.IsServer) Boombox.RequestSyncServerRpc(__instance.NetworkObject);
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(BoomboxItem), nameof(BoomboxItem.StartMusic))]
    private static void OnMusicChange(BoomboxItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.State.Boombox.Value)
            return;

        if (!__instance.IsOwner)
            return;

        __instance.AdditionalNetworking_dirtyStatus = true;
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    private static void OnLateUpdate(GrabbableObject __instance)
    {
        var boomboxItem = __instance as BoomboxItem;
        if (boomboxItem == null)
            return;


        if (!boomboxItem.AdditionalNetworking_dirtyStatus)
            return;

        boomboxItem.AdditionalNetworking_dirtyStatus = false;

        if (!__instance.NetworkObject.IsSpawned)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"{itemTag}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        if (!__instance.IsOwner)
            return;

        var track = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
        var state = boomboxItem.isPlayingMusic;

        try
        {
            Boombox.SyncStateServerRpc(__instance.NetworkObject, state, track);
        }
        catch (Exception ex)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"Exception syncing boombox state of {itemTag}({__instance.NetworkObjectId}):\n{ex}");
        }
    }
}