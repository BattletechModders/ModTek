using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Harmony;
using HBS.Util;
using Newtonsoft.Json.Linq;

namespace ModTek.Util
{
    internal class JObjectCache
    {
        private static Dictionary<string, JObject> cachedJObjects = new();

        internal static void Clear()
        {
            cachedJObjects = null;
        }

        internal static JObject ParseGameJSONFile(string path)
        {
            if (cachedJObjects.ContainsKey(path))
            {
                return cachedJObjects[path];
            }

            // because StripHBSCommentsFromJSON is private, use Harmony to call the method
            var commentsStripped = Traverse.Create(typeof(JSONSerializationUtility)).Method("StripHBSCommentsFromJSON", File.ReadAllText(path)).GetValue<string>();
            if (commentsStripped == null)
            {
                throw new Exception("StripHBSCommentsFromJSON returned null.");
            }

            // add missing commas, this only fixes if there is a newline
            var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
            var commasAdded = rgx.Replace(commentsStripped, "$1,\n$2");

            cachedJObjects[path] = JObject.Parse(commasAdded);
            return cachedJObjects[path];
        }
    }
}
