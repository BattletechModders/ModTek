using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            files.Add(Paths.PreloaderAssemblyFile);
            files.AddRange(Directory.GetFiles(Paths.ManagedDirectory));
            files.AddRange(Directory.GetFiles(Paths.InjectorsDirectory));
            if (Directory.Exists(Paths.AssembliesInjectedDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesInjectedDirectory));
            }
            if (Directory.Exists(Paths.AssembliesPublicizedDirectory))
            {
                files.AddRange(Directory.GetFiles(Paths.AssembliesPublicizedDirectory));
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
