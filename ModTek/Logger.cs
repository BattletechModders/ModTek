using System;
using System.IO;
using JetBrains.Annotations;

namespace ModTek
{
    internal static class Logger
    {
        internal static string LogPath { get; set; }
        private static StreamWriter logStream;

        private static StreamWriter GetOrCreateStream()
        {
            if (logStream == null && !string.IsNullOrEmpty(LogPath))
                logStream = File.AppendText(LogPath);

            return logStream;
        }

        internal static void CloseLogStream()
        {
            logStream?.Dispose();
            logStream = null;
        }

        internal static void FlushLogStream()
        {
            logStream?.Flush();
        }

        internal static void Log(string message)
        {
            var stream = GetOrCreateStream();
            stream?.WriteLine(message);
        }

        [StringFormatMethod("message")]
        internal static void Log(string message, params object[] formatObjects)
        {
            var stream = GetOrCreateStream();
            stream?.WriteLine(message, formatObjects);
        }

        [StringFormatMethod("message")]
        internal static void LogWithDate(string message, params object[] formatObjects)
        {
            var stream = GetOrCreateStream();
            stream?.WriteLine(DateTime.Now.ToLongTimeString() + " - " + message, formatObjects);
        }

        internal static void LogException(string message, Exception e)
        {
            var stream = GetOrCreateStream();
            if (stream == null)
                return;

            stream.WriteLine(message);
            stream.WriteLine(e.ToString());
            FlushLogStream();
        }
    }
}
