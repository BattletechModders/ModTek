using System.Collections.Generic;

namespace ModTek.Util
{
    internal static class DictionaryExtensions
    {
        public static V GetOrCreate<K, V>(this Dictionary<K, V> dict, K key) where V : new()
        {
            if (dict.TryGetValue(key, out var value))
            {
                return value;
            }

            value = new V();
            dict[key] = value;
            return value;
        }
    }
}
