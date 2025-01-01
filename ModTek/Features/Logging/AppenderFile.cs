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
            ModTek v{GitVersionInformation.InformationalVersion} ({GitVersionInformation.CommitDate}) ; HarmonyX {typeof(Harmony).Assembly.GetName().Version}
            {Environment.OSVersion} ; BattleTech {Application.version} ; Unity {Application.unityVersion} ; CLR {Environment.Version} ; {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}"
            {dateTime.ToLocalTime().ToString("o", CultureInfo.InvariantCulture)} ; Startup {unityStartupTime.ToString(null, CultureInfo.InvariantCulture)} ; Ticks {stopwatchTimestamp}
            {new string('-', 80)}

            """
        );
    }
    private void Write(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        _writer.Append(bytes, 0, bytes.Length);
    }

    internal static readonly MTStopwatch FlushStopWatch = new()
    {
        SkipFirstNumberOfMeasurements = 0
    };
    internal static readonly MTStopwatch FiltersStopWatch = new();
    internal static readonly MTStopwatch FormatterStopWatch = new();
    internal static readonly MTStopwatch WriteStopwatch = new();
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

        // using a pool would allow us to implement file async API via Win32 API
        // using thread local instance field would allow to use batching
        var buffer = FastBuffer.GetThreadStaticBufferAndReset();
        FormatterStopWatch.Start();
        try
        {
            using (buffer.PinnedUse())
            {
                _formatter.GetFormattedLogLine(ref messageDto, buffer);
            }
        }
        finally
        {
            FormatterStopWatch.Stop();
        }

        WriteStopwatch.Start();
        try
        {
            var length = buffer.GetBytes(out var bytes);
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