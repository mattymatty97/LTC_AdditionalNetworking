using System;
using AdditionalNetworking.Preloader;
using AdditionalNetworking.Utils;
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

        //host does not need to sync!
        if (__instance.IsServer)
            return;

        if (!__instance.itemProperties.isScrap)
            return;

        if (__instance.GetIsInitialized())
            return;

        if (__instance.GetHasRequestedSync())
            return;

        if (StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.GetValuablesSynced())
            return;

        if (RoundManager.Instance.GetSpawnedScrapPendingSync())
            return;

        if (!__instance.NetworkObject.IsSpawned)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"{itemTag}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        try
        {
            Networking.GrabbableObject.RequestSyncServerRpc(__instance.NetworkObject);
            __instance.SetHasRequestedSync(true);
        }
        catch (Exception ex)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.itemProperties);
            AdditionalNetworking.Log.LogError(
                $"Exception during networking of {itemTag}({__instance.NetworkObjectId}): {ex}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LoadItemSaveData))]
    private static void OnInitialize(GrabbableObject __instance)
    {
        __instance.SetIsInitialized(true);
    }
}