using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using HBS.Logging;
using ModTek.Misc;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Logging;

internal static class LoggingFeature
{
    private static LoggingSettings _settings;

    private static AppenderUnityConsole _consoleLog;
    private static AppenderFile _mainLog;
    private static AppenderFile[] _logsAppenders = [];

    private static MTLoggerAsyncQueue _queue;

    internal static void Init()
    {
        _settings = ModTek.Config.Logging;

        Directory.CreateDirectory(FilePaths.TempModTekDirectory);

        AppenderUnityConsole.SetupUnityLogHandler();
        _consoleLog = _settings.UnityConsoleAppenderEnabled ? new AppenderUnityConsole(_settings.UnityConsoleAppender) : null;

        {
            var mainLogPath =  Path.Combine(FilePaths.TempModTekDirectory, _settings.MainLogFilePath);
            _mainLog = new AppenderFile(mainLogPath, _settings.MainLog);
        }
        AddAppenders(FilePaths.TempModTekDirectory, _settings.Logs);

        if (_settings.LogUncaughtExceptions)
        {
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                const string message = "UnhandledException";
                if (e.ExceptionObject is Exception ex)
                {
                    Log.AppDomain.Debug?.Log(message, ex);
                }
                else
                {
                    Log.AppDomain.Debug?.Log($"{message} {e.ExceptionObject} {Environment.StackTrace}");
                }
            };
        }

        if (_settings.AsynchronousLoggingEnabled)
        {
            _queue = new MTLoggerAsyncQueue();
        }

        HarmonyXLoggerAdapter.Setup();
    }

    private static void AddAppenders(string basePath, Dictionary<string, AppenderSettings> logs)
    {
        if (logs == null || logs.Count < 1)
        {
            return;
        }

        // we dont want in-place changes, otherwise we would need to lock between write/read
        // hence we work by copy and replace (replace being an atomic operation)
        var logsAppenders = new AppenderFile[_logsAppenders.Length + logs.Count];
        Array.Copy(_logsAppenders, logsAppenders, _logsAppenders.Length);
        var index = _logsAppenders.Length;
        foreach (var kv in logs)
        {
            var logPath = Path.Combine(basePath, kv.Key);
            logsAppenders[index++] = new AppenderFile(logPath, kv.Value);
        }
        _logsAppenders = logsAppenders;
    }

    internal static void AddModLogAppenders(string basePath, Dictionary<string, AppenderSettings> logs)
    {
        if (!_settings.ModLogAppendersEnabled)
        {
            return;
        }
        AddAppenders(basePath, logs);
    }

    internal static void AddModLogAppender(string logPath, string loggerName)
    {
        if (!_settings.ModLogAppendersEnabled)
        {
            return;
        }
        var logsAppenders = new AppenderFile[_logsAppenders.Length + 1];
        Array.Copy(_logsAppenders, logsAppenders, _logsAppenders.Length);
        var index = _logsAppenders.Length;
        var settings = new AppenderSettings { Includes = [new FilterSettings { LoggerNames = [loggerName] }] };
        logsAppenders[index] = new AppenderFile(logPath, settings);
        _logsAppenders = logsAppenders;
    }

    internal static readonly MTStopwatchWithSampling DispatchStopWatch = new(1000);
    // used for intercepting all logging attempts and to log centrally
    internal static void LogAtLevel(string loggerName, LogLevel logLevel, object message, Exception exception, IStackTrace location)
    {
        // capture timestamp as early as possible, to be as close to the callers intended time
        var timestamp = MTStopwatch.GetTimestamp();

        // convert message to string while still in caller thread
        var messageAsString = message?.ToString(); // do this asap too, as this can throw exceptions

        // fill out location if not already filled out while still on caller stack
        if (location == null && (_settings.LogStackTraces || (_settings.LogStackTracesOnExceptions && exception != null)))
        {
            location = GrabStackTrace();
        }

        if (!IsDispatchAvailable(out var threadId))
        {
            var messageDto = new MTLoggerMessageDto
            {
                Timestamp = timestamp,
                LoggerName = loggerName,
                LogLevel = logLevel,
                Message = messageAsString,
                Exception = exception,
                Location = location,
                ThreadId = threadId
            };
            LogMessageThreadSafe(ref messageDto);
            return;
        }

        ref var updateDto = ref _queue.AcquireUncommitedOrWait();

        updateDto.Timestamp = timestamp;
        updateDto.LoggerName = loggerName;
        updateDto.LogLevel = logLevel;
        updateDto.Message = messageAsString;
        updateDto.Exception = exception;
        updateDto.Location = location;
        updateDto.ThreadId = threadId;
        updateDto.Commit();

        DispatchStopWatch.EndMeasurement(timestamp);
    }

    internal static void Flush()
    {
        var flushEvent = new ManualResetEventSlim(false);

        if (!IsDispatchAvailable(out _))
        {
            var messageDto = new MTLoggerMessageDto();
            messageDto.FlushToDiskPostEvent = flushEvent;
            LogMessageThreadSafe(ref messageDto);
            return;
        }
        var measurement = MTStopwatch.GetTimestamp();
        ref var updateDto = ref _queue.AcquireUncommitedOrWait();
        DispatchStopWatch.EndMeasurement(measurement);

        updateDto.FlushToDiskPostEvent = flushEvent;
        updateDto.Commit();

        // always wait
        // usually caller wants to flush to guarantee debug information on disk
        flushEvent.Wait();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDispatchAvailable(out int currentThreadId)
    {
        // capture caller thread
        currentThreadId = Thread.CurrentThread.ManagedThreadId;

        if (_queue == null) // async is disabled
        {
            return false;
        }

        if (_queue.IsShuttingOrShutDown)
        {
            if (_queue.LogWriterThreadId != currentThreadId) // avoid deadlock if we are on logger thread already
            {
                _queue.WaitForShutdown();
            }
            return false;
        }

        return true;
    }

    private static DiagnosticsStackTrace GrabStackTrace()
    {
        // HBS original
        // (3) Log, LogError, LogAtLevel x (official logging api)
        // (2) LogAtLevel x y z (internal logging api)
        // (1) GrabStackTrace

        // new
        // (7) Log(msg) / Log(ex) / Log(msg,ex)
        // (6) LogAtLevel x (official logging api)
        // (5) LogAtLevel x y z wrapper (internal logging api)
        // (4) ?
        // (3) LogAtLevel prefix patch
        // (2) GrabStackTrace
        // (1) DiagnosticsStackTrace

        return new DiagnosticsStackTrace(6, false);
    }

    // without dispatch, thread safety is not guaranteed
    // we also wait until dispatch thread is shutdown in IsDispatchAvailable before we go through here
    internal static void LogMessageThreadSafe(ref MTLoggerMessageDto messageDto)
    {
        lock (s_logMessageLock)
        {
            LogMessage(ref messageDto);
        }
    }
    private static readonly object s_logMessageLock = new(); // FastBuffer is not thread safe

    internal static void LogMessage(ref MTLoggerMessageDto messageDto)
    {
        try
        {
            _consoleLog?.Append(ref messageDto);

            _mainLog.Append(ref messageDto);
            foreach (var logAppender in _logsAppenders)
            {
                logAppender.Append(ref messageDto);
            }
        }
        finally
        {
            messageDto.FlushToDiskPostEvent?.Set();
        }
    }

    internal static void WriteExceptionToFatalLog(Exception exception)
    {
        File.WriteAllText(Path.Combine("Mods", ".modtek", "ModTekFatalError.log"), exception.ToString());
    }
}