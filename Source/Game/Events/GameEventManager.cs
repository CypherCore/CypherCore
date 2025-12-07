// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class GameEventManager : Singleton<GameEventManager>
    {
        GameEventManager() { }

        bool CheckOneGameEvent(ushort entry)
        {
            switch (mGameEvent[entry].state)
            {
                default:
                case GameEventState.Normal:
                {
                    long currenttime = GameTime.GetGameTime();
                    // Get the event information
                    return mGameEvent[entry].start < currenttime
                        && currenttime < mGameEvent[entry].end
                        && (currenttime - mGameEvent[entry].start) % (mGameEvent[entry].occurence * Time.Minute) < mGameEvent[entry].length * Time.Minute;
                }
                // if the state is conditions or nextphase, then the event should be active
                case GameEventState.WorldConditions:
                case GameEventState.WorldNextPhase:
                    return true;
                // finished world events are inactive
                case GameEventState.WorldFinished:
                case GameEventState.Internal:
                    return false;
                // if inactive world event, check the prerequisite events
                case GameEventState.WorldInactive:
                {
                    long currenttime = GameTime.GetGameTime();
                    foreach (var gameEventId in mGameEvent[entry].prerequisite_events)
                    {
                        if ((mGameEvent[gameEventId].state != GameEventState.WorldNextPhase && mGameEvent[gameEventId].state != GameEventState.WorldFinished) ||   // if prereq not in nextphase or finished state, then can't start this one
                            mGameEvent[gameEventId].nextstart > currenttime)               // if not in nextphase state for long enough, can't start this one
                            return false;
                    }
                    // all prerequisite events are met
                    // but if there are no prerequisites, this can be only activated through gm command
                    return !(mGameEvent[entry].prerequisite_events.Empty());
                }
            }
        }

        public uint NextCheck(ushort entry)
        {
            long currenttime = GameTime.GetGameTime();

            // for NEXTPHASE state world events, return the delay to start the next event, so the followup event will be checked correctly
            if ((mGameEvent[entry].state == GameEventState.WorldNextPhase || mGameEvent[entry].state == GameEventState.WorldFinished) && mGameEvent[entry].nextstart >= currenttime)
                return (uint)(mGameEvent[entry].nextstart - currenttime);

            // for CONDITIONS state world events, return the length of the wait period, so if the conditions are met, this check will be called again to set the timer as NEXTPHASE event
            if (mGameEvent[entry].state == GameEventState.WorldConditions)
            {
                if (mGameEvent[entry].length != 0)
                    return mGameEvent[entry].length * 60;
                else
                    return Time.Day;
            }

            // outdated event: we return max
            if (currenttime > mGameEvent[entry].end)
                return Time.Day;

            // never started event, we return delay before start
            if (mGameEvent[entry].start > currenttime)
                return (uint)(mGameEvent[entry].start - currenttime);

            uint delay;
            // in event, we return the end of it
            if ((((currenttime - mGameEvent[entry].start) % (mGameEvent[entry].occurence * 60)) < (mGameEvent[entry].length * 60)))
                // we return the delay before it ends
                delay = (uint)((mGameEvent[entry].length * Time.Minute) - ((currenttime - mGameEvent[entry].start) % (mGameEvent[entry].occurence * Time.Minute)));
            else                                                    // not in window, we return the delay before next start
                delay = (uint)((mGameEvent[entry].occurence * Time.Minute) - ((currenttime - mGameEvent[entry].start) % (mGameEvent[entry].occurence * Time.Minute)));
            // In case the end is before next check
            if (mGameEvent[entry].end < currenttime + delay)
                return (uint)(mGameEvent[entry].end - currenttime);
            else
                return delay;
        }

        void StartInternalEvent(ushort event_id)
        {
            if (event_id < 1 || event_id >= mGameEvent.Length)
                return;

            if (!mGameEvent[event_id].IsValid())
                return;

            if (m_ActiveEvents.Contains(event_id))
                return;

            StartEvent(event_id);
        }

        public bool StartEvent(ushort event_id, bool overwrite = false)
        {
            GameEventData data = mGameEvent[event_id];
            if (data.state == GameEventState.Normal || data.state == GameEventState.Internal)
            {
                AddActiveEvent(event_id);
                ApplyNewEvent(event_id);
                if (overwrite)
                {
                    mGameEvent[event_id].start = GameTime.GetGameTime();
                    if (data.end <= data.start)
                        data.end = data.start + data.length;
                }

                return false;
            }
            else
            {
                if (data.state == GameEventState.WorldInactive)
                    // set to conditions phase
                    data.state = GameEventState.WorldConditions;

                // add to active events
                AddActiveEvent(event_id);
                // add spawns
                ApplyNewEvent(event_id);

                // check if can go to next state
                bool conditions_met = CheckOneGameEventConditions(event_id);
                // save to db
                SaveWorldEventStateToDB(event_id);
                // force game event update to set the update timer if conditions were met from a command
                // this update is needed to possibly start events dependent on the started one
                // or to scedule another update where the next event will be started
                if (overwrite && conditions_met)
                    Global.WorldMgr.ForceGameEventUpdate();

                return conditions_met;
            }
        }

        public void StopEvent(ushort event_id, bool overwrite = false)
        {
            GameEventData data = mGameEvent[event_id];
            bool serverwide_evt = data.state != GameEventState.Normal && data.state != GameEventState.Internal;

            RemoveActiveEvent(event_id);
            UnApplyEvent(event_id);

            if (overwrite && !serverwide_evt)
            {
                data.start = GameTime.GetGameTime() - data.length * Time.Minute;
                if (data.end <= data.start)
                    data.end = data.start + data.length;
            }
            else if (serverwide_evt)
            {
                // if finished world event, then only gm command can stop it
                if (overwrite || data.state != GameEventState.WorldFinished)
                {
                    // reset conditions
                    data.nextstart = 0;
                    data.state = GameEventState.WorldInactive;
                    foreach (var pair in data.conditions)
                        pair.Value.done = 0;

                    SQLTransaction trans = new();
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_GAME_EVENT_CONDITION_SAVE);
                    stmt.AddValue(0, event_id);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
                    stmt.AddValue(0, event_id);
                    trans.Append(stmt);

                    DB.Characters.CommitTransaction(trans);
                }
            }
        }

        public void LoadFromDB()
        {
            {
                uint oldMSTime = Time.GetMSTime();
                //                                         0           1                           2                         3          4       5        6            7            8             9
                SQLResult result = DB.World.Query("SELECT eventEntry, UNIX_TIMESTAMP(start_time), UNIX_TIMESTAMP(end_time), occurence, length, holiday, holidayStage, description, world_event, announce FROM game_event");
                if (result.IsEmpty())
                {
                    mGameEvent.Clear();
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 game events. DB table `game_event` is empty.");
                    return;
                }

                uint count = 0;
                do
                {
                    byte event_id = result.Read<byte>(0);
                    if (event_id == 0)
                    {
                        Log.outError(LogFilter.Sql, "`game_event` game event entry 0 is reserved and can't be used.");
                        continue;
                    }

                    GameEventData pGameEvent = new();
                    ulong starttime = result.Read<ulong>(1);
                    pGameEvent.start = (long)starttime;
                    ulong endtime = result.Read<ulong>(2);
                    pGameEvent.end = (long)endtime;
                    pGameEvent.occurence = result.Read<uint>(3);
                    pGameEvent.length = result.Read<uint>(4);
                    pGameEvent.holiday_id = (HolidayIds)result.Read<uint>(5);

                    pGameEvent.holidayStage = result.Read<byte>(6);
                    pGameEvent.description = result.Read<string>(7);
                    pGameEvent.state = (GameEventState)result.Read<byte>(8);
                    pGameEvent.announce = result.Read<byte>(9);
                    pGameEvent.nextstart = 0;

                    ++count;

                    if (pGameEvent.length == 0 && pGameEvent.state == GameEventState.Normal)                            // length>0 is validity check
                    {
                        Log.outError(LogFilter.Sql, $"`game_event` game event id ({event_id}) isn't a world event and has length = 0, thus it can't be used.");
                        continue;
                    }

                    if (pGameEvent.holiday_id != HolidayIds.None)
                    {
                        if (!CliDB.HolidaysStorage.ContainsKey((uint)pGameEvent.holiday_id))
                        {
                            Log.outError(LogFilter.Sql, $"`game_event` game event id ({event_id}) contains nonexisting holiday id {pGameEvent.holiday_id}.");
                            pGameEvent.holiday_id = HolidayIds.None;
                            continue;
                        }
                        if (pGameEvent.holidayStage > SharedConst.MaxHolidayDurations)
                        {
                            Log.outError(LogFilter.Sql, "`game_event` game event id ({event_id}) has out of range holidayStage {pGameEvent.holidayStage}.");
                            pGameEvent.holidayStage = 0;
                            continue;
                        }

                        SetHolidayEventTime(pGameEvent);
                    }

                    mGameEvent[event_id] = pGameEvent;
                }
                while (result.NextRow());

                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Saves Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                       0       1        2
                SQLResult result = DB.Characters.Query("SELECT eventEntry, state, next_start FROM game_event_save");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 game event saves in game events. DB table `game_event_save` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        byte event_id = result.Read<byte>(0);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_save` game event entry ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        if (mGameEvent[event_id].state != GameEventState.Normal && mGameEvent[event_id].state != GameEventState.Internal)
                        {
                            mGameEvent[event_id].state = (GameEventState)result.Read<byte>(1);
                            mGameEvent[event_id].nextstart = result.Read<uint>(2);
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, "game_event_save includes event save for non-worldevent id {0}", event_id);
                            continue;
                        }

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} game event saves in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Prerequisite Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                   0             1
                SQLResult result = DB.World.Query("SELECT eventEntry, prerequisite_event FROM game_event_prerequisite");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 game event prerequisites in game events. DB table `game_event_prerequisite` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        ushort event_id = result.Read<byte>(0);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_prerequisite` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);
                            continue;
                        }

                        if (mGameEvent[event_id].state != GameEventState.Normal && mGameEvent[event_id].state != GameEventState.Internal)
                        {
                            ushort prerequisite_event = result.Read<byte>(1);
                            if (prerequisite_event >= mGameEvent.Length)
                            {
                                Log.outError(LogFilter.Sql, "`game_event_prerequisite` game event prerequisite id ({0}) not exist in `game_event`", prerequisite_event);
                                continue;
                            }
                            mGameEvent[event_id].prerequisite_events.Add(prerequisite_event);
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, "game_event_prerequisiste includes event entry for non-worldevent id {0}", event_id);
                            continue;
                        }

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} game event prerequisites in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Creature Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                 0        1
                SQLResult result = DB.World.Query("SELECT guid, eventEntry FROM game_event_creature");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creatures in game events. DB table `game_event_creature` is empty");
                else
                {
                    uint count = 0;
                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        short event_id = result.Read<sbyte>(1);
                        int internal_event_id = mGameEvent.Length + event_id - 1;

                        CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_creature` contains creature (GUID: {0}) not found in `creature` table.", guid);
                            continue;
                        }

                        if (internal_event_id < 0 || internal_event_id >= mGameEventCreatureGuids.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_creature` game event id ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        // Log error for pooled object, but still spawn it
                        if (data.poolId != 0)
                            Log.outError(LogFilter.Sql, $"`game_event_creature`: game event id ({event_id}) contains creature ({guid}) which is part of a pool ({data.poolId}). This should be spawned in game_event_pool");

                        mGameEventCreatureGuids[internal_event_id].Add(guid);

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creatures in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event GO Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                0         1
                SQLResult result = DB.World.Query("SELECT guid, eventEntry FROM game_event_gameobject");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 gameobjects in game events. DB table `game_event_gameobject` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        short event_id = result.Read<byte>(1);
                        int internal_event_id = mGameEvent.Length + event_id - 1;

                        GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);
                        if (data == null)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_gameobject` contains gameobject (GUID: {0}) not found in `gameobject` table.", guid);
                            continue;
                        }

                        if (internal_event_id < 0 || internal_event_id >= mGameEventGameobjectGuids.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_gameobject` game event id ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        // Log error for pooled object, but still spawn it
                        if (data.poolId != 0)
                            Log.outError(LogFilter.Sql, $"`game_event_gameobject`: game event id ({event_id}) contains game object ({guid}) which is part of a pool ({data.poolId}). This should be spawned in game_event_pool");

                        mGameEventGameobjectGuids[internal_event_id].Add(guid);

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} gameobjects in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Model/Equipment Change Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                       0           1                       2                                 3                                     4
                SQLResult result = DB.World.Query("SELECT creature.guid, creature.id, game_event_model_equip.eventEntry, game_event_model_equip.modelid, game_event_model_equip.equipment_id " +
                        "FROM creature JOIN game_event_model_equip ON creature.guid=game_event_model_equip.guid");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 model/equipment changes in game events. DB table `game_event_model_equip` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        uint entry = result.Read<uint>(1);
                        ushort event_id = result.Read<byte>(2);

                        if (event_id >= mGameEventModelEquip.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_model_equip` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);
                            continue;
                        }

                        ModelEquip newModelEquipSet = new();
                        newModelEquipSet.modelid = result.Read<uint>(3);
                        newModelEquipSet.equipment_id = result.Read<byte>(4);
                        newModelEquipSet.equipement_id_prev = 0;
                        newModelEquipSet.modelid_prev = 0;

                        if (newModelEquipSet.equipment_id > 0)
                        {
                            sbyte equipId = (sbyte)newModelEquipSet.equipment_id;
                            if (Global.ObjectMgr.GetEquipmentInfo(entry, equipId) == null)
                            {
                                Log.outError(LogFilter.Sql, "Table `game_event_model_equip` have creature (Guid: {0}, entry: {1}) with equipment_id {2} not found in table `creature_equip_template`, set to no equipment.",
                                    guid, entry, newModelEquipSet.equipment_id);
                                continue;
                            }
                        }

                        mGameEventModelEquip[event_id].Add(Tuple.Create(guid, newModelEquipSet));

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} model/equipment changes in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Quest Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                               0     1      2
                SQLResult result = DB.World.Query("SELECT id, quest, eventEntry FROM game_event_creature_quest");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 quests additions in game events. DB table `game_event_creature_quest` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        uint id = result.Read<uint>(0);
                        uint quest = result.Read<uint>(1);
                        ushort event_id = result.Read<byte>(2);

                        if (event_id >= mGameEventCreatureQuests.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_creature_quest` game event id ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        mGameEventCreatureQuests[event_id].Add(Tuple.Create(id, quest));

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event GO Quest Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                               0     1      2
                SQLResult result = DB.World.Query("SELECT id, quest, eventEntry FROM game_event_gameobject_quest");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 go quests additions in game events. DB table `game_event_gameobject_quest` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        uint id = result.Read<uint>(0);
                        uint quest = result.Read<uint>(1);
                        ushort event_id = result.Read<byte>(2);

                        if (event_id >= mGameEventGameObjectQuests.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_gameobject_quest` game event id ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        mGameEventGameObjectQuests[event_id].Add(Tuple.Create(id, quest));

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Quest Condition Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                 0       1         2             3
                SQLResult result = DB.World.Query("SELECT quest, eventEntry, condition_id, num FROM game_event_quest_condition");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 quest event conditions in game events. DB table `game_event_quest_condition` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        uint quest = result.Read<uint>(0);
                        ushort event_id = result.Read<byte>(1);
                        uint condition = result.Read<uint>(2);
                        float num = result.Read<float>(3);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_quest_condition` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);
                            continue;
                        }

                        if (!mQuestToEventConditions.ContainsKey(quest))
                            mQuestToEventConditions[quest] = new GameEventQuestToEventConditionNum();

                        mQuestToEventConditions[quest].event_id = event_id;
                        mQuestToEventConditions[quest].condition = condition;
                        mQuestToEventConditions[quest].num = num;

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quest event conditions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Condition Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                  0          1            2             3                      4
                SQLResult result = DB.World.Query("SELECT eventEntry, condition_id, req_num, max_world_state_field, done_world_state_field FROM game_event_condition");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 conditions in game events. DB table `game_event_condition` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        ushort event_id = result.Read<byte>(0);
                        uint condition = result.Read<uint>(1);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_condition` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);
                            continue;
                        }

                        mGameEvent[event_id].conditions[condition].reqNum = result.Read<float>(2);
                        mGameEvent[event_id].conditions[condition].done = 0;
                        mGameEvent[event_id].conditions[condition].max_world_state = result.Read<ushort>(3);
                        mGameEvent[event_id].conditions[condition].done_world_state = result.Read<ushort>(4);

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} conditions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Condition Save Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                      0           1         2
                SQLResult result = DB.Characters.Query("SELECT eventEntry, condition_id, done FROM game_event_condition_save");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 condition saves in game events. DB table `game_event_condition_save` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        ushort event_id = result.Read<byte>(0);
                        uint condition = result.Read<uint>(1);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_condition_save` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);
                            continue;
                        }

                        if (mGameEvent[event_id].conditions.ContainsKey(condition))
                        {
                            mGameEvent[event_id].conditions[condition].done = result.Read<uint>(2);
                        }
                        else
                        {
                            Log.outError(LogFilter.Sql, "game_event_condition_save contains not present condition evt id {0} cond id {1}", event_id, condition);
                            continue;
                        }

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} condition saves in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event NPCflag Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                0       1        2
                SQLResult result = DB.World.Query("SELECT guid, eventEntry, npcflag FROM game_event_npcflag");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 npcflags in game events. DB table `game_event_npcflag` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        ulong guid = result.Read<ulong>(0);
                        ushort event_id = result.Read<byte>(1);
                        ulong npcflag = result.Read<ulong>(2);

                        if (event_id >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_npcflag` game event id ({0}) is out of range compared to max event id in `game_event`", event_id);
                            continue;
                        }

                        mGameEventNPCFlags[event_id].Add((guid, npcflag));

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} npcflags in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Seasonal Quest Relations...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                  0          1
                SQLResult result = DB.World.Query("SELECT questId, eventEntry FROM game_event_seasonal_questrelation");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 seasonal quests additions in game events. DB table `game_event_seasonal_questrelation` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        uint questId = result.Read<uint>(0);
                        ushort eventEntry = result.Read<byte>(1); // @todo Change to byte

                        Quest questTemplate = Global.ObjectMgr.GetQuestTemplate(questId);
                        if (questTemplate == null)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_seasonal_questrelation` quest id ({0}) does not exist in `quest_template`", questId);
                            continue;
                        }

                        if (eventEntry >= mGameEvent.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_seasonal_questrelation` event id ({0}) not exist in `game_event`", eventEntry);
                            continue;
                        }

                        questTemplate.SetEventIdForQuest(eventEntry);
                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} quests additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Vendor Additions Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                               0           1     2     3         4         5             6     7             8                  9
                SQLResult result = DB.World.Query("SELECT eventEntry, guid, item, maxcount, incrtime, ExtendedCost, type, BonusListIDs, PlayerConditionId, IgnoreFiltering FROM game_event_npc_vendor ORDER BY guid, slot ASC");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 vendor additions in game events. DB table `game_event_npc_vendor` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        byte event_id = result.Read<byte>(0);
                        ulong guid = result.Read<ulong>(1);

                        if (event_id >= mGameEventVendors.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_npc_vendor` game event id ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        // get the event npc flag for checking if the npc will be vendor during the event or not
                        ulong event_npc_flag = 0;
                        var flist = mGameEventNPCFlags[event_id];
                        foreach (var pair in flist)
                        {
                            if (pair.guid == guid)
                            {
                                event_npc_flag = pair.npcflag;
                                break;
                            }
                        }
                        // get creature entry
                        uint entry = 0;
                        CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                        if (data != null)
                            entry = data.Id;

                        VendorItem vItem = new();
                        vItem.item = result.Read<uint>(2);
                        vItem.maxcount = result.Read<uint>(3);
                        vItem.incrtime = result.Read<uint>(4);
                        vItem.ExtendedCost = result.Read<uint>(5);
                        vItem.Type = (ItemVendorType)result.Read<byte>(6);
                        vItem.PlayerConditionId = result.Read<uint>(8);
                        vItem.IgnoreFiltering = result.Read<bool>(9);

                        var bonusListIDsTok = new StringArray(result.Read<string>(7), ' ');
                        if (!bonusListIDsTok.IsEmpty())
                            foreach (uint token in bonusListIDsTok)
                                vItem.BonusListIDs.Add(token);

                        // check validity with event's npcflag
                        if (!Global.ObjectMgr.IsVendorItemValid(entry, vItem, null, null, event_npc_flag))
                            continue;

                        mGameEventVendors[event_id].Add(entry, vItem);

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} vendor additions in game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }

            Log.outInfo(LogFilter.ServerLoading, "Loading Game Event Pool Data...");
            {
                uint oldMSTime = Time.GetMSTime();

                //                                                               0                         1
                SQLResult result = DB.World.Query("SELECT pool_template.entry, game_event_pool.eventEntry FROM pool_template" +
                        " JOIN game_event_pool ON pool_template.entry = game_event_pool.pool_entry");
                if (result.IsEmpty())
                    Log.outInfo(LogFilter.ServerLoading, "Loaded 0 pools for game events. DB table `game_event_pool` is empty.");
                else
                {
                    uint count = 0;
                    do
                    {
                        uint entry = result.Read<uint>(0);
                        short event_id = result.Read<sbyte>(1);
                        int internal_event_id = mGameEvent.Length + event_id - 1;

                        if (internal_event_id < 0 || internal_event_id >= mGameEventPoolIds.Length)
                        {
                            Log.outError(LogFilter.Sql, "`game_event_pool` game event id ({0}) not exist in `game_event`", event_id);
                            continue;
                        }

                        if (!Global.PoolMgr.CheckPool(entry))
                        {
                            Log.outError(LogFilter.Sql, "Pool Id ({0}) has all creatures or gameobjects with explicit chance sum <>100 and no equal chance defined. The pool system cannot pick one to spawn.", entry);
                            continue;
                        }


                        mGameEventPoolIds[internal_event_id].Add(entry);

                        ++count;
                    }
                    while (result.NextRow());

                    Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pools for game events in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
                }
            }
        }

        public ulong GetNPCFlag(Creature cr)
        {
            ulong mask = 0;
            ulong guid = cr.GetSpawnId();

            foreach (var id in m_ActiveEvents)
            {
                foreach (var pair in mGameEventNPCFlags[id])
                    if (pair.guid == guid)
                        mask |= pair.npcflag;
            }

            return mask;
        }

        public void Initialize()
        {
            SQLResult result = DB.World.Query("SELECT MAX(eventEntry) FROM game_event");
            if (!result.IsEmpty())
            {

                int maxEventId = result.Read<byte>(0);

                // Id starts with 1 and array with 0, thus increment
                maxEventId++;

                mGameEvent = new GameEventData[maxEventId];
                mGameEventCreatureGuids = new List<ulong>[maxEventId * 2 - 1];
                mGameEventGameobjectGuids = new List<ulong>[maxEventId * 2 - 1];
                mGameEventPoolIds = new List<uint>[maxEventId * 2 - 1];
                for (var i = 0; i < maxEventId * 2 - 1; ++i)
                {
                    mGameEventCreatureGuids[i] = new List<ulong>();
                    mGameEventGameobjectGuids[i] = new List<ulong>();
                    mGameEventPoolIds[i] = new List<uint>();
                }

                mGameEventCreatureQuests = new List<Tuple<uint, uint>>[maxEventId];
                mGameEventGameObjectQuests = new List<Tuple<uint, uint>>[maxEventId];
                mGameEventVendors = new Dictionary<uint, VendorItem>[maxEventId];
                mGameEventNPCFlags = new List<(ulong guid, ulong npcflag)>[maxEventId];
                mGameEventModelEquip = new List<Tuple<ulong, ModelEquip>>[maxEventId];
                for (var i = 0; i < maxEventId; ++i)
                {
                    mGameEvent[i] = new GameEventData();
                    mGameEventCreatureQuests[i] = new List<Tuple<uint, uint>>();
                    mGameEventGameObjectQuests[i] = new List<Tuple<uint, uint>>();
                    mGameEventVendors[i] = new Dictionary<uint, VendorItem>();
                    mGameEventNPCFlags[i] = new List<(ulong guid, ulong npcflag)>();
                    mGameEventModelEquip[i] = new List<Tuple<ulong, ModelEquip>>();
                }
            }
        }

        public uint StartSystem()                           // return the next event delay in ms
        {
            m_ActiveEvents.Clear();
            uint delay = Update();
            isSystemInit = true;
            return delay;
        }

        public void StartArenaSeason()
        {
            int season = WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId);
            SQLResult result = DB.World.Query("SELECT eventEntry FROM game_event_arena_seasons WHERE season = '{0}'", season);
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Gameevent, "ArenaSeason ({0}) must be an existant Arena Season", season);
                return;
            }

            ushort eventId = result.Read<byte>(0);

            if (eventId >= mGameEvent.Length)
            {
                Log.outError(LogFilter.Gameevent, "EventEntry {0} for ArenaSeason ({1}) does not exists", eventId, season);
                return;
            }

            StartEvent(eventId, true);
            Log.outInfo(LogFilter.Gameevent, "Arena Season {0} started...", season);
        }

        public uint Update()                               // return the next event delay in ms
        {
            long currenttime = GameTime.GetGameTime();
            uint nextEventDelay = Time.Day;             // 1 day
            uint calcDelay;
            List<ushort> activate = new();
            List<ushort> deactivate = new();
            for (ushort id = 1; id < mGameEvent.Length; ++id)
            {
                // must do the activating first, and after that the deactivating
                // so first queue it
                if (CheckOneGameEvent(id))
                {
                    // if the world event is in NEXTPHASE state, and the time has passed to finish this event, then do so
                    if (mGameEvent[id].state == GameEventState.WorldNextPhase && mGameEvent[id].nextstart <= currenttime)
                    {
                        // set this event to finished, null the nextstart time
                        mGameEvent[id].state = GameEventState.WorldFinished;
                        mGameEvent[id].nextstart = 0;
                        // save the state of this gameevent
                        SaveWorldEventStateToDB(id);
                        // queue for deactivation
                        if (IsActiveEvent(id))
                            deactivate.Add(id);
                        // go to next event, this no longer needs an event update timer
                        continue;
                    }
                    else if (mGameEvent[id].state == GameEventState.WorldConditions && CheckOneGameEventConditions(id))
                        // changed, save to DB the gameevent state, will be updated in next update cycle
                        SaveWorldEventStateToDB(id);

                    Log.outDebug(LogFilter.Misc, "GameEvent {0} is active", id);
                    // queue for activation
                    if (!IsActiveEvent(id))
                        activate.Add(id);
                }
                else
                {
                    Log.outDebug(LogFilter.Misc, "GameEvent {0} is not active", id);
                    if (IsActiveEvent(id))
                        deactivate.Add(id);
                    else
                    {
                        if (!isSystemInit)
                        {
                            short event_nid = (short)(-1 * id);
                            // spawn all negative ones for this event
                            GameEventSpawn(event_nid);
                        }
                    }
                }
                calcDelay = NextCheck(id);
                if (calcDelay < nextEventDelay)
                    nextEventDelay = calcDelay;
            }
            // now activate the queue
            // a now activated event can contain a spawn of a to-be-deactivated one
            // following the activate - deactivate order, deactivating the first event later will leave the spawn in (wont disappear then reappear clientside)
            foreach (var eventId in activate)
            {
                // start the event
                // returns true the started event completed
                // in that case, initiate next update in 1 second
                if (StartEvent(eventId))
                    nextEventDelay = 0;
            }

            foreach (var eventId in deactivate)
                StopEvent(eventId);

            Log.outInfo(LogFilter.Gameevent, "Next game event check in {0} seconds.", nextEventDelay + 1);
            return (nextEventDelay + 1) * Time.InMilliseconds;           // Add 1 second to be sure event has started/stopped at next call
        }

        void UnApplyEvent(ushort event_id)
        {
            Log.outInfo(LogFilter.Gameevent, "GameEvent {0} \"{1}\" removed.", event_id, mGameEvent[event_id].description);
            //! Run SAI scripts with SMART_EVENT_GAME_EVENT_END
            RunSmartAIScripts(event_id, false);
            // un-spawn positive event tagged objects
            GameEventUnspawn((short)event_id);
            // spawn negative event tagget objects
            short event_nid = (short)(-1 * event_id);
            GameEventSpawn(event_nid);
            // restore equipment or model
            ChangeEquipOrModel((short)event_id, false);
            // Remove quests that are events only to non event npc
            UpdateEventQuests(event_id, false);
            UpdateWorldStates(event_id, false);
            // update npcflags in this event
            UpdateEventNPCFlags(event_id);
            // remove vendor items
            UpdateEventNPCVendor(event_id, false);
        }

        void ApplyNewEvent(ushort event_id)
        {
            byte announce = mGameEvent[event_id].announce;
            if (announce == 1)// || (announce == 2 && WorldConfigEventAnnounce))
                Global.WorldMgr.SendWorldText(CypherStrings.Eventmessage, mGameEvent[event_id].description);

            Log.outInfo(LogFilter.Gameevent, "GameEvent {0} \"{1}\" started.", event_id, mGameEvent[event_id].description);

            // spawn positive event tagget objects
            GameEventSpawn((short)event_id);
            // un-spawn negative event tagged objects
            short event_nid = (short)(-1 * event_id);
            GameEventUnspawn(event_nid);
            // Change equipement or model
            ChangeEquipOrModel((short)event_id, true);
            // Add quests that are events only to non event npc
            UpdateEventQuests(event_id, true);
            UpdateWorldStates(event_id, true);
            // update npcflags in this event
            UpdateEventNPCFlags(event_id);
            // add vendor items
            UpdateEventNPCVendor(event_id, true);

            //! Run SAI scripts with SMART_EVENT_GAME_EVENT_START
            RunSmartAIScripts(event_id, true);

            // check for seasonal quest reset.
            Global.WorldMgr.ResetEventSeasonalQuests(event_id, GetLastStartTime(event_id));
        }

        void UpdateEventNPCFlags(ushort event_id)
        {
            MultiMap<uint, ulong> creaturesByMap = new();

            // go through the creatures whose npcflags are changed in the event
            foreach (var (guid, npcflag) in mGameEventNPCFlags[event_id])
            {
                // get the creature data from the low guid to get the entry, to be able to find out the whole guid
                CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                if (data != null)
                    creaturesByMap.Add(data.MapId, guid);
            }

            foreach (var key in creaturesByMap.Keys)
            {
                Global.MapMgr.DoForAllMapsWithMapId(key, (Map map) =>
                {
                    foreach (var spawnId in creaturesByMap[key])
                    {
                        var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(spawnId);
                        foreach (var creature in creatureBounds)
                        {
                            ulong npcflag = GetNPCFlag(creature);
                            CreatureTemplate creatureTemplate = creature.GetCreatureTemplate();
                            if (creatureTemplate != null)
                                npcflag |= (ulong)creatureTemplate.Npcflag;

                            creature.ReplaceAllNpcFlags((NPCFlags)(npcflag & 0xFFFFFFFF));
                            creature.ReplaceAllNpcFlags2((NPCFlags2)(npcflag >> 32));
                            // reset gossip options, since the flag change might have added / removed some
                            //cr.ResetGossipOptions();
                        }
                    }
                });
            }
        }

        void UpdateEventNPCVendor(ushort eventId, bool activate)
        {
            foreach (var npcEventVendor in mGameEventVendors[eventId])
            {
                if (activate)
                    Global.ObjectMgr.AddVendorItem(npcEventVendor.Key, npcEventVendor.Value, false);
                else
                    Global.ObjectMgr.RemoveVendorItem(npcEventVendor.Key, npcEventVendor.Value.item, npcEventVendor.Value.Type, false);
            }
        }

        void GameEventSpawn(short event_id)
        {
            int internal_event_id = mGameEvent.Length + event_id - 1;

            if (internal_event_id < 0 || internal_event_id >= mGameEventCreatureGuids.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventSpawn attempt access to out of range mGameEventCreatureGuids element {0} (size: {1})",
                    internal_event_id, mGameEventCreatureGuids.Length);
                return;
            }

            foreach (var guid in mGameEventCreatureGuids[internal_event_id])
            {
                // Add to correct cell
                CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                if (data != null)
                {
                    Global.ObjectMgr.AddCreatureToGrid(data);

                    // Spawn if necessary (loaded grids only)
                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
                    {
                        map.RemoveRespawnTime(SpawnObjectType.Creature, guid);
                        // We use spawn coords to spawn
                        if (map.IsGridLoaded(data.SpawnPoint))
                            Creature.CreateCreatureFromDB(guid, map);
                    });
                }
            }

            if (internal_event_id < 0 || internal_event_id >= mGameEventGameobjectGuids.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventSpawn attempt access to out of range mGameEventGameobjectGuids element {0} (size: {1})",
                    internal_event_id, mGameEventGameobjectGuids.Length);
                return;
            }

            foreach (var guid in mGameEventGameobjectGuids[internal_event_id])
            {
                // Add to correct cell
                GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);
                if (data != null)
                {
                    Global.ObjectMgr.AddGameObjectToGrid(data);
                    // Spawn if necessary (loaded grids only)
                    // this base map checked as non-instanced and then only existed
                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
                    {
                        map.RemoveRespawnTime(SpawnObjectType.GameObject, guid);
                        // We use current coords to unspawn, not spawn coords since creature can have changed grid
                        if (map.IsGridLoaded(data.SpawnPoint))
                        {
                            GameObject go = GameObject.CreateGameObjectFromDB(guid, map, false);
                            // @todo find out when it is add to map
                            if (go != null)
                            {
                                // @todo find out when it is add to map
                                if (go.IsSpawnedByDefault())
                                {
                                    if (!map.AddToMap(go))
                                        go.Dispose();
                                }
                            }
                        }
                    });
                }
            }

            if (internal_event_id < 0 || internal_event_id >= mGameEventPoolIds.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventSpawn attempt access to out of range mGameEventPoolIds element {0} (size: {1})",
                    internal_event_id, mGameEventPoolIds.Length);
                return;
            }

            foreach (var id in mGameEventPoolIds[internal_event_id])
            {
                PoolTemplateData poolTemplate = Global.PoolMgr.GetPoolTemplate(id);
                if (poolTemplate != null)
                {
                    Global.MapMgr.DoForAllMapsWithMapId((uint)poolTemplate.MapId, map =>
                    {
                        Global.PoolMgr.SpawnPool(map.GetPoolData(), id);
                    });
                }
            }
        }

        void GameEventUnspawn(short event_id)
        {
            int internal_event_id = mGameEvent.Length + event_id - 1;

            if (internal_event_id < 0 || internal_event_id >= mGameEventCreatureGuids.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventUnspawn attempt access to out of range mGameEventCreatureGuids element {0} (size: {1})",
                    internal_event_id, mGameEventCreatureGuids.Length);
                return;
            }

            foreach (var guid in mGameEventCreatureGuids[internal_event_id])
            {
                // check if it's needed by another event, if so, don't remove
                if (event_id > 0 && HasCreatureActiveEventExcept(guid, (ushort)event_id))
                    continue;

                // Remove the creature from grid
                CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
                if (data != null)
                {
                    Global.ObjectMgr.RemoveCreatureFromGrid(data);

                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
                    {
                        map.RemoveRespawnTime(SpawnObjectType.Creature, guid);
                        var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(guid);
                        foreach (var creature in creatureBounds)
                            creature.AddObjectToRemoveList();
                    });
                }
            }

            if (internal_event_id < 0 || internal_event_id >= mGameEventGameobjectGuids.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventUnspawn attempt access to out of range mGameEventGameobjectGuids element {0} (size: {1})",
                    internal_event_id, mGameEventGameobjectGuids.Length);
                return;
            }

            foreach (var guid in mGameEventGameobjectGuids[internal_event_id])
            {
                // check if it's needed by another event, if so, don't remove
                if (event_id > 0 && HasGameObjectActiveEventExcept(guid, (ushort)event_id))
                    continue;
                // Remove the gameobject from grid
                GameObjectData data = Global.ObjectMgr.GetGameObjectData(guid);
                if (data != null)
                {
                    Global.ObjectMgr.RemoveGameObjectFromGrid(data);

                    Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
                    {
                        map.RemoveRespawnTime(SpawnObjectType.GameObject, guid);
                        var gameobjectBounds = map.GetGameObjectBySpawnIdStore().LookupByKey(guid);
                        foreach (var go in gameobjectBounds)
                            go.AddObjectToRemoveList();

                    });
                }
            }

            if (internal_event_id < 0 || internal_event_id >= mGameEventPoolIds.Length)
            {
                Log.outError(LogFilter.Gameevent, "GameEventMgr.GameEventUnspawn attempt access to out of range mGameEventPoolIds element {0} (size: {1})", internal_event_id, mGameEventPoolIds.Length);
                return;
            }

            foreach (var poolId in mGameEventPoolIds[internal_event_id])
            {
                PoolTemplateData poolTemplate = Global.PoolMgr.GetPoolTemplate(poolId);
                if (poolTemplate != null)
                {
                    Global.MapMgr.DoForAllMapsWithMapId((uint)poolTemplate.MapId, map =>
                    {
                        Global.PoolMgr.DespawnPool(map.GetPoolData(), poolId, true);
                    });
                }
            }
        }

        void ChangeEquipOrModel(short event_id, bool activate)
        {
            foreach (var (spawnId, modelEquip) in mGameEventModelEquip[event_id])
            {
                // Remove the creature from grid
                CreatureData data = Global.ObjectMgr.GetCreatureData(spawnId);
                if (data == null)
                    continue;

                // Update if spawned
                Global.MapMgr.DoForAllMapsWithMapId(data.MapId, map =>
                {
                    var creatureBounds = map.GetCreatureBySpawnIdStore().LookupByKey(spawnId);
                    foreach (var creature in creatureBounds)
                    {
                        if (activate)
                        {
                            modelEquip.equipement_id_prev = creature.GetCurrentEquipmentId();
                            modelEquip.modelid_prev = creature.GetDisplayId();
                            creature.LoadEquipment(modelEquip.equipment_id, true);
                            if (modelEquip.modelid > 0 && modelEquip.modelid_prev != modelEquip.modelid && Global.ObjectMgr.GetCreatureModelInfo(modelEquip.modelid) != null)
                                creature.SetDisplayId(modelEquip.modelid, true);
                        }
                        else
                        {
                            creature.LoadEquipment(modelEquip.equipement_id_prev, true);
                            if (modelEquip.modelid_prev > 0 && modelEquip.modelid_prev != modelEquip.modelid && Global.ObjectMgr.GetCreatureModelInfo(modelEquip.modelid_prev) != null)
                                creature.SetDisplayId(modelEquip.modelid_prev, true);
                        }
                    }
                });

                // now last step: put in data
                CreatureData data2 = Global.ObjectMgr.NewOrExistCreatureData(spawnId);
                if (activate)
                {
                    modelEquip.modelid_prev = data2.display != null ? data2.display.CreatureDisplayID : 0;
                    modelEquip.equipement_id_prev = (byte)data2.equipmentId;
                    if (modelEquip.modelid != 0)
                        data2.display = new(modelEquip.modelid, SharedConst.DefaultPlayerDisplayScale, 1.0f);
                    else
                        data2.display = null;
                    data2.equipmentId = (sbyte)modelEquip.equipment_id;
                }
                else
                {
                    if (modelEquip.modelid_prev != 0)
                        data2.display = new(modelEquip.modelid_prev, SharedConst.DefaultPlayerDisplayScale, 1.0f);
                    else
                        data2.display = null;
                    data2.equipmentId = (sbyte)modelEquip.equipement_id_prev;
                }
            }
        }

        bool HasCreatureQuestActiveEventExcept(uint questId, ushort eventId)
        {
            foreach (var activeEventId in m_ActiveEvents)
            {
                if (activeEventId != eventId)
                    foreach (var pair in mGameEventCreatureQuests[activeEventId])
                        if (pair.Item2 == questId)
                            return true;
            }
            return false;
        }

        bool HasGameObjectQuestActiveEventExcept(uint questId, ushort eventId)
        {
            foreach (var activeEventId in m_ActiveEvents)
            {
                if (activeEventId != eventId)
                    foreach (var pair in mGameEventGameObjectQuests[activeEventId])
                        if (pair.Item2 == questId)
                            return true;
            }
            return false;
        }
        bool HasCreatureActiveEventExcept(ulong creatureId, ushort eventId)
        {
            foreach (var activeEventId in m_ActiveEvents)
            {
                if (activeEventId != eventId)
                {
                    int internal_event_id = mGameEvent.Length + activeEventId - 1;
                    foreach (var id in mGameEventCreatureGuids[internal_event_id])
                        if (id == creatureId)
                            return true;
                }
            }
            return false;
        }
        bool HasGameObjectActiveEventExcept(ulong goId, ushort eventId)
        {
            foreach (var activeEventId in m_ActiveEvents)
            {
                if (activeEventId != eventId)
                {
                    int internal_event_id = mGameEvent.Length + activeEventId - 1;
                    foreach (var id in mGameEventGameobjectGuids[internal_event_id])
                        if (id == goId)
                            return true;
                }
            }
            return false;
        }

        void UpdateEventQuests(ushort eventId, bool activate)
        {
            foreach (var pair in mGameEventCreatureQuests[eventId])
            {
                var CreatureQuestMap = Global.ObjectMgr.GetCreatureQuestRelationMapHACK();
                if (activate)                                           // Add the pair(id, quest) to the multimap
                    CreatureQuestMap.Add(pair.Item1, pair.Item2);
                else
                {
                    if (!HasCreatureQuestActiveEventExcept(pair.Item2, eventId))
                    {
                        // Remove the pair(id, quest) from the multimap
                        CreatureQuestMap.Remove(pair.Item1, pair.Item2);
                    }
                }
            }
            foreach (var pair in mGameEventGameObjectQuests[eventId])
            {
                var GameObjectQuestMap = Global.ObjectMgr.GetGOQuestRelationMapHACK();
                if (activate)                                           // Add the pair(id, quest) to the multimap
                    GameObjectQuestMap.Add(pair.Item1, pair.Item2);
                else
                {
                    if (!HasGameObjectQuestActiveEventExcept(pair.Item2, eventId))
                    {
                        // Remove the pair(id, quest) from the multimap
                        GameObjectQuestMap.Remove(pair.Item1, pair.Item2);
                    }
                }
            }
        }

        void UpdateWorldStates(ushort event_id, bool Activate)
        {
            GameEventData Event = mGameEvent[event_id];
            if (Event.holiday_id != HolidayIds.None)
            {
                BattlegroundTypeId bgTypeId = Global.BattlegroundMgr.WeekendHolidayIdToBGType(Event.holiday_id);
                if (bgTypeId != BattlegroundTypeId.None)
                {
                    var bl = CliDB.BattlemasterListStorage.LookupByKey(Global.BattlegroundMgr.WeekendHolidayIdToBGType(Event.holiday_id));
                    if (bl != null)
                        if (bl.HolidayWorldState != 0)
                            Global.WorldStateMgr.SetValue(bl.HolidayWorldState, Activate ? 1 : 0, false, null);
                }
            }
        }

        public void HandleQuestComplete(uint quest_id)
        {
            // translate the quest to event and condition
            var questToEvent = mQuestToEventConditions.LookupByKey(quest_id);
            // quest is registered
            if (questToEvent != null)
            {
                ushort event_id = questToEvent.event_id;
                uint condition = questToEvent.condition;
                float num = questToEvent.num;

                // the event is not active, so return, don't increase condition finishes
                if (!IsActiveEvent(event_id))
                    return;
                // not in correct phase, return
                if (mGameEvent[event_id].state != GameEventState.WorldConditions)
                    return;
                var eventFinishCond = mGameEvent[event_id].conditions.LookupByKey(condition);
                // condition is registered
                if (eventFinishCond != null)
                {
                    // increase the done count, only if less then the req
                    if (eventFinishCond.done < eventFinishCond.reqNum)
                    {
                        eventFinishCond.done += num;
                        // check max limit
                        if (eventFinishCond.done > eventFinishCond.reqNum)
                            eventFinishCond.done = eventFinishCond.reqNum;
                        // save the change to db
                        SQLTransaction trans = new();

                        PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_CONDITION_SAVE);
                        stmt.AddValue(0, event_id);
                        stmt.AddValue(1, condition);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_GAME_EVENT_CONDITION_SAVE);
                        stmt.AddValue(0, event_id);
                        stmt.AddValue(1, condition);
                        stmt.AddValue(2, eventFinishCond.done);
                        trans.Append(stmt);
                        DB.Characters.CommitTransaction(trans);
                        // check if all conditions are met, if so, update the event state
                        if (CheckOneGameEventConditions(event_id))
                        {
                            // changed, save to DB the gameevent state
                            SaveWorldEventStateToDB(event_id);
                            // force update events to set timer
                            Global.WorldMgr.ForceGameEventUpdate();
                        }
                    }
                }
            }
        }

        bool CheckOneGameEventConditions(ushort event_id)
        {
            foreach (var pair in mGameEvent[event_id].conditions)
                if (pair.Value.done < pair.Value.reqNum)
                    // return false if a condition doesn't match
                    return false;
            // set the phase
            mGameEvent[event_id].state = GameEventState.WorldNextPhase;
            // set the followup events' start time
            if (mGameEvent[event_id].nextstart == 0)
            {
                long currenttime = GameTime.GetGameTime();
                mGameEvent[event_id].nextstart = currenttime + mGameEvent[event_id].length * 60;
            }
            return true;
        }

        void SaveWorldEventStateToDB(ushort event_id)
        {
            SQLTransaction trans = new();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GAME_EVENT_SAVE);
            stmt.AddValue(0, event_id);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_GAME_EVENT_SAVE);
            stmt.AddValue(0, event_id);
            stmt.AddValue(1, (byte)mGameEvent[event_id].state);
            stmt.AddValue(2, mGameEvent[event_id].nextstart != 0 ? mGameEvent[event_id].nextstart : 0L);
            trans.Append(stmt);
            DB.Characters.CommitTransaction(trans);
        }

        void SendWorldStateUpdate(Player player, ushort event_id)
        {
            foreach (var pair in mGameEvent[event_id].conditions)
            {
                if (pair.Value.done_world_state != 0)
                    player.SendUpdateWorldState(pair.Value.done_world_state, (uint)(pair.Value.done));
                if (pair.Value.max_world_state != 0)
                    player.SendUpdateWorldState(pair.Value.max_world_state, (uint)(pair.Value.reqNum));
            }
        }

        void RunSmartAIScripts(ushort event_id, bool activate)
        {
            //! Iterate over every supported source type (creature and gameobject)
            //! Not entirely sure how this will affect units in non-loaded grids.
            Global.MapMgr.DoForAllMaps(map =>
            {
                GameEventAIHookWorker worker = new(event_id, activate);
                var visitor = new Visitor(worker, GridMapTypeMask.None);
                visitor.Visit(map.GetObjectsStore().Values.ToList());
            });
        }

        void SetHolidayEventTime(GameEventData gameEvent)
        {
            if (gameEvent.holidayStage == 0) // Ignore holiday
                return;

            var holiday = CliDB.HolidaysStorage.LookupByKey(gameEvent.holiday_id);
            if (holiday.Date[0] == 0 || holiday.Duration[0] == 0) // Invalid definitions
            {
                Log.outError(LogFilter.Sql, $"Missing date or duration for holiday {gameEvent.holiday_id}.");
                return;
            }

            byte stageIndex = (byte)(gameEvent.holidayStage - 1);
            gameEvent.length = (uint)(holiday.Duration[stageIndex] * Time.Hour / Time.Minute);

            TimeSpan stageOffset = TimeSpan.Zero;
            for (int i = 0; i < stageIndex; ++i)
                stageOffset += TimeSpan.FromHours(holiday.Duration[i]);

            switch (holiday.CalendarFilterType)
            {
                case -1: // Yearly
                    gameEvent.occurence = Time.Year / Time.Minute; // Not all too useful
                    break;
                case 0: // Weekly
                    gameEvent.occurence = Time.Week / Time.Minute;
                    break;
                case 1: // Defined dates only (Darkmoon Faire)
                    break;
                case 2: // Only used for looping events (Call to Arms)
                    break;
            }

            if (holiday.Looping != 0)
            {
                gameEvent.occurence = 0;
                for (int i = 0; i < SharedConst.MaxHolidayDurations && holiday.Duration[i] != 0; ++i)
                    gameEvent.occurence += (uint)(holiday.Duration[i] * Time.Hour / Time.Minute);
            }

            WowTime curTime = GameTime.GetWowTime();
            for (int i = 0; i < SharedConst.MaxHolidayDates && holiday.Date[i] != 0; ++i)
            {
                WowTime date = new();
                date.SetPackedTime(holiday.Date[i]);
                bool singleDate = date.GetYear() == -1;
                if (singleDate)
                    date.SetYear(GameTime.GetWowTime().GetYear() - 1); // First try last year (event active through New Year)

                if (curTime < date + TimeSpan.FromMinutes(gameEvent.length))
                {
                    gameEvent.start = (date + stageOffset).GetUnixTimeFromUtcTime();
                    break;
                }
                else if (singleDate)
                {
                    date.SetYear(date.GetYear() + 1); // This year
                    gameEvent.start = (date + stageOffset).GetUnixTimeFromUtcTime();
                    break;
                }
                else
                {
                    // date is due and not a singleDate event, try with next DBC date (modified by holiday_dates)
                    // if none is found we don't modify start date and use the one in game_event
                }
            }
        }

        long GetLastStartTime(ushort event_id)
        {
            if (event_id >= mGameEvent.Length)
                return 0;

            if (mGameEvent[event_id].state != GameEventState.Normal)
                return 0;

            DateTime now = GameTime.GetSystemTime();
            DateTime eventInitialStart = Time.UnixTimeToDateTime(mGameEvent[event_id].start);
            TimeSpan occurence = TimeSpan.FromMinutes(mGameEvent[event_id].occurence);
            TimeSpan durationSinceLastStart = TimeSpan.FromTicks((now - eventInitialStart).Ticks % occurence.Ticks);
            return Time.DateTimeToUnixTime(now - durationSinceLastStart);
        }

        public bool IsHolidayActive(HolidayIds id)
        {
            if (id == HolidayIds.None)
                return false;

            var events = GetEventMap();
            var activeEvents = GetActiveEventList();

            foreach (var eventId in activeEvents)
                if (events[eventId].holiday_id == id)
                    return true;

            return false;
        }

        public bool IsEventActive(ushort eventId)
        {
            var ae = GetActiveEventList();
            return ae.Contains(eventId);
        }

        public List<ushort> GetActiveEventList() { return m_ActiveEvents; }
        public GameEventData[] GetEventMap() { return mGameEvent; }
        public bool IsActiveEvent(ushort event_id) { return m_ActiveEvents.Contains(event_id); }

        void AddActiveEvent(ushort event_id) { m_ActiveEvents.Add(event_id); }
        void RemoveActiveEvent(ushort event_id) { m_ActiveEvents.Remove(event_id); }

        List<Tuple<uint, uint>>[] mGameEventCreatureQuests;
        List<Tuple<uint, uint>>[] mGameEventGameObjectQuests;
        Dictionary<uint, VendorItem>[] mGameEventVendors;
        List<Tuple<ulong, ModelEquip>>[] mGameEventModelEquip;
        List<uint>[] mGameEventPoolIds;
        GameEventData[] mGameEvent;
        Dictionary<uint, GameEventQuestToEventConditionNum> mQuestToEventConditions = new();
        List<(ulong guid, ulong npcflag)>[] mGameEventNPCFlags;
        List<ushort> m_ActiveEvents = new();
        bool isSystemInit;

        public List<ulong>[] mGameEventCreatureGuids;
        public List<ulong>[] mGameEventGameobjectGuids;
    }

    public class GameEventFinishCondition
    {
        public float reqNum;  // required number // use float, since some events use percent
        public float done;    // done number
        public uint max_world_state;  // max resource count world state update id
        public uint done_world_state; // done resource count world state update id
    }

    public class GameEventQuestToEventConditionNum
    {
        public ushort event_id;
        public uint condition;
        public float num;
    }

    public class GameEventData
    {
        public GameEventData()
        {
            start = 1;
        }

        public long start;           // occurs after this time
        public long end;             // occurs before this time
        public long nextstart;       // after this time the follow-up events count this phase completed
        public uint occurence;       // time between end and start
        public uint length;          // length of the event (Time.Minutes) after finishing all conditions
        public HolidayIds holiday_id;
        public byte holidayStage;
        public GameEventState state;   // state of the game event, these are saved into the game_event table on change!
        public Dictionary<uint, GameEventFinishCondition> conditions = new();  // conditions to finish
        public List<ushort> prerequisite_events = new();  // events that must be completed before starting this event
        public string description;
        public byte announce;         // if 0 dont announce, if 1 announce, if 2 take config value

        public bool IsValid() { return length > 0 || state > GameEventState.Normal; }
    }

    public class ModelEquip
    {
        public uint modelid;
        public uint modelid_prev;
        public byte equipment_id;
        public byte equipement_id_prev;
    }

    class GameEventAIHookWorker : Notifier
    {
        public GameEventAIHookWorker(ushort eventId, bool activate)
        {
            _eventId = eventId;
            _activate = activate;
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];
                if (creature.IsInWorld && creature.IsAIEnabled())
                    creature.GetAI().OnGameEvent(_activate, _eventId);
            }
        }
        public override void Visit(IList<GameObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                GameObject gameObject = objs[i];
                if (gameObject.IsInWorld)
                    gameObject.GetAI().OnGameEvent(_activate, _eventId);
            }
        }

        ushort _eventId;
        bool _activate;
    }

    public enum GameEventState
    {
        Normal = 0, // standard game events
        WorldInactive = 1, // not yet started
        WorldConditions = 2, // condition matching phase
        WorldNextPhase = 3, // conditions are met, now 'length' timer to start next event
        WorldFinished = 4, // next events are started, unapply this one
        Internal = 5  // never handled in update
    }
}
