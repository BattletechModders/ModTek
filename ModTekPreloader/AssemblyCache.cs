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
        private readonly Dictionary<string, AssemblyDefinition> definitions = new Dictionary<string, AssemblyDefinition>();
        private readonly Dictionary<string, byte[]> serializations = new Dictionary<string, byte[]>();
        private readonly IAssemblyResolver resolver = new DefaultAssemblyResolver();

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters)
        {
            parameters.AssemblyResolver = parameters.AssemblyResolver ?? this;

            if (!definitions.TryGetValue(reference.Name, out var definition))
            {
                definition = resolver.Resolve(new AssemblyNameReference(reference.Name, null), parameters);
                definitions[reference.Name] = definition;
                serializations[reference.Name] = serialize(definition);
            }
            return definition;
        }

        internal void SaveAssembliesToDiskAndPreloadInjected(string assembliesInjectedDirectory)
        {
            foreach (var kv in definitions)
            {
                var name = kv.Key;
                var definition = kv.Value;
                var serialization = serialize(definition);
                if (serialization.SequenceEqual(serializations[name]))
                {
                    continue;
                }
                var path = Path.Combine(assembliesInjectedDirectory, $"{name}.dll");
                File.WriteAllBytes(path, serialization);
                Assembly.LoadFile(path); // workaround to force injected assemblies to be used
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var location = string.IsNullOrWhiteSpace(a.Location) ? "Memory" : a.Location;
                Logger.Log($"Loaded assembly {a.GetName().Name}{Environment.NewLine}\tfrom {location}");
            }
        }

        private static byte[] serialize(AssemblyDefinition definition)
        {
            using (var s = new MemoryStream())
            {
                definition.Write(s);
                return s.ToArray();
            }
        }

        public void Dispose()
        {
            resolver.Dispose();
            definitions.Clear();
            serializations.Clear();
        }
    }
}
