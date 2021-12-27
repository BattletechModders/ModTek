using System;
using Newtonsoft.Json;

namespace ModTek.Features.Profiler
{
    internal class MethodMatchFilter
    {
        [JsonProperty]
        internal bool Enabled = true;
        [JsonProperty(Required = Required.Always)]
        internal string Name;
        [JsonProperty]
        internal Type[] ParameterTypes;
        [JsonProperty]
        internal Type ReturnType;
        [JsonProperty]
        internal Type SubClassOf;
        [JsonProperty]
        internal string AssemblyName;
    }
}
