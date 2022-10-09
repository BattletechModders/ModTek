using System;
using System.IO;
using System.Reflection;
using HarmonyLib;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Harmony12X
{
    internal static class ShimInjectorPatches
    {
        private static Preloader _preloader;
        internal static void Register(Preloader preloader)
        {
            _preloader = preloader;
            Harmony.CreateAndPatchAll(typeof(Patches), "ModTekPreloader.Harmony12X");
            AssemblyOriginalPathTracker.SetupAssemblyResolve();
        }

        // nesting avoids this being loaded by Utilities.BuildExtensionMethodCacheForType
        private static class Patches
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
                    _preloader.InjectShimIfNecessary(ref assemblyFile);
                }
                catch (Exception e)
                {
                    Logger.Log("Exception injecting shim: " + e);
                }
            }

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

            [HarmonyPatch(typeof(AppDomain), "LoadAssemblyRaw", MethodType.Normal)]
            [HarmonyPrefix]
            private static void LoadAssemblyRaw_Prefix(ref byte[] rawAssembly)
            {
                try
                {
                    _preloader.InjectShimIfNecessary(ref rawAssembly);
                }
                catch (Exception e)
                {
                    Logger.Log("Exception injecting shim: " + e);
                }
            }
        }
    }
}
