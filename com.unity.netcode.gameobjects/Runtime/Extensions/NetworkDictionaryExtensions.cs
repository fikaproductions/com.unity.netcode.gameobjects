using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    public static class NetworkDictionaryExtensions
    {
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this NetworkDictionary<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => ToDictionary(self, keySelector: key => key, valueSelector: value => value);

        public static Dictionary<TKeyMapped, TValue> ToDictionary<TKey, TValue, TKeyMapped>(this NetworkDictionary<TKey, TValue> self, Func<TKey, TKeyMapped> keySelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => ToDictionary(self, keySelector, valueSelector: value => value);

        public static Dictionary<TKey, TValueMapped> ToDictionary<TKey, TValue, TValueMapped>(this NetworkDictionary<TKey, TValue> self, Func<TValue, TValueMapped> valueSelector)
           where TKey : unmanaged, IEquatable<TKey>
           where TValue : unmanaged => ToDictionary(self, keySelector: key => key, valueSelector);

        public static Dictionary<TKeyMapped, TValueMapped> ToDictionary<TKey, TValue, TKeyMapped, TValueMapped>(this NetworkDictionary<TKey, TValue> self, Func<TKey, TKeyMapped> keySelector, Func<TValue, TValueMapped> valueSelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var dictionary = new Dictionary<TKeyMapped, TValueMapped>();

            foreach (var (Key, Value) in self)
            {
                dictionary.Add(keySelector.Invoke(Key), valueSelector.Invoke(Value));
            }

            return dictionary;
        }

        public static void UpdateDictionary<TKey, TValue>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKey, TValue> target)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => UpdateDictionary(self, target, keySelector: key => key, keyLookup: key => key, valueSelector: value => value);

        public static void UpdateDictionary<TKey, TValue, TKeyMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKeyMapped, TValue> target, Func<TKey, TKeyMapped> keySelector, Func<TKeyMapped, TKey> keyLookup)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => UpdateDictionary(self, target, keySelector, keyLookup, valueSelector: value => value);

        public static void UpdateDictionary<TKey, TValue, TValueMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKey, TValueMapped> target, Func<TValue, TValueMapped> valueSelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => UpdateDictionary(self, target, keySelector: key => key, keyLookup: key => key, valueSelector);

        public static void UpdateDictionary<TKey, TValue, TKeyMapped, TValueMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKeyMapped, TValueMapped> target, Func<TKey, TKeyMapped> keySelector, Func<TKeyMapped, TKey> keyLookup, Func<TValue, TValueMapped> valueSelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            foreach (var (Key, Value) in self)
            {
                target[keySelector.Invoke(Key)] = valueSelector.Invoke(Value);
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

        public static NetworkDictionary<TKey, TValue>.OnDictionaryChangedDelegate GetDictionaryUpdator<TKey, TValue, TValueMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKey, TValueMapped> target, Func<TValue, TValueMapped> valueSelector)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged => (NetworkDictionaryEvent<TKey, TValue> _) => UpdateDictionary(self, target, valueSelector);

        public static NetworkDictionary<TKey, TValue>.OnDictionaryChangedDelegate GetDictionaryUpdator<TKey, TValue, TKeyMapped, TValueMapped>(this NetworkDictionary<TKey, TValue> self, IDictionary<TKeyMapped, TValueMapped> target, Func<TKey, TKeyMapped> keySelector, Func<TKeyMapped, TKey> keyLookup, Func<TValue, TValueMapped> valueSelector)
           where TKey : unmanaged, IEquatable<TKey>
           where TValue : unmanaged => (NetworkDictionaryEvent<TKey, TValue> _) => UpdateDictionary(self, target, keySelector, keyLookup, valueSelector);
    }
}
