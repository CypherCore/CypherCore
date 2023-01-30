// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    internal class BattlegroundABScore : BattlegroundScore
    {
        private uint _basesAssaulted;
        private uint _basesDefended;

        public BattlegroundABScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
            _basesAssaulted = 0;
            _basesDefended = 0;
        }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.BasesAssaulted:
                    _basesAssaulted += value;

                    break;
                case ScoreType.BasesDefended:
                    _basesDefended += value;

                    break;
                default:
                    base.UpdateScore(type, value);

                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)ABObjectives.AssaultBase, _basesAssaulted));
            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)ABObjectives.DefendBase, _basesDefended));
        }

        public override uint GetAttr1()
        {
            return _basesAssaulted;
        }

        public override uint GetAttr2()
        {
            return _basesDefended;
        }
    }

}