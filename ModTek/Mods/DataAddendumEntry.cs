using Newtonsoft.Json;

namespace ModTek.Mods
{
    [JsonObject]
    internal class DataAddendumEntry
    {
        [JsonProperty]
        public string name;

        [JsonProperty]
        public string path;
    }
}
