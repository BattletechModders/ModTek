using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ModTek.Util;

internal static class LoadOrder
{
    // sort based on most shallow and then mod path
    // makes the duplicate detection behavior deterministic
    // makes the load order in general deterministic
    //
    // Paths:
    // Mods/ModB (but mod.json says ModA)
    // Mods/MyMods/ModA
    // Mods/ModA
    // =>
    // Mods/ModA
    // Mods/ModB (will be later filtered as duplicate)
    // Mods/MyMods/ModA (will be later filtered as duplicate)
    internal static void SortPaths(string[] modJsonPaths)
    {
        Array.Sort(modJsonPaths, (a, b) =>
        {
            var aCount = a.Count(c => c == Path.DirectorySeparatorChar);
            var bCount = b.Count(c => c == Path.DirectorySeparatorChar);
            var cmp = aCount - bCount;
            if (cmp != 0)
            {
                return cmp;
            }
            return string.Compare(a, b, StringComparison.Ordinal);
        });
    }

    public static List<string> CreateLoadOrder(Dictionary<string, ModDefEx> registeredMods, out List<ModDefEx> notLoaded)
    {
        var candidates = new Dictionary<string, ModDefEx>(registeredMods);
        var loadOrder = new List<string>();

        // remove all mods that have a conflict
        var tryToLoad = candidates.Keys.ToList();
        var hasConflicts = new List<ModDefEx>();
        foreach (var modDef in registeredMods.Values)
        {
            var conflicts = modDef.CalcConflicts(tryToLoad);
            if (conflicts.Count == 0)
            {
                continue;
            }
            candidates.Remove(modDef.Name);
            hasConflicts.Add(modDef);
        }

        FillInOptionalDependencies(candidates);

        // everything that is left in the candidates list hasn't been loaded before
        notLoaded = new List<ModDefEx>();
        notLoaded.AddRange(candidates.Values.OrderByDescending(x => x.Name).ToList());

        // there is nothing left to load
        if (candidates.Count == 0)
        {
            notLoaded.AddRange(hasConflicts);
            return loadOrder;
        }

        // this is the remainder that haven't been loaded before
        int removedThisPass;
        do
        {
            removedThisPass = 0;

            for (var i = notLoaded.Count - 1; i >= 0; i--)
            {
                var modDef = notLoaded[i];

                if (modDef.CalcMissingDependsOn(loadOrder).Count > 0)
                {
                    continue;
                }

                notLoaded.RemoveAt(i);
                loadOrder.Add(modDef.Name);
                removedThisPass++;
            }
        }
        while (removedThisPass > 0 && notLoaded.Count > 0);

        notLoaded.AddRange(hasConflicts);
        return loadOrder;
    }

    public static void ToFile(List<string> order, string path)
    {
        if (order == null)
        {
            return;
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(order, Formatting.Indented));
    }

    private static void FillInOptionalDependencies(Dictionary<string, ModDefEx> modDefs)
    {
        // add optional dependencies if they are present
        foreach (var modDef in modDefs.Values)
        {
            if (modDef.OptionallyDependsOn.Count == 0)
            {
                continue;
            }

            foreach (var optDep in modDef.OptionallyDependsOn)
            {
                if (modDefs.ContainsKey(optDep))
                {
                    modDef.DependsOn.Add(optDep);
                }
            }
        }
    }
}