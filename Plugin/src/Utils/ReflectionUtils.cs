using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HarmonyLib.Public.Patching;
using Mono.Cecil;
using MonoMod.Utils;
using Unity.Netcode;
using UnityEngine.Bindings;
using CecilOpCodes = Mono.Cecil.Cil.OpCodes;

namespace AdditionalNetworking.Utils;

public static class ReflectionUtils
{
    private static readonly MethodInfo BeginSendClientRpc =
        AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendClientRpc));

    private static readonly MethodInfo BeginSendServerRpc =
        AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.__beginSendServerRpc));

    internal static bool TryGetRpcID([NotNull] this MethodInfo methodInfo, out uint rpcID)
    {
        _ = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
        var instructions = methodInfo.GetMethodPatcher().CopyOriginal().Definition.Body.Instructions;

        rpcID = 0;
        for (var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i].OpCode == CecilOpCodes.Ldc_I4 && instructions[i - 1].OpCode == CecilOpCodes.Ldarg_0)
                rpcID = (uint)(int)instructions[i].Operand;

            if (instructions[i].OpCode != CecilOpCodes.Call ||
                instructions[i].Operand is not MethodReference operand ||
                !(operand.Is(BeginSendClientRpc) || operand.Is(BeginSendServerRpc)))
                continue;

            AdditionalNetworking.Log.LogDebug($"Rpc Id found for {methodInfo.Name}: {rpcID}U");
            return true;
        }

        AdditionalNetworking.Log.LogFatal($"Cannot find Rpc ID for {methodInfo.Name}");
        return false;
    }

    internal static Delegate FastGetter([NotNull] this FieldInfo field)
    {
        _ = field ?? throw new ArgumentNullException(nameof(field));
        var methodName = field.ReflectedType!.FullName + ".get_" + field.Name;
        var setterMethod = new DynamicMethod(methodName, field.FieldType, [field.DeclaringType], true);
        var gen = setterMethod.GetILGenerator();
        if (field.IsStatic)
        {
            gen.Emit(OpCodes.Ldsfld, field);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, field);
        }

        gen.Emit(OpCodes.Ret);
        return setterMethod.CreateDelegate(Expression.GetFuncType(field.DeclaringType, field.FieldType));
    }

    internal static Delegate FastSetter([NotNull] this FieldInfo field)
    {
        _ = field ?? throw new ArgumentNullException(nameof(field));
        var methodName = field.ReflectedType!.FullName + ".get_" + field.Name;
        var setterMethod = new DynamicMethod(methodName, field.FieldType, [field.DeclaringType, field.FieldType], true);
        var gen = setterMethod.GetILGenerator();
        if (field.IsStatic)
        {
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stsfld, field);
        }
        else
        {
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Stfld, field);
        }

        gen.Emit(OpCodes.Ldarg_1);
        gen.Emit(OpCodes.Ret);
        return setterMethod.CreateDelegate(
            Expression.GetFuncType(field.DeclaringType, field.FieldType, field.FieldType));
    }
}