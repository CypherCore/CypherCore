/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Linq;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static bool Empty<TValue>(this ICollection<TValue> collection)
        {
            return collection.Count == 0;
        }

        public static bool Empty<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary)
        {
            return dictionary.Count == 0;
        }

        /// <summary>
        /// Returns the entry in this list at the given index, or the default value of the element
        /// type if the index was out of bounds.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list to retrieve from.</param>
        /// <param name="index">The index to try to retrieve at.</param>
        /// <returns>The value, or the default value of the element type.</returns>
        public static T LookupByIndex<T>(this IList<T> list, int index)
        {
            return index >= list.Count ? default(T) : list[index];
        }

        /// <summary>
        /// Returns the entry in this dictionary at the given key, or the default value of the key
        /// if none.
        /// </summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="dict">The dictionary to operate on.</param>
        /// <param name="key">The key of the element to retrieve.</param>
        /// <returns>The value (if any).</returns>
        public static TValue LookupByKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, object key)
        {
            TValue val;
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));
            return dict.TryGetValue(newkey, out val) ? val : default(TValue);
        }
        public static TValue LookupByKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            TValue val;
            return dict.TryGetValue(key, out val) ? val : default(TValue);
        }
        public static KeyValuePair<TKey, TValue> Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.ContainsKey(key))
                return default(KeyValuePair<TKey, TValue>);

            return new KeyValuePair<TKey, TValue>(key, dict[key]);
        }

        public static bool ContainsKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, object key)
        {
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));
            return dict.ContainsKey(newkey);
        }

        public static void RemoveAll<T>(this List<T> collection, ICheck<T> check)
        {
            collection.RemoveAll(check.Invoke);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }

        public static void Swap<T>(this T[] array, int position1, int position2)
        {
            //
            // Swaps elements in an array. Doesn't need to return a reference.
            //
            T temp = array[position1]; // Copy the first position's element
            array[position1] = array[position2]; // Assign to the second element
            array[position2] = temp; // Assign to the first element
        }

        public static void Resize<T>(this List<T> list, uint size)
        {
            int cur = list.Count;
            if (size < cur)
                list.RemoveRange((int)size, cur - (int)size);
        }

        public static void RandomResize<T>(this IList<T> list, uint size)
        {
            int listSize = list.Count;

            while (listSize > size)
            {
                list.RemoveAt(RandomHelper.IRand(0, listSize));
                --listSize;
            }
        }

        public static void RandomResize<T>(this List<T> list, Predicate<T> predicate, uint size)
        {
            for (var i = 0; i < list.Count; ++i)
            {
                var obj = list[i];
                if (!predicate(obj))
                    list.Remove(obj);
            }

            if (size != 0)
                list.Resize(size);
        }

        public static T SelectRandom<T>(this IEnumerable<T> source)
        {
            return source.SelectRandom(1).Single();
        }

        public static IEnumerable<T> SelectRandom<T>(this IEnumerable<T> source, uint count)
        {
            return source.Shuffle().Take((int)count);
        }

        public static T SelectRandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, float> weightSelector)
        {
            float totalWeight = sequence.Sum(weightSelector);
            // The weight we are after...
            float itemWeightIndex = (float)RandomHelper.NextDouble() * totalWeight;
            float currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;

            }

            return default(T);
        }

        public static IEnumerable<TSource> Intersect<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second, Func<TSource, TSource, bool> comparer)
        {
            return first.Where(x => second.Count(y => comparer(x, y)) == 1);
        }

        public static uint[] ToBlockRange(this BitSet array)
        {
            uint[] blockValues = new uint[array.Length / 32 + 1];
            array.CopyTo(blockValues, 0);
            return blockValues;
        }

        public static void Clear(this Array array)
        {
            Array.Clear(array, 0, array.Length);
        }
    }

    public interface ICheck<in T>
    {
        bool Invoke(T obj);
    }

    public interface IDoWork<in T>
    {
        void Invoke(T obj);
    }
}
