﻿using System;
using System.Diagnostics;
using System.Threading;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(Thread), "StartInternal")]
internal static class Thread_StartInternal_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.Logging.LogThreadStarts;
    }

    public static void Postfix(Thread __instance)
    {
        Log.Debugger.Debug?.Log("A thread was started with ThreadId=" + __instance.ManagedThreadId + Environment.NewLine + new StackTrace(4));
    }
}