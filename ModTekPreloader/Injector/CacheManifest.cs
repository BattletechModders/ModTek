using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTekPreloader.Logging;

namespace ModTekPreloader.Injector
{
    internal class CacheManifest
    {
        internal bool IsUpToDate { get; }

        private CacheManifest(bool isUpToDate)
        {
            IsUpToDate = isUpToDate;
        }

        public static CacheManifest Load()
        {
            var expected = GetExpectedManifestContent();
            var actual = GetActualManifestContent();
            return new CacheManifest(string.Equals(expected, actual, StringComparison.Ordinal));
        }

        public void Save()
        {
            var content = GetActualManifestContent();
            File.WriteAllText(Paths.CacheManifestFile, content);
        }

        private static string GetExpectedManifestContent()
        {
            try
            {
                if (File.Exists(Paths.CacheManifestFile))
                {
                    return File.ReadAllText(Paths.CacheManifestFile);
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Error reading cache {Paths.CacheManifestFile}: {e}");
                File.Delete(Paths.CacheManifestFile);
            }
            return null;
        }

        private static string GetActualManifestContent()
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

            // output
            if (Directory.Exists(Paths.AssembliesInjectedDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesInjectedDirectory));
            }
            if (Directory.Exists(Paths.AssembliesPublicizedDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesPublicizedDirectory, "*.dll"));
            }

            var content = files
                .Select(CacheEntry.FromFile)
                .OrderBy(x => x)
                .Aggregate(CacheEntry.Header, (current, entry) => current + "\n" + entry);
            return content;
        }

        private class CacheEntry : IComparable<CacheEntry>, IEquatable<CacheEntry>
        {
            private readonly string RelativePath;
            private readonly string LastWriteTime;

            private CacheEntry(string lastWriteTime, string relativePath)
            {
                LastWriteTime = lastWriteTime;
                RelativePath = relativePath;
            }

            public int CompareTo(CacheEntry other)
            {
                return string.CompareOrdinal(RelativePath, other.RelativePath);
            }

            public bool Equals(CacheEntry other)
            {
                return other != null && CompareTo(other) == 0;
            }

            public override string ToString()
            {
                return $"{LastWriteTime},{RelativePath}";
            }

            public static CacheEntry FromFile(string absolutePath)
            {
                var lastWrite = File.GetLastWriteTimeUtc(absolutePath).ToString("o", System.Globalization.CultureInfo.InvariantCulture);
                var relativePath = Paths.GetRelativePath(absolutePath);
                return new CacheEntry(lastWrite, relativePath);
            }

            public const string Header = "LastWriteTime,RelativePath";
        }
    }
}
