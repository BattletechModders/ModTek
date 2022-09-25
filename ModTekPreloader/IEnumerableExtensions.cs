using System;
using System.Collections.Generic;
using System.Linq;
using ModTekPreloader.Logging;

namespace ModTekPreloader
{
    internal static class IEnumerableExtensions
    {
        internal static void LogAsList(this IEnumerable<object> list, string title)
        {
            Logger.Log(list
                .Select(o => o.ToString())
                .OrderBy(s => s)
                .Aggregate(title, (prev, item) => prev + Environment.NewLine + " - " + item)
            );
        }
    }
}
