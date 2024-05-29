using System.Collections.Generic;
using System.Reflection.Emit;
using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.Transform;

[HarmonyPatch]
internal class GrabbablePositionPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GrabbableObject), "Awake")]
    private static void OnAwake(GrabbableObject __instance)
    {
        if (!__instance.TryGetComponent<ClientNetworkTransform>(out _))
        {
            __instance.gameObject.AddComponent<ClientNetworkTransform>();
        }
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.FallWithCurve))]
    private static IEnumerable<CodeInstruction> PatchFallWithCurve(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {

        var ownerMethod = typeof(NetworkBehaviour).GetProperty(nameof(NetworkBehaviour.IsOwner))!.GetMethod;
        return new CodeMatcher(instructions, generator)
            .End()
            .MatchBack(false, 
                new CodeMatch(OpCodes.Ldarg_0), 
                new CodeMatch(OpCodes.Ldarg_0))
            .CreateLabel(out var endingLabel)
            .Start()
            .Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, ownerMethod),
                new CodeInstruction(OpCodes.Brfalse, endingLabel)
                )
            .Instructions();
    }    
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
    private static IEnumerable<CodeInstruction> PatchUpdate(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {

        var fieldInfo = typeof(GrabbableObject).GetField(nameof(GrabbableObject.reachedFloorTarget));
        return new CodeMatcher(instructions, generator)
            .End()
            .CreateLabel(out var endingLabel)
            .MatchBack(true, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Ldfld, fieldInfo), new CodeMatch(OpCodes.Brtrue))
            .SetOperandAndAdvance(endingLabel)
            .Instructions();
    }
    
}