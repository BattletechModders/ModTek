using System;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using Harmony;
using ModTek.Features.Logging;
using UnityEngine;

namespace ModTek.Features.Profiler.MPatches
{
    internal static class MethodMatcher_Patcher
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var patchableChecker = new PatchableChecker();
            var methods = MethodMatcher
                .FindMethodsToProfile(ModTek.Config.Profiling.Filters, patchableChecker)
                .ToHashSet();

            // patch all prefixes and postfixes around methods being profiles
            foreach (var method in ProfilerPatcher.harmony.GetPatchedMethods())
            {
                var info = ProfilerPatcher.harmony.GetPatchInfo(method);
                if (info == null || method.DeclaringType == null)
                {
                    continue;
                }
                if (!methods.Contains(method))
                {
                    continue;
                }

                patchableChecker.DebugLogging = true;
                void addPatches(IEnumerable<Patch> patches)
                {
                    foreach (var patch in patches)
                    {
                        if (patch.owner == ProfilerPatcher.harmony.Id)
                        {
                            continue;
                        }
                        var patchMethod = patch.patch;
                        if (patchableChecker.IsPatchable(patchMethod))
                        {
                            methods.Add(patchMethod);
                        }
                    }
                }
                addPatches(info.Prefixes);
                addPatches(info.Postfixes);
            }

            methods.Add(MethodToCheckFrameTime);
            methods.Remove(SetupCoroutine_InvokeMoveNext_Patch.TargetMethod());
            // methods.RemoveWhere(m => m.FullDescription().Contains("GravityMatters"));
            return methods;
        }

        private static readonly MethodBase MethodToCheckFrameTime = AccessTools.Method(typeof(UnityGameInstance), "Update");

        // [HarmonyPriority(Priority.First)]
        internal static void Prefix(MethodBase __originalMethod, out long __state)
        {
            try
            {
                if (MethodToCheckFrameTime.Equals(__originalMethod))
                {
                    ProfilerPatcher.timings.DumpAndResetIfSlow(Time.deltaTime);
                }
            }
            catch (Exception e)
            {
                MTLogger.Log("Error running prefix", e);
            }
            __state = ProfilerPatcher.timings.GetRawTicks();
        }

        // [HarmonyPriority(Priority.Last)]
        internal static void Postfix(MethodBase __originalMethod, long __state)
        {
            try
            {
                var deltaRawTicks = ProfilerPatcher.timings.GetRawTicks() - __state;
                ProfilerPatcher.timings.Increment(__originalMethod, deltaRawTicks);
            }
            catch (Exception e)
            {
                MTLogger.Log("Error running postfix", e);
            }
        }
    }
}
