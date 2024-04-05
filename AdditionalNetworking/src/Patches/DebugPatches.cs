using System;
using GameNetcodeStuff;
using HarmonyLib;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Awake))]
        private static void betterException(Exception __exception)
        {
            if (__exception != null)
            {
                AdditionalNetworking.Log.LogError($"Exception: {__exception}");
            }
        }
    }
}