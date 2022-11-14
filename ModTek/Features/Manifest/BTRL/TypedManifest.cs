using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.CustomDebugSettings;
using ModTek.Features.CustomGameTips;
using ModTek.Misc;
using ModTek.Util;
using TypedDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, BattleTech.VersionManifestEntry>>;

namespace ModTek.Features.Manifest.BTRL
{
    internal class TypedManifest
    {
        private readonly TypedDict manifest = new TypedDict();
        private readonly Dictionary<string, HashSet<string>> idToTypes = new Dictionary<string, HashSet<string>>();
        private readonly BetterCPI packIndex;
        private readonly Dictionary<string, VersionManifestAddendum> addendums = new Dictionary<string, VersionManifestAddendum>();

        private static readonly string ManifestDumpPath = Path.Combine(FilePaths.TempModTekDirectory, "Manifest.csv");
        private static readonly VersionManifestEntry[] emptyArray = Array.Empty<VersionManifestEntry>();

        public TypedManifest(BetterCPI packIndex)
        {
            this.packIndex = packIndex;
        }

        public void Reset(IEnumerable<VersionManifestEntry> defaultEntries)
        {
            manifest.Clear();
            idToTypes.Clear();
            addendums.Clear();
            SetEntries(defaultEntries);
            SetEntries(DebugSettingsFeature.DefaulManifestEntries);
            SetEntries(GameTipsFeature.DefaulManifestEntries);
        }

        private IEnumerable<VersionManifestEntry> FilterUnowned(IEnumerable<VersionManifestEntry> iterable, bool filterByOwnership)
        {
            if (!filterByOwnership || packIndex.AllContentPacksOwned)
            {
                return iterable;
            }
            return FilterUnowned(iterable);
        }

        private IEnumerable<VersionManifestEntry> FilterUnowned(IEnumerable<VersionManifestEntry> iterable)
        {
            return iterable.Where(entry => packIndex.IsResourceOwned(entry.Id));
        }

        public void AddAddendum(VersionManifestAddendum addendum)
        {
            addendums[addendum.Name] = addendum;
            SetEntries(addendum.Entries);
        }

        public VersionManifestAddendum GetAddendumByName(string name)
        {
            return addendums.TryGetValue(name, out var addendum) ? addendum : null;
        }

        public VersionManifestEntry[] AllEntries(bool filterByOwnership)
        {
            return FilterUnowned(manifest.Values.SelectMany(x => x.Values), filterByOwnership).ToArray();
        }

        public VersionManifestEntry[] AllEntriesOfType(string type)
        {
            if (manifest.TryGetValue(type, out var dict))
            {
                return dict.Values.ToArray();
            }

            return emptyArray;
        }

        public VersionManifestEntry[] AllEntriesOfResource(BattleTechResourceType type, bool filterByOwnership)
        {
            if (manifest.TryGetValue(type.ToString(), out var dict))
            {
                return FilterUnowned(dict.Values, filterByOwnership).ToArray();
            }

            return emptyArray;
        }

        public VersionManifestEntry EntryByIDAndType(string id, string type)
        {
            if (manifest.TryGetValue(type, out var dict) && dict.TryGetValue(id, out var entry))
            {
                return entry;
            }

            return default;
        }

        public VersionManifestEntry EntryByID(string id, BattleTechResourceType type, bool filterByOwnership)
        {
            if (manifest.TryGetValue(type.ToString(), out var dict) && dict.TryGetValue(id, out var entry))
            {
                if (!filterByOwnership || packIndex.IsResourceOwned(entry.Id))
                {
                    return entry;
                }
            }

            return default;
        }

        internal VersionManifestEntry[] EntriesByID(string id)
        {
            if (idToTypes.TryGetValue(id, out var set))
            {
                return set.Select(type => EntryByIDAndType(id, type)).ToArray();
            }
            return emptyArray;
        }

        private void SetEntries(IEnumerable<VersionManifestEntry> entries)
        {
            foreach (var entry in entries)
            {
                SetEntry(entry);
            }
        }

        internal void SetEntry(VersionManifestEntry entry)
        {
            var dict = manifest.GetOrCreate(entry.Type);
            dict[entry.Id] = entry;

            idToTypes.GetOrCreate(entry.Id).Add(entry.Type);
        }

        internal void DumpToDisk()
        {
            try
            {
                ModTekCacheStorage.CSVWriteTo(ManifestDumpPath, manifest.Values.SelectMany(x => x.Values));
                Log.Main.Info?.Log($"Manifest: Saved to {ManifestDumpPath}.");
            }
            catch (Exception e)
            {
                Log.Main.Info?.Log($"Manifest: Failed to save to {ManifestDumpPath}", e);
            }
        }
    }
}
