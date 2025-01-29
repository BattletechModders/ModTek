using System;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace ModTekSimpleInjector;

internal class SimpleInjection
{
    private readonly ModuleDefinition[] _coreModules;
    private readonly ModuleDefinition _moduleDefinition;
    private readonly TypeDefinition _typeDefinition;
    private readonly CustomAttribute _customAttribute;

    public SimpleInjection(
        string sourceFile,
        IAssemblyResolver resolver,
        Addition addition
    ) {
        _coreModules = [
            resolver.Resolve(new AssemblyNameReference("mscorlib", null)).MainModule,
            resolver.Resolve(new AssemblyNameReference("System", null)).MainModule,
            resolver.Resolve(new AssemblyNameReference("System.Core", null)).MainModule,
        ];

        Console.WriteLine($"Processing {addition}");
        var assemblyName = new AssemblyNameReference(addition.InAssembly, null);

        var assemblyDefinition = resolver.Resolve(assemblyName)
            ?? throw new ArgumentException($"Unable to resolve assembly {addition.InAssembly}");
        _typeDefinition = assemblyDefinition.MainModule.GetType(addition.ToType)
            ?? throw new ArgumentException($"Unable to resolve type {addition.ToType} in assembly {addition.InAssembly}");
        _moduleDefinition = assemblyDefinition.MainModule;

        _customAttribute = CreateCustomAttribute(_moduleDefinition,"ModTekSimpleInjector", "InjectedAttribute", [
            new ParameterInfo<string>("source", sourceFile),
            new ParameterInfo<string>("comment", addition.Comment)
        ]);
    }

    internal void InjectField(AddField fieldAddition)
    {
        var fieldType = ResolveType(fieldAddition.OfType);
        var fieldTypeReference = _moduleDefinition.ImportReference(fieldType);
        var fieldDefinition = new FieldDefinition(fieldAddition.Name, fieldAddition.Attributes, fieldTypeReference);
        fieldDefinition.CustomAttributes.Add(_customAttribute);
        _typeDefinition.Fields.Add(fieldDefinition);
    }

    internal void InjectEnumConstant(AddEnumConstant enumConstant)
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
        IParameterInfo[] parameters
    ) {
        var attributeTypeDefinition = moduleDefinition.GetType(@namespace, name);
        if (attributeTypeDefinition == null)
        {
            var baseType = moduleDefinition.ImportReference(typeof(Attribute));
            attributeTypeDefinition = new TypeDefinition(
                @namespace,
                name,
                TypeAttributes.NotPublic | TypeAttributes.Sealed,
                baseType
            );
            const MethodAttributes CtorAttributes =
                MethodAttributes.Public
                | MethodAttributes.HideBySig
                | MethodAttributes.SpecialName
                | MethodAttributes.RTSpecialName;
            var methodDefinition = new MethodDefinition(".ctor", CtorAttributes, moduleDefinition.TypeSystem.Void);
            // if not wanting CustomAttributeNamedArgument, one can use:
            // foreach (var parameter in parameters)
            // {
            //     var parameterDefinition = new ParameterDefinition(
            //         parameter.name,
            //         ParameterAttributes.None,
            //         moduleDefinition.ImportReference(parameter.type)
            //     );
            //     methodDefinition.Parameters.Add(parameterDefinition);
            // }
            attributeTypeDefinition.Methods.Add(methodDefinition);
            moduleDefinition.Types.Add(attributeTypeDefinition);
        }
        var attributeConstructor = attributeTypeDefinition.GetConstructors().First();
        var attribute = new CustomAttribute(attributeConstructor);
        foreach (var parameter in parameters)
        {
            if (parameter.Value is null)
            {
                continue;
            }
            var attributeArgument = new CustomAttributeArgument(
                moduleDefinition.ImportReference(parameter.Type),
                parameter.Value
            );
            var attributeNamedArgument = new CustomAttributeNamedArgument(parameter.Name, attributeArgument);
            attribute.Fields.Add(attributeNamedArgument);
        }
        return attribute;
    }
    private record ParameterInfo<T>(string Name, T value) : IParameterInfo
    {
        public Type Type => typeof(T);
        public object Value => value;
    }
    private interface IParameterInfo
    {
        string Name { get; }
        object Value { get; }
        Type Type { get; }
    }

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

        TypeReference typeReference = _coreModules.Select(m => m.GetType(typeName)).FirstOrDefault();
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