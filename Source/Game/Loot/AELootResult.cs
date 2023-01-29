// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Loots
{
    public class AELootResult
    {
        public struct ResultValue
        {
            public Item Item;
            public byte Count;
            public LootType LootType;
            public uint DungeonEncounterId;
        }

        private readonly Dictionary<Item, int> _byItem = new();

        private readonly List<ResultValue> _byOrder = new();

        public void Add(Item item, byte count, LootType lootType, uint dungeonEncounterId)
        {
            var id = _byItem.LookupByKey(item);

            if (id != 0)
            {
                var resultValue = _byOrder[id];
                resultValue.Count += count;
            }
            else
            {
                _byItem[item] = _byOrder.Count;
                ResultValue value;
                value.Item = item;
                value.Count = count;
                value.LootType = lootType;
                value.DungeonEncounterId = dungeonEncounterId;
                _byOrder.Add(value);
            }
        }

        public List<ResultValue> GetByOrder()
        {
            return _byOrder;
        }
    }
}