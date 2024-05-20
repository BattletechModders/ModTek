using System;
using System.Globalization;
using System.IO;
using ModTek.Common.Utils;

namespace ModTek.Common.Logging;

// thread safe but not async
internal class SimpleLogger
{
    private readonly StreamWriter _writer;
    internal SimpleLogger(string path)
    {
        FileUtils.CreateDirectoryForFile(path);
        FileUtils.RotatePath(path, 1);
        _writer = FileUtils.LogStream(path);
    }

    internal void Log(object obj)
    {
        var line = $"{GetTime()} {obj}";
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
