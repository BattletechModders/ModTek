using System;
using System.Linq;
using BattleTech.Data;

namespace ModTek.Features.CustomTags;

internal static class MetadataDatabaseExtensions
{
    public static bool AddOrUpdate(this MetadataDatabase mdd, Tag_MDD tag)
    {
        var success = false;

        var tag_MDD = mdd.Query<Tag_MDD>(
                "SELECT * FROM Tag WHERE Name = @TagName COLLATE NOCASE",
                new { TagName = tag.Name }
            )
            .FirstOrDefault();

        if (tag_MDD == null)
        {
            try
            {
                mdd.Execute(
                    "INSERT INTO Tag (Name, Important, PlayerVisible, FriendlyName, Description)" +
                    " VALUES (@TagName, @TagImportant, @TagPlayerVisible, @TagFriendlyName, @TagDescription)",
                    new
                    {
                        TagName = tag.Name,
                        TagImportant = tag.Important,
                        TagPlayerVisible = tag.PlayerVisible,
                        TagFriendlyName = tag.FriendlyName,
                        TagDescription = tag.Description
                    }
                );
                Log.Main.Info?.Log($"Inserted tag: {tag.Name} into MDDB");
                success = true;
            }
            catch (Exception e)
            {
                Log.Main.Info?.Log($"Failed to insert tag: {tag.Name} into MDD due to: {e}");
                return false;
            }
        }
        else
        {
            try
            {
                mdd.Execute(
                    "UPDATE Tag SET Important = @TagImportant, PlayerVisible = @TagPlayerVisible, " +
                    "FriendlyName = @TagFriendlyName, Description = @TagDescription" +
                    " WHERE Name = @TagName",
                    new
                    {
                        TagName = tag.Name,
                        TagImportant = tag.Important,
                        TagPlayerVisible = tag.PlayerVisible,
                        TagFriendlyName = tag.FriendlyName,
                        TagDescription = tag.Description
                    }
                );
                Log.Main.Info?.Log($"Updated tag: {tag.Name} in MDDB");
                success = true;
            }
            catch (Exception e)
            {
                Log.Main.Warning?.Log($"Failed to update tag: {tag.Name} in MDD due to: {e}");
                return false;
            }
        }

        return success;
    }
}