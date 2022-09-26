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
                _preloader.InjectShimIfNecessary(ref assemblyFile);
            }

            [HarmonyPatch(typeof(AppDomain), "LoadAssemblyRaw", MethodType.Normal)]
            [HarmonyPrefix]
            private static void LoadAssemblyRaw_Prefix(ref byte[] rawAssembly)
            {
                // TODO implement
            }

            [HarmonyPatch(typeof(AppDomain), "LoadAssemblyRaw", MethodType.Normal)]
            [HarmonyPostfix]
            private static void LoadAssemblyRaw_Postfix(ref byte[] rawAssembly, ref Assembly __result)
            {
                if (__result != null && (__result.GetName().Name.StartsWith("DMDASM.") || __result.GetName().Name == "MonoMod.Utils.GetManagedSizeHelper"))
                {
                    return;
                }
                Logger.Log($"Warning: LoadAssemblyRaw called with Assembly {__result?.GetName().Name} of size {rawAssembly.Length} bytes, shimming does not support loading this way." + Environment.NewLine + Environment.StackTrace);
            }
        }
    }
}
