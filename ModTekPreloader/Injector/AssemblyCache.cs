using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace ModTekPreloader.Injector
{
    internal class AssemblyCache : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyBag> assemblies = new Dictionary<string, AssemblyBag>();
        private readonly IAssemblyResolver resolver = new DefaultAssemblyResolver();

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters)
        {
            try
            {
                parameters.AssemblyResolver = parameters.AssemblyResolver ?? this;

                if (!assemblies.TryGetValue(reference.Name, out var assemblyBag))
                {
                    var assembly = resolver.Resolve(new AssemblyNameReference(reference.Name, null), parameters);
                    // Logger.Log($"assembly {assembly.Name.Name} {new AssemblySecurityPermission(assembly)}");
                    assemblyBag = new AssemblyBag(assembly);
                    assemblies[reference.Name] = assemblyBag;
                }

                return assemblyBag.Definition;
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }

            return null;
        }

        internal void SaveAssembliesToDisk()
        {
            foreach (var file in Directory.GetFiles(Paths.AssembliesInjectedDirectory))
            {
                File.Delete(file);
            }
            Logger.Log("Assemblies modified by injectors:");
            foreach (var kv in assemblies.OrderBy(kv => kv.Key))
            {
                var name = kv.Key;
                var assembly = kv.Value;
                if (!assembly.CheckIfChanged(out var serialized))
                {
                    continue;
                }
                var path = Path.Combine(Paths.AssembliesInjectedDirectory, $"{name}.dll");
                Logger.Log($"\t{Paths.GetRelativePath(path)}");
                File.WriteAllBytes(path, serialized);
            }
        }

        internal void SaveAssembliesPublicizedToDisk()
        {
            foreach (var file in Directory.GetFiles(Paths.AssembliesPublicizedDirectory))
            {
                File.Delete(file);
            }
            AssemblyPublicizer.MakePublic(resolver, Paths.AssembliesPublicizedDirectory);
        }

        public void Dispose()
        {
            resolver.Dispose();
            assemblies.Clear();
        }

        private class AssemblyBag
        {
            internal readonly AssemblyDefinition Definition;

            // skips change detection
            // performance improvements (1.4s)
            private static readonly string[] AlwaysChangedAssemblies =
            {
                "Assembly-CSharp"
            };

            // skips change detection
            // skips serialization
            // performance improvements (0.5s)
            private static readonly string[] NeverChangedAssemblies =
            {
                "mscorlib", "System", "System.Core"
            };

            private readonly bool AlwaysChanged;
            private readonly bool NeverChanged;
            private readonly byte[] Serialized;

            public AssemblyBag(AssemblyDefinition definition)
            {
                Definition = definition;
                AlwaysChanged = AlwaysChangedAssemblies.Contains(Name);
                NeverChanged = NeverChangedAssemblies.Contains(Name);
                Serialized = AlwaysChanged || NeverChanged ? null : Serialize(definition);
            }

            private string Name => Definition.Name.Name;

            internal bool CheckIfChanged(out byte[] serialized)
            {
                if (NeverChanged)
                {
                    serialized = null;
                    return false;
                }
                serialized = Serialize(Definition);
                if (AlwaysChanged)
                {
                    return true;
                }
                return !serialized.SequenceEqual(Serialized);
            }

            private static byte[] Serialize(AssemblyDefinition definition)
            {
                using (var stream = new MemoryStream())
                {
                    definition.Write(stream);
                    return stream.ToArray();
                }
            }
        }

        // SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)
        private class AssemblySecurityPermission
        {
            private readonly bool skipVerification;
            public AssemblySecurityPermission(AssemblyDefinition assembly)
            {
                skipVerification = assembly.SecurityDeclarations
                    .SelectMany(x => x.SecurityAttributes)
                    .SelectMany(x => x.Properties)
                    .Where(x => x.Name == "SkipVerification")
                    .Select(x => (bool)x.Argument.Value)
                    .FirstOrDefault();
            }
            public override string ToString()
            {
                return $"SkipVerification = {skipVerification})";
            }
        }
    }
}
