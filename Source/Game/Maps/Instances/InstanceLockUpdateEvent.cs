// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.DataStorage;

namespace Game.Maps
{
    public struct InstanceLockUpdateEvent
    {
        public uint InstanceId;
        public string NewData;
        public uint InstanceCompletedEncountersMask;
        public DungeonEncounterRecord CompletedEncounter;
        public uint? EntranceWorldSafeLocId;

        public InstanceLockUpdateEvent(uint instanceId, string newData, uint instanceCompletedEncountersMask, DungeonEncounterRecord completedEncounter, uint? entranceWorldSafeLocId)
        {
            InstanceId = instanceId;
            NewData = newData;
            InstanceCompletedEncountersMask = instanceCompletedEncountersMask;
            CompletedEncounter = completedEncounter;
            EntranceWorldSafeLocId = entranceWorldSafeLocId;
        }
    }
}