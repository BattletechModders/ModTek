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
    }
}
