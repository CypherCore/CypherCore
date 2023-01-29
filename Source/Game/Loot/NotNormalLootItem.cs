// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Loots
{
    public class NotNormalLootItem
    {
        public bool Is_looted { get; set; }
        public byte LootListId { get; set; } // position in quest_items or items;

        public NotNormalLootItem()
        {
            LootListId = 0;
            Is_looted = false;
        }

        public NotNormalLootItem(byte _index, bool _islooted = false)
        {
            LootListId = _index;
            Is_looted = _islooted;
        }
    }
}