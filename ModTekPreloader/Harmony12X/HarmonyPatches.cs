using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Harmony12X
{
    // from BepInEx 5
    internal static class HarmonyPatches
    {
        private static ShimCacheManifest cache;
        public static void RegisterHooks()
        {
            Logger.Log(nameof(RegisterHooks));
            cache = new ShimCacheManifest();
            cache.Load();
            Harmony.CreateAndPatchAll(typeof(HarmonyPatches), "ModTekPreloader.harmonyinterop");
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Assembly), nameof(Assembly.LoadFile), typeof(string))]
        private static Assembly LoadFile(string path) => null;

        [HarmonyPatch(typeof(Assembly), nameof(Assembly.LoadFile), typeof(string))]
        [HarmonyPatch(typeof(Assembly), nameof(Assembly.LoadFrom), typeof(string))]
        [HarmonyPrefix]
        private static void OnAssemblyLoad(ref string __0)
        {
            try
            {
                var originalPath = Path.GetFullPath(__0);
                Logger.Log($"Checking if shim required for `{Paths.GetRelativePath(originalPath)}`.");
                if (!originalPath.StartsWith(Paths.ManagedDirectory) && !originalPath.StartsWith(Paths.AssembliesInjectedDirectory) && !originalPath.StartsWith(Paths.AssembliesShimmedDirectory))
                {
                    // only main assembly should have harmony and we patched that already
                    __0 = cache.GetPath(originalPath);
                }
                Logger.Log($"Finished checking.");
            }
            catch (Exception e)
            {
                Logger.Log("Error preparing assembly load for shim: " + e);
            }
        }
    }
}
