using System.Diagnostics;
using NullableLogging;

namespace ModTek
{
    internal static class Log
    {
        internal static readonly NullableLogger Main = NullableLogger.GetLogger(typeof(Log).Assembly.GetName().Name);

        internal static void LogIf(this NullableLogger.ILevel @this, bool condition, string message)
        {
            if (condition)
            {
                @this.Log(message);
            }
        }

        internal static void LogIfSlow(this NullableLogger.ILevel @this, Stopwatch sw, string id = null, long threshold = 1000, bool resetIfLogged = true)
        {
            if (sw.ElapsedMilliseconds < threshold)
            {
                return;
            }

            id = id ?? "Method " + GetFullMethodName();
            @this.Log($"{id} took {sw.Elapsed}");

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