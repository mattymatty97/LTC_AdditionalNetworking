using System;
using System.Collections.Generic;
using AdditionalNetworking.Components;
using HarmonyLib;
using Unity.Netcode;

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
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsServer)
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
        [HarmonyPatch(typeof(GrabbableObject),nameof(GrabbableObject.LateUpdate))]
        private static void OnLateUpdate(GrabbableObject __instance)
        {
            var boomboxItem = __instance as BoomboxItem;
            if (boomboxItem == null)
                return;
            if (BoomboxNetworking.Instance == null || !BoomboxNetworking.Instance.Enabled)
                return;
            
            if (DirtyStatus.TryGetValue(boomboxItem, out var value) && value)
            {
                DirtyStatus[boomboxItem] = false;
                
                if (__instance.IsOwner)
                {
                    var track = Array.IndexOf(boomboxItem.musicAudios, boomboxItem.boomboxAudio.clip);
                    var state = boomboxItem.isPlayingMusic;
                    BoomboxNetworking.Instance.SyncStateServerRpc(__instance.NetworkObject, state, track);
                }
            }
            
        }
        
        /// <summary>
        ///  clear entries on Destroy.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(NetworkBehaviour),nameof(NetworkBehaviour.OnDestroy))]
        private static void OnDestroy(NetworkBehaviour __instance)
        {
            var boombox = __instance as BoomboxItem;
            if (boombox == null)
                return;
            DirtyStatus.Remove(boombox);
        }
        
    }
}