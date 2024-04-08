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

namespace AdditionalNetworking
{
    [BepInPlugin(GUID, NAME, VERSION)]
    //[BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.HardDependency)]
    internal class AdditionalNetworking : BaseUnityPlugin
    {
        public const string GUID = "mattymatty.AdditionalNetworking";
        public const string NAME = "AdditionalNetworking";
        public const string VERSION = "0.0.1";

        internal static ManualLogSource Log;

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
				networkObject.GlobalObjectIdHash = 28111997;
				networkObject.AutoObjectParentSync = false;
				NetcodePrefab.AddComponent<PlayerNetworking>();
				NetcodePrefab.AddComponent<ShotgunNetworking>();
				
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
                //ItemSync
                
                //remove unused options
                PropertyInfo orphanedEntriesProp = config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

                var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

                orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
                config.Save(); // Save the config file
            }
            
        }

    }
}