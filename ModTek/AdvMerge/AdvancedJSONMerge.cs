using System;
using System.Collections.Generic;
using System.IO;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.AdvMerge
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
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<AdvancedJSONMerge>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Logger.LogException($"\tCould not read AdvancedJSONMerge in path: {path}", e);
                return null;
            }
        }
    }
}
