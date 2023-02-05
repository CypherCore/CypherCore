// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class QuestPoolManager : Singleton<QuestPoolManager>
    {
        List<QuestPool> _dailyPools = new();
        List<QuestPool> _weeklyPools = new();
        List<QuestPool> _monthlyPools = new();
        Dictionary<uint, QuestPool> _poolLookup = new(); // questId -> pool

        QuestPoolManager() { }

        public static void RegeneratePool(QuestPool pool)
        {
            Cypher.Assert(!pool.members.Empty(), $"Quest pool {pool.poolId} is empty");
            Cypher.Assert(pool.numActive <= pool.members.Count, $"Quest Pool {pool.poolId} requests {pool.numActive} spawns, but has only {pool.members.Count} members.");

            int n = pool.members.Count - 1;
            pool.activeQuests.Clear();
            for (uint i = 0; i < pool.numActive; ++i)
            {
                uint j = RandomHelper.URand(i, n);
                if (i != j)
                {
                    var leftList = pool.members[i];
                    pool.members[i] = pool.members[j];
                    pool.members[j] = leftList;
                }

                foreach (uint quest in pool.members[i])
                    pool.activeQuests.Add(quest);
            }
        }

        public static void SaveToDB(QuestPool pool, SQLTransaction trans)
        {
            PreparedStatement delStmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_POOL_QUEST_SAVE);
            delStmt.AddValue(0, pool.poolId);
            trans.Append(delStmt);

            foreach (uint questId in pool.activeQuests)
            {
                PreparedStatement insStmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_POOL_QUEST_SAVE);
                insStmt.AddValue(0, pool.poolId);
                insStmt.AddValue(1, questId);
                trans.Append(insStmt);
            }
        }

        public void LoadFromDB()
        {
            uint oldMSTime = Time.GetMSTime();
            Dictionary<uint, Tuple<List<QuestPool>, int>> lookup = new(); // poolId -> (list, index)

            _poolLookup.Clear();
            _dailyPools.Clear();
            _weeklyPools.Clear();
            _monthlyPools.Clear();

            // load template data from world DB
            {
                SQLResult result = DB.World.Query("SELECT qpm.questId, qpm.poolId, qpm.poolIndex, qpt.numActive FROM quest_pool_members qpm LEFT JOIN quest_pool_template qpt ON qpm.poolId = qpt.poolId");
                if (result.IsEmpty())
                {
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 quest pools. DB table `quest_pool_members` is empty.");
                    return;
                }

                do
                {
                    if (result.IsNull(2))
                    {
                        Log.outError(LogFilter.Sql, $"Table `quest_pool_members` contains reference to non-existing pool {result.Read<uint>(1)}. Skipped.");
                        continue;
                    }

                    uint questId = result.Read<uint>(0);
                    uint poolId = result.Read<uint>(1);
                    uint poolIndex = result.Read<uint>(2);
                    uint numActive = result.Read<uint>(3);

                    Quest quest = Global.ObjectMgr.GetQuestTemplate(questId);
                    if (quest == null)
                    {
                        Log.outError(LogFilter.Sql, "Table `quest_pool_members` contains reference to non-existing quest %u. Skipped.", questId);
                        continue;
                    }
                    if (!quest.IsDailyOrWeekly() && !quest.IsMonthly())
                    {
                        Log.outError(LogFilter.Sql, "Table `quest_pool_members` contains reference to quest %u, which is neither daily, weekly nor monthly. Skipped.", questId);
                        continue;
                    }

                    if (!lookup.ContainsKey(poolId))
                    {
                        var poolList = quest.IsDaily() ? _dailyPools : quest.IsWeekly() ? _weeklyPools : _monthlyPools;
                        poolList.Add(new QuestPool() { poolId = poolId, numActive = numActive });

                        lookup.Add(poolId, new Tuple<List<QuestPool>, int>(poolList, poolList.Count - 1));
                    }

                    var pair = lookup[poolId];

                    var members = pair.Item1[pair.Item2].members;
                    members.Add(poolIndex, questId);
                } while (result.NextRow());
            }

            // load saved spawns from character DB
            {
                SQLResult result = DB.Characters.Query("SELECT pool_id, quest_id FROM pool_quest_save");
                if (!result.IsEmpty())
                {
                    List<uint> unknownPoolIds = new();
                    do
                    {
                        uint poolId = result.Read<uint>(0);
                        uint questId = result.Read<uint>(1);

                        var it = lookup.LookupByKey(poolId);
                        if (it == null || it.Item1 == null)
                        {
                            Log.outError(LogFilter.Sql, "Table `pool_quest_save` contains reference to non-existant quest pool %u. Deleted.", poolId);
                            unknownPoolIds.Add(poolId);
                            continue;
                        }

                        it.Item1[it.Item2].activeQuests.Add(questId);
                    } while (result.NextRow());

                    SQLTransaction trans0 = new SQLTransaction();
                    foreach (uint poolId in unknownPoolIds)
                    {
                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_POOL_QUEST_SAVE);
                        stmt.AddValue(0, poolId);
                        trans0.Append(stmt);
                    }
                    DB.Characters.CommitTransaction(trans0);
                }
            }

            // post-processing and sanity checks
            SQLTransaction trans = new SQLTransaction();
            foreach (var pair in lookup)
            {
                if (pair.Value.Item1 == null)
                    continue;

                QuestPool pool = pair.Value.Item1[pair.Value.Item2];
                if (pool.members.Count < pool.numActive)
                {
                    Log.outError(LogFilter.Sql, $"Table `quest_pool_template` contains quest pool {pool.poolId} requesting {pool.numActive} spawns, but only has {pool.members.Count} members. Requested spawns reduced.");
                    pool.numActive = (uint)pool.members.Count;
                }

                bool doRegenerate = pool.activeQuests.Empty();
                if (!doRegenerate)
                {
                    List<uint> accountedFor = new();
                    uint activeCount = 0;
                    for (uint i = (uint)pool.members.Count; (i--) != 0;)
                    {
                        var member = pool.members[i];
                        if (member.Empty())
                        {
                            Log.outError(LogFilter.Sql, $"Table `quest_pool_members` contains no entries at index {i} for quest pool {pool.poolId}. Index removed.");
                            pool.members.Remove(i);
                            continue;
                        }

                        // check if the first member is active
                        bool status = pool.activeQuests.Contains(member[0]);
                        // temporarily remove any spawns that are accounted for
                        if (status)
                        {
                            accountedFor.Add(member[0]);
                            pool.activeQuests.Remove(member[0]);
                        }

                        // now check if all other members also have the same status, and warn if not
                        foreach (var id in member)
                        {
                            bool otherStatus = pool.activeQuests.Contains(id);
                            if (status != otherStatus)
                                Log.outWarn(LogFilter.Sql, $"Table `pool_quest_save` {(status ? "does not have" : "has")} quest {id} (in pool {pool.poolId}, index {i}) saved, but its index is{(status ? "" : " not")} " +
                                    $"active (because quest {member[0]} is{(status ? "" : " not")} in the table). Set quest {id} to {(status ? "" : "in")}active.");
                            if (otherStatus)
                                pool.activeQuests.Remove(id);
                            if (status)
                                accountedFor.Add(id);
                        }

                        if (status)
                            ++activeCount;
                    }

                    // warn for any remaining active spawns (not part of the pool)
                    foreach (uint quest in pool.activeQuests)
                        Log.outWarn(LogFilter.Sql, $"Table `pool_quest_save` has saved quest {quest} for pool {pool.poolId}, but that quest is not part of the pool. Skipped.");

                    // only the previously-found spawns should actually be active
                    pool.activeQuests = accountedFor;

                    if (activeCount != pool.numActive)
                    {
                        doRegenerate = true;
                        Log.outError(LogFilter.Sql, $"Table `pool_quest_save` has {activeCount} active members saved for pool {pool.poolId}, which requests {pool.numActive} active members. Pool spawns re-generated.");
                    }
                }

                if (doRegenerate)
                {
                    RegeneratePool(pool);
                    SaveToDB(pool, trans);
                }

                foreach (var memberKey in pool.members.Keys)
                {
                    foreach (uint quest in pool.members[memberKey])
                    {
                        if (_poolLookup.ContainsKey(quest))
                        {
                            Log.outError(LogFilter.Sql, $"Table `quest_pool_members` lists quest {quest} as member of pool {pool.poolId}, but it is already a member of pool {_poolLookup[quest].poolId}. Skipped.");
                            continue;
                        }
                        _poolLookup[quest] = pool;
                    }
                }
            }
            DB.Characters.CommitTransaction(trans);

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_dailyPools.Count} daily, {_weeklyPools.Count} weekly and {_monthlyPools.Count} monthly quest pools in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        void Regenerate(List<QuestPool> pools)
        {
            SQLTransaction trans = new SQLTransaction();
            foreach (QuestPool pool in pools)
            {
                RegeneratePool(pool);
                SaveToDB(pool, trans);
            }
            DB.Characters.CommitTransaction(trans);
        }

        // the storage structure ends up making this kind of inefficient
        // we don't use it in practice (only in debug commands), so that's fine
        public QuestPool FindQuestPool(uint poolId)
        {
            bool lambda(QuestPool p) { return p.poolId == poolId; };

            var questPool = _dailyPools.Find(lambda);
            if (questPool != null)
                return questPool;

            questPool = _weeklyPools.Find(lambda);
            if (questPool != null)
                return questPool;

            questPool = _monthlyPools.Find(lambda);
            if (questPool != null)
                return questPool;

            return null;
        }

        public bool IsQuestActive(uint questId)
        {
            var it = _poolLookup.LookupByKey(questId);
            if (it == null) // not pooled
                return true;

            return it.activeQuests.Contains(questId);
        }

        public void ChangeDailyQuests() { Regenerate(_dailyPools); }
        public void ChangeWeeklyQuests() { Regenerate(_weeklyPools); }
        public void ChangeMonthlyQuests() { Regenerate(_monthlyPools); }

        public bool IsQuestPooled(uint questId) { return _poolLookup.ContainsKey(questId); }
    }

    public class QuestPool
    {
        public uint poolId;
        public uint numActive;
        public MultiMap<uint, uint> members = new();
        public List<uint> activeQuests = new();
    }
}
