namespace ModTek.Logging
{
    public class BetterLogFormatterSettings
    {
        public string LogLineFormat { get; set; } = "{0} [{1}] {2} {3}{4}";
        public bool IndentNewLines = true;
        public bool UseAbsoluteTime = false;
        public string StartupTimeFormat { get; set; } = "{1:D2}:{2:D2}.{3:D3}";
        public string AbsoluteTimeFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public string LogLevelFormat { get; set; } = "{0,-5}";
        public string NameFormat { get; set; } = "{0,5}";
        public string MessageFormat { get; set; } = "{0}";
        public string ExceptionFormat { get; set; } = " Exception: {0}";
        public string LocationFormat { get; set; } = " [{0}.{1}]";
    }
}