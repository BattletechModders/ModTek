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
            new MethodMatchFilter
            {
                Name = "FixedUpdate",
                ParameterTypes = Type.EmptyTypes,
                ReturnType = typeof(void),
                SubClassOf = typeof(MonoBehaviour),
            },
            new MethodMatchFilter
            {
                Name = "Update",
                ParameterTypes = Type.EmptyTypes,
                ReturnType = typeof(void),
                SubClassOf = typeof(MonoBehaviour),
            },
            new MethodMatchFilter
            {
                Name = "LateUpdate",
                ParameterTypes = Type.EmptyTypes,
                ReturnType = typeof(void),
                SubClassOf = typeof(MonoBehaviour),
            },
            // Start or Awake produce issues
            new MethodMatchFilter
            {
                Enabled = false,
                Name = "Start",
                ParameterTypes = Type.EmptyTypes,
                SubClassOf = typeof(MonoBehaviour),
            },
            new MethodMatchFilter
            {
                Enabled = false,
                Name = "Awake",
                ParameterTypes = Type.EmptyTypes,
                ReturnType = typeof(void),
                SubClassOf = typeof(MonoBehaviour),
            },
            // BT methods
            new MethodMatchFilter
            {
                Name = "Update",
                ParameterTypes = Type.EmptyTypes,
                ReturnType = typeof(void),
                AssemblyName = "Assembly-CSharp",
            },
            new MethodMatchFilter
            {
                Name = "Update",
                ParameterTypes = new[] { typeof(float) },
                ReturnType = typeof(void),
                AssemblyName = "Assembly-CSharp",
            },
        };
        [JsonProperty]
        internal readonly string Filters_Description = "Only listed methods and any related harmony patches are profiled." +
            " Not compatible with everything and skips some types of methods by default. Uses harmony itself to patch methods with pre/post methods";
    }
}
