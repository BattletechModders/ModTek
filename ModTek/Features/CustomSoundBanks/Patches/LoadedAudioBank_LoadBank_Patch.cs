using System.IO;
using Harmony;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Features.CustomSoundBanks.Patches;

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
        var entry = SoundBanksFeature.GetSoundBank(___name);
        if (entry == null)
        {
            return;
        }

        var directory = Path.GetDirectoryName(entry.FilePath);
        AkSoundEngine.SetBasePath(directory);
    }

    public static void Postfix(string ___name)
    {
        if (SoundBanksFeature.GetSoundBank(___name) == null)
        {
            return;
        }

        var basePath = AkBasePathGetter.GetValidBasePath();
        AkSoundEngine.SetBasePath(basePath);
    }
}