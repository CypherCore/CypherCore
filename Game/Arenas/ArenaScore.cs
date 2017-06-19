/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Arenas
{
    class ArenaScore : BattlegroundScore
    {
        public ArenaScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
            TeamId = (int)(team == Team.Alliance ? BattlegroundTeamId.Alliance : BattlegroundTeamId.Horde);
        }

        public override void BuildObjectivesBlock(List<int> stats) { }

        // For Logging purpose
        public override string ToString()
        {
            return string.Format("Damage done: {0}, Healing done: {1}, Killing blows: {2}", DamageDone, HealingDone, KillingBlows);
        }
    }

    public class ArenaTeamScore
    {
        void Reset()
        {
            OldRating = 0;
            NewRating = 0;
            MatchmakerRating = 0;
        }

        public void Assign(int oldRating, int newRating, uint matchMakerRating)
        {
            OldRating = oldRating;
            NewRating = newRating;
            MatchmakerRating = matchMakerRating;
        }

        public int OldRating;
        public int NewRating;
        public uint MatchmakerRating;
    }
}
