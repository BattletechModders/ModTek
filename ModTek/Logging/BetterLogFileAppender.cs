using System;
using System.IO;
using System.Linq;
using HBS.Logging;
using Object = UnityEngine.Object;

namespace ModTek.Logging
{
    internal class BetterLogFileAppender : ILogAppender, IDisposable
    {
        private readonly StreamWriter writer;
        private readonly BetterLogFormatter formatter;
        private readonly string[] patternsToIgnore;

        public BetterLogFileAppender(string path, BetterLogFormatter formatter = null, string[] patternsToIgnore = null)
        {
            this.formatter = formatter ?? new BetterLogFormatter();
            this.patternsToIgnore = patternsToIgnore ?? new string[0];
            writer = new StreamWriter(path) { AutoFlush = true };
        }

        public void OnLogMessage(string name, LogLevel logLevel, object message, Object context, Exception exception, IStackTrace location)
        {
            var text = message?.ToString();
            if (!string.IsNullOrEmpty(text) && patternsToIgnore.Any(text.StartsWith))
            {
                return;
            }
            var line = formatter.GetFormattedLogLine(name, logLevel, text, context, exception, location);
            writer.WriteLine(line);
        }

        public void Dispose()
        {
            writer?.Dispose();
        }
    }
}