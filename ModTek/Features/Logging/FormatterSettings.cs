using Newtonsoft.Json;

namespace ModTek.Features.Logging
{
    internal class FormatterSettings
    {
        [JsonProperty]
        internal bool IndentNewLines = true;
        [JsonProperty]
        internal const string IndentNewLines_Description = "If a log message with newlines inside is being logged, prefix every line with a tab character.";

        [JsonProperty]
        internal bool NormalizeNewLines = true;
        [JsonProperty]
        internal const string NormalizeNewLines_Description = "Makes sure all newline characters ([\\r\\n]) are converted to the OS preferred newline style.";

        [JsonProperty]
        internal bool UseAbsoluteTime;
        [JsonProperty]
        internal const string UseAbsoluteTime_Description = "Use the absolute time format instead of the relative time format." +
            " Absolute time is useful when using mods that do not use HBS logging and use absolute time for their logging.";

        [JsonProperty]
        internal string FormatTimeAndLine { get; set; } = "{0} {1}";

        [JsonProperty]
        internal string FormatStartupTime { get; set; } = "{1:D2}:{2:D2}.{3:D3}";
        [JsonProperty]
        internal const string FormatStartupTime_Description = "Runs through string.Format, arguments in order are: Hours, Minutes, Seconds, Milliseconds.";

        [JsonProperty]
        internal string FormatAbsoluteTime { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        [JsonProperty]
        internal const string FormatAbsoluteTime_Description = "Runs through DateTime.ToString .";

        [JsonProperty]
        internal string FormatLine { get; set; } = "{0} [{1}] {2}{3}";
        [JsonProperty]
        internal const string FormatLine_Description = "Arguments in order are: logger name, log level, message, (exception or location).";

        [JsonProperty]
        internal string FormatException { get; set; } = ": {0}";
        [JsonProperty]
        internal const string FormatException_Description = "The only argument is the exception converted to a string.";

        [JsonProperty]
        internal string FormatLocation { get; set; } = "";
        [JsonProperty]
        internal const string FormatLocation_Description = "Arguments in order are: DeclaringType of the caller, method name of the caller";
    }
}