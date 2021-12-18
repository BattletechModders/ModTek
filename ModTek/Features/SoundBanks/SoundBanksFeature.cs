using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Harmony;
using HBS;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest;
using ModTek.Features.Manifest.BTRL;
using ModTek.UI;
using Newtonsoft.Json;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.SoundBanks
{
    internal static class SoundBanksFeature
    {
        internal static readonly Dictionary<string, SoundBankDef> soundBanks = new Dictionary<string, SoundBankDef>();

        internal static IEnumerator<ProgressReport> SoundBanksProcessing()
        {
            var entries = BetterBTRL.Instance.AllEntriesOfType(InternalCustomResourceType.SoundBankDef.ToString());
            if (entries.Length == 0)
            {
                yield break;
            }

            Log($"Processing sound banks defs ({entries.Length}):");
            if (SceneSingletonBehavior<WwiseManager>.HasInstance == false)
            {
                Log("\tWWise manager not inited");
                yield break;
            }

            var sliderText = "Loading sound banks defs";
            yield return new ProgressReport(0, sliderText, "", true);

            var loadedBanks = Traverse.Create(SceneSingletonBehavior<WwiseManager>.Instance).Field<List<LoadedAudioBank>>("loadedBanks").Value;
            var countCurrent = 0;
            var countMax = (float)entries.Length;
            foreach (var entry in entries)
            {
                yield return new ProgressReport(countCurrent++/countMax, sliderText, entry.Id);
                Log($"\tProcessing {entry}");
                try
                {
                    var def = LoadDef(entry.FilePath);
                    Log($"\t\t{def.name}:{def.filename}:{def.type}");
                    if (soundBanks.ContainsKey(def.name))
                    {
                        Log("\t\tWarning replacing existing SoundBankDef");
                    }
                    soundBanks[def.name] = def;
                }
                catch (Exception e)
                {
                    Log("\t\tError" + e);
                }
            }

            sliderText = "Loading sound banks";
            yield return new ProgressReport(0, sliderText, "", true);

            // need to loop twice since entry.Id is not the actual id, but def.name is
            countCurrent = 0;
            countMax = soundBanks.Count;
            foreach (var kv in soundBanks)
            {
                var name = kv.Key;
                var def = kv.Value;

                yield return new ProgressReport(countCurrent++/countMax, sliderText, name);
                Log($"\tLoading {def.name}:{def.filename}:{def.type}");
                if (def.type != SoundBankType.Default)
                {
                    continue;
                }
                if (def.loaded)
                {
                    continue;
                }

                loadedBanks.Add(new LoadedAudioBank(def.name, true));
            }
        }

        private static SoundBankDef LoadDef(string path)
        {
            var def = JsonConvert.DeserializeObject<SoundBankDef>(File.ReadAllText(path));
            def.filename = Path.Combine(Path.GetDirectoryName(path), def.filename);
            return def;
        }
    }
}
