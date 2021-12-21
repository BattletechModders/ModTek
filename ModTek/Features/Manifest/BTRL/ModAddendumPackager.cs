using System.Collections.Generic;
using BattleTech;

namespace ModTek.Features.Manifest.BTRL
{
    internal class ModAddendumPackager
    {
        private readonly List<ModAddendumManifest> addendums = new List<ModAddendumManifest>();
        private readonly string ModName;
        private int Index;
        private ModAddendumManifest manifest;

        internal ModAddendumPackager(string modName)
        {
            ModName = modName;
        }

        internal void AddEntry(ModEntry entry)
        {
            if (manifest == null || manifest.RequiredContentPack != entry.RequiredContentPack)
            {
                var addendumName = "Mod_" + ModName + (Index > 0 ? Index.ToString() : "");
                manifest = new ModAddendumManifest(new VersionManifestAddendum(addendumName), entry.RequiredContentPack);
                addendums.Add(manifest);
                Index++;
            }

            var manifestEntry = entry.CreateVersionManifestEntry();
            manifest.Addendum.Add(manifestEntry);
        }

        internal void SaveToBTRL()
        {
            foreach (var addendum in addendums)
            {
                BetterBTRL.Instance.AddModAddendum(addendum, true);
            }
        }
    }
}
