using System;
using System.Reflection;
using Harmony;
using HBS.Logging;
using Logger = HBS.Logging.Logger;
using Object = UnityEngine.Object;

// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace ModTek.Patches
{
    [HarmonyPatch]
    public static class LoggerLogImpl_LogAtLevel_Patch
    {
        static MethodBase TargetMethod()
        {
            var type = AccessTools.Inner(typeof(Logger), "LogImpl");
            var method = AccessTools.Method(type, "LogAtLevel", new[]
            {
                typeof(LogLevel),
                typeof(object),
                typeof(Object),
                typeof(Exception),
                typeof(IStackTrace)
            });
            return method;
        }

        static void Postfix(string ___name, LogLevel level, object message, Object context, Exception exception, IStackTrace location)
        {
            try
            {
                ModTek.CleanedLog.OnLogMessage(___name, level, message, context, exception, location);
            }
            catch (Exception e)
            {
                Logging.Logger.LogException("Error when running LogAtLevel hook!", e);
            }
        }
    }
}
