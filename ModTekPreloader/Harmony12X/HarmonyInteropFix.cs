using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;
using Mono.Cecil;

namespace ModTekPreloader.Harmony12X
{
    // from BepInEx 5
    internal static class HarmonyInteropFix
    {
        private static Version maxAvailableShimVersion;
        private static readonly SortedDictionary<Version, string> Assemblies = new SortedDictionary<Version, string>();
        private static readonly HashSet<string> InteropAssemblyNames = new HashSet<string>();

        public static bool HasShims => maxAvailableShimVersion != null;

        public static void RegisterShims()
        {
            Logger.Log(nameof(RegisterShims));
            if (!Config.Instance.Harmony12XEnabled)
            {
                Logger.Log("HarmonyX not enabled, not loading interoperability.");
                return;
            }
            if (!Directory.Exists(Paths.Harmony12XDirectory))
            {
                return;
            }
            foreach (var file in Directory.GetFiles(Paths.Harmony12XDirectory, "*.dll"))
            {
                if (Path.GetFileName(file).StartsWith("0Harmony"))
                {
                    using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(file))
                    {
                        if (assemblyDefinition.Name.Name != "0Harmony")
                        {
                            Logger.Log($"\tFound shim {assemblyDefinition.Name.Name} {assemblyDefinition.Name.Version}");
                            Assemblies.Add(assemblyDefinition.Name.Version, assemblyDefinition.Name.Name);
                            InteropAssemblyNames.Add(assemblyDefinition.Name.Name);
                        }
                    }
                }
                Logger.Log($"\tPreloading assembly `{Paths.GetRelativePath(file)}`.");
                Assembly.LoadFile(file);
            }
            maxAvailableShimVersion = Assemblies.LastOrDefault().Key;
        }

        public static bool DetectAndPatchHarmony(AssemblyDefinition assemblyDefinition)
        {
            if (!HasShims)
            {
                return false;
            }

            var harmonyRef = assemblyDefinition.MainModule.AssemblyReferences
                .FirstOrDefault(a => a.Name.StartsWith("0Harmony") && !InteropAssemblyNames.Contains(a.Name));
            if (harmonyRef == null)
            {
                return false;
            }

            var assToLoad = Assemblies.LastOrDefault(kv => VersionMatches(kv.Key, harmonyRef.Version));
            Logger.Log($"\tAssembly {assemblyDefinition.Name.Name} relinked to {assToLoad.Value}.");
            harmonyRef.Name = assToLoad.Value;
            return true;
        }

        private static bool VersionMatches(Version cmpV, Version refV) =>
            refV <= maxAvailableShimVersion && cmpV.Major == refV.Major && cmpV.Minor == refV.Minor && cmpV <= refV;

    }
}
