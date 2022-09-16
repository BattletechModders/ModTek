using System;
using System.Collections.Generic;
using System.IO;
using BattleTech;
using BattleTech.Data;
using ModTek.Features.Logging;
using ModTek.Features.Manifest.Mods;
using UnityEngine;

namespace ModTek.Features.Manifest.MDD
{
    internal static class AddendumUtils
    {
        public static void ProcessDataAddendums()
        {
            foreach (var modDef in ModDefsDatabase.ModsInLoadOrder())
            {
                MTLogger.Info.LogIf(modDef.DataAddendumEntries.Count > 0, $"{modDef.QuotedName} DataAddendum:");
                foreach (var dataAddendumEntry in modDef.DataAddendumEntries)
                {
                    try
                    {
                        MTLogger.Info.Log($"\tLoading DataAddendum {dataAddendumEntry.Name}");
                        var filePath = Path.Combine(modDef.Directory, dataAddendumEntry.Path);
                        LoadDataAddendum(dataAddendumEntry.Name, filePath, ref MDDBCache.SaveMDDB);
                        MTLogger.Info.Log($"\tSuccessfully loaded DataAddendum {dataAddendumEntry.Name}");
                    }
                    catch (Exception ex)
                    {
                        MTLogger.Error.Log($"\tException caught while processing DataAddendum: {dataAddendumEntry.Name}", ex);
                    }
                }
            }
        }

        private static void LoadDataAddendum(string enumName, string filePath, ref bool hasMddbChanges)
        {
            var wrapper = new DataAddendumWrapper(enumName);

            MTLogger.Info.Log($"\tCurrent values");
            var maxIndex = 0;
            var names = new Dictionary<string, int>();
            var ids = new Dictionary<int, string>();
            foreach (var val in wrapper.GetCachedEnumerationValueList())
            {
                MTLogger.Info.Log($"\t\t{val.Name} ({val.ID})");
                names[val.Name] = val.ID;
                ids[val.ID] = val.Name;
                maxIndex = Mathf.Max(maxIndex, val.ID);
            }

            var fileData = File.ReadAllText(filePath);
            wrapper.FromJSON(fileData);

            var needFlush = false;
            MTLogger.Info.Log($"\tLoading values");
            foreach (var val in wrapper.GetCachedEnumerationValueList())
            {
                // we merge based on the name
                // otherwise we make sure to not replace existing IDs
                // ID=0 means to auto-assign an ID
                if (names.ContainsKey(val.Name))
                {
                    val.ID = names[val.Name];
                }
                else
                {
                    if (val.ID == 0)
                    {
                        val.ID = ++maxIndex;
                    }
                    else if (ids.ContainsKey(val.ID))
                    {
                        MTLogger.Warning.Log($"\tValue with same id:{val.ID} but different name {ids[val.ID]} already exist. Value: {val.Name} will not be added");
                        continue;
                    }

                    names.Add(val.Name, val.ID);
                    ids.Add(val.ID, val.Name);
                    maxIndex = Mathf.Max(maxIndex, val.ID);
                }

                if (InsertOrUpdateEnumValueInMDDB(val))
                {
                    needFlush = true;
                }
            }

            if (!needFlush)
            {
                return;
            }

            hasMddbChanges = true;
            wrapper.RefreshStaticData();
            MTLogger.Info.Log($"\tUpdated values [{enumName}]");
            foreach (var val in wrapper.GetCachedEnumerationValueList())
            {
                MTLogger.Info.Log($"\t\t{val.Name} ({val.ID})");
            }
        }

        private static bool InsertOrUpdateEnumValueInMDDB(EnumValue val)
        {
            switch (val)
            {
                case AmmoCategoryValue acv:
                    MetadataDatabase.Instance.InsertOrUpdateAmmoCategoryValue(acv);
                    break;
                case AmmunitionTypeValue _:
                    MetadataDatabase.Instance.InsertOrUpdateEnumValue(val, "AmmunitionType", true);
                    break;
                case ContractTypeValue ctv:
                    MetadataDatabase.Instance.InsertOrUpdateContractTypeValue(ctv);
                    break;
                case FactionValue fv:
                    MetadataDatabase.Instance.InsertOrUpdateFactionValue(fv);
                    break;
                case ShipUpgradeCategoryValue _:
                    MetadataDatabase.Instance.InsertOrUpdateEnumValue(val, "ShipUpgradeCategory", true);
                    break;
                case WeaponCategoryValue wcv:
                    MetadataDatabase.Instance.InsertOrUpdateWeaponCategoryValue(wcv);
                    break;
                default:
                    MTLogger.Warning.Log($"\t\tEnumValue type {val.GetType().Name} not known to be in MDDB");
                    return false;
            }
            MTLogger.Info.Log($"\t\tAdded or updated enum value: {val.Name} ({val.ID})");
            return true;
        }
    }
}
