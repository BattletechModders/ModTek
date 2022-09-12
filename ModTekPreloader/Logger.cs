using System;
using System.IO;

namespace ModTekPreloader
{
    public static class Logger
    {
        private const string LogFileRelativePath = "Mods/.modtek/ModTekPreloader.log";

        internal static DateTime Start { get; private set; }

        public static void Setup(DateTime? start = null)
        {
            if (start == null)
            {
                Start = DateTime.Now;
                Directory.CreateDirectory(Path.GetDirectoryName(LogFileRelativePath));
                File.WriteAllText(LogFileRelativePath, "");
            }
            else
            {
                Start = start.Value;
            }
        }

        public static void Log(object obj)
        {
            File.AppendAllText(LogFileRelativePath, $"{(DateTime.Now-Start).TotalSeconds:00.000} {obj}{Environment.NewLine}");
        }
    }
}
