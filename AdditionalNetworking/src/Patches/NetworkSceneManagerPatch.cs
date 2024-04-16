using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class NetworkSceneManagerPatch
{
/*
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(NetworkSceneManager), nameof(NetworkSceneManager.PopulateScenePlacedObjects))]
    private static void AddNetworkingObject(NetworkSceneManager __instance)
    {
        var scenePlacedObjects = __instance.ScenePlacedObjects;

        if (scenePlacedObjects.ContainsKey(NetworkObjectIdHash)) 
            return;
        
        var copy = Object.Instantiate<GameObject>(AdditionalNetworking.NetcodePrefab);
        copy.name = AdditionalNetworking.NAME;
        var copyNetworkObject = copy.GetComponent<NetworkObject>();
        copyNetworkObject.GlobalObjectIdHash = NetworkObjectIdHash;
        var handle = copyNetworkObject.gameObject.scene.handle;
        var globalObjectIdHash = copyNetworkObject.GlobalObjectIdHash;

        scenePlacedObjects.Add(globalObjectIdHash, new Dictionary<int, NetworkObject>());

        scenePlacedObjects[globalObjectIdHash][handle] = copyNetworkObject;
    }*/
}