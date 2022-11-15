using System.Diagnostics;

namespace ModTek.Util;

internal static class DebugUtils
{
    internal static string GetStackTraceWithoutPatch()
    {
        return new StackTrace(4).ToString();
    }
}