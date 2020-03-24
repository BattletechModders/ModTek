using BattleTech.Data;
using BattleTech.UI;
using Harmony;
using HBS;
using ModTek.RuntimeLog;

namespace ModTek.Patches
{
    [HarmonyPatch(typeof(UIManager), "Awake")]
    public static class UIManager_Awake
    {
        public static void Prefix()
        {
            DataManager dm = SceneSingletonBehavior<DataManagerUnityInstance>.Instance.DataManager;
            Traverse refreshTypedEntriesT = Traverse.Create(dm.ResourceLocator).Method("RefreshTypedEntries");
            RuntimeLog.RLog.LogWrite("FORCING REFRESH OF TYPED ENTRIES FROM UIMANAGER_AWAKE...");
            refreshTypedEntriesT.GetValue();
            RLog.LogWrite(" DONE");
        }
    }
}
