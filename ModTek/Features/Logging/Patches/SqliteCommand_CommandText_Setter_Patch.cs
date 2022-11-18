using System;
using System.Diagnostics;
using Harmony;
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
        Log.Debugger.Debug?.Log("A SQL query was initialized: " + cmd?.CommandText + Environment.NewLine + new StackTrace(1));
    }
}