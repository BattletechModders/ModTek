using System.Linq;
using System.Text.RegularExpressions;

namespace ModTek.Features.Logging
{
    internal static class FYLSFeature
    {
        internal static LoggingSettings ModSettings => ModTek.Config.Logging;
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

