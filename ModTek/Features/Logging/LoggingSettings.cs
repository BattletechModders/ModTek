using Newtonsoft.Json;

namespace ModTek.Features.Logging
{
    internal class LoggingSettings
    {
        [JsonProperty]
        public string[] PrefixesToIgnore = {};

        [JsonProperty]
        public bool PreserveFullLog;

        [JsonProperty]
        public bool IgnoreLoggerLogLevel;

        [JsonProperty]
        public bool SkipOriginalLoggers;

        [JsonProperty]
        public string[] IgnoreSkipForLoggers = {"ModTek"};

        [JsonProperty]
        public FormatterSettings ModTekLogFormatting = new()
        {
            FormatLine = "{2}{3}" // log level is always LOG and we know its ModTek
        };

        [JsonProperty]
        public FormatterSettings BattleTechLogFormatting = new();
    }
}
