using System.Collections.Generic;
using ModTek.Features.CustomSoundBankDefs;

// ReSharper disable once CheckNamespace
namespace ModTek;

public static class SoundBanksProcessHelper
{
    private static readonly Dictionary<string, ProcessParameters> procParams = new();

    public static void RegisterProcessParams(string soundbank, string param1, string param2)
    {
        if (procParams.ContainsKey(soundbank))
        {
            procParams[soundbank] = new ProcessParameters(param1, param2);
        }
        else
        {
            procParams.Add(soundbank, new ProcessParameters(param1, param2));
        }
    }

    internal static ProcessParameters GetRegisteredProcParams(string soundbank)
    {
        return procParams.TryGetValue(soundbank, out var p) ? p : null;
    }
}