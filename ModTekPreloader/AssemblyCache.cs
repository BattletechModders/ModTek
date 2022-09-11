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
        private readonly Dictionary<string, AssemblyDefinition> assemblies = new Dictionary<string, AssemblyDefinition>();
        private readonly IAssemblyResolver resolver;

        internal AssemblyCache()
        {
            resolver = new DefaultAssemblyResolver();
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters)
        {
            parameters.AssemblyResolver = parameters.AssemblyResolver ?? this;

            if (!assemblies.TryGetValue(reference.Name, out var def))
            {
                def = resolver.Resolve(new AssemblyNameReference(reference.Name, null), parameters);
                assemblies[reference.Name] = def;
            }
            return def;
        }

        // workaround to force those assemblies to be used
        internal void DumpAssembliesToDiskThenLoadFromFile(string output)
        {
            foreach (var kv in assemblies.ToList())
            {
                var assemblyName = kv.Key;
                var assembly = kv.Value;
                var path = Path.Combine(output, $"{assemblyName}.dll");
                assembly.Write(path);
                Assembly.LoadFile(path);
                /*
                byte[] assemblyBytes;
                {
                    assemblies.Remove(kv.Key);
                    using (var s = new MemoryStream())
                    {
                        assembly.Write(s);
                        assemblyBytes = s.ToArray();
                    }
                }
                Assembly.Load(assemblyBytes);
                File.WriteAllBytes(Path.Combine(output, $"{name}.dll"), assemblyBytes);
                */
            }
            assemblies.Clear();

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var location = string.IsNullOrWhiteSpace(a.Location) ? "Memory" : a.Location;
                Logger.Log($"Loaded assembly {a.GetName().Name}{Environment.NewLine}\tfrom {location}");
            }
        }

        public void Dispose()
        {
            resolver.Dispose();
            foreach (var assembly in assemblies.Values)
            {
                assembly.Dispose();
            }
        }
    }
}
