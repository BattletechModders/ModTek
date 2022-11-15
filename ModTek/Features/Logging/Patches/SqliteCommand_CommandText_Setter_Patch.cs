using System;
using Harmony;
using HBS.Logging;
using Mono.Data.Sqlite;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(SqliteCommand), nameof(SqliteCommand.CommandText), MethodType.Setter)]
internal static class SqliteCommand_CommandText_Setter_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.Logging.LogSqlQueryInitializations;
    }

    public static void Postfix(SqliteCommand __instance)
    {
        var cmd = __instance;
        var st = new System.Diagnostics.StackTrace(1).ToString();
        LoggingFeature.LogAtLevel(
            "Debugger",
            LogLevel.Debug,
            "A SQL query was initialized: " + cmd?.CommandText + Environment.NewLine + st,
            null,
            null
        );
    }
}