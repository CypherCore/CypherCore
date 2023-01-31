// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading;
using Google.Protobuf.WellKnownTypes;

namespace System.Collections.Generic
{
    public sealed class MultiMap<TKey, TValue> : IMultiMap<TKey, TValue>
    {
        private static readonly List<object> _emptyList = new();
        private Queue<Action> _actionQueue = new Queue<Action>();
        private readonly Dictionary<TKey, List<TValue>> _interalStorage = new();
        private bool _completing = false;
        internal int SyncObj = 0;
        internal HashSet<TKey> KeysRemoved = new HashSet<TKey>();
        internal HashSet<TValue> ValuesRemoved = new HashSet<TValue>();
        internal bool Itterating 
        { 
            get 
            { 
                return _completing ? false : SyncObj != 0; 
            } 
        }

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
            if (Itterating)
            {
                _actionQueue.Enqueue(() => Add(key, value));
                return;
            }

            if (!_interalStorage.TryGetValue(key, out var val))
            {
                val = new List<TValue>();
                _interalStorage.Add(key, val);
            }

            val.Add(value);
        }

        public void AddRange(TKey key, IEnumerable<TValue> valueList)
        {
            if (Itterating)
            {
                _actionQueue.Enqueue(() => AddRange(key, valueList));
                return;
            }

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
        
        public void Set(TKey key, List<TValue> values)
        {
            if (Itterating)
            {
                _actionQueue.Enqueue(() => Set(key, values));
                return;
            }

            _interalStorage[key] = values;
        }

        public bool Remove(TKey key)
        {
            if (Itterating)
            {
                KeysRemoved.Add(key);
                _actionQueue.Enqueue(() => Remove(key));
                return true;
            }

            return _interalStorage.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key, item.Value);
        }

        public bool Remove(TKey key, TValue value)
        {
            if (Itterating)
            {
                ValuesRemoved.Add(value);
                _actionQueue.Enqueue(() => Remove(key, value));
                return true;
            }

            if (!_interalStorage.TryGetValue(key, out var list))
                return false;

            bool val = list.Remove(value);

            if (!val)
                return false;

            if (list.Empty())
                _interalStorage.Remove(key);

            return true;
        }

        public bool ContainsKey(TKey key)
        {
            if (KeysRemoved.Contains(key))
                return false;

            return _interalStorage.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (KeysRemoved.Contains(item.Key))
                return false;

            if (_interalStorage.TryGetValue(item.Key, out var valueList))
                if (ValuesRemoved.Contains(item.Value))
                    return false;
                else
                    return valueList.Contains(item.Value);

            return false;
        }

        public bool Contains(TKey key, TValue item)
        {
            if (KeysRemoved.Contains(key))
                return false;

            if (!_interalStorage.ContainsKey(key)) 
                return false;

            if (ValuesRemoved.Contains(item))
                return false;

            return _interalStorage[key].Contains(item);
        }

        public List<TValue> LookupByKey(TKey key)
        {
            if (!KeysRemoved.Contains(key))
                if (_interalStorage.TryGetValue(key, out var values))
                    return values;

            return _emptyList.Cast<TValue>().ToList();
        }

        public List<TValue> LookupByKey(object key)
        {
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));

            if (!KeysRemoved.Contains(newkey))
                if (_interalStorage.ContainsKey(newkey))
                    return _interalStorage[newkey];

            return _emptyList.Cast<TValue>().ToList();
        }

        public List<TValue> this[TKey key]
        {
            get
            {
                if (KeysRemoved.Contains(key) || !_interalStorage.TryGetValue(key, out var val))
                    return _emptyList.Cast<TValue>().ToList();

                return val;
            }
            set
            {
                Set(key, value);
            }
        }

        public IEnumerable<TKey> Keys => _interalStorage.Keys.Where(k => KeysRemoved.Contains(k));

        public IEnumerable<TValue> Values
        {
            get
            {
                Interlocked.Increment(ref SyncObj);

                foreach (var item in _interalStorage)
                    if (!KeysRemoved.Contains(item.Key))
                        foreach (var li in item.Value)
                            if (!ValuesRemoved.Contains(li))
                                yield return li;

                ItteratingComplete();
                
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> KeyValueList
        {
            get
            {
                Interlocked.Increment(ref SyncObj);

                foreach (var pair in _interalStorage)
                    if (!KeysRemoved.Contains(pair.Key))
                        foreach (var value in pair.Value)
                            if (!ValuesRemoved.Contains(value))
                                yield return new KeyValuePair<TKey, TValue>(pair.Key, value);

                ItteratingComplete();
            }
        }

        internal void ItteratingComplete()
        {
            Interlocked.Decrement(ref SyncObj);

            if (SyncObj == 0)
            {
                _completing = true;
                while (_actionQueue.Count > 0)
                {
                    var action = _actionQueue.Dequeue();
                    action.Invoke();
                }
                KeysRemoved.Clear();
                ValuesRemoved.Clear();
                _completing = false;
            }
        }

        public void Clear()
        {
            if (Itterating)
            {
                foreach (var key in Keys)
                    KeysRemoved.Add(key);

                _actionQueue.Enqueue(Clear);
                return;
            }

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