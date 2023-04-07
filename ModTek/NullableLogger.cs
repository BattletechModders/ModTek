// get the latest version of the nullable logger from MechEngineer
#nullable enable
using System;
using System.Collections.Generic;
using HBS.Logging;
using ModTek.Features.Logging;

namespace NullableLogging;

[HarmonyPatch]
internal sealed class NullableLogger
{
    // instantiation

    internal static NullableLogger GetLogger(string name, LogLevel? defaultLogLevel = null)
    {
        lock (_loggers)
        {
            if (!_loggers.TryGetValue(name, out var logger))
            {
                logger = new(name, defaultLogLevel);
                _loggers[name] = logger;
            }
            return logger;
        }
    }

    // useful constants

    internal const LogLevel TraceLogLevel = (LogLevel)200;

    // tracking

    private static readonly SortedList<string, NullableLogger> _loggers = new();

    [HarmonyPatch(typeof(Logger.LogImpl), nameof(Logger.LogImpl.Level), MethodType.Setter)]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void LogImpl_set_Level_Postfix()
    {
        lock (_loggers)
        {
            foreach (var modLogger in _loggers.Values)
            {
                modLogger.RefreshLogLevel();
            }
        }
    }

    // instantiation

    internal Logger.LogImpl Log { get; }
    private NullableLogger(string name, LogLevel? defaultLogLevel)
    {
        Log = (Logger.LogImpl)(
            defaultLogLevel == null
                ? Logger.GetLogger(name)
                : Logger.GetLogger(name, defaultLogLevel.Value)
        );
        RefreshLogLevel();
    }

    // log levels

    internal ILevel? Trace { get; private set; }
    internal ILevel? Debug { get; private set; }
    internal ILevel? Info { get; private set; }
    internal ILevel? Warning { get; private set; }
    internal ILevel? Error { get; private set; }

    private void RefreshLogLevel()
    {
        var lowerLevelEnabled = false;
        Trace = SyncLevelLogger(ref lowerLevelEnabled, TraceLogLevel, Trace);
        Debug = SyncLevelLogger(ref lowerLevelEnabled, LogLevel.Debug, Debug);
        Info = SyncLevelLogger(ref lowerLevelEnabled, LogLevel.Log, Info);
        Warning = SyncLevelLogger(ref lowerLevelEnabled, LogLevel.Warning, Warning);
        Error = SyncLevelLogger(ref lowerLevelEnabled, LogLevel.Error, Error);
    }

    private ILevel? SyncLevelLogger(ref bool lowerLevelEnabled, LogLevel logLevel, ILevel? logger)
    {
        if (lowerLevelEnabled || Log.IsEnabledFor(logLevel))
        {
            lowerLevelEnabled = true;
            return logger ?? new LevelLogger(logLevel, Log);
        }
        return null;
    }

    // logging

    internal interface ILevel
    {
        void Log(Exception e);
        void Log(string message);
        void Log(string message, Exception e);
    }

    private sealed class LevelLogger : ILevel
    {
        private readonly LogLevel _level;
        private readonly Logger.LogImpl _log;

        internal LevelLogger(LogLevel level, Logger.LogImpl log)
        {
            _level = level;
            _log = log;
        }

        public void Log(Exception e)
        {
            // _log.LogAtLevel(_level, null, e);
            LoggingFeature.LogAtLevel(_log.Name, _level, null, e, null);
        }

        public void Log(string message)
        {
            // _log.LogAtLevel(_level, message);
            LoggingFeature.LogAtLevel(_log.Name, _level, message, null, null);
        }

        public void Log(string message, Exception e)
        {
            // _log.LogAtLevel(_level, message, e);
            LoggingFeature.LogAtLevel(_log.Name, _level, message, e, null);
        }
    }
}