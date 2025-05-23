﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ModTekSimpleInjector;

internal class SimpleInjection
{
    private readonly ModuleDefinition[] _resolveInModules;
    private readonly ModuleDefinition _moduleDefinition;
    private readonly CustomAttribute _customAttribute;
    private readonly TypeDefinition _typeDefinition;
    public SimpleInjection(
        string sourceFile,
        IAssemblyResolver resolver,
        MemberAddition addition
    ) {
        Console.WriteLine($"Processing {addition}");
        var assemblyName = new AssemblyNameReference(addition.InAssembly, null);

        var assemblyDefinition = resolver.Resolve(assemblyName, new ReaderParameters { ReadWrite = true })
            ?? throw new ArgumentException($"Unable to resolve assembly {addition.InAssembly}");
        _moduleDefinition = assemblyDefinition.MainModule;

        var modules = new List<ModuleDefinition>();
        modules.Add(_moduleDefinition);
        foreach (var assemblyReference in _moduleDefinition.AssemblyReferences)
        {
            // workaround for Mono.Cecil adding System.Private.CoreLib to assembly references when on msbuild task
            if (assemblyReference.Name == "System.Private.CoreLib")
            {
                continue;
            }
            var module = resolver.Resolve(assemblyReference).MainModule;
            modules.Add(module);
        }
        modules.Add(resolver.Resolve(new AssemblyNameReference("mscorlib", null)).MainModule);
        modules.Add(resolver.Resolve(new AssemblyNameReference("System", null)).MainModule);
        modules.Add(resolver.Resolve(new AssemblyNameReference("System.Core", null)).MainModule);
        _resolveInModules = modules.ToArray();

        _customAttribute = CreateCustomAttribute(_moduleDefinition,"ModTekSimpleInjector", "InjectedAttribute", [
            new ParameterInfo("source", sourceFile),
            new ParameterInfo("comment", addition.Comment)
        ]);

        _typeDefinition = _moduleDefinition.GetType(addition.ToType)
            ?? throw new ArgumentException($"Unable to resolve type {addition.ToType} in assembly {addition.InAssembly}");
    }

    internal void Inject(AddField fieldAddition)
    {
        var typeReference = ResolveType(fieldAddition.OfType);
        var typeReferenceImported = _moduleDefinition.ImportReference(typeReference);
        var fieldDefinition = new FieldDefinition(fieldAddition.Name, fieldAddition.Attributes, typeReferenceImported);
        fieldDefinition.CustomAttributes.Add(_customAttribute);
        _typeDefinition.Fields.Add(fieldDefinition);
    }

    internal void Inject(AddEnumConstant enumConstant)
    {
        const FieldAttributes EnumFieldAttributes =
            FieldAttributes.Static
            | FieldAttributes.Literal
            | FieldAttributes.Public
            | FieldAttributes.HasDefault;
        var fieldDefinition = new FieldDefinition(enumConstant.Name, EnumFieldAttributes, _typeDefinition)
        {
            Constant = enumConstant.Value
        };
        fieldDefinition.CustomAttributes.Add(_customAttribute);
        _typeDefinition.Fields.Add(fieldDefinition);
    }

    private static CustomAttribute CreateCustomAttribute(
        ModuleDefinition moduleDefinition,
        string @namespace,
        string name,
        ParameterInfo[] parameters
    ) {
        var attributeTypeDefinition = moduleDefinition.GetType(@namespace, name);
        if (attributeTypeDefinition == null)
        {
            var baseTypeReference = moduleDefinition.TypeSystem.LookupType("System", "Attribute");
            attributeTypeDefinition = new TypeDefinition(
                @namespace,
                name,
                TypeAttributes.NotPublic | TypeAttributes.Sealed,
                baseTypeReference
            );

            const MethodAttributes CtorAttributes =
                MethodAttributes.Assembly
                | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName;
            var methodDefinition = new MethodDefinition(".ctor", CtorAttributes, moduleDefinition.TypeSystem.Void);
            { // calling the base constructor does not seem required, better safe than sorry though
                var baseConstructorMethodReference = new MethodReference(".ctor", moduleDefinition.TypeSystem.Void, baseTypeReference);
                var methodBody = methodDefinition.Body = new MethodBody(methodDefinition);
                methodBody.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                methodBody.Instructions.Add(Instruction.Create(OpCodes.Call, baseConstructorMethodReference));
                methodBody.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            attributeTypeDefinition.Methods.Add(methodDefinition);

            foreach (var parameter in parameters)
            {
                var attributeFieldDefinition = new FieldDefinition(
                    parameter.Name,
                    FieldAttributes.Assembly,
                    moduleDefinition.TypeSystem.String
                );
                attributeTypeDefinition.Fields.Add(attributeFieldDefinition);
            }
            moduleDefinition.Types.Add(attributeTypeDefinition);
        }

        var attributeConstructor = attributeTypeDefinition.GetConstructors().Single();
        var attribute = new CustomAttribute(attributeConstructor);
        foreach (var parameter in parameters)
        {
            if (parameter.Value is null)
            {
                continue;
            }
            var attributeArgument = new CustomAttributeArgument(
                moduleDefinition.TypeSystem.String,
                parameter.Value
            );
            var attributeNamedArgument = new CustomAttributeNamedArgument(parameter.Name, attributeArgument);
            attribute.Fields.Add(attributeNamedArgument);
        }
        return attribute;
    }
    private record ParameterInfo(string Name, string Value);

    private TypeReference ResolveType(string typeName)
    {
        var isArray = typeName.EndsWith("[]");
        if (isArray)
        {
            typeName = typeName[..^2];
        }

        var genericArgumentsRegex = new Regex("^(.+?)<(.+)>$");
        var genericArgumentsMatch = genericArgumentsRegex.Match(typeName);
        TypeReference[] genericArgumentsTypes;
        if (genericArgumentsMatch.Success)
        {
            var genericArgumentsString = genericArgumentsMatch.Groups[2].Value;
            var genericArgumentsStrings = genericArgumentsString.Split(',');
            typeName = genericArgumentsMatch.Groups[1].Value;
            typeName += "`" + genericArgumentsStrings.Length;
            genericArgumentsTypes = new TypeReference[genericArgumentsStrings.Length];
            for (var i = 0; i < genericArgumentsTypes.Length; i++)
            {
                genericArgumentsTypes[i] = ResolveType(genericArgumentsStrings[i]);
            }
        }
        else
        {
            genericArgumentsTypes = null;
        }

        TypeReference typeReference = _resolveInModules
            .Select(m => m.GetType(typeName))
            .FirstOrDefault(t => t !=null);
        if (typeReference == null)
        {
            throw new ArgumentException($"Unable to resolve type {typeName}");
        }

        if (genericArgumentsTypes != null)
        {
            typeReference = typeReference.MakeGenericInstanceType(genericArgumentsTypes);
        }
        if (isArray)
        {
            typeReference = typeReference.MakeArrayType();
        }
        return typeReference;
    }
}