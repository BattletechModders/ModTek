using System;
using System.Diagnostics;
using HBS.Logging;

namespace ModTek.Features.Logging
{
    internal class MTLogger
    {
        // Error: ModTek can't continue or has a serious internal error.
        //        E.g. harmony patch not being applied or a prefix/postfix patch throwing an exception.
        internal static readonly MTLogger Error = new MTLogger(LogLevel.Error);
        // Warning: Some functionality might not work as expected, most likely to misconfiguration.
        //          E.g. mods referencing files that don't exist, or can't be loaded.
        internal static readonly MTLogger Warning = new MTLogger(LogLevel.Warning);
        // Info: Inform the user of what ModTek is doing with user specified configuration.
        //       E.g. includes listing the mod manifest content and how it is processed in relation to other mods.
        internal static readonly MTLogger Info = new MTLogger(LogLevel.Log);
        // Debug: If info tells us what files are being loaded, debug should tell what processing steps ModTek undergoes with the files.
        internal static readonly MTLogger Debug = new MTLogger(LogLevel.Debug);

        private readonly LogLevel logLevel;

        public MTLogger(LogLevel logLevel)
        {
            this.logLevel = logLevel;
        }

        internal void Log(Exception e)
        {
            Log(null, e);
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
