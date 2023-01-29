/*
 * Copyright (C) 2012-2016 CypherCore <http://github.com/CypherCore>
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

namespace Game.BattleGrounds.Zones.EyeOfTheStorm
{
    internal class BgEyeOfStormScore : BattlegroundScore
    {
        private uint _flagCaptures;

        public BgEyeOfStormScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
        }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.FlagCaptures: // Flags captured
                    _flagCaptures += value;

                    break;
                default:
                    base.UpdateScore(type, value);

                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)EotSMisc.OBJECTIVE_CAPTURE_FLAG, _flagCaptures));
        }

        public override uint GetAttr1()
        {
            return _flagCaptures;
        }
    }
}