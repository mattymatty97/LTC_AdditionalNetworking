using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
            Log.LogWarning($"Patching {assembly.Name.Name}");
            if (assembly.Name.Name == "Assembly-CSharp")
            {
                foreach (TypeDefinition type in assembly.MainModule.Types)
                {
                    if (type.FullName == "GrabbableObject")
                    {
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_isInitialized", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_isInitialized' to {type.FullName}");
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_hasRequestedSync", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_hasRequestedSync' to {type.FullName}");
                    }else if (type.FullName == "PlayerControllerB")
                    {
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_dirtyInventory", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_dirtyInventory' to {type.FullName}");
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_dirtySlots", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_dirtySlots' to {type.FullName}");
                    }else if (type.FullName == "ShotgunItem")
                    {
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_dirtyAmmo", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_dirtyAmmo' to {type.FullName}");
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_dirtySafety", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_dirtySafety' to {type.FullName}");
                    }else if (type.FullName == "BoomboxItem")
                    {
                        type.Fields.Add(new FieldDefinition("AdditionalNetworking_dirtyStatus", FieldAttributes.Private, type.Module.ImportReference(typeof(bool))));
                        Log.LogInfo($"Adding field 'AdditionalNetworking_dirtyStatus' to {type.FullName}");
                    }
                }
            }
            if (MainDir.Contains("Debug"))
                assembly.Write($"{MainDir}/{assembly.Name.Name}.pdll");
        }
        
        // Cannot be renamed, method name is important
        public static void Initialize()
        {
            Log.LogInfo($"AdditionalNetworking Prepatcher Started");
        }

        // Cannot be renamed, method name is important
        public static void Finish()
        {
            Log.LogInfo($"AdditionalNetworking Prepatcher Finished");
        }

    }
}