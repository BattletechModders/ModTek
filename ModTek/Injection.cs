using System.IO;
using System.Reflection;

// ReSharper disable UnusedMember.Global

namespace ModTek
{
    internal static class Injection
    {
        public static void LoadModTek()
        {
            try
            {
                var path = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory(),
                    Path.Combine(Path.Combine("..", ".."), Path.Combine("Mods", Path.Combine("ModTek", "ModTek.dll")))
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
