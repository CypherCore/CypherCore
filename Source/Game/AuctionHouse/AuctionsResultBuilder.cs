// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Game
{
    internal class AuctionsResultBuilder<T>
	{
		private bool _hasMoreResults;
		private readonly List<T> _items = new();
		private readonly AuctionHouseResultLimits _maxResults;
		private readonly uint _offset;
		private readonly IComparer<T> _sorter;

		public AuctionsResultBuilder(uint offset, IComparer<T> sorter, AuctionHouseResultLimits maxResults)
		{
			_offset         = offset;
			_sorter         = sorter;
			_maxResults     = maxResults;
			_hasMoreResults = false;
		}

		public void AddItem(T item)
		{
			var index = _items.BinarySearch(item, _sorter);

			if (index < 0)
				index = ~index;

			_items.Insert(index, item);

			if (_items.Count > (int)_maxResults + _offset)
			{
				_items.RemoveAt(_items.Count - 1);
				_hasMoreResults = true;
			}
		}

		public Span<T> GetResultRange()
		{
			Span<T> h = _items.ToArray();

			return h[(int)_offset..];
		}

		public bool HasMoreResults()
		{
			return _hasMoreResults;
		}
	}
}