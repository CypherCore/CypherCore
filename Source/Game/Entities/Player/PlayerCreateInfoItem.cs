// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class PlayerCreateInfoItem
    {
        public uint ItemAmount { get; set; }

        public uint ItemId { get; set; }

        public PlayerCreateInfoItem(uint id, uint amount)
        {
            ItemId = id;
            ItemAmount = amount;
        }
    }
}