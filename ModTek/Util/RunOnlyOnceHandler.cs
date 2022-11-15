using System;
using System.Threading;

namespace ModTek.Util;

internal class RunOnlyOnceHandler
{
    private int hasExecuted;
    internal void Run(Action callback) {
        if (Interlocked.CompareExchange(ref hasExecuted, 1, 0) == 1)
        {
            return;
        }
        callback();
    }
}