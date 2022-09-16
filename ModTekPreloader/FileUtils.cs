using System;
using System.IO;

namespace ModTekPreloader
{
    internal static class FileUtils
    {
        internal static string GetRelativePath(string path)
        {
            try
            {
                return new Uri(Directory.GetCurrentDirectory()).MakeRelativeUri(new Uri(path)).ToString();
            }
            catch
            {
                return path;
            }
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
}
