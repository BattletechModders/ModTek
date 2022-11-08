using System;
using System.Collections.Generic;
using System.Reflection;
using ModTek.Features.CustomResources;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.BTRL;
using ModTek.UI;

namespace ModTek.Features.AssembliesLoader
{
    internal static class CustomAssembliesLoader
    {
        private static readonly Dictionary<string, Assembly> registeredAssemblies = new Dictionary<string, Assembly>();
        private static Assembly ResolveAssembly(object sender, ResolveEventArgs evt)
        {
            Assembly res = null;
            try
            {
                MTLogger.Info.Log($"request resolve assembly:{evt.Name}");
                var assemblyName = new AssemblyName(evt.Name);
                if (registeredAssemblies.TryGetValue(assemblyName.Name, out var assembly))
                {
                    MTLogger.Info.Log($" loading registered assembly:{assembly.CodeBase}");
                    res = assembly;
                }
                else
                {
                    MTLogger.Info.Log(" assembly not registered");
                }
            }
            catch (Exception e)
            {
                MTLogger.Error.Log(e);
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

            MTLogger.Info.Log($"Processing assemblies");
            var sliderText = "Processing assemblies";
            yield return new ProgressReport(0, sliderText, "", true);

            for (var t = 0; t < entries.Length; ++t)
            {
                var name = string.Empty;
                try
                {
                    var assemblyPath = entries[t].FilePath;
                    name = AssemblyName.GetAssemblyName(assemblyPath).Name;
                    registeredAssemblies[name] = Assembly.LoadFile(assemblyPath);
                    MTLogger.Debug.Log($" Registered assembly {name}");
                }
                catch (BadImageFormatException e)
                {
                    MTLogger.Warning.Log(" Not a .NET assembly", e);
                }
                catch (Exception e)
                {
                    MTLogger.Error.Log(e);
                }
                yield return new ProgressReport((float)t / (float)entries.Length, sliderText, name);
            }
        }
    }
}