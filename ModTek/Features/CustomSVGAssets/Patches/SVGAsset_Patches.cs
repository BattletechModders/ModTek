using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using SVGImporter;
using static BattleTech.Data.DataManager;

namespace ModTek.Features.CustomSVGAssets.Patches
{
    [HarmonyPatch(typeof(ApplicationConstants))]
    [HarmonyPatch("FromJSON")]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch(
        new[]
        {
            typeof(string)
        }
    )]
    internal static class ApplicationConstants_FromJSON
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(ApplicationConstants __instance)
        {
            Log.Main.Info?.Log("ApplicationConstants.FromJSON. PrewarmRequests:");
            try
            {
                var prewarmRequests = new List<PrewarmRequest>();
                prewarmRequests.AddRange(__instance.PrewarmRequests);
                var svgs = new HashSet<string>();
                foreach (var ammoCat in AmmoCategoryEnumeration.AmmoCategoryList)
                {
                    if (ammoCat.isBuildinIcon() == false)
                    {
                        svgs.Add(ammoCat.Icon);
                    }
                }

                foreach (var weaponCat in WeaponCategoryEnumeration.WeaponCategoryList)
                {
                    if (weaponCat.isBuildinIcon() == false)
                    {
                        svgs.Add(weaponCat.Icon);
                    }
                }

                var fields = typeof(UILookAndColorConstants).GetFields();
                foreach (var field in fields)
                {
                    //RLog.M.WL(1, field.Name+":"+field.FieldType);
                    if (field.FieldType != typeof(SVGAsset))
                    {
                        continue;
                    }

                    var id = "UILookAndColorConstants." + field.Name;
                    if (SVGAssetFeature.isInSystemIcons(id))
                    {
                        svgs.Add(id);
                    }
                }

                foreach (var svg in svgs)
                {
                    if (string.IsNullOrEmpty(svg) == false)
                    {
                        prewarmRequests.Add(new PrewarmRequest(BattleTechResourceType.SVGAsset, svg));
                    }

                    ;
                }

                typeof(ApplicationConstants).GetProperty("PrewarmRequests", BindingFlags.Instance | BindingFlags.Public)
                    .GetSetMethod(true)
                    .Invoke(
                        __instance,
                        new object[]
                        {
                            prewarmRequests.ToArray()
                        }
                    );
                foreach (var preq in __instance.PrewarmRequests)
                {
                    Log.Main.Info?.Log("\t" + preq.ResourceType + ":" + preq.ResourceID);
                }
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e);
            }
        }
    }

    [HarmonyPatch(typeof(DataManager))]
    [HarmonyPatch("PrewarmComplete")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class DataManager_PrewarmComplete
    {
        public static DataManager dataManager { get; private set; }

        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(DataManager __instance)
        {
            Log.Main.Info?.Log("DataManager.PrewarmComplete");
            dataManager = __instance;
            if (UIManager.HasInstance)
            {
                var fields = typeof(UILookAndColorConstants).GetFields();
                foreach (var field in fields)
                {
                    //RLog.M.WL(1, field.Name+":"+field.FieldType);
                    if (field.FieldType != typeof(SVGAsset))
                    {
                        continue;
                    }

                    var result = dataManager.GetObjectOfType<SVGAsset>("UILookAndColorConstants." + field.Name, BattleTechResourceType.SVGAsset);
                    if (result != null)
                    {
                        Log.Main.Info?.Log("\tUpdating icon " + field.Name);
                        field.SetValue(UIManager.Instance.UILookAndColorConstants, result);
                    }
                }
            }
            else
            {
                Log.Main.Warning?.Log("\tUIManager have no instance");
            }
        }
    }

    [HarmonyPatch(typeof(AmmoCategoryValue))]
    [HarmonyPatch("GetIcon")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class AmmoCategory_GetIcon
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(AmmoCategoryValue __instance, ref SVGAsset __result)
        {
            if (DataManager_PrewarmComplete.dataManager == null)
            {
                return;
            }

            var result = DataManager_PrewarmComplete.dataManager.GetObjectOfType<SVGAsset>(__instance.Icon, BattleTechResourceType.SVGAsset);
            if (result != null)
            {
                __result = result;
            }
        }
    }

    [HarmonyPatch(typeof(WeaponCategoryValue))]
    [HarmonyPatch("GetIcon")]
    [HarmonyPatch(MethodType.Normal)]
    internal static class WeaponCategoryValue_GetIcon
    {
        public static bool Prepare()
        {
            return ModTek.Enabled;
        }

        public static void Postfix(WeaponCategoryValue __instance, ref SVGAsset __result)
        {
            if (DataManager_PrewarmComplete.dataManager == null)
            {
                return;
            }

            var result = DataManager_PrewarmComplete.dataManager.GetObjectOfType<SVGAsset>(__instance.Icon, BattleTechResourceType.SVGAsset);
            if (result != null)
            {
                __result = result;
            }
        }
    }

    internal static class SVGAssetLoadRequest_Load
    {
        private static HashSet<string> UILookAndColorConstantsIcons = new HashSet<string>();

        public static bool isBuildinIcon(this AmmoCategoryValue ammoCat)
        {
            return UILookAndColorConstantsIcons.Contains(ammoCat.Icon);
        }

        public static bool isBuildinIcon(this WeaponCategoryValue weaponCat)
        {
            return UILookAndColorConstantsIcons.Contains(weaponCat.Icon);
        }

        private static Action<ResourceLoadRequest<SVGAsset>> ResourceLoadRequest_Load;
        private static Type SVGAssetLoadRequest;
        private static MethodInfo m_StateSet = typeof(FileLoadRequest).GetProperty("State", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true);
        private static FieldInfo f_dataManager = typeof(FileLoadRequest).GetField("dataManager", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo f_manifestEntry = typeof(FileLoadRequest).GetField("manifestEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo m_AssetBundleManagerGet = typeof(DataManager).GetProperty("AssetBundleManager", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
        private static MethodInfo m_RequestResourcesLoad_SVGAsset = typeof(DataManager).GetMethod("RequestResourcesLoad", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(SVGAsset));

        private static void RequestResourcesLoad_SVGAsset(this DataManager dataManager, string path, Action<SVGAsset> onComplete)
        {
            m_RequestResourcesLoad_SVGAsset.Invoke(
                dataManager,
                new object[]
                {
                    path,
                    onComplete
                }
            );
        }

        private static AssetBundleManager AssetBundleManager(this DataManager dataManager)
        {
            return (AssetBundleManager) m_AssetBundleManagerGet.Invoke(dataManager, null);
        }

        private static void State(this FileLoadRequest request, FileLoadRequest.RequestState state)
        {
            m_StateSet.Invoke(
                request,
                new object[]
                {
                    state
                }
            );
        }

        private static DataManager dataManager(this FileLoadRequest request)
        {
            return (DataManager) f_dataManager.GetValue(request);
        }

        private static VersionManifestEntry manifestEntry(this FileLoadRequest request)
        {
            return (VersionManifestEntry) f_manifestEntry.GetValue(request);
        }

        public static void Patch(HarmonyInstance harmony)
        {
            //RLog.M.TWL(0, "SVGAssetLoadRequest patching");
            try
            {
                SVGAssetLoadRequest = typeof(DataManager).GetNestedType("SVGAssetLoadRequest", BindingFlags.NonPublic);
                if (SVGAssetLoadRequest == null)
                {
                    //RLog.M.WL(1, "BattleTech.Data.DataManager.SVGAssetLoadRequest is null");
                    var types = typeof(DataManager).GetNestedTypes(BindingFlags.NonPublic);
                    foreach (var tp in types)
                    {
                        //RLog.M.WL(2, tp.Name);
                        if (tp.Name.StartsWith("SVGAssetLoadRequest"))
                        {
                            SVGAssetLoadRequest = tp;
                            break;
                        }
                    }

                    if (SVGAssetLoadRequest == null)
                    {
                        return;
                    }
                }

                var target = SVGAssetLoadRequest.GetMethod("Load", BindingFlags.Instance | BindingFlags.Public);
                harmony.Patch(target, new HarmonyMethod(typeof(SVGAssetLoadRequest_Load).GetMethod("Prefix")));
                var method = typeof(ResourceLoadRequest<SVGAsset>).GetMethod("Load", BindingFlags.Instance | BindingFlags.Public);
                harmony.Patch(method, new HarmonyMethod(typeof(SVGAssetLoadRequest_Load).GetMethod("PrefixBase")));
                var dm = new DynamicMethod(
                    "ModTekResourceLoadRequestLoad",
                    null,
                    new[]
                    {
                        typeof(ResourceLoadRequest<SVGAsset>)
                    },
                    typeof(ResourceLoadRequest<SVGAsset>)
                );
                var gen = dm.GetILGenerator();
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, method);
                gen.Emit(OpCodes.Ret);
                ResourceLoadRequest_Load = (Action<ResourceLoadRequest<SVGAsset>>) dm.CreateDelegate(typeof(Action<ResourceLoadRequest<SVGAsset>>));
                var fields = typeof(UILookAndColorConstants).GetFields();
                Log.Main.Info?.Log("UILookAndColorConstants fields:" + fields.Length);
                foreach (var field in fields)
                {
                    //RLog.M.WL(1, field.Name+":"+field.FieldType);
                    if (field.FieldType != typeof(SVGAsset))
                    {
                        continue;
                    }

                    UILookAndColorConstantsIcons.Add(field.Name);
                }
            }
            catch (Exception e)
            {
                Log.Main.Error?.Log(e);
            }
        }

        public static bool PrefixBase(ResourceLoadRequest<SVGAsset> __instance)
        {
            //RLog.M.TWL(0, "ResourceLoadRequest<SVGAsset>.Load " + __instance.ManifestEntry.Name);
            return true;
        }

        public static bool Prefix(ResourceLoadRequest<SVGAsset> __instance)
        {
            //RLog.M.TWL(0, "SVGAssetLoadRequest.Load "+__instance.ManifestEntry.Name);
            //return true;
            ResourceLoadRequest_Load(__instance); //base.Load();
            if (__instance.State != FileLoadRequest.RequestState.Requested)
            {
                return false;
            }

            __instance.State(FileLoadRequest.RequestState.Processing);
            __instance.StartTimeoutTracking(0.0f);
            if (__instance.ManifestEntry.IsAssetBundled)
            {
                __instance.dataManager()
                    .AssetBundleManager()
                    .RequestAsset(
                        BattleTechResourceType.SVGAsset,
                        __instance.ResourceId,
                        new Action<SVGAsset>(
                            resource =>
                            {
                                SVGAssetLoadRequest.GetMethod("AssetLoaded", BindingFlags.Instance | BindingFlags.NonPublic)
                                    .Invoke(
                                        __instance,
                                        new object[]
                                        {
                                            resource
                                        }
                                    );
                            }
                        )
                    );
            }
            else
            {
                if (!__instance.ManifestEntry.IsResourcesAsset)
                {
                    try
                    {
                        var resource = SVGAsset.Load(File.ReadAllText(__instance.ManifestEntry.FilePath));
                        if (resource == null)
                        {
                            throw new NullReferenceException("Fail to load SVG file");
                        }

                        SVGAssetLoadRequest.GetMethod("AssetLoaded", BindingFlags.Instance | BindingFlags.NonPublic)
                            .Invoke(
                                __instance,
                                new object[]
                                {
                                    resource
                                }
                            );
                        Log.Main.Info?.Log("\tLoaded from external file:" + __instance.ManifestEntry.FilePath);
                    }
                    catch (Exception e)
                    {
                        Log.Main.Error?.Log("Failed to load external file:" + __instance.ManifestEntry.FilePath, e);
                        throw new ArgumentNullException("THIS IS !NOT! I REPEAT, !NOT! MODTEK ERROR. YOUR SVG FILE FAIL TO LOAD! More info in ModTek_runtime_log.txt");
                    }

                    return false;
                }

                __instance.dataManager()
                    .RequestResourcesLoad_SVGAsset(
                        __instance.ManifestEntry.ResourcesLoadPath,
                        resource =>
                        {
                            SVGAssetLoadRequest.GetMethod("AssetLoaded", BindingFlags.Instance | BindingFlags.NonPublic)
                                .Invoke(
                                    __instance,
                                    new object[]
                                    {
                                        resource
                                    }
                                );
                        }
                    );
            }

            return false;
        }
    }
}
