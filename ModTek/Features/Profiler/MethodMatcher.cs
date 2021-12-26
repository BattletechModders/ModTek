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
                .GroupBy(m => m.Name)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList()
                );
        }

        internal bool MatchesMethod(Type type, MethodInfo method)
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
                if (signature.SubClassOf != null && !type.IsSubclassOf(signature.SubClassOf))
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        internal bool MatchesType(Type type)
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
    }
}
