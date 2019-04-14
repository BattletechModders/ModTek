using HBS.Logging;

namespace ModTek.Logging
{
    public class BetterLogSettings
    {
        public bool Enabled = false;
        public LogLevel Level = LogLevel.Debug;
        public string[] IgnoreMessagePatterns = new string[0];

        public string LineFormat { get; set; } = "{0} [{1}] [{2}] - {3} {4}";
        public bool UseAbsoluteTime = false;
        public string StartupTimeFormat { get; set; } = "{0:D2}:{1:D2}:{2:D2}.{3:D3}";
        public string AbsoluteTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public string LevelFormat { get; set; } = "{0}";
        public string NameFormat { get; set; } = "{0,5}";
        public string MessageFormat { get; set; } = "{0}";
        public string ExceptionFormat { get; set; } = " Exception: {0}";
        public string LocationFormat { get; set; } = " [{0}]";
    }
}
