using System;
using System.Collections.Generic;
using System.Reflection;
using AdditionalNetworking.Components;
using AdditionalNetworking.Dependency;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;

namespace AdditionalNetworking
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(RuntimeNetcodeRPCValidator.MyPluginInfo.PLUGIN_GUID, RuntimeNetcodeRPCValidator.MyPluginInfo.PLUGIN_VERSION)]
    //[BepInDependency("BMX.LobbyCompatibility", Flags:BepInDependency.DependencyFlags.HardDependency)]
    internal class AdditionalNetworking : BaseUnityPlugin
    {
        public const string GUID = "mattymatty.AdditionalNetworking";
        public const string NAME = "AdditionalNetworking";
        public const string VERSION = "0.0.1";

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
				
				Log.LogInfo("Adding Netcode");
				var netcodeValidator = new RuntimeNetcodeRPCValidator.NetcodeValidator(GUID);
				netcodeValidator.PatchAll();
				
				netcodeValidator.BindToPreExistingObjectByBehaviour<PlayerNetworking, PlayerControllerB>();
				netcodeValidator.BindToPreExistingObjectByBehaviour<ShotgunNetworking, ShotgunItem>();
				//netcodeValidator.BindToPreExistingObjectByBehaviour<PlayerNetworking, PlayerControllerB>();
				
				Log.LogInfo("Patching Methods");
				var harmony = new Harmony(GUID);
				harmony.PatchAll(Assembly.GetExecutingAssembly());
				
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