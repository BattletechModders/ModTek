using System;
using BattleTech;
using Newtonsoft.Json;

namespace ModTek.Features.Manifest
{
    public class CacheKey : IEquatable<CacheKey>
    {
        [JsonProperty]
        internal readonly string Type;
        [JsonProperty]
        internal readonly string Id;

        [JsonConstructor]
        private CacheKey()
        {
        }

        internal CacheKey(string type, string id)
        {
            Type = type;
            Id = id;
        }

        internal CacheKey(VersionManifestEntry entry)
        {
            Type = entry.Type;
            Id = entry.Id;
        }

        internal CacheKey(ModEntry entry)
        {
            Type = entry.Type;
            Id = entry.Id;
        }

        // GENERATED CODE BELOW

        public bool Equals(CacheKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type == other.Type && Id == other.Id;
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

            return Equals((CacheKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Id != null ? Id.GetHashCode() : 0);
            }
        }
    }
}