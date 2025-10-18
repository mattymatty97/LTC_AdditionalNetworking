using System;
using AdditionalNetworking.Interfaces;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Utils;
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
        if (((INetworkPlayerControllerB)__instance).AdditionalNetworking_LastCrouchState == __instance.isCrouching)
            return;

        ((INetworkPlayerControllerB)__instance).AdditionalNetworking_LastCrouchState = __instance.isCrouching;

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectServerRpc))]
    private static void OnGrabRequest(PlayerControllerB __instance)
    {
        if (!__instance.IsRPCServerStage())
            return;

        var targetObject = __instance.currentlyGrabbingObject;
        if (!targetObject || targetObject is not ShotgunItem)
            return;

        Shotgun.RequestSyncServerRpc(__instance.currentlyGrabbingObject.NetworkObject);
    }
}