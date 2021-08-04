using System.Collections.Generic;
using ModTek.Features.SoundBanks;

// ReSharper disable once CheckNamespace
namespace ModTek
{
    public static partial class ModTek
    {
        public static readonly Dictionary<string, SoundBankDef> soundBanks = SoundBanksFeature.soundBanks;
    }

}
