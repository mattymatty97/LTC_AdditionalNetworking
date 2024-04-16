using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class NetworkManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetworkManager),nameof(NetworkManager.SetSingleton))]
    private static void AfterSingleton(NetworkManager __instance)
    {
        __instance.AddNetworkPrefab(AdditionalNetworking.NetcodePrefab);
        
        AdditionalNetworking.Log.LogInfo("Added Prefab!");
    }
}