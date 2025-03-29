using System;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Preloader;
using GameNetcodeStuff;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch]
internal class PlayerControllerBPatch
{
    /// <summary>
    ///     broadcast changed data.
    /// </summary>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LateUpdate))]
    private static void OnLateUpdate(PlayerControllerB __instance)
    {
        if (__instance.GetLastCrouchState() == __instance.isCrouching)
            return;

        __instance.SetLastCrouchState(__instance.isCrouching);

        if (AdditionalNetworking.PluginConfig.PlayerState.Crouching.Value && __instance.IsOwner)
        {
            try
            {
                PlayerController.SyncCrouchServerRpc(__instance.NetworkObject, __instance.isCrouching);
            }
            catch (Exception ex)
            {
                AdditionalNetworking.Log.LogFatal(
                    $"Exception during networking of {__instance.playerUsername}({__instance.NetworkObjectId}):\n{ex}");
            }
        }
    }
}