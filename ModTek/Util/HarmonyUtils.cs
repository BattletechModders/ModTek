using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using ModTek.Features.CustomSVGAssets.Patches;
using ModTek.Features.Profiler;
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
                ProfilerPatcher.Patch();
            }
            catch (Exception)
            {
                Log($"Applying patch {nameof(ProfilerPatcher)} failed");
                throw;
            }

            var harmony = CreateInstance();

            var path = FilePaths.HarmonySummaryPath;
            Log($"Writing Harmony Summary to {path}");
            using (var writer = File.CreateText(path))
            {
                writer.WriteLine($"Harmony Patched Methods (after ModTek startup) -- {DateTime.Now}");
                writer.WriteLine();
                writer.WriteLine("Format as follows:");
                writer.WriteLine("{assembly name of patched method}");
                writer.WriteLine("{name of patched method}");
                writer.WriteLine("\t{Harmony patch type}");
                writer.WriteLine("\t\t{assembly name of patch} ({Harmony id of patch}) {method name of patch}");
                writer.WriteLine();
                writer.WriteLine();

                var methodsIter = harmony
                    .GetPatchedMethods()
                    .Where(m => m.DeclaringType != null)
                    // order based on output
                    .OrderBy(m => m.DeclaringType.Assembly.GetName().Name)
                    .ThenBy(m => m.DeclaringType.FullName)
                    .ThenBy(m => m.Name);

                foreach (var method in methodsIter)
                {
                    var info = harmony.GetPatchInfo(method);

                    if (info == null)
                    {
                        continue;
                    }

                    writer.WriteLine(AssemblyUtil.GetAssemblyNameFromMemberInfo(method));
                    writer.WriteLine(AssemblyUtil.GetMethodFullName(method) + ":");

                    void WritePatches(string title, IList<Patch> patches)
                    {
                        if (patches.Count != 0)
                        {
                            writer.WriteLine($"\t{title}:");
                        }
                        foreach (var patch in patches.OrderBy(x => x))
                        {
                            var assemblyName = AssemblyUtil.GetAssemblyNameFromMemberInfo(patch.patch);
                            var methodName = AssemblyUtil.GetMethodFullName(patch.patch);
                            writer.WriteLine($"\t\t{assemblyName} ({patch.owner}) {methodName}");
                        }
                    }

                    WritePatches("Prefixes", info.Prefixes);
                    WritePatches("Transpilers", info.Transpilers);
                    WritePatches("Postfixes", info.Postfixes);

                    writer.WriteLine();
                }
            }
        }
    }
}
