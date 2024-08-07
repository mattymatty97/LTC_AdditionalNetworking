using HarmonyLib;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
    private static void AfterUnlockablesSync(StartOfRound __instance)
    {
        __instance.AdditionalNetworking_unlockablesSynced = true;
    }
}