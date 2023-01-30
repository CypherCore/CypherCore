// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Linq;

namespace System.Collections.Generic
{
    public sealed class MultiMap<TKey, TValue> : IMultiMap<TKey, TValue>
    {
        private static readonly List<object> _emptyList = new();

        private readonly Dictionary<TKey, List<TValue>> _interalStorage = new();

        public MultiMap()
        {
        }

        public MultiMap(IEnumerable<KeyValuePair<TKey, TValue>> initialData)
        {
            foreach (var item in initialData)
                Add(item);
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

        public ICollection<TKey> Keys => _interalStorage.Keys;

        public ICollection<TValue> Values
        {
            get
            {
                List<TValue> retVal = new();

                foreach (var item in _interalStorage)
                    retVal.AddRange(item.Value);

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

            if (arrayIndex >= array.Length ||
                Count > array.Length - arrayIndex)
                array = new KeyValuePair<TKey, TValue>[Count];

            int index = arrayIndex;

            foreach (KeyValuePair<TKey, TValue> pair in this)
                array[index++] = new KeyValuePair<TKey, TValue>(pair.Key, pair.Value);
        }

        public int Count
        {
            get
            {
                int count = 0;

                foreach (var item in _interalStorage)
                    count += item.Value.Count;

                return count;
            }
        }


        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new MultiMapEnumerator<TKey, TValue>(this);
        }

        public bool Empty()
        {
            return _interalStorage == null || _interalStorage.Count == 0;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private bool TryGetValue(TKey key, out TValue value)
        {
            value = default;

            if (_interalStorage.TryGetValue(key, out var val))
            {
                value = val.LastOrDefault();

                return true;
            }

            return false;
        }
    }
}