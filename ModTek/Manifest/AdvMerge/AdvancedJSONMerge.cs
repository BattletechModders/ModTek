using System;
using System.Collections.Generic;
using ModTek.Logging;
using ModTek.Misc;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Manifest.AdvMerge
{
    internal class AdvancedJSONMerge
    {
        public string TargetID;
        public List<string> TargetIDs;
        public string TargetType;

        [JsonProperty(Required = Required.Always)]
        public List<Instruction> Instructions;

        public static AdvancedJSONMerge FromFile(string path)
        {
            try
            {
                var objCache = JsonUtils.ParseGameJSON(path);
                return objCache.ToObject<AdvancedJSONMerge>();
            }
            catch (Exception e)
            {
                Logger.LogException($"\tCould not read AdvancedJSONMerge in path: {path}", e);
                return null;
            }
        }
    }
}
