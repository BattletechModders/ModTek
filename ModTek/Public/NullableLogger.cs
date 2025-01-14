#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HBS.Logging;
using JetBrains.Annotations;
using ModTek.Features.Logging;

namespace ModTek.Public;

extern alias MMB;

[HarmonyPatch]
[PublicAPI]
public sealed class NullableLogger
{
    // instantiation

    [PublicAPI]
    public static NullableLogger GetLogger(string name, LogLevel? defaultLogLevel = null)
    {
        lock (s_loggers)
        {
            if (!s_loggers.TryGetValue(name, out var logger))
            {
                logger = new(name, defaultLogLevel);
                s_loggers[name] = logger;
            }
            return logger;
        }
    }

    // useful "constants"

    [PublicAPI]
    public static LogLevel TraceLogLevel => (LogLevel)LogLevelExtension.TraceLogLevel;

    // tracking

    private static readonly SortedList<string, NullableLogger> s_loggers = new();

    [HarmonyPatch(typeof(Logger.LogImpl), nameof(Logger.LogImpl.Level), MethodType.Setter)]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void LogImpl_set_Level_Postfix()
    {
        lock (s_loggers)
        {
            foreach (var modLogger in s_loggers.Values)
            {
                modLogger.RefreshLogLevel();
            }
        }
    }

    // instantiation

    private readonly Logger.LogImpl _logImpl;
    private NullableLogger(string name, LogLevel? defaultLogLevel)
    {
        _logImpl = (Logger.LogImpl)(
            defaultLogLevel == null
                ? Logger.GetLogger(name)
                : Logger.GetLogger(name, defaultLogLevel.Value)
        );
        RefreshLogLevel();
    }

    // logging

    [PublicAPI] public ILog Log => _logImpl;
    [PublicAPI] public ILevel? Trace { get; private set; }
    [PublicAPI] public ILevel? Debug { get; private set; }
    [PublicAPI] public ILevel? Info { get; private set; }
    [PublicAPI] public ILevel? Warning { get; private set; }
    [PublicAPI] public ILevel? Error { get; private set; }

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
        if (lowerLevelEnabled || _logImpl.IsEnabledFor(logLevel))
        {
            lowerLevelEnabled = true;
            return logger ?? new LevelLogger(logLevel, _logImpl.Name);
        }
        return null;
    }

    // logging

    [PublicAPI]
    public interface ILevel
    {
        [PublicAPI] void Log(Exception e);
        [PublicAPI] void Log(string message);
        [PublicAPI] void Log(string message, Exception e);
        [PublicAPI] void Log(NullableLoggerInterpolatedStringHandler handler);
    }

    private sealed class LevelLogger : ILevel
    {
        private readonly LogLevel _level;
        private readonly string _loggerName;

        internal LevelLogger(LogLevel level, string loggerName)
        {
            _level = level;
            _loggerName = loggerName;
        }

        public void Log(Exception e)
        {
            LoggingFeature.LogAtLevel(_loggerName, _level, null, e, null);
        }

        public void Log(string message)
        {
            LoggingFeature.LogAtLevel(_loggerName, _level, message, null, null);
        }

        public void Log(string message, Exception e)
        {
            LoggingFeature.LogAtLevel(_loggerName, _level, message, e, null);
        }

        public void Log(NullableLoggerInterpolatedStringHandler handler)
        {
        }
    }

    [MMB::System.Runtime.CompilerServices.InterpolatedStringHandler]
    [PublicAPI]
    public ref struct NullableLoggerInterpolatedStringHandler
    {
        [PublicAPI]
        public NullableLoggerInterpolatedStringHandler(int literalLength, int formattedCount)
        {
        }

        [PublicAPI]
        public void AppendLiteral(string value)
        {
        }

        [PublicAPI]
        public void AppendFormatted<T>(T dt)
        {
        }

        [PublicAPI]
        public void AppendFormatted(MMB::System.ReadOnlySpan<char> value)
        {
        }

        [PublicAPI]
        public void AppendFormatted(string? value)
        {
        }
    }
}