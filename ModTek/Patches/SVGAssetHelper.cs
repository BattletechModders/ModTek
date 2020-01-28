using BattleTech;
using BattleTech.Assetbundles;
using BattleTech.Data;
using Harmony;
using ModTek.RuntimeLog;
using SVGImporter;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using static BattleTech.Data.DataManager;

namespace ModTek
{
    [HarmonyPatch(typeof(AmmoCategoryValue))]
    [HarmonyPatch("GetIcon")]
    [HarmonyPatch(MethodType.Normal)]
    public static class AmmoCategory_GetIcon
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static void Postfix(AmmoCategoryValue __instance, ref SVGAsset __result)
        {
            SVGAsset result = ModTek.GetFixedAsset(__instance.Icon);
            if (result != null) { __result = result; }
        }
    }
    [HarmonyPatch(typeof(WeaponCategoryValue))]
    [HarmonyPatch("GetIcon")]
    [HarmonyPatch(MethodType.Normal)]
    public static class WeaponCategoryValue_GetIcon
    {
        public static bool Prepare() { return ModTek.Enabled; }
        public static void Postfix(WeaponCategoryValue __instance, ref SVGAsset __result)
        {
            SVGAsset result = ModTek.GetFixedAsset(__instance.Icon);
            if (result != null) { __result = result; }
        }
    }
    public static class SVGAssetLoadRequest_Load
    {
        private static Action<ResourceLoadRequest<SVGAsset>> ResourceLoadRequest_Load = null;
        private static Type SVGAssetLoadRequest = null;
        private static MethodInfo m_StateSet = typeof(FileLoadRequest).GetProperty("State", BindingFlags.Instance | BindingFlags.Public).GetSetMethod(true);
        private static FieldInfo f_dataManager = typeof(FileLoadRequest).GetField("dataManager", BindingFlags.Instance | BindingFlags.NonPublic);
        private static FieldInfo f_manifestEntry = typeof(FileLoadRequest).GetField("manifestEntry", BindingFlags.Instance | BindingFlags.NonPublic);
        private static MethodInfo m_AssetBundleManagerGet = typeof(DataManager).GetProperty("AssetBundleManager", BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true);
        private static MethodInfo m_RequestResourcesLoad_SVGAsset = typeof(DataManager).GetMethod("RequestResourcesLoad", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(SVGAsset));
        
        private static void RequestResourcesLoad_SVGAsset(this DataManager dataManager, string path, Action<SVGAsset> onComplete)
        {
            m_RequestResourcesLoad_SVGAsset.Invoke(dataManager, new object[] { path, onComplete });
        }
        private static AssetBundleManager AssetBundleManager(this DataManager dataManager)
        {
            return (AssetBundleManager)m_AssetBundleManagerGet.Invoke(dataManager, null);
        }
        private static void State(this FileLoadRequest request, DataManager.FileLoadRequest.RequestState state)
        {
            m_StateSet.Invoke(request, new object[] { state });
        }
        private static DataManager dataManager(this FileLoadRequest request)
        {
            return (DataManager)f_dataManager.GetValue(request);
        }
        private static VersionManifestEntry manifestEntry(this FileLoadRequest request)
        {
            return (VersionManifestEntry)f_manifestEntry.GetValue(request);
        }
        public static void Patch(HarmonyInstance harmony)
        {
            //RLog.M.TWL(0, "SVGAssetLoadRequest patching");
            try
            {
                SVGAssetLoadRequest = typeof(DataManager).GetNestedType("SVGAssetLoadRequest", BindingFlags.NonPublic);
                if (SVGAssetLoadRequest == null) {
                    //RLog.M.WL(1, "BattleTech.Data.DataManager.SVGAssetLoadRequest is null");
                    Type[] types = typeof(DataManager).GetNestedTypes(BindingFlags.NonPublic);
                    foreach(Type tp in types)
                    {
                        //RLog.M.WL(2, tp.Name);
                        if (tp.Name.StartsWith("SVGAssetLoadRequest")) { SVGAssetLoadRequest = tp; break; }
                    }
                    if(SVGAssetLoadRequest == null) return;
                }
                MethodInfo target = SVGAssetLoadRequest.GetMethod("Load", BindingFlags.Instance | BindingFlags.Public);
                harmony.Patch(target, new HarmonyMethod(typeof(SVGAssetLoadRequest_Load).GetMethod("Prefix")));
                MethodInfo method = typeof(ResourceLoadRequest<SVGAsset>).GetMethod("Load", BindingFlags.Instance|BindingFlags.Public);
                harmony.Patch(method, new HarmonyMethod(typeof(SVGAssetLoadRequest_Load).GetMethod("PrefixBase")));
                var dm = new DynamicMethod("ModTekResourceLoadRequestLoad", null, new Type[] { typeof(ResourceLoadRequest<SVGAsset>) }, typeof(ResourceLoadRequest<SVGAsset>));
                var gen = dm.GetILGenerator();
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, method);
                gen.Emit(OpCodes.Ret);
                ResourceLoadRequest_Load = (Action<ResourceLoadRequest<SVGAsset>>)dm.CreateDelegate(typeof(Action<ResourceLoadRequest<SVGAsset>>));
                //RLog.M.WL(1,"DataManager methods");
                //MethodInfo[] methods = typeof(DataManager).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
                //foreach(MethodInfo mi in methods)
                //{
                //    RLog.M.WL(2, mi.Name);
                //}
            }catch(Exception e)
            {
                RLog.M.TWL(0, e.ToString(), true);
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
            ResourceLoadRequest_Load(__instance);//base.Load();
            if (__instance.State != DataManager.FileLoadRequest.RequestState.Requested) { return false; }
            __instance.State(DataManager.FileLoadRequest.RequestState.Processing);
            __instance.StartTimeoutTracking(0.0f);
            if (__instance.ManifestEntry.IsAssetBundled)
            {
                __instance.dataManager().AssetBundleManager().RequestAsset<SVGAsset>(BattleTechResourceType.SVGAsset, __instance.ResourceId, new Action<SVGAsset>(
                    (SVGAsset resource) => {
                        SVGAssetLoadRequest.GetMethod("AssetLoaded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { resource });
                    }
                    ));
            }
            else
            {
                if (!__instance.ManifestEntry.IsResourcesAsset)
                {

                    try
                    {
                        SVGAsset resource = SVGAsset.Load(File.ReadAllText(__instance.ManifestEntry.FilePath));
                        if (resource == null)
                        {
                            throw new NullReferenceException("Fail to load SVG file");
                        }
                        SVGAssetLoadRequest.GetMethod("AssetLoaded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { resource });
                        //RLog.M.WL(1, "Loaded from external file:" + __instance.ManifestEntry.FilePath);
                    }catch(Exception e)
                    {
                        RLog.M.TWL(0, "Fail to load external file:" + __instance.ManifestEntry.FilePath);
                        RLog.M.WL(0, e.ToString());
                        throw new ArgumentNullException("THIS IS !NOT! I REPEAT, !NOT! MODTEK ERROR. YOUR SVG FILE FAIL TO LOAD! More info in ModTek_runtime_log.txt");
                    }
                    return false;
                }
                __instance.dataManager().RequestResourcesLoad_SVGAsset(__instance.ManifestEntry.ResourcesLoadPath, new Action<SVGAsset>(
                    (SVGAsset resource) => {
                        SVGAssetLoadRequest.GetMethod("AssetLoaded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { resource });
                    }
                    ));
            }
            return false;
        }
    }
}