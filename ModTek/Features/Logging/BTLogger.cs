using System;
using System.IO;
using ModTek.Misc;

namespace ModTek.Features.Logging
{
    // TODO integrate all Loggers: BTLogger, MTLogger and RTLog!
    internal static class BTLogger
    {
        private static string CleanLogFilePath => Path.Combine(FilePaths.TempModTekDirectory, "battletech_log.txt");
        private static string FullLogFilePath  => Path.Combine(FilePaths.TempModTekDirectory, "battletech_log_original.txt");

        private static StreamWriter CleanWriter;
        private static StreamWriter FullWriter;

        public static void InitDebugFiles()
        {
            CleanWriter = new StreamWriter(CleanLogFilePath, false);
            CleanWriter.AutoFlush = true;
            CleanWriter.WriteLine($"{DateTime.Now} Cleaned Log");
            CleanWriter.WriteLine(new string('-', 80));
            CleanWriter.WriteLine(VersionInfo.GetFormattedInfo());
            if (FYLSFeature.ModSettings.preserveFullLog)
            {
                FullWriter = new StreamWriter(FullLogFilePath, false);
                FullWriter.AutoFlush = true;
                FullWriter.WriteLine($"{DateTime.Now} Full Log");
                FullWriter.WriteLine(new string('-', 80));
                FullWriter.WriteLine(VersionInfo.GetFormattedInfo());
            }
        }

        public static void Full(String line)
        {
            FullWriter.WriteLine($"{DateTime.Now:s} {line}");
        }

        public static void Clean(String line)
        {
            CleanWriter.WriteLine($"{DateTime.Now:s} {line}");
        }
    }
}
