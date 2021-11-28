using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.Data;
using Harmony;
using HBS.Data;
using UnityEngine;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.Patches
{
    [HarmonyPatch]
    public static class DataManagerFileLoadRequest_Load_Patch
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        private static MethodInfo RequestAssetOriginal;
        private static MethodInfo RequestAssetReplacement;
        private static MethodInfo RequestResourcesLoadOriginal;
        private static MethodInfo RequestResourcesLoadReplacement;
        private static MethodInfo LoadResourceOriginal;
        private static MethodInfo LoadResourceReplacement;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            RequestAssetOriginal = AccessTools.Method(
                typeof(AssetBundleManager),
                nameof(AssetBundleManager.RequestAsset),
                null,
                new[]
                {
                    typeof(TextAsset)
                }
            );
            RequestAssetReplacement = AccessTools.Method(
                typeof(DataManagerFileLoadRequest_Load_Patch),
                nameof(RequestAsset)
            );
            RequestResourcesLoadOriginal = AccessTools.Method(
                typeof(DataManager),
                "RequestResourcesLoad",
                null,
                new[]
                {
                    typeof(TextAsset)
                }
            );
            RequestResourcesLoadReplacement = AccessTools.Method(
                typeof(DataManagerFileLoadRequest_Load_Patch),
                nameof(RequestResourcesLoad)
            );
            LoadResourceOriginal = AccessTools.Method(
                typeof(DataLoader),
                nameof(DataLoader.LoadResource),
                new[]
                {
                    typeof(string),
                    typeof(Action<string>)
                }
            );
            LoadResourceReplacement = AccessTools.Method(
                typeof(DataManagerFileLoadRequest_Load_Patch),
                nameof(LoadResource)
            );

            return DataManagerFileLoadRequest_OnLoadedWithText_Patch.GetLoadMethods();
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(
                    RequestAssetOriginal,
                    RequestAssetReplacement
                )
                .MethodReplacer(
                    RequestResourcesLoadOriginal,
                    RequestResourcesLoadReplacement
                )
                .MethodReplacer(
                    LoadResourceOriginal,
                    LoadResourceReplacement
                );
        }

        public static void LoadResource(DataLoader instance, string path, Action<string> handler)
        {
            if (FinishWithMergedContent(handler.Target))
            {
                return;
            }

            instance.LoadResource(path, handler);
        }

        public static void RequestResourcesLoad(DataManager instance, string path, Action<TextAsset> onComplete)
        {
            throw new InvalidOperationException(); // TODO implement (how to test?)
        }

        public static void RequestAsset(AssetBundleManager instance, BattleTechResourceType type, string id, Action<TextAsset> loadedCallback)
        {
            if (FinishWithMergedContent(loadedCallback.Target))
            {
                return;
            }

            instance.RequestAsset(type, id, loadedCallback);
        }

        private static bool FinishWithMergedContent(object actionTarget)
        {
            if (actionTarget is not DataManager.FileLoadRequest request)
            {
                Log("Internal Error: request can't be not FileLoadRequest");
                return false;
            }

            var cachedContent = ModsManifest.GetMergedContent(request.ManifestEntry);
            if (cachedContent == null)
            {
                return false;
            }

            request.OnLoadedWithText(cachedContent);
            return true;
        }

        private static void OnLoadedWithText(this DataManager.FileLoadRequest request, string content)
        {
            var method = request.GetType()
                .GetMethod("OnLoadedWithText", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new ArgumentException();
            }
            method.Invoke(request, new object[]{content});
        }
    }
}
