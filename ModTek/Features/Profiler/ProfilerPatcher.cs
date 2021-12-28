using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BattleTech;
using Harmony;
using ModTek.Util;
using UnityEngine;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Profiler
{
    internal static class ProfilerPatcher
    {
        internal static HarmonyInstance harmony;
        internal static Timings timings;

        internal static void Patch()
        {
            if (!ModTek.Enabled || !ModTek.Config.Profiling.Enabled)
            {
                return;
            }

            timings = new Timings();
            harmony = HarmonyInstance.Create("ModTek.Profiler");
            new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMoveNext_Patch), null).Patch();
            new PatchProcessor(harmony, typeof(MethodMatcher_Patcher), null).Patch();
            harmony = null;
        }
    }

    internal static class MethodMatcher_Patcher
    {
        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var updateMethods = MethodMatcher
                .FindMethodsToProfile(ModTek.Config.Profiling.Filters)
                .ToHashSet();

            updateMethods.Add(MethodToCheckFrameTime);
            updateMethods.Remove(SetupCoroutine_InvokeMoveNext_Patch.TargetMethod());

            foreach (var method in updateMethods)
            {
                yield return method;
            }

            // patch all prefixes and postfixes around methods being profiles
            foreach (var method in ProfilerPatcher.harmony.GetPatchedMethods())
            {
                var info = ProfilerPatcher.harmony.GetPatchInfo(method);
                if (info == null || method.DeclaringType == null)
                {
                    continue;
                }
                if (!updateMethods.Contains(method))
                {
                    continue;
                }
                foreach (var patch in info.Prefixes)
                {
                    yield return patch.patch;
                }
                foreach (var patch in info.Postfixes)
                {
                    yield return patch.patch;
                }
            }
        }

        private static readonly MethodBase MethodToCheckFrameTime = AccessTools.Method(typeof(UnityGameInstance), "Update");

        [HarmonyPriority(Priority.First)]
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
                Log("Error running prefix", e);
            }
            __state = ProfilerPatcher.timings.GetRawTicks();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(MethodBase __originalMethod, long __state)
        {
            try
            {
                var deltaRawTicks = ProfilerPatcher.timings.GetRawTicks() - __state;
                ProfilerPatcher.timings.Increment(__originalMethod, deltaRawTicks);
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }

    internal static class SetupCoroutine_InvokeMoveNext_Patch
    {
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("UnityEngine.SetupCoroutine:InvokeMoveNext");
        }

        [HarmonyPriority(Priority.First)]
        public static void Prefix(out long __state)
        {
            __state = ProfilerPatcher.timings.GetRawTicks();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(IEnumerator enumerator, long __state)
        {
            try
            {
                var deltaRawTicks = ProfilerPatcher.timings.GetRawTicks() - __state;
                ProfilerPatcher.timings.Increment(enumerator.GetType(), deltaRawTicks);
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }
}