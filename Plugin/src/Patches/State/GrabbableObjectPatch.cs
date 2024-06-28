using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State
{
    [HarmonyPatch]
    internal class GrabbableObjectPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
        private static void CheckScrapHasValue(GrabbableObject __instance)
        {
            if (!GrabbableNetworking.Instance.Enabled)
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
            
            GrabbableNetworking.Instance.RequestValuesServerRpc(__instance.NetworkObject);
            __instance.AdditionalNetworking_hasRequestedSync = true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LoadItemSaveData))]
        private static void OnInitialize(GrabbableObject __instance)
        {
            __instance.AdditionalNetworking_isInitialized = true;
        }        
        
    }
}