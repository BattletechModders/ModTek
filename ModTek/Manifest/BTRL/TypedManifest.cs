using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using ModTek.Util;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest.BTRL
{
    internal class TypedManifest
    {
        private readonly Dictionary<string, Dictionary<string, VersionManifestEntry>> manifestAll = new();
        private readonly Dictionary<string, VersionManifestEntry> manifestAllStrings = new();
        private readonly Dictionary<string, Dictionary<string, VersionManifestEntry>> manifestOwned = new();
        private readonly HashSet<string> activeAddendums = new();

        public void Reset(IEnumerable<VersionManifestEntry> defaultEntries)
        {
            GetContent(true).Clear();
            GetContent(false).Clear();
            manifestAllStrings.Clear();
            activeAddendums.Clear();
            SetEntries(defaultEntries, null);
        }

        public void AddAddendum(VersionManifestAddendum addendum, ContentPackIndex contentPackIndex)
        {
            SetEntries(addendum.Entries, contentPackIndex);
            activeAddendums.Add(addendum.Name);
        }

        public void AddModAddendum(ModManifest modAddendum)
        {
            if (modAddendum.RequiredOwnedResources.Except(activeAddendums).Any())
            {
                // skip since not all requirements are met
                return;
            }
            SetEntries(modAddendum.Addendum.Entries, null);
            activeAddendums.Add(modAddendum.Addendum.Name);
        }

        public VersionManifestEntry StringEntryByID(string id)
        {
            return manifestAllStrings.TryGetValue(id, out var entry) ? entry : null;
        }

        public VersionManifestEntry[] AllEntries(bool filterByOwnership = false)
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

        private void SetEntries(IEnumerable<VersionManifestEntry> entries, ContentPackIndex contentPackIndex)
        {
            foreach (var entry in entries)
            {
                SetEntry(entry, contentPackIndex == null || contentPackIndex.IsResourceOwned(entry.Id));
            }
        }

        private void SetEntry(VersionManifestEntry entry, bool owned)
        {
            if (owned)
            {
                SetEntry(GetContent(true), entry);
            }
            SetEntry(GetContent(false), entry);
            SetStringEntry(entry);
        }

        private void SetStringEntry(VersionManifestEntry entry)
        {
            if (entry.FilePath == null || !FileUtils.IsStringType(entry.FilePath))
            {
                return;
            }

            if (manifestAllStrings.ContainsKey(entry.Id))
            {
                Log($"Error: found duplicate entry for same id of string type {entry.Id}");
                return;
            }

            manifestAllStrings[entry.Id] = entry;
        }

        private static void SetEntry(
            Dictionary<string, Dictionary<string, VersionManifestEntry>> container,
            VersionManifestEntry entry)
        {
            if (!container.TryGetValue(entry.Type, out var dict))
            {
                dict = new();
                container[entry.Type] = dict;
            }
            dict[entry.Id] = entry;
        }

        private Dictionary<string, Dictionary<string, VersionManifestEntry>> GetContent(bool filterByOwnership)
        {
            return filterByOwnership ? manifestOwned : manifestAll;
        }
    }
}
