using Newtonsoft.Json;

namespace ModTek.Mods
{
    [JsonObject]
    internal class DataAddendumEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
