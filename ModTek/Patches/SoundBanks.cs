using BattleTech;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.RuntimeLog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using UnityEngine;

namespace ModTek
{
    public enum SoundBankType
    {
        Default,
        Combat,
        Voice
    }

    public class SoundBankDef
    {
        [JsonIgnore]
        public bool loaded { get; set; }

        public string name { get; set; }
        public string filename { get; set; }
        public List<uint> volumeRTPCIds { get; set; }
        public float volumeShift { get; set; }
        public SoundBankType type { get; set; }
        public Dictionary<string, uint> events { get; set; }

        public SoundBankDef()
        {
            events = new Dictionary<string, uint>();
            type = SoundBankType.Default;
            loaded = false;
            volumeRTPCIds = new List<uint>();
            volumeShift = 0f;
        }
    }

    public static class CustomSoundHelper
    {
        private static FieldInfo f_guidIdMap = typeof(WwiseManager).GetField("guidIdMap", BindingFlags.Instance | BindingFlags.NonPublic);

        public static Dictionary<string, uint> guidIdMap(this WwiseManager manager)
        {
            return (Dictionary<string, uint>) f_guidIdMap.GetValue(manager);
        }

        public static void registerEvents(this SoundBankDef bank)
        {
            bank.loaded = true;
            foreach (var ev in bank.events)
            {
                RLog.M.WL(2, "sound event:" + ev.Key + ":" + ev.Value);
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

        public static void setVolume(this SoundBankDef bank)
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
            RLog.M.TWL(0, "SoundBankDef.setVolume " + bank.name);
            foreach (var id in bank.volumeRTPCIds)
            {
                var res = AkSoundEngine.SetRTPCValue(id, volume);
                RLog.M.WL(1, "SetRTPCValue " + id + " " + volume + " result:" + res);
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
    public static class AudioEventManager_LoadAudioSettings
    {
        public static void Postfix()
        {
            RLog.M.TWL(0, "AudioEventManager.LoadAudioSettings");
            foreach (var soundBank in ModTek.soundBanks)
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
    public static class AudioSettingsModule_SaveSettings
    {
        public static void Postfix(AudioSettingsModule __instance)
        {
            RLog.M.TWL(0, "AudioSettingsModule.SaveSettings");
            foreach (var soundBank in ModTek.soundBanks)
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
    public static class WwiseManager_LoadCombatBanks
    {
        public static void Postfix(WwiseManager __instance, ref List<LoadedAudioBank> ___loadedBanks)
        {
            RLog.M.TWL(0, "WwiseManager.LoadCombatBanks");
            foreach (var soundBank in ModTek.soundBanks)
            {
                if (soundBank.Value.type != SoundBankType.Combat)
                {
                    continue;
                }

                if (soundBank.Value.loaded == true)
                {
                    continue;
                }

                RLog.M.WL(1, "Loading:" + soundBank.Key);
                ___loadedBanks.Add(new LoadedAudioBank(soundBank.Key, true, false));
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
    public static class WwiseManager_UnloadCombatBanks
    {
        public static void Postfix(WwiseManager __instance, ref List<LoadedAudioBank> ___loadedBanks)
        {
            RLog.M.TWL(0, "WwiseManager.UnloadCombatBanks");
            foreach (var soundBank in ModTek.soundBanks)
            {
                if (soundBank.Value.type != SoundBankType.Combat)
                {
                    continue;
                }

                if (soundBank.Value.loaded == false)
                {
                    continue;
                }

                var loadedAudioBank = ___loadedBanks.Find((Predicate<LoadedAudioBank>) (x => x.name == soundBank.Value.name));
                if (loadedAudioBank != null)
                {
                    RLog.M.WL(1, "Unloading:" + soundBank.Key);
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
    public static class LoadedAudioBank_UnloadBank
    {
        public static void Postfix(LoadedAudioBank __instance)
        {
            RLog.M.TWL(0, "LoadedAudioBank.UnloadBank " + __instance.name);
            if (ModTek.soundBanks.ContainsKey(__instance.name))
            {
                ModTek.soundBanks[__instance.name].loaded = false;
            }
        }
    }

    public class ProcessParameters
    {
        public string param1 { get; set; }
        public string param2 { get; set; }

        public ProcessParameters(string p1, string p2)
        {
            param1 = p1;
            param2 = p2;
        }
    }

    public static class SoundBanksProcessHelper
    {
        private static Dictionary<string, ProcessParameters> procParams = new();

        public static void RegisterProcessParams(string soundbank, string param1, string param2)
        {
            if (procParams.ContainsKey(soundbank))
            {
                procParams[soundbank] = new ProcessParameters(param1, param2);
            }
            else
            {
                procParams.Add(soundbank, new ProcessParameters(param1, param2));
            }
        }

        public static ProcessParameters GetRegistredProcParams(string soundbank)
        {
            if (procParams.TryGetValue(soundbank, out var p))
            {
                return p;
            }

            return null;
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
    public static class LoadedAudioBank_LoadBankExternal
    {
        [Obsolete]
        public static bool Prefix(LoadedAudioBank __instance, ref AKRESULT __result, ref uint ___id)
        {
            RLog.M.TWL(0, "LoadedAudioBank.LoadBankExternal " + __instance.name);
            if (ModTek.soundBanks.ContainsKey(__instance.name) == false)
            {
                return false;
            }

            var uri = new Uri(ModTek.soundBanks[__instance.name].filename).AbsoluteUri;
            RLog.M.WL(1, uri);
            var www = new WWW(uri);
            while (!www.isDone)
            {
                Thread.Sleep(25);
            }

            RLog.M.WL(1, "'" + uri + "' loaded");
            var pparams = SoundBanksProcessHelper.GetRegistredProcParams(__instance.name);
            GCHandle? handle = null;
            var dataLength = (uint) www.bytes.Length;
            if (pparams != null)
            {
                RLog.M.WL(1, "found post-process parameters " + pparams.param1 + " " + pparams.param2);
                var aes = Aes.Create();
                aes.Mode = CipherMode.CBC;
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.FeedbackSize = 128;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = Convert.FromBase64String(pparams.param1);
                aes.IV = Convert.FromBase64String(pparams.param2);
                var encryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                byte[] result = null;
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(www.bytes, 0, www.bytes.Length);
                    }

                    result = msEncrypt.ToArray();
                }

                handle = GCHandle.Alloc((object) result, GCHandleType.Pinned);
                dataLength = (uint) result.Length;
            }
            else
            {
                handle = GCHandle.Alloc((object) www.bytes, GCHandleType.Pinned);
                dataLength = (uint) www.bytes.Length;
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
                    ModTek.soundBanks[__instance.name].registerEvents();
                    ModTek.soundBanks[__instance.name].setVolume();
                }

                ;
            }
            catch
            {
                __result = AKRESULT.AK_Fail;
            }

            RLog.M.WL(1, "Result:" + __result + " id:" + ___id + " length:" + dataLength);
            return false;
        }
    }
}
