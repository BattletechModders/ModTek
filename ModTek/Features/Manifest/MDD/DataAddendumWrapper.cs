using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BattleTech;
using HBS.Util;

namespace ModTek.Features.Manifest.MDD
{
    internal class DataAddendumWrapper
    {
        private readonly object enumeration;
        private readonly IJsonTemplated enumerationAsIJsonTemplated;
        private readonly PropertyInfo pCachedEnumerationValueList;
        private readonly MethodInfo mRefreshStaticData;
        private readonly FieldInfo fEnumerationValueList;

        internal DataAddendumWrapper(string enumerationTypeByName)
        {
            var type = typeof(FactionEnumeration).Assembly.GetType(enumerationTypeByName);
            if (type == null)
            {
                throw new Exception($"Could not find DataAddendum class");
            }

            var pInstance = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty);
            if (pInstance == null)
            {
                throw new Exception($"Could not find static method [Instance]");
            }

            // ReSharper disable once PossibleNullReferenceException
            pCachedEnumerationValueList = type.BaseType.GetProperty("CachedEnumerationValueList");
            if (pCachedEnumerationValueList == null)
            {
                throw new Exception($"Class does not implement property CachedEnumerationValueList property");
            }

            fEnumerationValueList = type.BaseType.GetField("enumerationValueList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fEnumerationValueList == null)
            {
                throw new Exception($"Class does not implement field enumerationValueList");
            }

            mRefreshStaticData = type.GetMethod("RefreshStaticData");
            if (mRefreshStaticData == null)
            {
                throw new Exception($"Class does not implement method pRefreshStaticData property");
            }

            enumeration = pInstance.GetValue(null);

            enumerationAsIJsonTemplated = enumeration as IJsonTemplated;
            if (enumerationAsIJsonTemplated == null)
            {
                throw new Exception($"Not IJsonTemplated");
            }
        }

        internal void RefreshStaticData()
        {
            mRefreshStaticData.Invoke(enumeration, new object[] { });
            fEnumerationValueList.SetValue(enumeration, null); // force reload from db next cache access
        }

        internal IEnumerable<EnumValue> GetCachedEnumerationValueList()
        {
            return GetCachedEnumerationValueListRaw().Cast<EnumValue>();
        }

        private IList GetCachedEnumerationValueListRaw()
        {
            return (IList)pCachedEnumerationValueList.GetValue(enumeration, null);
        }

        internal void FromJSON(string json)
        {
            enumerationAsIJsonTemplated.FromJSON(json);
            if (GetCachedEnumerationValueList() == null)
            {
                throw new Exception($"enumerationValueList is not set after loading JSON");
            }
        }
    }
}
