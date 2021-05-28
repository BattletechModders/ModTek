using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest.AdvMerge
{
    internal static class JSONMerger
    {
        private static bool IsAdvancedJSONMerge(JObject merge)
        {
            return (merge[nameof(AdvancedJSONMerge.TargetID)] != null || merge[nameof(AdvancedJSONMerge.TargetIDs)] != null) && merge[nameof(AdvancedJSONMerge.Instructions)] != null;
        }

        private static void DoAdvancedMerge(JObject target, JObject merge)
        {
            var instructions = merge[nameof(AdvancedJSONMerge.Instructions)].ToObject<List<Instruction>>();
            foreach (var instruction in instructions)
            {
                if (!instruction.Process(target))
                {
                    Log($"Warning: An instruction (Action: '{instruction.Action}' JSONPath: '{instruction.JSONPath}') did not perform anything.");
                }
            }
        }

        public static void MergeIntoTarget(JObject target, JObject merge)
        {
            if (IsAdvancedJSONMerge(merge))
            {
                DoAdvancedMerge(target, merge);
                return;
            }

            target.Merge(merge, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
        }
    }
}
