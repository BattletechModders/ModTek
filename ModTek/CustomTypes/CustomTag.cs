using BattleTech.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModTek.CustomTypes
{
    [JsonObject]
    public class CustomTag
    {
        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public bool Important { get; set; }

        [JsonProperty]
        public bool PlayerVisible { get; set; }

        [JsonProperty]
        public string FriendlyName { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        public override string ToString()
        {
            return $"CustomTag => name: {Name}  important: {Important}  playerVisible: {PlayerVisible}  friendlyName: {FriendlyName}  description: {Description}";
        }

        public Tag_MDD ToTagMDD()
        {
            return new(
                Name, Important, PlayerVisible,
                FriendlyName, Description
            );
        }
    }
}
