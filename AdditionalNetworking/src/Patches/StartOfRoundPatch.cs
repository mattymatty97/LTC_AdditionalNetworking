using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
public class StartOfRoundPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound),nameof(StartOfRound.Start))]
    private static void OnStart(StartOfRound __instance)
    {
        if (__instance.IsServer)
        {
            var NetcodeObject = UnityEngine.Object.Instantiate(AdditionalNetworking.NetcodePrefab);
            var networkObject = NetcodeObject.GetComponent<NetworkObject>();
            networkObject.SpawnWithObservers = true;
            networkObject.Spawn();
        }
    }
}