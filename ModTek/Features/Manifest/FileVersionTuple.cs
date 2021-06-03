using System;
using BattleTech;
using ModTek.Misc;
using Newtonsoft.Json;

namespace ModTek.Features.Manifest
{
    internal class FileVersionTuple : IEquatable<FileVersionTuple>
    {
        [JsonProperty]
        public string AssetBundleName { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public string Path { get; private set; }

        [JsonIgnore]
        public string AbsolutePath => System.IO.Path.Combine(FilePaths.ModsDirectory, Path);

        [JsonProperty(Required = Required.Always)]
        public DateTime UpdatedOn { get; private set; }

        [JsonIgnore]
        public bool CacheHit { get; set; } // used during cleanup

        internal static FileVersionTuple From(ModEntry entry)
        {
            // path for MergeCache is actually the relative path to the ModsDirectory
            return new() { AssetBundleName = entry.AssetBundleName, Path = entry.RelativePathToMods, UpdatedOn = entry.LastWriteTimeUtc };
        }

        internal static FileVersionTuple From(VersionManifestEntry entry)
        {
            // path for MDDBCache is just part of a unique identifier
            return new() { AssetBundleName = entry.AssetBundleName, Path = entry.GetRawPath(), UpdatedOn = entry.UpdatedOn };
        }

        public override string ToString()
        {
            return Path;
        }

        public bool Equals(VersionManifestEntry entry)
        {
            if (AssetBundleName != entry.AssetBundleName)
            {
                return false;
            }

            if (UpdatedOn != entry.UpdatedOn)
            {
                return false;
            }

            if (Path != entry.GetRawPath())
            {
                return false;
            }

            return true;
        }

        // GENERATED CODE BELOW

        public bool Equals(FileVersionTuple other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return AssetBundleName == other.AssetBundleName && Path == other.Path && UpdatedOn.Equals(other.UpdatedOn);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((FileVersionTuple) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (AssetBundleName != null ? AssetBundleName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ UpdatedOn.GetHashCode();
                return hashCode;
            }
        }
    }
}
