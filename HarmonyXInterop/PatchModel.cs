using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace HarmonyXInterop
{
    public class PatchInfoWrapper
    {
        public PatchMethod[] finalizers;
        public PatchMethod[] postfixes;
        public PatchMethod[] prefixes;
        public PatchMethod[] transpilers;
    }

    public class PatchMethod
    {
        /// <summary>After parameter</summary>
        public string[] after;

        /// <summary>Before parameter</summary>
        public string[] before;

        public MethodInfo method; // need to be called 'method'
        public string owner;

        /// <summary>Priority</summary>
        public int priority = -1;

        public HarmonyMethod ToHarmonyMethod()
        {
            return new HarmonyMethod
            {
                after = after,
                before = before,
                method = method,
                priority = priority
            };
        }
    }

    public class PatchMethodComparer : IEqualityComparer<PatchMethod>
    {
        public static PatchMethodComparer Instance { get; } = new PatchMethodComparer();
        
        public bool Equals(PatchMethod x, PatchMethod y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return Equals(x.method, y.method) && x.owner == y.owner;
        }

        public int GetHashCode(PatchMethod obj)
        {
            unchecked
            {
                return ((obj.method != null ? obj.method.GetHashCode() : 0) * 397) ^ (obj.owner != null ? obj.owner.GetHashCode() : 0);
            }
        }
    }
}