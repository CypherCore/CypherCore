// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Loots
{
    public class PlayerRollVote
    {
        public byte RollNumber { get; set; }
        public RollVote Vote { get; set; }

        public PlayerRollVote()
        {
            Vote = RollVote.NotValid;
            RollNumber = 0;
        }
    }
}