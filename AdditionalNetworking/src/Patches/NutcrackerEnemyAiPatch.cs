using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class NutcrackerEnemyAiPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
    private static void OnReload(NutcrackerEnemyAI __instance)
    {
        var networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            return;
        if (!__instance.IsOwner)
            return;
        if(ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
            ShotgunNetworking.Instance.syncAmmoServerRpc(__instance.gun.NetworkObject, __instance.gun.shellsLoaded);
    }
}