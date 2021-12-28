using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ModTek.Features.Profiler
{
    internal class MethodMatcher
    {
        private readonly Dictionary<string, List<MethodMatchFilter>> groups;

        internal MethodMatcher(MethodMatchFilter[] signatures)
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

        internal bool MatchesAssembly(Assembly assembly)
        {
            return true;
        }

        internal bool MatchesType(Assembly assembly, Type type)
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

        internal bool MatchesMethod(Assembly assembly, Type type, MethodInfo method)
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
                if (signature.ReturnType != null && signature.ReturnType != method.ReturnType)
                {
                    continue;
                }
                if (signature.ParameterTypes != null && !signature.ParameterTypes.SequenceEqual(parameterTypes))
                {
                    continue;
                }
                if (signature.SubClassOfType != null && !type.IsSubclassOf(signature.SubClassOfType))
                {
                    continue;
                }
                if (signature.Assembly != null && signature.Assembly != assembly)
                {
                    continue;
                }

                return true;
            }

            return false;
        }
    }
}
