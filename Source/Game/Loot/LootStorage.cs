// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Loots
{
    public class LootStorage
    {
        public static LootStore Creature { get; set; }
        public static LootStore Fishing { get; set; }
        public static LootStore Gameobject { get; set; }
        public static LootStore Items { get; set; }
        public static LootStore Mail { get; set; }
        public static LootStore Milling { get; set; }
        public static LootStore Pickpocketing { get; set; }
        public static LootStore Reference { get; set; }
        public static LootStore Skinning { get; set; }
        public static LootStore Disenchant { get; set; }
        public static LootStore Prospecting { get; set; }
        public static LootStore Spell { get; set; }
    }
}