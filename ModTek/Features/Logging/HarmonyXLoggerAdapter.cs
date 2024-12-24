using HBS.Logging;
using ModTek.Public;
using Logger = HarmonyLib.Tools.Logger;

namespace ModTek.Features.Logging;

internal static class HarmonyXLoggerAdapter
{
    private static readonly ILog s_log = Log.HarmonyX.Log;
    internal static void Setup()
    {
        Logger.MessageReceived += (_, args) =>
        {
            var level = MapHarmonyLogChannelToHbsLogLevel(args.LogChannel);
            s_log.LogAtLevel(level, args.Message);
        };
    }

    private static LogLevel MapHarmonyLogChannelToHbsLogLevel(Logger.LogChannel channel)
    {
        return channel switch
        {
            Logger.LogChannel.Error => LogLevel.Error,
            Logger.LogChannel.Warn => LogLevel.Warning,
            Logger.LogChannel.Info => LogLevel.Log,
            Logger.LogChannel.Debug => LogLevel.Debug,
            Logger.LogChannel.IL => NullableLogger.TraceLogLevel,
            _ => LogLevel.Log
        };
    }
}
