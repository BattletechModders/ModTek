using System;
using System.IO;

namespace ModTekPreloader.Logging
{
    internal static class Logger
    {
        internal static void Setup()
        {
            Paths.CreateDirectoryForFile(Paths.LogFile);
            Paths.RotatePath(Paths.LogFile, 1);
            File.WriteAllText(Paths.LogFile, "");
        }

        internal static void Log(object obj)
        {
            File.AppendAllText(Paths.LogFile, $"{GetTime()} {obj}{Environment.NewLine}");
        }

        private static string GetTime()
        {
            return DateTime.Now.ToString("hh:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
