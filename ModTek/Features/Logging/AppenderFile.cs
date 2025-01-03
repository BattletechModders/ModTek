﻿using System;
using System.Globalization;
using ModTek.Common.Utils;
using UnityEngine;

namespace ModTek.Features.Logging;

internal class AppenderFile : IDisposable
{
    private readonly Filters _filters;
    private readonly Formatter _formatter;
    private readonly LogStream _writer;

    private const int _bufferFlushThreshold = 16 * 1024; // 16kb seems to bring most gains
    private const int _bufferInitialCapacity = _bufferFlushThreshold + 8 * 1024;
    private readonly FastBuffer _buffer = new(_bufferInitialCapacity);

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
    internal static readonly MTStopwatch WriteStopwatch = new() { SkipFirstNumberOfMeasurements = 0 };
    internal void Append(ref MTLoggerMessageDto messageDto)
    {
        if (messageDto.FlushToDisk)
        {
            if (_buffer._length > 0)
            {
                FlushToOS();
            }

            var measurement = FlushStopWatch.StartMeasurement();
            _writer.FlushToDisk();
            measurement.Stop();
            return;
        }

        {
            var measurement = FiltersStopWatch.StartMeasurement();
            var included = _filters.IsIncluded(ref messageDto);
            measurement.Stop();
            if (!included)
            {
                if (!messageDto.HasMore && _buffer._length > 0)
                {
                    FlushToOS();
                }
                return;
            }
        }

        {
            var measurement = FormatterStopWatch.StartMeasurement();
            _formatter.SerializeMessage(ref messageDto, _buffer);
            measurement.Stop();

            if (!messageDto.HasMore || _buffer._length >= _bufferFlushThreshold)
            {
                FlushToOS();
            }
        }
    }

    private void FlushToOS()
    {
        var measurement = WriteStopwatch.StartMeasurement();
        var length = _buffer.GetBytes(out var threadUnsafeBytes);
        _writer.Append(threadUnsafeBytes, 0, length);
        _buffer.Reset();
        measurement.Stop();
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}