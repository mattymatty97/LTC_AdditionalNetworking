using AdditionalNetworking.Interfaces;
using AdditionalNetworking.Utils;
using HarmonyLib;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class RoundManagerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
    private static void OnNewLevel(RoundManager __instance)
    {
        if (!__instance.IsRPCClientStage())
            return;


        ((INetworkRoundManager)__instance).AdditionalNetworking_ScrapPendingSync = true;
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
    private static void AfterScrapValueSync(RoundManager __instance)
    {
        if (__instance.IsRPCClientStage())
            return;

        ((INetworkRoundManager)__instance).AdditionalNetworking_ScrapPendingSync = false;
    }
}