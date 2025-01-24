using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MethodAttributes = System.Reflection.MethodAttributes;

namespace HarmonyXInterop;

internal static class WrapperBuilder
{
    private static readonly Type s_boolRefType = typeof(bool).MakeByRefType();
    internal static MethodInfo CreatePrefixWrapper(MethodInfo originalMethod)
    {
        var originalType = originalMethod.DeclaringType;
        var originalParameters = originalMethod.GetParameters();
        var dm = new DynamicMethod(
            originalMethod.Name,
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            originalMethod.ReturnType,
            originalParameters.Select(pi => pi.ParameterType).Append(s_boolRefType).ToArray(),
            originalType, // make sure that DeclaringType is same as original, to properly work with __state
            true
        );
        if (!originalType.IsClass)
        {
            throw new ArgumentException("Type is not a class");
        }

        for (var index = 0; index < originalParameters.Length; index++)
        {
            var originalParameterInfo = originalParameters[index];
            var parameterBuilder = dm.DefineParameter(1 + index, originalParameterInfo.Attributes, originalParameterInfo.Name);
            if (originalParameterInfo.DefaultValue != DBNull.Value)
            {
                // setting the default value is not really necessary, but it should keep up the illusion of being the original method
                parameterBuilder.SetConstant(originalParameterInfo.DefaultValue);
            }
            foreach (var customBuilder in originalParameterInfo.GetCustomAttributesData().Select(ToCustomAttributeBuilder))
            {
                parameterBuilder.SetCustomAttribute(customBuilder);
            }
        }

        var runOriginalIndex = originalParameters.Length;
        dm.DefineParameter(1 + runOriginalIndex, 0, "__runOriginal");

        AddIL(dm, originalMethod, runOriginalIndex);
        return dm;
    }
    private static CustomAttributeBuilder ToCustomAttributeBuilder(CustomAttributeData data)
    {
        var attributeArgs = data.ConstructorArguments.Select(a => a.Value).ToArray();

        var propertyArgs = data.NamedArguments.Where(i => i.MemberInfo is PropertyInfo);
        var propertyInfos = propertyArgs.Select(a => (PropertyInfo)a.MemberInfo).ToArray();
        var propertyValues = propertyArgs.Select(a => a.TypedValue.Value).ToArray();

        var fieldArgs = data.NamedArguments.Where(i => i.MemberInfo is FieldInfo);
        var namedFieldInfos = fieldArgs.Select(a => (FieldInfo)a.MemberInfo).ToArray();
        var namedFieldValues = fieldArgs.Select(a => a.TypedValue.Value).ToArray();

        return new CustomAttributeBuilder(data.Constructor, attributeArgs, propertyInfos, propertyValues, namedFieldInfos, namedFieldValues);
    }

    private static void AddIL(DynamicMethod dm, MethodInfo originalMethod, int runOriginalIndex)
    {
        var il = dm.GetILGenerator();
        var labelToReturn = il.DefineLabel();

        if (originalMethod.ReturnType == typeof(bool))
        {
            il.Emit(OpCodes.Ldarg, runOriginalIndex);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Brfalse_S, labelToReturn);
            il.Emit(OpCodes.Ldarg, runOriginalIndex);
            for (var i = 0; i < runOriginalIndex; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Call, originalMethod);
            il.Emit(OpCodes.Stind_I1);
            il.MarkLabel(labelToReturn);
            il.Emit(OpCodes.Ldarg, runOriginalIndex);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Ret);
        }
        else
        {
            il.Emit(OpCodes.Ldarg, runOriginalIndex);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Brfalse_S, labelToReturn);
            for (var i = 0; i < runOriginalIndex; i++)
            {
                il.Emit(OpCodes.Ldarg, i);
            }
            il.Emit(OpCodes.Call, originalMethod);
            il.MarkLabel(labelToReturn);
            il.Emit(OpCodes.Ret);
        }
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
            OriginalWithoutReturn(a, ref b);
        }
    }
    private static bool PrefixOriginalWithReturn(int a, ref long b)
    {
        return true;
    }
    private static void OriginalWithoutReturn(int a, ref long b)
    {
    }
}