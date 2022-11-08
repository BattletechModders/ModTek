using ModTek.Features.CustomResources;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.BTRL;
using ModTek.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ModTek.Features.CustomAssemblies
{
    internal static class CustomAssembliesLoader
    {
        internal static Dictionary<string, Assembly> registredAssemblies = new Dictionary<string, Assembly>();
        private static Assembly ResolveAssembly(System.Object sender, ResolveEventArgs evt)
        {
            Assembly res = null;
            try
            {
                MTLogger.Info.Log($"request resolve assembly:{evt.Name}");
                AssemblyName assemblyName = new AssemblyName(evt.Name);
                if (registredAssemblies.TryGetValue(assemblyName.Name, out var assembly))
                {
                    MTLogger.Info.Log($" loading registered assembly:{assembly.CodeBase}");
                    res = assembly;
                }
                else
                {
                    MTLogger.Info.Log(" assembly not registered");
                }
            }
            catch (Exception err)
            {
                MTLogger.Info.Log(err.ToString());
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

            for (int t = 0; t < entries.Length; ++t)
            {
                string name = string.Empty;
                try
                {
                    string assemblyPath = entries[t].FilePath;
                    name = AssemblyName.GetAssemblyName(assemblyPath).Name;
                    registredAssemblies[name] = Assembly.LoadFile(assemblyPath);
                    MTLogger.Info.Log($"register assembly {name}");
                }
                catch (System.BadImageFormatException)
                {
                    MTLogger.Info.Log(" not a .NET assembly. skip");
                }
                catch (Exception e)
                {
                    MTLogger.Error.Log(e.ToString());
                }
                yield return new ProgressReport((float)t / (float)entries.Length, sliderText, name);
            }
        }
    }
}