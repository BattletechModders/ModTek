using BattleTech;
using ModTek.Features.Manifest.BTRL;

namespace ModTek.Public;

public static class BTRL
{
    public static string[] AllTypes()
    {
        return BetterBTRL.Instance.AllTypes();
    }

    public static VersionManifestEntry[] AllEntriesOfType(string type, bool filterByOwnership = false)
    {
        return BetterBTRL.Instance.AllEntriesOfType(type, filterByOwnership);
    }

    public static VersionManifestEntry EntryByIDAndType(string id, string type, bool filterByOwnership = false)
    {
        return BetterBTRL.Instance.EntryByIDAndType(id, type, filterByOwnership);
    }
}