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
        const int BufferSize = 128 * 1024;
        _writer = new FileStream(
            path,
            FileMode.Append,
            FileAccess.Write,
            FileShare.ReadWrite|FileShare.Delete,
            BufferSize,
            FileOptions.None
        );

        MTLoggerMessageDto.GetTimings(out var stopwatchTimestamp, out var dateTime, out var unityStartupTime);
        Write(
            $"""
            ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})
            {dateTime.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)} {nameof(unityStartupTime)}={unityStartupTime.ToString(null, CultureInfo.InvariantCulture)} {nameof(stopwatchTimestamp)}={stopwatchTimestamp}
            {new string('-', 80)}
            {VersionInfo.GetFormattedInfo()}
            """
        );
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
    private void Write(string text)
    {
        GetBytesStopwatch.Start();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        GetBytesStopwatch.Stop();
        Write(bytes);
    }

    internal static readonly MTStopwatch WriteStopwatch = new();
    private void Write(byte[] bytes)
    {
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