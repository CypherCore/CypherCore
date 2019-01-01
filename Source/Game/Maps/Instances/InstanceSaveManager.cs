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
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Scenarios;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Maps
{
    public class InstanceSaveManager : Singleton<InstanceSaveManager>
    {
        InstanceSaveManager() { }

        public InstanceSave AddInstanceSave(uint mapId, uint instanceId, Difficulty difficulty, long resetTime, uint entranceId, bool canReset, bool load = false)
        {
            InstanceSave old_save = GetInstanceSave(instanceId);
            if (old_save != null)
                return old_save;

            MapRecord entry = CliDB.MapStorage.LookupByKey(mapId);
            if (entry == null)
            {
                Log.outError(LogFilter.Server, "InstanceSaveManager.AddInstanceSave: wrong mapid = {0}, instanceid = {1}!", mapId, instanceId);
                return null;
            }

            if (instanceId == 0)
            {
                Log.outError(LogFilter.Server, "InstanceSaveManager.AddInstanceSave: mapid = {0}, wrong instanceid = {1}!", mapId, instanceId);
                return null;
            }

            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (difficultyEntry == null || difficultyEntry.InstanceType != entry.InstanceType)
            {
                Log.outError(LogFilter.Server, "InstanceSaveManager.AddInstanceSave: mapid = {0}, instanceid = {1}, wrong dificalty {2}!", mapId, instanceId, difficulty);
                return null;
            }

            if (entranceId != 0 && !CliDB.WorldSafeLocsStorage.ContainsKey(entranceId))
            {
                Log.outWarn(LogFilter.Misc, "InstanceSaveManager.AddInstanceSave: invalid entranceId = {0} defined for instance save with mapid = {1}, instanceid = {2}!", entranceId, mapId, instanceId);
                entranceId = 0;
            }

            if (resetTime == 0)
            {
                // initialize reset time
                // for normal instances if no creatures are killed the instance will reset in two hours
                if (entry.InstanceType == MapTypes.Raid || difficulty > Difficulty.Normal)
                    resetTime = GetResetTimeFor(mapId, difficulty);
                else
                {
                    resetTime = Time.UnixTime + 2 * Time.Hour;
                    // normally this will be removed soon after in InstanceMap.Add, prevent error
                    ScheduleReset(true, resetTime, new InstResetEvent(0, mapId, difficulty, instanceId));
                }
            }

            Log.outDebug(LogFilter.Maps, "InstanceSaveManager.AddInstanceSave: mapid = {0}, instanceid = {1}", mapId, instanceId);

            InstanceSave save = new InstanceSave(mapId, instanceId, difficulty, entranceId, resetTime, canReset);
            if (!load)
                save.SaveToDB();

            m_instanceSaveById[instanceId] = save;
            return save;
        }

        public InstanceSave GetInstanceSave(uint InstanceId)
        {
            return m_instanceSaveById.LookupByKey(InstanceId);
        }

        public void DeleteInstanceFromDB(uint instanceid)
        {
            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_INSTANCE_BY_INSTANCE);
            stmt.AddValue(0, instanceid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INSTANCE_BY_INSTANCE);
            stmt.AddValue(0, instanceid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GROUP_INSTANCE_BY_INSTANCE);
            stmt.AddValue(0, instanceid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_SCENARIO_INSTANCE_CRITERIA_FOR_INSTANCE);
            stmt.AddValue(0, instanceid);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
            // Respawn times should be deleted only when the map gets unloaded
        }

        public void RemoveInstanceSave(uint InstanceId)
        {
            var instanceSave = m_instanceSaveById.LookupByKey(InstanceId);
            if (instanceSave != null)
            {
                // save the resettime for normal instances only when they get unloaded
                long resettime = instanceSave.GetResetTimeForDB();
                if (resettime != 0)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_INSTANCE_RESETTIME);

                    stmt.AddValue(0, resettime);
                    stmt.AddValue(1, InstanceId);

                    DB.Characters.Execute(stmt);
                }

                instanceSave.SetToDelete(true);
                m_instanceSaveById.Remove(InstanceId);
            }
        }

        public void UnloadInstanceSave(uint InstanceId)
        {
            InstanceSave save = GetInstanceSave(InstanceId);
            if (save != null)
                save.UnloadIfEmpty();
        }

        public void LoadInstances()
        {
            uint oldMSTime = Time.GetMSTime();

            // Delete expired instances (Instance related spawns are removed in the following cleanup queries)
            DB.Characters.DirectExecute("DELETE i FROM instance i LEFT JOIN instance_reset ir ON mapid = map AND i.difficulty = ir.difficulty " +
                "WHERE (i.resettime > 0 AND i.resettime < UNIX_TIMESTAMP()) OR (ir.resettime IS NOT NULL AND ir.resettime < UNIX_TIMESTAMP())");

            // Delete invalid character_instance and group_instance references
            DB.Characters.DirectExecute("DELETE ci.* FROM character_instance AS ci LEFT JOIN characters AS c ON ci.guid = c.guid WHERE c.guid IS NULL");
            DB.Characters.DirectExecute("DELETE gi.* FROM group_instance     AS gi LEFT JOIN groups     AS g ON gi.guid = g.guid WHERE g.guid IS NULL");

            // Delete invalid instance references
            DB.Characters.DirectExecute("DELETE i.* FROM instance AS i LEFT JOIN character_instance AS ci ON i.id = ci.instance LEFT JOIN group_instance AS gi ON i.id = gi.instance WHERE ci.guid IS NULL AND gi.guid IS NULL");

            // Delete invalid references to instance
            DB.Characters.DirectExecute("DELETE FROM creature_respawn WHERE instanceId > 0 AND instanceId NOT IN (SELECT id FROM instance)");
            DB.Characters.DirectExecute("DELETE FROM gameobject_respawn WHERE instanceId > 0 AND instanceId NOT IN (SELECT id FROM instance)");
            DB.Characters.DirectExecute("DELETE tmp.* FROM character_instance AS tmp LEFT JOIN instance ON tmp.instance = instance.id WHERE tmp.instance > 0 AND instance.id IS NULL");
            DB.Characters.DirectExecute("DELETE tmp.* FROM group_instance     AS tmp LEFT JOIN instance ON tmp.instance = instance.id WHERE tmp.instance > 0 AND instance.id IS NULL");

            // Clean invalid references to instance
            DB.Characters.DirectExecute("UPDATE corpse SET instanceId = 0 WHERE instanceId > 0 AND instanceId NOT IN (SELECT id FROM instance)");
            DB.Characters.DirectExecute("UPDATE characters AS tmp LEFT JOIN instance ON tmp.instance_id = instance.id SET tmp.instance_id = 0 WHERE tmp.instance_id > 0 AND instance.id IS NULL");

            // Initialize instance id storage (Needs to be done after the trash has been clean out)
            Global.MapMgr.InitInstanceIds();

            // Load reset times and clean expired instances
            LoadResetTimes();

            Log.outInfo(LogFilter.ServerLoading, "Loaded instances in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        void LoadResetTimes()
        {
            long now = Time.UnixTime;
            long today = (now / Time.Day) * Time.Day;

            // NOTE: Use DirectPExecute for tables that will be queried later

            // get the current reset times for normal instances (these may need to be updated)
            // these are only kept in memory for InstanceSaves that are loaded later
            // resettime = 0 in the DB for raid/heroic instances so those are skipped
            Dictionary<uint, Tuple<uint, long>> instResetTime = new Dictionary<uint, Tuple<uint, long>>();

            // index instance ids by map/difficulty pairs for fast reset warning send
            MultiMap<uint, uint> mapDiffResetInstances = new MultiMap<uint, uint>();

            SQLResult result = DB.Characters.Query("SELECT id, map, difficulty, resettime FROM instance ORDER BY id ASC");
            if (!result.IsEmpty())
            {
                do
                {
                    uint instanceId = result.Read<uint>(0);

                    // Instances are pulled in ascending order from db and nextInstanceId is initialized with 1,
                    // so if the instance id is used, increment until we find the first unused one for a potential new instance
                    if (Global.MapMgr.GetNextInstanceId() == instanceId)
                        Global.MapMgr.SetNextInstanceId(instanceId + 1);

                    // Mark instance id as being used
                    Global.MapMgr.RegisterInstanceId(instanceId);
                    long resettime = result.Read<uint>(3);
                    if (resettime != 0)
                    {
                        uint mapid = result.Read<ushort>(1);
                        uint difficulty = result.Read<byte>(2);

                        instResetTime[instanceId] = Tuple.Create(MathFunctions.MakePair32(mapid, difficulty), resettime);
                        mapDiffResetInstances.Add(MathFunctions.MakePair32(mapid, difficulty), instanceId);
                    }
                }
                while (result.NextRow());

                // update reset time for normal instances with the max creature respawn time + X hours
                SQLResult result2 = DB.Characters.Query(DB.Characters.GetPreparedStatement(CharStatements.SEL_MAX_CREATURE_RESPAWNS));
                if (!result2.IsEmpty())
                {
                    do
                    {
                        uint instance = result2.Read<uint>(1);
                        long resettime = result2.Read<uint>(0) + 2 * Time.Hour;
                        var pair = instResetTime.LookupByKey(instance);
                        if (pair != null && pair.Item2 != resettime)
                        {
                            DB.Characters.DirectExecute("UPDATE instance SET resettime = '{0}' WHERE id = '{1}'", resettime, instance);
                            instResetTime[instance] = Tuple.Create(pair.Item1, resettime);
                        }
                    }
                    while (result2.NextRow());
                }

                // schedule the reset times
                foreach (var pair in instResetTime)
                    if (pair.Value.Item2 > now)
                        ScheduleReset(true, pair.Value.Item2, new InstResetEvent(0, MathFunctions.Pair32_LoPart(pair.Value.Item1), (Difficulty)MathFunctions.Pair32_HiPart(pair.Value.Item1), pair.Key));
            }

            // load the global respawn times for raid/heroic instances
            uint diff = (uint)(WorldConfig.GetIntValue(WorldCfg.InstanceResetTimeHour) * Time.Hour);
            result = DB.Characters.Query("SELECT mapid, difficulty, resettime FROM instance_reset");
            if (!result.IsEmpty())
            {
                do
                {
                    uint mapid = result.Read<ushort>(0);
                    Difficulty difficulty = (Difficulty)result.Read<byte>(1);
                    ulong oldresettime = result.Read<uint>(2);

                    MapDifficultyRecord mapDiff = Global.DB2Mgr.GetMapDifficultyData(mapid, difficulty);
                    if (mapDiff == null)
                    {
                        Log.outError(LogFilter.Server, "InstanceSaveManager.LoadResetTimes: invalid mapid({0})/difficulty({1}) pair in instance_reset!", mapid, difficulty);
                        DB.Characters.DirectExecute("DELETE FROM instance_reset WHERE mapid = '{0}' AND difficulty = '{1}'", mapid, difficulty);
                        continue;
                    }

                    // update the reset time if the hour in the configs changes
                    ulong newresettime = (oldresettime / Time.Day) * Time.Day + diff;
                    if (oldresettime != newresettime)
                        DB.Characters.DirectExecute("UPDATE instance_reset SET resettime = '{0}' WHERE mapid = '{1}' AND difficulty = '{2}'", newresettime, mapid, difficulty);

                    InitializeResetTimeFor(mapid, difficulty, (long)newresettime);
                } while (result.NextRow());
            }

            // calculate new global reset times for expired instances and those that have never been reset yet
            // add the global reset times to the priority queue
            foreach (var mapDifficultyPair in Global.DB2Mgr.GetMapDifficulties())
            {
                uint mapid = mapDifficultyPair.Key;

                foreach (var difficultyPair in mapDifficultyPair.Value)
                {
                    Difficulty difficulty = (Difficulty)difficultyPair.Key;
                    MapDifficultyRecord mapDiff = difficultyPair.Value;
                    if (mapDiff.GetRaidDuration() == 0)
                        continue;

                    // the reset_delay must be at least one day
                    uint period = (uint)(((mapDiff.GetRaidDuration() * WorldConfig.GetFloatValue(WorldCfg.RateInstanceResetTime)) / Time.Day) * Time.Day);
                    if (period < Time.Day)
                        period = Time.Day;

                    long t = GetResetTimeFor(mapid, difficulty);
                    if (t == 0)
                    {
                        // initialize the reset time
                        t = today + period + diff;
                        DB.Characters.DirectExecute("INSERT INTO instance_reset VALUES ('{0}', '{1}', '{2}')", mapid, (uint)difficulty, (uint)t);
                    }

                    if (t < now)
                    {
                        // assume that expired instances have already been cleaned
                        // calculate the next reset time
                        t = (t / Time.Day) * Time.Day;
                        t += ((today - t) / period + 1) * period + diff;
                        DB.Characters.DirectExecute("UPDATE instance_reset SET resettime = '{0}' WHERE mapid = '{1}' AND difficulty= '{2}'", t, mapid, (uint)difficulty);
                    }

                    InitializeResetTimeFor(mapid, difficulty, t);

                    // schedule the global reset/warning
                    byte type;
                    for (type = 1; type < 4; ++type)
                        if (t - ResetTimeDelay[type - 1] > now)
                            break;

                    ScheduleReset(true, t - ResetTimeDelay[type - 1], new InstResetEvent(type, mapid, difficulty, 0));

                    var range = mapDiffResetInstances.LookupByKey(MathFunctions.MakePair32(mapid, (uint)difficulty));
                    foreach (var id in range)
                        ScheduleReset(true, t - ResetTimeDelay[type - 1], new InstResetEvent(type, mapid, difficulty, id));

                }
            }
        }

        public long GetSubsequentResetTime(uint mapid, Difficulty difficulty, long resetTime)
        {
            MapDifficultyRecord mapDiff = Global.DB2Mgr.GetMapDifficultyData(mapid, difficulty);
            if (mapDiff == null || mapDiff.GetRaidDuration() == 0)
            {
                Log.outError(LogFilter.Misc, "InstanceSaveManager.GetSubsequentResetTime: not valid difficulty or no reset delay for map {0}", mapid);
                return 0;
            }

            long diff = WorldConfig.GetIntValue(WorldCfg.InstanceResetTimeHour) * Time.Hour;
            long period = (uint)(((mapDiff.GetRaidDuration() * WorldConfig.GetFloatValue(WorldCfg.RateInstanceResetTime)) / Time.Day) * Time.Day);
            if (period < Time.Day)
                period = Time.Day;

            return ((resetTime + Time.Minute) / Time.Day * Time.Day) + period + diff;
        }

        public void ScheduleReset(bool add, long time, InstResetEvent Event)
        {
            if (!add)
            {
                // find the event in the queue and remove it
                var range = m_resetTimeQueue.LookupByKey(time);
                foreach (var instResetEvent in range)
                {
                    if (instResetEvent == Event)
                    {
                        m_resetTimeQueue.Remove(time, instResetEvent);
                        return;
                    }
                }

                // in case the reset time changed (should happen very rarely), we search the whole queue
                foreach (var pair in m_resetTimeQueue)
                {
                    if (pair.Value == Event)
                    {
                        m_resetTimeQueue.Remove(pair);
                        return;
                    }
                }

                Log.outError(LogFilter.Server, "InstanceSaveManager.ScheduleReset: cannot cancel the reset, the event({0}, {1}, {2}) was not found!", Event.type, Event.mapid, Event.instanceId);
            }
            else
                m_resetTimeQueue.Add(time, Event);
        }

        public void ForceGlobalReset(uint mapId, Difficulty difficulty)
        {
            if (Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty) == null)
                return;
            // remove currently scheduled reset times
            ScheduleReset(false, 0, new InstResetEvent(1, mapId, difficulty, 0));
            ScheduleReset(false, 0, new InstResetEvent(4, mapId, difficulty, 0));
            // force global reset on the instance
            _ResetOrWarnAll(mapId, difficulty, false, Time.UnixTime);
        }

        public void Update()
        {
            long now = Time.UnixTime;

            while (!m_resetTimeQueue.Empty())
            {
                var pair = m_resetTimeQueue.First();
                long time = pair.Key;
                if (time >= now)
                    break;

                InstResetEvent Event = pair.Value;
                if (Event.type == 0)
                {
                    // for individual normal instances, max creature respawn + X hours
                    _ResetInstance(Event.mapid, Event.instanceId);
                    m_resetTimeQueue.Remove(pair);
                }
                else
                {
                    // global reset/warning for a certain map
                    long resetTime = GetResetTimeFor(Event.mapid, Event.difficulty);
                    _ResetOrWarnAll(Event.mapid, Event.difficulty, Event.type != 4, resetTime);
                    if (Event.type != 4)
                    {
                        // schedule the next warning/reset
                        ++Event.type;
                        ScheduleReset(true, resetTime - ResetTimeDelay[Event.type - 1], Event);
                    }
                    m_resetTimeQueue.Remove(pair);
                }
            }
        }

        void _ResetSave(KeyValuePair<uint, InstanceSave> pair)
        {
            // unbind all players bound to the instance
            // do not allow UnbindInstance to automatically unload the InstanceSaves
            lock_instLists = true;

            bool shouldDelete = true;
            var pList = pair.Value.m_playerList;
            List<Player> temp = new List<Player>(); // list of expired binds that should be unbound
            foreach (var player in pList)
            {
                InstanceBind bind = player.GetBoundInstance(pair.Value.GetMapId(), pair.Value.GetDifficultyID());
                if (bind != null)
                {
                    Cypher.Assert(bind.save == pair.Value);
                    if (bind.perm && bind.extendState != 0) // permanent and not already expired
                    {
                        // actual promotion in DB already happened in caller
                        bind.extendState = bind.extendState == BindExtensionState.Extended ? BindExtensionState.Normal : BindExtensionState.Expired;
                        shouldDelete = false;
                        continue;
                    }
                }
                temp.Add(player);
            }

            var gList = pair.Value.m_groupList;
            while (!gList.Empty())
            {
                Group group = gList.First();
                group.UnbindInstance(pair.Value.GetMapId(), pair.Value.GetDifficultyID(), true);
            }

            if (shouldDelete)
                m_instanceSaveById.Remove(pair.Key);

            lock_instLists = false;
        }

        void _ResetInstance(uint mapid, uint instanceId)
        {
            Log.outDebug(LogFilter.Maps, "InstanceSaveMgr._ResetInstance {0}, {1}", mapid, instanceId);
            Map map = Global.MapMgr.CreateBaseMap(mapid);
            if (!map.Instanceable())
                return;

            var pair = m_instanceSaveById.Find(instanceId);
            if (pair.Value != null)
                _ResetSave(pair);

            DeleteInstanceFromDB(instanceId);                       // even if save not loaded

            Map iMap = ((MapInstanced)map).FindInstanceMap(instanceId);

            if (iMap != null && iMap.IsDungeon())
                ((InstanceMap)iMap).Reset(InstanceResetMethod.RespawnDelay);

            if (iMap != null)
            {
                iMap.DeleteRespawnTimes();
                iMap.DeleteCorpseData();
            }
            else
                Map.DeleteRespawnTimesInDB(mapid, instanceId);

            // Free up the instance id and allow it to be reused
            Global.MapMgr.FreeInstanceId(instanceId);
        }

        void _ResetOrWarnAll(uint mapid, Difficulty difficulty, bool warn, long resetTime)
        {
            // global reset for all instances of the given map
            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapid);
            if (!mapEntry.Instanceable())
                return;

            Log.outDebug(LogFilter.Misc, "InstanceSaveManager.ResetOrWarnAll: Processing map {0} ({1}) on difficulty {2} (warn? {3})", mapEntry.MapName[Global.WorldMgr.GetDefaultDbcLocale()], mapid, difficulty, warn);
            long now = Time.UnixTime;

            if (!warn)
            {
                // calculate the next reset time
                long next_reset = GetSubsequentResetTime(mapid, difficulty, resetTime);
                if (next_reset == 0)
                    return;

                // delete them from the DB, even if not loaded
                SQLTransaction trans = new SQLTransaction();

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EXPIRED_CHAR_INSTANCE_BY_MAP_DIFF);
                stmt.AddValue(0, mapid);
                stmt.AddValue(1, (byte)difficulty);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GROUP_INSTANCE_BY_MAP_DIFF);
                stmt.AddValue(0, mapid);
                stmt.AddValue(1, (byte)difficulty);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EXPIRED_INSTANCE_BY_MAP_DIFF);
                stmt.AddValue(0, mapid);
                stmt.AddValue(1, (byte)difficulty);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_EXPIRE_CHAR_INSTANCE_BY_MAP_DIFF);
                stmt.AddValue(0, mapid);
                stmt.AddValue(1, (byte)difficulty);
                trans.Append(stmt);

                DB.Characters.CommitTransaction(trans);

                // promote loaded binds to instances of the given map
                foreach (var pair in m_instanceSaveById.ToList())
                {
                    if (pair.Value.GetMapId() == mapid && pair.Value.GetDifficultyID() == difficulty)
                        _ResetSave(pair);
                }

                SetResetTimeFor(mapid, difficulty, next_reset);
                ScheduleReset(true, next_reset - 3600, new InstResetEvent(1, mapid, difficulty, 0));

                // Update it in the DB
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GLOBAL_INSTANCE_RESETTIME);
                stmt.AddValue(0, (uint)next_reset);
                stmt.AddValue(1, (ushort)mapid);
                stmt.AddValue(2, (byte)difficulty);

                DB.Characters.Execute(stmt);
            }

            // note: this isn't fast but it's meant to be executed very rarely
            Map map = Global.MapMgr.CreateBaseMap(mapid);          // _not_ include difficulty
            var instMaps = ((MapInstanced)map).GetInstancedMaps();
            uint timeLeft;

            foreach (var pair in instMaps)
            {
                Map map2 = pair.Value;
                if (!map2.IsDungeon())
                    continue;

                if (warn)
                {
                    if (now >= resetTime)
                        timeLeft = 0;
                    else
                        timeLeft = (uint)(resetTime - now);

                    ((InstanceMap)map2).SendResetWarnings(timeLeft);
                }
                else
                    ((InstanceMap)map2).Reset(InstanceResetMethod.Global);
            }

            // @todo delete creature/gameobject respawn times even if the maps are not loaded
        }

        public uint GetNumBoundPlayersTotal()
        {
            uint ret = 0;
            foreach (var pair in m_instanceSaveById)
                ret += pair.Value.GetPlayerCount();

            return ret;
        }

        public uint GetNumBoundGroupsTotal()
        {
            uint ret = 0;
            foreach (var pair in m_instanceSaveById)
                ret += pair.Value.GetGroupCount();

            return ret;
        }


        public long GetResetTimeFor(uint mapid, Difficulty d)
        {
            return m_resetTimeByMapDifficulty.LookupByKey(MathFunctions.MakePair64(mapid, (uint)d));
        }

        // Use this on startup when initializing reset times
        void InitializeResetTimeFor(uint mapid, Difficulty d, long t)
        {
            m_resetTimeByMapDifficulty[MathFunctions.MakePair64(mapid, (uint)d)] = t;
        }
        // Use this only when updating existing reset times
        void SetResetTimeFor(uint mapid, Difficulty d, long t)
        {
            var key = MathFunctions.MakePair64(mapid, (uint)d);
            Cypher.Assert(m_resetTimeByMapDifficulty.ContainsKey(key));
            m_resetTimeByMapDifficulty[key] = t;
        }

        public Dictionary<ulong, long> GetResetTimeMap()
        {
            return m_resetTimeByMapDifficulty;
        }

        public int GetNumInstanceSaves() { return m_instanceSaveById.Count; }

        public class InstResetEvent
        {
            public InstResetEvent(byte t = 0, uint _mapid = 0, Difficulty d = Difficulty.Normal, uint _instanceid = 0)
            {
                type = t;
                difficulty = d;
                mapid = _mapid;
                instanceId = _instanceid;
            }

            public byte type;
            public Difficulty difficulty;
            public uint mapid;
            public uint instanceId;
        }

        static ushort[] ResetTimeDelay = { 3600, 900, 300, 60 };

        // used during global instance resets
        public bool lock_instLists;
        // fast lookup by instance id
        Dictionary<uint, InstanceSave> m_instanceSaveById = new Dictionary<uint, InstanceSave>();
        // fast lookup for reset times (always use existed functions for access/set)
        Dictionary<ulong, long> m_resetTimeByMapDifficulty = new Dictionary<ulong, long>();
        MultiMap<long, InstResetEvent> m_resetTimeQueue = new MultiMap<long, InstResetEvent>();
    }

    public class InstanceSave
    {
        public InstanceSave(uint MapId, uint InstanceId, Difficulty difficulty, uint entranceId,  long resetTime, bool canReset)
        {
            m_resetTime = resetTime;
            m_instanceid = InstanceId;
            m_mapid = MapId;
            m_difficulty = difficulty;
            m_entranceId = entranceId;
            m_canReset = canReset;
            m_toDelete = false;
        }

        public void SaveToDB()
        {
            // save instance data too
            string data = "";
            uint completedEncounters = 0;

            Map map = Global.MapMgr.FindMap(GetMapId(), m_instanceid);
            if (map != null)
            {
                Cypher.Assert(map.IsDungeon());
                InstanceScript instanceScript = ((InstanceMap)map).GetInstanceScript();
                if (instanceScript != null)
                {
                    data = instanceScript.GetSaveData();
                    completedEncounters = instanceScript.GetCompletedEncounterMask();
                    m_entranceId = instanceScript.GetEntranceLocation();
                }

                InstanceScenario scenario = map.ToInstanceMap().GetInstanceScenario();
                if (scenario != null)
                    scenario.SaveToDB();
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_INSTANCE_SAVE);
            stmt.AddValue(0, m_instanceid);
            stmt.AddValue(1, GetMapId());
            stmt.AddValue(2, GetResetTimeForDB());
            stmt.AddValue(3, (uint)GetDifficultyID());
            stmt.AddValue(4, completedEncounters);
            stmt.AddValue(5, data);
            stmt.AddValue(6, m_entranceId);
            DB.Characters.Execute(stmt);
        }

        public long GetResetTimeForDB()
        {
            // only save the reset time for normal instances
            MapRecord entry = CliDB.MapStorage.LookupByKey(GetMapId());
            if (entry == null || entry.InstanceType == MapTypes.Raid || GetDifficultyID() == Difficulty.Heroic)
                return 0;
            else
                return GetResetTime();
        }

        InstanceTemplate GetTemplate()
        {
            return Global.ObjectMgr.GetInstanceTemplate(m_mapid);
        }

        MapRecord GetMapEntry()
        {
            return CliDB.MapStorage.LookupByKey(m_mapid);
        }

        public void DeleteFromDB()
        {
          Global.InstanceSaveMgr.DeleteInstanceFromDB(GetInstanceId());
        }

        public bool UnloadIfEmpty()
        {
            if (m_playerList.Empty() && m_groupList.Empty())
            {
                if (!Global.InstanceSaveMgr.lock_instLists)
                    Global.InstanceSaveMgr.RemoveInstanceSave(GetInstanceId());

                return false;
            }
            else
                return true;
        }

        public uint GetPlayerCount() { return (uint)m_playerList.Count; }
        public uint GetGroupCount() { return (uint)m_groupList.Count; }

        public uint GetInstanceId() { return m_instanceid; }
        public uint GetMapId() { return m_mapid; }

        public long GetResetTime() { return m_resetTime; }
        public void SetResetTime(long resetTime) { m_resetTime = resetTime; }

        public uint GetEntranceLocation() { return m_entranceId; }
        void SetEntranceLocation(uint entranceId) { m_entranceId = entranceId; }

        public void AddPlayer(Player player)
        {
            m_playerList.Add(player);
        }
        public bool RemovePlayer(Player player)
        {
            m_playerList.Remove(player);

            return UnloadIfEmpty();
        }

        public void AddGroup(Group group) { m_groupList.Add(group); }
        public bool RemoveGroup(Group group)
        {
            m_groupList.Remove(group);

            return UnloadIfEmpty();
        }

        public bool CanReset() { return m_canReset; }
        public void SetCanReset(bool canReset) { m_canReset = canReset; }

        public Difficulty GetDifficultyID() { return m_difficulty; }

        public void SetToDelete(bool toDelete)
        {
            m_toDelete = toDelete;
        }

        public List<Player> m_playerList = new List<Player>();
        public List<Group> m_groupList = new List<Group>();
        long m_resetTime;
        uint m_instanceid;
        uint m_mapid;
        Difficulty m_difficulty;
        uint m_entranceId;
        bool m_canReset;
        bool m_toDelete;
    }
}
