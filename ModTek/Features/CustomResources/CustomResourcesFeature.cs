using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.Mods;
using static ModTek.Logging.Logger;
using CustomResourcesDict = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, BattleTech.VersionManifestEntry>>;

namespace ModTek.Features.CustomResources
{
    internal static class CustomResourcesFeature
    {
        private static readonly HashSet<string> CustomResources = new();

        private enum CRType
        {
            Video,
            SoundBank
        }
        internal static readonly string[] CRTypeNames = Enum.GetNames(typeof(CRType));

        internal static void Setup()
        {
            // setup custom resources for ModTek types
            CustomResources.Add(CRType.Video.ToString());
            CustomResources.Add(CRType.SoundBank.ToString());
        }

        internal static bool IsCustomResourceType(string type)
        {
            return type != null && CustomResources.Contains(type);
        }

        internal static void ProcessModDef(ModDefEx modDef)
        {
            foreach (var customResourceType in modDef.CustomResourceTypes)
            {
                if (BTConstants.PREDEFINED_TYPES.Contains(customResourceType))
                {
                    Log($"\tWarning: {modDef.Name} has a custom resource type that has the same name as a vanilla/modtek resource type. Ignoring this type.");
                    continue;
                }

                if (!CustomResources.Contains(customResourceType))
                {
                    CustomResources.Add(customResourceType);
                }
            }
        }

        internal static CustomResourcesDict GetResourceDictionariesForTypes(IEnumerable<string> modDefCustomResourceTypes)
        {
            return modDefCustomResourceTypes
                .ToDictionary(resourceType => resourceType, resourceType =>
                        BetterBTRL.Instance.AllEntriesOfType(resourceType)?.ToDictionary(e => e.Id)
                        ?? new Dictionary<string, VersionManifestEntry>());
        }

        public static VersionManifestEntry GetVideo(string videoName)
        {
            return BetterBTRL.Instance.AllEntriesOfType(CRType.Video.ToString())
                .LastOrDefault(entry => entry.Id == videoName || entry.Id == Path.GetFileNameWithoutExtension(videoName));
        }

        public static VersionManifestEntry GetSoundBank(string id)
        {
            return BetterBTRL.Instance.EntryByIDAndType(CRType.SoundBank.ToString(), id);
        }
    }
}
