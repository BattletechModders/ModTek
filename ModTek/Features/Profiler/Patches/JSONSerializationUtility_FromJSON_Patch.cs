using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using BattleTech;
using fastJSON;
using HBS.Util;
using ModTek.Features.Logging;
using ModTek.Misc;
using ModTek.Util;

namespace ModTek.Features.Profiler.Patches;

[HarmonyPatch(typeof(MechDef), nameof(MechDef.RefreshChassis))]
internal static class MechDef_RefreshChassis_Patch
{
    internal static void Prefix(ref bool __runOriginal, MechDef __instance)
    {
        if (__instance.dataManager == null && __instance.Description == null)
        {
            __runOriginal = false;
        }
    }
}

[HarmonyPatch(typeof(MechDef), nameof(MechDef.Chassis), MethodType.Setter)]
internal static class MechDef_set_Chassis_Patch
{
    [HarmonyPriority(Priority.Last)]
    internal static void Prefix(MechDef __instance, ChassisDef value)
    {
        Log.Main.Trace?.Log($"MechDef.Chassis={value?.GetHashCode()}", new Exception());
    }
}

[HarmonyPatch]
internal static class JSONSerializationUtility_FromJSON_Patch
{
    private static string basePath;
    private static bool testNewton = true;

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
        return ModTek.Enabled; // && ModTek.Config.ProfilerEnabled;
    }

    [HarmonyTargetMethods]
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        var genericMethod = typeof(JSONSerializationUtility)
            .GetMethods(BindingFlags.Public|BindingFlags.Static)
            .Single(x => x.Name== nameof(JSONSerializationUtility.FromJSON) && x.GetParameters().Length == 2);
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
            // break;
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

        internal string nn;
        internal string nh;
        internal string hn;
        internal string hh;

        internal void Save()
        {
            var differentPopulate = true;
            var differentSerializers = false; // tag sets and dictionaries work differently
            if ((differentSerializers && nn != nh) || (differentPopulate && hn != nn))
            {
                File.WriteAllText(Path.Combine(basePath, $"{counter}_NN.json"), nn);
            }
            if ((differentSerializers && nn != nh) || (differentPopulate && nh != hh))
            {
                File.WriteAllText(Path.Combine(basePath, $"{counter}_NH.json"), nh);
            }
            if ((differentSerializers && hn != hh) || (differentPopulate && hn != nn))
            {
                File.WriteAllText(Path.Combine(basePath, $"{counter}_HN.json"), hn);
            }
            if ((differentSerializers && hn != hh) || (differentPopulate && nh != hh))
            {
                File.WriteAllText(Path.Combine(basePath, $"{counter}_HH.json"), hh);
            }
        }
    }

    private static long s_counter;

    [HarmonyPriority(Priority.First)]
    public static void Prefix(object target, string json, ref MyState __state)
    {
        if (target == null)
        {
            return;
        }
        if (typeof(MechDef).Assembly != target.GetType().Assembly)
        {
            return;
        }

        __state = new MyState();

        if (testNewton) //  && target.GetType() == typeof(MechDef)
        {
            s_newton.Start();
            try
            {
                var objectCopy = Activator.CreateInstance(target.GetType());

                if (objectCopy is MechDef mechDef1)
                {
                    Log.Main.Trace?.Log($"BWTF???? mechDef.dataManager: {mechDef1.dataManager?.GetHashCode()}");
                    Log.Main.Trace?.Log($"BWTF???? mechDef._chassisDef: {mechDef1._chassisDef?.GetHashCode()}");
                }

                HBSJsonUtils.PopulateObject(objectCopy, json);

                if (objectCopy is MechDef mechDef2)
                {
                    Log.Main.Trace?.Log($"AWTF???? mechDef.dataManager: {mechDef2.dataManager?.GetHashCode()}");
                    Log.Main.Trace?.Log($"AWTF???? mechDef._chassisDef: {mechDef2._chassisDef?.GetHashCode()}");
                }

                __state.nn = HBSJsonUtils.SerializeObject(objectCopy);
                __state.nh = JSON.ToJSON(objectCopy);
            }
            catch (Exception ex)
            {
                Log.Main.Error?.Log("Error Populating and Serializing " + target.GetType(), ex);
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
        if (__state == null)
        {
            return;
        }

        s_stopwatch.AddMeasurement(__state.Tracker.End());

        if (testNewton) // && target.GetType() == typeof(MechDef)
        {
            try
            {
                __state.hn = HBSJsonUtils.SerializeObject(target);
                __state.hh = JSON.ToJSON(target);
            }
            catch (Exception ex)
            {
                Log.Main.Error?.Log("Error Serializing " + target.GetType(), ex);
            }

            try
            {
                __state.Save();
            }
            catch (Exception ex)
            {
                Log.Main.Error?.Log("Error Saving JSONs " + target.GetType(), ex);
            }
        }
    }
}