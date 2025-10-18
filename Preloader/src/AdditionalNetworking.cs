using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using AdditionalNetworking.Preloader.Utils;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;

namespace AdditionalNetworking.Preloader;

internal class AdditionalNetworking
{
    public const string GUID = MyPluginInfo.PLUGIN_GUID;
    public const string NAME = MyPluginInfo.PLUGIN_NAME;
    public const string VERSION = MyPluginInfo.PLUGIN_VERSION;

    internal static readonly BepInPlugin Plugin = new BepInPlugin(GUID, NAME, VERSION);

    internal static ManualLogSource Log { get; } = Logger.CreateLogSource(NAME);

    public static IEnumerable<string> TargetDLLs { get; } = ["Assembly-CSharp.dll"];

    private static readonly string MainDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private static readonly Dictionary<string, Dictionary<string, List<TypeDefinition>>> Interfaces = [];

    public static void Patch(AssemblyDefinition assembly)
    {
        var logHandler = (bool fail, string message) =>
        {
            if (fail)
                Log.LogWarning(message);
            else
                Log.LogDebug(message);
        };

        if (Interfaces.TryGetValue(assembly.Name.Name, out var dict))
        {
            Log.LogWarning($"Patching {assembly.Name.Name}");
            foreach (var type in assembly.MainModule.Types)
            {
                if (!dict.TryGetValue(type.Name, out var list))
                    continue;

                foreach (var @interface in list)
                {
                    if (!type.ImplementInterface(@interface, logHandler))
                        break;
                }
            }
        }

        if (!PluginConfig.Enabled.Value)
            return;

        var outputAssembly =
            $"{PluginConfig.OutputPath.Value}/{assembly.Name.Name}{PluginConfig.OutputExtension.Value}";
        Log.LogWarning($"Saving modified Assembly to {outputAssembly}");
        assembly.Write(outputAssembly);
    }

    // Cannot be renamed, method name is important
    public static void Initialize()
    {
        Log.LogInfo($"AdditionalNetworking Prepatcher Started");
        PluginConfig.Init();

        var pluginPath = Directory.EnumerateDirectories(Paths.PluginPath).FirstOrDefault(d => d.Contains(NAME));

        if (pluginPath == null)
        {
            Log.LogFatal("Could not find Interfaces dll!");
            return;
        }

        var dllPath = Path.Combine(pluginPath, $"{NAME}.Interfaces.dll");

        if (!File.Exists(dllPath))
        {
            Log.LogFatal("Could not find Interfaces dll!");
            return;
        }

        var interfaceAssembly = AssemblyDefinition.ReadAssembly(dllPath);

        var attributeName = typeof(InjectInterfaceAttribute).FullName;

        foreach (var type in interfaceAssembly.MainModule.Types)
        {
            if (!type.IsInterface)
                continue;

            var attributes = type.CustomAttributes.Where(at => at.AttributeType.FullName == attributeName);

            foreach (var customAttribute in attributes)
            {
                var attr = customAttribute.GetAttributeInstance<InjectInterfaceAttribute>();

                if (!Interfaces.TryGetValue(attr.AssemblyName, out var dict))
                {
                    Interfaces[attr.AssemblyName] = dict = [];
                }

                if (!dict.TryGetValue(attr.TypeName, out var list))
                {
                    dict[attr.TypeName] = list = [];
                }

                list.Add(type);
            }
        }
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
            var config = new ConfigFile(Utility.CombinePaths(MainDir, $"{NAME}.Development.cfg"), true);
            //Initialize Configs
            Enabled = config.Bind("DevelOptions", "Enabled", false, "Enable development dll output");
            OutputPath = config.Bind("DevelOptions", "OutputPath", MainDir,
                "Folder where to write the modified dlls");
            OutputExtension = config.Bind("DevelOptions", "OutputExtension", ".pdll",
                "Extension to use for the modified dlls\n( Do not use .dll if outputting inside the BepInEx folders )");

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