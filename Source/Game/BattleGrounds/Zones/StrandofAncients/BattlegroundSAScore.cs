// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds.Zones.StrandofAncients
{
    internal class BattlegroundSAScore : BattlegroundScore
    {
        private uint _demolishersDestroyed;
        private uint _gatesDestroyed;

        public BattlegroundSAScore(ObjectGuid playerGuid, Team team) : base(playerGuid, team)
        {
        }

        public override void UpdateScore(ScoreType type, uint value)
        {
            switch (type)
            {
                case ScoreType.DestroyedDemolisher:
                    _demolishersDestroyed += value;

                    break;
                case ScoreType.DestroyedWall:
                    _gatesDestroyed += value;

                    break;
                default:
                    base.UpdateScore(type, value);

                    break;
            }
        }

        public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
        {
            base.BuildPvPLogPlayerDataPacket(out playerData);

            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SAObjectives.DemolishersDestroyed, _demolishersDestroyed));
            playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SAObjectives.GatesDestroyed, _gatesDestroyed));
        }

        public override uint GetAttr1()
        {
            return _demolishersDestroyed;
        }

        public override uint GetAttr2()
        {
            return _gatesDestroyed;
        }
    }

}