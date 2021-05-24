using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HBS;
using ModTek.UI;
using Newtonsoft.Json;
using static ModTek.Util.Logger;

namespace ModTek.SoundBanks
{
    internal static class SoundBanksFeature
    {
        internal static Dictionary<string, SoundBankDef> soundBanks = new();

        internal static void AddSoundBankDef(string path)
        {
            try
            {
                Log($"\tAdd SoundBankDef {path}");
                var def = JsonConvert.DeserializeObject<SoundBankDef>(File.ReadAllText(path));
                def.filename = Path.Combine(Path.GetDirectoryName(path), def.filename);
                if (soundBanks.ContainsKey(def.name))
                {
                    soundBanks[def.name] = def;
                    Log($"\t\tReplace:" + def.name);
                }
                else
                {
                    soundBanks.Add(def.name, def);
                    Log($"\t\tAdd:" + def.name);
                }
            }
            catch (Exception e)
            {
                Log($"\tError while reading SoundBankDef:" + e);
            }
        }

        internal static IEnumerator<ProgressReport> SoundBanksProcessing()
        {
            Log($"Processing sound banks ({SoundBanksFeature.soundBanks.Count}):");
            if (SceneSingletonBehavior<WwiseManager>.HasInstance == false)
            {
                Log($"\tWWise manager not inited");
                yield break;
            }

            yield return new ProgressReport(0, "Processing sound banks", "");
            if (SoundBanksFeature.soundBanks.Count == 0)
            {
                yield break;
            }

            var loadedBanks = (List<LoadedAudioBank>) typeof(WwiseManager).GetField("loadedBanks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(SceneSingletonBehavior<WwiseManager>.Instance);
            var progeress = 0;
            foreach (var soundBank in SoundBanksFeature.soundBanks)
            {
                ++progeress;
                yield return new ProgressReport(progeress / (float) SoundBanksFeature.soundBanks.Count, $"Processing sound bank", soundBank.Key, true);
                Log($"\t{soundBank.Key}:{soundBank.Value.filename}:{soundBank.Value.type}");
                if (soundBank.Value.type != SoundBankType.Default)
                {
                    continue;
                }

                ;
                if (soundBank.Value.loaded)
                {
                    continue;
                }

                loadedBanks.Add(new LoadedAudioBank(soundBank.Key, true, false));
            }
        }
    }
}
