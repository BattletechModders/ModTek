using System;
using System.Collections.Generic;
using System.IO;
using Harmony;
using HBS;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest.BTRL;
using ModTek.Features.Manifest.MDD;
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

            Log($"Processing sound banks defs");
            if (SceneSingletonBehavior<WwiseManager>.HasInstance == false)
            {
                Log("\tWWise manager not inited");
                yield break;
            }

            var sliderText = "Processing sound banks defs";
            yield return new ProgressReport(0, sliderText, "", true);

            var loadedBanks = Traverse.Create(SceneSingletonBehavior<WwiseManager>.Instance).Field<List<LoadedAudioBank>>("loadedBanks").Value;
            var countCurrent = 0;
            var countMax = (float)entries.Length;
            foreach (var entry in entries)
            {
                yield return new ProgressReport(countCurrent++/countMax, sliderText, entry.Id);
                Log($"\tProcessing {entry.ToShortString()}");
                try
                {
                    var def = LoadDef(entry.FilePath);
                    Log($"\t\tDef: {def.name} ({def.type}): {def.filename}");
                    if (string.IsNullOrEmpty(def.name))
                    {
                        def.name = entry.Id;
                    }
                    else if (def.name != entry.Id)
                    {
                        Log("\t\tError: Name inside def is defined but not equal to the manifest entry id, skipping processing and load.");
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
                    Log("\t\tError" + e);
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
