using System;
using System.Collections.Generic;
using System.Reflection;
using AdditionalNetworking.Dependency;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace AdditionalNetworking;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
internal class AdditionalNetworking : BaseUnityPlugin
{
    public const string GUID = "mattymatty.AdditionalNetworking";
    public const string NAME = "AdditionalNetworking";
    public const string VERSION = "2.0.0";

    internal static ManualLogSource Log;

    private void Awake()
    {
        Log = Logger;
        try
        {
            if (LobbyCompatibilityChecker.Enabled)
                LobbyCompatibilityChecker.Init();
            if (AsyncLoggerProxy.Enabled)
                AsyncLoggerProxy.WriteEvent(NAME, "Awake", "Initializing");
            Log.LogInfo("Initializing Configs");

            PluginConfig.Init(this);

            Log.LogInfo("Patching Methods");
            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
            if (AsyncLoggerProxy.Enabled)
                AsyncLoggerProxy.WriteEvent(NAME, "Awake", "Finished Initializing");
        }
        catch (Exception ex)
        {
            Log.LogError("Exception while initializing: \n" + ex);
        }
    }

    internal static class PluginConfig
    {
        internal static void Init(BaseUnityPlugin plugin)
        {
            var config = plugin.Config;
            //Initialize Configs
            //Inventory
            Inventory.SlotChange = config.Bind("Inventory", "SlotChange", true,
                "use explicit slot numbers when swapping slots");
            Inventory.InventoryChange =
                config.Bind("Inventory", "InventoryChange", true, "broadcast the exact inventory order");
            //Item state
            State.Shotgun = config.Bind("Item state", "Shotgun", true,
                "use explicit values for ammo/safety instead of toggle states");
            State.Boombox = config.Bind("Item state", "Boombox", true, "sync state and track id");
            //Misc
            Misc.Username = config.Bind("Misc", "Username", true,
                "broadcast the local username once it is assigned to the player object");
            //Debug
            Debug.Verbose = config.Bind("Debug", "Verbose", false, "additional log lines");
            //remove unused options
            var orphanedEntriesProp = config.GetType()
                .GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            config.Save(); // Save the config file
        }

        internal static class Inventory
        {
            internal static ConfigEntry<bool> SlotChange;
            internal static ConfigEntry<bool> InventoryChange;
        }

        //Item state
        internal static class State
        {
            internal static ConfigEntry<bool> Shotgun;
            internal static ConfigEntry<bool> Boombox;
        }

        internal static class Misc
        {
            internal static ConfigEntry<bool> Username;
        }

        internal static class Debug
        {
            internal static ConfigEntry<bool> Verbose;
        }
    }
}