using System;
using System.Diagnostics;
using HBS.Logging;

namespace ModTek.Features.Logging
{
    internal class MTLogger
    {
        internal static readonly MTLogger Error = new MTLogger(LogLevel.Error);
        internal static readonly MTLogger Warning = new MTLogger(LogLevel.Warning);
        internal static readonly MTLogger Info = new MTLogger(LogLevel.Log);
        internal static readonly MTLogger Debug = new MTLogger(LogLevel.Debug);

        private readonly LogLevel logLevel;

        public MTLogger(LogLevel logLevel)
        {
            this.logLevel = logLevel;
        }

        internal void Log(string message = null, Exception e = null)
        {
            message = message ?? "Method " + GetFullMethodName() + " called";
            LoggingFeature.Log(logLevel, message, e);
        }

        internal void LogIf(bool condition, string message)
        {
            if (condition)
            {
                Log(message);
            }
        }

        internal void LogIfSlow(Stopwatch sw, string id = null, long threshold = 1000, bool resetIfLogged = true)
        {
            if (sw.ElapsedMilliseconds < threshold)
            {
                return;
            }

            id = id ?? "Method " + GetFullMethodName();
            Log($"{id} took {sw.Elapsed}");

            if (resetIfLogged)
            {
                sw.Reset();
            }
        }

        private static string GetFullMethodName()
        {
            var frame = new StackTrace().GetFrame(2);
            var method = frame.GetMethod();
            var methodName = method.Name;
            var className = method.ReflectedType?.FullName;
            var fullMethodName = className + "." + methodName;
            return fullMethodName;
        }
    }
}
