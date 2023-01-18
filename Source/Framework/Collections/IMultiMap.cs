// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace System.Collections.Generic
{
    public interface IMultiMap<TKey, TValue>
    {
        void AddRange(TKey key, IEnumerable<TValue> valueList);
        List<TValue> this[TKey key] { get; set;}
        bool Remove(TKey key, TValue value);
        void Add(TKey key, TValue value);
        bool ContainsKey(TKey key);

        ICollection<TKey> Keys {get;}
        bool Remove(TKey key);
        ICollection<TValue> Values{get;}

        void Add(KeyValuePair<TKey, TValue> item);
        void Clear();
        bool Contains(TKey key, TValue item);
        void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);
        int Count {get;}
        bool Remove(KeyValuePair<TKey, TValue> item);

        List<TValue> LookupByKey(TKey key);
    }
}
