using AdditionalNetworking.Networking;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class NetworkManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(NetworkManager), nameof(NetworkManager.Initialize))]
    private static void AfterInitialize()
    {
        AdditionalNetworking.Log.LogInfo("Registering CustomMessages!");
        PlayerController.RegisterMessages();
        Networking.GrabbableObject.RegisterMessages();
        Shotgun.RegisterMessages();
        Boombox.RegisterMessages();
    }
}