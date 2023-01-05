/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;

namespace Game.Maps
{
    public class SpawnGroupTemplateData
    {
        public uint groupId;
        public string name;
        public uint mapId;
        public SpawnGroupFlags flags;
    }

    public class SpawnData : SpawnMetadata
    {
        public uint Id; // entry in respective _template table
        public Position SpawnPoint;
        public PhaseUseFlagsValues PhaseUseFlags;
        public uint PhaseId;
        public uint PhaseGroup;
        public int terrainSwapMap;
        public uint poolId;
        public int spawntimesecs;
        public List<Difficulty> SpawnDifficulties;
        public uint ScriptId;
        public string StringId;

        public SpawnData(SpawnObjectType t) : base(t)
        {
            SpawnPoint = new Position();
            terrainSwapMap = -1;
            SpawnDifficulties = new List<Difficulty>();
        }

        public static SpawnObjectType TypeFor<T>()
        {
            switch (typeof(T).Name)
            {
                case nameof(Creature):
                    return SpawnObjectType.Creature;
                case nameof(GameObject):
                    return SpawnObjectType.GameObject;
                case nameof(AreaTrigger):
                    return SpawnObjectType.AreaTrigger;
                default:
                    return SpawnObjectType.NumSpawnTypes;
            }
        }
    }

    public class SpawnMetadata
    {
        public SpawnObjectType type;
        public ulong SpawnId;
        public uint MapId = 0xFFFFFFFF;
        public bool dbData = true;
        public SpawnGroupTemplateData spawnGroupData = null;

        public static bool TypeInMask(SpawnObjectType type, SpawnObjectTypeMask mask) { return ((1 << (int)type) & (int)mask) != 0; }
        public static bool TypeHasData(SpawnObjectType type) { return type < SpawnObjectType.NumSpawnTypesWithData; }
        public static bool TypeIsValid(SpawnObjectType type) { return type < SpawnObjectType.NumSpawnTypes; }

        public SpawnMetadata(SpawnObjectType t)
        {
            type = t;
        }

        public SpawnData ToSpawnData() { return TypeHasData(type) ? (SpawnData)this : null; }
    }
}
