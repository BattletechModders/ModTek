using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech.Data;
using HBS.Collections;
using ModTek.Features.Manifest;
using Newtonsoft.Json;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.CustomTags
{
    internal static class CustomTagFeature
    {
        private static HashSet<ModEntry> CustomTags = new HashSet<ModEntry>();
        private static HashSet<ModEntry> CustomTagSets = new HashSet<ModEntry>();

        internal static bool Add(ModEntry entry)
        {
            if (entry.Type == BTConstants.CustomType_Tag)
            {
                CustomTags.Add(entry);
                return true;
            }

            if (entry.Type == BTConstants.CustomType_TagSet)
            {
                CustomTagSets.Add(entry);
                return true;
            }

            return false;
        }

        internal static void ProcessTags()
        {
            LogIf(CustomTags.Count > 0, "Processing CustomTags:");
            foreach (var modEntry in CustomTags)
            {
                AddOrUpdateTag(modEntry.AbsolutePath);
            }

            LogIf(CustomTagSets.Count > 0, "Processing CustomTagSets:");
            foreach (var modEntry in CustomTagSets)
            {
                AddOrUpdateTagSet(modEntry.AbsolutePath);
            }

            CustomTags = null;
            CustomTagSets = null;
        }

        private static void AddOrUpdateTag(string pathToFile)
        {
            var fileContents = File.ReadAllText(pathToFile);
            var customTag = JsonConvert.DeserializeObject<CustomTag>(fileContents);

            var customTagMDD = customTag.ToTagMDD();
            MetadataDatabase.Instance.AddOrUpdate(customTagMDD);
        }

        private static void AddOrUpdateTagSet(string pathToFile)
        {
            var fileContents = File.ReadAllText(pathToFile);
            var customTagSet = JsonConvert.DeserializeObject<CustomTagSet>(fileContents);

            var tagSet_MDD = MetadataDatabase.Instance.Query<TagSet_MDD>(
                    "SELECT * FROM TagSet WHERE TagSetID = @TagSetID",
                    new { TagSetID = customTagSet.ID }
                )
                .FirstOrDefault();

            // TODO: Can error out, test for that
            var tagSetType = (TagSetType) customTagSet.TypeID;

            var tagSet = new TagSet(customTagSet.Tags);
            if (tagSet_MDD == null)
            {
                // Insert
                Log($"Creating new tagset: {customTagSet.ID} with tags: {string.Join(",", customTagSet.Tags)}");

                // TODO: If tagset is empty, use the other method
                MetadataDatabase.Instance.GetOrCreateTagSet(customTagSet.ID, tagSet, tagSetType);
            }
            else
            {
                // Update
                Log($"Updating tagset: {customTagSet.ID} to type: {(TagSetType) customTagSet.TypeID} and tags: {string.Join(",", customTagSet.Tags)}");
                MetadataDatabase.Instance.UpdateTagSet(customTagSet.ID, tagSet);
            }
        }
    }
}
