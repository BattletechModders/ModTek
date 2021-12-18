using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest.BTRL;

namespace ModTek.Features.CustomSoundBanks
{
    internal static class SoundBanksFeature
    {
        internal static VersionManifestEntry GetSoundBank(string id)
        {
            return BetterBTRL.Instance.EntryByIDAndType(InternalCustomResourceType.SoundBank.ToString(), id);
        }
    }
}
