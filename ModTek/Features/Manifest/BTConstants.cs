using System;
using BattleTech;

namespace ModTek.Features.Manifest
{
    internal static class BTConstants
    {
        internal static readonly string[] HBSContentNames =
        {
            ShadowHawkDlcContentName,
            FlashPointContentName,
            UrbanWarfareContentName,
            HeavyMetalContentName
        };

        // probably not complete
        internal const string ShadowHawkDlcContentName = "shadowhawkdlc";
        internal const string FlashPointContentName = "flashpoint";
        internal const string UrbanWarfareContentName = "urbanwarfare";
        internal const string HeavyMetalContentName = "heavymetal";

        internal const string CustomType_AdvancedJSONMerge = "AdvancedJSONMerge";
        internal const string CustomType_DebugSettings = "DebugSettings";
        internal const string CustomType_GameTip = "GameTip";
        internal const string CustomType_SoundBankDef = "SoundBankDef";
        internal const string CustomType_SoundBank = "SoundBank";
        internal const string CustomType_Tag = "CustomTag";
        internal const string CustomType_TagSet = "CustomTagSet";
        internal const string CustomType_Video = "Video";

        internal static readonly string[] MODTEK_TYPES =
        {
            CustomType_Video,
            CustomType_AdvancedJSONMerge,
            CustomType_GameTip,
            CustomType_DebugSettings,
            CustomType_SoundBank,
            CustomType_SoundBankDef,
            CustomType_Tag,
            CustomType_TagSet
        };

        internal static readonly string[] VANILLA_TYPES = Enum.GetNames(typeof(BattleTechResourceType));

        internal static bool ResourceType(string Type, out BattleTechResourceType type)
        {
            if (Type == null)
            {
                type = BattleTechResourceType.AbilityDef;
                return false;
            }
            return Enum.TryParse(Type, out type);
        }

        internal static readonly BattleTechResourceType[] MDDTypes =
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

        internal static readonly BattleTechResourceType[] StringTypes =
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
