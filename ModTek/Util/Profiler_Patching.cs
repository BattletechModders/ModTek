using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BattleTech;
using Harmony;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Util
{
    internal static class Profiler_Patching
    {
        private static HarmonyInstance harmony;

        internal static void Patch()
        {
            harmony = HarmonyInstance.Create("ModTek.Profiler");
            new PatchProcessor(harmony, typeof(Profiler_Patching), null).Patch();
            harmony = null;
        }

        internal static bool Prepare()
        {
            return ModTek.Enabled && ModTek.Config.ProfileGameUpdate;
        }

        private static IEnumerable<MethodBase> UpdateMethods()
        {
            var acceptableMethodNames = new List<string>
            {
                "Update",
                "LateUpdate"
            };
            // patch all Update methods in base game
            foreach (var type in AssemblyUtil.GetTypesSafe(typeof(UnityGameInstance).Assembly))
            {
                if (!type.IsClass)
                {
                    continue;
                }
                if (type.IsAbstract)
                {
                    continue;
                }
                if (type.ContainsGenericParameters)
                {
                    continue;
                }

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (method.IsConstructor)
                    {
                        continue;
                    }
                    if (method.IsAbstract)
                    {
                        continue;
                    }
                    if (method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (!acceptableMethodNames.Contains(method.Name))
                    {
                        continue;
                    }

                    if (method.ReturnType != typeof(void))
                    {
                        continue;
                    }

                    var parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        if (parameters[0].ParameterType != typeof(float))
                        {
                            continue;
                        }
                    }
                    else if (parameters.Length > 1)
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            var updateMethods = UpdateMethods().ToHashSet();
            foreach (var method in updateMethods)
            {
                yield return method;
            }

            // patch all prefixes and postfixes that are around Update methods
            foreach (var method in harmony.GetPatchedMethods())
            {
                var info = harmony.GetPatchInfo(method);
                if (info == null || method.ReflectedType == null)
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

        [HarmonyPriority(Priority.First)]
        internal static void Prefix(MethodBase __originalMethod, out Stopwatch __state)
        {
            var id = ProfilerId(__originalMethod);
            __state = GetOrCreateStopwatch(id);
            __state.Start();
        }

        [HarmonyPriority(Priority.Last)]
        internal static void Postfix(MethodBase __originalMethod, Stopwatch __state)
        {
            __state.Stop();
            const long DumpWhenFrameTimeSlowerThanMS = 1000L / 30;
            if (UnityGameInstanceUpdateMethod.Equals(__originalMethod))
            {
                var sws = GetStopWatches();
                if (__state.ElapsedMilliseconds > DumpWhenFrameTimeSlowerThanMS)
                {
                    Log("Too slow, dumping profiler stats");
                    var ordered = sws
                        .Select(kv => (kv.Key, kv.Value.Elapsed))
                        .Where(kv => kv.Elapsed.Ticks >= 10000)
                        .OrderByDescending(kv => kv.Elapsed);
                    foreach (var kv in ordered)
                    {
                        Log($"\t{kv.Elapsed:c} {kv.Key}");
                    }
                }
                foreach (var sw in sws.Values)
                {
                    sw.Reset();
                }
            }
        }

        private static MethodBase UnityGameInstanceUpdateMethod = AccessTools.Method(typeof(UnityGameInstance), "Update");
        private static Dictionary<string, Stopwatch> stopwatches = new Dictionary<string, Stopwatch>();
        private static Stopwatch GetOrCreateStopwatch(string id)
        {
            lock (stopwatches)
            {
                if (!stopwatches.TryGetValue(id, out var stopwatch))
                {
                    stopwatch = new Stopwatch();
                    stopwatches[id] = stopwatch;
                }
                return stopwatch;
            }
        }
        private static Dictionary<string, Stopwatch> GetStopWatches()
        {
            lock (stopwatches)
            {
                return new Dictionary<string, Stopwatch>(stopwatches);
            }
        }
        private static string ProfilerId(MethodBase methodBase)
        {
            return methodBase.DeclaringType?.FullName + "." + methodBase.Name;
        }
    }
}