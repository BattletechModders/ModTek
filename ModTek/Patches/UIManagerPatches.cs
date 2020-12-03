using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.RuntimeLog;
using BattleTech;

namespace ModTek.Patches
{
    /*
     * This patch exists to prevent https://github.com/BattletechModders/ModTek/issues/172. When HBS refactored the mod-loader for 1.9.1,
     *   users started receiving errors when loading ME mechDefs that overwrote the vanilla Shadowhawk DLC mechdef. Prior to 1.9 the patch
     *   VersionManifestUtilities::LoadDefaultManifest must have been just honored at game start. In 1.9.1 the DataManager.ResourceLocator
     *   associated with the UnityGameInstance is created before ModTek applies its harmony patches. This prevents it from reading
     *   the ModTek-modified DefaultManifest, and it instead uses the vanilla manfiest for all the TypedEntities. This ultimately causes
     *   the DM.LoadRequest() for the ShadowHawkDLC in AssetUnlocks::TrySHDUnlockAndRefresh() to use the Vanilla filePaths, instead of the
     *   ModTek modified ones.
     *
     *   This patch forces a reload of the TypedEntries at the earliest point where I could detect activity from harmony patches. I
     *     logged access from ResourceLocator.EntryByID to find that spot, which ends up being the FaderController initialization in UIManager.Awake().
     *     
     */
    [HarmonyPatch(typeof(UIManager), "Awake")]
    public static class UIManager_Awake
    {
        public static void Prefix()
        {
            DataManager dm = SceneSingletonBehavior<DataManagerUnityInstance>.Instance.DataManager;


            Stopwatch sw = new Stopwatch();
            sw.Start();
            if (ModTek.Config.EnableDebugLogging)
                RLog.LogWrite("Forcing refresh of BTRL Manifest...\n");

            Traverse.Create(dm.ResourceLocator).Property("manifest").SetValue(VersionManifestUtilities.LoadDefaultManifest());

            sw.Stop();
            if (ModTek.Config.EnableDebugLogging)
                RLog.LogWrite($"Refresh of BTRL manifest complete in {sw.ElapsedMilliSeconds}ms \n");

            sw.Restart();
            if (ModTek.Config.EnableDebugLogging)
                RLog.LogWrite("Forcing refresh of TypedEntities to prevent Shadowhawk DLC bug...\n");

            Traverse refreshTypedEntriesT = Traverse.Create(dm.ResourceLocator).Method("RefreshTypedEntries");
            refreshTypedEntriesT.GetValue();

            sw.Stop();
            if (ModTek.Config.EnableDebugLogging)
                RLog.LogWrite($"Forced refresh of TypedEntities complete in {sw.ElapsedMilliSeconds}ms\n");
        }
    }
}
