using System;
using Harmony;

namespace ModTek.Util
{
    internal static class MTUnityUtils
    {
        internal static bool CurrentThreadIsMainThread()
        {
            return Traverse.Create(typeof(UnityEngine.Object)).Method("CurrentThreadIsMainThread").GetValue<bool>();
        }

        internal static void EnsureRunningOnMainThread()
        {
            if (!CurrentThreadIsMainThread())
            {
                throw new InvalidOperationException("EnsureRunningOnMainThread can only be called from the main thread");
            }
        }
    }
}
