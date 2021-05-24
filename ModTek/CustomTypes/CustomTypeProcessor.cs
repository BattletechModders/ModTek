using System.IO;
using System.Linq;
using BattleTech.Data;
using HBS.Collections;
using ModTek.Misc;
using Newtonsoft.Json;
using static ModTek.Util.Logger;

namespace ModTek.CustomTypes
{
    internal static class CustomTypeProcessor
    {
        public static void AddOrUpdateTag(string pathToFile)
        {
            var fileContents = File.ReadAllText(pathToFile);
            var customTag = JsonConvert.DeserializeObject<CustomTag>(fileContents);

            var customTagMDD = customTag.ToTagMDD();
            MetadataDatabase.Instance.AddOrUpdate(customTagMDD);
        }

        public static void AddOrUpdateTagSet(string pathToFile)
        {
            var fileContents = File.ReadAllText(pathToFile);
            var customTagSet = JsonConvert.DeserializeObject<CustomTagSet>(fileContents);

            var tagSet_MDD = MetadataDatabase.Instance.Query<TagSet_MDD>(
                    "SELECT * FROM TagSet WHERE TagSetID = @TagSetID",
                    new { TagSetID = customTagSet.ID },
                    null,
                    true,
                    null,
                    null
                )
                .FirstOrDefault<TagSet_MDD>();

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
