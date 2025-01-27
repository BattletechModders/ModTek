using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;

namespace HarmonyXInterop;

public static class HarmonyInterop
{
    private static readonly Func<MethodBase, PatchInfo, MethodInfo> UpdateWrapper =
        AccessTools.MethodDelegate<Func<MethodBase, PatchInfo, MethodInfo>>(
            AccessTools.Method(typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.PatchFunctions"),
                "UpdateWrapper"));

    private static readonly HashSet<MethodBase> PrefixesWrapped = new();
    private static readonly Stopwatch OverheadTracking = new();
    public static void ApplyPatch(MethodBase target, PatchInfoWrapper add, PatchInfoWrapper remove)
    {
        try
        {
            PatchInfo pInfo;
            OverheadTracking.Start();
            try
            {
                pInfo = target.ToPatchInfo();
                lock (pInfo)
                {
                    if (PrefixesWrapped.Contains(target))
                    {
                        pInfo.prefixes = Sync(
                            WrapMethods(add.prefixes, PrefixInterop.WrapInterop),
                            WrapMethods(remove.prefixes, PrefixInterop.WrapInterop),
                            pInfo.prefixes
                        );
                    }
                    else if (
                        (add.prefixes.Any(p => p.method.ReturnType == typeof(bool)) && pInfo.prefixes.Any())
                        ||
                        (add.prefixes.Any() && pInfo.prefixes.Any(p => p.PatchMethod.ReturnType == typeof(bool)))
                    ) {
                        Logging.Info($"Detected a mix of skippable and skipping prefixes, wrapping for harmony 1 interoperability");
                        PrefixesWrapped.Add(target);
                        pInfo.prefixes = Sync(add.prefixes, remove.prefixes, pInfo.prefixes);
                        foreach (var patch in pInfo.prefixes)
                        {
                            patch.PatchMethod = PrefixInterop.WrapInterop(patch.PatchMethod);
                        }
                    }
                    else
                    {
                        pInfo.prefixes = Sync(add.prefixes, remove.prefixes, pInfo.prefixes);
                    }
                    pInfo.postfixes = Sync(add.postfixes, remove.postfixes, pInfo.postfixes);
                    pInfo.transpilers = Sync(
                        WrapMethods(add.transpilers, TranspilerInterop.WrapInterop),
                        WrapMethods(remove.transpilers, TranspilerInterop.WrapInterop),
                        pInfo.transpilers
                    );
                    pInfo.finalizers = Sync(add.finalizers, remove.finalizers, pInfo.finalizers);
                }
            }
            finally
            {
                OverheadTracking.Stop();
                Logging.Info($"Cumulative overhead setting up Harmony 1<>X interoperability: {OverheadTracking.ElapsedMilliseconds} ms");
            }

            UpdateWrapper(target, pInfo);
        }
        catch (Exception e)
        {
            Logging.Error($"Could not patch: {e.Message}{new StackTrace(e, true)}{Environment.NewLine}{new StackTrace(1, true)}");
            throw;
        }
    }

    private static PatchMethod[] WrapMethods(PatchMethod[] patches, Func<MethodInfo, MethodInfo> wrapper)
    {
        return patches.Select(p => WrapMethod(p, wrapper)).ToArray();
    }

    private static PatchMethod WrapMethod(PatchMethod patch, Func<MethodInfo, MethodInfo> wrapper)
    {
        return new()
        {
            after = patch.after,
            before = patch.before,
            method = wrapper(patch.method),
            owner = patch.owner,
            priority = patch.priority
        };
    }

    private static Patch[] Sync(PatchMethod[] add, PatchMethod[] remove, Patch[] current)
    {
        if (add.Length == 0 && remove.Length == 0)
            return current;
        current = current.Where(p => !remove.Any(r => r.method == p.PatchMethod && r.owner == p.owner)).ToArray();
        var initialIndex = current.Length;
        return current.Concat(add.Where(method => method != null).Select((method, i) =>
            new Patch(method.ToHarmonyMethod(), i + initialIndex, method.owner))).ToArray();
    }
}