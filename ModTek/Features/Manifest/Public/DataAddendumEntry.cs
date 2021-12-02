using Newtonsoft.Json;

// ReSharper disable once CheckNamespace
namespace ModTek
{
    [JsonObject]
    public class DataAddendumEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
