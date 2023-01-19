// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Arenas
{
    class ArenaScore : BattlegroundScore
    {
        public ArenaScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
            TeamId = (int)(team == Team.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            if (PreMatchRating != 0)
                playerData.PreMatchRating = PreMatchRating;

            if (PostMatchRating != PreMatchRating)
                playerData.RatingChange = (int)(PostMatchRating - PreMatchRating);

            if (PreMatchMMR != 0)
                playerData.PreMatchMMR = PreMatchMMR;

            if (PostMatchMMR != PreMatchMMR)
                playerData.MmrChange = (int)(PostMatchMMR - PreMatchMMR);
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
