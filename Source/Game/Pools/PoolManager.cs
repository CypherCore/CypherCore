/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
            mQuestSearchMap.Clear();
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

                    PoolTemplateData pPoolTemplate = new PoolTemplateData();
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

                //                                                 1       2         3
                SQLResult result = DB.World.Query("SELECT guid, pool_entry, chance FROM pool_creature");

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
                        PoolObject plObject = new PoolObject(guid, chance);

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

                //                                                 1        2         3
                SQLResult result = DB.World.Query("SELECT guid, pool_entry, chance FROM pool_gameobject");

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

                        GameObjectData data = Global.ObjectMgr.GetGOData(guid);
                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`pool_gameobject` has a non existing gameobject spawn (GUID: {0}) defined for pool id ({1}), skipped.", guid, pool_id);
                            continue;
                        }

                        GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(data.id);
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
                        PoolObject plObject = new PoolObject(guid, chance);

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

                //                                                  1        2            3
                SQLResult result = DB.World.Query("SELECT pool_id, mother_pool, chance FROM pool_pool");

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
                        PoolObject plObject = new PoolObject(child_pool_id, chance);

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

            Log.outInfo(LogFilter.ServerLoading, "Loading Quest Pooling Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_QUEST_POOLS);
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 quests in pools");
                }
                else
                {
                    List<uint> creBounds;
                    List<uint> goBounds;

                    Dictionary<uint, eQuestTypes> poolTypeMap = new Dictionary<uint, eQuestTypes>();
                    uint count = 0;
                    do
                    {
                        uint entry = result.Read<uint>(0);
                        uint pool_id = result.Read<uint>(1);

                        if (!poolTypeMap.ContainsKey(pool_id))
                            poolTypeMap[pool_id] = 0;

                        Quest quest = Global.ObjectMgr.GetQuestTemplate(entry);
                        if (quest == null)
                        {
                            Log.outError(LogFilter.Sql, "`pool_quest` has a non existing quest template (Entry: {0}) defined for pool id ({1}), skipped.", entry, pool_id);
                            continue;
                        }

                        if (!mPoolTemplate.ContainsKey(pool_id))
                        {
                            Log.outError(LogFilter.Sql, "`pool_quest` pool id ({0}) is not in `pool_template`, skipped.", pool_id);
                            continue;
                        }

                        if (!quest.IsDailyOrWeekly())
                        {
                            Log.outError(LogFilter.Sql, "`pool_quest` has an quest ({0}) which is not daily or weekly in pool id ({1}), use ExclusiveGroup instead, skipped.", entry, pool_id);
                            continue;
                        }

                        if (poolTypeMap[pool_id] == eQuestTypes.None)
                            poolTypeMap[pool_id] = quest.IsDaily() ? eQuestTypes.Daily : eQuestTypes.Weekly;

                        eQuestTypes currType = quest.IsDaily() ? eQuestTypes.Daily : eQuestTypes.Weekly;

                        if (poolTypeMap[pool_id] != currType)
                        {
                            Log.outError(LogFilter.Sql, "`pool_quest` quest {0} is {1} but pool ({2}) is specified for {3}, mixing not allowed, skipped.",
                                             entry, currType, pool_id, poolTypeMap[pool_id]);
                            continue;
                        }

                        creBounds = mQuestCreatureRelation.LookupByKey(entry);
                        goBounds = mQuestGORelation.LookupByKey(entry);

                        if (creBounds.Empty() && goBounds.Empty())
                        {
                            Log.outError(LogFilter.Sql, "`pool_quest` lists entry ({0}) as member of pool ({1}) but is not started anywhere, skipped.", entry, pool_id);
                            continue;
                        }

                        PoolTemplateData pPoolTemplate = mPoolTemplate[pool_id];
                        PoolObject plObject = new PoolObject(entry, 0.0f);

                        if (!mPoolQuestGroups.ContainsKey(pool_id))
                            mPoolQuestGroups[pool_id] = new PoolGroup<Quest>();

                        PoolGroup<Quest> questgroup = mPoolQuestGroups[pool_id];
                        questgroup.SetPoolId(pool_id);
                        questgroup.AddEntry(plObject, pPoolTemplate.MaxLimit);

                        mQuestSearchMap.Add(entry, pool_id);
                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests in pools in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            // The initialize method will spawn all pools not in an event and not in another pool, this is why there is 2 left joins with 2 null checks
            Log.outInfo(LogFilter.ServerLoading, "Starting objects pooling system...");
            {
                uint oldMSTime = Time.GetMSTime();

                SQLResult result = DB.World.Query("SELECT DISTINCT pool_template.entry, pool_pool.pool_id, pool_pool.mother_pool FROM pool_template" +
                    " LEFT JOIN game_event_pool ON pool_template.entry=game_event_pool.pool_entry" +
                    " LEFT JOIN pool_pool ON pool_template.entry=pool_pool.pool_id WHERE game_event_pool.pool_entry IS NULL");

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

                    Log.outDebug(LogFilter.Pool, "Pool handling system initialized, {0} pools spawned in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));

                }
            }
        }

        public void SaveQuestsToDB()
        {
            SQLTransaction trans = new SQLTransaction();

            foreach (var questPoolGroup in mPoolQuestGroups.Values)
            {
                if (questPoolGroup.isEmpty())
                    continue;
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_QUEST_POOL_SAVE);
                stmt.AddValue(0, questPoolGroup.GetPoolId());
                trans.Append(stmt);
            }

            foreach (var pair in mQuestSearchMap)
            {
                if (IsSpawnedObject<Quest>(pair.Key))
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_QUEST_POOL_SAVE);
                    stmt.AddValue(0, pair.Value);
                    stmt.AddValue(1, pair.Key);
                    trans.Append(stmt);
                }
            }

            DB.Characters.CommitTransaction(trans);
        }

        public void ChangeDailyQuests()
        {
            foreach (var questPoolGroup in mPoolQuestGroups.Values)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)questPoolGroup.GetFirstEqualChancedObjectId());
                if (quest != null)
                {
                    if (quest.IsWeekly())
                        continue;

                    UpdatePool<Quest>(questPoolGroup.GetPoolId(), 1);    // anything non-zero means don't load from db
                }
            }

            SaveQuestsToDB();
        }

        public void ChangeWeeklyQuests()
        {
            foreach (var questPoolGroup in mPoolQuestGroups.Values)
            {
                Quest quest = Global.ObjectMgr.GetQuestTemplate((uint)questPoolGroup.GetFirstEqualChancedObjectId());
                if (quest != null)
                {
                    if (quest.IsDaily())
                        continue;

                    UpdatePool<Quest>(questPoolGroup.GetPoolId(), 1);
                }
            }

            SaveQuestsToDB();
        }

        void SpawnPool<T>(uint pool_id, ulong db_guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    if (mPoolCreatureGroups.ContainsKey(pool_id) && !mPoolCreatureGroups[pool_id].isEmpty())
                        mPoolCreatureGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
                case "GameObject":
                    if (mPoolGameobjectGroups.ContainsKey(pool_id) && !mPoolGameobjectGroups[pool_id].isEmpty())
                        mPoolGameobjectGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
                case "Pool":
                    if (mPoolPoolGroups.ContainsKey(pool_id) && !mPoolPoolGroups[pool_id].isEmpty())
                        mPoolPoolGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
                case "Quest":
                    if (mPoolQuestGroups.ContainsKey(pool_id) && !mPoolQuestGroups[pool_id].isEmpty())
                        mPoolQuestGroups[pool_id].SpawnObject(mSpawnedData, mPoolTemplate[pool_id].MaxLimit, db_guid);
                    break;
            }
        }

        public void SpawnPool(uint pool_id)
        {
            SpawnPool<Pool>(pool_id, 0);
            SpawnPool<GameObject>(pool_id, 0);
            SpawnPool<Creature>(pool_id, 0);
            SpawnPool<Quest>(pool_id, 0);
        }

        public void DespawnPool(uint pool_id)
        {
            if (mPoolCreatureGroups.ContainsKey(pool_id) && !mPoolCreatureGroups[pool_id].isEmpty())
                mPoolCreatureGroups[pool_id].DespawnObject(mSpawnedData);

            if (mPoolGameobjectGroups.ContainsKey(pool_id) && !mPoolGameobjectGroups[pool_id].isEmpty())
                mPoolGameobjectGroups[pool_id].DespawnObject(mSpawnedData);

            if (mPoolPoolGroups.ContainsKey(pool_id) && !mPoolPoolGroups[pool_id].isEmpty())
                mPoolPoolGroups[pool_id].DespawnObject(mSpawnedData);

            if (mPoolQuestGroups.ContainsKey(pool_id) && !mPoolQuestGroups[pool_id].isEmpty())
                mPoolQuestGroups[pool_id].DespawnObject(mSpawnedData);
        }

        public bool CheckPool(uint pool_id)
        {
            if (mPoolGameobjectGroups.ContainsKey(pool_id) && !mPoolGameobjectGroups[pool_id].CheckPool())
                return false;

            if (mPoolCreatureGroups.ContainsKey(pool_id) && !mPoolCreatureGroups[pool_id].CheckPool())
                return false;

            if (mPoolPoolGroups.ContainsKey(pool_id) && !mPoolPoolGroups[pool_id].CheckPool())
                return false;

            if (mPoolQuestGroups.ContainsKey(pool_id) && !mPoolQuestGroups[pool_id].CheckPool())
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
                case "Quest":
                    return mQuestSearchMap.LookupByKey(db_guid);
            }
            return 0;
        }

        public enum eQuestTypes
        {
            None = 0,
            Daily = 1,
            Weekly = 2
        }

        public bool IsSpawnedObject<T>(ulong db_guid_or_pool_id) { return mSpawnedData.IsActiveObject<T>(db_guid_or_pool_id); }


        public MultiMap<uint, uint> mQuestCreatureRelation = new MultiMap<uint, uint>();
        public MultiMap<uint, uint> mQuestGORelation = new MultiMap<uint, uint>();

        Dictionary<uint, PoolTemplateData> mPoolTemplate = new Dictionary<uint, PoolTemplateData>();
        Dictionary<uint, PoolGroup<Creature>> mPoolCreatureGroups = new Dictionary<uint, PoolGroup<Creature>>();
        Dictionary<uint, PoolGroup<GameObject>> mPoolGameobjectGroups = new Dictionary<uint, PoolGroup<GameObject>>();
        Dictionary<uint, PoolGroup<Pool>> mPoolPoolGroups = new Dictionary<uint, PoolGroup<Pool>>();
        Dictionary<uint, PoolGroup<Quest>> mPoolQuestGroups = new Dictionary<uint, PoolGroup<Quest>>();
        Dictionary<ulong, uint> mCreatureSearchMap = new Dictionary<ulong, uint>();
        Dictionary<ulong, uint> mGameobjectSearchMap = new Dictionary<ulong, uint>();
        Dictionary<ulong, uint> mPoolSearchMap = new Dictionary<ulong, uint>();
        Dictionary<ulong, uint> mQuestSearchMap = new Dictionary<ulong, uint>();

        // dynamic data
        ActivePoolData mSpawnedData = new ActivePoolData();
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

        PoolObject RollOne(ActivePoolData spawns, ulong triggerFrom)
        {
            if (!ExplicitlyChanced.Empty())
            {
                float roll = (float)RandomHelper.randChance();

                for (int i = 0; i < ExplicitlyChanced.Count; ++i)
                {
                    roll -= ExplicitlyChanced[i].chance;
                    // Triggering object is marked as spawned at this time and can be also rolled (respawn case)
                    // so this need explicit check for this case
                    if (roll < 0 && (ExplicitlyChanced[i].guid == triggerFrom || !spawns.IsActiveObject<T>(ExplicitlyChanced[i].guid)))
                        return ExplicitlyChanced[i];
                }
            }
            if (!EqualChanced.Empty())
            {
                int index = RandomHelper.IRand(0, EqualChanced.Count - 1);
                // Triggering object is marked as spawned at this time and can be also rolled (respawn case)
                // so this need explicit check for this case
                if (EqualChanced[index].guid == triggerFrom || !spawns.IsActiveObject<T>(EqualChanced[index].guid))
                    return EqualChanced[index];
            }

            return null;
        }

        public void DespawnObject(ActivePoolData spawns, ulong guid = 0)
        {
            for (int i = 0; i < EqualChanced.Count; ++i)
            {
                // if spawned
                if (spawns.IsActiveObject<T>(EqualChanced[i].guid))
                {
                    if (guid == 0 || EqualChanced[i].guid == guid)
                    {
                        Despawn1Object(EqualChanced[i].guid);
                        spawns.RemoveObject<T>(EqualChanced[i].guid, poolId);
                    }
                }
            }

            for (int i = 0; i < ExplicitlyChanced.Count; ++i)
            {
                // spawned
                if (spawns.IsActiveObject<T>(ExplicitlyChanced[i].guid))
                {
                    if (guid == 0 || ExplicitlyChanced[i].guid == guid)
                    {
                        Despawn1Object(ExplicitlyChanced[i].guid);
                        spawns.RemoveObject<T>(ExplicitlyChanced[i].guid, poolId);
                    }
                }
            }
        }

        void Despawn1Object(ulong guid)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    {
                        var data = Global.ObjectMgr.GetCreatureData(guid);
                        if (data != null)
                        {
                            Global.ObjectMgr.RemoveCreatureFromGrid(guid, data);
                            Map map = Global.MapMgr.CreateBaseMap(data.mapid);
                            if (!map.Instanceable())
                            {
                                var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(guid);
                                foreach (var creature in creatureBounds)
                                    creature.AddObjectToRemoveList();
                            }
                        }
                        break;
                    }
                case "GameObject":
                    {
                        var data = Global.ObjectMgr.GetGOData(guid);
                        if (data != null)
                        {
                            Global.ObjectMgr.RemoveGameObjectFromGrid(guid, data);

                            Map map = Global.MapMgr.CreateBaseMap(data.mapid);
                            if (!map.Instanceable())
                            {
                                var gameobjectBounds = map.GetGameObjectBySpawnIdStore().LookupByKey(guid);
                                foreach (var go in gameobjectBounds)
                                    go.AddObjectToRemoveList();
                            }
                        }
                        break;
                    }
                case "Pool":
                    Global.PoolMgr.DespawnPool((uint)guid);
                    break;
                case "Quest":
                    // Creatures
                    var questMap = Global.ObjectMgr.GetCreatureQuestRelationMap();
                    var qr = Global.PoolMgr.mQuestCreatureRelation.LookupByKey(guid);
                    foreach (var creature in qr)
                    {
                        if (!questMap.ContainsKey(creature))
                            continue;

                        foreach (var quest in questMap[creature].ToList())
                        {
                            if (quest == guid)
                                questMap.Remove(creature, quest);
                        }
                    }

                    // Gameobjects
                    questMap = Global.ObjectMgr.GetGOQuestRelationMap();
                    qr = Global.PoolMgr.mQuestGORelation.LookupByKey(guid);
                    foreach (var go in qr)
                    {
                        if (!questMap.ContainsKey(go))
                            continue;

                        foreach (var quest in questMap[go])
                        {
                            if (quest == guid)
                                questMap.Remove(go, quest);
                        }
                    }
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
            if (typeof(T).Name == "Quest")
            {
                SpawnQuestObject(spawns, limit, triggerFrom);
                return;
            }

            ulong lastDespawned = 0;
            int count = (int)(limit - spawns.GetActiveObjectCount(poolId));

            // If triggered from some object respawn this object is still marked as spawned
            // and also counted into m_SpawnedPoolAmount so we need increase count to be
            // spawned by 1
            if (triggerFrom != 0)
                ++count;

            // This will try to spawn the rest of pool, not guaranteed
            for (int i = 0; i < count; ++i)
            {
                PoolObject obj = RollOne(spawns, triggerFrom);
                if (obj == null)
                    continue;
                if (obj.guid == lastDespawned)
                    continue;

                if (obj.guid == triggerFrom)
                {
                    ReSpawn1Object(obj);
                    triggerFrom = 0;
                    continue;
                }
                spawns.ActivateObject<T>(obj.guid, poolId);
                Spawn1Object(obj);

                if (triggerFrom != 0)
                {
                    // One spawn one despawn no count increase
                    DespawnObject(spawns, triggerFrom);
                    lastDespawned = triggerFrom;
                    triggerFrom = 0;
                }
            }
        }

        void SpawnQuestObject(ActivePoolData spawns, uint limit, ulong triggerFrom)
        {
            Log.outDebug(LogFilter.Pool, "PoolGroup<Quest>: Spawning pool {0}", poolId);
            // load state from db
            if (triggerFrom == 0)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_POOL_QUEST_SAVE);
                stmt.AddValue(0, poolId);
                SQLResult result = DB.Characters.Query(stmt);

                if (!result.IsEmpty())
                {
                    do
                    {
                        uint questId = result.Read<uint>(0);
                        spawns.ActivateObject<Quest>(questId, poolId);
                        PoolObject tempObj = new PoolObject(questId, 0.0f);
                        Spawn1Object(tempObj);
                        --limit;
                    } while (result.NextRow() && limit != 0);
                    return;
                }
            }

            List<ulong> currentQuests = spawns.GetActiveQuests();
            List<ulong> newQuests = new List<ulong>();

            // always try to select different quests
            foreach (var poolObject in EqualChanced)
            {
                if (spawns.IsActiveObject<Quest>(poolObject.guid))
                    continue;
                newQuests.Add(poolObject.guid);
            }

            // clear the pool
            DespawnObject(spawns);

            // recycle minimal amount of quests if possible count is lower than limit
            while (limit > newQuests.Count && !currentQuests.Empty())
            {
                ulong questId = currentQuests.SelectRandom();
                newQuests.Add(questId);
                currentQuests.Remove(questId);
            }

            if (newQuests.Empty())
                return;

            // activate <limit> random quests
            do
            {
                ulong questId = newQuests.SelectRandom();
                spawns.ActivateObject<Quest>(questId, poolId);
                PoolObject tempObj = new PoolObject(questId, 0.0f);
                Spawn1Object(tempObj);
                newQuests.Remove(questId);
                --limit;
            } while (limit != 0 && !newQuests.Empty());

            // if we are here it means the pool is initialized at startup and did not have previous saved state
            if (triggerFrom == 0)
                Global.PoolMgr.SaveQuestsToDB();
        }

        void Spawn1Object(PoolObject obj)
        {
            switch (typeof(T).Name)
            {
                case "Creature":
                    CreatureData data = Global.ObjectMgr.GetCreatureData(obj.guid);
                    if (data != null)
                    {
                        Global.ObjectMgr.AddCreatureToGrid(obj.guid, data);

                        // Spawn if necessary (loaded grids only)
                        Map map = Global.MapMgr.CreateBaseMap(data.mapid);
                        // We use spawn coords to spawn
                        if (!map.Instanceable() && map.IsGridLoaded(data.posX, data.posY))
                            Creature.CreateCreatureFromDB(obj.guid, map);
                    }
                    break;
                case "GameObject":
                    GameObjectData data_ = Global.ObjectMgr.GetGOData(obj.guid);
                    if (data_ != null)
                    {
                        Global.ObjectMgr.AddGameObjectToGrid(obj.guid, data_);
                        // Spawn if necessary (loaded grids only)
                        // this base map checked as non-instanced and then only existed
                        Map map = Global.MapMgr.CreateBaseMap(data_.mapid);
                        // We use current coords to unspawn, not spawn coords since creature can have changed grid
                        if (!map.Instanceable() && map.IsGridLoaded(data_.posX, data_.posY))
                        {
                            GameObject go = GameObject.CreateGameObjectFromDB(obj.guid, map, false);
                            if (go)
                            {
                                if (go.isSpawnedByDefault())
                                    map.AddToMap(go);
                            }
                        }
                    }
                    break;
                case "Pool":
                    Global.PoolMgr.SpawnPool((uint)obj.guid);
                    break;
                case "Quest":
                    // Creatures
                    var questMap = Global.ObjectMgr.GetCreatureQuestRelationMap();
                    var qr = Global.PoolMgr.mQuestCreatureRelation.LookupByKey(obj.guid);
                    foreach (var creature in qr)
                    {
                        Log.outDebug(LogFilter.Pool, "PoolGroup<Quest>: Adding quest {0} to creature {1}", obj.guid, creature);
                        questMap.Add(creature, (uint)obj.guid);
                    }

                    // Gameobjects
                    questMap = Global.ObjectMgr.GetGOQuestRelationMap();
                    qr = Global.PoolMgr.mQuestGORelation.LookupByKey(obj.guid);
                    foreach (var go in qr)
                    {
                        Log.outDebug(LogFilter.Pool, "PoolGroup<Quest>: Adding quest {0} to GO {1}", obj.guid, go);
                        questMap.Add(go, (uint)obj.guid);
                    }
                    break;
            }
        }

        void ReSpawn1Object(PoolObject obj)
        {
            // GameObject/Creature is still on map, nothing to do
        }

        public void SetPoolId(uint pool_id) { poolId = pool_id; }

        public bool isEmpty() { return ExplicitlyChanced.Empty() && EqualChanced.Empty(); }

        public ulong GetFirstEqualChancedObjectId()
        {
            if (EqualChanced.Empty())
                return 0;
            return EqualChanced.FirstOrDefault().guid;
        }
        public uint GetPoolId() { return poolId; }

        uint poolId;
        List<PoolObject> ExplicitlyChanced = new List<PoolObject>();
        List<PoolObject> EqualChanced = new List<PoolObject>();
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
                case "Quest":
                    return mActiveQuests.Contains(db_guid);
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
                case "Quest":
                    mActiveQuests.Add(db_guid);
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
                case "Quest":
                    mActiveQuests.Remove(db_guid);
                    break;
                default:
                    return;
            }

            uint val = mSpawnedPools[pool_id];
            if (val > 0)
                --val;
        }

        public List<ulong> GetActiveQuests() { return mActiveQuests; } // a copy of the set

        List<ulong> mSpawnedCreatures = new List<ulong>();
        List<ulong> mSpawnedGameobjects = new List<ulong>();
        List<ulong> mActiveQuests = new List<ulong>();
        Dictionary<ulong, uint> mSpawnedPools = new Dictionary<ulong, uint>();
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
