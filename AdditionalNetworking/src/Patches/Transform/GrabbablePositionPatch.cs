using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AdditionalNetworking.Components;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Patches.Transform;

[HarmonyPatch]
internal class GrabbablePositionPatch
{
    [HarmonyPatch]
    internal class GrabbablePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), "Awake")]
        private static void OnAwake(GrabbableObject __instance)
        {
            if (!AdditionalNetworking.PluginConfig.Transforms.Grabbables.Value)
                return;

            if (__instance.TryGetComponent<NetworkObject>(out _))
            {
                if (!__instance.TryGetComponent<GrabbableNetworkTransform>(out _))
                {
                    __instance.gameObject.AddComponent<GrabbableNetworkTransform>();
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        private static void AfterOnNetworkSpawn(NetworkBehaviour __instance)
        {
            if (__instance is GrabbableObject)
            {
                if (ParentNetworking.Instance != null && ParentNetworking.Instance.Enabled && !__instance.IsServer)
                {
                    ParentNetworking.Instance.RequestParentServerRpc(__instance.NetworkObject);
                }
            }
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.FallWithCurve))]
        private static IEnumerable<CodeInstruction> PatchFallWithCurve(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            if (!AdditionalNetworking.PluginConfig.Transforms.Grabbables.Value)
                return instructions;

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
            if (!AdditionalNetworking.PluginConfig.Transforms.Grabbables.Value)
                return instructions;

            var fieldInfo = typeof(GrabbableObject).GetField(nameof(GrabbableObject.reachedFloorTarget));
            return new CodeMatcher(instructions, generator)
                .End()
                .CreateLabel(out var endingLabel)
                .MatchBack(true,
                    new CodeMatch(OpCodes.Ldarg_0),
                    new CodeMatch(OpCodes.Ldfld, fieldInfo),
                    new CodeMatch(OpCodes.Brtrue))
                .SetOperandAndAdvance(endingLabel)
                .Instructions();
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void HandleSpawnOnGround(GrabbableObject __instance)
        {
            if (__instance.reachedFloorTarget)
                __instance.transform.localPosition = __instance.targetFloorPosition;
        }
        /**/
    }
    
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {
        /*
        private static void ParentIfServer(UnityEngine.Transform transform, UnityEngine.Transform parent,
            bool worldPositionStays)
        {
            if (NetworkManager.Singleton.IsServer)
            {
                transform.SetParent(parent, worldPositionStays);
            }
        }
        
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetObjectAsNoLongerHeld))]
        private static IEnumerable<CodeInstruction> PatchSetObjectAsNoLongerHeld(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            
            var parentMethod = typeof(UnityEngine.Transform).GetMethod(nameof(UnityEngine.Transform.SetParent), [typeof(UnityEngine.Transform), typeof(bool)]);
            var patchedParentMethod = typeof(PlayerControllerBPatch).GetMethod(nameof(ParentIfServer), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

            return new CodeMatcher(instructions, generator)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Callvirt, parentMethod))
                .Repeat(m =>
                {
                    m.SetOperandAndAdvance(patchedParentMethod);
                })
                .Instructions();
        }        
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DropAllHeldItems))]
        private static IEnumerable<CodeInstruction> PatchDropAllHeldItems(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var parentMethod = typeof(UnityEngine.Transform).GetMethod(nameof(UnityEngine.Transform.SetParent), [typeof(UnityEngine.Transform), typeof(bool)]);
            var patchedParentMethod = typeof(PlayerControllerBPatch).GetMethod(nameof(ParentIfServer), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

            return new CodeMatcher(instructions, generator)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Callvirt, parentMethod))
                .Repeat(m =>
                {
                    m.SetOperandAndAdvance(patchedParentMethod);
                })
                .Instructions();
        }
        
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlaceGrabbableObject))]
        private static IEnumerable<CodeInstruction> PatchPlaceGrabbableObject(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var parentMethod = typeof(UnityEngine.Transform).GetMethod(nameof(UnityEngine.Transform.SetParent), [typeof(UnityEngine.Transform), typeof(bool)]);
            var patchedParentMethod = typeof(PlayerControllerBPatch).GetMethod(nameof(ParentIfServer), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

            return new CodeMatcher(instructions, generator)
                .MatchForward(true,
                    new CodeMatch(OpCodes.Callvirt, parentMethod))
                .Repeat(m =>
                {
                    m.SetOperandAndAdvance(patchedParentMethod);
                })
                .Instructions();
        }
        /**/
    }
}