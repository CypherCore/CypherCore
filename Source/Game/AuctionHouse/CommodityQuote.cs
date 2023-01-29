// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;

namespace Game
{
    public class CommodityQuote
	{
		public uint Quantity { get; set; }
		public ulong TotalPrice { get; set; }
        public DateTime ValidTo = DateTime.MinValue;
	}
}