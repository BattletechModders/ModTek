using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security;
using Mono.Cecil;
using MonoMod.Utils;
using MonoMod.Utils.Cil;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace HarmonyXInterop;

internal class WrapperClassBuilder : IDisposable
{
    internal static MethodInfo CreatePrefixWrapper(MethodInfo originalMethod)
    {
        using var dynClass = new WrapperClassBuilder(originalMethod.DeclaringType);
        var md = dynClass.AddMethod(originalMethod);
        return dynClass.Build().GetMethod(md.Name, BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private static readonly ConstructorInfo UnverifiableCodeAttributeConstructorInfo = typeof(UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes);
    private static readonly ConstructorInfo IgnoresAccessChecksToAttributeConstructorInfo = typeof (IgnoresAccessChecksToAttribute).GetConstructor(new[] { typeof (string) });
    private static readonly Type BoolRefType = typeof(bool).MakeByRefType();
    private static readonly Dictionary<string, int> UniqueCounter = new();

    private readonly ModuleDefinition _module;
    private readonly TypeDefinition _type;
    private WrapperClassBuilder(Type originalType)
    {
        if (!originalType.IsClass)
        {
            throw new ArgumentException("Type is not a class");
        }

        var assemblyName = originalType.Assembly.GetName().Name;
        string moduleName;
        {
            var uniqueKey = assemblyName;
            if (!UniqueCounter.TryGetValue(uniqueKey, out var counter))
            {
                counter = 0;
            }
            UniqueCounter[uniqueKey] = ++counter;
            moduleName = $"HXI︳{uniqueKey}︳{counter}";
        }

        _module = ModuleDefinition.CreateModule(
            moduleName,
            new ModuleParameters
            {
                Kind = ModuleKind.Dll,
                ReflectionImporterProvider = MMReflectionImporter.ProviderNoDefault
            }
        );

        _module.Assembly.CustomAttributes.Add(new(_module.ImportReference(UnverifiableCodeAttributeConstructorInfo)));
        _module.Assembly.CustomAttributes.Add(new(_module.ImportReference(IgnoresAccessChecksToAttributeConstructorInfo))
        {
            ConstructorArguments = {
                new CustomAttributeArgument(_module.ImportReference(typeof(DebuggableAttribute.DebuggingModes)), assemblyName)
            }
        });

        // original namespace + name required to keep __state working
        _type = new(
            originalType.Namespace,
            originalType.Name,
            Mono.Cecil.TypeAttributes.Public | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Sealed
        )
        {
            BaseType = _module.TypeSystem.Object
        };
        _module.Types.Add(_type);
    }
    private MethodDefinition AddMethod(MethodInfo originalMethod)
    {
        var originalMethodReference = _module.ImportReference(originalMethod);

        var methodDefinition = new MethodDefinition(
            originalMethodReference.Name,
            Mono.Cecil.MethodAttributes.Public | Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.HideBySig,
            _module.ImportReference(originalMethodReference.ReturnType)
        )
        {
            ImplAttributes = Mono.Cecil.MethodImplAttributes.IL | Mono.Cecil.MethodImplAttributes.Managed,
            DeclaringType = _type,
            NoInlining = true
        };

        foreach (var parameterDefinition in originalMethodReference.Parameters)
        {
            methodDefinition.Parameters.Add(
                new(
                    parameterDefinition.Name,
                    ParameterAttributes.None,
                    _module.ImportReference(parameterDefinition.ParameterType)
                )
            );
        }

        { // allow skipping
            methodDefinition.Parameters.Add(
                new(
                    "__runOriginal",
                    0,
                    _module.ImportReference(BoolRefType)
                )
            );
        }

        AddIL(methodDefinition, originalMethod);

        _type.Methods.Add(methodDefinition);

        return methodDefinition;
    }
    private Type Build()
    {
        // Directory.CreateDirectory("Mods/.modtek/dlls");
        // var invalidChars = Regex.Escape(new(Path.GetInvalidFileNameChars()));
        // var filename = Regex.Replace(_module.Name, $@"[{invalidChars}]+", "_");
        // _module.Write($"Mods/.modtek/dlls/{filename}.dll");

        var asm = ReflectionHelper.Load(_module);
        asm.SetMonoCorlibInternal(true);
        var type = asm.GetType(_type.FullName.Replace("+", "\\+"), false, false);
        return type;
    }

    public void Dispose()
    {
        _module?.Dispose();
    }

    private static void AddIL(MethodDefinition methodDefinition, MethodInfo originalMethod)
    {
        var il = new CecilILGenerator(methodDefinition.Body.GetILProcessor());
        var labelToReturn = il.DefineLabel();

        var runOriginalIndex = methodDefinition.Parameters.Count - 1;
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