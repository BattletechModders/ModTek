using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BattleTech;
using Harmony;
using ModTek.Logging;
using ModTek.Manifest;
using ModTek.Misc;
using ModTek.SoundBanks;
using ModTek.UI;
using ModTek.Util;
using Newtonsoft.Json;

namespace ModTek.Mods
{
    internal static class ModDefExLoading
    {
        internal const string CustomType_AdvancedJSONMerge = "AdvancedJSONMerge";
        internal const string CustomType_DebugSettings = "DebugSettings";
        internal const string CustomType_FixedSVGAsset = "FixedSVGAsset";
        internal const string CustomType_GameTip = "GameTip";
        internal const string CustomType_SoundBankDef = "SoundBankDef";
        internal const string CustomType_SoundBank = "SoundBank";
        internal const string CustomType_Tag = "CustomTag";
        internal const string CustomType_TagSet = "CustomTagSet";
        internal const string CustomType_Video = "Video";

        private static readonly string[] MODTEK_TYPES =
        {
            CustomType_Video,
            CustomType_AdvancedJSONMerge,
            CustomType_GameTip,
            CustomType_SoundBank,
            CustomType_SoundBankDef,
            CustomType_DebugSettings,
            CustomType_FixedSVGAsset,
            CustomType_Tag,
            CustomType_TagSet
        };

        private static readonly string[] VANILLA_TYPES = Enum.GetNames(typeof(BattleTechResourceType));

        internal static bool LoadMod(ModDefEx modDef, out string reason)
        {
            Logger.Log((string) $"{modDef.Name} {modDef.Version}");

            // read in custom resource types
            foreach (var customResourceType in modDef.CustomResourceTypes)
            {
                if (VANILLA_TYPES.Contains(customResourceType) || MODTEK_TYPES.Contains(customResourceType))
                {
                    Logger.Log((string) $"\tWarning: {modDef.Name} has a custom resource type that has the same name as a vanilla/modtek resource type. Ignoring this type.");
                    continue;
                }

                if (!ModsManifest.CustomResources.ContainsKey(customResourceType))
                {
                    ModsManifest.CustomResources.Add(customResourceType, new Dictionary<string, VersionManifestEntry>());
                }
            }

            // expand the manifest (parses all JSON as well)
            var expandedManifest = ExpandManifest(modDef);
            if (expandedManifest == null)
            {
                reason = "Can't expand manifest";
                return false;
            }

            // load the mod assembly
            if (modDef.DLL != null && !LoadAssemblyAndCallInit(modDef))
            {
                reason = "Fail to call init method";
                return false;
            }

            // replace the manifest with our expanded manifest since we successfully got through loading the other stuff
            if (expandedManifest.Count > 0)
            {
                Logger.Log((string) $"\t{expandedManifest.Count} manifest entries");
            }

            modDef.Manifest = expandedManifest;
            reason = "Success";
            return true;
        }

        private static List<ModEntry> ExpandManifest(ModDefEx modDef)
        {
            // note: if a JSON has errors, this mod will not load, since InferIDFromFile will throw from parsing the JSON
            var expandedManifest = new List<ModEntry>();

            if (modDef.LoadImplicitManifest && modDef.Manifest.All(x => Path.GetFullPath(Path.Combine(modDef.Directory, x.Path)) != Path.GetFullPath(Path.Combine(modDef.Directory, "StreamingAssets"))))
            {
                modDef.Manifest.Add(new ModEntry("StreamingAssets", true));
            }

            Logger.Log((string) $"Expanding manifest {modDef.Name}:");
            foreach (var modEntry in modDef.Manifest)
            {
                // handle prefabs; they have potential internal path to assetbundle
                Logger.Log((string) $"\t{modEntry.Type}:{modEntry.Path}");
                if (modEntry.Type == "Prefab" && !string.IsNullOrEmpty(modEntry.AssetBundleName))
                {
                    if (!expandedManifest.Any(x => x.Type == "AssetBundle" && x.Id == modEntry.AssetBundleName))
                    {
                        Logger.Log((string) $"\tError: {modDef.Name} has a Prefab '{modEntry.Id}' that's referencing an AssetBundle '{modEntry.AssetBundleName}' that hasn't been loaded. Put the assetbundle first in the manifest!");
                        return null;
                    }

                    // TODO wtf, if type != Prefab then why have it as prevcondition of wrapper if == Prefab
                    //if (string.IsNullOrEmpty(modEntry.Id))
                    //{
                    if (modEntry.Type != "Prefab")
                    {
                        modEntry.Id = Path.GetFileNameWithoutExtension(modEntry.Path);
                    }
                    else if (string.IsNullOrEmpty(modEntry.Id))
                    {
                        modEntry.Id = Path.GetFileNameWithoutExtension(modEntry.Path);
                    }
                    //}

                    if (!FileUtils.FileIsOnDenyList(modEntry.Path))
                    {
                        expandedManifest.Add(modEntry);
                        Logger.Log((string) $"\t\t{modEntry.Path}");
                    }

                    continue;
                }

                if (string.IsNullOrEmpty(modEntry.Path) && string.IsNullOrEmpty(modEntry.Type) && modEntry.Path != "StreamingAssets")
                {
                    Logger.Log((string) $"\tError: {modDef.Name} has a manifest entry that is missing its path or type! Aborting load.");
                    return null;
                }

                if (!string.IsNullOrEmpty(modEntry.Type) && !VANILLA_TYPES.Contains(modEntry.Type) && !MODTEK_TYPES.Contains(modEntry.Type) && !ModsManifest.CustomResources.ContainsKey(modEntry.Type))
                {
                    Logger.Log((string) $"\tError: {modDef.Name} has a manifest entry that has a type '{modEntry.Type}' that doesn't match an existing type and isn't declared in CustomResourceTypes");
                    return null;
                }

                // TODO ok i assume its ok to think files exists on filesystem
                var entryPath = Path.GetFullPath(Path.Combine(modDef.Directory, modEntry.Path));
                if (Directory.Exists(entryPath))
                {
                    // path is a directory, add all the files there
                    var files = new List<string>();
                    switch (modEntry.Type)
                    {
                        // TODO too much code clone
                        case nameof(SoundBankDef):
                            files = Directory.GetFiles(entryPath, "*.json", SearchOption.AllDirectories).Where(filePath => !FileUtils.FileIsOnDenyList(filePath)).ToList();
                            break;
                        default:
                            files = Directory.GetFiles(entryPath, "*", SearchOption.AllDirectories).Where(filePath => !FileUtils.FileIsOnDenyList(filePath)).ToList();
                            break;
                    }

                    foreach (var filePath in files)
                    {
                        var path = Path.GetFullPath(filePath);
                        try
                        {
                            var childModEntry = new ModEntry(modEntry, path, InferIDFromFile(filePath));
                            expandedManifest.Add(childModEntry);
                            Logger.Log((string) $"\t\t{childModEntry.Path}");
                        }
                        catch (Exception e)
                        {
                            Logger.LogException((string) $"\tError: Canceling {modDef.Name} load!\n\tCaught exception reading file at {FileUtils.GetRelativePath(FilePaths.GameDirectory, path)}", e);
                            return null;
                        }
                    }
                }
                else if (File.Exists(entryPath) && !FileUtils.FileIsOnDenyList(entryPath))
                {
                    // path is a file, add the single entry
                    try
                    {
                        modEntry.Id = modEntry.Id ?? InferIDFromFile(entryPath);
                        modEntry.Path = entryPath;
                        expandedManifest.Add(modEntry);
                        Logger.Log((string) $"\t\t{modEntry.Path}");
                    }
                    catch (Exception e)
                    {
                        Logger.LogException((string) $"\tError: Canceling {modDef.Name} load!\n\tCaught exception reading file at {FileUtils.GetRelativePath(FilePaths.GameDirectory, entryPath)}", e);
                        return null;
                    }
                }
                else if (modEntry.Path != "StreamingAssets")
                {
                    // path is not StreamingAssets and it's missing
                    Logger.Log((string) $"\tWarning: Manifest specifies file/directory of {modEntry.Type} at path {modEntry.Path}, but it's not there. Continuing to load.");
                }
            }

            return expandedManifest;
        }

        private static string InferIDFromFile(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        private static bool LoadAssemblyAndCallInit(ModDefEx modDef)
        {
            var dllPath = Path.Combine(modDef.Directory, modDef.DLL);
            string typeName = null;
            var methodName = "Init";

            if (!File.Exists(dllPath))
            {
                Logger.Log((string) $"\tError: DLL specified ({dllPath}), but it's missing! Aborting load.");
                return false;
            }

            if (modDef.DLLEntryPoint != null)
            {
                var pos = modDef.DLLEntryPoint.LastIndexOf('.');
                if (pos == -1)
                {
                    methodName = modDef.DLLEntryPoint;
                }
                else
                {
                    typeName = modDef.DLLEntryPoint.Substring(0, pos);
                    methodName = modDef.DLLEntryPoint.Substring(pos + 1);
                }
            }

            var assembly = AssemblyUtil.LoadDLL(dllPath);
            if (assembly == null)
            {
                Logger.Log((string) $"\tError: Failed to load mod assembly at path {dllPath}.");
                return false;
            }

            var methods = AssemblyUtil.FindMethods(assembly, methodName, typeName);
            if (methods == null || methods.Length == 0)
            {
                Logger.Log((string) $"\t\tError: Could not find any methods in assembly with name '{methodName}' and with type '{typeName ?? "not specified"}'");
                return false;
            }

            foreach (var method in methods)
            {
                var directory = modDef.Directory;
                var settings = modDef.Settings.ToString(Formatting.None);

                var parameterDictionary = new Dictionary<string, object>
                {
                    { "modDir", directory },
                    { "modDirectory", directory },
                    { "directory", directory },
                    { "modSettings", settings },
                    { "settings", settings },
                    { "settingsJson", settings },
                    { "settingsJSON", settings },
                    { "JSON", settings },
                    { "json", settings }
                };

                try
                {
                    if (AssemblyUtil.InvokeMethodByParameterNames(method, parameterDictionary))
                    {
                        continue;
                    }

                    if (AssemblyUtil.InvokeMethodByParameterTypes(
                        method,
                        new object[]
                        {
                            directory,
                            settings
                        }
                    ))
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Logger.LogException((string) $"\tError: While invoking '{method.DeclaringType?.Name}.{method.Name}', an exception occured", e);
                    return false;
                }

                Logger.Log((string) $"\tError: Could not invoke method with name '{method.DeclaringType?.Name}.{method.Name}'");
                return false;
            }

            modDef.Assembly = assembly;

            if (!modDef.EnableAssemblyVersionCheck)
            {
                TryResolveAssemblies.Add(assembly.GetName().Name, assembly);
            }

            return true;
        }

        internal static void FinishedLoading(ModDefEx modDef, List<string> ModLoadOrder, Dictionary<string, Dictionary<string, VersionManifestEntry>> CustomResources)
        {
            var methods = AssemblyUtil.FindMethods(modDef.Assembly, "FinishedLoading");

            if (methods == null || methods.Length == 0)
            {
                return;
            }

            var paramsDictionary = new Dictionary<string, object> { { "loadOrder", new List<string>(ModLoadOrder) } };

            if (modDef.CustomResourceTypes.Count > 0)
            {
                var customResources = new Dictionary<string, Dictionary<string, VersionManifestEntry>>();
                foreach (var resourceType in modDef.CustomResourceTypes)
                {
                    customResources.Add(resourceType, new Dictionary<string, VersionManifestEntry>(CustomResources[resourceType]));
                }

                paramsDictionary.Add("customResources", customResources);
            }

            foreach (var method in methods)
            {
                if (!AssemblyUtil.InvokeMethodByParameterNames(method, paramsDictionary))
                {
                    Logger.Log($"\tError: {modDef.Name}: Failed to invoke '{method.DeclaringType?.Name}.{method.Name}', parameter mismatch");
                }
            }
        }

        private static Dictionary<string, Assembly> TryResolveAssemblies = new();

        internal static void Setup()
        {
            // setup assembly resolver
            TryResolveAssemblies.Add("0Harmony", Assembly.GetAssembly(typeof(HarmonyInstance)));
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var resolvingName = new AssemblyName(args.Name);
                return !TryResolveAssemblies.TryGetValue(resolvingName.Name, out var assembly) ? null : assembly;
            };
        }
    }
}
