using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BattleTech;
using Harmony;
using Harmony.ILCopying;
using ModTek.Features.Logging;
using ModTek.Util;

namespace ModTek.Features.Profiler
{
    internal class PatchableChecker
    {
        internal bool DebugLogging = false;

        private readonly Assembly[] BlacklistedAssemblies;
        private readonly Type[] BlacklistedTypes;
        internal PatchableChecker()
        {
            BlacklistedAssemblies =
                AssemblyUtil.GetAssembliesByPattern(ModTek.Config.Profiling.BlacklistedAssemblyNamePattern)
                    .ToArray();
            MTLogger.Log("profiler blacklisted assemblies:" + BlacklistedAssemblies.Select(x => x.FullName).AsTextList());

            BlacklistedTypes = ModTek.Config.Profiling.BlacklistedTypeNames
                .Select(AssemblyUtil.GetTypeByName)
                .Where(a => a != null)
                .ToArray();
            MTLogger.Log("profiler blacklisted types:" + BlacklistedTypes.Select(x => x.FullName).AsTextList());
        }

        internal bool IsAssemblyPatchable(Assembly assembly)
        {
            if (BlacklistedAssemblies.Contains(assembly))
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsAssemblyPatchable BlacklistedAssemblies assembly=" + assembly.FullName);
                }
                return false;
            }
            return true;
        }

        internal bool IsTypePatchable(Type type)
        {
            if (!type.IsClass)
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsTypePatchable !IsClass type=" + type.FullName);
                }
                return false;
            }
            // if (type.IsAbstract)
            // {
            //     if (DebugLogging)
            //     {
            //         MTLogger.Log("IsTypePatchable IsAbstract type=" + type.FullName);
            //     }
            //     return false;
            // }
            // generic patching with harmony is not fool proof
            if (type.IsGenericType || type.IsConstructedGenericType || type.ContainsGenericParameters)
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsTypePatchable IsGeneric type=" + type.FullName);
                }
                return false;
            }
            if (BlacklistedTypes.Contains(type))
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsTypePatchable BlacklistedTypes type=" + type.FullName);
                }
                return false;
            }
            return true;
        }

        internal bool IsMethodPatchable(MethodInfo method)
        {
            if (method.IsConstructor)
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsMethodPatchable IsConstructor method=" + method.FullDescription());
                }
                return false;
            }
            if (method.IsAbstract)
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsMethodPatchable IsAbstract method=" + method.FullDescription());
                }
                return false;
            }
            // generic patching with harmony is not fool proof
            if (method.IsGenericMethod || method.ContainsGenericParameters)
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsMethodPatchable IsGeneric method=" + method.FullDescription());
                }
                return false;
            }

            // this can be a harmful check, crashing the game
            // therefore keep it as the last check and do log any exceptions
            try
            {
                if (method.GetMethodBody() == null)
                {
                    if (DebugLogging)
                    {
                        MTLogger.Log("IsMethodPatchable GetMethodBody=null");
                    }
                    return false;
                }
            }
            catch (Exception e)
            {
                MTLogger.Log($"Error checking for body in {method.FullDescription()}", e);
                return false;
            }

            return true;
        }

        internal IEnumerable<MethodBase> FindPatchableMethodsCalledFromMethod(MethodBase containerMethod, int depth)
        {
            try
            {
                if (depth > 0)
                {
                    return FindMethodsCalledByMethodToBeWrapped(containerMethod, depth);
                }
            }
            catch (Exception e)
            {
                MTLogger.Log($"Issue finding methods in {containerMethod}", e);
            }
            return Array.Empty<MethodBase>();
        }

        private IEnumerable<MethodBase> FindMethodsCalledByMethodToBeWrapped(MethodBase containerMethod, int depth)
        {
            var furtherDepth = depth - 1;
            foreach (var method in FindMethodsCalledByMethod(containerMethod))
            {
                yield return method;
                if (furtherDepth <= 0)
                {
                    continue;
                }
                foreach (var callee in FindPatchableMethodsCalledFromMethod(method, furtherDepth))
                {
                    yield return callee;
                }
            }
        }

        internal bool IsPatchable(MethodInfo method)
        {
            var type = method.DeclaringType;
            if (type == null)
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsPatchable type=null method=" + method.FullDescription());
                }
                return false;
            }
            if (!IsAssemblyPatchable(type.Assembly))
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsPatchable !IsAssemblyPatchable method=" + method.FullDescription());
                }
                return false;
            }
            if (!IsTypePatchable(type))
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsPatchable !IsTypePatchable method=" + method.FullDescription());
                }
                return false;
            }
            if (!IsMethodPatchable(method))
            {
                if (DebugLogging)
                {
                    MTLogger.Log("IsPatchable !IsMethodPatchable method=" + method.FullDescription());
                }
                return false;
            }
            return true;
        }

        private int counter;
        private Assembly btAssembly = typeof(UnityGameInstance).Assembly;
        private IEnumerable<MethodBase> FindMethodsCalledByMethod(MethodBase containerMethod)
        {
            var dynamicMethod = DynamicTools.CreateDynamicMethod(containerMethod, "_Profiler" + counter++).GetILGenerator();

            var instructions = MethodBodyReader.GetInstructions(dynamicMethod, containerMethod);
            var callees = instructions
                .Where(i => i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt)
                .Select(i => i.operand);

            foreach (var callee in callees)
            {
                if (!(callee is MethodInfo method))
                {
                    // all the time a constructor, maybe we should also profile those?
                    continue;
                }
                // if (assembly != btAssembly)
                // {
                //     continue;
                // }
                if (!IsPatchable(method))
                {
                    continue;
                }
                yield return method;
            }
        }
    }
}
