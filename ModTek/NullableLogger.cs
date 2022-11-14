#nullable enable
using System;
using System.Collections.Generic;
using Harmony;
using HBS.Logging;

namespace NullableLogging;

internal sealed class NullableLogger
{
    // instantiation

    internal static NullableLogger GetLogger(string name, LogLevel? defaultLogLevel = TraceLogLevel)
    {
        lock (_modLoggers)
        {
            if (!_modLoggers.TryGetValue(name, out var modLogger))
            {
                modLogger = new NullableLogger(name, defaultLogLevel);
            }
            return modLogger;
        }
    }

    // useful constants

    internal const LogLevel TraceLogLevel = (LogLevel)200;

    // tracking (static)

    private static readonly SortedList<string, NullableLogger> _modLoggers = new();
    static NullableLogger()
    {
        TrackLoggerLevelChanges();
    }
    private static void TrackLoggerLevelChanges()
    {
        HarmonyInstance
            .Create(typeof(NullableLogger).FullName)
            .Patch(
                original: typeof(Logger.LogImpl).GetProperty(nameof(Logger.LogImpl.Level))!.SetMethod,
                postfix: new HarmonyMethod(typeof(NullableLogger), nameof(LogImpl_set_Level_Postfix))
            );
    }
    private static void LogImpl_set_Level_Postfix()
    {
        lock (_modLoggers)
        {
            foreach (var modLogger in _modLoggers.Values)
            {
                modLogger.RefreshLogLevel();
            }
        }
    }

    // instantiation

    internal Logger.LogImpl Log { get; }
    private NullableLogger(string name, LogLevel? defaultLogLevel = null)
    {
        Log = (Logger.LogImpl)(
            defaultLogLevel == null
                ? Logger.GetLogger(name)
                : Logger.GetLogger(name, defaultLogLevel.Value)
        );
        RefreshLogLevel();
    }

    // log levels

    internal ILevel? Trace => _trace;
    internal ILevel? Debug => _debug;
    internal ILevel? Info => _info;
    internal ILevel? Warning => _warning;
    internal ILevel? Error => _error;

    private LevelLogger? _trace;
    private LevelLogger? _debug;
    private LevelLogger? _info;
    private LevelLogger? _warning;
    private LevelLogger? _error;

    private void RefreshLogLevel()
    {
        var lowerLevelEnabled = SyncLevelLogger(false, TraceLogLevel, ref _trace);
        SyncLevelLogger(lowerLevelEnabled, LogLevel.Debug, ref _debug);
        SyncLevelLogger(lowerLevelEnabled, LogLevel.Log, ref _info);
        SyncLevelLogger(lowerLevelEnabled, LogLevel.Warning, ref _warning);
        SyncLevelLogger(lowerLevelEnabled, LogLevel.Error, ref _error);
    }

    private bool SyncLevelLogger(bool lowerLevelEnabled, LogLevel logLevel, ref LevelLogger? field)
    {
        if (lowerLevelEnabled)
        {
            field ??= new LevelLogger(logLevel, Log);
            return true;
        }
        field = null;
        return false;
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
            _log.LogAtLevel(_level, null, e);
        }

        public void Log(string message)
        {
            _log.LogAtLevel(_level, message);
        }

        public void Log(string message, Exception e)
        {
            _log.LogAtLevel(_level, message, e);
        }
    }
}