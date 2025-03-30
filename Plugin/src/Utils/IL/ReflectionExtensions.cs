using System;
using System.Reflection;

namespace AdditionalNetworking.Utils.IL;

public static class ReflectionExtensions
{
    public static MethodInfo GetGenericMethod(this Type type, string name, Type[] parameters, Type[] genericArgs)
    {
        var methods = type.GetMethods();
        foreach (var method in methods)
        {
            if (method.Name != name)
                continue;
            if (!method.IsGenericMethodDefinition)
                continue;

            MethodInfo specializedMethod;
            try
            {
                specializedMethod = method.MakeGenericMethod(genericArgs);
            }
            catch (ArgumentException)
            {
                continue;
            }

            var candidateParameters = specializedMethod.GetParameters();
            if (parameters.Length != candidateParameters.Length)
                continue;
            var parametersEqual = true;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] != candidateParameters[i].ParameterType)
                {
                    parametersEqual = false;
                    break;
                }
            }

            if (!parametersEqual)
                continue;

            return specializedMethod;
        }

        return null;
    }
}