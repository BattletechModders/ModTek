using System;
using System.IO;
using JetBrains.Annotations;

namespace ModTek
{
    internal static class Logger
    {
        // logging
        internal static string LogPath { get; set; }

        internal static StreamWriter LogStream;

        internal static StreamWriter GetStream()
        {
            if (LogStream == null && !string.IsNullOrEmpty(LogPath))
                LogStream = File.AppendText(LogPath);
            return LogStream;
        }

        [StringFormatMethod("message")]
        internal static void Log(string message, params object[] formatObjects)
        {
            if (string.IsNullOrEmpty(LogPath)) return;

            var stream = GetStream();
            if (stream == null) return;
            
            stream.WriteLine(message, formatObjects);
        }

        [StringFormatMethod("message")]
        internal static void LogWithDate(string message, params object[] formatObjects)
        {
            if (string.IsNullOrEmpty(LogPath)) return;

            var stream = GetStream();
            if (stream == null) return;

            stream.WriteLine(DateTime.Now.ToLongTimeString() + " - " + message, formatObjects);
        }
    }
}
