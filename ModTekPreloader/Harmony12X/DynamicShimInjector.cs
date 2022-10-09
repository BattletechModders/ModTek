using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTekPreloader.Loader;
using ModTekPreloader.Logging;
using Mono.Cecil;

namespace ModTekPreloader.Harmony12X
{
    /* from BepInEx 5
     * https://github.com/BepInEx/HarmonyInteropDlls/
     * https://github.com/BepInEx/BepInEx.Harmony
     */
    internal class DynamicShimInjector
    {
        internal bool Enabled => Config.Instance.Harmony12XEnabled;

        private readonly ShimCacheManifest cache;

        internal DynamicShimInjector()
        {
            if (!Enabled)
            {
                Logger.Log("HarmonyX not enabled, not loading interoperability.");
                return;
            }

            Logger.Log("Setting up HarmonyX interoperability");
            if (!Directory.Exists(Paths.Harmony12XDirectory))
            {
                throw new Exception($"HarmonyX can't be loaded, directory `{Paths.GetRelativePath(Paths.Harmony12XDirectory)}` missing.");
            }

            Logger.Log($"Verifying HarmonyX related assemblies at `{Paths.GetRelativePath(Paths.Harmony12XDirectory)}`.");
            foreach (var harmonyVersion in HarmonyVersions)
            {
                var file = Path.Combine(Paths.Harmony12XDirectory, $"{harmonyVersion.Name}.dll");
                if (!File.Exists(file))
                {
                    throw new Exception($"Can't find HarmonyX related assembly under {file}.");
                }
            }

            cache = new ShimCacheManifest(this);
        }

        private readonly List<HarmonyVersion> HarmonyVersions = new List<HarmonyVersion>
        {
            new HarmonyVersion("0Harmony12", new Version(1, 0), new Version(1, 3)),
            new HarmonyVersion("0Harmony20", new Version(2, 0), new Version(2, 1)),
            new HarmonyVersion("0Harmony", new Version(2, 9), new Version(2, 10)),
            /* TODO prepare harmonyX assemblies (for quickfix use dnSpy or cecil, for proper fix setup full repo with everything inside and gitflow working)
            new HarmonyVersion("0Harmony109", new Version(1, 0), new Version(1, 1)),
            new HarmonyVersion("0Harmony12", new Version(1, 1), new Version(1, 3)),
            new HarmonyVersion("0HarmonyX20", new Version(2, 0), new Version(2, 1)),
            new HarmonyVersion("0HarmonyX29", new Version(2, 9), new Version(2, 10)),
            */
        };

        private class HarmonyVersion
        {
            public HarmonyVersion(string name, Version lowerBoundInclusive, Version upperBoundExclusive)
            {
                Name = name;
                _lowerBoundInclusive = lowerBoundInclusive;
                _upperBoundExclusive = upperBoundExclusive;
            }

            internal string Name { get; }

            private Version _lowerBoundInclusive;
            private Version _upperBoundExclusive;

            internal bool IsMatch(Version version)
            {
                return _lowerBoundInclusive <= version && version < _upperBoundExclusive;
            }
        }

        internal bool DetectAndPatchHarmony(AssemblyDefinition assemblyDefinition)
        {
            if (!Enabled)
            {
                return false;
            }

            // has harmony ref
            var harmonyReference = assemblyDefinition.MainModule.AssemblyReferences
                .SingleOrDefault(a => a.Name == "0Harmony");
            if (harmonyReference == null)
            {
                // Logger.Log($"Assembly {assemblyDefinition.Name.Name} has no harmony reference.");
                return false;
            }

            // find compatible shim
            var compatibleHarmonyAssembly = HarmonyVersions.FirstOrDefault(h => h.IsMatch(harmonyReference.Version));
            if (compatibleHarmonyAssembly == null)
            {
                Logger.Log($"Assembly {assemblyDefinition.Name.Name} has no compatible shim to be relinked to for harmony {harmonyReference.Version}.");
                return false;
            }

            if (compatibleHarmonyAssembly.Name == "0Harmony")
            {
                // Logger.Log($"Assembly {assemblyDefinition.Name.Name} already uses latest harmony.");
                return false;
            }

            // replace ref
            Logger.Log($"Assembly {assemblyDefinition.Name.Name} using 0Harmony@{harmonyReference.Version} is being relinked to {compatibleHarmonyAssembly.Name}.");
            harmonyReference.Name = compatibleHarmonyAssembly.Name;
            return true;
        }

        internal void InjectShimIfNecessary(ref string uriOrPath)
        {
            if (!Enabled)
            {
                throw new Exception("Should not be called");
            }

            if (string.IsNullOrEmpty(uriOrPath))
            {
                return;
            }

            try
            {
                var originalPath = Path.GetFullPath(uriOrPath);
                if (originalPath.StartsWith(Paths.AssembliesShimmedDirectory))
                {
                    return;
                }
                uriOrPath = cache.GetPathToShimmedAssembly(originalPath);
            }
            catch (Exception e)
            {
                Logger.Log("Error preparing assembly load for shim: " + e);
            }
        }

        internal void InjectShimIfNecessary(ref byte[] rawAssembly)
        {
            if (!Enabled)
            {
                throw new Exception("Should not be called");
            }

            // TODO implement caching if needed
            // probably a checksum -> (no shimming necessary || path to shimmed assembly)
            using (var stream = new MemoryStream(rawAssembly, false))
            using (var definition = AssemblyDefinition.ReadAssembly(stream))
            {
                var name = definition.Name.Name;
                if (name.StartsWith("DMDASM.") || name == "MonoMod.Utils.GetManagedSizeHelper")
                {
                    return;
                }

                var begin = DateTime.Now;
                if (!DetectAndPatchHarmony(definition))
                {
                    return;
                }

                using (var newStream = new MemoryStream(rawAssembly.Length * 2))
                {
                    definition.Write(newStream);
                    rawAssembly = newStream.ToArray();
                }

                var text = $"Loading shimmed assembly {name} from memory";
                text += $", shimming took {(DateTime.Now-begin).TotalSeconds:#0.000}s";
                text += ".";
                Logger.Log(text);
            }
        }
    }
}
