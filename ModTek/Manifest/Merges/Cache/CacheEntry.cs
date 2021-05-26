using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Manifest.AdvMerge;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Manifest.Merges.Cache
{
    internal class CacheEntry : IEquatable<CacheEntry>
    {
        [JsonProperty(Required = Required.Always)]
        public MergeType MergeType { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public string CachedPath { get; private set; }

        [JsonProperty(Required = Required.Always)]
        public DateTime OriginalVersion { get; set; }

        [JsonProperty(Required = Required.Always)]
        public List<FileVersionTuple> Merges { get; private set; } = new();

        internal CacheEntry()
        {
        }

        internal CacheEntry(string bundleName, string id, ModEntry modEntry)
        {
            MergeType = modEntry.IsJson ? MergeType.JSON_MERGE : MergeType.TEXT_APPEND;
            CachedPath = Path.Combine(Path.Combine(FilePaths.CacheDirectory, bundleName), id + modEntry.FileExtension);
        }

        internal void Add(ModEntry modEntry)
        {
            if (MergeType == MergeType.JSON_MERGE && !modEntry.IsJson)
            {
                throw new ArgumentException($"Cannot mix json and csv for merges {modEntry.RelativePathToMods}");
            }

            var merge = FileVersionTuple.FromModEntry(modEntry);
            Merges.Add(merge);
        }

        internal string Merge(string content)
        {
            return MergeType == MergeType.JSON_MERGE ? JsonMerge(content) : TextAppend(content);
        }

        private string JsonMerge(string originalContent)
        {
            var target = JsonUtils.ParseGameJSON(originalContent);
            foreach (var entry in Merges)
            {
                var merge = JsonUtils.ParseGameJSON(entry.Path);
                JSONMerger.MergeIntoTarget(target, merge);
            }

            return target.ToString(Formatting.Indented);
        }

        private string TextAppend(string originalContent)
        {
            return Merges.Aggregate(originalContent, (current, entry) => current + File.ReadAllText(entry.Path));
        }

        // GENERATED CODE BELOW

        public bool Equals(CacheEntry other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return MergeType == other.MergeType && CachedPath == other.CachedPath && OriginalVersion.Equals(other.OriginalVersion) && Equals(Merges, other.Merges);
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

            return Equals((CacheEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) MergeType;
                hashCode = (hashCode * 397) ^ (CachedPath != null ? CachedPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ OriginalVersion.GetHashCode();
                hashCode = (hashCode * 397) ^ (Merges != null ? Merges.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
