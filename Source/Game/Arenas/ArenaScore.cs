// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Arenas
{
    public class ArenaTeamScore
    {
        public void Assign(uint preMatchRating, uint postMatchRating, uint preMatchMMR, uint postMatchMMR)
        {
            PreMatchRating = preMatchRating;
            PostMatchRating = postMatchRating;
            PreMatchMMR = preMatchMMR;
            PostMatchMMR = postMatchMMR;
        }

        public uint PreMatchRating;
        public uint PostMatchRating;
        public uint PreMatchMMR;
        public uint PostMatchMMR;
    }
}
