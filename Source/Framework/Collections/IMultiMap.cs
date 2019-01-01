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
