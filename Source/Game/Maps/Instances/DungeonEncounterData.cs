// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public class DungeonEncounterData
    {
        public uint BossId { get; set; }
        public uint[] DungeonEncounterId { get; set; } = new uint[4];

        public DungeonEncounterData(uint bossId, params uint[] dungeonEncounterIds)
        {
            BossId = bossId;
            DungeonEncounterId = dungeonEncounterIds;
        }
    }
}