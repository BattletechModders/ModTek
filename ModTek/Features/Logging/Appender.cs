using System;
using System.IO;
using System.Threading;
using HBS.Logging;
using ModTek.Util;
using Object = UnityEngine.Object;

namespace ModTek.Features.Logging
{
    internal class Appender : ILogAppender, IDisposable
    {
        private readonly StreamWriter writer;
        private readonly Formatter formatter;

        internal Appender(string path)
        {
            FileUtils.CreateParentOfPath(path);
            writer = new StreamWriter(path) { AutoFlush = true };
            writer.WriteLine($"{DateTime.Now} ModTek v{VersionTools.LongVersion}");
            writer.WriteLine(new string('-', 80));
            writer.WriteLine(VersionInfo.GetFormattedInfo());
        }

        internal Appender(string path, FormatterSettings settings) : this(path)
        {
            formatter = new Formatter(settings);
        }

        public void OnLogMessage(string logName, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            var logLine = formatter.GetFormattedLogLine(logName, level, message, exception, location, Thread.CurrentThread);
            WriteLine(logLine.PrefixLine);
        }

        public void Flush()
        {
            // AutoFlush = true
        }

        public void Dispose()
        {
            lock(this)
            {
                writer?.Dispose();
            }
        }

        internal void WriteLine(string line)
        {
            lock(this)
            {
                writer.WriteLine(line);
            }
        }
    }
}
