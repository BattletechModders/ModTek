using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ModTek.Common.Globals;
using ModTek.Common.Logging;
using ModTek.Preloader.Loader;

namespace ModTek.Preloader.Harmony12X;

internal static class ShimInjectorPatches
{
    private static DynamicShimInjector _shimInjector;
    internal static void Register(DynamicShimInjector shimInjector)
    {
        _shimInjector = shimInjector;

        {
            var filter = Config.Instance.Harmony12XLogChannelFilter;
            Logger.Main.Log($"HarmonyX channel filter(s): {HarmonyLib.Tools.Logger.ChannelFilter}");
            if (filter > 0)
            {
                HarmonyLib.Tools.Logger.ChannelFilter = (HarmonyLib.Tools.Logger.LogChannel)filter;
                var logger = new SimpleLogger(Paths.HarmonyLogFile);
                HarmonyLib.Tools.Logger.MessageReceived += (_, args) =>
                {
                    logger.Log($"[{args.LogChannel}] {args.Message}");
                };
            }
        }

        var harmony = new HarmonyLib.Harmony("ModTekPreloader.Harmony12X");
        harmony.PatchAll(typeof(AssemblyLoadPatches));
        if (Config.Instance.Harmony12XFakeAssemblyLocationEnabled)
        {
            harmony.PatchAll(typeof(AssemblyLocationPatch));
        }
        AssemblyOriginalPathTracker.SetupAssemblyResolve();
    }

    // nesting avoids this being loaded by Utilities.BuildExtensionMethodCacheForType
    private static class AssemblyLoadPatches
    {
        [HarmonyPatch(typeof(Assembly), nameof(Assembly.LoadFrom), typeof(string), typeof(bool))]
        [HarmonyPrefix]
        private static void LoadFrom_Prefix(ref string assemblyFile)
        {
            try
            {
                if (string.IsNullOrEmpty(assemblyFile))
                {
                    return;
                }

                assemblyFile = Path.GetFullPath(assemblyFile);
                if (assemblyFile.StartsWith(Paths.AssembliesShimmedDirectory))
                {
                    return;
                }

                AssemblyOriginalPathTracker.AddAssemblyPathsInSameDirectory(assemblyFile);
                _shimInjector.InjectShimIfNecessary(ref assemblyFile);
            }
            catch (Exception e)
            {
                Logger.Main.Log("Exception injecting shim: " + e);
            }
        }

        [HarmonyPatch(typeof(AppDomain), "LoadAssembly", MethodType.Normal)]
        [HarmonyPrefix]
        private static bool LoadAssembly_Prefix(ref string assemblyRef)
        {
            Logger.Main.Log("Warning: AppDomain.LoadAssembly called, which does not support shimming: " + assemblyRef);
            return true;
        }

        [HarmonyPatch(typeof(AppDomain), "LoadAssemblyRaw", MethodType.Normal)]
        [HarmonyPrefix]
        private static void LoadAssemblyRaw_Prefix(ref byte[] rawAssembly)
        {
            try
            {
                _shimInjector.InjectShimIfNecessary(ref rawAssembly);
            }
            catch (Exception e)
            {
                Logger.Main.Log("Exception injecting shim: " + e);
            }
        }
    }

    private static class AssemblyLocationPatch
    {
        // allow mods to think they are still loaded from their original location
        // hopefully doesn't mess up dnSpy
        [HarmonyPatch(typeof(Assembly), nameof(Assembly.Location), MethodType.Getter)]
        [HarmonyPrefix]
        private static bool Location(Assembly __instance, ref string __result)
        {
            if (AssemblyOriginalPathTracker.TryGetLocation(__instance.GetName().Name, out var path))
            {
                __result = path;
                return false;
            }
            return true;
        }
    }
}