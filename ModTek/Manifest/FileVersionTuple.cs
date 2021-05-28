using System;
using BattleTech;
using Newtonsoft.Json;

namespace ModTek.Manifest
{
    internal class FileVersionTuple : IEquatable<FileVersionTuple>
    {
        [JsonProperty]
        public string AssetBundleName { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public string Path { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime UpdatedOn { get; private set; }

        internal static FileVersionTuple From(ModEntry entry)
        {
            return new() { AssetBundleName = entry.AssetBundleName, Path = entry.RelativePathToMods, UpdatedOn = entry.LastWriteTimeUtc };
        }

        internal static FileVersionTuple From(VersionManifestEntry entry)
        {
            return new() { AssetBundleName = entry.AssetBundleName, Path = entry.GetRawPath(), UpdatedOn = entry.UpdatedOn };
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

            return Path == other.Path && UpdatedOn.Equals(other.UpdatedOn);
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
                return ((Path != null ? Path.GetHashCode() : 0) * 397) ^ UpdatedOn.GetHashCode();
            }
        }
    }
}
