using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Harmony12X
{
    // workaround for shims being loaded in another directory
    internal static class AssemblyOriginalPathTracker
    {
        // name to path
        private static readonly Dictionary<string, string> AssemblyPaths = new Dictionary<string, string>();

        internal static void SetupAssemblyResolve()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resolvingName = new AssemblyName(args.Name);
                if (AssemblyPaths.TryGetValue(resolvingName.Name, out var assemblyPath))
                {
                    Logger.Log($"Found assembly {resolvingName.Name} at {assemblyPath}.");
                    return Assembly.LoadFile(assemblyPath);
                }
                return null;
            };
        }

        private static readonly HashSet<string> ProcessedDirectories = new HashSet<string>();
        internal static void AddAssemblyPathsInSameDirectory(string assemblyFile)
        {
            var directory = Path.GetDirectoryName(assemblyFile);
            if (!ProcessedDirectories.Add(directory))
            {
                return;
            }
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(directory, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (AssemblyPaths.TryGetValue(name, out var existingPath))
                {
                    if (path != existingPath)
                    {
                        Logger.Log($"Warning: Assembly {name} found at {existingPath} and at {path}.");
                    }
                }
                else
                {
                    AssemblyPaths.Add(name, path);
                }
            }
        }

        internal static bool TryGetLocation(string assemblyName, out string path)
        {
            return AssemblyPaths.TryGetValue(assemblyName, out path);
        }
    }
}
