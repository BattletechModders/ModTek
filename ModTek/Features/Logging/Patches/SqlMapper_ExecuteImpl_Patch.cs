using System.Diagnostics;
using Dapper;
using Newtonsoft.Json;

namespace ModTek.Features.Logging.Patches;

[HarmonyPatch(typeof(SqlMapper), "ExecuteImpl")]
internal static class SqlMapper_ExecuteImpl_Patch
{
    public static bool Prepare()
    {
        return ModTek.Enabled && ModTek.Config.Logging.LogSqlQueryExecutionsFromDapper;
    }

    public static void Prefix(ref CommandDefinition command)
    {
        var debug = Log.Debugger.Debug;
        if (debug == null)
        {
            return;
        }

        var parameters = command.Parameters;
        if (parameters == null)
        {
            return;
        }
        var serializedParameters = JsonConvert.SerializeObject(command.Parameters, Formatting.Indented);

        debug.Log(
            $"""
             SQL query being executed via Dapper
             CommandText: {command.CommandText}
             Parameters: {serializedParameters}
             Stacktrace: {new StackTrace(1)}
             """
        );
    }
}