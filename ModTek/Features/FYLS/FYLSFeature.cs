using System.Linq;
using System.Text.RegularExpressions;

namespace ModTek.Features.FYLS
{
    internal static class FYLSFeature
    {
        internal static FYLSSettings ModSettings => ModTek.Config.FYLSSettings;
        internal static Regex LogPrefixesMatcher;

        public static void Init()
        {
            var prefixes = ModSettings.PrefixesToIgnore;
            if (prefixes.Any())
            {
                var ignoredPrefixesPattern = $"^(?:{string.Join("|", prefixes.Select(Regex.Escape))})";
                LogPrefixesMatcher = new Regex(ignoredPrefixesPattern);
            }
            else
            {
                LogPrefixesMatcher = new Regex("^$");
            }

            BTLogger.InitDebugFiles();
        }
    }
}

