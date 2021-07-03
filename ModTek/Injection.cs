using System.IO;
using System.Reflection;
using ModTek.Misc;

// ReSharper disable UnusedMember.Global

namespace ModTek
{
    public static class Injection
    {
        public static void LoadModTek()
        {
            try
            {
                var path = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(),
                    Path.Combine(Path.Combine("..", ".."), Path.Combine(FilePaths.MODS_DIRECTORY_NAME, Path.Combine(FilePaths.MODTEK_DIRECTORY_NAME, "ModTek.dll")))
                );

                if (File.Exists(path))
                {
                    Assembly.LoadFrom(path)
                        .GetType("ModTek.ModTek")
                        .GetMethod("Init", BindingFlags.Public | BindingFlags.Static)
                        ?.Invoke(null, null);
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}
