// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class PlayerChoiceResponseRewardEntry
    {
        public PlayerChoiceResponseRewardEntry(uint id, int quantity)
        {
            Id = id;
            Quantity = quantity;
        }

        public uint Id { get; set; }
        public int Quantity { get; set; }
    }
}