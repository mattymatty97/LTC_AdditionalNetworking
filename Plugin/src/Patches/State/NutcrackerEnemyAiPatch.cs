using System;
using AdditionalNetworking.Networking;
using AdditionalNetworking.Utils;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.State;

[HarmonyPatch]
internal class NutcrackerEnemyAiPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.ReloadGunClientRpc))]
    private static void OnReload(NutcrackerEnemyAI __instance)
    {
        var networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client ||
            (!networkManager.IsClient && !networkManager.IsHost))
            return;
        if (!__instance.IsOwner)
            return;

        if (!AdditionalNetworking.PluginConfig.ItemState.Shotgun.Value)
            return;

        if (!__instance.NetworkObject.IsSpawned)
        {
            AdditionalNetworking.Log.LogFatal(
                $"{__instance.NetworkObject.name}({__instance.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        if (!__instance.gun.NetworkObject.IsSpawned)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.gun.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"{itemTag}({__instance.gun.GetInstanceID()}) is not spawned! nobody else in the network knows about it!");
            return;
        }

        try
        {
            Shotgun.SyncAmmoServerRpc(__instance.gun.NetworkObject, __instance.gun.shellsLoaded);
        }
        catch (Exception ex)
        {
            var itemTag = ItemCategory.GetKeyForItem(__instance.gun.itemProperties);
            AdditionalNetworking.Log.LogFatal(
                $"Exception syncing ammo of {itemTag}({__instance.gun.NetworkObjectId}):\n{ex}");
        }
    }
}