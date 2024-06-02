using System;
using System.Collections.Generic;
using System.Reflection;
using AdditionalNetworking.Components;
using AdditionalNetworking.Dependency;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using CessilCellsCeaChells.CeaChore;

[assembly: RequiresMethod(typeof(GrabbableObject), "Awake", typeof(void), [])]
[assembly: RequiresMethod(typeof(EnemyAI), "Awake", typeof(void), [])]
[assembly: RequiresMethod(typeof(NutcrackerEnemyAI), "FixedUpdate", typeof(void), [])]

namespace AdditionalNetworking
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    internal class AdditionalNetworking : BaseUnityPlugin
    {
        public const string GUID = "mattymatty.AdditionalNetworking";
        public const string NAME = "AdditionalNetworking";
        public const string VERSION = "1.0.7";

        internal static ManualLogSource Log;

        internal const uint NetworkObjectIdHash = 28111997;
        internal static GameObject NetcodeContainer { get; private set; }
        internal static GameObject NetcodePrefab { get; private set; }
            
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
				
				Log.LogInfo("Initializing Netcode");
				
				var types = Assembly.GetExecutingAssembly().GetTypes();
				foreach (var type in types)
				{
					var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
					foreach (var method in methods)
					{
						var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
						if (attributes.Length > 0)
						{
							method.Invoke(null, null);
						}
					}
				}

				NetcodeContainer = new GameObject($"{NAME}Container");
				NetcodeContainer.hideFlags |= HideFlags.HideAndDontSave;
				Object.DontDestroyOnLoad(NetcodeContainer);
				NetcodeContainer.SetActive(false);
				NetcodePrefab = new GameObject($"{NAME}Prefab");
				NetcodePrefab.transform.parent = NetcodeContainer.transform;
				var networkObject = NetcodePrefab.AddComponent<NetworkObject>();
				networkObject.AutoObjectParentSync = false;
				networkObject.GlobalObjectIdHash = NetworkObjectIdHash;
				NetcodePrefab.AddComponent<PlayerNetworking>();
				NetcodePrefab.AddComponent<ShotgunNetworking>();
				NetcodePrefab.AddComponent<BoomboxNetworking>();
				
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
                Inventory.SlotChange = config.Bind("Inventory", "SlotChange", true, "use explicit slot numbers when swapping slots");
                Inventory.InventoryChange = config.Bind("Inventory", "InventoryChange", true, "broadcast the exact inventory order");
                //Item state
                State.Shotgun = config.Bind("Item state", "Shotgun", true, "use explicit values for ammo/safety instead of toggle states");
                State.Boombox = config.Bind("Item state", "Boombox", true, "sync state and track id"); 
                //Item state
                Transforms.Grabbables = config.Bind("Transforms", "Grabbables", true, "enable patches for Grabbable position and rotation");
                //Misc
                Misc.Username = config.Bind("Misc", "Username", true, "broadcast the local username once it is assigned to the player object");
                //remove unused options
                PropertyInfo orphanedEntriesProp = config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

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
            
            internal static class Transforms
            {
	            internal static ConfigEntry<bool> Grabbables;
            }
            
            internal static class Misc
            {
	            internal static ConfigEntry<bool> Username;
            }
        }

    }
}