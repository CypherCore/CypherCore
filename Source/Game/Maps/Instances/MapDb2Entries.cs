// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using InstanceLockKey = System.Tuple<uint, uint>;

namespace Game.Maps
{
    public struct MapDb2Entries
    {
        public MapRecord Map;
        public MapDifficultyRecord MapDifficulty;

        public MapDb2Entries(uint mapId, Difficulty difficulty)
        {
            Map = CliDB.MapStorage.LookupByKey(mapId);
            MapDifficulty = Global.DB2Mgr.GetMapDifficultyData(mapId, difficulty);
        }

        public MapDb2Entries(MapRecord map, MapDifficultyRecord mapDifficulty)
        {
            Map = map;
            MapDifficulty = mapDifficulty;
        }

        public InstanceLockKey GetKey()
        {
            return Tuple.Create(MapDifficulty.MapID, (uint)MapDifficulty.LockID);
        }

        public bool IsInstanceIdBound()
        {
            return !Map.IsFlexLocking() && !MapDifficulty.IsUsingEncounterLocks();
        }
    }
}