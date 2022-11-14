using System.Collections.Generic;

namespace ModTek.Features.Manifest.Mods
{
    internal static class ModDefExExtensions
    {
        public static void GatherAffectingOfflineRec(this ModDefEx mod)
        {
            var deps = new Dictionary<ModDefEx, bool>();
            Log.Main.Debug?.Log("Gathering " + mod.QuotedName + "->Disable influence. My state:" + mod.Enabled + " fail:" + (mod.LoadFail ? mod.FailReason : "no"));
            GatherAffectingOfflineRec(mod, ref deps, 1);
            mod.AffectingOffline = deps;
        }

        private static void GatherAffectingOfflineRec(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps, int level)
        {
            foreach (var dmod in mod.DependsOnMe)
            {
                if (deps.ContainsKey(dmod) == false)
                {
                    var i = new string(' ', level);
                    Log.Main.Debug?.Log(i + dmod.QuotedName + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                    deps.Add(dmod, false);
                    GatherAffectingOfflineRec(dmod, ref deps, level + 1);
                }
            }
        }

        private static void GatherAffectingOnlineRec(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps, int level)
        {
            foreach (var dep in mod.DependsOn)
            {
                var i = new string(' ', level);
                if (ModDefsDatabase.allModDefs.ContainsKey(dep) == false)
                {
                    Log.Main.Debug?.Log(i + dep + " state:Absent!");
                    continue;
                }

                var dmod = ModDefsDatabase.allModDefs[dep];
                if (deps.ContainsKey(dmod) == false)
                {
                    Log.Main.Debug?.Log(i + dmod.QuotedName + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                    deps.Add(dmod, true);
                    GatherAffectingOnlineRec(dmod, ref deps, level + 1);
                }
            }
        }

        private static void GatherConflicts(this ModDefEx mod, ref Dictionary<ModDefEx, bool> deps)
        {
            foreach (var dep in mod.ConflictsWith)
            {
                if (ModDefsDatabase.allModDefs.ContainsKey(dep) == false)
                {
                    Log.Main.Debug?.Log("  due to " + mod.QuotedName + " with " + dep + " state:Abcent");
                    continue;
                }

                var dmod = ModDefsDatabase.allModDefs[dep];
                Log.Main.Debug?.Log("  due to " + mod.QuotedName + " with " + dmod.QuotedName + " state:" + dmod.Enabled + " fail:" + (dmod.LoadFail ? dmod.FailReason : "no"));
                if (deps.ContainsKey(dmod) == false)
                {
                    deps.Add(dmod, false);
                }
            }
        }

        public static void GatherAffectingOnline(this ModDefEx mod)
        {
            var deps = new Dictionary<ModDefEx, bool>();
            Log.Main.Debug?.Log("Gathering " + mod.QuotedName + "->Enable influence. My state:" + mod.Enabled + " fail:" + (mod.LoadFail ? mod.FailReason : "no"));
            Log.Main.Debug?.Log(" I'm depends on:");
            GatherAffectingOnlineRec(mod, ref deps, 2);
            var conflicts = deps.Keys.ToHashSet();
            Log.Main.Debug?.Log(" Conflicts:");
            foreach (var cmod in conflicts)
            {
                GatherConflicts(cmod, ref deps);
            }

            mod.AffectingOnline = deps;
        }
    }
}
