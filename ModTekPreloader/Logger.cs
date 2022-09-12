using System;
using System.IO;

namespace ModTekPreloader
{
    public static class Logger
    {
        private const string LogFileRelativePath = "Mods/.modtek/ModTek.log";
        private static DateTime start;

        public static void Setup()
        {
            start = DateTime.Now;
            Directory.CreateDirectory(Path.GetDirectoryName(LogFileRelativePath));
            File.WriteAllText(LogFileRelativePath, "");
        }

        public static void Log(object obj)
        {
            File.AppendAllText(LogFileRelativePath, $"{(DateTime.Now-start).TotalSeconds:00.000} {obj}{Environment.NewLine}");
        }
    }
}
