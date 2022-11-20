using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using MonoMod.Utils;

namespace HarmonyXInterop;

internal static class PrefixInterop
{
    private static readonly Dictionary<MethodInfo, MethodInfo> Wrappers = new();

    public static MethodInfo WrapInterop(MethodInfo method)
    {
        if (method.ReturnType != typeof(bool))
        {
            return method;
        }

        lock (Wrappers)
        {
            if (!Wrappers.TryGetValue(method, out var wrapper))
            {
                wrapper = CreatePrefixWrapper(method);
                Wrappers[method] = wrapper;
            }
            return wrapper;
        }
    }

    private static MethodInfo CreatePrefixWrapper(MethodInfo original)
    {
        Type[] wrapperArgumentsTypes;
        string[] wrapperArgumentsNames;
        int wrapperArgumentsRunOriginalIndex;
        {
            var originalParameters = original.GetParameters();
            var wrapperArgumentsLength = (original.IsStatic ? 0 : 1) + originalParameters.Length + 1;
            wrapperArgumentsTypes = new Type[wrapperArgumentsLength];
            wrapperArgumentsNames = new string[wrapperArgumentsLength];
            var wrapperArgumentsIndex = 0;

            if (!original.IsStatic)
            {
                wrapperArgumentsNames[wrapperArgumentsIndex] = "this";
                wrapperArgumentsTypes[wrapperArgumentsIndex] = original.GetThisParamType();
                wrapperArgumentsIndex++;
            }

            foreach (var parameter in originalParameters)
            {
                wrapperArgumentsNames[wrapperArgumentsIndex] = parameter.Name;
                wrapperArgumentsTypes[wrapperArgumentsIndex] = parameter.ParameterType;
                wrapperArgumentsIndex++;
            }

            wrapperArgumentsRunOriginalIndex = wrapperArgumentsIndex;
            wrapperArgumentsNames[wrapperArgumentsIndex] = "__runOriginal";
            wrapperArgumentsTypes[wrapperArgumentsIndex] = typeof(bool).MakeByRefType();
        }

        using var dmd = new DynamicMethodDefinition(
            $"PrefixWrapper<{original.GetID(simple: true)}>",
            original.ReturnType,
            wrapperArgumentsTypes
        );
        {
            var parameters = dmd.Definition.Parameters;
            for (var i = 0; i < wrapperArgumentsNames.Length; i++)
            {
                parameters[i].Name = wrapperArgumentsNames[i];
            }
        }

        var il = dmd.GetILGenerator();
        var labelToOriginalMethodCall = il.DefineLabel();

        il.Emit(OpCodes.Ldarg, wrapperArgumentsRunOriginalIndex);
        il.Emit(OpCodes.Ldind_U1);
        il.Emit(OpCodes.Brtrue_S, labelToOriginalMethodCall);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ret);

        il.MarkLabel(labelToOriginalMethodCall);
        for (var i = 0; i < wrapperArgumentsRunOriginalIndex; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
        }

        il.Emit(OpCodes.Call, original);
        il.Emit(OpCodes.Ret);

        return dmd.GenerateWith<DMDCecilGenerator>();
    }
}