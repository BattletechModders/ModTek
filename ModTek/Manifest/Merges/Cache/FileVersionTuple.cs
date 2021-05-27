using System;
using Newtonsoft.Json;

namespace ModTek.Manifest.Merges.Cache
{
    internal class FileVersionTuple : IEquatable<FileVersionTuple>
    {
        [JsonProperty(Required = Required.Always)]
        public string Path { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime Version { get; private set; }

        internal static FileVersionTuple FromModEntry(ModEntry entry)
        {
            return new() { Path = entry.AbsolutePath, Version = entry.LastWriteTimeUtc };
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

            return Path == other.Path && Version.Equals(other.Version);
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
                return ((Path != null ? Path.GetHashCode() : 0) * 397) ^ Version.GetHashCode();
            }
        }
    }
}
