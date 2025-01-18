using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AdditionalNetworking.Dependency;
using AdditionalNetworking.Patches;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MonoMod.RuntimeDetour;

namespace AdditionalNetworking;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("BMX.LobbyCompatibility", BepInDependency.DependencyFlags.SoftDependency)]
internal class AdditionalNetworking : BaseUnityPlugin
{
    public const string GUID = "mattymatty.AdditionalNetworking";
    public const string NAME = "AdditionalNetworking";
    public const string VERSION = "2.1.3";

    internal static ManualLogSource Log;
    internal static List<Hook> Hooks = [];

    internal static void VerboseLog(LogLevel logLevel, Func<string> message)
    {
        if (message == null)
            return;

        if ((PluginConfig.Debug.Verbose.Value & logLevel) != 0)
            Log.Log(logLevel, message());
    }

    private void Awake()
    {
        Log = Logger;
        try
        {
            if (LobbyCompatibilityChecker.Enabled)
                LobbyCompatibilityChecker.Init();
            Log.LogInfo("Initializing Configs");

            PluginConfig.Init(this);

            Log.LogInfo("Patching Methods");
            var harmony = new Harmony(GUID);
            harmony.PatchAll();

            StartOfRoundPatch.Init();

            Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
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

            config.SaveOnConfigSet = false;
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
            //Item value
            Value.Enabled = config.Bind("Item Values", "Enabled", true, "sync value of scrap if missing");
            Value.IgnoreScanNodes = config.Bind("Item Values", "Ignored Scan Nodes", "Vanilla/Apparatus,",
                "list of items that have custom scan node texts\nListSeparator=,");

            ParseScanNodeList();
            Value.IgnoreScanNodes.SettingChanged += (_, _) => ParseScanNodeList();
            //Misc
            Misc.Username = config.Bind("Misc", "Username", true,
                "broadcast the local username once it is assigned to the player object");
            //Debug
            Debug.Verbose = config.Bind("Debug", "Verbose", LogLevel.None, "additional log lines");


            config.SaveOnConfigSet = true;
            //remove unused options
            var orphanedEntriesProp = config.GetType()
                .GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

            orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
            config.Save(); // Save the config file


            if (LethalConfigProxy.Enabled)
            {
                LethalConfigProxy.AddConfig(Inventory.InventoryChange);
                LethalConfigProxy.AddConfig(Inventory.SlotChange);
                LethalConfigProxy.AddConfig(State.Boombox);
                LethalConfigProxy.AddConfig(State.Shotgun);
                LethalConfigProxy.AddConfig(Value.Enabled);
                LethalConfigProxy.AddConfig(Value.IgnoreScanNodes);
                LethalConfigProxy.AddConfig(State.Shotgun);
                LethalConfigProxy.AddConfig(Misc.Username);
                LethalConfigProxy.AddConfig(Debug.Verbose);
            }

            return;

            void ParseScanNodeList()
            {
                var items = Value.IgnoreScanNodes.Value.Split(",");

                Value.IgnoreScanNodesList = items.Select(s => s.Trim()).Where(s => !s.IsNullOrWhiteSpace()).ToHashSet();
            }
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

        //Item value
        internal static class Value
        {
            internal static ConfigEntry<bool> Enabled;
            internal static ConfigEntry<string> IgnoreScanNodes;
            internal static HashSet<string> IgnoreScanNodesList = [];
        }

        internal static class Misc
        {
            internal static ConfigEntry<bool> Username;
        }

        internal static class Debug
        {
            internal static ConfigEntry<LogLevel> Verbose;
        }
    }
}
