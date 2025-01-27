using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Mono.Cecil;
using FieldAttributes = Mono.Cecil.FieldAttributes;

namespace ModTekSimpleInjector;

/*
What is hard and therefore not supported:
- custom attributes
  - e.g. [JsonIgnored]
  - mainly due to constructor arguments being hard [SerializableMember(SerializationTarget.SaveGame)]
- custom types introduced by mods
  - injectors run before mods are loaded. if a modded type is added, but the mod is not even loaded.. what now?
  - also how does the injector know where to find the types? -> mod dlls are unknown during injector time
  - some kind of special assembly type with "injectable" types would be a solution, requires some ahead of type loading
- modifying existing fields
  - visibility should not be modified during runtime since it can crash (subclassing), and compile time via publicizer is good enough
  - changing static, const would also crash stuff
  - changing type -> contra/covariance issues
  - custom attributes -> might be interesting but highly situational.
- adding properties
  - this is more complicate due to compiler backed fields etc.. just use extension methods and "Unsafe.As"
- adding methods
  - a mod can just use static methods and use the first argument with (this ok ...) and it will look like instance methods
*/
internal static class Injector
{
    public static void Inject(IAssemblyResolver resolver)
    {
        var baseDirectory = Path.GetDirectoryName(typeof(Injector).Assembly.Location);
        var files = Directory.GetFiles(baseDirectory, "ModTekSimpleInjector.*.xml");
        Array.Sort(files);
        var serializer = new XmlSerializer(typeof(Additions));
        // find a valid class name character in front of "<" and assume its part of the ofType expression
        // in XML < is invalid in attribute values, Rider does not care though
        var greaterThanFix = new Regex(@"(?<=[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}])<");
        foreach (var file in files)
        {
            Console.WriteLine($"Processing additions in file {file}");
            var xml = File.ReadAllText(file);
            var sanitized = greaterThanFix.Replace(xml, "&lt;");
            using var reader = new StringReader(sanitized);
            var additions = (Additions)serializer.Deserialize(reader);
            ProcessAdditions(resolver, additions);
        }
    }

    private static void ProcessAdditions(IAssemblyResolver resolver, Additions additions)
    {
        foreach (var addition in additions.AddField)
        {
            if (addition.Name.StartsWith("example"))
            {
                continue;
            }
            Console.WriteLine($"Processing {addition}");
            ResolveAssemblyAndType(resolver, addition, out var assemblyDefinition, out var typeDefinition);
            ProcessAddField(assemblyDefinition, typeDefinition, addition);
        }
        foreach (var addition in additions.AddEnumConstant)
        {
            if (addition.Name.StartsWith("example"))
            {
                continue;
            }
            Console.WriteLine($"Processing {addition}");
            ResolveAssemblyAndType(resolver, addition, out _, out var typeDefinition);
            ProcessAddEnumConstant(typeDefinition, addition);
        }
    }

    private static void ResolveAssemblyAndType(
        IAssemblyResolver resolver,
        Addition addition,
        out AssemblyDefinition assemblyDefinition,
        out TypeDefinition typeDefinition
    ) {
        var assemblyName = new AssemblyNameReference(addition.InAssembly, null);
        assemblyDefinition = resolver.Resolve(assemblyName);
        if (assemblyDefinition == null)
        {
            throw new ArgumentException($"Unable to resolve assembly {addition.InAssembly}");
        }
        typeDefinition = assemblyDefinition.MainModule.GetType(addition.ToType);
        if (typeDefinition == null)
        {
            throw new ArgumentException($"Unable to resolve type {addition.ToType} in assembly {addition.InAssembly}");
        }
    }

    private static void ProcessAddField(AssemblyDefinition assembly, TypeDefinition type, AddField fieldAddition)
    {
        var fieldType = ResolveType(fieldAddition.OfType);
        var fieldTypeReference = assembly.MainModule.ImportReference(fieldType);
        var field = new FieldDefinition(fieldAddition.Name, fieldAddition.Attributes, fieldTypeReference);
        type.Fields.Add(field);
    }

    private static void ProcessAddEnumConstant(TypeDefinition type, AddEnumConstant enumConstant)
    {
        const FieldAttributes EnumFieldAttributes =
            FieldAttributes.Static
            | FieldAttributes.Literal
            | FieldAttributes.Public
            | FieldAttributes.HasDefault;
        var constantValue = enumConstant.Value;
        var field = new FieldDefinition(enumConstant.Name, EnumFieldAttributes, type)
        {
            Constant = constantValue
        };
        type.Fields.Add(field);
    }

    private static Type ResolveType(string typeName)
    {
        var isArray = typeName.EndsWith("[]");
        if (isArray)
        {
            typeName = typeName[..^2];
        }

        var genericArgumentsRegex = new Regex("^(.+?)<(.+)>$");
        var genericArgumentsMatch = genericArgumentsRegex.Match(typeName);
        Type[] genericArgumentsTypes;
        if (genericArgumentsMatch.Success)
        {
            var genericArgumentsString = genericArgumentsMatch.Groups[2].Value;
            var genericArgumentsStrings = genericArgumentsString.Split(',');
            typeName = genericArgumentsMatch.Groups[1].Value;
            typeName += "`" + genericArgumentsStrings.Length;
            genericArgumentsTypes = new Type[genericArgumentsStrings.Length];
            for (var i = 0; i < genericArgumentsTypes.Length; i++)
            {
                genericArgumentsTypes[i] = ResolveType(genericArgumentsStrings[i]);
            }
        }
        else
        {
            genericArgumentsTypes = null;
        }

        // we only support mscorlib classes for now
        var fieldType = typeof(int).Assembly.GetType(typeName);

        if (genericArgumentsTypes != null)
        {
            fieldType = fieldType.MakeGenericType(genericArgumentsTypes);
        }
        if (isArray)
        {
            fieldType = fieldType.MakeArrayType();
        }
        return fieldType;
    }
}

// XmlSerializer requires the following classes to be public

[XmlRoot(ElementName = "ModTekSimpleInjector")]
public class Additions
{
    [XmlElement(ElementName = "AddField")]
    public AddField[] AddField = [];
    [XmlElement(ElementName = "AddEnumConstant")]
    public AddEnumConstant[] AddEnumConstant = [];
}

public abstract class Addition
{
    [XmlAttribute("InAssembly")]
    public string InAssembly;
    [XmlAttribute("ToType")]
    public string ToType;

    public override string ToString()
    {
        return $"{this.GetType().Name}:{InAssembly}:{ToType}";
    }
}

[XmlType("AddField")]
[XmlRoot(ElementName = "AddField")]
public class AddField : Addition
{
    [XmlAttribute("Name")]
    public string Name;
    [XmlAttribute("OfType")]
    public string OfType;
    [XmlAttribute("Attributes")]
    public FieldAttributes Attributes = FieldAttributes.Private;

    public override string ToString()
    {
        return $"{base.ToString()}:{Name}:{OfType}:{Attributes}";
    }
}

[XmlType("AddEnumConstant")]
[XmlRoot(ElementName = "AddEnumConstant")]
public class AddEnumConstant : Addition
{
    [XmlAttribute("Name")]
    public string Name;
    [XmlAttribute("Value")]
    public int Value;

    public override string ToString()
    {
        return $"{base.ToString()}:{Name}:{Value}";
    }
}
