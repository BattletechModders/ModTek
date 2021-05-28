using BattleTech;

namespace ModTek.Manifest
{
    internal static class CacheKeys
    {
        internal static string Unique(ModEntry entry)
        {
            return Unique(entry.Type, entry.Id);
        }

        internal static string Unique(VersionManifestEntry entry)
        {
            return Unique(entry.Type, entry.Id);
        }

        private static string Unique(string type, string id)
        {
            return type + ":" + id;
        }
    }
}
