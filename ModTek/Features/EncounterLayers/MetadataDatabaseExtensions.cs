using System.Linq;
using BattleTech.Data;

namespace ModTek.Features.EncounterLayers
{
    internal static class MetadataDatabaseExtensions
    {
        public static EncounterLayer_MDD InsertOrUpdateEncounterLayer(this MetadataDatabase mdd, EncounterLayer encounterLayer)
        {
            mdd.Execute("INSERT OR REPLACE INTO EncounterLayer (EncounterLayerID, MapID, Name, FriendlyName, Description, BattleValue, ContractTypeID, EncounterLayerGUID, TagSetID, IncludeInBuild) values(@EncounterLayerID, @MapID, @Name, @FriendlyName, @Description, @BattleValue, @ContractTypeID, @EncounterLayerGUID, @TagSetID, @IncludeInBuild)", new
            {
                EncounterLayerID = encounterLayer.EncounterLayerID,
                MapID = encounterLayer.MapID,
                Name = encounterLayer.Name,
                FriendlyName = encounterLayer.FriendlyName,
                Description = encounterLayer.Description,
                BattleValue = encounterLayer.BattleValue,
                ContractTypeID = encounterLayer.ContractTypeID,
                EncounterLayerGUID = encounterLayer.EncounterLayerGUID,
                TagSetID = encounterLayer.TagSetID,
                IncludeInBuild = encounterLayer.IncludeInBuild
            }, null, null, null);
            return mdd.SelectEncounterLayerByID(encounterLayer.EncounterLayerID);
        }

        public static EncounterLayer_MDD SelectEncounterLayerByID(this MetadataDatabase mdd, string encounterLayerId)
        {
            return mdd.Query<EncounterLayer_MDD>("SELECT * FROM EncounterLayer WHERE EncounterLayerID=@encounterLayerId", new
            {
                EncounterLayerID = encounterLayerId
            }, null, true, null, null).FirstOrDefault<EncounterLayer_MDD>();
        }
    }
}
