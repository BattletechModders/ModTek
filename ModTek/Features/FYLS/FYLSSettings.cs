using Newtonsoft.Json;

namespace ModTek.Features.FYLS
{
    internal class FYLSSettings
    {
        [JsonProperty]
        public string[] PrefixesToIgnore = {};

        [JsonProperty]
        public bool preserveFullLog;

        [JsonProperty]
        public bool skipOriginalLoggers;
    }
}
