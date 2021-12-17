using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using BattleTech;
using Harmony;
using HBS.Util;
using Newtonsoft.Json;
using static ModTek.Features.Logging.MTLogger;

namespace ModTek.Misc
{
    internal static class ModTekCacheStorage
    {
        internal static void CleanModTekTempDir(DirectoryInfo baseDir)
        {
            if (!baseDir.Exists)
            {
                return;
            }

            foreach (var dir in baseDir.EnumerateDirectories())
            {
                CleanModTekTempDir(dir);
            }

            foreach (var file in baseDir.EnumerateFiles())
            {
                file.IsReadOnly = false;
                Log("delete file " + file.FullName);
                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            Log("delete directory " + baseDir.FullName);
            try
            {
                baseDir.Delete();
            }
            catch (Exception)
            {
                // ignored
            }
        }

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
