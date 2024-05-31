using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Patches.Transform;

[HarmonyPatch]
internal class EnemyAIPositionPatch
{
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemyAI), "Awake")]
    private static void OnAwake(EnemyAI __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Transforms.EnemyAI.Value)
            return;
        
        if (!__instance.TryGetComponent<EnemyNetworkTransform>(out _))
        {
            __instance.gameObject.AddComponent<EnemyNetworkTransform>();
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.SyncPositionToClients))]
    private static bool DoNotSyncPosition(EnemyAI __instance)
    {
        if (!AdditionalNetworking.PluginConfig.Transforms.EnemyAI.Value)
            return true;
        
        return false;
    }
    
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Update))]
    private static IEnumerable<CodeInstruction> PatchUpdate(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        if (!AdditionalNetworking.PluginConfig.Transforms.EnemyAI.Value)
            return instructions;

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
    }

    [HarmonyPatch]
    internal static class NutcrackerPatches
    {
        private static readonly FieldInfo OldTargetTorsoDegrees = typeof(NutcrackerEnemyAI).GetField("oldTorsoDegrees", BindingFlags.Instance | BindingFlags.NonPublic);


        // ReSharper disable Unity.PerformanceAnalysis
        private static IEnumerator DoTorsoSync(NutcrackerEnemyAI nutcrackerObject)
        {
            while (!nutcrackerObject.isEnemyDead)
            {
                if ((int)OldTargetTorsoDegrees.GetValue(nutcrackerObject) != nutcrackerObject.targetTorsoDegrees && nutcrackerObject.IsOwner)
                {
                    if (NutcrackerNetworking.Instance != null && NutcrackerNetworking.Instance.Enabled)
                    {
                        NutcrackerNetworking.Instance.SyncTorsoServerRpc(nutcrackerObject.NetworkObject,nutcrackerObject.targetTorsoDegrees);
                    }
                }

                OldTargetTorsoDegrees.SetValue(nutcrackerObject, nutcrackerObject.targetTorsoDegrees);
                yield return new WaitForSeconds(AdditionalNetworking.PluginConfig.Transforms.NutcrackerUpdatePeriod.Value);
            }
        } 
        
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.Start))]
        private static void AfterNutcrackerSpawn(NutcrackerEnemyAI __instance)
        {
            if (!AdditionalNetworking.PluginConfig.Transforms.NutcrackerAI.Value)
                return;
            
            __instance.StartCoroutine(DoTorsoSync(__instance));
        }
        
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(NutcrackerEnemyAI), nameof(NutcrackerEnemyAI.SetTargetDegreesToPosition))]
        private static IEnumerable<CodeInstruction> PatchSetTargetDegreesToPosition(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            if (!AdditionalNetworking.PluginConfig.Transforms.NutcrackerAI.Value)
                return instructions;

            var ownerMethod = typeof(NetworkBehaviour).GetProperty(nameof(NetworkBehaviour.IsOwner))!.GetMethod;
            var fieldInfo = typeof(NutcrackerEnemyAI).GetField(nameof(NutcrackerEnemyAI.torsoTurnSpeed));
            return new CodeMatcher(instructions, generator)
                .End()
                .MatchBack(false, 
                    new CodeMatch(OpCodes.Ldarg_0), 
                    new CodeMatch(OpCodes.Ldc_R4),
                    new CodeMatch(OpCodes.Stfld, fieldInfo))
                .CreateLabel(out var endingLabel)
                .Start()
                .Insert(
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, ownerMethod),
                    new CodeInstruction(OpCodes.Brfalse, endingLabel)
                )
                .Instructions();
        }    
    }
    
}