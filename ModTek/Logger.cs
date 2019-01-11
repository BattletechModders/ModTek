using System;
using System.IO;
using JetBrains.Annotations;

namespace ModTek
{
    internal static class Logger
    {
        internal static string LogPath { get; set; }
        private static StreamWriter LogStream;

        private static StreamWriter GetOrCreateStream()
        {
            if (LogStream == null && !string.IsNullOrEmpty(LogPath))
                LogStream = File.AppendText(LogPath);

            return LogStream;
        }

        internal static void CloseLogStream()
        {
            if (LogStream == null)
                return;

            LogStream.Dispose();
            LogStream = null;
        }

        [StringFormatMethod("message")]
        internal static void Log(string message, params object[] formatObjects)
        {
            var stream = GetOrCreateStream();
            if (stream == null)
                return;

            stream.WriteLine(message, formatObjects);
        }

        [StringFormatMethod("message")]
        internal static void LogWithDate(string message, params object[] formatObjects)
        {
            var stream = GetOrCreateStream();
            if (stream == null)
                return;

            stream.WriteLine(DateTime.Now.ToLongTimeString() + " - " + message, formatObjects);
        }
    }
}
