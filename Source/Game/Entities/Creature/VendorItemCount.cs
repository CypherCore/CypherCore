// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class VendorItemCount
    {
        public uint Count { get; set; }
        public uint ItemId { get; set; }
        public long LastIncrementTime { get; set; }

        public VendorItemCount(uint _item, uint _count)
        {
            ItemId = _item;
            Count = _count;
            LastIncrementTime = GameTime.GetGameTime();
        }
    }
}