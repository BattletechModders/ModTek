using System;
using System.Threading;
using HBS.Logging;
using Object = UnityEngine.Object;

namespace ModTek.Features.Logging
{
    internal class MTLoggerMessageDto
    {
        // we want to be as close to the time when the log statement was triggered
        internal readonly DateTimeOffset time = DateTimeOffset.Now;
        // we need the thread before switch to async logging
        internal readonly Thread thread = Object.CurrentThreadIsMainThread() ? null : Thread.CurrentThread;

        internal readonly string message;
        internal readonly string loggerName;
        internal readonly LogLevel logLevel;
        internal readonly Exception exception;

        public MTLoggerMessageDto(string loggerName, LogLevel logLevel, object message, Exception exception)
        {
            this.loggerName = loggerName;
            this.logLevel = logLevel;
            this.message = message?.ToString(); // message as object might not be thread-safe
            this.exception = exception;
        }
    }
}
