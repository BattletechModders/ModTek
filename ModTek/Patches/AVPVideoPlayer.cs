using System;
using System.IO;
using System.Linq;
using BattleTech.Rendering;
using BattleTech.Save;
using BattleTech.UI;
using Harmony;
using Localize;
using RenderHeads.Media.AVProVideo;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    /// <summary>
    /// Patch PlayVideo to allow for videos from mods to be played
    /// </summary>
    [HarmonyPatch(typeof(AVPVideoPlayer), "PlayVideo")]
    public static class AVPVideoPlayer_PlayVideo_Patch
    {
        public static bool Prefix(AVPVideoPlayer __instance, string video, Strings.Culture culture, Action<string> onComplete = null)
        {
            var videoEntry = ModTek.CustomResources["Video"].Values.LastOrDefault(entry =>
                entry.Id == video || entry.Id == Path.GetFileNameWithoutExtension(video));

            if (videoEntry == null)
                return true;

            // THIS CODE IS REWRITTEN FROM DECOMPILED HBS CODE
            // AND IS NOT SUBJECT TO MODTEK LICENSE

            var instance = Traverse.Create(__instance);
            var AVPMediaPlayer = instance.Field("AVPMediaPlayer").GetValue<MediaPlayer>();

            if (AVPMediaPlayer.Control == null)
            {
                instance.Method("ConfigureMediaPlayer").GetValue();
            }
            AVPMediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, videoEntry.FilePath, false);
            if (ActiveOrDefaultSettings.CloudSettings.subtitles)
            {
                instance.Method("LoadSubtitle", video, Strings.GetCultureNameEnglish(culture));
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
}
