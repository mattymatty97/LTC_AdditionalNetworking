using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnect))]
    private static void OnClientDisconnect(ulong clientId)
    {
        if (PlayerNetworking.Instance != null)
            PlayerNetworking.Instance.ValidClientIDs.Remove(clientId);
        if (ShotgunNetworking.Instance != null)
            ShotgunNetworking.Instance.ValidClientIDs.Remove(clientId);
    }
    
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnLocalDisconnect))]
    private static void OnLocalDisconnect()
    {
        PlayerControllerBPatch.DirtyInventory.Clear();
        PlayerControllerBPatch.DirtySlots.Clear();
        ShotgunItemPatch.DirtyAmmo.Clear();
        ShotgunItemPatch.DirtySafety.Clear();
    }
}