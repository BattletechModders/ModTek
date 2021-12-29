using System;
using Newtonsoft.Json;
using UnityEngine;

namespace ModTek.Features.Profiler
{
    public class ProfilingSettings
    {
        [JsonProperty]
        internal bool Enabled;
        [JsonProperty]
        internal readonly string Enabled_Description = $"Enable or disable profiling, recommended to stay off as it saves a lot of performance and avoids instability. Based on harmony patching and comes with all its flaws and issues.";

        [JsonProperty]
        internal float DumpWhenFrameTimeDeltaLargerThan = 1f/30;
        [JsonProperty]
        internal readonly string DumpWhenFrameTimeDeltaLargerThan_Description = $"Dump profiler stats if a frame takes longer than the specified amount (in seconds). Dumping happens regardless if any method was actually profiled during the last frame.";

        [JsonProperty]
        internal int RecursiveDepthToFindCalleesBelowFilteredMethods = 1;
        [JsonProperty]
        internal readonly string RecursiveDepthToFindCalleesBelowFilteredMethods_Description = $"Drill down on methods that are currently being filtered. Might corrupt or crash the game if harmony has trouble with patching. Set to 0 to disable.";

        [JsonProperty]
        internal string BlacklistedAssemblyNamePattern = "^(" +
            "mscorlib" + // keeps profiling overhead low
            "|System(\\..*)?" + // keeps profiling overhead low
            "|ManagedBass" + // does actually not properly load, so iterating over types triggers lots of exceptions
            ")$";
        [JsonProperty]
        internal readonly string BlacklistedAssemblyNames_Description = $"A pattern of assemblies to always ignore. Uses Regex. System assemblies can be profiled but the overhead is quite high.";

        [JsonProperty]
        internal bool StackTraceEnabled;
        [JsonProperty]
        internal readonly string StackTraceEnabled_Description = $"Use StackTraces to figure out the callers of methods already identified as running slow (this is not for drilling down!)." +
            $" Enabling stack traces will make the game extremely slow and should only be used to debug very specific use cases with very narrow scoped method filters." +
            $" Really, it will be slow, instead of 10% overhead it will be 5000%.";

        [JsonProperty]
        internal int StackTraceMaxFrameCount = 5;
        [JsonProperty]
        internal readonly string StackTraceMaxFrameCount_Description = $"Defines the maximum number of frames to go back on a stack trace.";

        [JsonProperty]
        internal bool CoverCoroutines = true;
        [JsonProperty]
        internal readonly string CoverCoroutines_Description = $"Enable profiling of Unity Coroutines, should be relatively safe as long as no other patch interfers.";

        // type blacklisting is here due to issues with Harmony and maybe with the underlying mono of Unity
        // probably using another way of injecting post/pre for profiling would do the job
        // or harmony 2 might solve the issue, not sure how to integrate that though
        //  could also try to use MonoMod.Common directly
        [JsonProperty]
        internal string BlacklistedTypeNamePattern = "^(" +
            "UIWidgets\\.ListViewBase" + // IL can't be read by harmony
            "|UnityEngine\\.Object" + // unity is slow
            "|GravityMatters\\.GravityMatters.*" + // harmony tries to init the static constructor and that fails
            "|BattleTech\\.Save\\.SaveGameStructure\\.SaveRejectListManager" + // harmony tries to init the static constructor and that fails
            "|MonthlyTechandMoraleAdjustment.*Patch" + // harmony tries to init the static constructor and that fails
            ")$";
        [JsonProperty]
        internal readonly string BlacklistedTypeNamePattern_Description = $"A pattern of types to always ignore. Uses Regex. Mostly used to avoid crashes and state corruption triggered by Harmony doing weird stuff during patching.";

        [JsonProperty]
        internal MethodMatchFilter[] Filters = {
            // some Unity methods of interest
            // see https://docs.unity3d.com/2018.3/Documentation/Manual/ExecutionOrder.html
            // Unity MonoBehavior
            // see https://docs.unity3d.com/2018.3/Documentation/ScriptReference/MonoBehaviour.html
            new MethodMatchFilter
            {
                Enabled = false,
                Name = "FixedUpdate",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
                Enabled = true,
                Name = "Update",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
                Enabled = true,
                Name = "LateUpdate",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
                Enabled = false,
                Name = "Start",
                ParameterTypeNames = Array.Empty<string>(),
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
                Enabled = false, // Awake produces issues
                Name = "Awake",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            // Unity
            new MethodMatchFilter
            {
                Enabled = false,
                Name = "op_Equality",
                ClassTypeName = typeof(UnityEngine.Object).FullName
            },
            new MethodMatchFilter
            {
                Enabled = false,
                Name = "op_Inequality",
                ClassTypeName = typeof(UnityEngine.Object).FullName
            },
            // BT methods
            new MethodMatchFilter
            {
                Enabled = true,
                Name = "Update",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                AssemblyName = "Assembly-CSharp",
            },
            new MethodMatchFilter
            {
                Enabled = true,
                Name = "Update",
                ParameterTypeNames = new[] { typeof(float).FullName },
                ReturnTypeName = typeof(void).FullName,
                AssemblyName = "Assembly-CSharp",
            },
            new MethodMatchFilter
            {
                Enabled = true,
                Name = "OnDayPassed",
                AssemblyName = "Assembly-CSharp",
            },
        };
        [JsonProperty]
        internal readonly string Filters_Description = "Only matching methods and any related harmony patches are profiled." +
            " Not compatible with everything and skips some types of methods by default." +
            " Uses harmony itself to patch methods with pre/post methods, see harmony summary dump.";
    }
}
