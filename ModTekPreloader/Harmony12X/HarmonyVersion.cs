using System;
using System.Collections.Generic;

namespace ModTekPreloader.Harmony12X
{
    internal class HarmonyVersion
    {
        internal HarmonyVersion(string name, Version lowerBoundInclusive, Version upperBoundExclusive)
        {
            Name = name;
            _lowerBoundInclusive = lowerBoundInclusive;
            _upperBoundExclusive = upperBoundExclusive;
        }

        internal string Name { get; }

        private readonly Version _lowerBoundInclusive;
        private readonly Version _upperBoundExclusive;

        internal bool IsMatch(Version version)
        {
            return _lowerBoundInclusive <= version && version < _upperBoundExclusive;
        }

        public override string ToString()
        {
            return $"{Name}@[{_lowerBoundInclusive},{_upperBoundExclusive})";
        }

        // a list of supported version and the respective shim assembly names
        internal static readonly List<HarmonyVersion> SupportedVersions = new List<HarmonyVersion>
        {
            // older harmony, only used by few mods
            new HarmonyVersion("0Harmony109", new Version(1, 0), new Version(1, 1)),
            // current harmony as provided by HBS and ModTek by default
            new HarmonyVersion("0Harmony12", new Version(1, 1), new Version(1, 3)),
            // HarmonyX 2.0 was not yet compatible with Harmony 2.0, Harmony 2.0 or X 2.0 was never used in the ModTek community
            new HarmonyVersion("0Harmony20", new Version(2, 0), new Version(2, 1)),
            // newer HarmonyX versions are highly compatible with older HarmonyX versions, no shimming required
            new HarmonyVersion("0Harmony", new Version(2, 1), new Version(2, 99)),
        };
    }
}
