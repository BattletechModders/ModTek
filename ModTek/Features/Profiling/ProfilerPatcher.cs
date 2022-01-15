using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using ModTek.Features.Logging;
using ModTek.Features.Profiling.MPatches;
using ModTek.UI;
using ModTek.Util;
using UnityEngine.Profiling;

namespace ModTek.Features.Profiling
{
    internal static class ProfilerPatcher
    {
        internal static ModTekProfiler ModTekProfiler;

        internal static IEnumerator<ProgressReport> ProfilerSetupLoop()
        {
            if (!ModTek.Enabled || !ModTek.Config.Profiling.Enabled)
            {
                yield break;
            }

            var sliderText = "Patching methods for profiling";
            yield return new ProgressReport(1, sliderText, "Gathering methods to be profiled", true);
            MTLogger.Info.Log(sliderText);

            if (ModTek.Config.Profiling.UseUnityProfilerIfSupported)
            {
                Profiler.enabled = true;
                Profiler.maxUsedMemory = ModTek.Config.Profiling.UnityProfilerMaxMemory;
                MTLogger.Info.Log($"Unity Profiling supported={Profiler.supported} enabled={Profiler.enabled}");
            }

            var harmony = HarmonyInstance.Create("ModTek.Profiler");
            {
                var methodTuples = TargetMethods(harmony)
                    .Select(m => (Method: m, AssemblyName: m.ReflectedType?.Assembly.GetName().Name, TypeName: m.ReflectedType?.FullName))
                    .OrderBy(t => t.AssemblyName)
                    .ThenBy(t => t.TypeName)
                    .ThenBy(t => t.Method.Name)
                    .ToList();

                HarmonyMethod prefix, postfix;
                if (ModTek.Config.Profiling.UseUnityProfiler && ModTek.Config.Profiling.ModTekProfiler.Enabled)
                {
                    prefix = new HarmonyMethod(AccessTools.Method(typeof(Profiling_UnityAndModTek_Patch), nameof(Profiling_UnityAndModTek_Patch.Prefix)));
                    postfix = new HarmonyMethod(AccessTools.Method(typeof(Profiling_UnityAndModTek_Patch), nameof(Profiling_UnityAndModTek_Patch.Postfix)));
                }
                else if (ModTek.Config.Profiling.UseUnityProfiler)
                {
                    prefix = new HarmonyMethod(AccessTools.Method(typeof(Profiling_UnityOnly_Patch), nameof(Profiling_UnityOnly_Patch.Prefix)));
                    postfix = new HarmonyMethod(AccessTools.Method(typeof(Profiling_UnityOnly_Patch), nameof(Profiling_UnityOnly_Patch.Postfix)));
                }
                else if (ModTek.Config.Profiling.ModTekProfiler.Enabled)
                {
                    prefix = new HarmonyMethod(AccessTools.Method(typeof(Profiling_ModTekOnly_Patch), nameof(Profiling_ModTekOnly_Patch.Prefix)));
                    postfix = new HarmonyMethod(AccessTools.Method(typeof(Profiling_ModTekOnly_Patch), nameof(Profiling_ModTekOnly_Patch.Postfix)));
                }
                else
                {
                    throw new ArgumentException("When enabling profiling, need either unity or modtek profiler enabled!");
                }

                if (ModTek.Config.Profiling.ModTekProfiler.Enabled)
                {
                    ModTekProfiler = new ModTekProfiler();
                }

                prefix.prioritiy = Priority.First;
                postfix.prioritiy = Priority.Last;
                MTLogger.Info.Log($"\tPatching using Prefix {prefix.method.GetFullName()} and Postfix {postfix.method.GetFullName()}");

                var countCurrent = 0;
                var countMax = (float) methodTuples.Count;
                foreach (var t in methodTuples)
                {
                    yield return new ProgressReport(
                        countCurrent++/countMax,
                        sliderText,
                        $"{t.AssemblyName}\n{t.TypeName}\n{t.Method.Name}"
                    );
                    MTLogger.Info.Log($"\tPatching {t.TypeName}.{t.Method.Name} in {t.AssemblyName}");

                    var processor = new PatchProcessor(
                        harmony,
                        new List<MethodBase> { t.Method },
                        prefix,
                        postfix
                    );
                    try
                    {
                        processor.Patch();
                    }
                    catch (Exception e)
                    {
                        MTLogger.Warning.Log("Failed applying profiler patch", e);
                    }
                }
            }

            if (ModTek.Config.Profiling.ModTekProfiler.Enabled)
            {
                new PatchProcessor(harmony, typeof(UnityGameInstance_Update_Patch), null).Patch();
            }

            if (ModTek.Config.Profiling.CoverCoroutines)
            {
                new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMoveNext_Patch), null).Patch();
                new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeMember_Patch), null).Patch();
                new PatchProcessor(harmony, typeof(SetupCoroutine_InvokeStatic_Patch), null).Patch();
            }
        }

        internal static IEnumerable<MethodBase> TargetMethods(HarmonyInstance harmony)
        {
            var patchableChecker = new PatchableChecker();
            var methods = MethodMatcher
                .FindMethodsToProfile(ModTek.Config.Profiling.Filters, patchableChecker)
                .ToHashSet();

            foreach (var method in harmony.GetPatchedMethods())
            {
                var info = harmony.GetPatchInfo(method);
                if (info == null || method.DeclaringType == null)
                {
                    continue;
                }
                if (!methods.Contains(method))
                {
                    continue;
                }

                // patchableChecker.DebugLogging = true;
                void addPatches(IEnumerable<Patch> patches)
                {
                    foreach (var patch in patches)
                    {
                        if (patch.owner == harmony.Id)
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

            // make sure to not patch twice
            methods.Remove(UnityGameInstance_Update_Patch.TargetMethod());
            methods.Remove(SetupCoroutine_InvokeMoveNext_Patch.TargetMethod());
            methods.Remove(SetupCoroutine_InvokeMember_Patch.TargetMethod());
            methods.Remove(SetupCoroutine_InvokeStatic_Patch.TargetMethod());

            return methods;
        }
    }
}