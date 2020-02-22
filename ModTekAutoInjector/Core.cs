using BattleTech;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModTekAutoInjector
{
    [HarmonyPatch(typeof(MainMenu), "Init")]
    public static class MainMenu_Init_Patch
    {
        private static bool Played = false;
        public static void Postfix()
        {
            if (Played) { return; }
            GenericPopupBuilder.Create("ModTek need to be reinjected", "ModTek have to be reinjected. Please restart application")
                .AddButton("Exit", (Action)(() => { Application.Quit(); }))
                .Render();
            Played = true;
        }
    }
    public static class Core
    {
        public static void CopyFile(string dir1,string dir2, string filename)
        {
            if (!File.Exists(Path.Combine(dir2,filename))) File.Copy(Path.Combine(dir1, filename),Path.Combine(dir2,filename));
        }
        public static void Init(string directory, string settingsJson)
        {
            Log.BaseDirectory = directory;
            Log.InitLog();
            Log.M.TWL(0, "Inited");
            bool ModTekInjected = false;
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Log.M.TWL(0, assembly.FullName);
                if (assembly.FullName.StartsWith("ModTek,")) { ModTekInjected = true; break; }
            }
            if (ModTekInjected) {
                Log.M.TWL(0, "Already injected");
                return;
            }
            var harmony = HarmonyInstance.Create("ru.mission.modtekautopatcher");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.M.TWL(0, "Patched");
            Injector.Main(directory);
            Log.M.TWL(0, "Injected");
            string ModsDir = Path.GetDirectoryName(typeof(BattleTech.GameInstance).Assembly.Location);
            ModsDir = Path.Combine(ModsDir, ".."); //Battletech_Data
            ModsDir = Path.Combine(ModsDir, ".."); //root
            ModsDir = Path.Combine(ModsDir, "Mods"); //Mods
            Directory.CreateDirectory(ModsDir);
            ModsDir = Path.Combine(ModsDir, "ModTek"); //ModTek
            Directory.CreateDirectory(ModsDir);
            Core.CopyFile(directory, ModsDir, "ModTek.dll");
            Core.CopyFile(directory, ModsDir, "0Harmony.dll");
            Core.CopyFile(directory, ModsDir, "Ionic.Zip.dll");
            Core.CopyFile(directory, ModsDir, "Mono.Cecil.dll");
            Core.CopyFile(directory, ModsDir, "Mono.Cecil.Mdb.dll");
            Core.CopyFile(directory, ModsDir, "Mono.Cecil.Pdb.dll");
            Core.CopyFile(directory, ModsDir, "Mono.Cecil.Rocks.dll");
            Core.CopyFile(directory, ModsDir, "Newtonsoft.Json.dll");
            Core.CopyFile(directory, ModsDir, "README.md");
            Core.CopyFile(directory, ModsDir, "System.Runtime.Serialization.dll");
            Core.CopyFile(directory, ModsDir, "modtekassetbundle");
            Core.CopyFile(directory, ModsDir, "ModTekInjector.exe");
        }
    }
}
