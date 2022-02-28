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
using Framework.Database;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class PoolManager : Singleton<PoolManager>
    {
        PoolManager() { }

        public void Initialize()
        {
            mGameobjectSearchMap.Clear();
            mCreatureSearchMap.Clear();
        }

        public void LoadFromDB()
        {
            // Pool templates
            {
                uint oldMSTime = Time.GetMSTime();

                SQLResult result = DB.World.Query("SELECT entry, max_limit FROM pool_template");
                if (result.IsEmpty())
                {
                    mPoolTemplate.Clear();
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 object pools. DB table `pool_template` is empty.");
                    return;
                }

                uint count = 0;
                do
                {
                    uint pool_id = result.Read<uint>(0);

                    PoolTemplateData pPoolTemplate = new();
                    pPoolTemplate.MaxLimit = result.Read<uint>(1);
                    mPoolTemplate[pool_id] = pPoolTemplate;
                    ++count;
                }
                while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} objects pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }

            // Creatures

            Log.outInfo(LogFilter.ServerLoading, "Loading Creatures Pooling Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                         1        2            3
                SQLResult result = DB.World.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 0");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creatures in  pools. DB table `pool_creature` is empty.");
                }
                else
                {
                    uint count = 0;
                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        uint pool_id = result.Read<uint>(1);
                        float chance = result.Read<float>(2);

                        CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`pool_creature` has a non existing creature spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, pool_id);
                            continue;
                        }
                        if (!mPoolTemplate.ContainsKey(pool_id))
                        {
                            Log.outError(LogFilter.Sql, "`pool_creature` pool id ({0}) is not in `pool_template`, skipped.", pool_id);
                            continue;
                        }
                        if (chance < 0 || chance > 100)
                        {
                            Log.outError(LogFilter.Sql, "`pool_creature` has an invalid chance ({0}) for creature guid ({1}) in pool id ({2}), skipped.", chance, guid, pool_id);
                            continue;
                        }
                        PoolTemplateData pPoolTemplate = mPoolTemplate[pool_id];
                        PoolObject plObject = new(guid, chance);

                        if (!mPoolCreatureGroups.ContainsKey(pool_id))
                            mPoolCreatureGroups[pool_id] = new PoolGroup<Creature>();

                        PoolGroup<Creature> cregroup = mPoolCreatureGroups[pool_id];
                        cregroup.SetPoolId(pool_id);
                        cregroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

                        mCreatureSearchMap.Add(guid, pool_id);
                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creatures in pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            // Gameobjects

            Log.outInfo(LogFilter.ServerLoading, "Loading Gameobject Pooling Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                         1        2            3
                SQLResult result = DB.World.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 1");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 gameobjects in  pools. DB table `pool_gameobject` is empty.");
                }
                else
                {
                    uint count = 0;
                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        uint pool_id = result.Read<uint>(1);
                        float chance = result.Read<float>(2);

                        GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);
                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`pool_gameobject` has a non existing gameobject spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, pool_id);
                            continue;
                        }

                        GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(data.Id);
                        if (goinfo.type != GameObjectTypes.Chest &&
                            goinfo.type != GameObjectTypes.FishingHole &&
                            goinfo.type != GameObjectTypes.GatheringNode &&
                            goinfo.type != GameObjectTypes.Goober)
                        {
                            Log.outError(LogFilter.Sql, "`pool_gameobject` has a not lootable gameobject spawn (GUID: {0}, type: {1}) defined for pool id ({2}), skipped.", guid, goinfo.type, pool_id);
                            continue;
                        }

                        if (!mPoolTemplate.ContainsKey(pool_id))
                        {
                            Log.outError(LogFilter.Sql, "`pool_gameobject` pool id ({0}) is not in `pool_template`, skipped.", pool_id);
                            continue;
                        }

                        if (chance < 0 || chance > 100)
                        {
                            Log.outError(LogFilter.Sql, "`pool_gameobject` has an invalid chance ({0}) for gameobject guid ({1}) in pool id ({2}), skipped.", chance, guid, pool_id);
                            continue;
                        }

                        PoolTemplateData pPoolTemplate = mPoolTemplate[pool_id];
                        PoolObject plObject = new(guid, chance);

                        if (!mPoolGameobjectGroups.ContainsKey(pool_id))
                            mPoolGameobjectGroups[pool_id] = new PoolGroup<GameObject>();

                        PoolGroup<GameObject> gogroup = mPoolGameobjectGroups[pool_id];
                        gogroup.SetPoolId(pool_id);
                        gogroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

                        mGameobjectSearchMap.Add(guid, pool_id);
                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} gameobject in pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            // Pool of pools

            Log.outInfo(LogFilter.ServerLoading, "Loading Mother Pooling Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                         1        2            3
                SQLResult result = DB.World.Query("SELECT spawnId, poolSpawnId, chance FROM pool_members WHERE type = 2");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 pools in pools");
                }
                else
                {
                    uint count = 0;
                    do
                    {
                        uint child_pool_id = result.Read<uint>(0);
                        uint mother_pool_id = result.Read<uint>(1);
                        float chance = result.Read<float>(2);

                        if (!mPoolTemplate.ContainsKey(mother_pool_id))
                        {
                            Log.outError(LogFilter.Sql, "`pool_pool` mother_pool id ({0}) is not in `pool_template`, skipped.", mother_pool_id);
                            continue;
                        }
                        if (!mPoolTemplate.ContainsKey(child_pool_id))
                        {
                            Log.outError(LogFilter.Sql, "`pool_pool` included pool_id ({0}) is not in `pool_template`, skipped.", child_pool_id);
                            continue;
                        }
                        if (mother_pool_id == child_pool_id)
                        {
                            Log.outError(LogFilter.Sql, "`pool_pool` pool_id ({0}) includes itself, dead-lock detected, skipped.", child_pool_id);
                            continue;
                        }
                        if (chance < 0 || chance > 100)
                        {
                            Log.outError(LogFilter.Sql, "`pool_pool` has an invalid chance ({0}) for pool id ({1}) in mother pool id ({2}), skipped.", chance, child_pool_id, mother_pool_id);
                            continue;
                        }
                        PoolTemplateData pPoolTemplateMother = mPoolTemplate[mother_pool_id];
                        PoolObject plObject = new(child_pool_id, chance);

                        if (!mPoolPoolGroups.ContainsKey(mother_pool_id))
                            mPoolPoolGroups[mother_pool_id] = new PoolGroup<Pool>();

                        PoolGroup<Pool> plgroup = mPoolPoolGroups[mother_pool_id];
                        plgroup.SetPoolId(mother_pool_id);
                        plgroup.AddEntry(plObject, pPoolTemplateMother.MaxLimit);

                        mPoolSearchMap.Add(child_pool_id, mother_pool_id);
                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pools in mother pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            // The initialize method will spawn all pools not in an event and not in another pool, this is why there is 2 left joins with 2 null checks
            Log.outInfo(LogFilter.ServerLoading, "Starting objects pooling system...");
            {
                uint oldMSTime = Time.GetMSTime();

                SQLResult result = DB.World.Query("SELECT DISTINCT pool_template.entry, pool_members.spawnId, pool_members.poolSpawnId FROM pool_template" +
                    " LEFT JOIN game_event_pool ON pool_template.entry=game_event_pool.pool_entry" +
                    " LEFT JOIN pool_members ON pool_members.type = 2 AND pool_template.entry = pool_members.spawnId WHERE game_event_pool.pool_entry IS NULL");

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Pool handling system initialized, 0 pools spawned.");
                }
                else
                {
                    uint count = 0;
                    do
                    {
                        uint pool_entry = result.Read<uint>(0);
                        uint pool_pool_id = result.Read<uint>(1);

                        if (!CheckPool(pool_entry))
                        {
                            if (pool_pool_id != 0)
                                // The pool is a child pool in pool_pool table. Ideally we should remove it from the pool handler to ensure it never gets spawned,
                                // however that could recursively invalidate entire chain of mother pools. It can be done in the future but for now we'll do nothing.
                                Log.outError(LogFilter.Sql, "Pool Id {0} has no equal chance pooled entites defined and explicit chance sum is not 100. This broken pool is a child pool of Id {1} and cannot be safely removed.", pool_entry, result.Read<uint>(2));
                            else
                                Log.outError(LogFilter.Sql, "Pool Id {0} has no equal chance pooled entites defined and explicit chance sum is not 100. The pool will not be spawned.", pool_entry);
                            continue;
                        }

                        // Don't spawn child pools, they are spawned recursively by their parent pools
                        if (pool_pool_id == 0)
                        {
                            SpawnPool(pool_entry);
                            count++;
                        }
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Pool handling system initialized, {0} pools spawned in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

                }
            }
        }

        void SpawnPool<T>(uint pool_id, ulong db_guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    if (mPoolCreatureGroups.ContainsKey(pool_id) && !mPoolCreatureGroups[pool_id].IsEmpty())
                        mPoolCreatureGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
                case "GameObject":
                    if (mPoolGameobjectGroups.ContainsKey(pool_id) && !mPoolGameobjectGroups[pool_id].IsEmpty())
                        mPoolGameobjectGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
                case "Pool":
                    if (mPoolPoolGroups.ContainsKey(pool_id) && !mPoolPoolGroups[pool_id].IsEmpty())
                        mPoolPoolGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
            }
        }

        public void SpawnPool(uint pool_id)
        {
            SpawnPool<Pool>(pool_id, 0);
            SpawnPool<GameObject>(pool_id, 0);
            SpawnPool<Creature>(pool_id, 0);
        }

        public void DespawnPool(uint pool_id, bool alwaysDeleteRespawnTime = false)
        {
            if (mPoolCreatureGroups.ContainsKey(pool_id) && !mPoolCreatureGroups[pool_id].IsEmpty())
                mPoolCreatureGroups[pool_id].DespawnObject(mSpawnedData, 0, alwaysDeleteRespawnTime);

            if (mPoolGameobjectGroups.ContainsKey(pool_id) && !mPoolGameobjectGroups[pool_id].IsEmpty())
                mPoolGameobjectGroups[pool_id].DespawnObject(mSpawnedData, 0, alwaysDeleteRespawnTime);

            if (mPoolPoolGroups.ContainsKey(pool_id) && !mPoolPoolGroups[pool_id].IsEmpty())
                mPoolPoolGroups[pool_id].DespawnObject(mSpawnedData, 0, alwaysDeleteRespawnTime);
        }

        public bool CheckPool(uint pool_id)
        {
            if (mPoolGameobjectGroups.ContainsKey(pool_id) && !mPoolGameobjectGroups[pool_id].CheckPool())
                return false;

            if (mPoolCreatureGroups.ContainsKey(pool_id) && !mPoolCreatureGroups[pool_id].CheckPool())
                return false;

            if (mPoolPoolGroups.ContainsKey(pool_id) && !mPoolPoolGroups[pool_id].CheckPool())
                return false;

            return true;
        }

        public void UpdatePool<T>(uint pool_id, ulong db_guid_or_pool_id)
        {
            uint motherpoolid = IsPartOfAPool<Pool>(pool_id);
            if (motherpoolid != 0)
                SpawnPool<Pool>(motherpoolid, pool_id);
            else
                SpawnPool<T>(pool_id, db_guid_or_pool_id);
        }

        public uint IsPartOfAPool<T>(ulong db_guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    return mCreatureSearchMap.LookupByKey(db_guid);
                case "GameObject":
                    return mGameobjectSearchMap.LookupByKey(db_guid);
                case "Pool":
                    return mPoolSearchMap.LookupByKey(db_guid);
            }
            return 0;
        }

        // Selects proper template overload to call based on passed type
        public uint IsPartOfAPool(SpawnObjectType type, ulong spawnId)
        {
            switch (type)
            {
                case SpawnObjectType.Creature:
                    return IsPartOfAPool<Creature>(spawnId);
                case SpawnObjectType.GameObject:
                    return IsPartOfAPool<GameObject>(spawnId);
                case SpawnObjectType.AreaTrigger:
                    return 0;
                default:
                    Cypher.Assert(false, $"Invalid spawn type {type} passed to PoolMgr.IsPartOfPool (with spawnId {spawnId})");
                    return 0;
            }
        }
        
        public enum QuestTypes
        {
            None = 0,
            Daily = 1,
            Weekly = 2
        }

        public bool IsSpawnedObject<T>(ulong db_guid_or_pool_id) { return mSpawnedData.IsActiveObject<T>(db_guid_or_pool_id); }


        public MultiMap<uint, uint> mQuestCreatureRelation = new();
        public MultiMap<uint, uint> mQuestGORelation = new();

        Dictionary<uint, PoolTemplateData> mPoolTemplate = new();
        Dictionary<uint, PoolGroup<Creature>> mPoolCreatureGroups = new();
        Dictionary<uint, PoolGroup<GameObject>> mPoolGameobjectGroups = new();
        Dictionary<uint, PoolGroup<Pool>> mPoolPoolGroups = new();
        Dictionary<ulong, uint> mCreatureSearchMap = new();
        Dictionary<ulong, uint> mGameobjectSearchMap = new();
        Dictionary<ulong, uint> mPoolSearchMap = new();

        // dynamic data
        ActivePoolData mSpawnedData = new();
    }

    public class PoolGroup<T>
    {
        public PoolGroup()
        {
            poolId = 0;
        }

        public void AddEntry(PoolObject poolitem, uint maxentries)
        {
            if (poolitem.chance != 0 && maxentries == 1)
                ExplicitlyChanced.Add(poolitem);
            else
                EqualChanced.Add(poolitem);
        }

        public bool CheckPool()
        {
            if (EqualChanced.Empty())
            {
                float chance = 0;
                for (int i = 0; i < ExplicitlyChanced.Count; ++i)
                    chance += ExplicitlyChanced[i].chance;
                if (chance != 100 && chance != 0)
                    return false;
            }
            return true;
        }

        public void DespawnObject(ActivePoolData spawns, ulong guid = 0, bool alwaysDeleteRespawnTime = false)
        {
            for (int i = 0; i < EqualChanced.Count; ++i)
            {
                // if spawned
                if (spawns.IsActiveObject<T>(EqualChanced[i].guid))
                {
                    if (guid == 0 || EqualChanced[i].guid == guid)
                    {
                        Despawn1Object(EqualChanced[i].guid, alwaysDeleteRespawnTime);
                        spawns.RemoveObject<T>(EqualChanced[i].guid, poolId);
                    }
                }
                else if (alwaysDeleteRespawnTime)
                    RemoveRespawnTimeFromDB(EqualChanced[i].guid);
            }

            for (int i = 0; i < ExplicitlyChanced.Count; ++i)
            {
                // spawned
                if (spawns.IsActiveObject<T>(ExplicitlyChanced[i].guid))
                {
                    if (guid == 0 || ExplicitlyChanced[i].guid == guid)
                    {
                        Despawn1Object(ExplicitlyChanced[i].guid, alwaysDeleteRespawnTime);
                        spawns.RemoveObject<T>(ExplicitlyChanced[i].guid, poolId);
                    }
                }
                else if (alwaysDeleteRespawnTime)
                    RemoveRespawnTimeFromDB(ExplicitlyChanced[i].guid);
            }
        }

        void Despawn1Object(ulong guid, bool alwaysDeleteRespawnTime = false)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    {
                        var data = Global.ObjectMgr.GetCreatureData(guid);
                        if (data != null)
                        {
                            Global.ObjectMgr.RemoveCreatureFromGrid(data);
                            Map map = Global.MapMgr.FindMap(data.MapId, 0);
                            if (map != null && !map.Instanceable())
                            {
                                var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(guid);
                                foreach (var creature in creatureBounds)
                                {
                                    // For dynamic spawns, save respawn time here
                                    if (!creature.GetRespawnCompatibilityMode())
                                        creature.SaveRespawnTime();

                                    creature.AddObjectToRemoveList();
                                }

                            if (alwaysDeleteRespawnTime)
                                map.RemoveRespawnTime(SpawnObjectType.Creature, guid, null, true);
                        }
                        }
                        break;
                    }
                case "GameObject":
                    {
                        var data = Global.ObjectMgr.GetGameObjectData(guid);
                        if (data != null)
                        {
                            Global.ObjectMgr.RemoveGameObjectFromGrid(data);
                            Map map = Global.MapMgr.FindMap(data.MapId, 0);
                            if (map != null && !map.Instanceable())
                            {
                                var gameobjectBounds = map.GetGameObjectBySpawnIdStore().LookupByKey(guid);
                                foreach (var go in gameobjectBounds)
                                {
                                    // For dynamic spawns, save respawn time here
                                    if (!go.GetRespawnCompatibilityMode())
                                        go.SaveRespawnTime();

                                    go.AddObjectToRemoveList();
                                }

                            if (alwaysDeleteRespawnTime)
                                map.RemoveRespawnTime(SpawnObjectType.GameObject, guid, null, true);
                        }
                        }
                        break;
                    }
                case "Pool":
                    Global.PoolMgr.DespawnPool((uint)guid, alwaysDeleteRespawnTime);
                    break;
            }
        }

        public void RemoveOneRelation(uint child_pool_id)
        {
            if (typeof(T).Name != "Pool")
                return;

            foreach (var poolObject in ExplicitlyChanced)
            {
                if (poolObject.guid == child_pool_id)
                {
                    ExplicitlyChanced.Remove(poolObject);
                    break;
                }
            }
            foreach (var poolObject in EqualChanced)
            {
                if (poolObject.guid == child_pool_id)
                {
                    EqualChanced.Remove(poolObject);
                    break;
                }
            }
        }

        public void SpawnObject(ActivePoolData spawns, uint limit, ulong triggerFrom)
        {
            int count = (int)(limit - spawns.GetActiveObjectCount(poolId));

            // If triggered from some object respawn this object is still marked as spawned
            // and also counted into m_SpawnedPoolAmount so we need increase count to be
            // spawned by 1
            if (triggerFrom != 0)
                ++count;

            // This will try to spawn the rest of pool, not guaranteed
            if (count > 0)
            {
                List<PoolObject> rolledObjects = new();

                // roll objects to be spawned
                if (!ExplicitlyChanced.Empty())
                {
                    float roll = (float)RandomHelper.randChance();

                    foreach (PoolObject obj in ExplicitlyChanced)
                    {
                        roll -= obj.chance;
                        // Triggering object is marked as spawned at this time and can be also rolled (respawn case)
                        // so this need explicit check for this case
                        if (roll < 0 && (/*obj.guid == triggerFrom ||*/ !spawns.IsActiveObject<T>(obj.guid)))
                        {
                            rolledObjects.Add(obj);
                            break;
                        }
                    }
                }
                
                if (!EqualChanced.Empty() && rolledObjects.Empty())
                {
                    rolledObjects.AddRange(EqualChanced.Where(obj => /*obj.guid == triggerFrom ||*/ !spawns.IsActiveObject<T>(obj.guid)));
                    rolledObjects.RandomResize((uint)count);
                }

                // try to spawn rolled objects
                foreach (PoolObject obj in rolledObjects)
                {
                    if (obj.guid == triggerFrom)
                    {
                        ReSpawn1Object(obj);
                        triggerFrom = 0;
                    }
                    else
                    {
                        spawns.ActivateObject<T>(obj.guid, poolId);
                        Spawn1Object(obj);
                    }
                }
            }

            // One spawn one despawn no count increase
            if (triggerFrom != 0)
                DespawnObject(spawns, triggerFrom);
        }

        void Spawn1Object(PoolObject obj)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    {
                        CreatureData data = Global.ObjectMgr.GetCreatureData(obj.guid);
                        if (data != null)
                        {
                            Global.ObjectMgr.AddCreatureToGrid(data);

                            // Spawn if necessary (loaded grids only)
                            Map map = Global.MapMgr.FindMap(data.MapId, 0);
                            // We use spawn coords to spawn
                            if (map != null && !map.Instanceable() && map.IsGridLoaded(data.SpawnPoint))
                                Creature.CreateCreatureFromDB(obj.guid, map);
                        }
                    }
                    break;
                case "GameObject":
                    {
                        GameObjectData data = Global.ObjectMgr.GetGameObjectData(obj.guid);
                        if (data != null)
                        {
                            Global.ObjectMgr.AddGameObjectToGrid(data);
                            // Spawn if necessary (loaded grids only)
                            // this base map checked as non-instanced and then only existed
                            Map map = Global.MapMgr.FindMap(data.MapId, 0);
                            // We use current coords to unspawn, not spawn coords since creature can have changed grid
                            if (map != null && !map.Instanceable() && map.IsGridLoaded(data.SpawnPoint))
                            {
                                GameObject go = GameObject.CreateGameObjectFromDB(obj.guid, map, false);
                                if (go)
                                {
                                    if (go.IsSpawnedByDefault())
                                        map.AddToMap(go);
                                }
                            }
                        }
                    }
                    break;
                case "Pool":
                    Global.PoolMgr.SpawnPool((uint)obj.guid);
                    break;
            }
        }

        void ReSpawn1Object(PoolObject obj)
        {
            // GameObject/Creature is still on map, nothing to do
        }

        void RemoveRespawnTimeFromDB(ulong guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                {
                    CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                    if (data != null)
                    {
                        Map map = Global.MapMgr.CreateBaseMap(data.MapId);
                        if (!map.Instanceable())
                        {
                            map.RemoveRespawnTime(SpawnObjectType.Creature, guid, null, true);
                        }
                    }
                }
                break;
                case "GameObject":
                {
                    GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);
                    if (data != null)
                    {
                        Map map = Global.MapMgr.CreateBaseMap(data.MapId);
                        if (!map.Instanceable())
                        {
                            map.RemoveRespawnTime(SpawnObjectType.GameObject, guid, null, true);
                        }
                    }
                    break;
                }
            }
        }

        public void SetPoolId(uint pool_id) { poolId = pool_id; }

        public bool IsEmpty() { return ExplicitlyChanced.Empty() && EqualChanced.Empty(); }

        public ulong GetFirstEqualChancedObjectId()
        {
            if (EqualChanced.Empty())
                return 0;
            return EqualChanced.FirstOrDefault().guid;
        }
        public uint GetPoolId() { return poolId; }

        uint poolId;
        List<PoolObject> ExplicitlyChanced = new();
        List<PoolObject> EqualChanced = new();
    }

    public class ActivePoolData
    {
        public uint GetActiveObjectCount(uint pool_id)
        {
            return mSpawnedPools.LookupByKey(pool_id);
        }

        public bool IsActiveObject<T>(ulong db_guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    return mSpawnedCreatures.Contains(db_guid);
                case "GameObject":
                    return mSpawnedGameobjects.Contains(db_guid);
                case "Pool":
                    return mSpawnedPools.ContainsKey(db_guid);
                default:
                    return false;
            }            
        }

        public void ActivateObject<T>(ulong db_guid, uint pool_id)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    mSpawnedCreatures.Add(db_guid);
                    break;
                case "GameObject":
                    mSpawnedGameobjects.Add(db_guid);
                    break;
                case "Pool":
                    mSpawnedPools[db_guid] = 0;
                    break;
                default:
                    return;
            }
            if (!mSpawnedPools.ContainsKey(pool_id))
                mSpawnedPools[pool_id] = 0;

            ++mSpawnedPools[pool_id];
        }

        public void RemoveObject<T>(ulong db_guid, uint pool_id)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    mSpawnedCreatures.Remove(db_guid);
                    break;
                case "GameObject":
                    mSpawnedGameobjects.Remove(db_guid);
                    break;
                case "Pool":
                    mSpawnedPools.Remove(db_guid);
                    break;
                default:
                    return;
            }

            if (mSpawnedPools[pool_id] > 0)
                --mSpawnedPools[pool_id];
        }

        List<ulong> mSpawnedCreatures = new();
        List<ulong> mSpawnedGameobjects = new();
        Dictionary<ulong, uint> mSpawnedPools = new();
    }

    public class PoolObject
    {
        public PoolObject(ulong _guid, float _chance)
        {
            guid = _guid;
            chance = Math.Abs(_chance);
        }

        public ulong guid;
        public float chance;
    }

    public struct PoolTemplateData
    {
        public uint MaxLimit;
    }

    public class Pool { }                 // for Pool of Pool case
}
