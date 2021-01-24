using BattleTech.Data;
using HBS.Collections;
using ModTek.Extensions;
using ModTek.CustomTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModTek.Util.Logger;

namespace ModTek
{
    public static class CustomTypeProcessor
    {
        public static void AddOrUpdateTag(string pathToFile)
        {
            Log($"Adding custom Tag from path: {pathToFile}");
            string fileContents = File.ReadAllText(pathToFile);
            CustomTag customTag = JsonConvert.DeserializeObject<CustomTag>(fileContents);
            Log($"  {customTag}");

            Tag_MDD customTagMDD = customTag.ToTagMDD();
            MetadataDatabase.Instance.AddOrUpdate(customTagMDD);
        }

        public static void AddOrUpdateTagSet(string pathToFile)
        {
            Log($"Adding custom TagSet from path: {pathToFile}");
            string fileContents = File.ReadAllText(pathToFile);
            CustomTagSet customTagSet = JsonConvert.DeserializeObject<CustomTagSet>(fileContents);

            TagSet_MDD tagSet_MDD = MetadataDatabase.Instance.Query<TagSet_MDD>(
                "SELECT * FROM TagSet WHERE TagSetID = @TagSetID", new
            {
                TagSetID = customTagSet.ID
            }, null, true, null, null).FirstOrDefault<TagSet_MDD>();

            // TODO: Can error out, test for that
            TagSetType tagSetType = (TagSetType)customTagSet.TypeID;

            TagSet tagSet = new TagSet(customTagSet.Tags);
            if (tagSet_MDD == null)
            {
                // Insert
                Log($"Creating new tagset of id: {customTagSet.ID}");

                // TODO: If tagset is empty, use the other method
                MetadataDatabase.Instance.GetOrCreateTagSet(customTagSet.ID, tagSet, tagSetType);
            }
            else
            {
                // Update
                Log($"Updating tagset of id: {customTagSet.ID}");
                MetadataDatabase.Instance.UpdateTagSet(customTagSet.ID, tagSet);
            }

        }

    }
}
