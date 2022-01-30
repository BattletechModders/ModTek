using System;
using System.Reflection.Emit;
using System.Threading;
using Harmony;

namespace ModTek.Util
{
    internal static class MTUnityUtils
    {
        internal static void Init()
        {
            {
                var type = typeof(UnityEngine.Object);
                var method = AccessTools.Method(type, "CurrentThreadIsMainThread");
                var dm = new DynamicMethod(
                    "ModTek_CurrentThreadIsMainThread",
                    typeof(bool),
                    null,
                    type
                );
                var gen = dm.GetILGenerator();
                gen.Emit(OpCodes.Call, method);
                gen.Emit(OpCodes.Ret);
                CurrentThreadIsMainThread = (Func<bool>) dm.CreateDelegate(typeof(Func<bool>));
            }
            if (!CurrentThreadIsMainThread())
            {
                throw new InvalidOperationException();
            }
            MainManagedThreadId = Thread.CurrentThread.ManagedThreadId;
        }
        private static Func<bool> CurrentThreadIsMainThread;
        internal static int MainManagedThreadId { get; private set; }
    }
}
