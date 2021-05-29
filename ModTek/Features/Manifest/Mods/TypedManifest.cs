using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Misc;
using ModTek.Util;
using static ModTek.Logging.Logger;
using TypedDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, BattleTech.VersionManifestEntry>>;

namespace ModTek.Features.Manifest.Mods
{
    internal class TypedManifest
    {
        private readonly TypedDict manifestAll = new();
        private readonly TypedDict manifestOwned = new();
        private readonly List<VersionManifestAddendum> addendums = new();

        private ContentPackIndex contentPackIndex;
        public void Reset(IEnumerable<VersionManifestEntry> defaultEntries, ContentPackIndex packIndex)
        {
            contentPackIndex = packIndex;
            GetContent(true).Clear();
            GetContent(false).Clear();
            SetEntries(defaultEntries);
        }

        public void AddAddendum(VersionManifestAddendum addendum)
        {
            SetEntries(addendum.Entries);
        }

        public VersionManifestEntry[] AllEntries(bool filterByOwnership)
        {
            return GetContent(filterByOwnership).Values.SelectMany(x => x.Values).ToArray();
        }

        public VersionManifestEntry[] AllEntriesOfResource(BattleTechResourceType type, bool filterByOwnership)
        {
            if (GetContent(filterByOwnership).TryGetValue(type.ToString(), out var dict))
            {
                return dict.Values.ToArray();
            }

            return default;
        }

        public VersionManifestEntry GetEntryByID(string id, BattleTechResourceType type, bool filterByOwnership)
        {
            if (GetContent(filterByOwnership).TryGetValue(type.ToString(), out var dict) && dict.TryGetValue(id, out var entry))
            {
                return entry;
            }

            return default;
        }

        private void SetEntries(IEnumerable<VersionManifestEntry> entries)
        {
            foreach (var entry in entries)
            {
                SetEntry(entry);
            }
        }

        private void SetEntry(VersionManifestEntry entry)
        {
            // if content pack index not yet loaded, assume its owned (vanilla behavior)
            // need to check every record as mods could have not specified a required addendum
            var owned = contentPackIndex?.IsResourceOwned(entry.Id) ?? true;
            if (owned)
            {
                SetEntry(GetContent(true), entry);
            }

            SetEntry(GetContent(false), entry);
        }

        private static void SetEntry(TypedDict container, VersionManifestEntry entry)
        {
            var dict = container.GetOrCreate(entry.Type);
            dict[entry.Id] = entry;
        }

        private TypedDict GetContent(bool filterByOwnership)
        {
            return filterByOwnership ? manifestOwned : manifestAll;
        }

        internal void DumpToDisk()
        {
            try
            {
                ModTekCacheStorage.CompressedCSVWriteTo(FilePaths.ManifestAllDumpPath, manifestAll.Values.SelectMany(x => x.Values));
                Log($"TypedManifest: Saved to {FilePaths.ManifestAllDumpPath}.");
            }
            catch (Exception e)
            {
                Log($"TypedManifest: Failed to save to {FilePaths.ManifestAllDumpPath}", e);
            }
            try
            {
                ModTekCacheStorage.CompressedCSVWriteTo(FilePaths.ManifestOwnedDumpPath, manifestOwned.Values.SelectMany(x => x.Values));
                Log($"TypedManifest: Saved to {FilePaths.ManifestOwnedDumpPath}.");
            }
            catch (Exception e)
            {
                Log($"TypedManifest: Failed to save to {FilePaths.ManifestOwnedDumpPath}", e);
            }
        }
    }
}
