using AdditionalNetworking.Components;
using AdditionalNetworking.Patches.Inventory;
using AdditionalNetworking.Patches.State;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    private static void OnStart(StartOfRound __instance)
    {
        if (!__instance.IsServer)
            return;

        AdditionalNetworking.Log.LogDebug("Here!");
        var networkHandler = UnityEngine.Object.Instantiate<GameObject>(AdditionalNetworking.NetcodePrefab);
        networkHandler.name = $"{AdditionalNetworking.NAME}";
        var networkObject = networkHandler.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
    
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnect))]
    private static void OnClientDisconnect(ulong clientId)
    {
        if (PlayerNetworking.Instance != null)
            PlayerNetworking.Instance.ValidClientIDs.Remove(clientId);
        if (ShotgunNetworking.Instance != null)
            ShotgunNetworking.Instance.ValidClientIDs.Remove(clientId);
        if (BoomboxNetworking.Instance != null)
            BoomboxNetworking.Instance.ValidClientIDs.Remove(clientId);
        if (GrabbableNetworking.Instance != null)
            GrabbableNetworking.Instance.ValidClientIDs.Remove(clientId);
    }
    
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnLocalDisconnect))]
    private static void OnLocalDisconnect()
    {
        PlayerControllerBPatch.DirtyInventory.Clear();
        PlayerControllerBPatch.DirtySlots.Clear();
        ShotgunItemPatch.DirtyAmmo.Clear();
        ShotgunItemPatch.DirtySafety.Clear();
        BoomboxItemPatch.DirtyStatus.Clear();
        GrabbableObjectPatch.RequestedValues.Clear();
    }
}