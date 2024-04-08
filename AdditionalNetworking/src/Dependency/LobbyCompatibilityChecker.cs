using System.Runtime.CompilerServices;
using LobbyCompatibility.Enums;
using LobbyCompatibility.Features;

namespace AdditionalNetworking.Dependency
{
    public static class LobbyCompatibilityChecker
    {
        private static bool? _enabled;

        public static bool Enabled
        {
            get
            {
                _enabled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("BMX.LobbyCompatibility");
                return _enabled.Value;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            PluginHelper.RegisterPlugin(AdditionalNetworking.GUID, System.Version.Parse(AdditionalNetworking.VERSION), CompatibilityLevel.ClientOptional, VersionStrictness.Minor);
        }
        
    }
}