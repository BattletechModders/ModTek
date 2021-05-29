using System;
using System.IO;
using ModTek.Misc;

namespace ModTek.Logging
{
    internal static class Logger
    {
        private static StreamWriter stream;
        internal static void LogInit()
        {
            if (stream == null)
            {
                Directory.CreateDirectory(FilePaths.TempModTekDirectory);
                stream = File.CreateText(FilePaths.LogPath);
            }
        }

        internal static void LogIf(bool condition, string message)
        {
            if (condition)
            {
                Log(message);
            }
        }

        internal static void Log(string message)
        {
            stream.WriteLine(message);
            stream.Flush();
        }

        internal static void Log(string message, Exception e)
        {
            stream.WriteLine(message);
            stream.WriteLine(e.ToString());
            stream.Flush();
        }

        internal static void Log(string message, params object[] formatObjects)
        {
            stream.WriteLine(message, formatObjects);
            stream.Flush();
        }

        internal static void LogWithDate(string message, params object[] formatObjects)
        {
            Log(DateTime.Now.ToLongTimeString() + " - " + message, formatObjects);
        }
    }
}
