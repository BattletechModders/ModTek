using System;
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
        private readonly DynamicShimInjector _shimInjector;

        internal AssemblyCache(DynamicShimInjector shimInjector)
        {
            _shimInjector = shimInjector;
            searchDirectories = new List<string>
            {
                Paths.AssembliesOverrideDirectory,
                Paths.ModTekDirectory,
                Paths.ManagedDirectory
            };
            if (shimInjector.Enabled)
            {
                searchDirectories.Insert(0, Paths.Harmony12XDirectory);
            }
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
                var wasShimmed = _shimInjector.DetectAndPatchHarmony(assembly);
                assemblyBag = new AssemblyBag(assembly, wasShimmed);
                assemblies[reference.Name] = assemblyBag;
            }

            return assemblyBag.Definition;
        }

        private AssemblyDefinition SearchAssembly(AssemblyNameReference reference, ReaderParameters parameters)
        {
            // TODO allow Harmony modifications
            // TODO allow all Harmony12X versions
            if (reference.Name.StartsWith("OHarmony"))
            {
                var version = reference.Version;
                if (!(version.Major == 1 && version.Minor == 2))
                {
                    throw new NotSupportedException("Missing harmony version number, only 1.2 is supported for assembly definition loading for now.");
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

        internal void MakeAssembliesPublicAndSaveToDisk()
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

            public AssemblyBag(AssemblyDefinition definition, bool hasAlreadyChanged)
            {
                Definition = definition;
                AlwaysChanged = hasAlreadyChanged || AlwaysChangedAssemblies.Contains(Name);
                NeverChanged = !hasAlreadyChanged && NeverChangedAssemblies.Contains(Name);
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
