using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using BattleTech;
using BattleTech.UI;
using HBS;
using UnityEngine;

namespace ModTek.Features.CustomSoundBankDefs;

internal static class CustomSoundHelper
{
    internal static void RegisterEvents(this SoundBankDef bank)
    {
        var guidIdMap = SceneSingletonBehavior<WwiseManager>.Instance.guidIdMap;
        foreach (var ev in bank.events)
        {
            Log.Main.Debug?.Log("\t\tsound event:" + ev.Key + ":" + ev.Value);
            guidIdMap[ev.Key] = ev.Value;
        }
    }

    internal static void SetVolume(this SoundBankDef bank)
    {
        var volume = AudioEventManager.MasterVolume / 100f;
        switch (bank.type)
        {
            case SoundBankType.Voice:
                volume *= AudioEventManager.VoiceVolume / 100f * (AudioEventManager.VoiceVolume / 100f);
                break; //долбанный HBS
            case SoundBankType.Combat:
                volume *= AudioEventManager.SFXVolume / 100f;
                break;
        }

        volume *= 100f;
        volume += bank.volumeShift;
        volume = Mathf.Min(100f, volume);
        volume = Mathf.Max(0f, volume);
        Log.Main.Debug?.Log("SoundBankDef.setVolume " + bank.name);
        foreach (var id in bank.volumeRTPCIds)
        {
            var res = AkSoundEngine.SetRTPCValue(id, volume);
            Log.Main.Debug?.Log("\tSetRTPCValue " + id + " " + volume + " result:" + res);
        }
    }
}

[HarmonyPatch(typeof(AudioEventManager))]
[HarmonyPatch(nameof(AudioEventManager.LoadAudioSettings))]
internal static class AudioEventManager_LoadAudioSettings
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix()
    {
        Log.Main.Debug?.Log("AudioEventManager.LoadAudioSettings");
        foreach (var (_, soundBank) in SoundBanksFeature.soundBanks)
        {
            if (soundBank.loaded != true)
            {
                continue;
            }

            soundBank.SetVolume();
        }
    }
}

[HarmonyPatch(typeof(AudioSettingsModule))]
[HarmonyPatch(nameof(AudioSettingsModule.SaveSettings))]
internal static class AudioSettingsModule_SaveSettings
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix(AudioSettingsModule __instance)
    {
        Log.Main.Debug?.Log("AudioSettingsModule.SaveSettings");
        foreach (var (_, soundBank) in SoundBanksFeature.soundBanks)
        {
            if (soundBank.loaded)
            {
                soundBank.SetVolume();
            }
        }
    }
}

[HarmonyPatch(typeof(WwiseManager))]
[HarmonyPatch(nameof(WwiseManager.LoadCombatBanks))]
internal static class WwiseManager_LoadCombatBanks
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix(WwiseManager __instance, ref List<LoadedAudioBank> ___loadedBanks)
    {
        Log.Main.Debug?.Log("WwiseManager.LoadCombatBanks");
        foreach (var soundBank in SoundBanksFeature.soundBanks)
        {
            if (soundBank.Value.type != SoundBankType.Combat)
            {
                continue;
            }

            if (soundBank.Value.loaded)
            {
                continue;
            }

            Log.Main.Debug?.Log("\tLoading:" + soundBank.Key);
            ___loadedBanks.Add(new LoadedAudioBank(soundBank.Key, true));
        }
    }
}

[HarmonyPatch(typeof(WwiseManager))]
[HarmonyPatch(nameof(WwiseManager.UnloadCombatBanks))]
internal static class WwiseManager_UnloadCombatBanks
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix(WwiseManager __instance, ref List<LoadedAudioBank> ___loadedBanks)
    {
        Log.Main.Debug?.Log("WwiseManager.UnloadCombatBanks");
        foreach (var soundBank in SoundBanksFeature.soundBanks)
        {
            if (soundBank.Value.type != SoundBankType.Combat)
            {
                continue;
            }

            if (soundBank.Value.loaded == false)
            {
                continue;
            }

            var loadedAudioBank = ___loadedBanks.Find(x => x.name == soundBank.Value.name);
            if (loadedAudioBank != null)
            {
                Log.Main.Debug?.Log("\tUnloading:" + soundBank.Key);
                loadedAudioBank.UnloadBank();
                ___loadedBanks.Remove(loadedAudioBank);
            }
        }
    }
}

[HarmonyPatch(typeof(LoadedAudioBank))]
[HarmonyPatch(nameof(LoadedAudioBank.UnloadBank))]
internal static class LoadedAudioBank_UnloadBank
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix(LoadedAudioBank __instance)
    {
        Log.Main.Debug?.Log("LoadedAudioBank.UnloadBank " + __instance.name);
        if (SoundBanksFeature.soundBanks.TryGetValue(__instance.name, out var soundBank))
        {
            soundBank.Unload();
        }
    }
}

[HarmonyPatch(typeof(LoadedAudioBank))]
[HarmonyPatch(nameof(LoadedAudioBank.LoadBankExternal))]
internal static class LoadedAudioBank_LoadBankExternal
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static bool Prefix(LoadedAudioBank __instance, ref AKRESULT __result)
    {
        Log.Main.Debug?.Log("LoadedAudioBank.LoadBankExternal " + __instance.name);
        if (!SoundBanksFeature.soundBanks.TryGetValue(__instance.name, out var soundBank))
        {
            Log.Main.Warning?.Log($"Soundbank {__instance.name} does not exists");
            return false;
        }

        if(!File.Exists(soundBank.filename))
        {
            Log.Main.Warning?.Log($"Soundbank {soundBank.filename} does not exists");
            return false;
        };
        byte[] content;
        try
        {
            content = File.ReadAllBytes(soundBank.filename);
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log($"\tfail to read soundbank from fs\n{e}");
            return false;
        }
        Log.Main.Debug?.Log($"\t'{soundBank.filename}' loaded. Length: {content.Length}");
        ProcessParameters pparams;
        if (content[0] == 'B' && content[1] == 'K' && content[2] == 'H' && content[3] == 'D')
        {
            pparams = null;
        }
        else
        {
            pparams = SoundBanksProcessHelper.GetRegisteredProcParams(__instance.name);
        }

        if (pparams != null)
        {
            Log.Main.Debug?.Log("\tfound post-process parameters " + pparams.param1 + " " + pparams.param2);
            try
            {
                var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.FeedbackSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Convert.FromBase64String(pparams.param1);
                aes.IV = Convert.FromBase64String(pparams.param2);
                var encryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] result;
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(content, 0, content.Length);
                    }

                    result = msEncrypt.ToArray();
                }
                content = result;
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log($"\tfail to process soundbank from fs\n{e}");
                // why try to continue on exceptions? instead of failing?
            }
        }

        soundBank.Load(content, ref __instance.id, ref __result);

        if (__result == AKRESULT.AK_Success)
        {
            soundBank.RegisterEvents();
            soundBank.SetVolume();
        }

        Log.Main.Debug?.Log("\tResult:" + __result + " id:" + __instance.id + " length:" + content.Length);
        return false;
    }
}