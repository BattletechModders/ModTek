using System;
using System.Globalization;
using ModTek.Common.Utils;
using UnityEngine;

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
        Write(
            $"""
            ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate})
            {Environment.OSVersion} ; BattleTech {Application.version} ; Unity {Application.unityVersion} ; CLR {Environment.Version} ; {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}"
            {dateTime.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)} {nameof(unityStartupTime)}={unityStartupTime.ToString(null, CultureInfo.InvariantCulture)} {nameof(stopwatchTimestamp)}={stopwatchTimestamp}
            {new string('-', 80)}
            {VersionInfo.GetFormattedInfo()}
            """
        );
    }
    private void Write(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        Write(bytes, bytes.Length);
    }

    internal static readonly MTStopwatch FlushStopWatch = new();
    internal static readonly MTStopwatch FiltersStopWatch = new();
    internal static readonly MTStopwatch FormatterStopWatch = new();
    internal void Append(ref MTLoggerMessageDto messageDto)
    {
        if (messageDto.FlushToDisk)
        {
            FlushStopWatch.Start();
            try
            {
                _writer.FlushToDisk();
            }
            finally
            {
                FlushStopWatch.Stop();
            }
            return;
        }

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
        _writer?.Dispose();
    }
}