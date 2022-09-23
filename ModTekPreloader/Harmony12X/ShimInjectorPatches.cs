using System;
using System.Reflection;
using HarmonyLib;
using ModTekPreloader.Loader;

namespace ModTekPreloader.Harmony12X
{
    internal static class ShimInjectorPatches
    {
        private static Preloader _preloader;
        internal static void Register(Preloader preloader)
        {
            _preloader = preloader;
            Harmony.CreateAndPatchAll(typeof(ShimInjectorPatches), typeof(ShimInjectorPatches).FullName);
        }

        [HarmonyPatch(typeof(Assembly), nameof(Assembly.LoadFrom), typeof(string), typeof(bool))]
        [HarmonyPrefix]
        private static void LoadFrom(ref string assemblyFile)
        {
            if (string.IsNullOrEmpty(assemblyFile))
            {
                return;
            }

            _preloader.InjectShimIfNecessary(ref assemblyFile);
        }

        [HarmonyPatch(typeof(AppDomain), "LoadAssemblyRaw", MethodType.Normal)]
        [HarmonyPrefix]
        private static void LoadAssemblyRaw(ref byte[] rawAssembly)
        {
            // TODO implement
        }
    }
}
