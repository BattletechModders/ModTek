using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Harmony;
using HBS.Util;
using ModTek.Logging;
using Newtonsoft.Json.Linq;

namespace ModTek.Util
{
    internal static class JsonUtils
    {
        internal static JObject ParseGameJSONFile(string path, bool log = false)
        {
            var content = File.ReadAllText(path);
            return ParseGameJSON(content, log);
        }

        internal static JObject ParseGameJSON(string content, bool log = false)
        {
            Logger.LogIf(log,"content: " + content);

            var commentsStripped = StripHBSCommentsFromJSON(content);
            Logger.LogIf(log, "commentsStripped: " + commentsStripped);

            var commasAdded = FixHBSJsonCommas(commentsStripped);
            Logger.LogIf(log,"commasAdded: " + commasAdded);

            return JObject.Parse(commasAdded);
        }

        private static readonly MethodBase StripHBSCommentsFromJSONTraverse = AccessTools.Method(typeof(JSONSerializationUtility), "StripHBSCommentsFromJSON");
        private static string StripHBSCommentsFromJSON(string json)
        {
            return (string) StripHBSCommentsFromJSONTraverse.Invoke(null, new object[]{json});
        }

        private static string FixHBSJsonCommas(string json)
        {
            // add missing commas, this only fixes if there is a newline
            var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
            return rgx.Replace(json, "$1,\n$2");
        }
    }
}
