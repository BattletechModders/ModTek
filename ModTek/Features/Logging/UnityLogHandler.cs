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
        LoggingFeature.LogAtLevel(
            "Unity",
            UnityLogTypeToHBSLogLevel(type),
            logString,
            null,
            GetLocation(stackTrace)
        );
    }

    private static IStackTrace GetLocation(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return null;
        }
        if (stackTrace.StartsWith("UnityEngine.Debug:Log"))
        {
            return new UnityStackTrace(stackTrace, 1);
        }
        return new UnityStackTrace(stackTrace);
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
}