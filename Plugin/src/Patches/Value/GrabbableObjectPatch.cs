using System;
using HarmonyLib;

namespace AdditionalNetworking.Patches.Value;

[HarmonyPatch]
internal class GrabbableObjectPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
    private static void CheckScrapHasValue(GrabbableObject __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Value.Enabled.Value)
            return;

        if (!__instance.itemProperties.isScrap)
            return;

        if (__instance.AdditionalNetworking_isInitialized)
            return;

        if (__instance.AdditionalNetworking_hasRequestedSync)
            return;

        if (StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.AdditionalNetworking_unlockablesSynced)
            return;

        if (RoundManager.Instance.AdditionalNetworking_spawnedScrapPendingSync)
            return;

        if (!__instance.NetworkObject.IsSpawned)
        {
            AdditionalNetworking.Log.LogFatal(
                $"{__instance.itemProperties.itemName}({__instance.NetworkObjectId}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        try
        {
            Networking.GrabbableObject.RequestSyncServerRpc(__instance.NetworkObject);
            __instance.AdditionalNetworking_hasRequestedSync = true;
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogFatal(
                $"Exception syncing value of {__instance.itemProperties.itemName}({__instance.NetworkObjectId}):\n{ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LoadItemSaveData))]
    private static void OnInitialize(GrabbableObject __instance)
    {
        __instance.AdditionalNetworking_isInitialized = true;
    }
}