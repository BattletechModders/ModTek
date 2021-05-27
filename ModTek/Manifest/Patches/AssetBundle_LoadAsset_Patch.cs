using System;
using Harmony;
using UnityEngine;
using Object = UnityEngine.Object;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest.Patches
{
    [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string), typeof(Type))]
    internal static class AssetBundle_LoadAsset_Patch
    {
        internal static bool Prefix(AssetBundle __instance, string name, Type type, ref Object __result)
        {
            try
            {
                if (typeof(TextAsset) != type)
                {
                    return true;
                }

                var cache = ModsManifest.GetMergedContent(__instance.name, name, DateTime.MinValue);
                if (cache == null)
                {
                    return true;
                }

                __result = new TextAsset(cache); // maybe a memory leak?
                return false;
            }
            catch (Exception e)
            {
                Log("Error", e);
                return true;
            }
        }

        internal static void Postfix(AssetBundle __instance, string name, Type type, ref Object __result)
        {
            try
            {
                if (typeof(TextAsset) != type)
                {
                    return;
                }

                var ta = (TextAsset) __result;

                var originalContent = ta.text;
                var changedContent = ModsManifest.ContentLoaded(__instance.name, name, DateTime.MinValue, originalContent);
                if (changedContent != null)
                {
                    __result = new TextAsset(changedContent); // maybe a memory leak?
                }
            }
            catch (Exception e)
            {
                Log("Error", e);
            }
        }
    }
}