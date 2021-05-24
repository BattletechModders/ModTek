using System.Collections.Generic;

namespace ModTek.SoundBanks
{
    public static class SoundBanksProcessHelper
    {
        private static Dictionary<string, ProcessParameters> procParams = new();

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

        internal static ProcessParameters GetRegistredProcParams(string soundbank)
        {
            if (procParams.TryGetValue(soundbank, out var p))
            {
                return p;
            }

            return null;
        }
    }
}
