using System;
using System.IO;
using System.Linq;
using BattleTech.BinkMedia;
using BinkPlugin;
using Harmony;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
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
            var videoEntry = ModTek.CustomResources["Video"]
                .Values.LastOrDefault(
                    entry =>
                        entry.Id == videoName || entry.Id == Path.GetFileNameWithoutExtension(videoName)
                );

            if (videoEntry == null)
            {
                return true;
            }

            var iTraverse = Traverse.Create(__instance);

            // some code taken from HBS decompiled code, license does not apply here
            var bink = Bink.Open(
                videoEntry.FilePath,
                Bink.SoundTrackTypes.SndSimple,
                0,
                Bink.BufferingTypes.Stream,
                0UL
            );
            iTraverse.Field("bink").SetValue(bink);

            if (bink == IntPtr.Zero)
            {
                Debug.LogError($"ModTek error playing file at {videoEntry.FilePath}\n{Bink.GetError()}");
                return false;
            }

            var info = default(Bink.Info);
            Bink.GetInfo(bink, ref info);

            iTraverse.Field("info").SetValue(info);
            iTraverse.Field("binkw").SetValue(info.Width);
            iTraverse.Field("binkh").SetValue(info.Height);

            var loopCount = iTraverse.Field("loopCount").GetValue<int>();
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
}
