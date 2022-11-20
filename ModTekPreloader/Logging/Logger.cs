using System;
using System.Globalization;
using System.IO;
using ModTekPreloader.Injector;

namespace ModTekPreloader.Logging;

internal class Logger
{
    internal static readonly Logger Main = new(Paths.LogFile)
    {
        Prefix = AppDomain.CurrentDomain.FriendlyName == InjectorsAppDomain.ModTekInjectorsDomainName ? " [Injectors]" : ""
    };

    internal string Prefix { get; set; }
    private readonly string _path;
    internal Logger(string path)
    {
        _path = path;
    }

    internal void Rotate()
    {
        Paths.CreateDirectoryForFile(_path);
        Paths.RotatePath(_path, 1);
        File.WriteAllText(_path, "");
    }

    internal void Log(object obj)
    {
        File.AppendAllText(_path, $"{GetTime()}{Prefix} {obj}{Environment.NewLine}");
    }

    private static string GetTime()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
