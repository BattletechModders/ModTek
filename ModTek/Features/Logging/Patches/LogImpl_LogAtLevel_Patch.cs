using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using HBS.Logging;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch]
    internal static class LogImpl_LogAtLevel_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static MethodInfo TargetMethod()
        {
            var logImpl = AccessTools.Inner(typeof(Logger), "LogImpl");
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

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = AccessTools.Field(typeof(Logger), nameof(Logger.IsUnityApplication));
            var count = 0;
            foreach (var i in instructions)
            {
                // replace second bool HBS.Logging.Logger::IsUnityApplication call
                // avoids logging twice
                if (i.opcode == OpCodes.Ldsfld && m.Equals(i.operand) && count++ == 1)
                {
                    i.opcode = OpCodes.Ldc_I4_0;
                    i.operand = null;
                }
                yield return i;
            }
        }

        [HarmonyPriority(Priority.High)]
        public static bool Prefix(string ___name, LogLevel level, object message, Exception exception, IStackTrace location)
        {
            try
            {
                LoggingFeature.LogAtLevel(
                    ___name,
                    level,
                    message,
                    exception,
                    location,
                    out var skipOriginal
                );
                if (skipOriginal)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Log("Couldn't rewrite LogAtLevel call",  e);
            }
            return true;
        }
    }
}
