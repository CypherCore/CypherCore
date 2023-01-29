// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    internal struct ABNodeInfo
    {
        public ABNodeInfo(uint nodeId, uint textAllianceAssaulted, uint textHordeAssaulted, uint textAllianceTaken, uint textHordeTaken, uint textAllianceDefended, uint textHordeDefended, uint textAllianceClaims, uint textHordeClaims)
        {
            NodeId = nodeId;
            TextAllianceAssaulted = textAllianceAssaulted;
            TextHordeAssaulted = textHordeAssaulted;
            TextAllianceTaken = textAllianceTaken;
            TextHordeTaken = textHordeTaken;
            TextAllianceDefended = textAllianceDefended;
            TextHordeDefended = textHordeDefended;
            TextAllianceClaims = textAllianceClaims;
            TextHordeClaims = textHordeClaims;
        }

        public uint NodeId;
        public uint TextAllianceAssaulted;
        public uint TextHordeAssaulted;
        public uint TextAllianceTaken;
        public uint TextHordeTaken;
        public uint TextAllianceDefended;
        public uint TextHordeDefended;
        public uint TextAllianceClaims;
        public uint TextHordeClaims;
    }

}