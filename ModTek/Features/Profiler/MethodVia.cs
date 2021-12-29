using System.Reflection;
using ModTek.Util;

namespace ModTek.Features.Profiler
{
    internal class MethodVia
    {
        internal readonly MethodBase source;
        internal readonly object target;

        internal MethodVia(MethodBase source, object target)
        {
            this.source = source;
            this.target = target;
        }

        public bool Equals(MethodVia other)
        {
            return source.Equals(other.source) && target.Equals(other.target);
        }

        public override bool Equals(object obj)
        {
            return obj is MethodVia other && Equals(other);
        }

        private string rawString;
        public override string ToString()
        {
            rawString = rawString ?? Timings.GetIdFromObject(target) + " via " + AssemblyUtil.GetMethodFullName(source);
            return rawString;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (source.GetHashCode() * 397) ^ target.GetHashCode();
            }
        }
    }
}
