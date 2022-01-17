using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using HBS.Logging;

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

            if (Settings == null)
            {
                Settings = ModTek.Config.Logging;
                if (Settings.SkipOriginalLoggers)
                {
                    IgnoreSkipForLoggers = Settings.IgnoreSkipForLoggers.ToHashSet();
                }

                {
                    var logImplType = AccessTools.Inner(typeof(Logger), "LogImpl");
                    var LogImplIsEnabledForMethod = AccessTools.Method(logImplType, "IsEnabledFor");
                    var dm = new DynamicMethod(
                        "IsEnabledForWrapper",
                        typeof(bool),
                        new[]
                        {
                            typeof(object),
                            typeof(LogLevel)
                        },
                        logImplType
                    );
                    var gen = dm.GetILGenerator();
                    gen.Emit(OpCodes.Ldarg_0);
                    gen.Emit(OpCodes.Ldarg_1);
                    gen.Emit(OpCodes.Call, LogImplIsEnabledForMethod);
                    gen.Emit(OpCodes.Ret);
                    LogImplIsEnabledFor = (Func<object, LogLevel, bool>) dm.CreateDelegate(typeof(Func<object, LogLevel, bool>));
                }
            }

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
        private static LoggingSettings Settings;
        private static HashSet<string> IgnoreSkipForLoggers;
        private static bool SkipOriginalLoggers => IgnoreSkipForLoggers != null;
        private static Func<object, LogLevel, bool> LogImplIsEnabledFor;

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
        public static bool Prefix(object __instance, string ___name, LogLevel level, object message, Exception exception, IStackTrace location)
        {
            try
            {
                if (Settings.IgnoreLoggerLogLevel || LogImplIsEnabledFor(__instance, level))
                {
                    LoggingFeature.LogAtLevel(
                        ___name,
                        level,
                        message,
                        exception,
                        location
                    );
                }

                var skipOriginal = SkipOriginalLoggers && !IgnoreSkipForLoggers.Contains(___name);
                if (skipOriginal)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                MTLogger.Error.Log("Couldn't rewrite LogAtLevel call", e);
            }
            return true;
        }
    }
}
