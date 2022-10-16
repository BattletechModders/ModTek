using System;
using System.IO;
using ModTekPreloader.Injector;

namespace ModTekPreloader.Logging
{
    internal static class Logger
    {
        private static readonly string Prefix = AppDomain.CurrentDomain.FriendlyName == InjectorsAppDomain.ModTekInjectorsDomainName ? $" [Injectors]" : "" ;
        internal static void Setup()
        {
            Paths.CreateDirectoryForFile(Paths.LogFile);
            Paths.RotatePath(Paths.LogFile, 1);
            File.WriteAllText(Paths.LogFile, "");
        }

        internal static void Log(object obj)
        {
            File.AppendAllText(Paths.LogFile, $"{GetTime()}{Prefix} {obj}{Environment.NewLine}");
        }

        private static string GetTime()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
