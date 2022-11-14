using System;
using System.Collections.Generic;
using System.Reflection;
using ModTek.Features.CustomResources;
using ModTek.Features.Manifest.BTRL;
using ModTek.UI;
using ModTek.Util;

namespace ModTek.Features.AssembliesLoader
{
    internal static class CustomAssembliesLoader
    {
        private static readonly Dictionary<string, string> registeredAssemblyPaths = new Dictionary<string, string>();
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs evt)
        {
            Assembly res = null;
            try
            {
                var assemblyName = new AssemblyName(evt.Name);
                Log.Main.Debug?.Log($"Request to resolve custom assembly {assemblyName}");
                if (registeredAssemblyPaths.TryGetValue(assemblyName.Name, out var assemblyPath))
                {
                    var assembly = Assembly.LoadFile(assemblyPath);
                    Log.Main.Info?.Log($"Loaded custom assembly {AssemblyUtil.GetLocationOrName(assembly)}");
                    res = assembly;
                }
                else
                {
                    Log.Main.Debug?.Log($"Assembly {assemblyName} not known to {nameof(CustomAssembliesLoader)}.");
                }
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e);
            }
            return res;
        }

        internal static IEnumerator<ProgressReport> AssembliesProcessing()
        {
            var entries = BetterBTRL.Instance.AllEntriesOfType(InternalCustomResourceType.Assembly.ToString());
            if (entries.Length == 0)
            {
                yield break;
            }
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            var sliderText = "Processing assemblies";
            Log.Main.Info?.Log(sliderText);
            yield return new ProgressReport(0, sliderText, "", true);

            for (var t = 0; t < entries.Length; ++t)
            {
                var name = string.Empty;
                try
                {
                    var assemblyPath = entries[t].FilePath;
                    name = AssemblyName.GetAssemblyName(assemblyPath).Name;
                    registeredAssemblyPaths[name] = assemblyPath;
                    Log.Main.Info?.Log($"Registered custom assembly {name} at location {assemblyPath}");
                }
                catch (BadImageFormatException e)
                {
                    Log.Main.Warning?.Log(" Not a .NET assembly", e);
                }
                catch (Exception e)
                {
                    Log.Main.Error?.Log(e);
                }
                yield return new ProgressReport((float)t / (float)entries.Length, sliderText, name);
            }
        }
    }
}