using System.Runtime.CompilerServices;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace AdditionalNetworking.Dependency
{
    public static class LobbyCompatibilityChecker
    {
        public static bool Enabled { get { return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility"); } }
        
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            PluginHelper.RegisterPlugin(AdditionalNetworking.GUID, System.Version.Parse(AdditionalNetworking.VERSION), CompatibilityLevel.ClientOptional, VersionStrictness.Minor);
        }
        
    }
}