using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTek.CustomTypes
{
    [JsonObject]
    public class CustomTagSet
    {
        [JsonProperty]
        public string ID { get; set; }

        [JsonProperty]
        public int TypeID { get; set; }

        [JsonProperty]
        public string[] Tags { get; set; }
    }
}
