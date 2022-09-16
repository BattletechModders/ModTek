using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HBS.Util;
using ModTek.Features.Logging;
using Newtonsoft.Json;

namespace ModTek.Misc
{
    internal static class ModTekCacheStorage
    {
        internal static void CSVWriteTo<T>
        (
            string path,
            IEnumerable<T> enumerable,
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        )
        {
            void process(CSVWriter csvWriter)
            {
                CSVTemplatingUtility.WriteFieldNamesToRow<T>(csvWriter, flags);
                csvWriter.AdvanceRow();
                foreach (var entry in enumerable)
                {
                    CSVSerializationUtility.WriteFieldValuesToRow(entry, csvWriter, flags);
                    csvWriter.AdvanceRow();
                }
            }

            using(var c = new CSVWriter(path))
            {
                process(c);
            }
        }

        internal static void WriteTo(object obj, string path)
        {
            using (var f = new FileStream(path, FileMode.Create))
            using (var s = new StreamWriter(f))
            using (var j = new JsonTextWriter(s))
            {
                var ser = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
                ser.Serialize(j, obj);
                j.Flush();
            }
        }

        internal static T ReadFrom<T>(string path)
        {
            using (var f = new FileStream(path, FileMode.Open))
            using (var s = new StreamReader(f))
            using (var j = new JsonTextReader(s))
            {
                var ser = new JsonSerializer();
                return ser.Deserialize<T>(j);
            }
        }
    }
}
