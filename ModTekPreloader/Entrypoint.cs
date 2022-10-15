using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTekPreloader;
using ModTekPreloader.Harmony12X;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;

// ReSharper disable once CheckNamespace
namespace Doorstop
{
    // ReSharper disable once UnusedMember.Global
    class Entrypoint
    {
        // ReSharper disable once UnusedMember.Global
        public static void Start()
        {
            try
            {
                Logger.Setup();

                // this AppDomain allows us to unload all dlls used by the Preloader and Injectors
                // TODO allow Harmony modifications
                // not supporting modifying Harmony assemblies during injection
                // would need to implement different AppDomains to support Harmony1, 2 and X modifications
                // meaning 3 different injection phases and in between upgrading the shims
                // all the while having to share the assembly cache between the app domains
                var domain = AppDomain.CreateDomain("ModTekPreloader Domain");
                try
                {
                    var preloader = (Preloader)domain.CreateInstance(
                            typeof(Preloader).Assembly.FullName,
                            // ReSharper disable once AssignNullToNotNullAttribute
                            typeof(Preloader).FullName
                        )
                        .Unwrap();

                    preloader.RunInjectors();
                }
                finally
                {
                    AppDomain.Unload(domain);
                }

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
            catch (Exception e)
            {
                var message = "Exiting the game, preloader failed: " + e;
                try { Console.Error.WriteLine(message); } catch { /* ignored */ }
                try { Logger.Log(message); } catch { /* ignored */ }
                Environment.Exit(0);
            }
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