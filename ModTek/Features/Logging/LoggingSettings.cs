using Newtonsoft.Json;

namespace ModTek.Features.Logging
{
    internal class LoggingSettings
    {
        [JsonProperty]
        public string[] PrefixesToIgnore = {};

        [JsonProperty]
        public bool preserveFullLog;

        [JsonProperty]
        public bool skipOriginalLoggers;
    }
}
