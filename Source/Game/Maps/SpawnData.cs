// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
