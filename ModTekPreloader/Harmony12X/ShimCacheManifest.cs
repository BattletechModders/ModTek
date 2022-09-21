using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTekPreloader.Logging;
using Mono.Cecil;

namespace ModTekPreloader.Harmony12X
{
    internal class ShimCacheManifest
    {
        private readonly SortedDictionary<string, CacheEntry> data = new SortedDictionary<string, CacheEntry>();

        public void Load()
        {
            try
            {
                if (File.Exists(Paths.ShimmedCacheManifestFile))
                {
                    foreach (var line in File.ReadAllLines(Paths.ShimmedCacheManifestFile))
                    {
                        var entry = new CacheEntry(line);
                        data[entry.OriginalPath] = entry;
                    }
                    return;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Error reading cache {Paths.ShimmedCacheManifestFile}: {e}");
                File.Delete(Paths.ShimmedCacheManifestFile);
            }
            Paths.SetupCleanDirectory(Paths.AssembliesShimmedDirectory);
        }

        public string GetPath(string originalAbsolutePath)
        {
            var time = CacheEntry.GetTimeFromFile(originalAbsolutePath);
            var originalPath = Paths.GetRelativePath(originalAbsolutePath);

            if (data.TryGetValue(originalPath, out var entry))
            {
                if (time.Equals(entry.Time) && File.Exists(entry.AbsolutePath))
                {
                    if (originalAbsolutePath != entry.AbsolutePath)
                    {
                        Logger.Log($"\tLoading shimmed assembly from `{Paths.GetRelativePath(entry.AbsolutePath)}`.");
                    }
                    return entry.AbsolutePath;
                }
            }

            var shimmedPath = DetectAndPatchHarmony(originalAbsolutePath);
            var absolutePath = shimmedPath ?? originalAbsolutePath;
            data[originalPath] = new CacheEntry(time, originalPath, absolutePath);
            Save();
            return absolutePath;
        }

        private static string DetectAndPatchHarmony(string originalPath)
        {
            Logger.Log($"\tOpening assembly to check if a harmony shim should be applied `{Paths.GetRelativePath(originalPath)}`.");
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(originalPath);
            if (!HarmonyInteropFix.DetectAndPatchHarmony(assemblyDefinition))
            {
                return null;
            }

            var path = Path.Combine(Paths.AssembliesShimmedDirectory, $"{assemblyDefinition.Name.Name}.dll");
            Logger.Log($"\tSaving shimmed assembly to `{Paths.GetRelativePath(path)}`.");
            assemblyDefinition.Write(path);
            return path;
        }

        private void Save()
        {
            File.WriteAllLines(Paths.ShimmedCacheManifestFile, data.Values.Select(x => x.ToString()));
        }

        private class CacheEntry : IComparable<CacheEntry>, IEquatable<CacheEntry>
        {
            internal readonly string Time;
            internal readonly string OriginalPath;
            internal readonly string AbsolutePath;

            internal CacheEntry(string time, string originalPath, string absolutePath)
            {
                Time = time;
                OriginalPath = originalPath;
                AbsolutePath = absolutePath;
            }

            internal CacheEntry(string line)
            {
                var cols = line.Split('\0');
                Time = cols[0];
                OriginalPath = cols[1];
                AbsolutePath = cols[2];
            }

            public int CompareTo(CacheEntry other)
            {
                return string.CompareOrdinal(OriginalPath, other.OriginalPath);
            }

            public bool Equals(CacheEntry other)
            {
                return other != null && CompareTo(other) == 0;
            }

            public override string ToString()
            {
                return $"{Time}\0{OriginalPath}\0{AbsolutePath}";
            }

            public static string GetTimeFromFile(string file)
            {
                return File.GetLastWriteTimeUtc(file).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
            }
        }
    }
}
