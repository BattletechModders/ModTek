using System;
using System.IO;
using ModTek.Misc;

namespace ModTek.Logging
{
    internal static class Logger
    {
        private static readonly object lockObject = new();

        private static StreamWriter stream;
        internal static void LogInit()
        {
            lock (lockObject)
            {
                if (stream == null)
                {
                    Directory.CreateDirectory(FilePaths.TempModTekDirectory);
                    stream = File.CreateText(FilePaths.LogPath);
                    stream.AutoFlush = true;
                }
            }
        }

        internal static void LogIf(bool condition, string message)
        {
            if (condition)
            {
                Log(message);
            }
        }

        internal static void LogWithDate(string message)
        {
            Log(DateTime.Now.ToLongTimeString() + " - " + message);
        }

        internal static void Log(string message, Exception e)
        {
            lock (lockObject)
            {
                Log(message);
                Log(e.ToString());
            }
        }

        internal static void Log(string message)
        {
            lock (lockObject)
            {
                stream.WriteLine(message);
            }
        }
    }
}
