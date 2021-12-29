using Newtonsoft.Json;

namespace ModTek.Features.Profiling
{
    internal class ModTekProfilerSettings
    {
        [JsonProperty]
        internal bool Enabled = true;
        [JsonProperty]
        internal readonly string Enabled_Description = $"Use the a custom made profiler to collect and display stats in log files.";

        [JsonProperty]
        internal float DumpWhenFrameTimeDeltaLargerThan = 1f/30;
        [JsonProperty]
        internal readonly string DumpWhenFrameTimeDeltaLargerThan_Description = $"Dump profiler stats if a frame takes longer than the specified amount (in seconds). Dumping happens regardless if any method was actually profiled during the last frame.";

        [JsonProperty]
        internal bool StackTraceEnabled;

        [JsonProperty]
        internal readonly string StackTraceEnabled_Description = $"Use StackTraces to figure out the callers of methods already identified as running slow (this is not for drilling down!)." +
            $" Enabling stack traces will make the game extremely slow and should only be used to debug very specific use cases with very narrow scoped method filters." +
            $" Really, it will be slow, instead of 10% overhead it will be 5000%. Better use the unity development player.";

        [JsonProperty]
        internal int StackTraceMaxFrameCount = 5;
        [JsonProperty]
        internal readonly string StackTraceMaxFrameCount_Description = $"Defines the maximum number of frames to go back on a stack trace.";
    }
}
