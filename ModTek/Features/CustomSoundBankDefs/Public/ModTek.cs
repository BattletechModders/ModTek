using System.Collections.Generic;
using ModTek.Features.CustomSoundBankDefs;

// ReSharper disable once CheckNamespace
// ReSharper disable once UnusedMember.Global
namespace ModTek;

public static partial class ModTek
{
    public static readonly Dictionary<string, SoundBankDef> soundBanks = SoundBanksFeature.soundBanks;
}