using System.Collections.Generic;
using ModTek.Features.Manifest.Mods;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global
namespace ModTek
{
    public static partial class ModTek
    {
        public static readonly Dictionary<string, ModDefEx> allModDefs = ModDefsDatabase.allModDefs;
    }
}
