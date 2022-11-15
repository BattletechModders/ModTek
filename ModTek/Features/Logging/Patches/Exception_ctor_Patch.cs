using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using HBS.Logging;
using UnityEngine;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch]
internal static class Exception_ctor_Patch
{
    private static bool Setup;
    private static bool ApplicationIsQuitting;

    public static bool Prepare()
    {
        if (ModTek.Enabled && ModTek.Config.Logging.LogExceptionInitializations)
        {
            if (!Setup)
            {
                Application.quitting += () => ApplicationIsQuitting = true;
                Setup = true;
            }
            return true;
        }
        return false;
    }

    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return typeof(Exception).GetConstructors();
    }

    public static void Postfix(Exception __instance)
    {
        // fix for "crash during exit"
        if (ApplicationIsQuitting)
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