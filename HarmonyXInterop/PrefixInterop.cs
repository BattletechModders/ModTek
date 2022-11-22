using System;
using System.Collections.Generic;
using System.Reflection;

namespace HarmonyXInterop;

internal static class PrefixInterop
{
    private static readonly Dictionary<MethodInfo, MethodInfo> Wrappers = new();

    public static MethodInfo WrapInterop(MethodInfo original)
    {
        try
        {
            if (!original.IsStatic)
            {
                throw new ArgumentException("Patch can't be non-static");
            }
            if (original.DeclaringType == null)
            {
                throw new ArgumentException("Patch method has to have a DeclaringType");
            }
            if (original.DeclaringType.FullName == null)
            {
                throw new ArgumentException("Patch DeclaringType has to have a full name");
            }

            lock (Wrappers)
            {
                if (!Wrappers.TryGetValue(original, out var wrapper))
                {
                    wrapper = WrapperClassBuilder.CreatePrefixWrapper(original);
                    Wrappers[original] = wrapper;
                }
                return wrapper;
            }
        }
        catch (Exception e)
        {
            Logging.Error($"Error creating prefix wrapper: {e}");
        }
        return original;
    }
}