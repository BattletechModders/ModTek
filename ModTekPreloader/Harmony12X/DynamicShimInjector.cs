using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Doorstop;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;
using Mono.Cecil;

namespace ModTekPreloader.Harmony12X
{
    // from BepInEx 5
    internal class DynamicShimInjector
    {
        private readonly SortedDictionary<Version, string> InteropAssemblyVersions = new SortedDictionary<Version, string>();
        private readonly HashSet<string> InteropAssemblyNames = new HashSet<string>();
        private readonly Version maxAvailableShimVersion;
        private readonly ShimCacheManifest cache;

        internal DynamicShimInjector()
        {
            if (!Config.Instance.Harmony12XEnabled)
            {
                Logger.Log("HarmonyX not enabled, not loading interoperability.");
                return;
            }

            Logger.Log("Setting up HarmonyX interoperability");
            if (!Directory.Exists(Paths.Harmony12XDirectory))
            {
                throw new Exception($"HarmonyX can't be loaded, directory `{Paths.GetRelativePath(Paths.Harmony12XDirectory)}` missing.");
            }

            Logger.Log($"Loading HarmonyX related assemblies from `{Paths.GetRelativePath(Paths.Harmony12XDirectory)}` into {Entrypoint.AppDomainNamePreloader}.");
            foreach (var file in Directory.GetFiles(Paths.Harmony12XDirectory, "*.dll"))
            {
                var shimText = "";
                if (Path.GetFileName(file).StartsWith("0Harmony"))
                {
                    using (var assemblyDefinition = AssemblyDefinition.ReadAssembly(file))
                    {
                        if (assemblyDefinition.Name.Name != "0Harmony")
                        {
                            shimText = $" ; Shim found named {assemblyDefinition.Name.Name} for Harmony version {assemblyDefinition.Name.Version}";
                            InteropAssemblyVersions.Add(assemblyDefinition.Name.Version, assemblyDefinition.Name.Name);
                            InteropAssemblyNames.Add(assemblyDefinition.Name.Name);
                        }
                    }
                }
                Logger.Log($"\t{Path.GetFileName(file)}{shimText}");
                // TODO allow Harmony modifications
                Assembly.LoadFile(file);
            }
            maxAvailableShimVersion = InteropAssemblyVersions.LastOrDefault().Key;
            cache = new ShimCacheManifest(this);
        }

        internal bool Enabled => maxAvailableShimVersion != null;

        internal void PreloadAssembliesHarmonyX(Entrypoint.GameAssemblyLoader assemblyLoader)
        {
            Logger.Log($"Preloading Harmony12X assemblies from `{Paths.GetRelativePath(Paths.Harmony12XDirectory)}` into {Entrypoint.AppDomainNameUnity}:");
            foreach (var file in Directory.GetFiles(Paths.Harmony12XDirectory, "*.dll").OrderBy(p => p))
            {
                Logger.Log($"\t{Path.GetFileName(file)}");
                assemblyLoader.LoadFile(file);
            }
        }

        internal bool DetectAndPatchHarmony(AssemblyDefinition assemblyDefinition)
        {
            if (!Enabled)
            {
                return false;
            }

            // has harmony ref
            var harmonyRef = assemblyDefinition.MainModule.AssemblyReferences
                .FirstOrDefault(a => a.Name.StartsWith("0Harmony") && !InteropAssemblyNames.Contains(a.Name));
            if (harmonyRef == null)
            {
                return false;
            }

            // find compatible shim
            var assToLoad = InteropAssemblyVersions.LastOrDefault(kv => VersionMatches(kv.Key, harmonyRef.Version));
            if (assToLoad.Key == null)
            {
                Logger.Log($"\tAssembly {assemblyDefinition.Name.Name} has no compatible shim to be relinked to.");
                return false;
            }

            // replace ref
            Logger.Log($"\tAssembly {assemblyDefinition.Name.Name} being relinked to {assToLoad.Value}.");
            harmonyRef.Name = assToLoad.Value;
            return true;
        }

        private bool VersionMatches(Version cmpV, Version refV) =>
            refV <= maxAvailableShimVersion && cmpV.Major == refV.Major && cmpV.Minor == refV.Minor && cmpV <= refV;

        internal void InjectShimIfNecessary(ref string uriOrPath)
        {
            if (!Enabled)
            {
                throw new Exception("Should not be called");
            }

            try
            {
                var originalPath = Path.GetFullPath(uriOrPath);
                Logger.Log($"Checking if shim required for `{Paths.GetRelativePath(originalPath)}`.");
                if (!originalPath.StartsWith(Paths.ManagedDirectory) && !originalPath.StartsWith(Paths.AssembliesInjectedDirectory) && !originalPath.StartsWith(Paths.AssembliesShimmedDirectory))
                {
                    // only main assembly should have harmony and we patched that already
                    uriOrPath = cache.GetPathToShimmedAssembly(originalPath);
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
