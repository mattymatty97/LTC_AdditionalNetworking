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
        AdditionalNetworking.Log.LogInfo("Registering Named Messages!");
        PlayerController.RegisterMessages();
        Networking.GrabbableObject.RegisterMessages();
        Shotgun.RegisterMessages();
        Boombox.RegisterMessages();
    }

    [HarmonyPatch(typeof(GameNetworkManager), "SetInstanceValuesBackToDefault")]
    [HarmonyPostfix]
    public static void SetInstanceValuesBackToDefault()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.CustomMessagingManager == null)
            return;

        AdditionalNetworking.Log.LogInfo("Unregistering Named Messages!");
        PlayerController.UnregisterMessages();
        Networking.GrabbableObject.UnregisterMessages();
        Shotgun.UnregisterMessages();
        Boombox.UnregisterMessages();
    }
}