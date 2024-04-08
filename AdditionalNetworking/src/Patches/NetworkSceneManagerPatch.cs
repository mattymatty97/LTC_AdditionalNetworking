using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class NetworkSceneManagerPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(NetworkSceneManager), nameof(NetworkSceneManager.PopulateScenePlacedObjects))]
    private static void addNetworkingObject(NetworkSceneManager __instance)
    {
        var ScenePlacedObjects = __instance.ScenePlacedObjects;
        var copy = Object.Instantiate<GameObject>(AdditionalNetworking.NetcodePrefab);
        copy.name = AdditionalNetworking.NAME;       
        NetworkObject copyNetworkObject = copy.GetComponent<NetworkObject>();
        var handle = copyNetworkObject.gameObject.scene.handle;
        var globalObjectIdHash = copyNetworkObject.GlobalObjectIdHash;

        if (!ScenePlacedObjects.ContainsKey(globalObjectIdHash))
        {
                ScenePlacedObjects.Add(globalObjectIdHash, new Dictionary<int, NetworkObject>());
        }
        
        ScenePlacedObjects[globalObjectIdHash][handle] = copyNetworkObject;
    }
}