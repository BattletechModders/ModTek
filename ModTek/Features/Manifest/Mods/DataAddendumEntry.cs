using Newtonsoft.Json;

namespace ModTek.Features.Manifest.Mods
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
