using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModTek.Logging;

namespace ModTek.Util
{
    internal static class FileUtils
    {
        internal static void CleanModTekTempDir(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
            {
                return;
            }

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                CleanModTekTempDir(dir);
            }

            var files = baseDir.GetFiles();
            foreach (var file in files)
            {
                if (file.Name == "ModTek.log")
                {
                    continue;
                }

                if (file.Name == "ModTek_runtime_log.txt")
                {
                    continue;
                }

                file.IsReadOnly = false;
                RLog.M.TWL(0, "delete file " + file.FullName);
                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                }
            }

            RLog.M.TWL(0, "delete directory " + baseDir.FullName);
            try
            {
                baseDir.Delete();
            }
            catch (Exception)
            {
            }
        }

        internal static string ResolvePath(string basePath, string relativePath)
        {
            if (!Path.IsPathRooted(relativePath))
            {
                relativePath = Path.Combine(basePath, relativePath);
            }

            return Path.GetFullPath(relativePath);
        }

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

        internal static List<string> FindFiles(string path, string pattern)
        {
            return Directory.GetFiles(path, pattern, SearchOption.AllDirectories).Where(filePath => !FileUtils.FileIsOnDenyList(filePath)).ToList();
        }
    }
}
