using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BattleTech;
using HBS.Util;
using ModTek.Features.Logging;
using ModTek.Misc;
using ModTek.Util;

namespace ModTek.Features.Profiler.Patches;

[HarmonyPatch]
internal static class JSONSerializationUtility_FromJSON_Patch
{
    private static string basePath;
    private static bool testNewton = false;

    public static bool Prepare()
    {
        if (testNewton)
        {
            basePath = Path.Combine(FilePaths.MergeCacheDirectory, "newton");
            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, true);
            }
            Directory.CreateDirectory(basePath);
        }
        return ModTek.Enabled && ModTek.Config.ProfilerEnabled;
    }

    [HarmonyTargetMethods]
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        var genericMethod = typeof(JSONSerializationUtility)
            .GetMethods(BindingFlags.Public|BindingFlags.Static)
            .Single(x => x.Name== "FromJSON" && x.GetParameters().Length == 2);
        Log.Main.Trace?.Log("JSONSerializationUtility.FromJSON " + genericMethod);

        foreach (
            var jsonTemplated in
            typeof(JSONSerializationUtility)
                .Assembly
                .GetTypes()
                .Where(x => !x.IsAbstract && typeof(IJsonTemplated).IsAssignableFrom(x))
        ) {
            Log.Main.Trace?.Log("IJsonTemplated " + jsonTemplated);

            MethodBase GetMethod(BindingFlags bindingAttr = BindingFlags.Default)
            {
                return jsonTemplated
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | bindingAttr)
                    .SingleOrDefault(x => x.Name == nameof(IJsonTemplated.FromJSON));
            }

            var fromJsonMethod = GetMethod(BindingFlags.DeclaredOnly);
            if (fromJsonMethod == null)
            {
                fromJsonMethod = GetMethod();
            }
            if (fromJsonMethod == null)
            {
                throw new Exception("WTF");
            }
            if (fromJsonMethod.ContainsGenericParameters)
            {
                // TODO required by MDD indexer, goes through alot of jsons!
                Log.Main.Trace?.Log(fromJsonMethod+ " ContainsGenericParameters");
                continue;
            }
            Log.Main.Trace?.Log("IJsonTemplated.FromJSON " + fromJsonMethod);

            yield return genericMethod.MakeGenericMethod(jsonTemplated);
        }
    }

    private static readonly MTStopwatch s_newton = new();
    private static readonly MTStopwatch s_stopwatch = new()
    {
        Callback = stats =>
        {
            var newtonStats = s_newton.GetStats();
            Log.Main.Trace?.Log(
                $"""
                JSONSerializationUtility.FromJSON called {stats.Count} times, taking a total of {stats.TotalTime} with an average of {stats.AverageNanoseconds}ns.
                Newton called {newtonStats.Count} times, taking a total of {newtonStats.TotalTime} with an average of {newtonStats.AverageNanoseconds}ns.
                """
            );
        },
        CallbackForEveryNumberOfMeasurements = 100
    };

    internal class MyState
    {
        internal MTStopwatch.Tracker Tracker;
        internal long counter = Interlocked.Increment(ref s_counter);
    }

    private static long s_counter;

    [HarmonyPriority(Priority.First)]
    public static void Prefix(object target, string json, ref MyState __state)
    {
        __state = new MyState();

        if (testNewton && target.GetType() == typeof(MechDef))
        {
            s_newton.Start();
            try
            {
                var mechDef = new MechDef();
                HBSJsonUtils.PopulateObject(mechDef, json);
                var output = HBSJsonUtils.SerializeObject(mechDef);
                var path = Path.Combine(basePath, $"{__state.counter}_N.json");
                File.WriteAllText(path, output);
            }
            catch (Exception ex)
            {
                Log.Main.Error?.Log("Error populating " + target.GetType(), ex);
            }
            finally
            {
                s_newton.Stop();
            }
        }

        __state.Tracker.Begin();
    }

    [HarmonyPriority(Priority.Last)]
    public static void Postfix(object target, string json, ref MyState __state)
    {
        s_stopwatch.AddMeasurement(__state.Tracker.End());

        if (testNewton)
        {
            try
            {
                var output = HBSJsonUtils.SerializeObject(target);
                var path = Path.Combine(basePath, $"{__state.counter}_H.json");
                File.WriteAllText(path, output);
            }
            catch (Exception ex)
            {
                Log.Main.Error?.Log("Error populating " + target.GetType(), ex);
            }
        }
    }
}