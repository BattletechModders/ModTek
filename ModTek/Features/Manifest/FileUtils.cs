using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Util.Stopwatch;

namespace ModTek.Features.Manifest;

internal static class FileUtils2
{
    private static readonly string[] s_ignoreList =
    [
        ".DS_STORE",
        "~",
        ".nomedia"
    ];

    private static bool FileIsOnDenyList(string filePath)
    {
        return s_ignoreList.Any(x => filePath.EndsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    internal static readonly MTStopwatch FindFilesSW = new();
    internal static List<string> FindFiles(string basePath, params string[] suffixes)
    {
        var dirs = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
        var start = MTStopwatch.GetTimestamp();
        var query = dirs.Where(path => !FileIsOnDenyList(path));
        if (suffixes is { Length: > 0 })
        {
            query = query.Where(path => suffixes.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase)));
        }
        var result = query.ToList();
        FindFilesSW.EndMeasurement(start);
        return result;
    }
}