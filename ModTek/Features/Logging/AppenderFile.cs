using System;
using System.Globalization;
using System.IO;
using ModTek.Common.Utils;
using ModTek.Public;

namespace ModTek.Features.Logging;

internal class AppenderFile : IDisposable
{
    private readonly Filters _filters;
    private readonly Formatter _formatter;
    private readonly StreamWriter _writer;

    internal AppenderFile(string path, AppenderSettings settings)
    {
        _filters = new Filters(settings);
        _formatter = new Formatter(settings);

        FileUtils.CreateParentOfPath(path);
        FileUtils.RotatePath(path, settings.LogRotationCount);
        _writer = FileUtils.LogStream(path);
        _writer.WriteLine($"ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
        _writer.WriteLine(DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
        _writer.WriteLine(new string('-', 80));
        _writer.WriteLine(VersionInfo.GetFormattedInfo());
    }

    public void Dispose()
    {
        lock(this)
        {
            _writer?.Dispose();
        }
    }

    internal void Append(MTLoggerMessageDto messageDto)
    {
        if (!_filters.IsIncluded(messageDto))
        {
            return;
        }

        var logLine = _formatter.GetFormattedLogLine(messageDto);

        lock(this)
        {
            _writer.WriteLine(logLine);
        }
    }
}