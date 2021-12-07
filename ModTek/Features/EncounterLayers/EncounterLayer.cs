using Newtonsoft.Json;

namespace ModTek.Features.EncounterLayers
{
    [JsonObject]
    internal class EncounterLayer
    {
        [JsonProperty("EncounterLayerID")]
        public string EncounterLayerID { get; set; }

        [JsonProperty("MapID")]
        public string MapID { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("FriendlyName")]
        public string FriendlyName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("BattleValue")]
        public string BattleValue { get; set; }

        [JsonProperty("ContractTypeID")]
        public string ContractTypeID { get; set; }

        [JsonProperty("EncounterLayerGUID")]
        public string EncounterLayerGUID { get; set; }

        [JsonProperty("TagSetID")]
        public string TagSetID { get; set; }

        [JsonProperty("IncludeInBuild")]
        public string IncludeInBuild { get; set; }

        public override string ToString()
        {
            return $"EncounterLayer => encounterLayerID: {EncounterLayerID}  mapID: {MapID}  friendlyName: {FriendlyName}  description: {Description}  battleValue: {BattleValue} contractTypeID: {ContractTypeID} encounterLayerGUID: {EncounterLayerGUID} tagSetID {TagSetID} includeInBuild: {IncludeInBuild}";
        }
    }
}