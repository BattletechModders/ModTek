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
        internal readonly string Enabled_Description = $"Enable or disable profiling, recommended to stay off as it saved some performance.";

        [JsonProperty]
        internal float DumpWhenFrameTimeDeltaLargerThan = 1f/30;
        [JsonProperty]
        internal readonly string DumpWhenFrameTimeDeltaLargerThan_Description = $"Dump profiler stats if a frame takes longer than the specified amount (in seconds).";

        [JsonProperty]
        internal MethodMatchFilter[] Filters = {
            // some Unity methods of interest
            // see https://docs.unity3d.com/2018.3/Documentation/Manual/ExecutionOrder.html
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
