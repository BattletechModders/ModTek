using System.Collections.Generic;
using BattleTech;

namespace ModTek.Features.Manifest.BTRL
{
    internal class ModAddendumPackager
    {
        private readonly List<ModAddendumManifest> addendums = new();
        private readonly string ModName;
        private int Index;
        private ModAddendumManifest manifest;

        internal ModAddendumPackager(string modName)
        {
            ModName = modName;
        }

        internal void AddEntry(ModEntry entry)
        {
            if (manifest == null || manifest.RequiredAddendums != entry.RequiredAddendums)
            {
                var addendumName = "Mod_" + ModName + (Index > 0 ? Index.ToString() : "");
                manifest = new ModAddendumManifest(new VersionManifestAddendum(addendumName), entry.RequiredAddendums);
                addendums.Add(manifest);
                Index++;
            }

            manifest.Addendum.Add(entry.CreateVersionManifestEntry());
        }

        internal void SaveToBTRL()
        {
            foreach (var addendum in addendums)
            {
                BetterBTRL.Instance.AddModAddendum(addendum);
            }
        }
    }
}
