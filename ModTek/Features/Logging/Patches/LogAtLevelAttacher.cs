using System;
using System.Reflection;
using Harmony;
using HBS.Logging;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch]
    internal static class LogAtLevelAttacher
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;

        }

        public static MethodInfo TargetMethod()
        {
            var logImpl = AccessTools.Inner(typeof(HBS.Logging.Logger), "LogImpl");
            var original = AccessTools.Method(logImpl, "LogAtLevel", new[]
            {
                typeof(LogLevel),
                typeof(object),
                typeof(UnityEngine.Object),
                typeof(Exception),
                typeof(IStackTrace)
            });
            return original;
        }

        private static readonly FormatHelper FormatHelper = new();
        public static bool Prefix(string ___name, LogLevel level, object message, Exception exception, IStackTrace location)
        {
            try
            {
                var logString = FormatHelper.FormatMessage(
                    ___name,
                    level,
                    message,
                    exception,
                    location
                );
                if (FYLSFeature.ModSettings.preserveFullLog)
                {
                    BTLogger.Full(logString);
                }

                if (!FYLSFeature.LogPrefixesMatcher.IsMatch(logString))
                {
                    BTLogger.Clean(logString);
                }

                return !FYLSFeature.ModSettings.skipOriginalLoggers;
            }
            catch (Exception e)
            {
                Log("can't write to BTLogger",  e);
            }
            return true;
        }
    }
}
