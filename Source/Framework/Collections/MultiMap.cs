// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Collections.Generic
{
    public sealed class MultiMap<TKey, TValue> : IMultiMap<TKey, TValue>
    {
        static List<object> _emptyList = new List<object>();
        public MultiMap() { }

        public MultiMap(IEnumerable<KeyValuePair<TKey, TValue>> initialData)
        {
            foreach (var item in initialData)
            {
                Add(item);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (!_interalStorage.TryGetValue(key, out var val))
            {
                val = new List<TValue>();
                _interalStorage.Add(key, val);
            }

            val.Add(value);
        }

        public void AddRange(TKey key, IEnumerable<TValue> valueList)
        {
            if (!_interalStorage.TryGetValue(key, out var val))
            {
                val = new List<TValue>();
                _interalStorage.Add(key, val);
            }

            val.AddRange(valueList);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Remove(TKey key)
        {
            return _interalStorage.Remove(key);
        }

        /// <summary>
        ///     Removes all the entries of the matching expression
        /// </summary>
        /// <param name="pred">Expression to check to remove an item</param>
        /// <returns>Multimap of removed values.</returns>
        public MultiMap<TKey, TValue> RemoveIfMatchingMulti(Func<KeyValuePair<TKey, TValue>, bool> pred)
        {
            var toRemove = new MultiMap<TKey, TValue>();

            foreach (var item in KeyValueList)
                if (pred(item))
                    toRemove.Add(item);

            foreach (var item in toRemove.KeyValueList)
                Remove(item.Key, item.Value);

            return toRemove;
        }

        public bool RemoveFirstMatching(Func<KeyValuePair<TKey, TValue>, bool> pred, out KeyValuePair<TKey, TValue> foundValue)
        {
            foundValue = new KeyValuePair<TKey, TValue>();
            bool found = false;

            foreach (var item in KeyValueList)
                if (pred(item))
                {
                    foundValue = item;
                    found = true;
                    break;
                }

            if (found)
                Remove(foundValue.Key, foundValue.Value);

            return found;
        }

        /// <summary>
        ///     Removes all the entries of the matching expression
        /// </summary>
        /// <param name="pred">Expression to check to remove an item</param>
        /// <returns>List of removed key/value pairs.</returns>
        public List<KeyValuePair<TKey, TValue>> RemoveIfMatching(Func<KeyValuePair<TKey, TValue>, bool> pred)
        {
            var toRemove = new List<KeyValuePair<TKey, TValue>>();

            foreach (var item in KeyValueList)
                if (pred(item))
                    toRemove.Add(item);

            foreach (var item in toRemove)
                Remove(item.Key, item.Value);

            return toRemove;
        }

        /// <summary>
        ///     Calls the action for the first matching pred and returns. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public bool CallOnFirstMatch(Func<KeyValuePair<TKey, TValue>, bool> pred, Action<KeyValuePair<TKey, TValue>> action)
        {
            foreach (var item in KeyValueList)
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
        public List<KeyValuePair<TKey, TValue>> CallOnMatch(Func<KeyValuePair<TKey, TValue>, bool> pred, Action<KeyValuePair<TKey, TValue>> action)
        {
            var matches = new List<KeyValuePair<TKey, TValue>>();

            foreach (var item in KeyValueList)
                if (pred(item))
                    matches.Add(item);

            foreach (var item in matches)
                action(item);

            return matches;
        }

        /// <summary>
        ///     Calls the action for each matching pred. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public List<TValue> CallOnMatch(TKey key, Func<TValue, bool> pred, Action<TValue> action)
        {
            var matches = new List<TValue>();

            if (_interalStorage.TryGetValue(key, out var list))
                foreach(var val in list)
                    if (pred(val))
                        matches.Add(val);

            foreach (var val in matches)
                action(val);

            return matches;
        }

        /// <summary>
        ///     Calls the action for the first matching pred and returns. Allows the action to be safely modify this map without getting enumeration exceptions
        /// </summary>
        public bool CallOnFirstMatch(TKey key, Func<TValue, bool> pred, Action<TValue> action)
        {
            var matches = default(TValue);
            bool found = false;

            if (_interalStorage.TryGetValue(key, out var list))
                foreach (var val in list)
                    if (pred(val))
                    {
                        matches = val;
                        found = true;
                        break;
                    }

            if (found)
                action(matches);

            return found;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!ContainsKey(item.Key))
                return false;

            bool val = _interalStorage[item.Key].Remove(item.Value);

            if (!val)
                return false;

            if (_interalStorage[item.Key].Empty())
                _interalStorage.Remove(item.Key);

            return true;
        }

        public bool Remove(TKey key, TValue value)
        {
            if (!ContainsKey(key))
                return false;

            bool val = _interalStorage[key].Remove(value);
            if (!val)
                return false;

            if (_interalStorage[key].Empty())
                _interalStorage.Remove(key);

            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return _interalStorage.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            List<TValue> valueList;
            if (_interalStorage.TryGetValue(item.Key, out valueList))
                return valueList.Contains(item.Value);
            return false;
        }

        public bool Contains(TKey key, TValue item)
        {
            if (!_interalStorage.ContainsKey(key)) return false;
            return _interalStorage[key].Contains(item);
        }

        public List<TValue> LookupByKey(TKey key)
        {
            if (_interalStorage.TryGetValue(key, out var values))
                return values;

            return _emptyList.Cast<TValue>().ToList();
        }

        public List<TValue> LookupByKey(object key)
        {
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));
            if (_interalStorage.ContainsKey(newkey))
                return _interalStorage[newkey];

            return _emptyList.Cast<TValue>().ToList();
        }

        bool TryGetValue(TKey key, out List<TValue> value)
        {
            return _interalStorage.TryGetValue(key, out value);
        }

        public List<TValue> this[TKey key]
        {
            get
            {
                if (!_interalStorage.TryGetValue(key, out var val))
                    return _emptyList.Cast<TValue>().ToList();
                return val;
            }
            set
            {
                if (!_interalStorage.ContainsKey(key))
                    _interalStorage.Add(key, value);
                else
                    _interalStorage[key] = value;
            }
        }

        public ICollection<TKey> Keys
        {
            get { return _interalStorage.Keys; }
        }

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> retVal = new();
                foreach (var item in _interalStorage)
                {
                    retVal.AddRange(item.Value);
                }
                return retVal;
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> KeyValueList
        {
            get
            {
                foreach (var pair in _interalStorage)
                    foreach (var value in pair.Value)
                        yield return new KeyValuePair<TKey, TValue>(pair.Key, value);
            }
        }

        public void Clear()
        {
            _interalStorage.Clear();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, "argument 'arrayIndex' cannot be negative");

            if (arrayIndex >= array.Length || Count > array.Length - arrayIndex)
                array = new KeyValuePair<TKey, TValue>[Count];

            int index = arrayIndex;
            foreach (KeyValuePair<TKey, TValue> pair in KeyValueList)
                array[index++] = pair;
        }

        /// <summary>
        ///     Returns an exact copy of the multimap as a new instance.
        /// </summary>
        /// <returns></returns>
        public MultiMap<TKey, TValue> GetCopy()
        {
            var retval = new MultiMap<TKey, TValue>();

            foreach (var pair in _interalStorage)
                foreach (var item in pair.Value)
                    retval.Add(pair.Key, item);

            return retval;
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var item in _interalStorage)
                {
                    count += item.Value.Count;
                }
                return count;
            }
        }

        public bool Empty()
        {
            return _interalStorage == null || _interalStorage.Count == 0;  
        }

        private Dictionary<TKey, List<TValue>> _interalStorage = new();
    }
    
}
