using System;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Preloader;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectServerRpc))]
    private static void OnGrabRequest(PlayerControllerB __instance)
    {
        var networkManager = __instance.NetworkManager;
        if ((object)networkManager == null || !networkManager.IsListening)
            return;

        if (__instance.__rpc_exec_stage == NetworkBehaviour.__RpcExecStage.Server ||
            (!networkManager.IsClient && !networkManager.IsHost) ||
            __instance.OwnerClientId == networkManager.LocalClientId)
            return;

        var targetObject = __instance.currentlyGrabbingObject;
        if (!targetObject || targetObject is not ShotgunItem)
            return;

        Shotgun.RequestSyncServerRpc(__instance.currentlyGrabbingObject.NetworkObject);
    }
}