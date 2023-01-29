// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class PlayerChoiceResponseRewardItem
    {
        public PlayerChoiceResponseRewardItem()
        {
        }

        public PlayerChoiceResponseRewardItem(uint id, List<uint> bonusListIDs, int quantity)
        {
            Id = id;
            BonusListIDs = bonusListIDs;
            Quantity = quantity;
        }

        public List<uint> BonusListIDs { get; set; } = new();

        public uint Id { get; set; }
        public int Quantity { get; set; }
    }
}