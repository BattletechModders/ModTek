using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTekPreloader.Logging;
using Mono.Cecil;

namespace ModTekPreloader.Injector
{
    internal class AssemblyCache : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyBag> assemblies = new Dictionary<string, AssemblyBag>();
        private readonly List<string> searchDirectories;

        internal AssemblyCache()
        {
            searchDirectories = new List<string>
            {
                Paths.AssembliesOverrideDirectory,
                Paths.ModTekDirectory,
                Paths.ManagedDirectory
            };
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters)
        {
            parameters.AssemblyResolver = parameters.AssemblyResolver ?? this;

            if (!assemblies.TryGetValue(reference.Name, out var assemblyBag))
            {
                var assembly = SearchAssembly(reference, parameters);
                assemblyBag = new AssemblyBag(assembly);
                assemblies[reference.Name] = assemblyBag;
            }

            return assemblyBag.Definition;
        }

        private AssemblyDefinition SearchAssembly(AssemblyNameReference reference, ReaderParameters parameters)
        {
            // TODO allow Harmony modifications
            if (reference.Name.StartsWith("OHarmony"))
            {
                if (reference.Name != "OHarmony")
                {
                    throw new NotSupportedException("Only 0Harmony is supported, no shims.");
                }
                var version = reference.Version;
                if (!(version.Major == 1 && version.Minor == 2))
                {
                    throw new NotSupportedException("Only 0Harmony 1.2 is supported for assembly definition loading.");
                }
            }
            var searchPattern = $"{reference.Name}.dll";
            foreach (var searchDirectory in searchDirectories)
            {
                foreach (var file in Directory.GetFiles(searchDirectory, searchPattern))
                {
                    return ModuleDefinition.ReadModule(file, parameters).Assembly;
                }
            }
            throw new AssemblyResolutionException(reference);
        }

        internal void SaveAssembliesToDisk()
        {
            Paths.SetupCleanDirectory(Paths.AssembliesInjectedDirectory);
            Logger.Log($"Assemblies modified by injectors and saved to `{Paths.GetRelativePath(Paths.AssembliesInjectedDirectory)}`:");
            foreach (var kv in assemblies.OrderBy(kv => kv.Key))
            {
                var name = kv.Key;
                var bag = kv.Value;
                if (!bag.CheckIfChanged(out var serialized))
                {
                    continue;
                }
                // TODO allow Harmony modifications
                if (name.StartsWith("OHarmony"))
                {
                    Logger.Log($"\t {name} not saved. Modifying harmony assemblies is not supported.");
                    return;
                }
                var path = Path.Combine(Paths.AssembliesInjectedDirectory, $"{name}.dll");
                Logger.Log($"\t{Path.GetFileName(path)}");
                File.WriteAllBytes(path, serialized);
            }
        }

        public void Dispose()
        {
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
