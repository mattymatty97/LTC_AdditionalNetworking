using System.Collections.Generic;
using AdditionalNetworking.Components;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace AdditionalNetworking.Patches
{
    [HarmonyPatch]
    internal class PlayerControllerBPatch
    {

        internal static readonly Dictionary<PlayerControllerB, bool> dirtySlots = [];
        
        /// <summary>
        ///  Request the username.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.Start))]
        private static void OnStart(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsServer)
                PlayerNetworking.Instance.RequestSyncUsernameServerRpc(__instance.NetworkObject);
        }
        
        /// <summary>
        ///  mark changed held slot.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.SwitchToItemSlot))]
        private static void OnSlotChange(PlayerControllerB __instance, int slot)
        {
            dirtySlots[__instance] = true;
        }
        
        /// <summary>
        ///  broadcast changed held slot.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.LateUpdate))]
        private static void BroadcastNewSlot(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
                return;
            
            if (dirtySlots.TryGetValue(__instance, out var value) && value)
            {
                dirtySlots[__instance] = false;
                
                if (__instance.IsOwner)
                {
                    PlayerNetworking.Instance.SyncSelectedSlotServerRpc(__instance.NetworkObject, __instance.currentItemSlot);
                }
            }
        }

        
        /// <summary>
        ///  broadcast new inventory status on item grab.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
        private static void OnItemGrabbed(PlayerControllerB __instance, bool grabValidated)
        {
            NetworkManager networkManager = __instance.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
                return;
            if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
                return;
            
            if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
                return;
            
            if (!grabValidated)
                return;
            
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.SyncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
            }
        }        
        
        /// <summary>
        ///  broadcast new inventory status on item discarded.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DiscardHeldObject))]
        private static void OnDiscardItem(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
                return;
            
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.SyncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
            }
        }        
        
        /// <summary>
        ///  broadcast new inventory status.
        /// </summary>
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.DropAllHeldItems))]
        private static void OnDropItem(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
                return;
            
            if (__instance.IsOwner)
            {
                List<NetworkObjectReference> networkObjects= new List<NetworkObjectReference>();
                List<int> slots = new List<int>();
                for (var i = 0; i < __instance.ItemSlots.Length; i++)
                {
                    var slot = __instance.ItemSlots[i];
                    if (slot != null)
                    {
                        networkObjects.Add(slot.NetworkObject);
                        slots.Add(i);
                    }
                }
                PlayerNetworking.Instance.SyncInventoryServerRpc(__instance.NetworkObject,networkObjects.ToArray(),slots.ToArray());
            }
        }
        
        /// <summary>
        ///  broadcast username change.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        private static void OnPlayerConnected(PlayerControllerB __instance)
        {
            if (PlayerNetworking.Instance == null || !PlayerNetworking.Instance.Enabled)
                return;
            
            if (!__instance.IsServer && __instance.IsOwner)
            {
                PlayerNetworking.Instance.SyncUsernameServerRpc(__instance.NetworkObject, __instance.playerUsername);
            }
        }
        
    }
}