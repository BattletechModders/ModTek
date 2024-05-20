using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ModTek.Common.Globals;
using ModTek.Common.Utils;

namespace ModTek.Preloader.Loader;

// useful for debugging
internal static class AssemblyTracker
{
    private static readonly HashSet<Assembly> s_assembliesLoaded = new();

    internal static void Setup()
    {
        File.WriteAllText(Paths.AssembliesLoadedLogPath, "Loaded assemblies:");
        FileUtils.SetupCleanDirectory(Paths.AssembliesLoadedDirectory);

        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
        {
            try
            {
                ProcessAssembly(args.LoadedAssembly);
            }
            catch (Exception e)
            {
                Logger.Main.Log($"Error during OnCurrentDomainOnAssemblyLoad event with assembly={args.LoadedAssembly}: {e}");
            }
        };

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            ProcessAssembly(assembly);
        }
    }

    private static void ProcessAssembly(Assembly assembly)
    {
        if (assembly.ReflectionOnly)
        {
            return;
        }

        if (!s_assembliesLoaded.Add(assembly))
        {
            return;
        }

        var locationOrName = AssemblyUtils.GetLocationOrName(assembly);
        File.AppendAllText(Paths.AssembliesLoadedLogPath, CSharpUtils.AsTextListLine(locationOrName));

        if (assembly.IsDynamic || !Path.IsPathRooted(assembly.Location))
        {
            return;
        }

        var targetPath = new Uri(assembly.CodeBase).AbsoluteUri;
        var linkPath = Path.Combine(Paths.AssembliesLoadedDirectory, assembly.GetName().Name + ".dll.url");
        File.WriteAllText(
            linkPath,
            $"""
[InternetShortcut]
URL={targetPath}
"""
        );
    }
}
