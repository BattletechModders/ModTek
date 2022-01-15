using System.IO;
using System.Linq;
using BattleTech.Data;
using HBS.Collections;
using ModTek.Features.CustomResources;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.BTRL;
using Newtonsoft.Json;

namespace ModTek.Features.CustomTags
{
    internal static class CustomTagFeature
    {
        internal static void ProcessTags()
        {
            var customTags = BetterBTRL.Instance.AllEntriesOfType(InternalCustomResourceType.CustomTag.ToString());
            MTLogger.Info.LogIf(customTags.Length > 0, "Processing CustomTags:");
            foreach (var entry in customTags)
            {
                AddOrUpdateTag(entry.FilePath);
            }

            var customTagSets = BetterBTRL.Instance.AllEntriesOfType(InternalCustomResourceType.CustomTagSet.ToString());
            MTLogger.Info.LogIf(customTagSets.Length > 0, "Processing CustomTagSets:");
            foreach (var entry in customTagSets)
            {
                AddOrUpdateTagSet(entry.FilePath);
            }
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
                MTLogger.Info.Log($"Creating new tagset: {customTagSet.ID} with tags: {string.Join(",", customTagSet.Tags)}");

                // TODO: If tagset is empty, use the other method
                MetadataDatabase.Instance.GetOrCreateTagSet(customTagSet.ID, tagSet, tagSetType);
            }
            else
            {
                // Update
                MTLogger.Info.Log($"Updating tagset: {customTagSet.ID} to type: {(TagSetType) customTagSet.TypeID} and tags: {string.Join(",", customTagSet.Tags)}");
                MetadataDatabase.Instance.UpdateTagSet(customTagSet.ID, tagSet);
            }
        }
    }
}
