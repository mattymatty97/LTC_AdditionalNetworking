using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace AdditionalNetworking_Preloader
{
    internal class AdditionalNetworking
    {
        internal static ManualLogSource Log { get; } = Logger.CreateLogSource(nameof(AdditionalNetworking));

        public static IEnumerable<string> TargetDLLs { get; } = new string[] { "Assembly-CSharp.dll" };

        private static readonly string MainDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        
        public static void Patch(AssemblyDefinition assembly)
        {

            var logHandler = (bool fail, string message) =>
            {
                if (fail)
                    Log.LogWarning(message);
                Log.LogInfo(message);
            };
            
            Log.LogWarning($"Patching {assembly.Name.Name}");
            if (assembly.Name.Name == "Assembly-CSharp")
            {
                foreach (var type in assembly.MainModule.Types)
                {
                    switch (type.FullName)
                    {
                        case "GrabbableObject":
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_isInitialized",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler); 
                        
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_hasRequestedSync",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                            break;
                        case "PlayerControllerB":
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_dirtyInventory",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                        
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_dirtySlots",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                            break;
                        case "ShotgunItem":
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_dirtyAmmo",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                        
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_dirtySafety",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                            break;
                        case "BoomboxItem":
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_dirtyStatus",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                            break;
                        case "StartOfRound":
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_unlockablesSynced",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                            break;
                        case "RoundManager":
                            type.AddField(
                                FieldAttributes.Private,
                                "AdditionalNetworking_spawnedScrapPendingSync",
                                type.Module.ImportReference(typeof(bool)),
                                logHandler);
                            break;
                    }
                }
            }

            if (!PluginConfig.Enabled.Value) 
                return;
            
            var outputAssembly = $"{PluginConfig.OutputPath.Value}/{assembly.Name.Name}{PluginConfig.OutputExtension.Value}";
            Log.LogWarning($"Saving modified Assembly to {outputAssembly}");
            assembly.Write(outputAssembly);
        }

        // Cannot be renamed, method name is important
        public static void Initialize()
        {
            Log.LogInfo($"AdditionalNetworking Prepatcher Started");
            PluginConfig.Init();
        }

        // Cannot be renamed, method name is important
        public static void Finish()
        {
            Log.LogInfo($"AdditionalNetworking Prepatcher Finished");
        }

        public static class PluginConfig
        {
            public static void Init()
            {
                var config = new ConfigFile(Utility.CombinePaths(MainDir, "Development.cfg"), true);
                //Initialize Configs
                Enabled = config.Bind("DevelOptions", "Enabled", false, "Enable development dll output");
                OutputPath = config.Bind("DevelOptions", "OutputPath", MainDir, "Folder where to write the modified dlls");
                OutputExtension = config.Bind("DevelOptions", "OutputExtension", ".pdll", "Extension to use for the modified dlls\n( Do not use .dll if outputting inside the BepInEx folders )");

                //remove unused options
                PropertyInfo orphanedEntriesProp = config.GetType()
                    .GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

                var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

                orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
                config.Save(); // Save the config file
            }

            internal static ConfigEntry<bool> Enabled;
            internal static ConfigEntry<string> OutputPath;
            internal static ConfigEntry<string> OutputExtension;
        }
    }
}