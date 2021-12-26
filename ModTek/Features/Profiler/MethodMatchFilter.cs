using System;

namespace ModTek.Features.Profiler
{
    internal class MethodMatchFilter
    {
        internal string Name;
        internal Type[] ParameterTypes;
        internal Type ReturnType;
        internal Type SubClassOf;
    }
}
