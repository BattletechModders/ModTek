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
        internal readonly string Enabled_Description = $"Enable or disable profiling, recommended to stay off as it saved some performance. Based on harmony patching and does not cover everything.";

        [JsonProperty]
        internal float DumpWhenFrameTimeDeltaLargerThan = 1f/30;
        [JsonProperty]
        internal readonly string DumpWhenFrameTimeDeltaLargerThan_Description = $"Dump profiler stats if a frame takes longer than the specified amount (in seconds).";

        [JsonProperty]
        internal int RecursiveDepthToFindCalleesBelowFilteredMethods = 3;
        [JsonProperty]
        internal readonly string RecursiveDepthToFindCalleesBelowFilteredMethods_Description = $"Methods that were found by the filter might be too generic, so we recursively find further profiling candidates. Set to 0 to disable.";

        [JsonProperty]
        internal string BlacklistedAssemblyNamePattern = "^(" +
            "mscorlib" + // keeps profiling overhead low
            "|System(\\..*)?" + // keeps profiling overhead low
            "|ManagedBass" + // does actually not properly load, so iterating over types triggers lots of exceptions
            ")$";
        [JsonProperty]
        internal readonly string BlacklistedAssemblyNames_Description = $"A pattern of assemblies to always ignore. Uses Regex. System assemblies can be profiled but the overhead is quite high.";

        // type blacklisting is here due to issues with Harmony and maybe with the underlying mono of Unity
        // probably using another way of injecting post/pre for profiling would do the job
        // or harmony 2 might solve the issue, not sure how to integrate that though
        //  could also try to use MonoMod.Common directly
        [JsonProperty]
        internal string BlacklistedTypeNamePattern = "^(" +
            "UIWidgets\\.ListViewBase" + // IL can't be read by harmony
            //"|UnityEngine\\.Object" + // unity is slow, but we don't need generic checks
            "|GravityMatters\\.GravityMatters.*" + // harmony tries to init the static constructor and that fails
            "|BattleTech\\.Save\\.SaveGameStructure\\.SaveRejectListManager" + // harmony tries to init the static constructor and that fails
            "|MonthlyTechandMoraleAdjustment.*Patch" + // harmony tries to init the static constructor and that fails
            ")$";
        [JsonProperty]
        internal readonly string BlacklistedTypeNamePattern_Description = $"A pattern of types to always ignore. Uses Regex.";

        [JsonProperty]
        internal MethodMatchFilter[] Filters = {
            // some Unity methods of interest
            // see https://docs.unity3d.com/2018.3/Documentation/Manual/ExecutionOrder.html
            // Unity MonoBehavior
            // see https://docs.unity3d.com/2018.3/Documentation/ScriptReference/MonoBehaviour.html
            new MethodMatchFilter
            {
                Name = "FixedUpdate",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
                Name = "Update",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
                Name = "LateUpdate",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                SubClassOfTypeName = typeof(MonoBehaviour).FullName,
            },
            new MethodMatchFilter
            {
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
            // BT methods
            new MethodMatchFilter
            {
                Name = "Update",
                ParameterTypeNames = Array.Empty<string>(),
                ReturnTypeName = typeof(void).FullName,
                AssemblyName = "Assembly-CSharp",
            },
            new MethodMatchFilter
            {
                Name = "Update",
                ParameterTypeNames = new[] { typeof(float).FullName },
                ReturnTypeName = typeof(void).FullName,
                AssemblyName = "Assembly-CSharp",
            },
        };
        [JsonProperty]
        internal readonly string Filters_Description = "Only matching methods and any related harmony patches are profiled." +
            " Not compatible with everything and skips some types of methods by default." +
            " Uses harmony itself to patch methods with pre/post methods, see harmony summary dump.";
    }
}
