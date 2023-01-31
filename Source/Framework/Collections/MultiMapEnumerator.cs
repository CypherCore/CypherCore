// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.Collections.Generic
{
    public class MultiMapEnumerator<TKey, TValue> : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private IEnumerator<TKey> _keyEnumerator;
        private MultiMap<TKey, TValue> _map;
        private IEnumerator<TValue> _valueEnumerator;

        public MultiMapEnumerator(MultiMap<TKey, TValue> map)
        {
            _map = map;
            Reset();
        }

        object IEnumerator.Current => Current;

        public KeyValuePair<TKey, TValue> Current => new(_keyEnumerator.Current, _valueEnumerator.Current);

        public void Dispose()
        {
            _keyEnumerator = null;
            _valueEnumerator = null;
            _map = null;
        }

        public bool MoveNext()
        {
            while (true)
                if (!_valueEnumerator.MoveNext())
                {
                    while (true)
                        if (!_keyEnumerator.MoveNext())
                        {
                            _map.ItteratingComplete();
                            return false;
                        }
                        else if (!_map.KeysRemoved.Contains(_keyEnumerator.Current))
                            break;

                    _valueEnumerator = _map[_keyEnumerator.Current].GetEnumerator();
                    _valueEnumerator.MoveNext();

                    if (!_map.ValuesRemoved.Contains(_valueEnumerator.Current))
                        return true;
                }
                else if (!_map.ValuesRemoved.Contains(_valueEnumerator.Current))
                    break;
        

            return true;
        }

        public void Reset()
        {
            Interlocked.Increment(ref _map.SyncObj);
            _keyEnumerator = _map.Keys.GetEnumerator();
            _valueEnumerator = new List<TValue>().GetEnumerator();
        }
    }
}