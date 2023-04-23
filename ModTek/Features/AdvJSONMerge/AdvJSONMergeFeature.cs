using System;
using System.Collections.Generic;
using ModTek.Features.Manifest;
using ModTek.Util;
using Newtonsoft.Json.Linq;

namespace ModTek.Features.AdvJSONMerge;

internal static class AdvJSONMergeFeature
{
    private static bool IsAdvancedJSONMerge(JObject merge)
    {
        return (merge[nameof(AdvancedJSONMerge.TargetID)] != null || merge[nameof(AdvancedJSONMerge.TargetIDs)] != null) && merge[nameof(AdvancedJSONMerge.Instructions)] != null;
    }

    private static void DoAdvancedMerge(JObject target, JObject merge)
    {
        var instructions = merge[nameof(AdvancedJSONMerge.Instructions)].ToObject<List<Instruction>>();
        for (var index = 0; index < instructions.Count; index++)
        {
            var instruction = instructions[index];
            try
            {
                instruction.Process(target);
            }
            catch (Exception e)
            {
                throw new Exception($"The instruction (Index: {index} Action: '{instruction.Action}' JSONPath: '{instruction.JSONPath}') produced an error", e);
            }
        }
    }

    public static void MergeIntoTarget(JObject target, FileVersionTuple mergeVersion)
    {
        try
        {
            var merge = HBSJsonUtils.ParseGameJSONFile(mergeVersion.AbsolutePath);

            if (IsAdvancedJSONMerge(merge))
            {
                DoAdvancedMerge(target, merge);
                return;
            }

            target.Merge(merge, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });
        }
        catch (Exception e)
        {
            throw new Exception($"Error merging {mergeVersion.Path}", e);
        }
    }
}