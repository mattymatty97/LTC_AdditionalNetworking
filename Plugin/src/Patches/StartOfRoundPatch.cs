using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using AdditionalNetworking.Dependency;
using AdditionalNetworking.Interfaces;
using AdditionalNetworking.Utils;
using AdditionalNetworking.Utils.IL;
using HarmonyLib;
using MonoMod.RuntimeDetour;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
    private static void AfterUnlockablesClientSync(StartOfRound __instance)
    {
        if (!__instance.IsRPCClientStage())
            return;

        if (!AdditionalNetworking.PluginConfig.Value.ShouldSkipGrabbableSync)
            ((INetworkStartOfRound)__instance).AdditionalNetworking_ValuablesSynced = true;
    }

    private static void SyncGrabbableReplacement()
    {
        var targets = NetworkManager.Singleton.ConnectedClientsIds.Where(id => id != NetworkManager.ServerClientId)
            .ToArray();

        var grabbables = Object.FindObjectsOfType<GrabbableObject>();
        var holders = grabbables.Where(g => g.IsSpawned).Select(g => new GrabbableDataHolder(g));
        try
        {
            AdditionalNetworking.Log.LogInfo($"Syncing {grabbables.Length} items!");
            foreach (var holderChunk in holders.Chunk(500))
            {
                Networking.GrabbableObject.SyncMultipleValuesClientRpc(holderChunk.ToArray(), targets);
            }

            Networking.GrabbableObject.MarkValuablesSyncedClientRpc(targets);
        }
        catch (Exception ex)
        {
            AdditionalNetworking.Log.LogFatal($"Exception syncing all grabbables\n{ex}");
        }
    }

    [HarmonyTranspiler]
    [HarmonyBefore("LethalPerformance")]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesServerRpc))]
    private static IEnumerable<CodeInstruction> PatchSyncShipUnlockablesServerRpc(
        IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator, MethodBase method)
    {
        var codes = instructions.ToArray();

        var injector = new ILInjector(codes, ilGenerator);

        // + if (!AdditionalNetworking.PluginConfig.Value.ShouldSkipGrabbableSync)
        // + {
        // =     GrabbableObject[] array3 = (from x in UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
        // =         orderby Vector3.Distance(x.transform.position, Vector3.zero)
        // =         select x).ToArray();
        // + }
        // + else
        // + {
        // +     GrabbableObject[] array3 = [];
        // + }

        injector.Find([
            ILMatcher.Ldc(),
            ILMatcher.Ldc(),
            ILMatcher.Call(typeof(Object).GetGenericMethod(nameof(Object.FindObjectsByType),
                [typeof(FindObjectsInactive), typeof(FindObjectsSortMode)], [typeof(GrabbableObject)])),
        ]);


        if (!injector.IsValid)
        {
            AdditionalNetworking.Log.LogError(
                $"Failed to find FindObjectsByType in {method.DeclaringType!.FullName}.{method.Name}");
            return codes;
        }

        injector.DefineLabel(out var continueLabel)
            .DefineLabel(out var emptyLabel)
            .InsertAfterBranch([
                new CodeInstruction(OpCodes.Call,
                    typeof(AdditionalNetworking.PluginConfig.Value)
                        .GetProperty(nameof(AdditionalNetworking.PluginConfig.Value.ShouldSkipGrabbableSync),
                            BindingFlags.Static | BindingFlags.NonPublic)!.GetMethod),
                new CodeInstruction(OpCodes.Brtrue, emptyLabel),
            ]);

        injector.Find([
            ILMatcher.Call(typeof(Enumerable).GetGenericMethod(nameof(Enumerable.ToArray),
                [typeof(IEnumerable<GrabbableObject>)],
                [typeof(GrabbableObject)])),
            ILMatcher.Stloc().CaptureAs(out var storeInstruction)
        ]);

        if (!injector.IsValid)
        {
            AdditionalNetworking.Log.LogError(
                $"Failed to find ToArray in {method.DeclaringType!.FullName}.{method.Name}");
            return codes;
        }

        injector.GoToMatchEnd()
            .AddLabel(continueLabel)
            .Insert([
                new CodeInstruction(OpCodes.Br, continueLabel),
                new CodeInstruction(OpCodes.Call, typeof(Array)
                    .GetGenericMethod(nameof(Array.Empty), [], [typeof(GrabbableObject)])) { labels = [emptyLabel] },
                storeInstruction
            ]);

        // = SyncShipUnlockablesClientRpc([...]);
        // + SyncGrabbableReplacement();

        injector.Find([
            ILMatcher.Call(typeof(StartOfRound).GetMethod(nameof(StartOfRound.SyncShipUnlockablesClientRpc))),
        ]);

        if (!injector.IsValid)
        {
            AdditionalNetworking.Log.LogError(
                $"Failed to find call to SyncShipUnlockablesClientRpc in {method.DeclaringType!.FullName}.{method.Name}");
            return codes;
        }

        injector.GoToMatchEnd()
            .Insert([
                new CodeInstruction(OpCodes.Call, typeof(StartOfRoundPatch)
                    .GetMethod(nameof(SyncGrabbableReplacement), BindingFlags.Static | BindingFlags.NonPublic)),
            ]);

        return injector.ReleaseInstructions();
    }

    internal static void Init()
    {
        AdditionalNetworking.Hooks.Add(new Hook(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.Awake)),
            PrepareItemCache));
    }

    private static void PrepareItemCache(Action<StartOfRound> orig, StartOfRound __instance)
    {
        ItemCategory.ItemModMap.Clear();

        ItemCategory.VanillaItems ??= __instance.allItemsList.itemsList.ToArray();

        foreach (var itemType in ItemCategory.VanillaItems) ItemCategory.ItemModMap.TryAdd(itemType, ("Vanilla", ""));

        orig(__instance);
    }

    [HarmonyPrefix]
    [HarmonyAfter("imabatby.lethallevelloader")]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    private static void PopulateModdedCache(StartOfRound __instance)
    {
        if (LethalLibProxy.Enabled)
            LethalLibProxy.GetModdedItems(in ItemCategory.ItemModMap);

        if (LethalLevelLoaderProxy.Enabled)
            LethalLevelLoaderProxy.GetModdedItems(in ItemCategory.ItemModMap);

        foreach (var itemType in __instance.allItemsList.itemsList)
        {
            ItemCategory.ItemModMap.TryAdd(itemType, ("Unknown", ""));
        }
    }
}