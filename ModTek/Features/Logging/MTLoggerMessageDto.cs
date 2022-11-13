using System;
using System.Diagnostics;
using System.Threading;
using HBS.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ModTek.Features.Logging
{
    internal class MTLoggerMessageDto
    {
        internal static readonly TimeSpan InitialUnityStartupTime;
        internal static readonly long InitialStopwatchTicks;
        internal static readonly DateTimeOffset InitialDatTimeOffsetUtc;

        static MTLoggerMessageDto()
        {
            InitialUnityStartupTime = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
            InitialStopwatchTicks = Stopwatch.GetTimestamp();
            InitialDatTimeOffsetUtc = DateTimeOffset.UtcNow;
        }

        // we want to be as close to the time when the log statement was triggered
        private readonly long _ticks = Stopwatch.GetTimestamp();

        // we need the thread before switch to async logging
        internal readonly Thread thread = Object.CurrentThreadIsMainThread() ? null : Thread.CurrentThread;

        internal readonly string message;
        internal readonly string loggerName;
        internal readonly LogLevel logLevel;
        internal readonly Exception exception;

        internal MTLoggerMessageDto(string loggerName, LogLevel logLevel, object message, Exception exception)
        {
            this.loggerName = loggerName;
            this.logLevel = logLevel;
            this.message = message?.ToString(); // message as object might not be thread-safe
            this.exception = exception;
        }

        internal TimeSpan StartupTime()
        {
            return InitialUnityStartupTime.Add(GetElapsedSinceInitial());
        }

        internal DateTimeOffset GetDateTimeOffsetUtc()
        {
            return InitialDatTimeOffsetUtc.Add(GetElapsedSinceInitial());
        }

        private TimeSpan GetElapsedSinceInitial()
        {
            return MTStopwatch.TimeSpanFromTicks(_ticks - InitialStopwatchTicks);
        }
    }
}
