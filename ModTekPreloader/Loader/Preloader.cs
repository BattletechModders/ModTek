using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTekPreloader.Harmony12X;
using ModTekPreloader.Injector;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Loader
{
    internal static class Preloader
    {
        internal static void Run()
        {

            Logger.Setup();

            Logger.Log("Preloader starting");
            Paths.Print();
            SingleInstanceEnforcer.Enforce();
            Cleaner.Clean();
            InjectorsAppDomain.Run();

            Logger.Log("Note that when preloading assemblies of the same name, the first one loaded wins.");
            if (Config.Instance.Harmony12XEnabled)
            {
                // TODO move dynamic shim injector into own AppDomain?
                DynamicShimInjector.Setup();
            }
            else
            {
                PreloadAssemblyHarmony();
            }
            PreloadAssembliesInjected();
            PreloadAssembliesOverride();
            PreloadModTek();

            AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.CodeBase)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(Paths.GetRelativePath)
                .LogAsList("Assemblies loaded:");
        }

        private static void PreloadAssemblyHarmony()
        {
            {
                var path = Path.Combine(Paths.AssembliesOverrideDirectory, "0Harmony.dll");
                if (File.Exists(path))
                {
                    Logger.Log($"Preloading harmony from {Path.GetFileName(path)}.");
                    Assembly.LoadFile(path);
                    return;
                }
            }

            {
                var path = Path.Combine(Paths.ManagedDirectory, "0Harmony.dll");
                Logger.Log($"Preloading harmony from {Path.GetFileName(path)}.");
                Assembly.LoadFile(path);
            }
        }

        private static void PreloadAssembliesInjected()
        {
            Logger.Log($"Preloading injected assemblies from `{Paths.GetRelativePath(Paths.AssembliesInjectedDirectory)}`:");
            foreach (var file in Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                Assembly.LoadFile(file);
            }
        }

        private static void PreloadAssembliesOverride()
        {
            Logger.Log($"Preloading override assemblies from `{Paths.GetRelativePath(Paths.AssembliesOverrideDirectory)}`:");
            foreach (var file in Directory.GetFiles(Paths.AssembliesOverrideDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                Assembly.LoadFile(file);
            }
        }

        private static void PreloadModTek()
        {
            var file = Path.Combine(Paths.ModTekDirectory, "ModTek.dll");
            Logger.Log($"Preloading ModTek from `{Paths.GetRelativePath(file)}`:");
            Assembly.LoadFile(file);
        }
    }
}
