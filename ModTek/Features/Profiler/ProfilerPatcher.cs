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
            harmony = HarmonyInstance.Create("ModTek.Profiler");
            new PatchProcessor(harmony, typeof(ProfilerPatcher), null).Patch();
            harmony = null;
        }

        internal static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.ProfileGameUpdate;
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

        // TODO config
        private static IEnumerable<MethodBase> MethodsToProfile()
        {
            foreach (var p in BattleTechMethodsToProfile())
            {
                yield return p;
            }

            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var p in UnityMethodsToProfile(a))
                {
                    yield return p;
                }
            }
        }

        // TODO config
        private static IEnumerable<MethodBase> UnityMethodsToProfile(Assembly assembly)
        {
            var matcher = new MethodMatcher(new[]
            {
                new MethodMatchFilter
                {
                    Name = "FixedUpdate",
                    ParameterTypes = Type.EmptyTypes,
                    ReturnType = typeof(void),
                    SubClassOf = typeof(MonoBehaviour),
                },
                new MethodMatchFilter
                {
                    Name = "Update",
                    ParameterTypes = Type.EmptyTypes,
                    ReturnType = typeof(void),
                    SubClassOf = typeof(MonoBehaviour),
                },
                new MethodMatchFilter
                {
                    Name = "LateUpdate",
                    ParameterTypes = Type.EmptyTypes,
                    ReturnType = typeof(void),
                    SubClassOf = typeof(MonoBehaviour),
                },
                // new MethodMatchFilter
                // {
                //     Name = "Start",
                //     ParameterTypes = Type.EmptyTypes,
                //     SubClassOf = typeof(MonoBehaviour),
                // },
                // new MethodMatchFilter
                // {
                //     Name = "Awake",
                //     ParameterTypes = Type.EmptyTypes,
                //     ReturnType = typeof(void),
                //     SubClassOf = typeof(MonoBehaviour),
                // },
            });
            return FindMethodsInAssembly(assembly, matcher);
        }

        // TODO config
        private static IEnumerable<MethodBase> BattleTechMethodsToProfile()
        {
            var assembly = typeof(UnityGameInstance).Assembly;
            var matcher = new MethodMatcher(new[]
            {
                // BT methods
                new MethodMatchFilter
                {
                    Name = "Update",
                    ParameterTypes = Type.EmptyTypes,
                    ReturnType = typeof(void)
                },
                new MethodMatchFilter
                {
                    Name = "Update",
                    ParameterTypes = new[]{typeof(float)},
                    ReturnType = typeof(void)
                },
            });
            return FindMethodsInAssembly(assembly, matcher);
        }

        private static IEnumerable<MethodBase> FindMethodsInAssembly(Assembly assembly, MethodMatcher matcher)
        {
            // patch all Update methods in base game
            foreach (var type in AssemblyUtil.GetTypesSafe(assembly))
            {
                if (!matcher.MatchesType(type))
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
                    if (!matcher.MatchesMethod(type, method))
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        [HarmonyPriority(Priority.First)]
        internal static void Prefix(MethodBase __originalMethod, out long __state)
        {
            __state = timings.GetRawTicks();
        }

        private static Timings timings = new Timings();
        private static MethodBase MethodToCheckFrameTime = AccessTools.Method(typeof(UnityGameInstance), "Update");

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(MethodBase __originalMethod, long __state)
        {
            try
            {
                var deltaRawTicks = timings.GetRawTicks() - __state;
                timings.Increment(__originalMethod, deltaRawTicks, MethodToCheckFrameTime.Equals(__originalMethod));
            }
            catch (Exception e)
            {
                Log("Error running postfix", e);
            }
        }
    }
}