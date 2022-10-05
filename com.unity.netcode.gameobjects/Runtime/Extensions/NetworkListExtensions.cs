using System;
using System.Collections.Generic;

namespace Unity.Netcode
{
    public static class NetworkListExtensions
    {
        public static void AddRange<T>(this NetworkList<T> self, IEnumerable<T> enumerable) where T : unmanaged, IEquatable<T>
        {
            foreach (var item in enumerable)
            {
                self.Add(item);
            }
        }

        public static bool All<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            foreach (var item in self)
            {
                if (!predicate.Invoke(item))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool Any<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            foreach (var item in self)
            {
                if (predicate.Invoke(item))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Contains<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            foreach (var item in self)
            {
                if (predicate.Invoke(item))
                {
                    return true;
                }
            }

            return false;
        }

        public static int Count<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            var count = 0;

            foreach (var item in self)
            {
                if (predicate.Invoke(item))
                {
                    count++;
                }
            }

            return count;
        }

        public static T Find<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            foreach (var item in self)
            {
                if (predicate.Invoke(item))
                {
                    return item;
                }
            }

            return default;
        }

        public static int FindIndex<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            var index = 0;

            foreach (var item in self)
            {
                if (predicate.Invoke(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public static int RemoveWhere<T>(this NetworkList<T> self, Func<T, bool> match) where T : unmanaged, IEquatable<T>
        {
            var index = FindIndex(self, match);
            var numberOfItemsRemoved = 0;

            while (index != -1)
            {
                if (match.Invoke(self[index]))
                {
                    self.RemoveAt(index);
                    numberOfItemsRemoved++;
                }

                index = FindIndex(self, match);
            }

            return numberOfItemsRemoved;
        }

        public static T[] ToArray<T>(this NetworkList<T> self) where T : unmanaged, IEquatable<T> => ToArray(self, selector: value => value);

        public static TMapped[] ToArray<T, TMapped>(this NetworkList<T> self, Func<T, TMapped> selector) where T : unmanaged, IEquatable<T>
        {
            var array = new TMapped[self.Count];
            var index = 0;

            foreach (var item in self)
            {
                array[index] = selector.Invoke(item);
                index++;
            }

            return array;
        }

        public static void UpdateList<T>(this NetworkList<T> self, IList<T> target) where T : unmanaged, IEquatable<T> =>
            UpdateList(self, target, selector: value => value);

        public static void UpdateList<T, TMapped>(this NetworkList<T> self, IList<TMapped> target, Func<T, TMapped> selector) where T : unmanaged, IEquatable<T>
        {
            var index = 0;

            foreach (var item in self)
            {
                if (index < target.Count)
                {
                    target[index] = selector.Invoke(item);
                }
                else
                {
                    target.Add(selector.Invoke(item));
                }

                index++;
            }

            while (index < target.Count)
            {
                target.RemoveAt(index);
            }
        }

        public static NetworkList<T>.OnListChangedDelegate GetListUpdator<T>(this NetworkList<T> self, IList<T> target) where T : unmanaged, IEquatable<T> =>
           (NetworkListEvent<T> _) => UpdateList(self, target);

        public static NetworkList<T>.OnListChangedDelegate GetListUpdator<T, TMapped>(this NetworkList<T> self, IList<TMapped> target, Func<T, TMapped> selector)
            where T : unmanaged, IEquatable<T> => (NetworkListEvent<T> _) => UpdateList(self, target, selector);

        public static void Upsert<T>(this NetworkList<T> self, int index, T item) where T : unmanaged, IEquatable<T>
        {
            if (self.Count > index)
            {
                self[index] = item;
            }
            else
            {
                for (var fillerIndex = 0; fillerIndex < index - self.Count; fillerIndex++)
                {
                    self.Add(default);
                }

                self.Add(item);
            }
        }

        public static IEnumerable<T> Where<T>(this NetworkList<T> self, Func<T, bool> predicate) where T : unmanaged, IEquatable<T>
        {
            foreach (var item in self)
            {
                if (predicate.Invoke(item))
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this NetworkList<T> self) where T : unmanaged, IEquatable<T>
        {
            var index = 0;

            foreach (var item in self)
            {
                yield return (item, index);

                index++;
            }
        }
    }
}
