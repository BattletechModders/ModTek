using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BattleTech.ModSupport;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using ModTek.Features.Manifest.Mods;
using UnityEngine;

namespace ModTek.Features.ModLoaderSupport.Patches
{
    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch(nameof(ModLoader.Init))]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModLoader_Init
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(Action callback)
        {
            callback();
            return false;
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("AreModsEnabled")]
    [HarmonyPatch(MethodType.Getter)]
    internal static class ModLoader_AreModsEnabled
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("LoadSystemModStatus")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModLoader_LoadSystemMods
    {
        public static bool Prepare()
        {
            return !ModTek.Enabled;
        }

        public static bool Prefix()
        {
            return true;
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("LoadSystemModStatus")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModLoader_LoadSystemModStatus
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix()
        {
            ModLoader.loadedSystemModStatus = new Dictionary<string, ModStatusItem>();
            ModLoader.loadedSystemModStatus.Add(ModTek.MODTEK_DEF_NAME, ModTek.SettingsDef.ToVanilla());
            return false;
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("LoadGameModStatus")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModLoader_LoadGameModStatus
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix()
        {
            ModLoader.loadedGameModStatus = new Dictionary<string, ModStatusItem>();
            foreach (var mod in ModDefsDatabase.allModDefs)
            {
                if (mod.Key == ModTek.MODTEK_DEF_NAME)
                {
                    continue;
                }

                if (mod.Value.Hidden)
                {
                    continue;
                }

                ModLoader.loadedGameModStatus.Add(mod.Key, mod.Value.ToVanilla());
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerScreen))]
    [HarmonyPatch("Init")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerScreen_InitModResources
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ModManagerScreen __instance)
        {
            PlayerPrefs.SetInt("ModsEnabled", 1);
            return true;
        }
    }

    [HarmonyPatch(typeof(ModManagerScreen))]
    [HarmonyPatch("Init")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerScreen_InitModResourcesDisabled
    {
        public static bool Prepare()
        {
            return !ModTek.Enabled;
        }

        public static void Postfix(ModManagerScreen __instance)
        {
            if (__instance.tempLoadedMods.ContainsKey(ModTek.MODTEK_DEF_NAME) == false)
            {
                __instance.tempLoadedMods.Add(ModTek.MODTEK_DEF_NAME, ModTek.SettingsDef.ToVanilla());
            }
            else
            {
                __instance.tempLoadedMods[ModTek.MODTEK_DEF_NAME] = ModTek.SettingsDef.ToVanilla();
            }
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("GetCombinedModStatus")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerInstalledModsPanel_GetCombinedModStatusDisabled
    {
        public static bool Prepare()
        {
            return !ModTek.Enabled;
        }

        public static void Postfix(ref Dictionary<string, ModStatusItem> __result)
        {
            if (__result.ContainsKey(ModTek.MODTEK_DEF_NAME) == false)
            {
                __result.Add(ModTek.MODTEK_DEF_NAME, ModTek.SettingsDef.ToVanilla());
            }
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("GetCombinedModStatus")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerInstalledModsPanel_GetCombinedModStatus
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ref Dictionary<string, ModStatusItem> __result)
        {
            __result = new Dictionary<string, ModStatusItem>();
            foreach (var mod in ModDefsDatabase.allModDefs)
            {
                if (mod.Key == ModTek.MODTEK_DEF_NAME)
                {
                    continue;
                }

                if (mod.Value.Hidden)
                {
                    continue;
                }

                __result.Add(mod.Key, mod.Value.ToVanilla());
            }

            __result.Add(ModTek.MODTEK_DEF_NAME, ModTek.SettingsDef.ToVanilla());
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("SaveModStatusToFile")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModLoader_SaveSystemModStatusToFile
    {
        public static bool Prepare()
        {
            return !ModTek.Enabled;
        }

        public static bool Prefix(Dictionary<string, ModStatusItem> tempLoadedMods)
        {
            return true;
        }

        public static void Postfix(Dictionary<string, ModStatusItem> tempLoadedMods)
        {
            if (tempLoadedMods.TryGetValue(ModTek.MODTEK_DEF_NAME, out var modtek))
            {
                ModTek.SettingsDef.Enabled = modtek.enabled;
                ModTek.SettingsDef.SaveState();
            }
        }
    }

    [HarmonyPatch(typeof(ModLoader))]
    [HarmonyPatch("SaveModStatusToFile")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModLoader_SaveModStatusToFile
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(Dictionary<string, ModStatusItem> tempLoadedMods)
        {
            Log.Main.Info?.Log("SaveModStatusToFile");
            foreach (var mod in ModDefsDatabase.allModDefs)
            {
                Log.Main.Info?.Log("\t" + mod.Value.QuotedName + ":" + mod.Value.Enabled + ":" + mod.Value.PendingEnable + ":" + mod.Value.LoadFail);
                if (mod.Value.PendingEnable != mod.Value.Enabled)
                {
                    try
                    {
                        mod.Value.Enabled = mod.Value.PendingEnable;
                        mod.Value.SaveState();
                        Log.Main.Info?.Log("\t\tsave state:" + mod.Value.Enabled);
                    }
                    catch (Exception e)
                    {
                        Log.Main.Info?.Log("Can't save mod state", e);
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerScreen))]
    [HarmonyPatch("UnsavedSettings")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerScreen_UnsavedSettings
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ModManagerScreen __instance, ref bool __result)
        {
            __result = false;
            foreach (var mod in ModDefsDatabase.allModDefs)
            {
                if (mod.Value.PendingEnable != mod.Value.Enabled)
                {
                    __result = true;
                    return false;
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerScreen))]
    [HarmonyPatch("ReceiveButtonPress")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerScreen_ReceiveButtonPress
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ModManagerScreen __instance, string button)
        {
            if (button == "revert")
            {
                foreach (var mod in ModDefsDatabase.allModDefs)
                {
                    mod.Value.PendingEnable = mod.Value.Enabled;
                }

                __instance.installedModsPanel.RefreshListViewItems();
                return false;
            }

            if (button == "save")
            {
                return true;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(ModManagerScreen))]
    [HarmonyPatch("ToggleModsEnabled")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerScreen_ToggleModsEnabled
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ModManagerScreen __instance, HBSDOTweenToggle ___modsEnabledToggleBox)
        {
            if (___modsEnabledToggleBox.IsToggled() == false)
            {
                ___modsEnabledToggleBox.SetToggled(true);
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerListViewItem))]
    [HarmonyPatch("ToggleItemEnabled")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerListViewItem_ToggleItemEnabled
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static bool Prefix(ModManagerListViewItem __instance, HBSDOTweenToggle ___toggleBox, ModManagerScreen ____screen)
        {
            if (ModDefsDatabase.allModDefs.ContainsKey(__instance.ModStatusItem.name) == false)
            {
                ___toggleBox.SetToggled(false);
            }
            else
            {
                var mod = ModDefsDatabase.allModDefs[__instance.ModStatusItem.name];
                if (mod.Locked)
                {
                    ___toggleBox.SetToggled(mod.PendingEnable);
                    return false;
                }

                if (mod.PendingEnable)
                {
                    var deps = mod.GatherDependsOnMe();
                    if (deps.Count > 0)
                    {
                        var text = new StringBuilder();
                        text.AppendLine("Some mods relay on this:");
                        var listdeps = deps.ToList();
                        for (var t = 0; t < Mathf.Min(7, deps.Count); ++t)
                        {
                            text.AppendLine(listdeps[t].Key.QuotedName + "->" + (listdeps[t].Value ? "Enable" : "Disable"));
                        }

                        if (deps.Count > 7)
                        {
                            text.AppendLine("...........");
                        }

                        text.AppendLine("They will fail to load, but it will be only your damn problem.");
                        GenericPopupBuilder.Create("Dependency conflict", text.ToString())
                            .AddButton(
                                "Return",
                                () =>
                                {
                                    mod.PendingEnable = true;
                                    ___toggleBox.SetToggled(mod.PendingEnable);
                                }
                            )
                            .AddButton(
                                "Resolve",
                                () =>
                                {
                                    mod.PendingEnable = false;
                                    ___toggleBox.SetToggled(mod.PendingEnable);
                                    foreach (var dmod in deps)
                                    {
                                        dmod.Key.PendingEnable = dmod.Value;
                                    }

                                    ____screen.installedModsPanel.RefreshListViewItems();
                                }
                            )
                            .AddButton(
                                "Shoot own leg",
                                () =>
                                {
                                    mod.PendingEnable = false;
                                    ___toggleBox.SetToggled(mod.PendingEnable);
                                }
                            )
                            .IsNestedPopupWithBuiltInFader()
                            .SetAlwaysOnTop()
                            .Render();
                    }
                    else
                    {
                        mod.PendingEnable = false;
                        ___toggleBox.SetToggled(mod.PendingEnable);
                    }
                }
                else
                {
                    var conflicts = mod.ConflictsWithMe();
                    if (conflicts.Count > 0)
                    {
                        var text = new StringBuilder();
                        text.AppendLine("Some mods conflics with this or this mod have unresolved dependency:");
                        var listconflicts = conflicts.ToList();
                        for (var t = 0; t < Mathf.Min(7, listconflicts.Count); ++t)
                        {
                            text.AppendLine(listconflicts[t].Key.QuotedName + "->" + (listconflicts[t].Value ? "Enable" : "Disable"));
                        }

                        if (conflicts.Count > 7)
                        {
                            text.AppendLine("...........");
                        }

                        text.AppendLine("They will fail to load, but it will be only your damn problem.");
                        GenericPopupBuilder.Create("Dependency conflict", text.ToString())
                            .AddButton(
                                "Return",
                                () =>
                                {
                                    mod.PendingEnable = false;
                                    ___toggleBox.SetToggled(mod.PendingEnable);
                                }
                            )
                            .AddButton(
                                "Resolve",
                                () =>
                                {
                                    mod.PendingEnable = true;
                                    ___toggleBox.SetToggled(mod.PendingEnable);
                                    foreach (var dmod in listconflicts)
                                    {
                                        dmod.Key.PendingEnable = dmod.Value;
                                    }

                                    ____screen.installedModsPanel.RefreshListViewItems();
                                }
                            )
                            .AddButton(
                                "Shoot own leg",
                                () =>
                                {
                                    mod.PendingEnable = true;
                                    ___toggleBox.SetToggled(mod.PendingEnable);
                                }
                            )
                            .IsNestedPopupWithBuiltInFader()
                            .SetAlwaysOnTop()
                            .Render();
                    }
                    else
                    {
                        mod.PendingEnable = true;
                        ___toggleBox.SetToggled(mod.PendingEnable);
                    }
                }
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(ModManagerListViewItem))]
    [HarmonyPatch("SetData")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerListViewItem_SetData
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ModManagerListViewItem __instance, ModStatusItem modStatusItem, LocalizableText ___modNameText, HBSDOTweenToggle ___toggleBox)
        {
            if (ModDefsDatabase.allModDefs.ContainsKey(modStatusItem.name))
            {
                var mod = ModDefsDatabase.allModDefs[modStatusItem.name];
                ___toggleBox.SetToggled(mod.PendingEnable);
                if (mod.LoadFail)
                {
                    ___modNameText.color = Color.red;
                    ___modNameText.SetText("!" + mod.QuotedName);
                }
                else
                {
                    ___modNameText.color = Color.white;
                    ___modNameText.SetText(mod.QuotedName);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ModManagerInstalledModsPanel))]
    [HarmonyPatch("InitializeList")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class ModManagerInstalledModsPanel_InitializeList
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static ModStatusItem ToVanilla(this ModDefEx mod)
        {
            var result = new ModStatusItem();
            result.name = mod.Name;
            result.enabled = mod.Enabled;
            result.version = mod.Version;
            result.website = mod.Website;
            result.failedToLoad = mod.LoadFail;
            result.dependsOn = mod.DependsOn.ToList();
            result.directory = mod.Directory;
            return result;
        }

        public static Dictionary<ModDefEx, bool> GatherDependsOnMe(this ModDefEx moddef)
        {
            var result = new Dictionary<ModDefEx, bool>();
            foreach (var mod in moddef.AffectingOffline)
            {
                if (mod.Key.PendingEnable != mod.Value)
                {
                    result.Add(mod.Key, mod.Value);
                }
            }

            return result;
        }

        public static Dictionary<ModDefEx, bool> ConflictsWithMe(this ModDefEx moddef)
        {
            var result = new Dictionary<ModDefEx, bool>();
            foreach (var mod in moddef.AffectingOnline)
            {
                if (mod.Key.PendingEnable != mod.Value)
                {
                    result.Add(mod.Key, mod.Value);
                }
            }

            return result;
        }

        public static bool Prefix(ModManagerInstalledModsPanel __instance, ref bool __result, ModManagerListView ___modsList)
        {
            typeof(ModManagerInstalledModsPanel).GetMethod("Clear", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(
                    __instance,
                    new object[0]
                    {
                    }
                );
            __instance.SetSort(0);
            if (ModDefsDatabase.allModDefs.Count == 0)
            {
                __result = false;
                return false;
            }

            ;
            Log.Main.Info?.Log("InitializeList");
            foreach (var mod in ModDefsDatabase.allModDefs)
            {
                mod.Value.PendingEnable = mod.Value.Enabled;
                if (mod.Value.Hidden == false)
                {
                    ___modsList.Add(mod.Value.ToVanilla());
                }

                Log.Main.Info?.Log("\t" + mod.Value.QuotedName + ":" + mod.Value.Enabled + ":" + mod.Value.PendingEnable + ":" + mod.Value.LoadFail);
            }

            __result = true;
            return false;
        }
    }
}
