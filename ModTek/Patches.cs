using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.Data;
using BattleTech.Rendering;
using BattleTech.Save;
using BattleTech.UI;
using Harmony;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek
{
    [HarmonyPatch(typeof(VersionInfo), "GetReleaseVersion")]
    public static class VersionInfo_GetReleaseVersion_Patch
    {
        public static void Postfix(ref string __result)
        {
            var old = __result;
            __result = old + $" w/ ModTek v{Assembly.GetExecutingAssembly().GetName().Version}";
        }
    }

    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFilepath")]
    public static class AssetBundleManager_AssetBundleNameToFilepath_Patch
    {
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
                return;

            __result = ModTek.ModAssetBundlePaths[assetBundleName];
        }
    }

    [HarmonyPatch(typeof(AssetBundleManager), "AssetBundleNameToFileURL")]
    public static class AssetBundleManager_AssetBundleNameToFileURL_Patch
    {
        public static void Postfix(string assetBundleName, ref string __result)
        {
            if (!ModTek.ModAssetBundlePaths.ContainsKey(assetBundleName))
                return;

            __result = $"file://{ModTek.ModAssetBundlePaths[assetBundleName]}";
        }
    }

    [HarmonyPatch(typeof(MetadataDatabase))]
    [HarmonyPatch("MDD_DB_PATH", MethodType.Getter)]
    public static class MetadataDatabase_MDD_DB_PATH_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(ModTek.ModMDDBPath))
                return;

            __result = ModTek.ModMDDBPath;
        }
    }

    [HarmonyPatch(typeof(AVPVideoPlayer), "PlayVideo")]
    public static class AVPVideoPlayer_PlayVideo_Patch
    {
        public static bool Prefix(AVPVideoPlayer __instance, string video, Language language, Action<string> onComplete = null)
        {
            if (!ModTek.ModVideos.ContainsKey(video))
                return true;

            // THIS CODE IS REWRITTEN FROM DECOMPILED HBS CODE
            // AND IS NOT SUBJECT TO MODTEK LICENSE

            var instance = Traverse.Create(__instance);
            var AVPMediaPlayer = instance.Field("AVPMediaPlayer").GetValue<MediaPlayer>();

            if (AVPMediaPlayer.Control == null)
            {
                instance.Method("ConfigureMediaPlayer").GetValue();
            }
            AVPMediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, ModTek.ModVideos[video], false);
            if (ActiveOrDefaultSettings.CloudSettings.subtitles)
            {
                instance.Method("LoadSubtitle", video, language.ToString()).GetValue();
            }
            else
            {
                AVPMediaPlayer.DisableSubtitles();
            }
            BTPostProcess.SetUIPostprocessing(false);

            instance.Field("OnPlayerComplete").SetValue(onComplete);
            instance.Method("Initialize").GetValue();

            // END REWRITTEN DECOMPILED HBS CODE

            return false;
        }
    }

    [HarmonyPatch(typeof(SimGame_MDDExtensions), "UpdateContract")]
    public static class SimGame_MDDExtensions_UpdateContract_Patch
    {
        public static void Prefix(ref string fileID)
        {
            if (Path.IsPathRooted(fileID))
                fileID = Path.GetFileNameWithoutExtension(fileID);
        }
    }

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        public static bool Prefix(ref VersionManifest __result)
        {
            // Return the cached manifest if it exists -- otherwise call the method as normal
            if (ModTek.CachedVersionManifest != null)
            {
                __result = ModTek.CachedVersionManifest;
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    [HarmonyPatch(typeof(BattleTechResourceLocator), "RefreshTypedEntries")]
    public static class BattleTechResourceLocator_RefreshTypedEntries_Patch
    {
        public static void Postfix(ContentPackIndex ___contentPackIndex,
            Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> ___baseManifest,
            Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>> ___contentPacksManifest,
            Dictionary<VersionManifestAddendum, Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>> ___addendumsManifest)
        {
            if (ModTek.BTRLEntries.Count > 0)
            {
                foreach(var entry in ModTek.BTRLEntries)
                {
                    var versionManifestEntry = entry.GetVersionManifestEntry();
                    var resourceType = (BattleTechResourceType)Enum.Parse(typeof(BattleTechResourceType), entry.Type);

                    if (___contentPackIndex == null || ___contentPackIndex.IsResourceOwned(entry.Id))
                    {
                        // add to baseManifest
                        if (!___baseManifest.ContainsKey(resourceType))
                            ___baseManifest.Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                        ___baseManifest[resourceType][entry.Id] = versionManifestEntry;
                    }
                    else
                    {
                        // add to contentPackManifest
                        if (!___contentPacksManifest.ContainsKey(resourceType))
                            ___contentPacksManifest.Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                        ___contentPacksManifest[resourceType][entry.Id] = versionManifestEntry;
                    }

                    if (!string.IsNullOrEmpty(entry.AddToAddendum))
                    {
                        // add to addendumsManifest
                        var addendum = ModTek.CachedVersionManifest.GetAddendumByName(entry.AddToAddendum);
                        if (addendum != null)
                        {
                            if (!___addendumsManifest.ContainsKey(addendum))
                                ___addendumsManifest.Add(addendum, new Dictionary<BattleTechResourceType, Dictionary<string, VersionManifestEntry>>());

                            if (!___addendumsManifest[addendum].ContainsKey(resourceType))
                                ___addendumsManifest[addendum].Add(resourceType, new Dictionary<string, VersionManifestEntry>());

                            ___addendumsManifest[addendum][resourceType][entry.Id] = versionManifestEntry;
                        }
                    }
                }
            }
        }
    }

    // This will disable activateAfterInit from functioning for the Start() on the "Main" game object which activates the BattleTechGame object
    // This stops the main game object from loading immediately -- so work can be done beforehand
    [HarmonyPatch(typeof(ActivateAfterInit), "Start")]
    public static class ActivateAfterInit_Start_Patch
    {
        public static bool Prefix(ActivateAfterInit __instance)
        {
            Traverse trav = Traverse.Create(__instance);
            if (ActivateAfterInit.ActivateAfter.Start.Equals(trav.Field("activateAfter").GetValue<ActivateAfterInit.ActivateAfter>()))
            {
                GameObject[] gameObjects = trav.Field("activationSet").GetValue<GameObject[]>();
                foreach (var gameObject in gameObjects)
                {
                    if ("BattleTechGame".Equals(gameObject.name))
                    {
                        // Don't activate through this call!
                        return false;
                    }
                }
            }
            // Call the method
            return true;
        }
    }
}

