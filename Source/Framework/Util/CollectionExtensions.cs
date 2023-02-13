// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Runtime.CompilerServices;
using Framework.Collections;
using Google.Protobuf.WellKnownTypes;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static bool Empty<T>(this Queue<T> queue)
        {
            return queue.Count == 0;
        }

        public static bool Empty<TValue>(this ICollection<TValue> collection)
        {
            return collection.Count == 0;
        }

        public static bool Empty<Tkey, TValue>(this IDictionary<Tkey, TValue> dictionary)
        {
            return dictionary.Count == 0;
        }

        /// <summary>
        ///  Returns the entry in this list at the given index, or the default value of the element
        ///  type if the index was out of bounds.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the list.</typeparam>
        /// <param name="list">The list to retrieve from.</param>
        /// <param name="index">The index to try to retrieve at.</param>
        /// <returns>The value, or the default value of the element type.</returns>
        public static T LookupByIndex<T>(this IList<T> list, int index)
        {
            return index >= list.Count ? default : list[index];
        }

        /// <summary>
        ///  Returns the entry in this dictionary at the given key, or the default value of the key
        ///  if none.
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

            return dict.TryGetValue(newkey, out val) ? val : default;
        }

        public static TValue LookupByKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.TryGetValue(key, out var val))
                return val;

            return default;
        }

        public static KeyValuePair<TKey, TValue> Find<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
        {
            if (!dict.ContainsKey(key))
                return default;

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
            T temp = array[position1];           // Copy the first position's element
            array[position1] = array[position2]; // Assign to the second element
            array[position2] = temp;             // Assign to the first element
        }

        public static void Resize<T>(this List<T> list, uint size)
        {
            int cur = list.Count;

            if (size < cur)
                list.RemoveRange((int)size, cur - (int)size);
            else
                for (var i = list.Count; i < size; ++i)
                    list.Add(default);
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

            foreach (var item in from weightedItem in sequence
                                 select new
                                 {
                                     Value = weightedItem,
                                     Weight = weightSelector(weightedItem)
                                 })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;
            }

            return default;
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

        public static void EnsureWritableListIndex<T>(this List<T> list, uint index, T defaultValue)
        {
            while (list.Count <= index)
                list.Add(defaultValue);
        }

        public static void AddToDictList<T, L>(this Dictionary<T, List<L>> dict, T key, L item)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<L>();
                dict.Add(key, list);
            }

            list.Add(item);
        }

        public static void RemoveIf<T>(this LinkedList<T> values, Func<T, bool> func)
        {
            var toRemove = new List<T>();

            foreach (var v in values)
                if (func.Invoke(v))
                    toRemove.Add(v);

            foreach (var v in toRemove)
                    values.Remove(v);
        }

        public static void RemoveIf<T>(this List<T> values, Func<T, bool> func)
        {
            for (int i = values.Count - 1; i >= 0; i--)
                if (func.Invoke(values[i]))
                    values.RemoveAt(i);
        }

        public static void RemoveIf<T>(this List<T> values, ICheck<T> check)
        {
            RemoveIf(values, check.Invoke);
        }

        public static void RemoveIf<T>(this LinkedList<T> values, ICheck<T> check)
        {
            RemoveIf(values, check.Invoke);
        }

        public static bool has_value(this object obj)
        { 
            return obj != null; 
        }

        public static void Add<T, V>(this IDictionary<T, List<V>> dict, T key, V val)
        {
            if (dict == null) throw new ArgumentNullException();

            if (!dict.TryGetValue(key, out var list))
            {
                list= new List<V>();
                dict.Add(key, list);
            }
            list.Add(val);
        }

        public static void AddToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out var val))
            {
                val = new List<TValue>();
                dict.Add(key, val);
            }

            val.Add(value);
        }

        public static void AddUniqueToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out var val))
            {
                val = new List<TValue>();
                dict.Add(key, val);
            }

            if (!val.Contains(value))
                val.Add(value);
        }

        public static void AddIf<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value, Func<TValue, TValue, bool> testExistingVsNew)
        {
            if (!dict.TryGetValue(key, out var val))
            {
                val = new List<TValue>();
                dict.Add(key, val);
            }
            bool ok = true;

            foreach(var kv in val)
                if (!testExistingVsNew(kv, value))
                {
                    ok = false; 
                    break;
                }

            if (ok)
                val.Add(value);
        }

        public static void AddToList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, KeyValuePair<TKey, TValue> item)
        {
            dict.AddToList(item.Key, item.Value);
        }


        public static bool Remove<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, KeyValuePair<TKey, TValue> item)
        {
            if (!dict.TryGetValue(item.Key, out var valList))
                return false;

            bool val = valList.Remove(item.Value);

            if (!val)
                return false;

            if (valList.Empty())
                dict.Remove(item.Key);

            return true;
        }

        public static bool Remove<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, TValue value)
        {
            if (!dict.TryGetValue(key, out var valList))
                return false;

            bool val = valList.Remove(value);
            if (!val)
                return false;

            if (valList.Empty())
                dict.Remove(key);

            return true;
        }

        /// <summary>
        ///     Removes all the entries of the matching expression
        /// </summary>
        /// <param name="pred">Expression to check to remove an item</param>
        /// <returns>Multimap of removed values.</returns>
        public static void RemoveIfMatchingMulti<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<KeyValuePair<TKey, TValue>, bool> pred)
        {
            foreach (var item in dict.KeyValueList())
                if (pred(item))
                    dict.Remove(item.Key, item.Value);
        }

        public static bool RemoveFirstMatching<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<KeyValuePair<TKey, TValue>, bool> pred, out KeyValuePair<TKey, TValue> foundValue)
        {
            foreach (var item in dict.KeyValueList())
                if (pred(item))
                {
                    dict.Remove(item.Key, item.Value);
                    foundValue = item;
                    return true;
                }

            foundValue = default;
            return false;
        }

        public static bool RemoveFirstMatching<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<KeyValuePair<TKey, TValue>, bool> pred)
        {
            return RemoveFirstMatching(dict, pred, out var _);
        }

        /// <summary>
        ///     Removes all the entries of the matching expression
        /// </summary>
        /// <param name="pred">Expression to check to remove an item</param>
        /// <returns>List of removed key/value pairs.</returns>
        public static List<KeyValuePair<TKey, TValue>> RemoveIfMatching<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<KeyValuePair<TKey, TValue>, bool> pred)
        {
            var toRemove = new List<KeyValuePair<TKey, TValue>>();

            foreach (var item in dict.KeyValueList())
                if (pred(item))
                {
                    toRemove.Add(item);
                    dict.Remove(item.Key, item.Value);
                }

            return toRemove;
        }

        public static void RemoveIfMatching<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, Func<TValue, bool> pred)
        {
            if (dict.TryGetValue(key, out var values))
                for (int i = values.Count - 1; i >= 0; i--)
                    if (pred.Invoke(values[i]))
                        values.RemoveAt(i);
        }

        /// <summary>
        ///     Calls the action for the first matching pred and returns. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public static bool CallOnFirstMatch<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<KeyValuePair<TKey, TValue>, bool> pred, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var item in dict.KeyValueList())
                if (pred(item))
                {
                    action(item);
                    return true;
                }

            return false;
        }

        /// <summary>
        ///     Calls the action for the first matching pred and returns. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public static bool CallOnFirstMatch<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, Func<TValue, bool> pred, Action<TValue> action)
        {
            if (dict.TryGetValue(key, out var list))
                foreach (var item in list)
                    if (pred(item))
                    {
                        action(item);
                        return true;
                    }

            return false;
        }

        /// <summary>
        ///     Calls the action for each matching pred. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public static List<KeyValuePair<TKey, TValue>> CallOnMatch<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, Func<KeyValuePair<TKey, TValue>, bool> pred, Action<KeyValuePair<TKey, TValue>> action)
        {
            var matches = new List<KeyValuePair<TKey, TValue>>();

            foreach (var item in dict.KeyValueList())
                if (pred(item))
                {
                    matches.Add(item);
                    action(item);
                }

            return matches;
        }

        /// <summary>
        ///     Calls the action for each matching pred. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public static List<TValue> CallOnMatch<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict, TKey key, Func<TValue, bool> pred, Action<TValue> action)
        {
            var matches = new List<TValue>();

            if (dict.TryGetValue(key, out var list))
                foreach (var val in list)
                    if (pred(val))
                    {
                        matches.Add(val);
                        action(val);
                    }

            return matches;
        }

        public static void RemoveAllMatchingKeys<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> pred)
        {
            foreach (var key in dict.Keys.ToArray())
                if (pred.Invoke(key))
                    dict.Remove(key);
        }

        public static IEnumerable<KeyValuePair<TKey, TValue>> KeyValueList<TKey, TValue>(this IDictionary<TKey, List<TValue>> dict)
        {
            foreach (var key in dict.Keys.ToArray()) // this allows it to be safely enumerated off of and have items removed.
            {
                var val = dict[key];

                for (int i = val.Count - 1; i >= 0; i--)
                    yield return new KeyValuePair<TKey, TValue>(key, val[i]);
            }
        }

        public static void Remove<T>(this List<T> list, List<T> toRemove)
        {
            foreach (var val in toRemove)
                list.Remove(val);
        }

        public static void Remove<TKey, TValue>(this IDictionary<TKey, TValue> dict, List<TKey> toRemove)
        {
            foreach (var val in toRemove)
                dict.Remove(val);
        }

        public static ManyToOneLookup<TKey, TValue> ToManyToOneLookup<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> keyComparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            var manyToOne = new ManyToOneLookup<TKey, TValue>(keyComparer, valueComparer);

            foreach (var item in source)
            {
                var key = keySelector(item);

                if (key != null)
                    manyToOne.Add(key, item);
            }

            return manyToOne;
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