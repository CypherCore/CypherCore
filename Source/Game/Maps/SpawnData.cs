// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps
{
    public class SpawnGroupTemplateData
    {
        public SpawnGroupFlags flags;
        public uint groupId;
        public uint mapId;
        public string name;
    }

    public class SpawnData : SpawnMetadata
    {
        public uint Id; // entry in respective _template table
        public uint PhaseGroup;
        public uint PhaseId;
        public PhaseUseFlagsValues PhaseUseFlags;
        public uint poolId;
        public uint ScriptId;
        public List<Difficulty> SpawnDifficulties;
        public Position SpawnPoint;
        public int spawntimesecs;
        public string StringId;
        public int terrainSwapMap;

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
        public bool dbData = true;
        public uint MapId = 0xFFFFFFFF;
        public SpawnGroupTemplateData spawnGroupData = null;
        public ulong SpawnId;
        public SpawnObjectType type;

        public SpawnMetadata(SpawnObjectType t)
        {
            type = t;
        }

        public static bool TypeInMask(SpawnObjectType type, SpawnObjectTypeMask mask)
        {
            return ((1 << (int)type) & (int)mask) != 0;
        }

        public static bool TypeHasData(SpawnObjectType type)
        {
            return type < SpawnObjectType.NumSpawnTypesWithData;
        }

        public static bool TypeIsValid(SpawnObjectType type)
        {
            return type < SpawnObjectType.NumSpawnTypes;
        }

        public SpawnData ToSpawnData()
        {
            return TypeHasData(type) ? (SpawnData)this : null;
        }
    }
}