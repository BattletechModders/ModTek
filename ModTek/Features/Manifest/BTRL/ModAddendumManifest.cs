using BattleTech;

namespace ModTek.Features.Manifest.BTRL
{
    internal class ModAddendumManifest
    {
        internal readonly VersionManifestAddendum Addendum;
        internal readonly string[] RequiredContentPacks;

        public ModAddendumManifest(VersionManifestAddendum addendum, string[] requiredContentPacks)
        {
            Addendum = addendum;
            RequiredContentPacks = requiredContentPacks;
        }
    }
}
