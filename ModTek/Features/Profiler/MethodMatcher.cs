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
        internal static IEnumerable<MethodBase> FindMethodsToProfile(MethodMatchFilter[] signatures)
        {
            var matcher = new MethodMatcher(signatures);
            return matcher.FindMethods();
        }

        private readonly Dictionary<string, List<MethodMatchFilter>> groups;

        private MethodMatcher(MethodMatchFilter[] signatures)
        {
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
                foreach (var p in FindMethodsInAssembly(assembly))
                {
                    yield return p;
                }
            }
        }

        private IEnumerable<MethodBase> FindMethodsInAssembly(Assembly assembly)
        {
            if (!MatchesAssembly(assembly))
            {
                yield break;
            }

            // patch all Update methods in base game
            foreach (var type in AssemblyUtil.GetTypesSafe(assembly))
            {
                if (!MatchesType(assembly, type))
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
                    MTLogger.Log($"Can't get methods from Type {type}", e);
                    continue;
                }

                foreach (var method in methods)
                {
                    if (!MatchesMethod(assembly, type, method))
                    {
                        continue;
                    }

                    yield return method;
                }
            }
        }

        private bool MatchesAssembly(Assembly assembly)
        {
            return true;
        }

        private bool MatchesType(Assembly assembly, Type type)
        {
            if (!type.IsClass)
            {
                return false;
            }
            if (type.IsAbstract)
            {
                return false;
            }
            // generic patching with harmony is not fool proof
            if (type.ContainsGenericParameters)
            {
                return false;
            }
            return true;
        }

        private bool MatchesMethod(Assembly assembly, Type type, MethodInfo method)
        {
            if (string.IsNullOrEmpty(method.Name) || !groups.TryGetValue(method.Name, out var group))
            {
                return false;
            }
            if (method.IsConstructor)
            {
                return false;
            }
            if (method.IsAbstract)
            {
                return false;
            }
            if (method.GetMethodBody() == null)
            {
                return false;
            }

            // generic patching with harmony is not fool proof
            if (method.ContainsGenericParameters)
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
