using System;
using System.IO;
using System.Linq;
using Harmony;

namespace ModTek.Util
{
    internal static class HarmonyUtils
    {
        internal static void PrintHarmonySummary(string path)
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.ModTek");

            var patchedMethods = harmony.GetPatchedMethods().ToArray();
            if (patchedMethods.Length == 0)
            {
                return;
            }

            using (var writer = File.CreateText(path))
            {
                writer.WriteLine($"Harmony Patched Methods (after ModTek startup) -- {DateTime.Now}\n");

                foreach (var method in patchedMethods)
                {
                    var info = harmony.GetPatchInfo(method);

                    if (info == null || method.ReflectedType == null)
                    {
                        continue;
                    }

                    writer.WriteLine($"{method.ReflectedType.FullName}.{method.Name}:");

                    // prefixes
                    if (info.Prefixes.Count != 0)
                    {
                        writer.WriteLine("\tPrefixes:");
                    }

                    foreach (var patch in info.Prefixes)
                    {
                        writer.WriteLine($"\t\t{patch.owner}");
                    }

                    // transpilers
                    if (info.Transpilers.Count != 0)
                    {
                        writer.WriteLine("\tTranspilers:");
                    }

                    foreach (var patch in info.Transpilers)
                    {
                        writer.WriteLine($"\t\t{patch.owner}");
                    }

                    // postfixes
                    if (info.Postfixes.Count != 0)
                    {
                        writer.WriteLine("\tPostfixes:");
                    }

                    foreach (var patch in info.Postfixes)
                    {
                        writer.WriteLine($"\t\t{patch.owner}");
                    }

                    writer.WriteLine("");
                }
            }
        }
    }
}
