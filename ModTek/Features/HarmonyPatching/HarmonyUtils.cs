using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using ModTek.Features.CustomSVGAssets.Patches;
using ModTek.Misc;
using ModTek.Util;

namespace ModTek.Features.HarmonyPatching;

internal static class HarmonyUtils
{
    private static HarmonyInstance CreateInstance() => HarmonyInstance.Create("io.github.mpstark.ModTek");

    internal static void PatchAll()
    {
        Log.Main.Info?.Log("Applying ModTek harmony patches");

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
                Log.Main.Error?.Log($"Applying patch {type} failed");
                throw;
            }
        }

        try
        {
            SVGAssetLoadRequest_Load.Patch(harmony);
        }
        catch
        {
            Log.Main.Error?.Log($"Applying patch {nameof(SVGAssetLoadRequest_Load)} failed");
            throw;
        }
    }

    internal static void PrintHarmonySummary()
    {
        var harmony = CreateInstance();

        var path = FilePaths.HarmonySummaryPath;
        Log.Main.Info?.Log($"Writing Harmony Summary to {path}");

        using var writer = File.CreateText(path);
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

            writer.WriteLine(method.GetAssemblyName());
            writer.WriteLine(method.GetFullName() + ":");

            void WritePatches(string title, IList<Patch> patches)
            {
                if (patches.Count != 0)
                {
                    writer.WriteLine($"\t{title}:");
                }
                foreach (var patch in patches.OrderBy(x => x))
                {
                    var assemblyName = patch.patch.GetAssemblyName();
                    var methodName = patch.patch.GetFullName();
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