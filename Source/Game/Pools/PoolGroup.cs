// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;

namespace Game
{
    public class PoolGroup<T>
    {
        private readonly List<PoolObject> _equalChanced = new();
        private readonly List<PoolObject> _explicitlyChanced = new();

        private uint _poolId;

        public PoolGroup()
        {
            _poolId = 0;
        }

        public bool IsEmptyDeepCheck()
        {
            if (typeof(T).Name != "Pool")
                return IsEmpty();

            foreach (PoolObject explicitlyChanced in _explicitlyChanced)
                if (!Global.PoolMgr.IsEmpty((uint)explicitlyChanced.Guid))
                    return false;

            foreach (PoolObject equalChanced in _equalChanced)
                if (!Global.PoolMgr.IsEmpty((uint)equalChanced.Guid))
                    return false;

            return true;
        }

        public void AddEntry(PoolObject poolitem, uint maxentries)
        {
            if (poolitem.Chance != 0 &&
                maxentries == 1)
                _explicitlyChanced.Add(poolitem);
            else
                _equalChanced.Add(poolitem);
        }

        public bool CheckPool()
        {
            if (_equalChanced.Empty())
            {
                float chance = 0;

                for (int i = 0; i < _explicitlyChanced.Count; ++i)
                    chance += _explicitlyChanced[i].Chance;

                if (chance != 100 &&
                    chance != 0)
                    return false;
            }

            return true;
        }

        public void DespawnObject(SpawnedPoolData spawns, ulong guid = 0, bool alwaysDeleteRespawnTime = false)
        {
            for (int i = 0; i < _equalChanced.Count; ++i)
                // if spawned
                if (spawns.IsSpawnedObject<T>(_equalChanced[i].Guid))
                {
                    if (guid == 0 ||
                        _equalChanced[i].Guid == guid)
                    {
                        Despawn1Object(spawns, _equalChanced[i].Guid, alwaysDeleteRespawnTime);
                        spawns.RemoveSpawn<T>(_equalChanced[i].Guid, _poolId);
                    }
                }
                else if (alwaysDeleteRespawnTime)
                {
                    RemoveRespawnTimeFromDB(spawns, _equalChanced[i].Guid);
                }

            for (int i = 0; i < _explicitlyChanced.Count; ++i)
                // spawned
                if (spawns.IsSpawnedObject<T>(_explicitlyChanced[i].Guid))
                {
                    if (guid == 0 ||
                        _explicitlyChanced[i].Guid == guid)
                    {
                        Despawn1Object(spawns, _explicitlyChanced[i].Guid, alwaysDeleteRespawnTime);
                        spawns.RemoveSpawn<T>(_explicitlyChanced[i].Guid, _poolId);
                    }
                }
                else if (alwaysDeleteRespawnTime)
                {
                    RemoveRespawnTimeFromDB(spawns, _explicitlyChanced[i].Guid);
                }
        }

        public void RemoveOneRelation(uint child_pool_id)
        {
            if (typeof(T).Name != "Pool")
                return;

            foreach (var poolObject in _explicitlyChanced)
                if (poolObject.Guid == child_pool_id)
                {
                    _explicitlyChanced.Remove(poolObject);

                    break;
                }

            foreach (var poolObject in _equalChanced)
                if (poolObject.Guid == child_pool_id)
                {
                    _equalChanced.Remove(poolObject);

                    break;
                }
        }

        public void SpawnObject(SpawnedPoolData spawns, uint limit, ulong triggerFrom)
        {
            int count = (int)(limit - spawns.GetSpawnedObjects(_poolId));

            // If triggered from some object respawn this object is still marked as spawned
            // and also counted into _SpawnedPoolAmount so we need increase Count to be
            // spawned by 1
            if (triggerFrom != 0)
                ++count;

            // This will try to spawn the rest of pool, not guaranteed
            if (count > 0)
            {
                List<PoolObject> rolledObjects = new();

                // roll objects to be spawned
                if (!_explicitlyChanced.Empty())
                {
                    float roll = (float)RandomHelper.randChance();

                    foreach (PoolObject obj in _explicitlyChanced)
                    {
                        roll -= obj.Chance;

                        // Triggering object is marked as spawned at this Time and can be also rolled (respawn case)
                        // so this need explicit check for this case
                        if (roll < 0 &&
                            (obj.Guid == triggerFrom || !spawns.IsSpawnedObject<T>(obj.Guid)))
                        {
                            rolledObjects.Add(obj);

                            break;
                        }
                    }
                }

                if (!_equalChanced.Empty() &&
                    rolledObjects.Empty())
                {
                    rolledObjects.AddRange(_equalChanced.Where(obj => obj.Guid == triggerFrom || !spawns.IsSpawnedObject<T>(obj.Guid)));
                    rolledObjects.RandomResize((uint)count);
                }

                // try to spawn rolled objects
                foreach (PoolObject obj in rolledObjects)
                    if (obj.Guid == triggerFrom)
                    {
                        ReSpawn1Object(spawns, obj);
                        triggerFrom = 0;
                    }
                    else
                    {
                        spawns.AddSpawn<T>(obj.Guid, _poolId);
                        Spawn1Object(spawns, obj);
                    }
            }

            // One spawn one despawn no Count increase
            if (triggerFrom != 0)
                DespawnObject(spawns, triggerFrom);
        }

        public void SetPoolId(uint pool_id)
        {
            _poolId = pool_id;
        }

        public bool IsEmpty()
        {
            return _explicitlyChanced.Empty() && _equalChanced.Empty();
        }

        public uint GetPoolId()
        {
            return _poolId;
        }

        private void Despawn1Object(SpawnedPoolData spawns, ulong guid, bool alwaysDeleteRespawnTime = false, bool saveRespawnTime = true)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    {
                        var creatureBounds = spawns.GetMap().GetCreatureBySpawnIdStore().LookupByKey(guid);

                        foreach (var creature in creatureBounds)
                        {
                            // For dynamic spawns, save respawn Time here
                            if (saveRespawnTime && !creature.GetRespawnCompatibilityMode())
                                creature.SaveRespawnTime();

                            creature.AddObjectToRemoveList();
                        }

                        if (alwaysDeleteRespawnTime)
                            spawns.GetMap().RemoveRespawnTime(SpawnObjectType.Creature, guid, null, true);

                        break;
                    }
                case "GameObject":
                    {
                        var gameobjectBounds = spawns.GetMap().GetGameObjectBySpawnIdStore().LookupByKey(guid);

                        foreach (var go in gameobjectBounds)
                        {
                            // For dynamic spawns, save respawn Time here
                            if (saveRespawnTime && !go.GetRespawnCompatibilityMode())
                                go.SaveRespawnTime();

                            go.AddObjectToRemoveList();
                        }

                        if (alwaysDeleteRespawnTime)
                            spawns.GetMap().RemoveRespawnTime(SpawnObjectType.GameObject, guid, null, true);

                        break;
                    }
                case "Pool":
                    Global.PoolMgr.DespawnPool(spawns, (uint)guid, alwaysDeleteRespawnTime);

                    break;
            }
        }

        private void Spawn1Object(SpawnedPoolData spawns, PoolObject obj)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    {
                        CreatureData data = Global.ObjectMgr.GetCreatureData(obj.Guid);

                        if (data != null)
                            // Spawn if necessary (loaded grids only)
                            // We use spawn coords to spawn
                            if (spawns.GetMap().IsGridLoaded(data.SpawnPoint))
                                Creature.CreateCreatureFromDB(obj.Guid, spawns.GetMap());
                    }

                    break;
                case "GameObject":
                    {
                        GameObjectData data = Global.ObjectMgr.GetGameObjectData(obj.Guid);

                        if (data != null)
                            // Spawn if necessary (loaded grids only)
                            // We use current coords to unspawn, not spawn coords since creature can have changed grid
                            if (spawns.GetMap().IsGridLoaded(data.SpawnPoint))
                            {
                                GameObject go = GameObject.CreateGameObjectFromDB(obj.Guid, spawns.GetMap(), false);

                                if (go && go.IsSpawnedByDefault())
                                    if (!spawns.GetMap().AddToMap(go))
                                        go.Dispose();
                            }
                    }

                    break;
                case "Pool":
                    Global.PoolMgr.SpawnPool(spawns, (uint)obj.Guid);

                    break;
            }
        }

        private void ReSpawn1Object(SpawnedPoolData spawns, PoolObject obj)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                case "GameObject":
                    Despawn1Object(spawns, obj.Guid, false, false);
                    Spawn1Object(spawns, obj);

                    break;
            }
        }

        private void RemoveRespawnTimeFromDB(SpawnedPoolData spawns, ulong guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    spawns.GetMap().RemoveRespawnTime(SpawnObjectType.Creature, guid, null, true);

                    break;
                case "GameObject":
                    spawns.GetMap().RemoveRespawnTime(SpawnObjectType.GameObject, guid, null, true);

                    break;
            }
        }
    }
}