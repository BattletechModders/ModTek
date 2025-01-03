using System;
using HBS.Logging;
using UnityEngine;
using Logger = HBS.Logging.Logger;

namespace ModTek.Features.Logging;

internal class AppenderUnityConsole
{
    // appender part

    private readonly Filters _filters;
    private readonly Formatter _formatter;
    private readonly ILogger _debugUnityLogger;
    private readonly FastBuffer _buffer = new();

    internal AppenderUnityConsole(AppenderSettings settings)
    {
        _filters = new Filters(settings);
        _formatter = new Formatter(settings);
        _debugUnityLogger = Debug.unityLogger;
    }

    internal void Append(ref MTLoggerMessageDto messageDto)
    {
        if (messageDto.FlushToDisk)
        {
            // this is in-memory, nothing to flush to disk!
            return;
        }

        // breaks the loop: Unity -> HBS -(x)-> Unity
        if (messageDto.LoggerName == UnityLoggerName)
        {
            return;
        }

        if (!_filters.IsIncluded(ref messageDto))
        {
            return;
        }

        _buffer.Reset();
        _formatter.SerializeMessage(ref messageDto, _buffer);
        var length = _buffer.GetBytes(out var threadUnsafeBytes);
        // working with bytes and converting is more costly here, but cheaper elsewhere
        // unity console is slow anyway, and also disabled by default
        var logLine = System.Text.Encoding.UTF8.GetString(threadUnsafeBytes, 0, length);
        s_ignoreNextUnityCapture = true;
        _debugUnityLogger.Log(LogLevelToLogType(messageDto.LogLevel), logLine);
    }

    private static LogType LogLevelToLogType(LogLevel level)
    {
        return level switch
        {
            LogLevel.Error => LogType.Error,
            LogLevel.Warning => LogType.Warning,
            _ => LogType.Log
        };
    }

    // static part

    internal static void SetupUnityLogHandler()
    {
        Application.logMessageReceivedThreaded += LogMessageReceivedThreaded;
    }

    internal const string UnityLoggerName = "Unity";
    private static readonly ILog _unityLogger = Logger.GetLogger(UnityLoggerName);

    private static void LogMessageReceivedThreaded(string logString, string stackTrace, LogType type)
    {
        // breaks the loop: HBS -> Unity -(x)-> HBS
        if (s_ignoreNextUnityCapture)
        {
            s_ignoreNextUnityCapture = false;
            return;
        }

        _unityLogger.LogAtLevel(
            UnityLogTypeToHBSLogLevel(type),
            logString + (string.IsNullOrWhiteSpace(stackTrace) ? "" : $": {stackTrace}")
        );
    }

    private static LogLevel UnityLogTypeToHBSLogLevel(LogType unity)
    {
        switch (unity)
        {
            case LogType.Assert:
            case LogType.Error:
            case LogType.Exception:
                return LogLevel.Error;
            case LogType.Warning:
                return LogLevel.Warning;
            case LogType.Log:
            default:
                return LogLevel.Log;
        }
    }

    [ThreadStatic]
    private static bool s_ignoreNextUnityCapture;
}