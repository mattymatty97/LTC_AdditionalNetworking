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
        Networking.PlayerControllerB.RegisterMessages();
        Networking.GrabbableObject.RegisterMessages();
        Networking.Shotgun.RegisterMessages();
        Networking.Boombox.RegisterMessages();
    }
}