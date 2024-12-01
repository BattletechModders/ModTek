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

    internal AppenderUnityConsole(AppenderSettings settings)
    {
        _filters = new Filters(settings);
        _formatter = new Formatter(settings);
        _debugUnityLogger = Debug.unityLogger;
    }

    internal void Append(MTLoggerMessageDto messageDto)
    {
        // breaks the loop: Unity -> HBS -(x)-> Unity
        if (messageDto.LoggerName == UnityLoggerName)
        {
            return;
        }

        if (!_filters.IsIncluded(messageDto))
        {
            return;
        }

        var logLine = _formatter.GetFormattedLogLine(messageDto);
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