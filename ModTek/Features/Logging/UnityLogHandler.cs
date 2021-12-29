using System;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging
{
    internal class UnityLogHandler : ILogHandler
    {
        internal static void Setup()
        {
            // the normal way
            Application.logMessageReceived += HandleUnityLog;

            // the proper way
            // Debug.unityLogger.logHandler = new UnityLogHandler(Debug.unityLogger.logHandler);
        }

        private readonly ILogHandler logHandler;
        internal UnityLogHandler(ILogHandler logHandler)
        {
            this.logHandler = logHandler;
        }
        public void LogException(Exception exception, UnityEngine.Object context)
        {
            logHandler.LogException(exception, context);
            LoggingFeature.LogAtLevel(
                "Unity.Via.LogException",
                LogLevel.Error,
                "Unity is logging an exception", // lets output any inner exception and stack traces
                exception,
                null,
                out _
            );
        }
        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            logHandler.LogFormat(logType, context, format, args);
            LoggingFeature.LogAtLevel(
                "Unity.Via.LogFormat",
                UnityLogTypeToHBSLogLevel(logType),
                string.Format(format, args),
                null,
                null,
                out _
            );
        }

        private static void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            LoggingFeature.LogAtLevel(
                "Unity",
                UnityLogTypeToHBSLogLevel(type),
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
