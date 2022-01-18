using System.Collections.Generic;
using BattleTech;

namespace ModTek.Features.Manifest.BTRL
{
    internal class VersionManifestEntryComparer : IEqualityComparer<VersionManifestEntry>
    {
        public bool Equals(VersionManifestEntry x, VersionManifestEntry y)
        {
            if (x == null || y == null)
            {
                return false;
            }
            return x.Type == y.Type && x.Id == y.Id;
        }

        public int GetHashCode(VersionManifestEntry obj)
        {
            unchecked
            {
                return ((obj.Type != null ? obj.Type.GetHashCode() : 0) * 397) ^ (obj.Id != null ? obj.Id.GetHashCode() : 0);
            }
        }
    }
}
