using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.Transform;

[HarmonyPatch]
internal class GrabbablePositionPatch
{
    [HarmonyPatch]
    internal class GrabbablePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), "Awake")]
        private static void OnAwake(GrabbableObject __instance)
        {
            if (!AdditionalNetworking.PluginConfig.Transforms.Grabbables.Value)
                return;

            if (__instance.TryGetComponent<NetworkObject>(out _))
            {
                if (!__instance.TryGetComponent<GrabbableNetworkTransform>(out _))
                {
                    __instance.gameObject.AddComponent<GrabbableNetworkTransform>();
                }
            }
        }

    }
    
}