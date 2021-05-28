using System.IO;
using Harmony;
using ModTek.Manifest;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.SoundBanks
{
    /// <summary>
    /// Patch LoadedAudioBank to use modded replacement SoundBank instead by changing the base path of the AkSoundEngine.
    /// </summary>
    [HarmonyPatch(typeof(LoadedAudioBank), "LoadBank")]
    internal static class LoadedAudioBank_LoadBank_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Prefix(string ___name)
        {
            if (!ModsManifest.CustomResources["SoundBank"].ContainsKey(___name))
            {
                return;
            }

            var directory = Path.GetDirectoryName(ModsManifest.CustomResources["SoundBank"][___name].FilePath);
            AkSoundEngine.SetBasePath(directory);
        }

        //ZipFile.CreateFromDirectory(startPath, zipPath, CompressionLevel.Fastest, true);
        //System.IO.Compression.ZipFile.CreateFromDirectory("","",CompressionLevel.Fastest,true);
        public static void Postfix(string ___name)
        {
            if (!ModsManifest.CustomResources["SoundBank"].ContainsKey(___name))
            {
                return;
            }

            var basePath = AkBasePathGetter.GetValidBasePath();
            AkSoundEngine.SetBasePath(basePath);
        }
    }
}
