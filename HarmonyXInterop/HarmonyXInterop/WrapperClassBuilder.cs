using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using MonoMod.Utils;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;
using TypeAttributes = System.Reflection.TypeAttributes;

namespace HarmonyXInterop;

// assemblies created through here are in theory garbage collectible
//  to make it gc, requires changes in SetMonoCorlibInternal and PrefixInterop.Wrappers
//  chances are that with SetMonoCorlibInternal or just mono it might not work anyway
internal class WrapperClassBuilder
{
    internal static MethodInfo CreatePrefixWrapper(MethodInfo originalMethod)
    {
        var dynClass = new WrapperClassBuilder(originalMethod.DeclaringType);
        dynClass.AddMethod(originalMethod);
        var type = dynClass.Build();
        var method = type.GetMethod(originalMethod.Name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
        return method;
    }

    private static readonly ConstructorInfo UnverifiableCodeAttributeConstructorInfo = typeof(UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes);
    private static readonly ConstructorInfo IgnoresAccessChecksToAttributeConstructorInfo = typeof (IgnoresAccessChecksToAttribute).GetConstructor(new[] { typeof (string) });
    private static readonly Type BoolRefType = typeof(bool).MakeByRefType();
    private static long uniqueCounter;

    private readonly TypeBuilder _type;
    private WrapperClassBuilder(Type originalType)
    {
        if (!originalType.IsClass)
        {
            throw new ArgumentException("Type is not a class");
        }

        var assemblyName = originalType.Assembly.GetName().Name;
        string moduleName;
        {
            var counter = Interlocked.Increment(ref uniqueCounter);
            moduleName = $"HXI︳{counter}︳";
        }

        var assembly = AssemblyBuilder.DefineDynamicAssembly(new(moduleName), AssemblyBuilderAccess.RunAndCollect);
        var module = assembly.DefineDynamicModule(moduleName);

        assembly.SetCustomAttribute(new(UnverifiableCodeAttributeConstructorInfo, Array.Empty<object>()));
        assembly.SetCustomAttribute(new(IgnoresAccessChecksToAttributeConstructorInfo, new object[] { assemblyName} ));

        TypeBuilder typeBuilder = null;
        { // replicate nested structure as __state is based on method.DeclaredType.FullName
            var types = new List<Type>();
            {
                var candidate = originalType;
                while (true)
                {
                    if (candidate == null)
                    {
                        break;
                    }
                    types.Add(candidate);
                    if (!candidate.IsNested)
                    {
                        break;
                    }
                    candidate = candidate.DeclaringType;
                }
                types.Reverse();
            }

            foreach (var type in types)
            {
                if (typeBuilder == null)
                {
                    typeBuilder = module.DefineType(
                        type.FullName!,
                        TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed
                    );
                }
                else
                {
                    typeBuilder = typeBuilder.DefineNestedType(
                        type.Name,
                        TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed
                    );
                }
            }
        }

        _type = typeBuilder;
    }
    private void AddMethod(MethodInfo originalMethod)
    {
        var originalParameters = originalMethod.GetParameters();

        var methodBuilder = _type.DefineMethod(
            originalMethod.Name,
            MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard,
            originalMethod.ReturnType,
            originalParameters.Select(pi => pi.ParameterType).Append(BoolRefType).ToArray());

        methodBuilder.SetImplementationFlags(MethodImplAttributes.NoInlining);

        for (var index = 0; index < originalParameters.Length; index++)
        {
            var originalParameterInfo = originalParameters[index];
            var parameterBuilder = methodBuilder.DefineParameter(1 + index, originalParameterInfo.Attributes, originalParameterInfo.Name);
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
        methodBuilder.DefineParameter(1 + runOriginalIndex, 0, "__runOriginal");

        AddIL(methodBuilder, originalMethod, runOriginalIndex);
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
    private Type Build()
    {
        var type = _type.CreateType();
        type.Assembly.SetMonoCorlibInternal(true); // mono alternative for IgnoresAccessChecksToAttribute
        return type;
    }

    private static void AddIL(MethodBuilder methodBuilder, MethodInfo originalMethod, int runOriginalIndex)
    {
        var il = methodBuilder.GetILGenerator();
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