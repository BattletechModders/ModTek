using System;
using System.Globalization;
using ModTek.Common.Utils;

namespace ModTek.Features.Logging;

internal class AppenderFile : IDisposable
{
    private readonly Filters _filters;
    private readonly Formatter _formatter;
    private readonly LogStream _writer;

    internal AppenderFile(string path, AppenderSettings settings)
    {
        _filters = new Filters(settings);
        _formatter = new Formatter(settings);

        FileUtils.CreateParentOfPath(path);
        FileUtils.RotatePath(path, settings.LogRotationCount);
        _writer = new LogStream(path);

        MTLoggerMessageDto.GetTimings(out var stopwatchTimestamp, out var dateTime, out var unityStartupTime);
        Write(System.Text.Encoding.UTF8.GetBytes(
            $"""
            ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})
            {dateTime.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)} {nameof(unityStartupTime)}={unityStartupTime.ToString(null, CultureInfo.InvariantCulture)} {nameof(stopwatchTimestamp)}={stopwatchTimestamp}
            {new string('-', 80)}
            {VersionInfo.GetFormattedInfo()}
            """
        ));
    }
    private void Write(byte[] bytes)
    {
        Write(bytes, bytes.Length);
    }

    internal static readonly MTStopwatch FiltersStopWatch = new();
    internal static readonly MTStopwatch FormatterStopWatch = new();
    internal void Append(ref MTLoggerMessageDto messageDto)
    {
        FiltersStopWatch.Start();
        try
        {
            if (!_filters.IsIncluded(ref messageDto))
            {
                return;
            }
        }
        finally
        {
            FiltersStopWatch.Stop();
        }

        byte[] logBytes;
        int length;
        FormatterStopWatch.Start();
        try
        {
            length = _formatter.GetFormattedLogLine(ref messageDto, out logBytes);
        }
        finally
        {
            FormatterStopWatch.Stop();
        }

        Write(logBytes, length);
    }

    internal static readonly MTStopwatch WriteStopwatch = new();
    private void Write(byte[] bytes, int length)
    {
        WriteStopwatch.Start();
        try
        {
            _writer.Append(bytes, 0, length);
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