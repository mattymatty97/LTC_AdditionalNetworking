using AdditionalNetworking.Components;
using AdditionalNetworking.Patches.Inventory;
using AdditionalNetworking.Patches.State;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class RoundManagerPatch
{

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
    private static void OnNewLevel(RoundManager __instance)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            return;
        __instance.AdditionalNetworking_spawnedScrapPendingSync = true;
    }
    
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc))]
    private static void AfterScrapValueSync(RoundManager __instance)
    {
        NetworkManager networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
            return;
        __instance.AdditionalNetworking_spawnedScrapPendingSync = false;
    }

}