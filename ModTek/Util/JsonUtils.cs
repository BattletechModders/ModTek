using System;
using System.Text.RegularExpressions;
using Harmony;
using HBS.Util;
using Newtonsoft.Json.Linq;

namespace ModTek.Util
{
    internal static class JsonUtils
    {
        internal static JObject ParseGameJSON(string content)
        {
            // because StripHBSCommentsFromJSON is private, use Harmony to call the method
            var commentsStripped = Traverse.Create(typeof(JSONSerializationUtility)).Method("StripHBSCommentsFromJSON", content).GetValue<string>();
            if (commentsStripped == null)
            {
                throw new Exception("StripHBSCommentsFromJSON returned null.");
            }

            // add missing commas, this only fixes if there is a newline
            var rgx = new Regex(@"(\]|\}|""|[A-Za-z0-9])\s*\n\s*(\[|\{|"")", RegexOptions.Singleline);
            var commasAdded = rgx.Replace(commentsStripped, "$1,\n$2");

            return JObject.Parse(commasAdded);
        }
    }
}
