/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using Game.Network.Packets;

namespace Game.Arenas
{
    class ArenaScore : BattlegroundScore
    {
        public ArenaScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
            TeamId = (int)(team == Team.Alliance ? BattlegroundTeamId.Alliance : BattlegroundTeamId.Horde);
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPLogData.PlayerData playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            if (PreMatchRating != 0)
                playerData.PreMatchRating.Set(PreMatchRating);

            if (PostMatchRating != PreMatchRating)
                playerData.RatingChange.Set((int)(PostMatchRating - PreMatchRating));

            if (PreMatchMMR != 0)
                playerData.PreMatchMMR.Set(PreMatchMMR);

            if (PostMatchMMR != PreMatchMMR)
                playerData.MmrChange.Set((int)(PostMatchMMR - PreMatchMMR));
        }

        // For Logging purpose
        public override string ToString()
        {
            return $"Damage done: {DamageDone} Healing done: {HealingDone} Killing blows: {KillingBlows} PreMatchRating: {PreMatchRating} " +
                $"PreMatchMMR: {PreMatchMMR} PostMatchRating: {PostMatchRating} PostMatchMMR: {PostMatchMMR}";
        }

        uint PreMatchRating;
        uint PreMatchMMR;
        uint PostMatchRating;
        uint PostMatchMMR;
    }

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
