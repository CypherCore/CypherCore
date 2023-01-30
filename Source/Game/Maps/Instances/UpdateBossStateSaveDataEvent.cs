// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Maps
{
    public struct UpdateBossStateSaveDataEvent
    {
        public DungeonEncounterRecord DungeonEncounter;
        public uint BossId;
        public EncounterState NewState;

        public UpdateBossStateSaveDataEvent(DungeonEncounterRecord dungeonEncounter, uint bossId, EncounterState state)
        {
            DungeonEncounter = dungeonEncounter;
            BossId = bossId;
            NewState = state;
        }
    }
}