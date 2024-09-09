using System;
using AdditionalNetworking.Networking;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch]
internal class BoomboxItemPatch
{
    /// <summary>
    ///     Sync on Creation
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BoomboxItem), nameof(BoomboxItem.Start))]
    private static void OnStart(BoomboxItem __instance)
    {
        if (!AdditionalNetworking.PluginConfig.State.Boombox.Value)
            return;

        if (!StartOfRound.Instance.IsServer) Boombox.RequestSyncServerRpc(__instance.NetworkObject);
    }

    /// <summary>
    ///     broadcast the new ammo count after a reload animation.
    /// </summary>
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


    /// <summary>
    ///     broadcast changed data.
    /// </summary>
    /// >
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    private static void OnLateUpdate(GrabbableObject __instance)
    {
        var boomboxItem = __instance as BoomboxItem;
        if (boomboxItem == null)
            return;

        if (boomboxItem.AdditionalNetworking_dirtyStatus)
        {
            boomboxItem.AdditionalNetworking_dirtyStatus = false;

            if (__instance.IsOwner)
            {
                var track = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
                var state = boomboxItem.isPlayingMusic;
                Boombox.SyncStateServerRpc(__instance.NetworkObject, state, track);
            }
        }
    }
}