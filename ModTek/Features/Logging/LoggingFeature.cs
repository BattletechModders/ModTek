using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ModTek.Misc;

namespace ModTek.Features.Logging
{
    internal static class LoggingFeature
    {
        internal static LoggingSettings Settings => ModTek.Config.Logging;
        internal static Regex LogPrefixesMatcher;

        // TODO integrate all loggers, find nice interface and enforce use everywhere.

        internal static void InitMTLogger()
        {
            MTLogger.LogInit();
        }

        internal static void InitRTLogger()
        {
            RLog.InitLog(FilePaths.TempModTekDirectory, true);
            RLog.M.TWL(0, "Init ModTek version " + Assembly.GetExecutingAssembly().GetName().Version);
        }

        internal static void InitBTLogger()
        {
            var prefixes = Settings.PrefixesToIgnore;
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

