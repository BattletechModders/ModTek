using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Features.AdvJSONMerge;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Features.Manifest.Merges
{
    internal class MergeCacheEntry : IEquatable<MergeCacheEntry>
    {
        [JsonProperty(Required = Required.Always)]
        public string CachedFileName { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public string CachedUpdatedOn { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime OriginalUpdatedOn { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<FileVersionTuple> Merges { get; private set; } = new();

        // can't be null on persistence, but can be for temp
        [JsonProperty(Required = Required.Always)]
        public string ResourceType { get; set; }

        internal string CachedPath => Path.Combine(Path.Combine(FilePaths.CacheDirectory, ResourceType), CachedFileName);
        private bool IsJsonMerge => FileUtils.IsJson(CachedFileName);

        internal MergeCacheEntry()
        {
        }

        internal MergeCacheEntry(ModEntry entry)
        {
            ResourceType = entry.Type;
            CachedFileName = entry.Id + entry.FileExtension;
        }

        internal void Add(ModEntry modEntry)
        {
            if (IsJsonMerge && !modEntry.IsJson)
            {
                throw new ArgumentException($"Cannot mix json and csv for merges {CachedFileName}");
            }

            if (ResourceType != modEntry.Type)
            {
                if (ResourceType == null)
                {
                    ResourceType = modEntry.Type;
                }
                else if (modEntry.Type == null)
                {
                    // ignore
                }
                else
                {
                    throw new ArgumentException($"Cannot mix and match types for {CachedFileName}");
                }
            }

            var merge = FileVersionTuple.From(modEntry);
            Merges.Add(merge);
        }

        internal string Merge(string content)
        {
            return IsJsonMerge ? JsonMerge(content) : TextAppend(content);
        }

        private string JsonMerge(string originalContent)
        {
            var target = JsonUtils.ParseGameJSON(originalContent);
            foreach (var entry in Merges)
            {
                var merge = JsonUtils.ParseGameJSON(entry.Path);
                AdvJSONMergeFeature.MergeIntoTarget(target, merge);
            }

            return target.ToString(Formatting.Indented);
        }

        private string TextAppend(string originalContent)
        {
            return Merges.Aggregate(originalContent, (current, entry) => current + File.ReadAllText(entry.Path));
        }

        // GENERATED CODE BELOW, used Rider IDE for that

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

            return CachedFileName == other.CachedFileName && OriginalUpdatedOn.Equals(other.OriginalUpdatedOn) && Equals(Merges, other.Merges) && ResourceType == other.ResourceType;
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

            return Equals((MergeCacheEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CachedFileName != null ? CachedFileName.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ OriginalUpdatedOn.GetHashCode();
                hashCode = (hashCode * 397) ^ (Merges != null ? Merges.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ResourceType != null ? ResourceType.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
