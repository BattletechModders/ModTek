using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging
{
    internal static class UnityLogHandler
    {
        internal static void Setup()
        {
            Application.logMessageReceived -= HandleUnityLog; // setup is called several times
            Application.logMessageReceived += HandleUnityLog;
            Application.logMessageReceived -= HBS.Logging.Logger.HandleUnityLog;
        }

        private const string UnityLoggerName = "Unity";
        private static void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            var level = UnityLogTypeToHBSLogLevel(type);
            LoggingFeature.LogAtLevel(
                UnityLoggerName,
                level,
                logString,
                null,
                GetLocation(stackTrace),
                out _
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
            return new UnityStackTrace(stackTrace, 0);
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
}
