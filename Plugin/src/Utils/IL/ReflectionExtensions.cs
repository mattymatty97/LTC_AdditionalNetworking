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
            return method.MakeGenericMethod(genericArgs);
        }

        return null;
    }
}