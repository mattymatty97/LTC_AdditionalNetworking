using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnect))]
    private static void onClientDisconnect(ulong clientId)
    {
        if (PlayerNetworking.Instance != null)
            PlayerNetworking.Instance.ValidClientIDs.Remove(clientId);
        if (ShotgunNetworking.Instance != null)
            ShotgunNetworking.Instance.ValidClientIDs.Remove(clientId);
    }
}