using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using ModTek.Features.CustomResources;
using ModTek.Features.CustomStreamingAssets;

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

        internal static readonly string[] PREDEFINED_TYPES =
            Enum.GetNames(typeof(BattleTechResourceType))
            .Concat(Enum.GetNames(typeof(InternalCustomResourceType)))
            .Concat(Enum.GetNames(typeof(CustomStreamingAssetsType)))
            .Concat(Enum.GetNames(typeof(CustomType)))
            .ToArray();

        internal static readonly BattleTechResourceType[] VanillaMDDBTypes =
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

        internal static readonly HashSet<string> MDDBTypes =
            VanillaMDDBTypes.Select(x => x.ToString())
                .Union(new[]
                {
                    InternalCustomResourceType.EncounterLayer.ToString()
                })
                .ToHashSet();

        internal static bool BTResourceType(string Type, out BattleTechResourceType type)
        {
            return Enum.TryParse(Type, out type);
        }

        internal static bool ICResourceType(string Type, out InternalCustomResourceType type)
        {
            return Enum.TryParse(Type, out type);
        }

        internal static bool CSAssetsType(string Type, out CustomStreamingAssetsType type)
        {
            return Enum.TryParse(Type, out type);
        }

        internal static bool CType(string Type, out CustomType type)
        {
            return Enum.TryParse(Type, out type);
        }
    }
}
