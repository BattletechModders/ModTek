using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using ModTek.Common.Utils;
using ModTekPreloader.Injector;

namespace ModTekPreloader.Logging;

// thread safe but not async
internal class Logger
{
    internal static readonly Logger Main = new(Paths.LogFile);

    private readonly string _prefix;
    private readonly StreamWriter _writer;
    internal Logger(string path)
    {
        FileUtils.CreateDirectoryForFile(path);
        if (AppDomain.CurrentDomain.FriendlyName == InjectorsAppDomain.ModTekInjectorsDomainName)
        {
            // TODO move injector code in own project/library and use own log
            _prefix = " [Injectors]";
        }
        else
        {
            FileUtils.RotatePath(path, 1);
        }
        _writer = FileUtils.LogStream(path);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    internal void Log(object obj)
    {
        var line = $"{GetTime()}{_prefix} {obj}";
        lock (this)
        {
            _writer.WriteLine(line);
        }
    }

    private static string GetTime()
    {
        return DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
