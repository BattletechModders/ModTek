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

        var IsHarmony12XShim = typeof(HarmonyInstance).Assembly.GetName().Name == "0Harmony12";

        foreach (var method in methodsIter)
        {
            var info = harmony.GetPatchInfo(method);

            if (info == null)
            {
                continue;
            }

            writer.WriteLine(method.GetAssemblyName());
            writer.WriteLine(method.GetFullName() + ":");

            void WritePatches(string title, IList<Patch> patches, bool checkSkip = false)
            {
                if (patches.Count != 0)
                {
                    writer.WriteLine($"\t{title}:");
                }

                var hasSkipper = !checkSkip;
                foreach (var patch in patches.OrderBy(x => x))
                {
                    var assemblyName = patch.patch.GetAssemblyName();
                    var methodName = patch.patch.GetFullName();
                    var noSkip = IsHarmony12XShim && checkSkip && hasSkipper && patch.patch.GetParameters().Any(p => p.Name == "__state");
                    if (noSkip)
                    {
                        Log.Main.Warning?.Log($"Prefix {methodName} in {assemblyName} has __state and is behind a bool Prefix. In HarmonyX it won't be skipped as it would in Harmony 1.");
                    }
                    var noSkipPrefix = noSkip ? "NOSKIP: " : "";
                    writer.WriteLine($"\t\t{noSkipPrefix}{assemblyName} ({patch.owner}) {methodName}");
                    hasSkipper = hasSkipper || patch.patch.ReturnType == typeof(bool);
                }
            }

            WritePatches("Prefixes", info.Prefixes, true);
            WritePatches("Transpilers", info.Transpilers);
            WritePatches("Postfixes", info.Postfixes);

            writer.WriteLine();
        }
    }
}