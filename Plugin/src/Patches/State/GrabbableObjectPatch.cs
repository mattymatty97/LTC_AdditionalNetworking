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
            
            if (__instance.AdditionalNetworking_isInitialized)
                return;            
            
            if (__instance.AdditionalNetworking_hasRequestedSync)
                return;
            
            if (StartOfRound.Instance.inShipPhase)
                return;
            
            if (!StartOfRound.Instance.shipHasLanded && !StartOfRound.Instance.shipIsLeaving)
                return;
            
            if (!StartOfRound.Instance.localPlayerController)
                return;

            if (!__instance.itemProperties.isScrap || __instance.scrapValue != 0) 
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