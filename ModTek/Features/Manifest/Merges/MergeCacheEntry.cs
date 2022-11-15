using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.AdvJSONMerge;
using ModTek.Features.Manifest.MDD;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Features.Manifest.Merges;

internal class MergeCacheEntry : IEquatable<MergeCacheEntry>
{
    [JsonProperty(Required = Required.Always)]
    public string CachedPath { get; private set; }

    [JsonProperty(Required = Required.Always)]
    public DateTime CachedUpdatedOn { get; set; } // set when cache was updated, needed by MDDB indexer

    [JsonProperty(Required = Required.Always)]
    public DateTime? OriginalUpdatedOn { get; private set; }

    [JsonProperty(Required = Required.Always)]
    public List<FileVersionTuple> Merges { get; private set; } = new();

    [JsonIgnore]
    public bool CacheHit { get; set; } // used during cleanup

    internal string CachedAbsolutePath => Path.Combine(FilePaths.MergeCacheDirectory, CachedPath);
    private bool IsJsonMerge => FileUtils.IsJson(CachedPath);
    private bool IsCsvAppend => FileUtils.IsCsv(CachedPath);

    // used by newtonsoft?
    internal MergeCacheEntry()
    {
    }

    internal MergeCacheEntry(VersionManifestEntry entry)
    {
        var extension = Path.GetExtension(entry.FileName);
        CachedPath = Path.Combine(entry.Type, entry.Id + extension);
        OriginalUpdatedOn = entry.GetUpdatedOnForTracking();
    }

    internal void Add(ModEntry modEntry)
    {
        var merge = FileVersionTuple.From(modEntry);
        Merges.Add(merge);
    }

    internal void Merge(string originalContent)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CachedAbsolutePath) ?? throw new InvalidOperationException());

        if (IsJsonMerge)
        {
            JsonMerge(originalContent);
        }
        else if (IsCsvAppend && ModTek.Config.NormalizeCsvIfAppending)
        {
            CsvAppend(originalContent);
        }
        else
        {
            TextAppend(originalContent);
        }

        CachedUpdatedOn = DateTime.Now;
        CacheHit = true;
    }

    private void JsonMerge(string originalContent)
    {
        var target = HBSJsonUtils.ParseGameJSON(originalContent);
        foreach (var entry in Merges)
        {
            var merge = HBSJsonUtils.ParseGameJSONFile(entry.AbsolutePath);
            AdvJSONMergeFeature.MergeIntoTarget(target, merge);
        }

        var mergedContent = target.ToString(Formatting.Indented);
        File.WriteAllText(CachedAbsolutePath, mergedContent);
    }

    private void CsvAppend(string originalContent)
    {
        using (var writer = new StreamWriter(CachedAbsolutePath))
        {
            string titleLine = null;

            void process(StreamReader reader)
            {
                var checkTitle = true;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    if (checkTitle)
                    {
                        checkTitle = false;
                        if (titleLine == null) // very first valid line is assumed to be the title
                        {
                            titleLine = line;
                        }
                        else if (titleLine == line) // any duplication of a detected title will be removed
                        {
                            continue;
                        }
                    }
                    writer.WriteLine(line);
                }
            }

            using (var reader = FileUtils.StreamReaderFromString(originalContent))
            {
                process(reader);
            }

            foreach (var path in Merges.Select(x => x.AbsolutePath))
            {
                using (var reader = new StreamReader(path))
                {
                    process(reader);
                }
            }
        }
    }

    private void TextAppend(string originalContent)
    {
        var mergedContent = Merges.Aggregate(originalContent, (current, entry) => current + File.ReadAllText(entry.AbsolutePath));
        File.WriteAllText(CachedAbsolutePath, mergedContent);
    }

    public override string ToString()
    {
        return CachedAbsolutePath;
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
            var hashCode = CachedPath != null ? CachedPath.GetHashCode() : 0;
            hashCode = (hashCode * 397) ^ OriginalUpdatedOn.GetHashCode();
            hashCode = (hashCode * 397) ^ (Merges != null ? Merges.GetHashCode() : 0);
            return hashCode;
        }
    }
}