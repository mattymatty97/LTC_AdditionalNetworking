using System;
using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches.State
{
    [HarmonyPatch]
    internal class BoomboxItemPatch
    {
        /// <summary>
        ///  Sync on Creation
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoomboxItem),nameof(BoomboxItem.Start))]
        private static void OnStart(BoomboxItem __instance)
        {
            if (!AdditionalNetworking.PluginConfig.State.Boombox.Value)
                return;
            
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;
            
            if (!StartOfRound.Instance.IsServer)
            {
                BoomboxNetworking.Instance.RequestSyncServerRpc(__instance.NetworkObject);
            }
        }
        
        /// <summary>
        ///  broadcast the new ammo count after a reload animation.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(BoomboxItem),nameof(BoomboxItem.StartMusic))]
        private static void OnMusicChange(BoomboxItem __instance)
        {
            if (!AdditionalNetworking.PluginConfig.State.Boombox.Value)
                return;
            
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;

            if (!__instance.IsOwner)
                return;

            __instance.AdditionalNetworking_dirtyStatus = true;
        }
        
                        
                
        /// <summary>
        ///  broadcast changed data.
        /// </summary>>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GrabbableObject),nameof(GrabbableObject.LateUpdate))]
        private static void OnLateUpdate(GrabbableObject __instance)
        {
            var boomboxItem = __instance as BoomboxItem;
            if (boomboxItem == null)
                return;
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;
            
            if (boomboxItem.AdditionalNetworking_dirtyStatus)
            {
                boomboxItem.AdditionalNetworking_dirtyStatus = false;
                
                if (__instance.IsOwner)
                {
                    var track = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
                    var state = boomboxItem.isPlayingMusic;
                    BoomboxNetworking.Instance.SyncStateServerRpc(__instance.NetworkObject, state, track);
                }
            }
            
        }

    }
}