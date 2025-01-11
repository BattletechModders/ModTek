using System;
using System.Collections.Generic;
using HBS.Reflection;

namespace ModTek.Features.BetterReflectionCache;

[HarmonyPatch(typeof(ReflectionCache))]
public class BetterReflectionCache_Patches
{
    private static readonly Dictionary<Key, MemberInfoHelper> s_members = new();
    private static readonly Dictionary<Key, MethodInfoHelper> s_methods = new();

    private record struct Key(Type Type, string MemberName);

    /*
    [HarmonyPatch(nameof(ReflectionCache.Instance))]
    [HarmonyPrepare]
    public static void Migrate()
    {
        foreach (var helper in ReflectionCache.instance.members.Values)
        {
            s_members[key] = helper;
        }
    }
    */

    [HarmonyPatch(nameof(ReflectionCache.Get))]
    [HarmonyPrefix]
    public static void Get(
        ref bool __runOriginal,
        ref bool __result,
        Type t,
        string memberName,
        object target,
        out object result,
        params object[] idx)
    {
        __runOriginal = false;
        if (TryCacheMember(t, memberName, out var helper))
        {
            result = helper.GetValue(target, idx);
            __result = true;
            return;
        }
        result = null;
        __result = false;
    }

    [HarmonyPatch(nameof(ReflectionCache.Set))]
    [HarmonyPrefix]
    public static void Set(
        ref bool __runOriginal,
        ref bool __result,
        Type t,
        string memberName,
        ref object target,
        object value,
        params object[] idx)
    {
        __runOriginal = false;
        if (TryCacheMember(t, memberName, out var helper))
        {
            helper.SetValue(ref target, value, idx);
            __result = true;
            return;
        }
        __result = false;
    }

    [HarmonyPatch(nameof(ReflectionCache.GetMemberHelper))]
    [HarmonyPrefix]
    public static void GetMemberHelper(
        ref bool __runOriginal,
        ref MemberInfoHelper __result,
        Type t,
        string memberName)
    {
        __runOriginal = false;
        __result = TryCacheMember(t, memberName, out var helper) ? helper : null;
    }

    private static bool TryCacheMember(Type t, string memberName, out MemberInfoHelper helper)
    {
        var key = new Key(t, memberName);
        if (s_members.TryGetValue(key, out helper))
        {
            return true;
        }
        helper = MemberInfoHelper.GetHelper(t, memberName);
        if (helper == null || !helper.Initalized)
        {
            return false;
        }
        s_members[key] = helper;
        return true;
    }

    [HarmonyPatch(nameof(ReflectionCache.Invoke))]
    [HarmonyPrefix]
    public static void Invoke(
        ref bool __runOriginal,
        ref bool __result,
        Type t,
        string methodName,
        object target,
        out object result,
        params object[] args)
    {
        __runOriginal = false;
        if (TryCacheMethod(t, methodName, out var helper))
        {
            result = helper.Call(target, args);
            __result = true;
            return;
        }

        result = null;
        __result = false;
    }

    [HarmonyPatch(nameof(ReflectionCache.GetMethodHelper))]
    [HarmonyPrefix]
    public static void GetMethodHelper(
        ref bool __runOriginal,
        ref MethodInfoHelper __result,
        Type t,
        string memberName)
    {
        __runOriginal = false;
        __result = TryCacheMethod(t, memberName, out var helper) ? helper : null;
    }

    private static bool TryCacheMethod(Type t, string methodName, out MethodInfoHelper helper)
    {
        var key = new Key(t, methodName);
        if (s_methods.TryGetValue(key, out helper))
        {
            return true;
        }
        helper = MethodInfoHelper.GetHelper(t, methodName);
        if (helper == null || !helper.Initalized)
        {
            return false;
        }
        s_methods[key] = helper;
        return true;
    }

    [HarmonyPatch(
        nameof(ReflectionCache.TryCacheMethod),
        [typeof(Type), typeof(string), typeof(string)],
        [ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out]
    )]
    [HarmonyPrefix]
    public static void TryCacheMethod(ref bool __runOriginal)
    {
        __runOriginal = false;
        throw new NotImplementedException();
    }
}