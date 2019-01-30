using System;
using System.IO;
using System.Reflection;

// ReSharper disable UnusedMember.Global

namespace ModTek
{
    public static class Injection
    {
        public static void LoadModTek()
        {
            try
            {
                Assembly.LoadFrom(
                        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "",
                            "../../Mods/ModTek.dll"))
                    .GetType("ModTek.ModTek")
                    .GetMethod("Init", BindingFlags.Public | BindingFlags.Static)
                    ?.Invoke(null, null);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
