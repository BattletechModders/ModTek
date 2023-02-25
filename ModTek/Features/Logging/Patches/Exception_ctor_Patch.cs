using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
        Log.Debugger.Debug?.Log("An exception was initialized: " + ex.GetType().Name + ": " + ex.Message + Environment.NewLine + new StackTrace(1));
    }
}