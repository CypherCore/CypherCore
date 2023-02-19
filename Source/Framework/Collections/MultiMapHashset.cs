// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Google.Protobuf.WellKnownTypes;

namespace System.Collections.Generic
{
    public class MultiMapHashSet<TKey, TValue>
    {
        static HashSet<object> _emptyList = new HashSet<object>();
        public MultiMapHashSet() { }

        public MultiMapHashSet(IEqualityComparer<TKey> keyComparer)
        {
            _interalStorage = new Dictionary<TKey, HashSet<TValue>>(keyComparer);
        }

        public MultiMapHashSet(IEnumerable<KeyValuePair<TKey, TValue>> initialData)
        {
            foreach (var item in initialData)
            {
                Add(item);
            }
        }

        public void Add(TKey key, TValue value)
        {
            _interalStorage.AddToList(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public bool Remove(TKey key)
        {
            return _interalStorage.Remove(key);
        }


        public bool Remove(TKey key, TValue value)
        {
            return _interalStorage.Remove(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _interalStorage.ContainsKey(key);
        }

        public bool Contains(TKey key, TValue item)
        {
            if (!_interalStorage.ContainsKey(key)) return false;
            return _interalStorage[key].Contains(item);
        }

        public HashSet<TValue> LookupByKey(TKey key)
        {
            if (_interalStorage.TryGetValue(key, out var values))
                return values;

            return _emptyList.Cast<TValue>().ToHashSet();
        }

        public HashSet<TValue> LookupByKey(object key)
        {
            TKey newkey = (TKey)Convert.ChangeType(key, typeof(TKey));
            if (_interalStorage.TryGetValue(newkey, out var values))
                return values;

            return _emptyList.Cast<TValue>().ToHashSet();
        }

        public bool TryGetValue(TKey key, out HashSet<TValue> value)
        {
            return _interalStorage.TryGetValue(key, out value);
        }

        public HashSet<TValue> this[TKey key]
        {
            get
            {
                if (!_interalStorage.TryGetValue(key, out var val))
                    return _emptyList.Cast<TValue>().ToHashSet();
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

        public void Clear()
        {
            _interalStorage.Clear();
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

        private Dictionary<TKey, HashSet<TValue>> _interalStorage = new();
    }
    
}
