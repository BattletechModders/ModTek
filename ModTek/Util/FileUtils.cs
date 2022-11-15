using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Misc;

namespace ModTek.Util;

internal static class FileUtils
{
    // this is more an informative path that works for diagnosing issues
    internal static string GetRelativePath(string absolutePath)
    {
        return new Uri(FilePaths.BaseDirectory).MakeRelativeUri(new Uri(absolutePath)).ToString();
    }

    // this is the correct relative path with proper directory separators for internal use
    internal static string GetRealRelativePath(string absolutePath, string basePath)
    {
        if (!Path.IsPathRooted(absolutePath))
        {
            return absolutePath;
        }

        basePath = Path.GetFullPath(basePath);
        if (basePath.Last() != Path.DirectorySeparatorChar)
        {
            basePath += Path.DirectorySeparatorChar;
        }

        var pathUri = new Uri(Path.GetFullPath(absolutePath), UriKind.Absolute);
        var rootUri = new Uri(basePath, UriKind.Absolute);

        if (pathUri.Scheme != rootUri.Scheme)
        {
            return absolutePath;
        }

        var relativeUri = rootUri.MakeRelativeUri(pathUri);
        var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (pathUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
    }

    private static readonly string[] IGNORE_LIST =
    {
        ".DS_STORE",
        "~",
        ".nomedia"
    };

    internal static bool FileIsOnDenyList(string filePath)
    {
        return IGNORE_LIST.Any(x => filePath.EndsWith(x, StringComparison.OrdinalIgnoreCase));
    }

    internal static List<string> FindFiles(string basePath, params string[] suffixes)
    {
        var query = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories)
            .Where(path => !FileIsOnDenyList(path));
        if (suffixes != null && suffixes.Length > 0)
        {
            query = query.Where(path => suffixes.Any(p => path.EndsWith(p, StringComparison.OrdinalIgnoreCase)));
        }
        return query.ToList();
    }

    internal const string JSON_TYPE = ".json";
    private const string CSV_TYPE = ".csv";
    private const string TXT_TYPE = ".txt";

    internal static bool IsJson(string name)
    {
        return name.HasExtension(JSON_TYPE);
    }

    internal static bool IsCsv(string name)
    {
        return name.HasExtension(CSV_TYPE);
    }

    internal static bool IsTxt(string name)
    {
        return name.HasExtension(TXT_TYPE);
    }

    internal static string GetExtension(this string str)
    {
        var index = str.LastIndexOf('.');
        return index >= 0 ? str.Substring(index) : default;
    }

    internal static bool HasExtension(this string str, string ext)
    {
        var index = str.LastIndexOf('.');
        return index >= 0 && str.Substring(index).Equals(ext);
    }

    internal static bool HasStringExtension(this string str)
    {
        var ext = str.GetExtension();
        return ext != null && StringExtensions.Contains(ext);
    }

    internal static readonly string[] StringExtensions = {
        JSON_TYPE,
        CSV_TYPE,
        TXT_TYPE,
    };

    internal static void CleanDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }
        Directory.CreateDirectory(path);
    }

    public static void CreateParentOfPath(string path)
    {
        var fi = new FileInfo(path);
        var dir = fi.Directory;
        if (dir != null && !dir.Exists)
        {
            dir.Create();
        }
    }

    internal static StreamReader StreamReaderFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return new StreamReader(stream);
    }

    internal static void RotatePath(string path, int backups)
    {
        for (var i = backups - 1; i >= 0; i--)
        {
            var pathCurrent = path + (i == 0 ? "" : "." + i);
            var pathNext = path + "." + (i + 1);
            if (!File.Exists(pathCurrent))
            {
                continue;
            }
            File.Delete(pathNext);
            File.Move(pathCurrent, pathNext);
        }
    }
}