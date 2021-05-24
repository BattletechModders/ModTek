using System.Collections.Generic;
using ModTek.Logging;
using ModTek.UI;

namespace ModTek.Mods
{
    internal static class DependencyTree
    {
        internal static IEnumerator<ProgressReport> GatherDependencyTreeLoop()
        {
            yield return new ProgressReport(0, "Gathering dependencies trees", "");
            if (ModTek.allModDefs.Count == 0)
            {
                yield break;
            }

            var progress = 0;
            foreach (var mod in ModTek.allModDefs)
            {
                ++progress;
                foreach (var depname in mod.Value.DependsOn)
                {
                    if (ModTek.allModDefs.ContainsKey(depname))
                    {
                        if (ModTek.allModDefs[depname].DependsOnMe.Contains(mod.Value) == false)
                        {
                            ModTek.allModDefs[depname].DependsOnMe.Add(mod.Value);
                        }
                    }
                }
            }

            yield return new ProgressReport(1 / 3f, $"Gather depends on me", string.Empty, true);
            progress = 0;
            foreach (var mod in ModTek.allModDefs)
            {
                ++progress;
                mod.Value.GatherAffectingOfflineRec();
            }

            yield return new ProgressReport(2 / 3f, $"Gather disable influence tree", string.Empty, true);
            progress = 0;
            foreach (var mod in ModTek.allModDefs)
            {
                ++progress;
                mod.Value.GatherAffectingOnline();
            }

            yield return new ProgressReport(1, $"Gather enable influence tree", string.Empty, true);
            Logger.Log((string) $"FAIL LIST:");
            foreach (var mod in ModTek.allModDefs.Values)
            {
                if (mod.Enabled == false)
                {
                    continue;
                }

                ;
                if (mod.LoadFail == false)
                {
                    continue;
                }

                Logger.Log((string) $"\t{mod.Name} fail {mod.FailReason}");
                foreach (var dmod in mod.AffectingOnline)
                {
                    var state = dmod.Key.Enabled && dmod.Key.LoadFail == false;
                    if (state != dmod.Value)
                    {
                        Logger.Log((string) $"\t\tdepends on {dmod.Key.Name} should be loaded:{dmod.Value} but it is not cause enabled:{dmod.Key.Enabled} and fail:{dmod.Key.LoadFail} due to {dmod.Key.FailReason}");
                    }
                }

                foreach (var deps in mod.DependsOn)
                {
                    if (ModTek.allModDefs.ContainsKey(deps) == false)
                    {
                        Logger.Log((string) $"\t\tdepends on {deps} but abcent");
                    }
                }
            }
        }
    }
}
