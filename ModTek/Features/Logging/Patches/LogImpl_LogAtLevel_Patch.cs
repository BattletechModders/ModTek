using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HBS.Logging;
using Object = UnityEngine.Object;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(
    typeof(Logger.LogImpl),
    nameof(Logger.LogImpl.LogAtLevel),
    typeof(LogLevel),
    typeof(object),
    typeof(Object),
    typeof(Exception),
    typeof(IStackTrace)
)]
internal static class LogImpl_LogAtLevel_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled;
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
    public static bool Prefix(Logger.LogImpl __instance, string ___name, LogLevel level, object message, Exception exception, IStackTrace location)
    {
        try
        {
            // TODO __instance.IsEnabledFor is slow, speedup requires to keep track of all loggers and/or children
            // NullableLogger can skip this check
            if (__instance.IsEnabledFor(level))
            {
                LoggingFeature.LogAtLevel(
                    ___name,
                    level,
                    message,
                    exception,
                    location
                );
            }
            return false;
        }
        catch (Exception e)
        {
            Log.Main.Error?.Log("Couldn't rewrite LogAtLevel call", e);
        }
        return true;
    }
}