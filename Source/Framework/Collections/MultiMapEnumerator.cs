// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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

		public KeyValuePair<TKey, TValue> Current => new KeyValuePair<TKey, TValue>(_keyEnumerator.Current, _valueEnumerator.Current);

		public void Dispose()
		{
			_keyEnumerator   = null;
			_valueEnumerator = null;
			_map             = null;
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
			_keyEnumerator   = _map.Keys.GetEnumerator();
			_valueEnumerator = new List<TValue>().GetEnumerator();
		}
	}
}