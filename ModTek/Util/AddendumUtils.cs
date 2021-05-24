using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BattleTech;
using BattleTech.Data;
using HBS.Util;
using ModTek.Mods;

namespace ModTek.Util
{
    internal static class AddendumUtils
    {
        public static bool LoadDataAddendum(DataAddendumEntry dataAddendumEntry, string modDefDirectory)
        {
            try
            {
                var type = typeof(FactionEnumeration).Assembly.GetType(dataAddendumEntry.Name);
                if (type == (Type) null)
                {
                    Logger.Log("\tError: Could not find DataAddendum class named " + dataAddendumEntry.Name);
                    return false;
                }
                else
                {
                    var property = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
                    if (property == (PropertyInfo) null)
                    {
                        Logger.Log("\tError: Could not find static method [Instance] on class named [" + dataAddendumEntry.Name + "]");
                        return false;
                    }
                    else
                    {
                        var bdataAddendum = property.GetValue((object) null);
                        var pCachedEnumerationValueList = type.BaseType.GetProperty("CachedEnumerationValueList");
                        if (pCachedEnumerationValueList == null)
                        {
                            Logger.Log("\tError: Class does not implement property CachedEnumerationValueList property on class named [" + dataAddendumEntry.Name + "]");
                            return false;
                        }

                        var f_enumerationValueList = type.BaseType.GetField("enumerationValueList", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (f_enumerationValueList == null)
                        {
                            Logger.Log("\tError: Class does not implement field enumerationValueList on class named [" + dataAddendumEntry.Name + "]");
                            return false;
                        }

                        var enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                        if (enumList == null)
                        {
                            Logger.Log("\tError: Can't get CachedEnumerationValueList from [" + dataAddendumEntry.Name + "]");
                            return false;
                        }

                        Logger.Log("\tCurrent values [" + dataAddendumEntry.Name + "]");
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
                            Logger.Log("\t\t[" + val.Name + ":" + val.ID + "]");
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
                            Logger.Log("\tError: Class does not implement method pRefreshStaticData property on class named [" + dataAddendumEntry.Name + "]");
                            return false;
                        }

                        var jdataAddEnum = bdataAddendum as IJsonTemplated;
                        if (jdataAddEnum == null)
                        {
                            Logger.Log("\tError: not IJsonTemplated [" + dataAddendumEntry.Name + "]");
                            return false;
                        }

                        var fileData = File.ReadAllText(Path.Combine(modDefDirectory, dataAddendumEntry.Path));
                        jdataAddEnum.FromJSON(fileData);
                        enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                        if (enumList == null)
                        {
                            Logger.Log("\tError: Can't get CachedEnumerationValueList from [" + dataAddendumEntry.Name + "]");
                            return false;
                        }
                        else
                        {
                            var needFlush = false;
                            Logger.Log("\tLoading values [" + dataAddendumEntry.Name + "] from " + dataAddendumEntry.Path);
                            for (var index = 0; index < enumList.Count; ++index)
                            {
                                var val = enumList[index] as EnumValue;
                                if (val == null)
                                {
                                    continue;
                                }

                                ;
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
                                            Logger.Log("\tError value with same id:" + val.ID + " but different name " + ids[val.ID] + " already exist. Value: " + val.Name + " will not be added");
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

                                ;
                                if (val.GetType() == typeof(FactionValue))
                                {
                                    MetadataDatabase.Instance.InsertOrUpdateFactionValue(val as FactionValue);
                                    Logger.Log("\t\tAddind FactionValue to db [" + val.Name + ":" + val.ID + "]");
                                    needFlush = true;
                                }
                                else if (val.GetType() == typeof(WeaponCategoryValue))
                                {
                                    MetadataDatabase.Instance.InsertOrUpdateWeaponCategoryValue(val as WeaponCategoryValue);
                                    Logger.Log("\t\tAddind WeaponCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                                    needFlush = true;
                                }
                                else if (val.GetType() == typeof(AmmoCategoryValue))
                                {
                                    MetadataDatabase.Instance.InsertOrUpdateAmmoCategoryValue(val as AmmoCategoryValue);
                                    Logger.Log("\t\tAddind AmmoCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                                    needFlush = true;
                                }
                                else if (val.GetType() == typeof(ContractTypeValue))
                                {
                                    MetadataDatabase.Instance.InsertOrUpdateContractTypeValue(val as ContractTypeValue);
                                    Logger.Log("\t\tAddind ContractTypeValue to db [" + val.Name + ":" + val.ID + "]");
                                    needFlush = true;
                                }
                                else if (val.GetType() == typeof(ShipUpgradeCategoryValue))
                                {
                                    MetadataDatabase.Instance.InsertOrUpdateEnumValue((EnumValue) val, "ShipUpgradeCategory", true);
                                    Logger.Log("\t\tAddind ShipUpgradeCategoryValue to db [" + val.Name + ":" + val.ID + "]");
                                    needFlush = true;
                                }
                                else
                                {
                                    Logger.Log((string) "\t\tUnknown enum type");
                                    break;
                                }
                            }

                            if (needFlush)
                            {
                                Logger.Log("\tLog: DataAddendum successfully loaded name[" + dataAddendumEntry.Name + "] path[" + dataAddendumEntry.Path + "]");
                                pRefreshStaticData.Invoke(
                                    bdataAddendum,
                                    new object[]
                                    {
                                    }
                                );
                                f_enumerationValueList.SetValue(bdataAddendum, null);
                                enumList = pCachedEnumerationValueList.GetValue(bdataAddendum, null) as IList;
                                Logger.Log("\tUpdated values [" + dataAddendumEntry.Name + "]");
                                for (var index = 0; index < enumList.Count; ++index)
                                {
                                    var val = enumList[index] as EnumValue;
                                    if (val == null)
                                    {
                                        continue;
                                    }

                                    ;
                                    Logger.Log("\t\t[" + val.Name + ":" + val.ID + "]");
                                }
                            }

                            return needFlush;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("\tException: Exception caught while processing DataAddendum [" + dataAddendumEntry.Name + "]");
                Logger.Log(ex.ToString());
                return false;
            }
        }
    }
}
