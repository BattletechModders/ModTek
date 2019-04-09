using HBS.Logging;
using Newtonsoft.Json;

namespace ModTek.Logging
{
    public class BetterLogSettings
    {
        public bool LogFileEnabled = false;
        public LogLevel LogLevel = LogLevel.Debug;
        public string[] IgnoreMessagePatterns = new string[0];
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public BetterLogFormatter Formatter = null;
    }
}