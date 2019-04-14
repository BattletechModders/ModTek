using System;
using System.IO;
using HBS.Logging;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    internal class BetterLog : ILogAppender, IDisposable
    {
        private readonly StreamWriter streamWriter;
        private readonly BetterLogSettings logSettings;


        public BetterLog(string path, BetterLogSettings settings)
        {
            streamWriter = new StreamWriter(path) { AutoFlush = true };
            logSettings = settings;

            streamWriter.WriteLine($"{DateTime.Now} -- {ModTek.GetRelativePath(path, ModTek.ModsDirectory)}");
            streamWriter.WriteLine(new string('-', 80));
            streamWriter.WriteLine(VersionInfo.GetFormattedInfo());
        }

        public void OnLogMessage(string logName, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            var messageString = message?.ToString();
            if (string.IsNullOrEmpty(messageString))
                return;

            // TODO: filter with a pattern as opposed to starts with
            if (logSettings.IgnoreMessagePatterns.Any(str => messageString.StartsWith(str)))
                return;

            var formatted = FormatLogMessage(logName, level, messageString, exception, location);
            var indented = formatted.Replace("\n", "\n\t");
            streamWriter.WriteLine(indented);
        }

        public void Dispose()
        {
            streamWriter?.Dispose();
        }


        internal static ILog SetupModLog(string path, string name, BetterLogSettings settings)
        {
            if (!settings.Enabled)
                return null;

            var log = HBS.Logging.Logger.GetLogger(name);
            HBS.Logging.Logger.AddAppender(name, new BetterLog(path, settings));
            HBS.Logging.Logger.SetLoggerLevel(name, settings.Level);

            return log;
        }


        // FORMATTING
        private string FormatLogMessage (string logName, LogLevel level, string message, Exception exception, IStackTrace location)
        {
            // TODO: format log message
            return string.Format(logSettings.LineFormat,
                GetFormattedTime(),
                string.Format(logSettings.LevelFormat, GetLogLevelString(level)),
                string.Format(logSettings.NameFormat, logName),
                string.Format(logSettings.MessageFormat, message),
                exception == null ? GetFormattedLocation(location) : string.Format(logSettings.ExceptionFormat, exception)
            );
        }

        private string GetFormattedTime()
        {
            return logSettings.UseAbsoluteTime ? GetFormattedAbsoluteTime() : GetFormattedStartupTime();
        }

        private string GetFormattedAbsoluteTime()
        {
            return DateTime.UtcNow.ToString(logSettings.AbsoluteTimeFormat, System.Globalization.CultureInfo.InvariantCulture);
        }

        private string GetFormattedStartupTime()
        {
            var timeSinceStart = TimeSpan.FromSeconds(Time.realtimeSinceStartup);

            return string.Format(logSettings.StartupTimeFormat,
                timeSinceStart.Hours,
                timeSinceStart.Minutes,
                timeSinceStart.Seconds,
                timeSinceStart.Milliseconds);
        }

        private string GetLogLevelString(LogLevel level)
        {
            return Enum.GetName(typeof(LogLevel), level)?.ToUpper();
        }

        private string GetFormattedLocation(IStackTrace location)
        {
            if (!HBS.Logging.Logger.IsStackTraceEnabled || location == null || location.FrameCount < 1)
                return null;

            return string.Format(logSettings.LocationFormat, location.GetFrame(0).Method);
        }
    }
}
