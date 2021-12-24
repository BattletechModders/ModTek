using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using ModTek.Features.CustomSVGAssets.Patches;
using ModTek.Misc;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Util
{
    internal static class HarmonyUtils
    {
        private static HarmonyInstance CreateInstance() => HarmonyInstance.Create("io.github.mpstark.ModTek");

        internal static void PatchAll()
        {
            Log("Applying ModTek harmony patches");

            var harmony = CreateInstance();

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                try
                {
                    var harmonyMethods = type.GetHarmonyMethods();
                    if (harmonyMethods == null || !harmonyMethods.Any())
                    {
                        continue;
                    }
                    var attributes = HarmonyMethod.Merge(harmonyMethods);
                    new PatchProcessor(harmony, type, attributes).Patch();
                }
                catch
                {
                    Log($"ERROR: Applying patch {type} failed");
                    throw;
                }
            }

            try
            {
                SVGAssetLoadRequest_Load.Patch(harmony);
            }
            catch
            {
                Log($"ERROR: Applying patch {nameof(SVGAssetLoadRequest_Load)} failed");
                throw;
            }
        }

        internal static void PrintHarmonySummary()
        {
            try
            {
                Profiler_Patching.Patch();
            }
            catch (Exception)
            {
                Log($"Applying patch {nameof(Profiler_Patching)} failed");
                throw;
            }

            var harmony = CreateInstance();

            var path = FilePaths.HarmonySummaryPath;
            Log($"Writing Harmony Summary to {path}");
            using (var writer = File.CreateText(path))
            {
                writer.WriteLine($"Harmony Patched Methods (after ModTek startup) -- {DateTime.Now}\n");

                foreach (var method in harmony.GetPatchedMethods())
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
