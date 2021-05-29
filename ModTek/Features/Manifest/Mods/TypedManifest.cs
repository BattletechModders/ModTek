using System.Collections.Generic;
using System.Linq;
using BattleTech;
using ModTek.Util;
using TypedDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, BattleTech.VersionManifestEntry>>;

namespace ModTek.Features.Manifest.Mods
{
    internal class TypedManifest
    {
        private readonly TypedDict manifestAll = new();
        private readonly TypedDict manifestOwned = new();

        public void Reset(IEnumerable<VersionManifestEntry> defaultEntries)
        {
            GetContent(true).Clear();
            GetContent(false).Clear();
            SetEntries(defaultEntries, true);
        }

        public void AddAddendum(VersionManifestAddendum addendum, bool owned)
        {
            SetEntries(addendum.Entries, owned);
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

        private void SetEntries(IEnumerable<VersionManifestEntry> entries, bool owned)
        {
            foreach (var entry in entries)
            {
                SetEntry(entry, owned);
            }
        }

        private void SetEntry(VersionManifestEntry entry, bool owned)
        {
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
    }
}
