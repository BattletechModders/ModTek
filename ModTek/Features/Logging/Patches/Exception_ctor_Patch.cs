using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using HBS.Logging;
using ModTek.Util;

namespace ModTek.Features.Logging.Patches
{
    [HarmonyPatch]
    internal static class Exception_ctor_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.Logging.LogExceptionInitializations;
        }

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(Exception).GetConstructors();
        }

        public static void Postfix(Exception __instance)
        {
            // fix for "crash during exit"
            if (MTUnityUtils.ApplicationIsQuitting)
            {
                return;
            }

            var ex = __instance;
            var st = new System.Diagnostics.StackTrace(1).ToString();
            LoggingFeature.LogAtLevel(
                "Debugger",
                LogLevel.Debug,
                "An exception was initialized: " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + st,
                null,
                null
            );
        }
    }
}
