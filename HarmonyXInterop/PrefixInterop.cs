using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using HarmonyLib.Tools;
using MonoMod.Utils;

namespace HarmonyXInterop;

internal static class PrefixInterop
{
    private static readonly Dictionary<MethodInfo, MethodInfo> Wrappers = new();
    private static readonly MethodInfo LogTextMethod = AccessTools.Method(typeof(Logger), "LogText");

    private static void LogText(Logger.LogChannel channel, string message)
    {
        LogTextMethod.Invoke(null, new object[] { channel, message, false });
    }

    public static MethodInfo WrapInterop(MethodInfo original)
    {
        try
        {
            // due to PrefixWrapper, Harmony can't pass __state since HarmonyX passes state via DeclaredType.FullName
            if (original.GetParameters().Any(x => x.Name == "__state"))
            {
                LogText(Logger.LogChannel.Warn, $"Prefix contains __state, Harmony 1 prefix skip behavior is not possible for {original.GetID(simple: true)}");
                return original;
            }

            lock (Wrappers)
            {
                if (!Wrappers.TryGetValue(original, out var wrapper))
                {
                    wrapper = CreatePrefixWrapper(original);
                    Wrappers[original] = wrapper;
                }
                return wrapper;
            }
        }
        catch (Exception e)
        {
            LogText(Logger.LogChannel.Error, $"Error creating prefix wrapper: {e}");
        }
        return original;
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

        var canSkipPrefixes = original.ReturnType == typeof(bool);

        using var dmd = new DynamicMethodDefinition(
            $"PrefixWrapper<{original.GetID(simple: true)}>",
            canSkipPrefixes ? typeof(bool) : null,
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
        var labelToReturn = il.DefineLabel();

        if (canSkipPrefixes)
        {
            il.Emit(OpCodes.Ldarg, wrapperArgumentsRunOriginalIndex);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Brfalse_S, labelToReturn);
            il.Emit(OpCodes.Ldarg, wrapperArgumentsRunOriginalIndex);
            for (var i = 0; i < wrapperArgumentsRunOriginalIndex; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Call, original);
            il.Emit(OpCodes.Stind_I1);
            il.MarkLabel(labelToReturn);
            il.Emit(OpCodes.Ldarg, wrapperArgumentsRunOriginalIndex);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            il.Emit(OpCodes.Ldarg, wrapperArgumentsRunOriginalIndex);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Brfalse_S, labelToReturn);
            for (var i = 0; i < wrapperArgumentsRunOriginalIndex; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Call, original);
            il.MarkLabel(labelToReturn);
            il.Emit(OpCodes.Ret);
        }

        return dmd.GenerateWith<DMDCecilGenerator>();
    }

    // use IL viewer on the methods below to figure out the IL used above

    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    private static bool PrefixWrapperWithReturn(int a, ref long b, ref bool __runOriginal)
    {
        if (__runOriginal)
        {
            // Harmony 2 claims its readonly, HarmonyX claims it can be set
            __runOriginal = PrefixOriginalWithReturn(a, ref b);
        }
        return __runOriginal;
    }

    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once InconsistentNaming
    private static void PrefixWrapperWithoutReturn(int a, ref long b, ref bool __runOriginal)
    {
        if (__runOriginal)
        {
            PrefixOriginalWithoutReturn(a, ref b);
        }
    }

    private static bool PrefixOriginalWithReturn(int a, ref long b)
    {
        return true;
    }

    private static void PrefixOriginalWithoutReturn(int a, ref long b)
    {
    }
}