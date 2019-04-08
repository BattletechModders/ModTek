using HBS.Logging;

namespace ModTek.Logging
{
    public class BetterLogSettings
    {
        public bool LogFileEnabled = false;
        public LogLevel LogLevel = LogLevel.Debug;
        public BetterLogFormatter Formatter = new BetterLogFormatter();
        public string[] PatternsToIgnore = null;
    }
}