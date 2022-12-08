using System;
using HBS.Logging;
using UnityEngine;
using Logger = HBS.Logging.Logger;

namespace ModTek.Features.Logging;

internal class UnityLogHandler
{
    internal static void Setup()
    {
        Application.logMessageReceivedThreaded += LogMessageReceivedThreaded;
        Application.logMessageReceived -= Logger.HandleUnityLog;
    }

    private static void LogMessageReceivedThreaded(string logString, string stackTrace, LogType type)
    {
        if (s_ignoreNextUnityCapture)
        {
            s_ignoreNextUnityCapture = false;
            return;
        }

        Log.Unity.Log.LogAtLevel(
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

    // TODO support for LogLevelExtension
    // TODO support for custom log formatter
    // TODO support for context -> threading issue
    internal static void LogMessage(MTLoggerMessageDto messageDto)
    {
        // TODO use dedicated and extensive log level management, this should converted to a fully customizable appender
        if (!Log.Unity.Log.IsEnabledFor(messageDto.logLevel))
        {
            return;
        }

        s_ignoreNextUnityCapture = true;

        var message = Logger.formatHelper.FormatMessage(
            messageDto.loggerName,
            messageDto.logLevel,
            messageDto.message,
            messageDto.exception,
            messageDto.location
        );

        switch (messageDto.logLevel)
        {
            case LogLevel.Error:
                Debug.LogError(message, null);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(message, null);
                break;
            default:
                Debug.Log(message, null);
                break;
        }
    }

    [ThreadStatic]
    private static bool s_ignoreNextUnityCapture;
}