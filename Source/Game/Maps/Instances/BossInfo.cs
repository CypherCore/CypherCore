// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Maps
{
    public class BossInfo
    {
        public List<AreaBoundary> Boundary { get; set; } = new();
        public List<ObjectGuid>[] Door { get; set; } = new List<ObjectGuid>[(int)DoorType.Max];
        public DungeonEncounterRecord[] DungeonEncounters { get; set; } = new DungeonEncounterRecord[MapConst.MaxDungeonEncountersPerBoss];
        public List<ObjectGuid> Minion { get; set; } = new();
        public EncounterState State;

        public BossInfo()
        {
            State = EncounterState.ToBeDecided;

            for (var i = 0; i < (int)DoorType.Max; ++i)
                Door[i] = new List<ObjectGuid>();
        }

        public DungeonEncounterRecord GetDungeonEncounterForDifficulty(Difficulty difficulty)
        {
            return DungeonEncounters.FirstOrDefault(dungeonEncounter => dungeonEncounter?.DifficultyID == 0 || (Difficulty)dungeonEncounter?.DifficultyID == difficulty);
        }
    }
}