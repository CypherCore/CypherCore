﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds
{
    public class BattlegroundScore
    {
        public BattlegroundScore(ObjectGuid playerGuid, Team team)
        {
            PlayerGuid = playerGuid;
            TeamId = (int)(team == Team.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
        }

        public virtual void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.KillingBlows:
                    KillingBlows += value;
                    break;
                case ScoreType.Deaths:
                    Deaths += value;
                    break;
                case ScoreType.HonorableKills:
                    HonorableKills += value;
                    break;
                case ScoreType.BonusHonor:
                    BonusHonor += value;
                    break;
                case ScoreType.DamageDone:
                    DamageDone += value;
                    break;
                case ScoreType.HealingDone:
                    HealingDone += value;
                    break;
                default:
                    Cypher.Assert(false, "Not implemented Battleground score Type!");
                    break;
            }
        }

        public virtual void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            playerData = new PVPMatchStatistics.PVPMatchPlayerStatistics();
            playerData.PlayerGUID = PlayerGuid;
            playerData.Kills = KillingBlows;
            playerData.Faction = (byte)TeamId;
            if (HonorableKills != 0 || Deaths != 0 || BonusHonor != 0)
            {
                PVPMatchStatistics.HonorData playerDataHonor = new();
                playerDataHonor.HonorKills = HonorableKills;
                playerDataHonor.Deaths = Deaths;
                playerDataHonor.ContributionPoints = BonusHonor;
                playerData.Honor = playerDataHonor;
            }

            playerData.DamageDone = DamageDone;
            playerData.HealingDone = HealingDone;
        }

        public virtual uint GetAttr1() { return 0; }
        public virtual uint GetAttr2() { return 0; }
        public virtual uint GetAttr3() { return 0; }
        public virtual uint GetAttr4() { return 0; }
        public virtual uint GetAttr5() { return 0; }

        public ObjectGuid PlayerGuid;
        public int TeamId;

        // Default score, present in every Type
        public uint KillingBlows;
        public uint Deaths;
        public uint HonorableKills;
        public uint BonusHonor;
        public uint DamageDone;
        public uint HealingDone;
    }
}
