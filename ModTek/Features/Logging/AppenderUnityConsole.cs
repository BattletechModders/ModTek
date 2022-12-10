using System;
using HBS.Logging;
using UnityEngine;
using Logger = HBS.Logging.Logger;

namespace ModTek.Features.Logging;

internal class AppenderUnityConsole : AppenderBase
{
    // appender part

    private readonly ILogger _logger;

    internal AppenderUnityConsole(AppenderSettings settings) : base(settings)
    {
        _logger = Debug.unityLogger;
    }

    // TODO support for LogLevelExtension
    // TODO support for context -> threading issue
    protected override void WriteLine(MTLoggerMessageDto messageDto, string line)
    {
        s_ignoreNextUnityCapture = true;
        _logger.Log(LogLevelToLogType(messageDto.logLevel), line);
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