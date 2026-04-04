using AdditionalNetworking.Networking;
using AdditionalNetworking.Utils;
using GameNetcodeStuff;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch]
internal class PlayerControllerBPatch
{
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