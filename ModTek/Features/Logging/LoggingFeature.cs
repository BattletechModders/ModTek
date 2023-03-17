using System;
using System.Collections.Generic;
using System.IO;
using HBS.Logging;
using ModTek.Misc;

namespace ModTek.Features.Logging;

internal static class LoggingFeature
{
    private static LoggingSettings _settings;

    private static AppenderUnityConsole _consoleLog;
    private static AppenderFile _mainLog;
    private static readonly List<AppenderFile> _logsAppenders = new();

    private static MTLoggerAsyncQueue _queue;

    internal static void Init()
    {
        _settings = ModTek.Config.Logging;

        Directory.CreateDirectory(FilePaths.TempModTekDirectory);

        AppenderUnityConsole.SetupUnityLogHandler();
        _consoleLog = _settings.UnityConsoleAppenderEnabled ? new AppenderUnityConsole(_settings.UnityConsoleAppender) : null;

        _mainLog = new AppenderFile(_settings.MainLogFilePath, _settings.MainLog);
        foreach (var kv in _settings.Logs)
        {
            _logsAppenders.Add(new AppenderFile(kv.Key, kv.Value));
        }

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
            _queue = new MTLoggerAsyncQueue(ProcessLoggerMessage);
        }

        HarmonyXLoggerAdapter.Setup();
    }

    // used for intercepting all logging attempts and to log centrally
    internal static void LogAtLevel(string loggerName, LogLevel logLevel, object message, Exception exception, IStackTrace location)
    {
        if (location == null && (_settings.LogStackTraces || (_settings.LogStackTracesOnExceptions && exception != null)))
        {
            location = GrabStackTrace();
        }

        var messageDto = new MTLoggerMessageDto(loggerName, logLevel, message, exception, location);
        if (_queue == null || !_queue.Add(messageDto))
        {
            ProcessLoggerMessage(messageDto);
        }
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

    // note this can be called sync or async
    private static void ProcessLoggerMessage(MTLoggerMessageDto messageDto)
    {
        _consoleLog?.Append(messageDto);

        _mainLog.Append(messageDto);
        foreach (var logAppender in _logsAppenders)
        {
            logAppender.Append(messageDto);
        }
    }

    internal static void WriteExceptionToFatalLog(Exception exception)
    {
        File.WriteAllText(Path.Combine("Mods", ".modtek", "ModTekFatalError.log"), exception.ToString());
    }
}