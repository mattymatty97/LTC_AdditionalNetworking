using System;
using AdditionalNetworking.Interfaces;
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


        if (((INetworkGrabbableObject)__instance).AdditionalNetworking_IsInitialized)
            return;

        if (((INetworkGrabbableObject)__instance).AdditionalNetworking_RequestedSync)
            return;

        if (StartOfRound.Instance.inShipPhase &&
            !((INetworkStartOfRound)StartOfRound.Instance).AdditionalNetworking_ValuablesSynced)
            return;

        if (((INetworkRoundManager)RoundManager.Instance).AdditionalNetworking_ScrapPendingSync)
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
            ((INetworkGrabbableObject)__instance).AdditionalNetworking_RequestedSync = true;
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
        ((INetworkGrabbableObject)__instance).AdditionalNetworking_IsInitialized = true;
    }
}