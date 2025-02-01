using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Mono.Cecil;

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
  - if a mod wants to inject into another mod -> just PR the damn thing!
  - only valid use-case are struct-based fields. those can be more efficient performance wise
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
        var prefixLength =
            Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(baseDirectory))))!
                .Length + 1;
        var files = Directory.GetFiles(baseDirectory, "ModTekSimpleInjector.*.xml");
        Array.Sort(files);
        var serializer = new XmlSerializer(typeof(Additions));
        // find a valid class name character in front of "<" and assume its part of the ofType expression
        // in XML < is invalid in attribute values, Rider does not care though
        var greaterThanFix = new Regex(@"(?<=[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}])<");
        foreach (var file in files)
        {
            Console.WriteLine($"Processing additions in file `{file[prefixLength..]}`");
            var xml = File.ReadAllText(file);
            var sanitized = greaterThanFix.Replace(xml, "&lt;");
            using var reader = new StringReader(sanitized);
            var additions = (Additions)serializer.Deserialize(reader);
            var fileName = Path.GetFileName(file);
            ProcessAdditions(fileName, resolver, additions);
        }
    }

    private static void ProcessAdditions(string sourceFile, IAssemblyResolver resolver, Additions additions)
    {
        if (additions.AddField is { Length: > 0 })
        {
            foreach (var addition in additions.AddField)
            {
                if (addition.Name.StartsWith("example"))
                {
                    continue;
                }
                var injection = new SimpleInjection(sourceFile, resolver, addition);
                injection.Inject(addition);
            }
        }

        if (additions.AddEnumConstant is { Length: > 0 })
        {
            foreach (var addition in additions.AddEnumConstant)
            {
                if (addition.Name.StartsWith("example"))
                {
                    continue;
                }
                var injection = new SimpleInjection(sourceFile, resolver, addition);
                injection.Inject(addition);
            }
        }
    }
}
