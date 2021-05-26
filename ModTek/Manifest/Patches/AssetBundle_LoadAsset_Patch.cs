using System;
using Harmony;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;
using static ModTek.Logging.Logger;

namespace ModTek.Manifest.Patches
{
    [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string), typeof(Type))]
    internal static class AssetBundle_LoadAsset_Patch
    {
        [UsedImplicitly]
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

        [UsedImplicitly]
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
                var mergedContent = ModsManifest.MergeOriginalContent(__instance.name, name, DateTime.MinValue, originalContent);
                if (mergedContent != null)
                {
                    __result = new TextAsset(mergedContent); // maybe a memory leak?
                }
            }
            catch (Exception e)
            {
                Log("Error", e);
            }
        }
    }
}