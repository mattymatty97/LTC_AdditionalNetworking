using System.Collections.Generic;
using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches.State
{
    [HarmonyPatch]
    internal class GrabbableObjectPatch
    {
        internal static readonly HashSet<GrabbableObject> RequestedValues = new();
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.LateUpdate))]
        private static void CheckScrapHasValue(GrabbableObject __instance)
        {
            if (!GrabbableNetworking.Instance.Enabled)
                return;

            if (RequestedValues.Contains(__instance))
                return;

            if (!__instance.itemProperties.isScrap || __instance.scrapValue != 0) 
                return;
            
            GrabbableNetworking.Instance.RequestValuesServerRpc(__instance.NetworkObject);
            RequestedValues.Add(__instance);
        }
        
    }
}