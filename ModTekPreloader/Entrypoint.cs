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
        private const string AppDomainNameUnity = "Unity Root Domain";
        internal const string AppDomainNamePreloader = "ModTekPreloader Domain";

        // ReSharper disable once UnusedMember.Global
        public static void Start()
        {
            try
            {
                Logger.Setup();
                // this AppDomain allows us to unload all dlls used by the Preloader and Injectors
                var domain = AppDomain.CreateDomain(AppDomainNamePreloader);
                try
                {
                    var preloader = (Preloader)domain.CreateInstance(
                        typeof(Preloader).Assembly.FullName,
                        // ReSharper disable once AssignNullToNotNullAttribute
                        typeof(Preloader).FullName
                    ).Unwrap();

                    if (preloader.Harmony12XEnabled)
                    {
                        PreloadAssembliesHarmonyX();
                    }
                    else
                    {
                        PreloadAssemblyHarmony();
                    }

                    preloader.RunInjectors();

                    if (preloader.Harmony12XEnabled)
                    {
                        ShimInjectorPatches.Register(preloader);
                    }
                    PreloadAssembliesInjected();
                    PreloadAssembliesOverride();

                    // AppDomain is unused without the Patches
                    if (!preloader.Harmony12XEnabled)
                    {
                        AppDomain.Unload(domain);
                    }

                    AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.Location)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(Paths.GetRelativePath)
                        .LogAsList("Assemblies loaded:");
                }
                catch
                {
                    AppDomain.Unload(domain);
                    throw;
                }
            }
            catch (Exception e)
            {
                var message = "Exiting the game, preloader failed: " + e;
                try { Console.Error.WriteLine(message); } catch { /* ignored */ }
                try { Logger.Log(message); } catch { /* ignored */ }
                Environment.Exit(0);
            }
        }

        // TODO allow Harmony modifications
        // not supporting modifying Harmony assemblies during injection, hence preloading them now
        // would need to implement different AppDomains to support Harmony1, 2 and X modifications
        // meaning 3 different injection phases and in between upgrading the shims
        // all the while having to share the assembly cache between the app domains
        private static void PreloadAssemblyHarmony()
        {
            {
                var path = Path.Combine(Paths.AssembliesOverrideDirectory, "0Harmony.dll");
                if (File.Exists(path))
                {
                    Logger.Log($"Preloading harmony from {Path.GetFileName(path)} into {AppDomainNameUnity}.");
                    Assembly.LoadFile(path);
                    return;
                }
            }

            {
                var path = Path.Combine(Paths.ManagedDirectory, "0Harmony.dll");
                Logger.Log($"Preloading harmony from {Path.GetFileName(path)} into the Unity AppDomain.");
                Assembly.LoadFile(path);
            }
        }

        private static void PreloadAssembliesHarmonyX()
        {
            Logger.Log($"Preloading Harmony12X assemblies from `{Paths.GetRelativePath(Paths.Harmony12XDirectory)}` into {AppDomainNameUnity}:");
            foreach (var file in Directory.GetFiles(Paths.Harmony12XDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                Assembly.LoadFile(file);
            }
        }

        private static void PreloadAssembliesInjected()
        {
            Logger.Log($"Preloading injected assemblies from `{Paths.GetRelativePath(Paths.AssembliesInjectedDirectory)}` into {AppDomainNameUnity}:");
            foreach (var file in Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                Assembly.LoadFile(file);
            }
        }

        private static void PreloadAssembliesOverride()
        {
            Logger.Log($"Preloading override assemblies from `{Paths.GetRelativePath(Paths.AssembliesOverrideDirectory)}` into {AppDomainNameUnity}:");
            foreach (var file in Directory.GetFiles(Paths.AssembliesOverrideDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                Assembly.LoadFile(file);
            }
        }
    }
}