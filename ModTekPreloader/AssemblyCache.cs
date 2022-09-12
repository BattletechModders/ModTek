using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace ModTekPreloader
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

                if (!assemblies.TryGetValue(reference.Name, out var assembly))
                {
                    var definition = resolver.Resolve(new AssemblyNameReference(reference.Name, null), parameters);
                    assembly = new AssemblyBag(definition);
                    assemblies[reference.Name] = assembly;
                }

                return assembly.Definition;
            }
            catch (Exception e)
            {
                Logger.Log(e);
            }

            return null;
        }

        internal void SaveAssembliesToDiskAndPreloadInjected(string assembliesInjectedDirectory)
        {
            Logger.Log("Assemblies loaded after injectors ran:");
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.Location))
            {
                var location = string.IsNullOrWhiteSpace(a.Location) ? "Memory" : FileUtils.GetRelativePath(a.Location);
                Logger.Log($"\t{location}");
            }

            Logger.Log("Assemblies modified by injectors:");
            foreach (var kv in assemblies.OrderBy(kv => kv.Key))
            {
                var name = kv.Key;
                var assembly = kv.Value;
                if (!assembly.HasChanged(out var serialized))
                {
                    continue;
                }
                var path = Path.Combine(assembliesInjectedDirectory, $"{name}.dll");
                Logger.Log($"\t{FileUtils.GetRelativePath(path)}");
                File.WriteAllBytes(path, serialized);
                Assembly.LoadFile(path); // workaround; to force injected assemblies to be used
            }
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

            internal bool HasChanged(out byte[] serialized)
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
    }
}
