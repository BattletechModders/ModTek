using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Harmony12X;

// workaround for shimmed assemblies being loaded from another directory
internal static class AssemblyOriginalPathTracker
{
    // name to path
    private static readonly Dictionary<string, string> AssemblyPaths = new();

    internal static void SetupAssemblyResolve()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Logger.Main.Log($"Unhandled exception: {args.ExceptionObject}");
        };

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            try
            {
                var resolvingName = new AssemblyName(args.Name);
                if (AssemblyPaths.TryGetValue(resolvingName.Name, out var assemblyPath))
                {
                    Logger.Main.Log($"Found assembly {resolvingName.Name} at {assemblyPath}.");
                    return Assembly.LoadFile(assemblyPath);
                }
            }
            catch (Exception e)
            {
                Logger.Main.Log($"Error when resolving assembly {args.Name}: {e}");
            }
            return null;
        };
    }

    private static readonly HashSet<string> ProcessedDirectories = new();
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
            string name;
            try
            {
                name = AssemblyName.GetAssemblyName(path).Name;
            }
            catch (Exception e)
            {
                Logger.Main.Log($"Error when getting assembly name from {path}: {e}");
                continue;
            }
            if (AssemblyPaths.TryGetValue(name, out var existingPath))
            {
                if (path != existingPath)
                {
                    Logger.Main.Log($"Warning: Assembly {name} found at {existingPath} and at {path}.");
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