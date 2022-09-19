using System;
using System.IO;

namespace ModTekPreloader.Logging
{
    internal static class Logger
    {
        private static readonly DateTime start;

        static Logger()
        {
            start = DateTime.Now;
            Paths.CreateDirectoryForFile(Paths.LogFile);
            Paths.RotatePath(Paths.LogFile, 1);
            File.WriteAllText(Paths.LogFile, "");
        }

        internal static void Log(object obj)
        {
            File.AppendAllText(Paths.LogFile, $"{(DateTime.Now-start).TotalSeconds:00.000} {obj}{Environment.NewLine}");
        }
    }
}
