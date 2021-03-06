using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using HBS.Util;
using ModTek.Features.Manifest.Mods;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Features.Manifest.MDD
{
    internal static class AddendumUtils
    {
        public static bool LoadDataAddendum(DataAddendumEntry dataAddendumEntry, string modDefDirectory)
        {
            try
            {
                var type = typeof(FactionEnumeration).Assembly.GetType(dataAddendumEntry.Name);
                if (type == null)
                {
                    Log("\tError: Could not find DataAddendum class named " + dataAddendumEntry.Name);
                    return false;
                }

                var property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
                if (property == null)
                {
                    Log("\tError: Could not find static method [Instance] on class named [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                var bdataAddendum = property.GetValue(null);
                var pCachedEnumerationValueList = type.BaseType.GetProperty("CachedEnumerationValueList");
                if (pCachedEnumerationValueList == null)
                {
                    Log("\tError: Class does not implement property CachedEnumerationValueList property on class named [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                var f_enumerationValueList = type.BaseType.GetField("enumerationValueList", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f_enumerationValueList == null)
                {
                    Log("\tError: Class does not implement field enumerationValueList on class named [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                var enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                if (enumList == null)
                {
                    Log("\tError: Can't get CachedEnumerationValueList from [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                Log("\tCurrent values [" + dataAddendumEntry.Name + "]");
                var maxIndex = 0;
                var names = new Dictionary<string, int>();
                var ids = new Dictionary<int, string>();
                for (var index = 0; index < enumList.Count; ++index)
                {
                    var val = enumList[index] as EnumValue;
                    if (val == null)
                    {
                        continue;
                    }

                    ;
                    Log("\t\t[" + val.Name + ":" + val.ID + "]");
                    if (maxIndex < val.ID)
                    {
                        maxIndex = val.ID;
                    }

                    ;
                    if (names.ContainsKey(val.Name) == false)
                    {
                        names.Add(val.Name, val.ID);
                    }
                    else
                    {
                        names[val.Name] = val.ID;
                    }

                    if (ids.ContainsKey(val.ID) == false)
                    {
                        ids.Add(val.ID, val.Name);
                    }
                    else
                    {
                        ids[val.ID] = val.Name;
                    }
                }

                var pRefreshStaticData = type.GetMethod("RefreshStaticData");
                if (pRefreshStaticData == null)
                {
                    Log("\tError: Class does not implement method pRefreshStaticData property on class named [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                var jdataAddEnum = bdataAddendum as IJsonTemplated;
                if (jdataAddEnum == null)
                {
                    Log("\tError: not IJsonTemplated [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                var fileData = File.ReadAllText(Path.Combine(modDefDirectory, dataAddendumEntry.Path));
                jdataAddEnum.FromJSON(fileData);
                enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                if (enumList == null)
                {
                    Log("\tError: Can't get CachedEnumerationValueList from [" + dataAddendumEntry.Name + "]");
                    return false;
                }

                var needFlush = false;
                Log("\tLoading values [" + dataAddendumEntry.Name + "] from " + dataAddendumEntry.Path);
                for (var index = 0; index < enumList.Count; ++index)
                {
                    var val = enumList[index] as EnumValue;
                    if (val == null)
                    {
                        continue;
                    }

                    if (names.ContainsKey(val.Name))
                    {
                        val.ID = names[val.Name];
                    }
                    else
                    {
                        if (ids.ContainsKey(val.ID))
                        {
                            if (val.ID == 0)
                            {
                                val.ID = maxIndex + 1;
                                ++maxIndex;
                                names.Add(val.Name, val.ID);
                                ids.Add(val.ID, val.Name);
                            }
                            else
                            {
                                Log("\tError value with same id:" + val.ID + " but different name " + ids[val.ID] + " already exist. Value: " + val.Name + " will not be added");
                                continue;
                            }
                        }
                        else
                        {
                            names.Add(val.Name, val.ID);
                            ids.Add(val.ID, val.Name);
                            if (val.ID > maxIndex)
                            {
                                maxIndex = val.ID;
                            }
                        }
                    }

                    if (val.GetType() == typeof(FactionValue))
                    {
                        MetadataDatabase.Instance.InsertOrUpdateFactionValue(val as FactionValue);
                        Log("\t\tAddind FactionValue to db [" + val.Name + ":" + val.ID + "]");
                        needFlush = true;
                    }
                    else if (val.GetType() == typeof(WeaponCategoryValue))
                    {
                        MetadataDatabase.Instance.InsertOrUpdateWeaponCategoryValue(val as WeaponCategoryValue);
                        Log("\t\tAddind WeaponCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                        needFlush = true;
                    }
                    else if (val.GetType() == typeof(AmmoCategoryValue))
                    {
                        MetadataDatabase.Instance.InsertOrUpdateAmmoCategoryValue(val as AmmoCategoryValue);
                        Log("\t\tAddind AmmoCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                        needFlush = true;
                    }
                    else if (val.GetType() == typeof(ContractTypeValue))
                    {
                        MetadataDatabase.Instance.InsertOrUpdateContractTypeValue(val as ContractTypeValue);
                        Log("\t\tAddind ContractTypeValue to db [" + val.Name + ":" + val.ID + "]");
                        needFlush = true;
                    }
                    else if (val.GetType() == typeof(ShipUpgradeCategoryValue))
                    {
                        MetadataDatabase.Instance.InsertOrUpdateEnumValue(val, "ShipUpgradeCategory", true);
                        Log("\t\tAddind ShipUpgradeCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                        needFlush = true;
                    }
                    else
                    {
                        Log("\t\tUnknown enum type");
                        break;
                    }
                }

                if (needFlush)
                {
                    Log("\tLog: DataAddendum successfully loaded name[" + dataAddendumEntry.Name + "] path[" + dataAddendumEntry.Path + "]");
                    pRefreshStaticData.Invoke(
                        bdataAddendum,
                        new object[]
                        {
                        }
                    );
                    f_enumerationValueList.SetValue(bdataAddendum, null);
                    enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                    Log("\tUpdated values [" + dataAddendumEntry.Name + "]");
                    for (var index = 0; index < enumList.Count; ++index)
                    {
                        var val = enumList[index] as EnumValue;
                        if (val == null)
                        {
                            continue;
                        }

                        Log("\t\t[" + val.Name + ":" + val.ID + "]");
                    }
                }

                return needFlush;
            }
            catch (Exception ex)
            {
                Log("\tException: Exception caught while processing DataAddendum [" + dataAddendumEntry.Name + "]");
                Log(ex.ToString());
                return false;
            }
        }
    }
}
