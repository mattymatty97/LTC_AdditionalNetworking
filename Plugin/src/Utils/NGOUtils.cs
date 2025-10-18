using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using Unity.Netcode;
using UnityEngine;

namespace AdditionalNetworking.Utils;

public static class NgoUtils
{
    private static readonly MethodInfo BeginSendClientRpc =
        AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendClientRpc));

    private static readonly MethodInfo BeginSendServerRpc =
        AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendServerRpc));

    private static readonly NetworkBehaviour.__RpcExecStage ClientRpcStage = Application.unityVersion switch
    {
        "2022.3.9f1" => (NetworkBehaviour.__RpcExecStage)2,
        _ => NetworkBehaviour.__RpcExecStage.Execute
    };

    private static readonly NetworkBehaviour.__RpcExecStage ServerRpcStage = Application.unityVersion switch
    {
        "2022.3.9f1" => (NetworkBehaviour.__RpcExecStage)1,
        _ => NetworkBehaviour.__RpcExecStage.Execute
    };

    public static bool IsRPCClientStage(this NetworkBehaviour self)
    {
        var networkManager = self.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return false;

        if (self.__rpc_exec_stage != ClientRpcStage || (!networkManager.IsClient && !networkManager.IsHost))
            return false;

        return true;
    }

    public static bool IsRPCServerStage(this NetworkBehaviour self)
    {
        var networkManager = self.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return false;

        if (self.__rpc_exec_stage != ServerRpcStage || (!networkManager.IsServer && !networkManager.IsHost))
            return false;

        return true;
    }

    internal static bool TryGetRpcID(this MethodInfo methodInfo, out uint rpcID)
    {
        var instructions = methodInfo.GetMethodPatcher().CopyOriginal().Definition.Body.Instructions;

        rpcID = 0;
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == OpCodes.Ldc_I4 && instructions[i - 1].OpCode == OpCodes.Ldarg_0)
                rpcID = (uint)(int)instructions[i].Operand;

            if (instructions[i].OpCode != OpCodes.Call ||
                instructions[i].Operand is not MethodReference operand ||
                !(operand.Is(BeginSendClientRpc) || operand.Is(BeginSendServerRpc)))
                continue;

            AdditionalNetworking.Log.LogDebug($"Rpc Id found for {methodInfo.Name}: {rpcID}U");
            return true;
        }

        AdditionalNetworking.Log.LogFatal($"Cannot find Rpc ID for {methodInfo.Name}");
        return false;
    }
}