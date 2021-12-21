using BattleTech;

namespace ModTek.Features.Manifest.BTRL
{
    internal class ModAddendumManifest
    {
        internal readonly VersionManifestAddendum Addendum;
        internal readonly string RequiredContentPack;

        public ModAddendumManifest(VersionManifestAddendum addendum, string requiredContentPack)
        {
            Addendum = addendum;
            RequiredContentPack = requiredContentPack;
        }
    }
}
