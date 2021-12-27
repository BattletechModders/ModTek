using System;
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
        private static HarmonyInstance harmony;

        internal static void Patch()
        {
            if (!ModTek.Config.Profiling.Enabled)
            {
                return;
            }

            harmony = HarmonyInstance.Create("ModTek.Profiler");
            new PatchProcessor(harmony, typeof(ProfilerPatcher), null).Patch();
            harmony = null;
        }

        internal static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.Profiling.Enabled;
        }

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var updateMethods = MethodsToProfile().ToHashSet();
            updateMethods.Add(MethodToCheckFrameTime);
            foreach (var method in updateMethods)
            {
                yield return method;
            }

            // patch all prefixes and postfixes around methods being profiles
            foreach (var method in harmony.GetPatchedMethods())
            {
                var info = harmony.GetPatchInfo(method);
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

        private static IEnumerable<MethodBase> MethodsToProfile()
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var p in FindMatchingMethodInAssembly(a))
                {
                    yield return p;
                }
            }
        }

        private static IEnumerable<MethodBase> FindMatchingMethodInAssembly(Assembly assembly)
        {
            var matcher = new MethodMatcher(ModTek.Config.Profiling.Filters);
            return FindMethodsInAssembly(assembly, matcher);
        }

        private static IEnumerable<MethodBase> FindMethodsInAssembly(Assembly assembly, MethodMatcher matcher)
        {
            if (!matcher.MatchesAssembly(assembly))
            {
                yield break;
            }

            // patch all Update methods in base game
            foreach (var type in AssemblyUtil.GetTypesSafe(assembly))
            {
                if (!matcher.MatchesType(assembly, type))
                {
                    continue;
                }

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                }
                catch (Exception e)
                {
                    Log($"Can't get methods from Type {type}", e);
                    continue;
                }

                foreach (var method in methods)
                {
                    if (!matcher.MatchesMethod(assembly, type, method))
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        private static readonly Timings timings = new Timings();
        private static readonly MethodBase MethodToCheckFrameTime = AccessTools.Method(typeof(UnityGameInstance), "Update");

        [HarmonyPriority(Priority.First)]
        internal static void Prefix(MethodBase __originalMethod, out long __state)
        {
            try
            {
                if (MethodToCheckFrameTime.Equals(__originalMethod))
                {
                    timings.DumpAndResetIfSlow();
                }
            }
            catch (Exception e)
            {
                Log("Error running prefix", e);
            }
            __state = timings.GetRawTicks();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(MethodBase __originalMethod, long __state)
        {
            try
            {
                var deltaRawTicks = timings.GetRawTicks() - __state;
                timings.Increment(__originalMethod, deltaRawTicks);
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }
}