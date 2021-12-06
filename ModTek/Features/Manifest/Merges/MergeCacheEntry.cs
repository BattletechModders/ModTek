using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Features.Manifest.Merges
{
    internal class MergeCacheEntry : IEquatable<MergeCacheEntry>
    {
        [JsonProperty(Required = Required.Always)]
        public string CachedPath { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime? OriginalUpdatedOn { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public List<FileVersionTuple> Merges { get; private set; } = new List<FileVersionTuple>();

        [JsonIgnore]
        public bool CacheHit { get; set; } // used during cleanup

        internal string CachedAbsolutePath => Path.Combine(FilePaths.MergeCacheDirectory, CachedPath);
        private bool IsJsonMerge => FileUtils.IsJson(CachedPath);

        internal void SetCachedPath(ModEntry entry)
        {
            SetCachedPath(entry.Type, entry.Id, entry.FileExtension);
        }

        internal void SetCachedPathAndUpdatedOn(VersionManifestEntry entry)
        {
            SetCachedPath(entry.Type, entry.Id, Path.GetExtension(entry.GetRawPath()));
            OriginalUpdatedOn = entry.UpdatedOn;
        }

        private void SetCachedPath(string type, string id, string extension)
        {
            CachedPath = Path.Combine(type, id + extension);
        }

        internal void Add(ModEntry modEntry)
        {
            var merge = FileVersionTuple.From(modEntry);
            Merges.Add(merge);
        }

        internal string Merge(string content)
        {
            return IsJsonMerge ? JsonMerge(content) : TextAppend(content);
        }

        private string JsonMerge(string originalContent)
        {
            var target = HBSJsonUtils.ParseGameJSON(originalContent);
            foreach (var entry in Merges)
            {
                var merge = HBSJsonUtils.ParseGameJSONFile(entry.AbsolutePath);
                AdvJSONMergeFeature.MergeIntoTarget(target, merge);
            }

            return target.ToString(Formatting.Indented);
        }

        private string TextAppend(string originalContent)
        {
            return Merges.Aggregate(originalContent, (current, entry) => current + File.ReadAllText(entry.AbsolutePath));
        }

        // GENERATED CODE BELOW, used Rider IDE for that, Merges have to be done using SequenceEqual (rider uses Equals)
        public bool Equals(MergeCacheEntry other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return CachedPath == other.CachedPath && OriginalUpdatedOn.Equals(other.OriginalUpdatedOn) && Merges.SequenceEqual(other.Merges);
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

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((MergeCacheEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (CachedPath != null ? CachedPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ OriginalUpdatedOn.GetHashCode();
                hashCode = (hashCode * 397) ^ (Merges != null ? Merges.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
