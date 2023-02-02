// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace System.Collections.Generic
{
    public interface IMultiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        List<TValue> this[TKey key] { get; set; }

        int Count { get; }
        ICollection<TKey> Keys { get; }
        IEnumerable<KeyValuePair<TKey, TValue>> KeyValueList { get; }
        ICollection<TValue> Values { get; }

        void Add(KeyValuePair<TKey, TValue> item);
        void Add(TKey key, TValue value);
        void AddRange(TKey key, IEnumerable<TValue> valueList);
        void Clear();
        bool Contains(KeyValuePair<TKey, TValue> item);
        bool Contains(TKey key, TValue item);
        bool ContainsKey(TKey key);
        void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);
        List<TValue> LookupByKey(object key);
        List<TValue> LookupByKey(TKey key);
        bool Remove(KeyValuePair<TKey, TValue> item);
        bool Remove(TKey key);
        bool Remove(TKey key, TValue value);
        bool Empty();
    }
}