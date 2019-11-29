using BattleTech.ModSupport;
using BattleTech.Save;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using Harmony;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using static ModTek.Util.Logger;

namespace ModTek.Patches
{
    [HarmonyPatch(typeof(BattleTech.ModSupport.ModLoader))]
    [HarmonyPatch("AreModsEnabled")]
    [HarmonyPatch(MethodType.Getter)]
    public static class ModLoader_AreModsEnabled
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static void Postfix(ref bool __result)
        {
            //Action OnModLoadComplete = (Action)typeof(BattleTech.ModSupport.ModLoader).GetField("OnModLoadComplete",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
            //OnModLoadComplete.Invoke();
            //typeof(BattleTech.ModSupport.ModLoader).GetProperty("ModFilePaths", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null,new List<string>().ToArray());
            __result = false;
        }
    }
    [HarmonyPatch(typeof(BattleTech.ModSupport.ModLoader))]
    [HarmonyPatch("InitializeModSettings")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModLoader_InitializeModSettings
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static Dictionary<string, bool> loadedModsCache = new Dictionary<string, bool>();
        public static bool Prefix(ref BattleTech.Save.LocalUserSettings playerSettings)
        {
            loadedModsCache = playerSettings.loadedMods;
            playerSettings.loadedMods.Clear();
            foreach (var mod in ModTek.allModDefs)
            {
                if (mod.Value.Hidden == false) { playerSettings.loadedMods.Add(mod.Key, mod.Value.Enabled); }
            }
            return false;
        }
        public static void Postfix(ref BattleTech.Save.LocalUserSettings playerSettings)
        {
            playerSettings.loadedMods = loadedModsCache;
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerScreen))]
    [HarmonyPatch("Init")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerScreen_InitModResources
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix(BattleTech.UI.ModManagerScreen __instance)
        {
            PlayerPrefs.SetInt("ModsEnabled",1);
            return true;
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerScreen))]
    [HarmonyPatch("Init")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerScreen_InitModResourcesDisabled
    {
        public static bool Prepare() { return !ModTek.Enabled; }
        public static void Postfix(BattleTech.UI.ModManagerScreen __instance)
        {
            //if(ModLoader.ModDefs)
            if (__instance.tempLoadedMods.ContainsKey(ModTek.MODTEK_DEF_NAME) == false) { __instance.tempLoadedMods.Add(ModTek.MODTEK_DEF_NAME, false); };
            __instance.tempLoadedMods[ModTek.MODTEK_DEF_NAME] = false;
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerInstalledModsPanel))]
    [HarmonyPatch("InitializeList")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerInstalledModsPanel_InitializeListDisabled
    {
        public static bool Prepare() { return !ModTek.Enabled; }
        public static bool Prefix(BattleTech.UI.ModManagerInstalledModsPanel __instance, ref bool __result, BattleTech.UI.ModManagerListView ___modsList)
        {
            if (ModLoader.ModDefs.ContainsKey(ModTek.MODTEK_DEF_NAME) == false) { ModLoader.ModDefs.Add(ModTek.MODTEK_DEF_NAME, ModTek.SettingsDef.ToVanilla()); };
            return true;
        }
    }
    [HarmonyPatch(typeof(BattleTech.Save.ActiveOrDefaultSettings))]
    [HarmonyPatch("SaveUserSettings")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ActiveOrDefaultSettings_SaveUserSettingsDisabled
    {
        public static bool Prepare() { return !ModTek.Enabled; }
        public static bool Prefix()
        {
            if (PlayerPrefs.GetInt("ModsEnabled", 0) == 0) { return true; };
            LocalUserSettings playerSettings = BattleTech.Save.ActiveOrDefaultSettings.LocalSettings;
            if (playerSettings.loadedMods.ContainsKey(ModTek.MODTEK_DEF_NAME))
            {
                if(playerSettings.loadedMods[ModTek.MODTEK_DEF_NAME] == true)
                {
                    string moddefpath = Path.Combine(ModTek.SettingsDef.Directory,ModTek.MOD_JSON_NAME);
                    try
                    {
                        ModTek.SettingsDef.Enabled = true;
                        File.WriteAllText(moddefpath, JsonConvert.SerializeObject(ModTek.SettingsDef, Formatting.Indented));
                        ModTek.SettingsDef.SaveState();
                    }
                    catch(Exception e)
                    {

                    }
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(BattleTech.Save.ActiveOrDefaultSettings))]
    [HarmonyPatch("SaveUserSettings")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ActiveOrDefaultSettings_SaveUserSettings
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix()
        {
            PlayerPrefs.SetInt("ModsEnabled", 1);
            LocalUserSettings playerSettings = BattleTech.Save.ActiveOrDefaultSettings.LocalSettings;
            playerSettings.loadedMods = ModLoader_InitializeModSettings.loadedModsCache;
            foreach(var mod in ModTek.allModDefs)
            {
                if (mod.Value.PendingEnable != mod.Value.Enabled)
                {
                    string moddefpath = Path.Combine(mod.Value.Directory, ModTek.MOD_JSON_NAME);
                    try
                    {
                        mod.Value.Enabled = mod.Value.PendingEnable;
                        mod.Value.SaveState();
                    }
                    catch (Exception e)
                    {

                    }
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerScreen))]
    [HarmonyPatch("UnsavedSettings")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerScreen_UnsavedSettings
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix(BattleTech.UI.ModManagerScreen __instance, ref bool __result)
        {
            __result = false;
            foreach(var mod in ModTek.allModDefs){
                if (mod.Value.PendingEnable != mod.Value.Enabled) { __result = true; return false; }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerScreen))]
    [HarmonyPatch("ReceiveButtonPress")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerScreen_ReceiveButtonPress
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix(BattleTech.UI.ModManagerScreen __instance, string button)
        {
            if(button == "revert")
            {
                foreach (var mod in ModTek.allModDefs){ mod.Value.PendingEnable = mod.Value.Enabled; }
                __instance.installedModsPanel.RefreshListViewItems();
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerScreen))]
    [HarmonyPatch("ToggleModsEnabled")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerScreen_ToggleModsEnabled
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix(BattleTech.UI.ModManagerScreen __instance, BattleTech.UI.HBSDOTweenToggle ___modsEnabledToggleBox)
        {
            if (___modsEnabledToggleBox.IsToggled() == false) { ___modsEnabledToggleBox.SetToggled(true); }
            return false;
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerListViewItem))]
    [HarmonyPatch("ToggleItemEnabled")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerListViewItem_ToggleItemEnabled
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static bool Prefix(BattleTech.UI.ModManagerListViewItem __instance, BattleTech.UI.HBSDOTweenToggle ___toggleBox, ModManagerScreen ____screen)
        {
            if (ModTek.allModDefs.ContainsKey(__instance.modDef.Name) == false)
            {
                //if (ModTek.allModDefs[modDef.Name].Enabled == false) { ___modNameText.color = Color.red; };
                ___toggleBox.SetToggled(false);
            }
            else
            {
                ModDefEx mod = ModTek.allModDefs[__instance.modDef.Name];
                if (mod.Locked)
                {
                    ___toggleBox.SetToggled(mod.PendingEnable);
                    return false;
                }
                if(mod.PendingEnable == true)
                {
                    List<ModDefEx> deps = mod.GatherDependsOnMe();
                    if (deps.Count > 0)
                    {
                        StringBuilder text = new StringBuilder();
                        text.AppendLine("Some mods relay on this:");
                        for (int t = 0; t < Mathf.Min(7, deps.Count); ++t)
                        {
                            text.AppendLine(deps[t].Name);
                        }
                        if (deps.Count > 7) { text.AppendLine("..........."); }
                        text.AppendLine("They will fail to load, but it will be only your damn problem.");
                        GenericPopupBuilder.Create("Dependency conflict", text.ToString()).AddButton("Return").AddButton("Resolve", (Action)(() =>
                        {
                            mod.PendingEnable = false;
                            ___toggleBox.SetToggled(mod.PendingEnable);
                            foreach (ModDefEx dmod in deps)
                            {
                                dmod.PendingEnable = false;
                            }
                            ____screen.installedModsPanel.RefreshListViewItems();
                        })).AddButton("Shoot own leg", (Action)(() => { mod.PendingEnable = false; ___toggleBox.SetToggled(mod.PendingEnable); })).IsNestedPopupWithBuiltInFader().SetAlwaysOnTop().Render();
                    }
                    else
                    {
                        mod.PendingEnable = false;
                        ___toggleBox.SetToggled(mod.PendingEnable);
                    }
                }
                else {
                    Dictionary<ModDefEx,bool> conflicts = mod.ConflictsWithMe();
                    if (conflicts.Count > 0)
                    {
                        StringBuilder text = new StringBuilder();
                        text.AppendLine("Some mods conflics with this or this mod have unresolved dependency:");
                        List<KeyValuePair<ModDefEx, bool>> listconflicts = conflicts.ToList();
                        for (int t = 0; t < Mathf.Min(7, listconflicts.Count); ++t)
                        {
                            text.AppendLine(listconflicts[t].Key.Name + "->" + (listconflicts[t].Value ? "Enable":"Disable"));
                        }
                        if (conflicts.Count > 7) { text.AppendLine("..........."); }
                        text.AppendLine("They will fail to load, but it will be only your damn problem.");
                        GenericPopupBuilder.Create("Dependency conflict", text.ToString()).AddButton("Return").AddButton("Resolve", (Action)(() =>
                        {
                            mod.PendingEnable = true;
                            ___toggleBox.SetToggled(mod.PendingEnable);
                            foreach (var dmod in listconflicts)
                            {
                                dmod.Key.PendingEnable = dmod.Value;
                            }
                            ____screen.installedModsPanel.RefreshListViewItems();
                        })).AddButton("Shoot own leg", (Action)(() => { mod.PendingEnable = true; ___toggleBox.SetToggled(mod.PendingEnable); })).IsNestedPopupWithBuiltInFader().SetAlwaysOnTop().Render();
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
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerListViewItem))]
    [HarmonyPatch("SetData")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerListViewItem_SetData
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static void Postfix(BattleTech.UI.ModManagerListViewItem __instance, ModDef modDef, LocalizableText ___modNameText, BattleTech.UI.HBSDOTweenToggle ___toggleBox)
        {
            if (ModTek.allModDefs.ContainsKey(modDef.Name))
            {
                ModDefEx mod = ModTek.allModDefs[modDef.Name];
                ___toggleBox.SetToggled(mod.PendingEnable);
                if (mod.LoadFail) {
                    ___modNameText.color = Color.red;
                    ___modNameText.SetText("!" + mod.Name);
                } else {
                    ___modNameText.color = Color.white;
                    ___modNameText.SetText(mod.Name);
                }
            }
        }
    }
    [HarmonyPatch(typeof(BattleTech.UI.ModManagerInstalledModsPanel))]
    [HarmonyPatch("InitializeList")]
    [HarmonyPatch(MethodType.Normal)]
    public static class ModManagerInstalledModsPanel_InitializeList
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static ModDef ToVanilla(this ModDefEx mod)
        {
            ModDef result = new ModDef();
            result.Name = mod.Name;
            result.Enabled = mod.Enabled;
            result.Version = mod.Version;
            result.Website = mod.Website;
            result.Author = mod.Author;
            result.Contact = mod.Contact;
            result.Description = mod.Description;
            return result;
        }
        public static List<ModDefEx> GatherDependsOnMe(this ModDefEx moddef)
        {
            HashSet<ModDefEx> result = new HashSet<ModDefEx>();
            GatherDependsOnMeRecursive(moddef, ref result);
            return result.ToList();
        }
        private static void GatherDependsOnMeRecursive(ModDefEx moddef, ref HashSet<ModDefEx> result)
        {
            if (result == null) { result = new HashSet<ModDefEx>(); };
            foreach (var mod in ModTek.allModDefs)
            {
                if (mod.Value.PendingEnable == false) { continue; }
                if (mod.Value.DependsOn.Contains(moddef.Name)) {
                    if (result.Contains(mod.Value) == false)
                    {
                        result.Add(mod.Value);
                        GatherDependsOnMeRecursive(mod.Value, ref result);
                    }
                };
            }
        }
        public static Dictionary<ModDefEx,bool> ConflictsWithMe(this ModDefEx moddef)
        {
            Dictionary<ModDefEx, bool> result = new Dictionary<ModDefEx, bool>();
            foreach (string modname in moddef.ConflictsWith)
            {
                if (ModTek.allModDefs.ContainsKey(modname) == false) { continue; }
                ModDefEx mod = ModTek.allModDefs[modname];
                if (mod.PendingEnable == false) { continue; }
                if (result.ContainsKey(mod) == false){ result.Add(mod, false); }
            }
            foreach (var mod in ModTek.allModDefs) {
                if (mod.Value.PendingEnable == false) { continue; }
                if (mod.Value.ConflictsWith.Contains(moddef.Name)) { continue; }
            }
            HashSet<ModDefEx> conflictDeps = new HashSet<ModDefEx>();
            foreach(ModDefEx mod in result.Keys.ToHashSet())
            {
                GatherDependsOnMeRecursive(mod, ref conflictDeps);
            }
            foreach(ModDefEx mod in conflictDeps)
            {
                if (result.ContainsKey(mod) == false) { result.Add(mod, false); }
            }
            foreach (string modname in moddef.DependsOn)
            {
                if (ModTek.allModDefs.ContainsKey(modname) == false) { continue; }
                ModDefEx mod = ModTek.allModDefs[modname];
                if (mod.PendingEnable == true) { continue; }
                Dictionary<ModDefEx, bool> subresult = mod.ConflictsWithMe();
                if (result.ContainsKey(mod) == false) { result.Add(mod, true); }
                foreach(var submod in subresult)
                {
                    if (result.ContainsKey(submod.Key) == false) { result.Add(submod.Key, submod.Value); }
                }
            }
            return result;
        }
        public static bool Prefix(BattleTech.UI.ModManagerInstalledModsPanel __instance, ref bool __result, BattleTech.UI.ModManagerListView ___modsList)
        {
            typeof(BattleTech.UI.ModManagerInstalledModsPanel).GetMethod("Clear", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[0] { });
            __instance.SetSort(0);
            if (ModTek.allModDefs.Count == 0) { __result = false; return false; };
            foreach(var mod in ModTek.allModDefs)
            {
                mod.Value.PendingEnable = mod.Value.Enabled;
                if (mod.Value.Hidden == false){ ___modsList.Add(mod.Value.ToVanilla()); }
            }
            __result = true;
            /*StringBuilder dbg = new StringBuilder();
            try
            {
                IList items = (IList)typeof(BattleTech.UI.ModManagerListView).BaseType.BaseType.BaseType.GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(___modsList);
                foreach (object itm in items)
                {
                    BattleTech.UI.ModManagerListViewItem item = itm as BattleTech.UI.ModManagerListViewItem;
                    if (item != null)
                    {
                        item.SetData(item.modDef);
                    }
                }
            }catch(Exception e)
            {
                dbg.AppendLine(e.ToString());
            }*/
            return false;
        }
    }
}