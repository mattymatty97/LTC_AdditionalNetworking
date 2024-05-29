using System.Collections.Generic;
using System.Reflection.Emit;
using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.Transform;

[HarmonyPatch]
internal class EnemyAIPositionPatch
{
    /*
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemyAI), "Awake")]
    private static void OnAwake(EnemyAI __instance)
    {
        if (!__instance.TryGetComponent<ClientNetworkTransform>(out _))
        {
            __instance.gameObject.AddComponent<ClientNetworkTransform>();
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SyncPositionToClients))]
    private static bool DoNotSyncPosition(EnemyAI __instance)
    {
        return false;
    }
    
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Update))]
    private static IEnumerable<CodeInstruction> PatchUpdate(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {

        var fldInfo = typeof(NetworkBehaviour).GetProperty(nameof(NetworkBehaviour.IsServer))!.GetMethod;
        var serverRpc = typeof(EnemyAI).GetMethod(nameof(EnemyAI.UpdateEnemyRotationServerRpc));
        
        return new CodeMatcher(instructions, generator)
            .End()
            .MatchBack(true, 
                new CodeMatch(OpCodes.Call, serverRpc))
            .Advance(1)
            .CreateLabel(out var ending)
            .MatchBack(false, 
                new CodeMatch(OpCodes.Ldarg_0), 
                new CodeMatch(OpCodes.Call, fldInfo))
            .Insert(new CodeInstruction(OpCodes.Br, ending))
            .Instructions();
    }*/
}