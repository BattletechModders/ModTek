using System;
using BattleTech;
using BattleTech.Data;
using Harmony;
using ModTek.Features.Manifest.Mods;

namespace ModTek.Features.LoadAtBattleStart.Patches
{
    [HarmonyPatch(typeof(Contract), "RequestConversations")]
    public static class Contract_RequestConversations_Patch
    {
        public static void Postfix(Contract __instance, LoadRequest loadRequest)
        {
            try
            {
                foreach(var modDef in ModDefsDatabase.ModDefs)
                {
                    foreach(var resToRequest in modDef.Value.requestAtBattleStarts)
                    {
                        if(__instance.DataManager.Exists(resToRequest.Type, resToRequest.Id))
                        {
                            continue;
                        }
                        if(__instance.DataManager.ResourceLocator.EntryByID(resToRequest.Id, resToRequest.Type) == null) {
                            Log.Main.Warning?.Log($"Absent in data manager {resToRequest.Id}:{resToRequest.Type}");
                            continue;
                        }
                        Log.Main.Info?.Log($"Requesting {resToRequest.Id}:{resToRequest.Type}");
                        loadRequest.AddBlindLoadRequest(resToRequest.Type, resToRequest.Id);
                    }
                }
            }
            catch(Exception e)
            {
                Log.Main.Error?.Log(e);
            }
        }
    }
}
