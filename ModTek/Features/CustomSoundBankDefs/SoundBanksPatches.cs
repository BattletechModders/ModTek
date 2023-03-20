using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using BattleTech;
using BattleTech.UI;
using HBS;
using UnityEngine;

namespace ModTek.Features.CustomSoundBankDefs;

internal static class CustomSoundHelper
{
    private static readonly FieldInfo f_guidIdMap = typeof(WwiseManager).GetField("guidIdMap", BindingFlags.Instance | BindingFlags.NonPublic);

    private static Dictionary<string, uint> guidIdMap(this WwiseManager manager)
    {
        return (Dictionary<string, uint>) f_guidIdMap.GetValue(manager);
    }

    internal static void registerEvents(this SoundBankDef bank)
    {
        bank.loaded = true;
        foreach (var ev in bank.events)
        {
            Log.Main.Debug?.Log("\t\tsound event:" + ev.Key + ":" + ev.Value);
            if (SceneSingletonBehavior<WwiseManager>.Instance.guidIdMap().ContainsKey(ev.Key) == false)
            {
                SceneSingletonBehavior<WwiseManager>.Instance.guidIdMap().Add(ev.Key, ev.Value);
            }
            else
            {
                SceneSingletonBehavior<WwiseManager>.Instance.guidIdMap()[ev.Key] = ev.Value;
            }
        }
    }

    internal static void setVolume(this SoundBankDef bank)
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
[HarmonyPatch("LoadAudioSettings")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(
    new Type[]
    {
    }
)]
internal static class AudioEventManager_LoadAudioSettings
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix()
    {
        Log.Main.Debug?.Log("AudioEventManager.LoadAudioSettings");
        foreach (var soundBank in SoundBanksFeature.soundBanks)
        {
            if (soundBank.Value.loaded != true)
            {
                continue;
            }

            soundBank.Value.setVolume();
        }
    }
}

[HarmonyPatch(typeof(AudioSettingsModule))]
[HarmonyPatch("SaveSettings")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(
    new Type[]
    {
    }
)]
internal static class AudioSettingsModule_SaveSettings
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix(AudioSettingsModule __instance)
    {
        Log.Main.Debug?.Log("AudioSettingsModule.SaveSettings");
        foreach (var soundBank in SoundBanksFeature.soundBanks)
        {
            if (soundBank.Value.loaded != true)
            {
                continue;
            }

            soundBank.Value.setVolume();
        }
    }
}

[HarmonyPatch(typeof(WwiseManager))]
[HarmonyPatch("LoadCombatBanks")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(
    new Type[]
    {
    }
)]
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
[HarmonyPatch("UnloadCombatBanks")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(
    new Type[]
    {
    }
)]
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
[HarmonyPatch("UnloadBank")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(
    new Type[]
    {
    }
)]
internal static class LoadedAudioBank_UnloadBank
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    public static void Postfix(LoadedAudioBank __instance)
    {
        Log.Main.Debug?.Log("LoadedAudioBank.UnloadBank " + __instance.name);
        if (SoundBanksFeature.soundBanks.ContainsKey(__instance.name))
        {
            SoundBanksFeature.soundBanks[__instance.name].loaded = false;
        }
    }
}

[HarmonyPatch(typeof(LoadedAudioBank))]
[HarmonyPatch("LoadBankExternal")]
[HarmonyPatch(MethodType.Normal)]
[HarmonyPatch(
    new Type[]
    {
    }
)]
internal static class LoadedAudioBank_LoadBankExternal
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
    }

    [Obsolete]
    public static bool Prefix(LoadedAudioBank __instance, ref AKRESULT __result, ref uint ___id)
    {
        Log.Main.Debug?.Log("LoadedAudioBank.LoadBankExternal " + __instance.name);
        if (SoundBanksFeature.soundBanks.ContainsKey(__instance.name) == false)
        {
            Log.Main.Warning?.Log($"Soundbank {__instance.name} does not exists");
            return false;
        }
        string filename = SoundBanksFeature.soundBanks[__instance.name].filename;
        if(File.Exists(filename) == false)
        {
            Log.Main.Warning?.Log($"Soundbank {filename} does not exists");
            return false;
        };
        byte[] content = null;
        try
        {
            content = File.ReadAllBytes(filename);
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log($"\tfail to read soundbank from fs\n{e.ToString()}");
        }
        Log.Main.Debug?.Log($"\t'{filename}' loaded. Length: {content.Length}");
        ProcessParameters pparams = null;
        if ((content[0] == 'B') && (content[1] == 'K') && (content[2] == 'H') && (content[3] == 'D'))
        {
            pparams = null;
        }
        else
        {
            pparams = SoundBanksProcessHelper.GetRegisteredProcParams(__instance.name);
        }
        GCHandle? handle = null;
        var dataLength = (uint)content.Length;
        if (pparams != null)
        {
            Log.Main.Debug?.Log("\tfound post-process parameters " + pparams.param1 + " " + pparams.param2);
            byte[] result = null;
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
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(content, 0, content.Length);
                    }

                    result = msEncrypt.ToArray();
                }
                handle = GCHandle.Alloc(result, GCHandleType.Pinned);
                dataLength = (uint)result.Length;
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log($"\tfail to process soundbank from fs\n{e.ToString()}");
                handle = GCHandle.Alloc(content, GCHandleType.Pinned);
                dataLength = (uint)content.Length;
            }
        }
        else
        {
            handle = GCHandle.Alloc(content, GCHandleType.Pinned);
            dataLength = (uint)content.Length;
        }

        if (handle.HasValue == false)
        {
            return false;
        }

        try
        {
            var id = uint.MaxValue;
            __result = AkSoundEngine.LoadBank(handle.Value.AddrOfPinnedObject(), dataLength, out id);
            ___id = id;
            if (__result == AKRESULT.AK_Success)
            {
                SoundBanksFeature.soundBanks[__instance.name].registerEvents();
                SoundBanksFeature.soundBanks[__instance.name].setVolume();
            };
        }
        catch
        {
            __result = AKRESULT.AK_Fail;
        }

        Log.Main.Debug?.Log("\tResult:" + __result + " id:" + ___id + " length:" + dataLength);
        return false;
    }
}