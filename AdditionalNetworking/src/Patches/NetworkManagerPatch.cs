using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class NetworkManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetworkManager),nameof(NetworkManager.SetSingleton))]
    private static void RegisterPrefab()
    {
        NetworkManager.Singleton.AddNetworkPrefab(AdditionalNetworking.NetcodePrefab);
    }
}