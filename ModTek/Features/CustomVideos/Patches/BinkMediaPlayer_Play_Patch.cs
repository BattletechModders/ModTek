using System;
using BattleTech.BinkMedia;
using BinkPlugin;
using Harmony;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.CustomVideos.Patches;

/// <summary>
/// Patch Play to allow for videos from mods to be played
/// </summary>
[HarmonyPatch(typeof(BinkMediaPlayer), "Play")]
internal static class BinkMediaPlayer_Play_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(BinkMediaPlayer __instance, string videoName)
    {
        var videoEntry = VideosFeature.GetVideo(videoName);
        if (videoEntry == null)
        {
            return true;
        }

        // some code taken from HBS decompiled code, license does not apply here
        var bink = Bink.Open(
            videoEntry.FilePath,
            Bink.SoundTrackTypes.SndSimple,
            0,
            Bink.BufferingTypes.Stream,
            0UL
        );
        __instance.bink = bink;

        if (bink == IntPtr.Zero)
        {
            Debug.LogError($"ModTek error playing file at {videoEntry.FilePath}\n{Bink.GetError()}");
            return false;
        }

        var info = default(Bink.Info);
        Bink.GetInfo(bink, ref info);

        __instance.info = info;
        __instance.binkw = info.Width;
        __instance.binkh = info.Height;

        var loopCount = __instance.loopCount;
        Bink.Loop(bink, (uint) loopCount);

        var bmpTraverse = Traverse.Create(typeof(BinkMediaPlayer));
        var cr = bmpTraverse.Field("cr").GetValue<Coroutine>();
        if (cr == null)
        {
            bmpTraverse.Field("cr").SetValue(__instance.StartCoroutine("EndOfFrame"));
        }

        var cr_num = bmpTraverse.Field("cr_num").GetValue<int>();
        bmpTraverse.Field("cr_num").SetValue(cr_num + 1);

        return false;
    }
}