using Newtonsoft.Json;

namespace ModTek.Features.Logging
{
    internal class FormatterSettings
    {
        [JsonProperty]
        public bool IndentNewLines = true;

        [JsonProperty]
        public bool NormalizeNewLines = true;

        [JsonProperty]
        public bool UseAbsoluteTime = false;

        [JsonProperty]
        public string FormatTimeAndLine { get; set; } = "{0} {1}";

        [JsonProperty]
        public string FormatStartupTime { get; set; } = "{1:D2}:{2:D2}.{3:D3}";

        [JsonProperty]
        public string FormatAbsoluteTime { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

        [JsonProperty]
        public string FormatLine { get; set; } = "{0} [{1}] {2}{3}";

        [JsonProperty]
        public string FormatException { get; set; } = ": {0}";

        [JsonProperty]
        public string FormatLocation { get; set; } = "";
    }
}