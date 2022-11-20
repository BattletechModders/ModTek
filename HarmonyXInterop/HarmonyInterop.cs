using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using HarmonyLib.Public.Patching;

namespace HarmonyXInterop
{
    public static class HarmonyInterop
    {
        private static readonly Func<MethodBase, PatchInfo, MethodInfo> UpdateWrapper =
            AccessTools.MethodDelegate<Func<MethodBase, PatchInfo, MethodInfo>>(
                AccessTools.Method(typeof(HarmonyManipulator).Assembly.GetType("HarmonyLib.PatchFunctions"),
                    "UpdateWrapper"));

        public static void ApplyPatch(MethodBase target, PatchInfoWrapper add, PatchInfoWrapper remove)
        {
            var pInfo = target.ToPatchInfo();
            lock (pInfo)
            {
                pInfo.prefixes = Sync(
                    WrapMethods(add.prefixes, PrefixInterop.WrapInterop),
                    WrapMethods(remove.prefixes, PrefixInterop.WrapInterop),
                    pInfo.prefixes
                );
                pInfo.postfixes = Sync(add.postfixes, remove.postfixes, pInfo.postfixes);
                pInfo.transpilers = Sync(
                    WrapMethods(add.transpilers, TranspilerInterop.WrapInterop),
                    WrapMethods(remove.transpilers, TranspilerInterop.WrapInterop),
                    pInfo.transpilers
                );
                pInfo.finalizers = Sync(add.finalizers, remove.finalizers, pInfo.finalizers);
            }

            UpdateWrapper(target, pInfo);
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
}