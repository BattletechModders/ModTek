using System.Diagnostics;

namespace ModTek.Common.Utils;

internal static class DebugUtils
{
    internal static string GetStackTraceWithoutPatch()
    {
        return new StackTrace(4).ToString();
    }
}