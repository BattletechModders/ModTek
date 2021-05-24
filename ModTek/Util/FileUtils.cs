using System;
using System.IO;
using System.Linq;
using ModTek.RuntimeLog;

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

        internal static string ResolvePath(string path, string rootPathToUse)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(rootPathToUse, path);
            }

            return Path.GetFullPath(path);
        }

        internal static string GetRelativePath(string path, string rootPath)
        {
            if (!Path.IsPathRooted(path))
            {
                return path;
            }

            rootPath = Path.GetFullPath(rootPath);
            if (rootPath.Last() != Path.DirectorySeparatorChar)
            {
                rootPath += Path.DirectorySeparatorChar;
            }

            var pathUri = new Uri(Path.GetFullPath(path), UriKind.Absolute);
            var rootUri = new Uri(rootPath, UriKind.Absolute);

            if (pathUri.Scheme != rootUri.Scheme)
            {
                return path;
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

        public static bool FileIsOnDenyList(string filePath)
        {
            return IGNORE_LIST.Any(x => filePath.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
