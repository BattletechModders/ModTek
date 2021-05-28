using System;
using BattleTech;

namespace ModTek.Manifest
{
    internal static class BTResourceUtils
    {
        internal static BattleTechResourceType? ResourceType(string Type)
        {
            return Enum.TryParse<BattleTechResourceType>(Type, out var resType) ? resType : null;
        }

        internal static BattleTechResourceType[] MDDTypes =
        {
            BattleTechResourceType.ContractOverride,
            BattleTechResourceType.LanceDef,
            BattleTechResourceType.PilotDef,
            BattleTechResourceType.SimGameEventDef,
            BattleTechResourceType.MechDef,
            BattleTechResourceType.WeaponDef,
            BattleTechResourceType.TurretDef,
            BattleTechResourceType.VehicleDef,
            BattleTechResourceType.UpgradeDef
        };

        // from CustomComponents mod
        internal static BattleTechResourceType[] CCTypes =
        {
            BattleTechResourceType.HeatSinkDef,
            BattleTechResourceType.UpgradeDef,
            BattleTechResourceType.WeaponDef,
            BattleTechResourceType.AmmunitionBoxDef,
            BattleTechResourceType.JumpJetDef
        };

        internal static BattleTechResourceType[] StringTypes =
        {
            // one time
            BattleTechResourceType.ApplicationConstants,
            BattleTechResourceType.AudioConstants,
            BattleTechResourceType.BehaviorVariableScope,
            BattleTechResourceType.CombatGameConstants,
            BattleTechResourceType.MechStatisticsConstants,
            BattleTechResourceType.SimGameConstants,
            // simple text
            BattleTechResourceType.SimpleText,
            // json
            BattleTechResourceType.AbilityDef,
            BattleTechResourceType.AmmunitionBoxDef,
            BattleTechResourceType.AmmunitionDef,
            BattleTechResourceType.AudioEventDef,
            BattleTechResourceType.BackgroundDef,
            BattleTechResourceType.BackgroundQuestionDef,
            BattleTechResourceType.BaseDescriptionDef,
            BattleTechResourceType.BuildingDef,
            BattleTechResourceType.CastDef,
            BattleTechResourceType.ChassisDef,
            BattleTechResourceType.ContentPackDef,
            BattleTechResourceType.ContractOverride,
            BattleTechResourceType.ConversationContent,
            BattleTechResourceType.DesignMaskDef,
            BattleTechResourceType.DialogBucketDef,
            BattleTechResourceType.FactionDef,
            BattleTechResourceType.FlashpointDef,
            BattleTechResourceType.GenderedOptionsListDef,
            BattleTechResourceType.HardpointDataDef,
            BattleTechResourceType.HeatSinkDef,
            BattleTechResourceType.HeraldryDef,
            BattleTechResourceType.JumpJetDef,
            BattleTechResourceType.LanceDef,
            BattleTechResourceType.LifepathNodeDef,
            BattleTechResourceType.MechDef,
            BattleTechResourceType.SimGameMilestoneSet,
            BattleTechResourceType.MovementCapabilitiesDef,
            BattleTechResourceType.PathingCapabilitiesDef,
            BattleTechResourceType.PilotDef,
            BattleTechResourceType.PortraitSettings,
            BattleTechResourceType.RegionDef,
            BattleTechResourceType.ShipModuleUpgrade,
            BattleTechResourceType.ShopDef,
            BattleTechResourceType.SimGameDifficultySettingList,
            BattleTechResourceType.SimGameEventDef,
            BattleTechResourceType.SimGameMilestoneDef,
            BattleTechResourceType.SimGameStatDescDef,
            BattleTechResourceType.SimGameSubstitutionListDef,
            BattleTechResourceType.StarSystemDef,
            BattleTechResourceType.SystemModDef,
            BattleTechResourceType.TurretChassisDef,
            BattleTechResourceType.TurretDef,
            BattleTechResourceType.UpgradeDef,
            BattleTechResourceType.VehicleChassisDef,
            BattleTechResourceType.VehicleDef,
            BattleTechResourceType.WeaponDef
        };
    }
}
