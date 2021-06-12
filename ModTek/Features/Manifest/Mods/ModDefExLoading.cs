using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Harmony;
using ModTek.Features.CustomResources;
using ModTek.Util;
using Newtonsoft.Json;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Mods
{
    internal static class ModDefExLoading
    {
        internal static bool LoadMod(ModDefEx modDef, out string reason)
        {
            Log($"{modDef.Name} {modDef.Version}");

            // although manifest related, CR listings are mostly relevant for the FinishedLoading call
            CustomResourcesFeature.ProcessModDef(modDef);

            // load the mod assembly
            if (modDef.DLL != null && !LoadAssemblyAndCallInit(modDef))
            {
                reason = "Fail to call init method";
                return false;
            }

            reason = "Success";
            return true;
        }

        private static bool LoadAssemblyAndCallInit(ModDefEx modDef)
        {
            var dllPath = Path.Combine(modDef.Directory, modDef.DLL);
            string typeName = null;
            var methodName = "Init";

            if (!File.Exists(dllPath))
            {
                Log($"\tError: DLL specified ({dllPath}), but it's missing! Aborting load.");
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
                Log($"\tError: Failed to load mod assembly at path {dllPath}.");
                return false;
            }

            var methods = AssemblyUtil.FindMethods(assembly, methodName, typeName);
            if (methods == null || methods.Length == 0)
            {
                Log($"\t\tError: Could not find any methods in assembly with name '{methodName}' and with type '{typeName ?? "not specified"}'");
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
                    Log($"\tError: While invoking '{method.DeclaringType?.Name}.{method.Name}', an exception occured", e);
                    return false;
                }

                Log($"\tError: Could not invoke method with name '{method.DeclaringType?.Name}.{method.Name}'");
                return false;
            }

            modDef.Assembly = assembly;

            if (!modDef.EnableAssemblyVersionCheck)
            {
                TryResolveAssemblies.Add(assembly.GetName().Name, assembly);
            }

            return true;
        }

        internal static void FinishedLoading(ModDefEx modDef, List<string> ModLoadOrder)
        {
            var methods = AssemblyUtil.FindMethods(modDef.Assembly, "FinishedLoading");

            if (methods == null || methods.Length == 0)
            {
                return;
            }

            var paramsDictionary = new Dictionary<string, object> { { "loadOrder", new List<string>(ModLoadOrder) } };

            if (modDef.CustomResourceTypes.Count > 0)
            {
                var customResources = CustomResourcesFeature.GetResourceDictionariesForTypes(modDef.CustomResourceTypes);
                paramsDictionary.Add("customResources", customResources);
            }

            foreach (var method in methods)
            {
                try
                {
                    if (!AssemblyUtil.InvokeMethodByParameterNames(method, paramsDictionary))
                    {
                        Log($"\tError: {modDef.Name}: Failed to invoke '{method.DeclaringType?.Name}.{method.Name}', parameter mismatch");
                    }
                }
                catch (Exception e)
                {
                    Log($"\tError: {modDef.Name}: Failed to invoke '{method.DeclaringType?.Name}.{method.Name}', exception", e);
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
