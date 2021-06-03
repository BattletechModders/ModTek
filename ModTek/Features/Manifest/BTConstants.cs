using System;
using System.Linq;
using BattleTech;
using BattleTech.ModSupport.Utils;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomStreamingAssets;
using ModTek.Features.CustomTags;
using ModTek.Features.SoundBanks;

namespace ModTek.Features.Manifest
{
    internal static class BTConstants
    {
        // possibly not complete
        internal static readonly string[] HBSContentNames =
        {
            "shadowhawkdlc",
            "flashpoint",
            "urbanwarfare",
            "heavymetal"
        };

        internal const string CustomType_AdvancedJSONMerge = nameof(AdvancedJSONMerge);
        internal const string CustomType_SoundBankDef = nameof(SoundBankDef);
        internal const string CustomType_Tag = nameof(CustomTag);
        internal const string CustomType_TagSet = nameof(CustomTagSet);

        internal static readonly string[] PREDEFINED_TYPES = new []
        {
            CustomType_AdvancedJSONMerge,
            CustomType_SoundBankDef,
            CustomType_Tag,
            CustomType_TagSet
        }
            .Concat(Enum.GetNames(typeof(BattleTechResourceType)))
            .Concat(CustomStreamingAssetsFeature.CSATypeNames)
            .Concat(CustomResourcesFeature.CRTypeNames)
            .ToArray();

        internal static bool ResourceType(string Type, out BattleTechResourceType type)
        {
            if (Type == null)
            {
                type = BattleTechResourceType.AbilityDef;
                return false;
            }
            return Enum.TryParse(Type, out type);
        }

        internal static readonly BattleTechResourceType[] MDDBTypes =
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

        internal static readonly BattleTechResourceType[] StringOneTimeTypes =
        {
            BattleTechResourceType.ApplicationConstants,
            BattleTechResourceType.AudioConstants,
            BattleTechResourceType.BehaviorVariableScope,
            BattleTechResourceType.CombatGameConstants,
            BattleTechResourceType.MechStatisticsConstants,
            BattleTechResourceType.SimGameConstants,
        };

        internal static readonly BattleTechResourceType[] StringSimpleTextTypes =
        {
            BattleTechResourceType.SimpleText,
        };

        internal static readonly BattleTechResourceType[] StringJsonTypes =
        {
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
