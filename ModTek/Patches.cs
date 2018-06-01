using System;
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
    [HarmonyPatch("MDD_DB_PATH", PropertyMethod.Getter)]
    public static class MetadataDatabase_MDD_DB_PATH_Patch
    {
        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(ModTek.ModDBPath))
                return;

            __result = ModTek.ModDBPath;
        }
    }

    [HarmonyPatch]
    public static class DataManager_Texture2DLoadRequest_OnLoaded_Patch
    {
        public static MethodBase TargetMethod()
        {
            var type = Traverse.Create(typeof(DataManager)).Type("Texture2DLoadRequest").GetValue<Type>();
            return type.GetMethod("OnLoaded");
        }

        public static void Postfix(object __instance)
        {
            var resourceId = Traverse.Create(__instance).Field("resourceId").GetValue<string>();

            if (!ModTek.ModTexture2Ds.Contains(resourceId))
                return;

            var resource = Traverse.Create(__instance).Field("resource").GetValue<Texture2D>();
            var dataManager = Traverse.Create(__instance).Field("dataManager").GetValue<DataManager>();
            var textureManager = Traverse.Create(dataManager).Property("TextureManager").GetValue<TextureManager>();

            textureManager.InsertTexture(resourceId, resource);
        }
    }

    [HarmonyPatch(typeof(AVPVideoPlayer), "PlayVideo")]
    public static class AVPVideoPlayer_PlayVideo_Patch
    {
        public static bool Prefix(AVPVideoPlayer __instance, string video, AVPVideoPlayer.Language language, Action<string> onComplete = null)
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

    [HarmonyPatch(typeof(VersionManifestUtilities), "LoadDefaultManifest")]
    public static class VersionManifestUtilities_LoadDefaultManifest_Patch
    {
        public static void Postfix(VersionManifest __result)
        {
            ModTek.AddModEntries(__result);
        }
    }

    [HarmonyPatch(typeof(DataManager), new[] { typeof(MessageCenter) })]
    public static class DataManager_CTOR_Patch
    {
        public static void Prefix()
        {
            ModTek.LoadMods();
        }
    }
}
