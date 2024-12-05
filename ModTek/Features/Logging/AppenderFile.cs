using System;
using System.Globalization;
using System.IO;
using ModTek.Common.Utils;

namespace ModTek.Features.Logging;

internal class AppenderFile : IDisposable
{
    private readonly Filters _filters;
    private readonly Formatter _formatter;
    private readonly FileStream _writer;

    internal AppenderFile(string path, AppenderSettings settings)
    {
        _filters = new Filters(settings);
        _formatter = new Formatter(settings);

        FileUtils.CreateParentOfPath(path);
        FileUtils.RotatePath(path, settings.LogRotationCount);
        const int BufferSize = 1 << 24; // 16MB
        _writer = new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite|FileShare.Delete,
            BufferSize,
            FileOptions.None
        );
        WriteLine($"ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})");
        WriteLine(DateTimeOffset.Now.ToString("o", CultureInfo.InvariantCulture));
        WriteLine(new string('-', 80));
        WriteLine(VersionInfo.GetFormattedInfo());
    }

    private void WriteLine(string line)
    {
        Write(line + Environment.NewLine);
    }

    internal static readonly MTStopwatch FiltersStopWatch = new();
    internal static readonly MTStopwatch FormatterStopWatch = new();
    internal void Append(MTLoggerMessageDto messageDto)
    {
        FiltersStopWatch.Start();
        try
        {
            if (!_filters.IsIncluded(messageDto))
            {
                return;
            }
        }
        finally
        {
            FiltersStopWatch.Stop();
        }

        string logLine;
        FormatterStopWatch.Start();
        try
        {
            logLine = _formatter.GetFormattedLogLine(messageDto);
        }
        finally
        {
            FormatterStopWatch.Stop();
        }

        Write(logLine);
    }

    internal static readonly MTStopwatch GetBytesStopwatch = new();
    internal static readonly MTStopwatch WriteStopwatch = new();
    private void Write(string text)
    {
        GetBytesStopwatch.Start();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        GetBytesStopwatch.Stop();

        WriteStopwatch.Start();
        try
        {
            lock(this)
            {
                _writer.Write(bytes, 0, bytes.Length);
                _writer.Flush();
            }
        }
        finally
        {
            WriteStopwatch.Stop();
        }
    }

    public void Dispose()
    {
        lock(this)
        {
            _writer?.Dispose();
        }
    }
}