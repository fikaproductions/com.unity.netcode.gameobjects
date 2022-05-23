using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    public static class NetworkDictionaryExtensions
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this NetworkDictionary<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var dictionary = new Dictionary<TKey, TValue>();

            foreach (var pair in self)
            {
                dictionary.Add(pair.Key, pair.Value);
            }

            return dictionary;
        }

        public static Dictionary<TKeyMapped, TValue> ToDictionary<TKey, TValue, TKeyMapped>(this NetworkDictionary<TKey, TValue> self, Func<TKey, TKeyMapped> keySelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var dictionary = new Dictionary<TKeyMapped, TValue>();

            foreach (var pair in self)
            {
                dictionary.Add(keySelector.Invoke(pair.Key), pair.Value);
            }

            return dictionary;
        }
    }
}
