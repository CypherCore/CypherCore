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
    public class MultiMapEnumerator<TKey, TValue> : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        MultiMap<TKey, TValue> _map;
        IEnumerator<TKey> _keyEnumerator;
        IEnumerator<TValue> _valueEnumerator;

        public MultiMapEnumerator(MultiMap<TKey, TValue> map)
        {
            _map = map;
            Reset();
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                return new KeyValuePair<TKey, TValue>(_keyEnumerator.Current, _valueEnumerator.Current);
            }
        }

        public void Dispose()
        {
            _keyEnumerator = null;
            _valueEnumerator = null;
            _map = null;
        }

        public bool MoveNext()
        {
            if (!_valueEnumerator.MoveNext())
            {
                if (!_keyEnumerator.MoveNext())
                    return false;
                _valueEnumerator = _map[_keyEnumerator.Current].GetEnumerator();
                _valueEnumerator.MoveNext();
                return true;
            }
            return true;
        }

        public void Reset()
        {
            _keyEnumerator = _map.Keys.GetEnumerator();
            _valueEnumerator = new List<TValue>().GetEnumerator();
        }
    }

    public class SortedMultiMapEnumerator<TKey, TValue> : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        SortedMultiMap<TKey, TValue> _map;
        IEnumerator<TKey> _keyEnumerator;
        IEnumerator<TValue> _valueEnumerator;

        public SortedMultiMapEnumerator(SortedMultiMap<TKey, TValue> map)
        {
            _map = map;
            Reset();
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                return new KeyValuePair<TKey, TValue>(_keyEnumerator.Current, _valueEnumerator.Current);
            }
        }

        public void Dispose()
        {
            _keyEnumerator = null;
            _valueEnumerator = null;
            _map = null;
        }

        public bool MoveNext()
        {
            if (!_valueEnumerator.MoveNext())
            {
                if (!_keyEnumerator.MoveNext())
                    return false;
                _valueEnumerator = _map[_keyEnumerator.Current].GetEnumerator();
                _valueEnumerator.MoveNext();
                return true;
            }
            return true;
        }

        public void Reset()
        {
            _keyEnumerator = _map.Keys.GetEnumerator();
            _valueEnumerator = new List<TValue>().GetEnumerator();
        }
    }
}
