using System;
using System.Linq;
using HBS.Logging;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    internal class CleanedLog : BetterLog
    {
        private readonly string[] ignoreMessagePatterns;

        public CleanedLog(string path, CleanedLogSettings settings) : base(path, settings)
        {
            ignoreMessagePatterns = settings.IgnoreMessagePatterns;
        }

        public override void OnLogMessage(string logName, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            // this check makes sure not to write debug information into the cleaned log if so configured
            // we don't want this check for the mod loggers, as those log levels could be manipulated outside of the BetterLogSettings
            if (level < LogSettings.Level)
                return;
            
            var messageString = message?.ToString();
            if (string.IsNullOrEmpty(messageString))
                return;

            if (ignoreMessagePatterns.Any(str => messageString.StartsWith(str)))
                return;

            base.OnLogMessage(logName, level, message, context, exception, location);
        }
    }
}
