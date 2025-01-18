using System;
using AdditionalNetworking.Dependency;
using AdditionalNetworking.Utils;
using HarmonyLib;
using MonoMod.RuntimeDetour;

namespace AdditionalNetworking.Patches;

[HarmonyPatch]
internal class StartOfRoundPatch
{
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
    private static void AfterUnlockablesSync(StartOfRound __instance)
    {
        __instance.AdditionalNetworking_unlockablesSynced = true;
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