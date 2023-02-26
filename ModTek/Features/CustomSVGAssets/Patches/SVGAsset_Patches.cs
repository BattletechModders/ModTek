using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BattleTech;
using BattleTech.Data;
using BattleTech.UI;
using SVGImporter;
using static BattleTech.Data.DataManager;

namespace ModTek.Features.CustomSVGAssets.Patches;

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
            }

            __instance.PrewarmRequests = prewarmRequests.ToArray();

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

[HarmonyPatch]
internal static class SVGAssetLoadRequest_Load
{
    private static readonly HashSet<string> UILookAndColorConstantsIcons =
        typeof(UILookAndColorConstants)
            .GetFields()
            .Where(f => f.FieldType == typeof(SVGAsset))
            .Select(f => f.Name)
            .ToHashSet();

    public static bool isBuildinIcon(this AmmoCategoryValue ammoCat)
    {
        return UILookAndColorConstantsIcons.Contains(ammoCat.Icon);
    }

    public static bool isBuildinIcon(this WeaponCategoryValue weaponCat)
    {
        return UILookAndColorConstantsIcons.Contains(weaponCat.Icon);
    }

    [HarmonyReversePatch]
    [HarmonyPatch(typeof(ResourceLoadRequest<SVGAsset>), nameof(ResourceLoadRequest<SVGAsset>.Load))]
    public static void BaseLoad(ResourceLoadRequest<SVGAsset> __instance)
    {
    }

    [HarmonyPatch(typeof(SVGAssetLoadRequest), nameof(SVGAssetLoadRequest.Load))]
    [HarmonyPrefix]
    public static bool Prefix(SVGAssetLoadRequest __instance)
    {
        BaseLoad(__instance);
        if (__instance.State != FileLoadRequest.RequestState.Requested)
        {
            return false;
        }

        __instance.State = FileLoadRequest.RequestState.Processing;
        __instance.StartTimeoutTracking(0.0f);
        if (__instance.ManifestEntry.IsAssetBundled)
        {
            __instance.dataManager.AssetBundleManager
                .RequestAsset(
                    BattleTechResourceType.SVGAsset,
                    __instance.ResourceId,
                    new Action<SVGAsset>(__instance.AssetLoaded)
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

                    __instance.AssetLoaded(resource);
                    Log.Main.Info?.Log("\tLoaded from external file:" + __instance.ManifestEntry.FilePath);
                }
                catch (Exception e)
                {
                    Log.Main.Error?.Log("Failed to load external file:" + __instance.ManifestEntry.FilePath, e);
                    throw new ArgumentNullException("THIS IS !NOT! I REPEAT, !NOT! MODTEK ERROR. YOUR SVG FILE FAIL TO LOAD! More info in ModTek_runtime_log.txt");
                }

                return false;
            }

            var onComplete = __instance.AssetLoaded;
            __instance.dataManager.RequestResourcesLoad(__instance.ManifestEntry.ResourcesLoadPath, onComplete);
        }

        return false;
    }
}