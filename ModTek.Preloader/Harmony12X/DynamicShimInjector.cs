using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ModTek.Common.Globals;
using ModTek.Common.Utils;
using Mono.Cecil;

namespace ModTek.Preloader.Harmony12X;

/* from BepInEx 5
 * https://github.com/BepInEx/HarmonyInteropDlls/
 * https://github.com/BepInEx/BepInEx.Harmony
 */
internal class DynamicShimInjector
{
    private readonly ShimCacheManifest cache;

    internal static void Setup()
    {
        var shimInjector = new DynamicShimInjector();
        ShimInjectorPatches.Register(shimInjector);
    }

    private DynamicShimInjector()
    {
        Logger.Main.Log("Setting up HarmonyX interoperability");

        Logger.Main.Log($"Preloading supported Harmony12X assemblies from `{FileUtils.GetRelativePath(Paths.ModTekLibDirectory)}`.");
        foreach (var harmonyVersion in HarmonyVersion.SupportedVersions)
        {
            var file = Path.Combine(Paths.ModTekLibDirectory, $"{harmonyVersion.Name}.dll");
            Logger.Main.Log($"\t{Path.GetFileName(file)}");
            var assembly = Assembly.LoadFile(file);
            if (!harmonyVersion.IsMatch(assembly.GetName().Version))
            {
                throw new Exception($"Harmony shim version {assembly.GetName().Version} does not fall within {harmonyVersion}.");
            }
        }

        cache = new ShimCacheManifest(this);
    }

    internal bool DetectAndPatchHarmony(AssemblyDefinition assemblyDefinition)
    {
        // has harmony ref
        var harmonyReference = assemblyDefinition.MainModule.AssemblyReferences
            .SingleOrDefault(a => a.Name == "0Harmony");
        if (harmonyReference == null)
        {
            // Logger.Log($"Assembly {assemblyDefinition.Name.Name} has no harmony reference.");
            return false;
        }

        // find compatible shim
        var compatibleHarmonyAssembly = HarmonyVersion.SupportedVersions.FirstOrDefault(h => h.IsMatch(harmonyReference.Version));
        if (compatibleHarmonyAssembly == null)
        {
            Logger.Main.Log($"Assembly {assemblyDefinition.Name.Name} has no compatible shim to be relinked to for harmony {harmonyReference.Version}.");
            return false;
        }

        if (compatibleHarmonyAssembly.Name == "0Harmony")
        {
            // Logger.Log($"Assembly {assemblyDefinition.Name.Name} already uses latest harmony.");
            return false;
        }

        // replace ref
        Logger.Main.Log($"Assembly {assemblyDefinition.Name.Name} using 0Harmony@{harmonyReference.Version} is being relinked to {compatibleHarmonyAssembly.Name}.");
        harmonyReference.Name = compatibleHarmonyAssembly.Name;
        return true;
    }

    internal void InjectShimIfNecessary(ref string path)
    {
        try
        {
            path = cache.GetPathToShimmedAssembly(path);
        }
        catch (Exception e)
        {
            Logger.Main.Log("Error preparing assembly load for shim: " + e);
        }
    }

    internal void InjectShimIfNecessary(ref byte[] rawAssembly)
    {
        // TODO implement caching if needed
        // probably a checksum -> (no shimming necessary || path to shimmed assembly)
        using var stream = new MemoryStream(rawAssembly, false);
        using var definition = AssemblyDefinition.ReadAssembly(stream);
        var name = definition.Name.Name;
        if (name.StartsWith("DMDASM.") || name == "MonoMod.Utils.GetManagedSizeHelper")
        {
            return;
        }

        var begin = DateTime.Now;
        if (!DetectAndPatchHarmony(definition))
        {
            return;
        }

        using (var newStream = new MemoryStream(rawAssembly.Length * 2))
        {
            definition.Write(newStream);
            rawAssembly = newStream.ToArray();
        }

        var text = $"Loading shimmed assembly {name} from memory";
        text += $", shimming took {(DateTime.Now-begin).TotalSeconds:#0.000}s";
        text += ".";
        Logger.Main.Log(text);
    }
}