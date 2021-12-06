using System;
using System.Diagnostics;
using System.IO;
using ModTek.Misc;

namespace ModTek.Features.Logging
{
    // TODO introduce log levels
    // MTLogger.Info.Log(message)
    // if debug is optional: MTLogger.Debug?.Log(message)
    internal static class MTLogger
    {
        internal static void Log(string message = null, Exception e = null)
        {
            message = message ?? "Method " + GetFullMethodName() + " called";
            LoggingFeature.Log(message, e);
        }

        internal static void LogIf(bool condition, string message)
        {
            if (condition)
            {
                Log(message);
            }
        }

        internal static void LogIfSlow(Stopwatch sw, string id = null, long threshold = 1000, bool resetIfLogged = true)
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
