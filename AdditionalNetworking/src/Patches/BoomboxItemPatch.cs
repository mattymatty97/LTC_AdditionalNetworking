using System;
using System.Collections.Generic;
using AdditionalNetworking.Components;
using HarmonyLib;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class BoomboxItemPatch
    {
        
        internal static readonly Dictionary<BoomboxItem, bool> DirtyStatus = [];
        
        /// <summary>
        ///  Sync on Creation
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoomboxItem),nameof(BoomboxItem.Start))]
        private static void OnStart(BoomboxItem __instance)
        {
            if (ShotgunNetworking.Instance == null || !ShotgunNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsServer)
            {
                ShotgunNetworking.Instance.RequestSyncServerRpc(__instance.NetworkObject);
            }
        }
        
        /// <summary>
        ///  broadcast the new ammo count after a reload animation.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(BoomboxItem),nameof(BoomboxItem.StartMusic))]
        private static void OnMusicChange(BoomboxItem __instance)
        {
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;

            if (!__instance.IsOwner)
                return;

            DirtyStatus[__instance] = true;
        }
        
                        
                
        /// <summary>
        ///  broadcast changed data.
        /// </summary>>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(BoomboxItem),nameof(BoomboxItem.LateUpdate))]
        private static void OnLateUpdate(BoomboxItem __instance)
        {
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;
            
            if (DirtyStatus.TryGetValue(__instance, out var value) && value)
            {
                DirtyStatus[__instance] = false;
                
                if (__instance.IsOwner)
                {
                    var track = Array.IndexOf(__instance.musicAudios, __instance.boomboxAudio.clip);
                    var state = __instance.isPlayingMusic;
                    BoomboxNetworking.Instance.SyncStateServerRpc(__instance.NetworkObject, state, track);
                }
            }
            
        }
        
        /// <summary>
        ///  clear entries on Destroy.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(BoomboxItem),nameof(BoomboxItem.OnDestroy))]
        private static void OnDestroy(BoomboxItem __instance)
        {
            DirtyStatus.Remove(__instance);
        }
        
    }
}