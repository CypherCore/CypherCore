using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Collections
{
    public class ManyToOneLookup<TKey, TValue>
    {
        ulong _index;
        private Dictionary<TValue, ulong> _values;
        private Dictionary<ulong, TValue> _valuesMap = new();
        private MultiMap<TKey, ulong> _keys;
        private MultiMap<ulong, TKey> _keysMap = new();

        public ManyToOneLookup()
        {
            _values = new();
            _keys = new();
        }

        public ManyToOneLookup(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer = null)
        {
            if (keyComparer != null)
                _keys = new MultiMap<TKey, ulong>(keyComparer);
            else
                _keys = new();

            if (valueComparer != null)
                _values = new(valueComparer);
            else
                _values = new();
        }

        public void Add(TKey key, TValue value)
        {
            if (_keys.TryGetValue(key, out var indexes)) // Check if the key exists
            {
                if (_values.ContainsKey(value)) // we already have this mapping
                    return;

                // key exists but the value does not, we incriement the index and add the new value
                _index++;
                indexes.Add(_index);
                _keysMap.Add(_index, key);
                _values.Add(value, _index);
                _valuesMap.Add(_index, value);
            }
            else if (_values.TryGetValue(value, out var valIndex)) // check if the value exists
            {
                _keys.Add(key, valIndex); // value index already exists, just map key to the existing index.
                _keysMap.Add(valIndex, key);
            }
            else // if key or value is unknown add as new
            {
                _index++;
                _keys.Add(key, _index);
                _keysMap.Add(_index, key);
                _values.Add(value, _index);
                _valuesMap.Add(_index, value);
            }
        }

        public void Remove(TKey key)
        {
            if (_keys.TryGetValue(key, out var indexes)) // check if we have this key known, get all known value indexs
            {
                _keys.Remove(key);

                foreach(var index in indexes) // go through each value index and remove them
                {
                    _keysMap.Remove(index, key);

                    // The multimap will delete the key on Remove() if there are no items left in the list
                    // this means the index is no longer mapped to a value, we need to remove the value
                    if (!_keysMap.ContainsKey(index))
                    {
                        if (_valuesMap.TryGetValue(index, out var valIndex)) // this should never fail.
                        {
                            _valuesMap.Remove(index);
                            _values.Remove(valIndex);
                        }
                        else
                            Log.outError(LogFilter.Misc, $"ManyToOneLookup: Unknown index {index} for key type {typeof(TKey)} and value type {typeof(TValue)}.");
                    }
                }
            }
        }

        public void Remove(TValue value)
        {
            if (_values.TryGetValue(value, out var valIndex))
            {
                _values.Remove(value);
                _valuesMap.Remove(valIndex);
                
                if (_keysMap.TryGetValue(valIndex, out var mappedKeys))
                {
                    _keysMap.Remove(valIndex);

                    foreach (var key in mappedKeys)
                        _keys.Remove(key, valIndex);
                }    
            }
        }

        public bool TryGetValue(TKey key, out List<TValue> values)
        {
            values = new List<TValue>();
            if (_keys.TryGetValue(key, out var indexes))
                foreach(var indx in indexes)
                    if (_valuesMap.TryGetValue(indx, out var val))
                        values.Add(val);

            return values.Any();            
        }

        public bool Contains(TValue value)
        {
            return _values.ContainsKey(value);
        }

        public bool Contains(TKey key)
        {
            return _keys.ContainsKey(key);
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                return _values.Keys;
            }
        }

        public IEnumerable<TKey> Keys
        {
            get
            {
                return _keys.Keys;
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> KeyValuePairs
        {
            get
            {
                foreach (var key in _keysMap.KeyValueList)
                    if (_valuesMap.TryGetValue(key.Key, out var val))
                        yield return new KeyValuePair<TKey, TValue>(key.Value, val);
            }
        }

        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                if (_keys.TryGetValue(key, out var indexes))
                    foreach (var inx in indexes)
                        if (_valuesMap.TryGetValue(inx, out var val))
                            yield return val;
            }
            set
            {
                foreach (var val in value)
                    Add(key, val);
            }
        }

        public void Clear() 
        { 
            _values.Clear();
            _keys.Clear();
            _keysMap.Clear();
            _valuesMap.Clear();
        }
    }
}
