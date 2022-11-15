namespace ModTek.Util;

internal static class DebugUtils
{
    internal static string GetStackTraceWithoutPatch()
    {
        return new System.Diagnostics.StackTrace(4).ToString();
    }
}