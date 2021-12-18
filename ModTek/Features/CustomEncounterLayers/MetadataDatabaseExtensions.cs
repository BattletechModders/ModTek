using System.Linq;
using BattleTech.Data;

namespace ModTek.Features.CustomEncounterLayers
{
    internal static class MetadataDatabaseExtensions
    {
        public static EncounterLayer_MDD UpdateEncounterLayer(this MetadataDatabase mdd, EncounterLayer encounterLayer)
        {
            mdd.Execute("INSERT OR REPLACE INTO EncounterLayer (EncounterLayerID, MapID, Name, FriendlyName, Description, BattleValue, ContractTypeID, EncounterLayerGUID, TagSetID, IncludeInBuild) values(@EncounterLayerID, @MapID, @Name, @FriendlyName, @Description, @BattleValue, @ContractTypeID, @EncounterLayerGUID, @TagSetID, @IncludeInBuild)", new
            {
                encounterLayer.EncounterLayerID,
                encounterLayer.MapID,
                encounterLayer.Name,
                encounterLayer.FriendlyName,
                encounterLayer.Description,
                encounterLayer.BattleValue,
                encounterLayer.ContractTypeID,
                encounterLayer.EncounterLayerGUID,
                encounterLayer.TagSetID,
                encounterLayer.IncludeInBuild
            });
            return mdd.SelectEncounterLayerByID(encounterLayer.EncounterLayerID);
        }

        public static EncounterLayer_MDD SelectEncounterLayerByID(this MetadataDatabase mdd, string encounterLayerId)
        {
            return mdd.Query<EncounterLayer_MDD>("SELECT * FROM EncounterLayer WHERE EncounterLayerID=@encounterLayerId", new
            {
                EncounterLayerID = encounterLayerId
            }).FirstOrDefault();
        }
    }
}
