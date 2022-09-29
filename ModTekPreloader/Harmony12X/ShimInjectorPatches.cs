using System;
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
                    _preloader.InjectShimIfNecessary(ref assemblyFile);
                }
                catch (Exception e)
                {
                    Logger.Log("Exception injecting shim: " + e);
                }
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
