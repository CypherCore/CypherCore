// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Maps;

namespace Game
{
    public class SpawnedPoolData
    {
        private readonly Map _owner;
        private readonly List<ulong> _spawnedCreatures = new();
        private readonly List<ulong> _spawnedGameobjects = new();
        private readonly Dictionary<ulong, uint> _spawnedPools = new();

        public SpawnedPoolData(Map owner)
        {
            _owner = owner;
        }

        public uint GetSpawnedObjects(uint pool_id)
        {
            return _spawnedPools.LookupByKey(pool_id);
        }

        public bool IsSpawnedObject<T>(ulong db_guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    return _spawnedCreatures.Contains(db_guid);
                case "GameObject":
                    return _spawnedGameobjects.Contains(db_guid);
                case "Pool":
                    return _spawnedPools.ContainsKey(db_guid);
                default:
                    return false;
            }
        }

        public bool IsSpawnedObject(SpawnObjectType type, ulong db_guid_or_pool_id)
        {
            switch (type)
            {
                case SpawnObjectType.Creature:
                    return _spawnedCreatures.Contains(db_guid_or_pool_id);
                case SpawnObjectType.GameObject:
                    return _spawnedGameobjects.Contains(db_guid_or_pool_id);
                default:
                    Log.outFatal(LogFilter.Misc, $"Invalid spawn Type {type} passed to SpawnedPoolData::IsSpawnedObject (with spawnId {db_guid_or_pool_id})");

                    return false;
            }
        }

        public void AddSpawn<T>(ulong db_guid, uint pool_id)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    _spawnedCreatures.Add(db_guid);

                    break;
                case "GameObject":
                    _spawnedGameobjects.Add(db_guid);

                    break;
                case "Pool":
                    _spawnedPools[db_guid] = 0;

                    break;
                default:
                    return;
            }

            if (!_spawnedPools.ContainsKey(pool_id))
                _spawnedPools[pool_id] = 0;

            ++_spawnedPools[pool_id];
        }

        public void RemoveSpawn<T>(ulong db_guid, uint pool_id)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    _spawnedCreatures.Remove(db_guid);

                    break;
                case "GameObject":
                    _spawnedGameobjects.Remove(db_guid);

                    break;
                case "Pool":
                    _spawnedPools.Remove(db_guid);

                    break;
                default:
                    return;
            }

            if (_spawnedPools[pool_id] > 0)
                --_spawnedPools[pool_id];
        }

        public Map GetMap()
        {
            return _owner;
        }
    }
}