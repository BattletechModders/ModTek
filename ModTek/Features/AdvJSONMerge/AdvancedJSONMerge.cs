using System;
using System.Collections.Generic;
using ModTek.Features.Logging;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Features.AdvJSONMerge
{
    internal class AdvancedJSONMerge
    {
        [JsonProperty]
        public string TargetID;
        [JsonProperty]
        public List<string> TargetIDs;
        [JsonProperty]
        public string TargetType;

        [JsonProperty(Required = Required.Always)]
        public List<Instruction> Instructions;

        public static AdvancedJSONMerge FromFile(string path)
        {
            try
            {
                var objCache = HBSJsonUtils.ParseGameJSONFile(path);
                return objCache.ToObject<AdvancedJSONMerge>();
            }
            catch (Exception e)
            {
                MTLogger.Warning.Log($"\tCould not read AdvancedJSONMerge in path: {path}", e);
                return null;
            }
        }
    }
}
