using BattleTech;

namespace ModTek.Features.Manifest.BTRL
{
    internal class ModAddendumManifest
    {
        internal readonly VersionManifestAddendum Addendum;
        internal readonly string[] RequiredAddendums;

        public ModAddendumManifest(VersionManifestAddendum addendum, string[] requiredAddendums)
        {
            Addendum = addendum;
            RequiredAddendums = requiredAddendums;
        }
    }
}
