using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    public static class NetworkDictionaryExtensions
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this NetworkDictionary<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => ToDictionary(self, keySelector: key => key);

        public static Dictionary<TKeyMapped, TValue> ToDictionary<TKey, TValue, TKeyMapped>(this NetworkDictionary<TKey, TValue> self, Func<TKey, TKeyMapped> keySelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var dictionary = new Dictionary<TKeyMapped, TValue>();

            foreach (var (Key, Value) in self)
            {
                dictionary.Add(keySelector.Invoke(Key), Value);
            }

            return dictionary;
        }

        public static void UpdateDictionary<TKey, TValue>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKey, TValue> target)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => UpdateDictionary(self, target, keySelector: key => key, keyLookup: key => key);

        public static void UpdateDictionary<TKey, TValue, TKeyMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKeyMapped, TValue> target, Func<TKey, TKeyMapped> keySelector, Func<TKeyMapped, TKey> keyLookup)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            foreach (var (Key, Value) in self)
            {
                target[keySelector.Invoke(Key)] = Value;
            }

            foreach (var pair in target)
            {
                if (!self.ContainsKey(keyLookup.Invoke(pair.Key)))
                {
                    target.Remove(pair.Key);
                }
            }
        }

        public static NetworkDictionary<TKey, TValue>.OnDictionaryChangedDelegate GetDictionaryUpdator<TKey, TValue>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKey, TValue> target)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => (NetworkDictionaryEvent<TKey, TValue> _) => UpdateDictionary(self, target);

        public static NetworkDictionary<TKey, TValue>.OnDictionaryChangedDelegate GetDictionaryUpdator<TKey, TValue, TKeyMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKeyMapped, TValue> target, Func<TKey, TKeyMapped> keySelector, Func<TKeyMapped, TKey> keyLookup)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => (NetworkDictionaryEvent<TKey, TValue> _) => UpdateDictionary(self, target, keySelector, keyLookup);
    }
}
