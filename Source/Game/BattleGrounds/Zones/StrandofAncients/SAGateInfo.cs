// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.StrandofAncients
{
    internal class SAGateInfo
    {
        public uint DamagedText { get; set; }
        public uint DestroyedText { get; set; }
        public uint GameObjectId { get; set; }

        public uint GateId { get; set; }
        public uint WorldState { get; set; }

        public SAGateInfo(uint gateId, uint gameObjectId, uint worldState, uint damagedText, uint destroyedText)
        {
            GateId = gateId;
            GameObjectId = gameObjectId;
            WorldState = worldState;
            DamagedText = damagedText;
            DestroyedText = destroyedText;
        }
    }

}