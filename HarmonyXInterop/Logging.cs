using System.Reflection;
using HarmonyLib;
using HarmonyLib.Tools;

namespace HarmonyXInterop;

internal static class Logging
{
    private static readonly MethodInfo LogTextMethod = AccessTools.Method(typeof(Logger), "LogText");

    internal static void Info(object message)
    {
        LogText(Logger.LogChannel.Info, message?.ToString());
    }

    internal static void Error(object message)
    {
        LogText(Logger.LogChannel.Error, message?.ToString());
    }

    internal static void LogText(Logger.LogChannel channel, string message)
    {
        LogTextMethod.Invoke(null, new object[] { channel, message, false });
    }
}