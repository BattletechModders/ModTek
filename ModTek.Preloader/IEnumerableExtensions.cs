using System;
using System.Collections.Generic;
using System.Linq;

namespace ModTek.Preloader;

internal static class IEnumerableExtensions
{
    internal static void LogAsList(this IEnumerable<object> list, string title)
    {
        Logger.Main.Log(list
            .Select(o => o.ToString())
            .OrderBy(s => s)
            .Aggregate(title, (prev, item) => prev + Environment.NewLine + " - " + item));
    }
}