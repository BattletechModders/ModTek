using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTekPreloader.Harmony12X;
using ModTekPreloader.Loader;
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
                var assembly = kv.Value;
                if (!assembly.CheckIfChanged(out var serialized))
                {
                    continue;
                }
                var path = Path.Combine(Paths.AssembliesInjectedDirectory, $"{name}.dll");
                Logger.Log($"\t{Path.GetFileName(path)}");
                File.WriteAllBytes(path, serialized);
            }
        }

        internal void SaveAssembliesPublicizedToDisk()
        {
            Paths.SetupCleanDirectory(Paths.AssembliesPublicizedDirectory);
            AssemblyPublicizer.MakePublic(this);
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
                "Assembly-CSharp", "ModTek"
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
                var hasChanged = HarmonyInteropFix.DetectAndPatchHarmony(definition);
                AlwaysChanged = hasChanged || AlwaysChangedAssemblies.Contains(Name);
                NeverChanged = !hasChanged && NeverChangedAssemblies.Contains(Name);
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
