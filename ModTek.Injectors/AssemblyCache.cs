using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ModTek.Common.Globals;
using ModTek.Common.Utils;
using Mono.Cecil;

namespace ModTek.Injectors;

class AssemblyCache : IAssemblyResolver
{
    private readonly Dictionary<string, AssemblyBag> _assemblies = new(StringComparer.Ordinal);

    public AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        return Resolve(name, new ReaderParameters());
    }

    public AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters)
    {
        parameters.AssemblyResolver ??= this;
        var readWrite = parameters.ReadWrite;
        parameters.ReadWrite = false;

        var assemblyName = reference.Name;
        if (!_assemblies.TryGetValue(assemblyName, out var assemblyBag))
        {
            if (!s_assemblyCandidates.TryGetValue(assemblyName, out var assemblyCandidate))
            {
                throw new AssemblyResolutionException(reference);
            }
            var assembly = ModuleDefinition.ReadModule(assemblyCandidate.Path, parameters).Assembly;
            assemblyBag = new AssemblyBag(assembly, assemblyCandidate.IsReadOnly);
            _assemblies[assemblyName] = assemblyBag;
        }

        if (readWrite)
        {
            if (assemblyBag.IsReadOnly)
            {
                throw new ArgumentException($"Assembly {assemblyName} is not allowed to be opened for modification");
            }
            assemblyBag.Modified = true;
        }

        return assemblyBag.Definition;
    }

    private static readonly Dictionary<string, AssemblyCandidate> s_assemblyCandidates = GatherAssemblyCandidates();
    private static Dictionary<string, AssemblyCandidate> GatherAssemblyCandidates()
    {
        var candidates = new Dictionary<string, AssemblyCandidate>(StringComparer.Ordinal);
        // this represents the loaded assemblies from the Managed directory when the injectors run
        string[] alreadyLoadedAssemblies =
        [
            "mscorlib", "System", "System.Core", "Mono.Security"
        ];
        string[] searchDirectories =
        [
            Paths.AssembliesOverrideDirectory,
            Paths.ModTekLibDirectory, // this is not allowed to be patched!
            Paths.ManagedDirectory,
        ];
        foreach (var searchDirectory in searchDirectories)
        {
            if (!Directory.Exists(searchDirectory))
            {
                continue;
            }
            foreach (var file in Directory.GetFiles(searchDirectory, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                // fix wrong dll name
                if (name == "Dapper.Unity")
                {
                    name = "Dapper";
                }
                if (candidates.ContainsKey(name))
                {
                    continue;
                }
                var isReadOnly = alreadyLoadedAssemblies.Contains(name)
                                 || ReferenceEquals(Paths.ModTekLibDirectory, searchDirectory);
                candidates[name] = new AssemblyCandidate(file, isReadOnly);
            }
        }
        return candidates;
    }
    private record AssemblyCandidate(string Path, bool IsReadOnly);

    internal void SaveAssembliesToDisk()
    {
        FileUtils.SetupCleanDirectory(Paths.AssembliesInjectedDirectory);
        Logger.Main.Log($"Assemblies modified by injectors and saved to `{FileUtils.GetRelativePath(Paths.AssembliesInjectedDirectory)}`:");
        foreach (var kv in _assemblies.OrderBy(kv => kv.Key))
        {
            var name = kv.Key;
            var bag = kv.Value;
            if (!bag.Modified)
            {
                continue;
            }
            var path = Path.Combine(Paths.AssembliesInjectedDirectory, $"{name}.dll");
            Logger.Main.Log($"\t{Path.GetFileName(path)}");
            bag.SaveTo(path);
        }
    }

    public void Dispose()
    {
        _assemblies.Clear();
    }

    private class AssemblyBag(AssemblyDefinition definition, bool isReadOnly)
    {
        internal AssemblyDefinition Definition { get; } = definition;
        internal bool IsReadOnly { get; } = isReadOnly;
        internal bool Modified;

        public void SaveTo(string path)
        {
            using var stream = new FileStream(path, FileMode.Create);
            Definition.Write(stream);
        }
    }
}