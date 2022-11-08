using System;
using System.Collections.Generic;
using System.IO;
using Harmony;
using HBS;
using ModTek.Features.CustomResources;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
using ModTek.UI;
using Newtonsoft.Json;

namespace ModTek.Features.CustomSoundBankDefs
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

            MTLogger.Info.Log($"Processing sound banks defs");
            if (SceneSingletonBehavior<WwiseManager>.HasInstance == false)
            {
                MTLogger.Warning.Log("\tWWise manager not inited");
                yield break;
            }

            var sliderText = "Processing sound banks defs";
            yield return new ProgressReport(0, sliderText, "", true);

            var loadedBanks = SceneSingletonBehavior<WwiseManager>.Instance.loadedBanks;
            var countCurrent = 0;
            var countMax = (float)entries.Length;
            foreach (var entry in entries)
            {
                yield return new ProgressReport(countCurrent++/countMax, sliderText, entry.Id);
                MTLogger.Info.Log($"\tProcessing {entry.ToShortString()}");
                try
                {
                    var def = LoadDef(entry.FilePath);
                    MTLogger.Debug.Log($"\t\tDef: {def.name} ({def.type}): {def.filename}");
                    if (string.IsNullOrEmpty(def.name))
                    {
                        def.name = entry.Id;
                    }
                    else if (def.name != entry.Id)
                    {
                        MTLogger.Warning.Log("\t\tName inside def is defined but not equal to the manifest entry id, skipping processing and load.");
                        continue;
                    }
                    soundBanks[def.name] = def;
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
                catch (Exception e)
                {
                    MTLogger.Error.Log("\t\tFailed processing", e);
                }
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
