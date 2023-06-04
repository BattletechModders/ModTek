using System.Collections.Generic;
using System.Linq;

namespace ModTek.Common.Utils;

internal static class CSharpUtils
{
    internal static IEnumerator<T> Enumerate<T>(params IEnumerator<T>[] enumerators)
    {
        foreach (var enumerator in enumerators)
        {
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    internal static string AsTextList(this IEnumerable<string> items)
    {
        return items?.Aggregate("", (current, item) => current + AsTextListLine(item));
    }

    private const string TextListLinePrefix = "\n - ";
    internal static string AsTextListLine(string line)
    {
        return TextListLinePrefix + line;
    }
}