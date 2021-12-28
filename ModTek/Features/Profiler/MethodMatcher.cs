using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ModTek.Features.Logging;
using ModTek.Util;

namespace ModTek.Features.Profiler
{
    internal class MethodMatcher
    {
        internal static IEnumerable<MethodBase> FindMethodsToProfile(MethodMatchFilter[] signatures, PatchableChecker patchableChecker)
        {
            var matcher = new MethodMatcher(signatures, patchableChecker);
            return matcher.FindMethods();
        }

        private readonly PatchableChecker patchableChecker;
        private readonly Dictionary<string, List<MethodMatchFilter>> groups;

        private MethodMatcher(MethodMatchFilter[] signatures, PatchableChecker patchableChecker)
        {
            this.patchableChecker = patchableChecker;
            groups = signatures
                .Where(s => s.Enabled)
                .Where(s => !string.IsNullOrEmpty(s.Name))
                .Where(s => s.FillTypes())
                .GroupBy(s => s.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );
        }

        private IEnumerable<MethodBase> FindMethods()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!patchableChecker.IsAssemblyPatchable(assembly))
                {
                    continue;
                }

                foreach (var p in FindMethodsInAssembly(assembly))
                {
                    yield return p;
                }
            }
        }

        private IEnumerable<MethodBase> FindMethodsInAssembly(Assembly assembly)
        {
            // patch all Update methods in base game
            foreach (var type in AssemblyUtil.GetTypesSafe(assembly))
            {
                if (!patchableChecker.IsTypePatchable(type))
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
                    MTLogger.Log($"Can't get methods from Type {type.FullName}", e);
                    continue;
                }

                foreach (var method in methods)
                {
                    if (!MatchesFilter(assembly, type, method))
                    {
                        continue;
                    }

                    yield return method;

                    foreach (var calledMethod in patchableChecker.FindPatchableMethodsCalledFromMethod(method, ModTek.Config.Profiling.RecursiveDepthToFindCalleesBelowFilteredMethods))
                    {
                        yield return calledMethod;
                    }
                }
            }
        }

        private bool MatchesFilter(Assembly assembly, Type type, MethodInfo method)
        {
            if (string.IsNullOrEmpty(method.Name) || !groups.TryGetValue(method.Name, out var group))
            {
                return false;
            }
            if (!patchableChecker.IsMethodPatchable(method))
            {
                return false;
            }
            var parameterTypes = method.GetParameters().Select(t => t.ParameterType).ToArray();
            foreach (var signature in group)
            {
                if (!signature.MatchMethod(assembly, type, method, parameterTypes))
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
