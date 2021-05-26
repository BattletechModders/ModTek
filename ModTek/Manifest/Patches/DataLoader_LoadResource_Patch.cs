using System;
using System.IO;
using Harmony;
using HBS.Data;
using JetBrains.Annotations;
using ModTek.Misc;
using ModTek.Util;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest.Patches
{
    [HarmonyPatch(typeof(DataLoader), nameof(DataLoader.LoadResource), typeof(string), typeof(Action<string>))]
    internal static class DataLoader_LoadResource_Patch
    {
        private static string GetId(string path)
        {
            var relativePath = FileUtils.GetRelativePath(FilePaths.StreamingAssetsDirectory, path);
            return relativePath.StartsWith("..") ? null : Path.GetFileNameWithoutExtension(path);
        }

        [UsedImplicitly]
        internal static bool Prefix(DataLoader __instance, string path, ref Action<string> handler)
        {
            try
            {
                var id = GetId(path);
                if (id == null)
                {
                    return true;
                }

                DateTime lastWriteTimeUTC;
                try
                {
                    lastWriteTimeUTC = File.GetLastWriteTimeUtc(path);
                }
                catch (Exception e)
                {
                    Log($"Error: Can't read last write time from {path}");
                    return true;
                }

                var cache = ModsManifest.GetMergedContent(null, id, lastWriteTimeUTC);
                if (cache == null)
                {
                    var oldHandler = handler;
                    handler = originalContent =>
                    {
                        Merge(id, lastWriteTimeUTC, ref originalContent);
                        oldHandler(originalContent);
                    };
                    return true;
                }

                handler(cache);
                return false;
            }
            catch (Exception e)
            {
                Log("Error", e);
                return true;
            }
        }

        private static void Merge(string id, DateTime lastWriteTimeUtc, ref string content)
        {
            if (content == null)
            {
                return;
            }

            var mergedContent = ModsManifest.MergeOriginalContent(null, id, lastWriteTimeUtc, content);
            if (mergedContent != null)
            {
                content = mergedContent;
            }
        }
    }
}