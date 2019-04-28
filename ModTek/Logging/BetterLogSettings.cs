using HBS.Logging;

namespace ModTek.Logging
{
    public class BetterLogSettings
    {
        public bool Enabled = false;
        public LogLevel Level = LogLevel.Debug;

        public BetterLogFormatterSettings Formatter = new BetterLogFormatterSettings();
    }
}
