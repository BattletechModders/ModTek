using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Injector
{
    internal class InjectionCacheManifest
    {
        internal bool IsUpToDate { get; }

        internal InjectionCacheManifest()
        {
            var expected = GetExpectedManifestContent();
            var actual = GetActualManifestContent();
            // File.WriteAllText("Mods/.modtek/cache_a.csv", actual);
            // File.WriteAllText("Mods/.modtek/cache_e.csv", expected);
            IsUpToDate = string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
            if (IsUpToDate)
            {
                Logger.Log($"Injection cache manifest at `{Paths.GetRelativePath(Paths.InjectionCacheManifestFile)}` is up to date.");
            }
            else
            {
                Logger.Log($"Injection cache manifest at `{Paths.GetRelativePath(Paths.InjectionCacheManifestFile)}` is outdated.");
            }
        }

        internal void RefreshAndSave()
        {
            var content = GetActualManifestContent();
            File.WriteAllText(Paths.InjectionCacheManifestFile, content);
        }

        private static string GetExpectedManifestContent()
        {
            try
            {
                if (File.Exists(Paths.InjectionCacheManifestFile))
                {
                    return File.ReadAllText(Paths.InjectionCacheManifestFile);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Error reading cache {Paths.InjectionCacheManifestFile}: {e}");
                File.Delete(Paths.InjectionCacheManifestFile);
            }
            return null;
        }

        internal static string GetActualManifestContent()
        {
            var files = new List<string>();
            // input
            files.AddRange(Directory.GetFiles(Paths.ManagedDirectory));

            // preloader
            files.Add(Paths.PreloaderConfigFile);
            if (Directory.Exists(Paths.ModTekDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.ModTekDirectory, "*.dll"));
            }
            files.AddRange(Directory.GetFiles(Paths.InjectorsDirectory));
            if (Directory.Exists(Paths.AssembliesOverrideDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesOverrideDirectory, "*.dll"));
            }
            if (Directory.Exists(Paths.Harmony12XDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.Harmony12XDirectory, "*.dll"));
            }

            // output
            if (Directory.Exists(Paths.AssembliesInjectedDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesInjectedDirectory, "*.dll"));
            }
            if (Directory.Exists(Paths.AssembliesPublicizedDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesPublicizedDirectory, "*.dll"));
            }

            var content = files
                .Select(CacheEntry.FromFile)
                .OrderBy(x => x)
                .Aggregate("Generated:", (current, entry) => current + "\0" + entry);
            return content;
        }

        private class CacheEntry : IComparable<CacheEntry>, IEquatable<CacheEntry>
        {
            private readonly string Time;
            private readonly string Path;

            private CacheEntry(string time, string path)
            {
                Time = time;
                Path = path;
            }

            public int CompareTo(CacheEntry other)
            {
                return string.CompareOrdinal(Path, other.Path);
            }

            public bool Equals(CacheEntry other)
            {
                return other != null && CompareTo(other) == 0;
            }

            public override string ToString()
            {
                return $"{Time},{Path}";
            }

            public static CacheEntry FromFile(string absolutePath)
            {
                var time = File.GetLastWriteTimeUtc(absolutePath).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                var path = Paths.GetRelativePath(absolutePath);
                return new CacheEntry(time, path);
            }
        }
    }
}
