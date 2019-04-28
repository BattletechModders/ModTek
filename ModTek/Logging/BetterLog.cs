using System;
using System.IO;
using HBS.Logging;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    internal class BetterLog : ILogAppender, IDisposable
    {
        protected readonly BetterLogSettings LogSettings;
        private readonly StreamWriter streamWriter;
        private readonly BetterLogFormatter formatter;

        public BetterLog(string path, BetterLogSettings settings)
        {
            LogSettings = settings;
            streamWriter = new StreamWriter(path) { AutoFlush = true };
            formatter = new BetterLogFormatter(settings.Formatter);

            streamWriter.WriteLine($"{formatter.GetFormattedAbsoluteTime()} -- {ModTek.GetRelativePath(path, ModTek.ModsDirectory)}");
            streamWriter.WriteLine(new string('-', 80));
            streamWriter.WriteLine(VersionInfo.GetFormattedInfo());
        }

        public virtual void OnLogMessage(string logName, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            var formatted = formatter.GetFormattedLogLine(logName, level, message, context, exception, location);
            streamWriter.WriteLine(formatted);
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
    }
}
