using System;
using System.IO;

namespace ModTekPreloader
{
    public static class Logger
    {
        internal static DateTime Start { get; private set; }

        public static void Setup(DateTime? start = null)
        {
            if (start == null)
            {
                Start = DateTime.Now;
                Paths.CreateDirectoryForFile(Paths.LogFile);
                Paths.RotatePath(Paths.LogFile, 1);
                File.WriteAllText(Paths.LogFile, "");
            }
            else
            {
                Start = start.Value;
            }
        }

        public static void Log(object obj)
        {
            File.AppendAllText(Paths.LogFile, $"{(DateTime.Now-Start).TotalSeconds:00.000} {obj}{Environment.NewLine}");
        }
    }
}
