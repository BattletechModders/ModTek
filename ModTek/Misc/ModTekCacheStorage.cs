using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
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

        // ReSharper disable once ConvertToConstant.Local
        // wanted to do compression mainly to force all normal cache data through here
        // we need zipfile support to allow zipped mods
        private static bool compress => ModTek.Config.UseFileCompression;
        private static string CompressedPath(string path)
        {
            return compress ? path + ".gz" : path;
        }
        private static Stream CompressedWriteStream(Stream stream)
        {
            return compress ? new GZipStream(stream, CompressionLevel.Fastest) : stream;
        }
        private static Stream CompressedReadStream(Stream stream)
        {
            return compress ? new GZipStream(stream, CompressionMode.Decompress) : stream;
        }

        internal static bool CompressedExists(string path)
        {
            return File.Exists(CompressedPath(path));
        }

        internal static void CompressedCSVWriteTo<T>
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
                if (compress)
                {
                    using (var f = new FileStream(CompressedPath(path), FileMode.Create))
                    {
                        using (var g = CompressedWriteStream(f))
                        {
                            using(var s = new StreamWriter(g))
                            {
                                s.NewLine = "\n";
                                c.Close();
                                File.Delete(path);
                                Traverse.Create(c).Field<TextWriter>("writer").Value = s;
                                process(c);
                            }
                        }
                    }
                }
                else
                {
                    process(c);
                }
            }
        }

        public static void CompressedStringWriteTo(string path, string content)
        {
            using (var f = new FileStream(CompressedPath(path), FileMode.Create))
            {
                using (var g = CompressedWriteStream(f))
                {
                    using (var s = new StreamWriter(g))
                    {
                        s.Write(content);
                    }
                }
            }
        }

        public static string CompressedStringReadFrom(string path)
        {
            using (var f = new FileStream(CompressedPath(path), FileMode.Open))
            {
                using (var g = CompressedReadStream(f))
                {
                    using (var s = new StreamReader(g))
                    {
                        return s.ReadToEnd();
                    }
                }
            }
        }

        internal static void CompressedWriteTo(object obj, string path)
        {
            using (var f = new FileStream(CompressedPath(path), FileMode.Create))
            {
                using (var g = CompressedWriteStream(f))
                {
                    using (var s = new StreamWriter(g))
                    {
                        using (var j = new JsonTextWriter(s))
                        {
                            var ser = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
                            ser.Serialize(j, obj);
                            j.Flush();
                        }
                    }
                }
            }
        }

        internal static T CompressedReadFrom<T>(string path)
        {
            using (var f = new FileStream(CompressedPath(path), FileMode.Open))
            {
                using (var g = CompressedReadStream(f))
                {
                    using (var s = new StreamReader(g))
                    {
                        using (var j = new JsonTextReader(s))
                        {
                            var ser = new JsonSerializer();
                            return ser.Deserialize<T>(j);
                        }
                    }
                }
            }
        }
    }
}
