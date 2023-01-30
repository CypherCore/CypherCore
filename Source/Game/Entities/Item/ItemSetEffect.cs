// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game.Entities
{
    public class ItemSetEffect
    {
        public List<Item> EquippedItems { get; set; } = new();
        public uint ItemSetID { get; set; }
        public List<ItemSetSpellRecord> SetBonuses { get; set; } = new();
    }
}