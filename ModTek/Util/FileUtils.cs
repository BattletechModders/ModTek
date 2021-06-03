using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModTek.Util
{
    internal static class FileUtils
    {
        internal static string GetRelativePath(string basePath, string absolutePath)
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

            if (pathUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
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
            return IGNORE_LIST.Any(x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }

        internal static List<string> FindFiles(string basePath, params string[] suffixes)
        {
            return Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
                .Where(path => !FileIsOnDenyList(path))
                .Where(path => suffixes == null || suffixes.Any(p => path.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
        }

        internal const string JSON_TYPE = ".json";
        internal const string CSV_TYPE = ".csv";
        internal const string TXT_TYPE = ".txt";

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
    }
}
