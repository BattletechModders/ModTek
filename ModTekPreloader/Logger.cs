using System;
using System.IO;

namespace ModTekPreloader
{
    public static class Logger
    {
        private const string LogFileRelativePath = "Mods/.modtek/ModTek.log";

        public static void Reset()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFileRelativePath));
            File.WriteAllText(LogFileRelativePath, "");
        }

        public static void Log(object obj)
        {
            File.AppendAllText(LogFileRelativePath, obj + Environment.NewLine);
        }
    }
}
