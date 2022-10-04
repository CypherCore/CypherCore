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

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Maps
{
    //#define INSTANCE_ID_HIGH_MASK   0x1F440000
    //#define INSTANCE_ID_LFG_MASK    0x00000001
    //#define INSTANCE_ID_NORMAL_MASK 0x00010000

    using InstanceLockKey = Tuple<uint, uint>;

    //using PlayerLockMap = std::unordered_map<InstanceLockKey, std::unique_ptr<InstanceLock>>;
    //using LockMap = std::unordered_map<ObjectGuid, PlayerLockMap>;

    public class InstanceLockManager : Singleton<InstanceLockManager>
    {
        object _lockObject = new();
        Dictionary<ObjectGuid, Dictionary<InstanceLockKey, InstanceLock>> _temporaryInstanceLocksByPlayer = new(); // locks stored here before any boss gets killed
        Dictionary<ObjectGuid, Dictionary<InstanceLockKey, InstanceLock>> _instanceLocksByPlayer = new();
        Dictionary<uint, SharedInstanceLockData> _instanceLockDataById = new();
        bool _unloading;

        InstanceLockManager() { }

        public void Load()
        {
            Dictionary<uint, SharedInstanceLockData> instanceLockDataById = new();

            //                                              0           1     2
            SQLResult result = DB.Characters.Query("SELECT instanceId, data, completedEncountersMask FROM instance");
            if (!result.IsEmpty())
            {
                do
                {
                    uint instanceId = result.Read<uint>(0);

                    SharedInstanceLockData data = new();
                    data.Data = result.Read<string>(1);
                    data.CompletedEncountersMask = result.Read<uint>(2);
                    data.InstanceId = instanceId;

                    instanceLockDataById[instanceId] = data;

                } while (result.NextRow());
            }

            //                                                  0     1      2       3           4           5     6                        7           8
            SQLResult lockResult = DB.Characters.Query("SELECT guid, mapId, lockId, instanceId, difficulty, data, completedEncountersMask, expiryTime, extended FROM character_instance_lock");
            if (!result.IsEmpty())
            {
                do
                {
                    ObjectGuid playerGuid = ObjectGuid.Create(HighGuid.Player, lockResult.Read<ulong>(0));
                    uint mapId = lockResult.Read<uint>(1);
                    uint lockId = lockResult.Read<uint>(2);
                    uint instanceId = lockResult.Read<uint>(3);
                    Difficulty difficulty = (Difficulty)lockResult.Read<byte>(4);
                    DateTime expiryTime = Time.UnixTimeToDateTime(lockResult.Read<long>(7));

                    // Mark instance id as being used
                    Global.MapMgr.RegisterInstanceId(instanceId);

                    InstanceLock instanceLock;
                    if (new MapDb2Entries(mapId, difficulty).IsInstanceIdBound())
                    {
                        var sharedData = instanceLockDataById.LookupByKey(instanceId);
                        if (sharedData == null)
                        {
                            Log.outError(LogFilter.Instance, $"Missing instance data for instance id based lock (id {instanceId})");
                            DB.Characters.Query($"DELETE FROM character_instance_lock WHERE instanceId = {instanceId}");
                            continue;
                        }

                        instanceLock = new SharedInstanceLock(mapId, difficulty, expiryTime, instanceId, sharedData);
                        _instanceLockDataById[instanceId] = sharedData;
                    }
                    else
                        instanceLock = new InstanceLock(mapId, difficulty, expiryTime, instanceId);

                    instanceLock.GetData().Data = lockResult.Read<string>(5);
                    instanceLock.GetData().CompletedEncountersMask = lockResult.Read<uint>(6);
                    instanceLock.SetExtended(lockResult.Read<bool>(8));

                    _instanceLocksByPlayer[playerGuid][Tuple.Create(mapId, lockId)] = instanceLock;

                } while (result.NextRow());
            }
        }

        public void Unload()
        {
            _unloading = true;
            _instanceLocksByPlayer.Clear();
            _instanceLockDataById.Clear();
        }

        public TransferAbortReason CanJoinInstanceLock(ObjectGuid playerGuid, MapDb2Entries entries, InstanceLock instanceLock)
        {
            if (!entries.MapDifficulty.HasResetSchedule())
                return TransferAbortReason.None;

            InstanceLock playerInstanceLock = FindActiveInstanceLock(playerGuid, entries);
            if (playerInstanceLock == null)
                return TransferAbortReason.None;

            if (entries.Map.IsFlexLocking())
            {
                // compare completed encounters - if instance has any encounters unkilled in players lock then cannot enter
                if ((playerInstanceLock.GetData().CompletedEncountersMask & ~instanceLock.GetData().CompletedEncountersMask) != 0)
                    return TransferAbortReason.AlreadyCompletedEncounter;

                return TransferAbortReason.None;
            }

            if (!entries.MapDifficulty.IsUsingEncounterLocks() && playerInstanceLock.GetInstanceId() != 0 && playerInstanceLock.GetInstanceId() != instanceLock.GetInstanceId())
                return TransferAbortReason.LockedToDifferentInstance;

            return TransferAbortReason.None;
        }

        public InstanceLock FindInstanceLock(Dictionary<ObjectGuid, Dictionary<InstanceLockKey, InstanceLock>> locks, ObjectGuid playerGuid, MapDb2Entries entries)
        {
            var playerLocks = locks.LookupByKey(playerGuid);
            if (playerLocks == null)
                return null;

            return playerLocks.LookupByKey(entries.GetKey());
        }

        public InstanceLock FindActiveInstanceLock(ObjectGuid playerGuid, MapDb2Entries entries)
        {
            lock(_lockObject)
                return FindActiveInstanceLock(playerGuid, entries, false, true);
        }

        public InstanceLock FindActiveInstanceLock(ObjectGuid playerGuid, MapDb2Entries entries, bool ignoreTemporary, bool ignoreExpired)
        {
            InstanceLock instanceLock = FindInstanceLock(_instanceLocksByPlayer, playerGuid, entries);

            // Ignore expired and not extended locks
            if (instanceLock != null && (!instanceLock.IsExpired() || instanceLock.IsExtended() || !ignoreExpired))
                return instanceLock;

            if (ignoreTemporary)
                return null;

            return FindInstanceLock(_temporaryInstanceLocksByPlayer, playerGuid, entries);
        }

        public ICollection<InstanceLock> GetInstanceLocksForPlayer(ObjectGuid playerGuid)
        {
            return _instanceLocksByPlayer.LookupByKey(playerGuid)?.Values;
        }

        public InstanceLock CreateInstanceLockForNewInstance(ObjectGuid playerGuid, MapDb2Entries entries, uint instanceId)
        {
            if (!entries.MapDifficulty.HasResetSchedule())
                return null;

            InstanceLock instanceLock;
            if (entries.IsInstanceIdBound())
            {
                SharedInstanceLockData sharedData = new();
                _instanceLockDataById[instanceId] = sharedData;
                instanceLock = new SharedInstanceLock(entries.MapDifficulty.MapID, (Difficulty)entries.MapDifficulty.DifficultyID,
                    GetNextResetTime(entries), 0, sharedData);
            }
            else
                instanceLock = new InstanceLock(entries.MapDifficulty.MapID, (Difficulty)entries.MapDifficulty.DifficultyID,
                    GetNextResetTime(entries), 0);

            _temporaryInstanceLocksByPlayer[playerGuid][entries.GetKey()] = instanceLock;
            Log.outDebug(LogFilter.Instance, $"[{entries.Map.Id}-{entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()]} | " +
                $"{entries.MapDifficulty.DifficultyID}-{CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Created new temporary instance lock for {playerGuid} in instance {instanceId}");
            return instanceLock;
        }

        public InstanceLock UpdateInstanceLockForPlayer(SQLTransaction trans, ObjectGuid playerGuid, MapDb2Entries entries, InstanceLockUpdateEvent updateEvent)
        {
            InstanceLock instanceLock = FindActiveInstanceLock(playerGuid, entries, true, true);
            if (instanceLock == null)
            {
                lock (_lockObject)
                {
                    // Move lock from temporary storage if it exists there
                    // This is to avoid destroying expired locks before any boss is killed in a fresh lock
                    // player can still change his mind, exit instance and reactivate old lock
                    var playerLocks = _temporaryInstanceLocksByPlayer.LookupByKey(playerGuid);
                    if (playerLocks != null)
                    {
                        var playerInstanceLock = playerLocks.LookupByKey(entries.GetKey());
                        if (playerInstanceLock != null)
                        {
                            instanceLock = playerInstanceLock;
                            _instanceLocksByPlayer[playerGuid][entries.GetKey()] = instanceLock;

                            playerLocks.Remove(entries.GetKey());
                            if (playerLocks.Empty())
                                _temporaryInstanceLocksByPlayer.Remove(playerGuid);

                            Log.outDebug(LogFilter.Instance, $"[{entries.Map.Id}-{entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()]} | " +
                                $"{entries.MapDifficulty.DifficultyID}-{CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Promoting temporary lock to permanent for {playerGuid} in instance {updateEvent.InstanceId}");
                        }
                    }
                }
            }

            if (instanceLock == null)
            {
                if (entries.IsInstanceIdBound())
                {
                    var sharedDataItr = _instanceLockDataById.LookupByKey(updateEvent.InstanceId);
                    Cypher.Assert(sharedDataItr != null);

                    instanceLock = new SharedInstanceLock(entries.MapDifficulty.MapID, (Difficulty)entries.MapDifficulty.DifficultyID,
                        GetNextResetTime(entries), updateEvent.InstanceId, sharedDataItr);
                    Cypher.Assert((instanceLock as SharedInstanceLock).GetSharedData().InstanceId == updateEvent.InstanceId);
                }
                else
                    instanceLock = new InstanceLock(entries.MapDifficulty.MapID, (Difficulty)entries.MapDifficulty.DifficultyID,
                        GetNextResetTime(entries), updateEvent.InstanceId);

                lock(_lockObject)
                    _instanceLocksByPlayer[playerGuid][entries.GetKey()] = instanceLock;

                Log.outDebug(LogFilter.Instance, $"[{entries.Map.Id}-{entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()]} | " +
                    $"{entries.MapDifficulty.DifficultyID}-{CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Created new instance lock for {playerGuid} in instance {updateEvent.InstanceId}");
            }
            else
            {
                if (entries.IsInstanceIdBound())
                {
                    Cypher.Assert(instanceLock.GetInstanceId() == 0 || instanceLock.GetInstanceId() == updateEvent.InstanceId);
                    var sharedDataItr = _instanceLockDataById.LookupByKey(updateEvent.InstanceId);
                    Cypher.Assert(sharedDataItr != null);
                    Cypher.Assert(sharedDataItr == (instanceLock as SharedInstanceLock).GetSharedData());
                }

                instanceLock.SetInstanceId(updateEvent.InstanceId);
            }

            instanceLock.GetData().Data = updateEvent.NewData;
            if (updateEvent.CompletedEncounter != null)
            {
                instanceLock.GetData().CompletedEncountersMask |= 1u << updateEvent.CompletedEncounter.Bit;
                Log.outDebug(LogFilter.Instance, $"[{entries.Map.Id}-{entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()]} | " +
                    $"{entries.MapDifficulty.DifficultyID}-{CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] " +
                    $"Instance lock for {playerGuid} in instance {updateEvent.InstanceId} gains completed encounter [{updateEvent.CompletedEncounter.Id}-{updateEvent.CompletedEncounter.Name[Global.WorldMgr.GetDefaultDbcLocale()]}]");
            }

            // Synchronize map completed encounters into players completed encounters for UI
            if (!entries.MapDifficulty.IsUsingEncounterLocks())
                instanceLock.GetData().CompletedEncountersMask |= updateEvent.InstanceCompletedEncountersMask;

            if (updateEvent.EntranceWorldSafeLocId.HasValue)
                instanceLock.GetData().EntranceWorldSafeLocId = updateEvent.EntranceWorldSafeLocId.Value;

            if (instanceLock.IsExpired())
            {
                Cypher.Assert(instanceLock.IsExtended(), "Instance lock must have been extended to create instance map from it");
                instanceLock.SetExpiryTime(GetNextResetTime(entries));
                instanceLock.SetExtended(false);
                Log.outDebug(LogFilter.Instance, $"[{entries.Map.Id}-{entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()]} | " +
                    $"{entries.MapDifficulty.DifficultyID}-{CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Expired instance lock for {playerGuid} in instance {updateEvent.InstanceId} is now active");
            }

            // TODO: DB SAVE IN TRANSACTION
            trans.Append($"DELETE FROM character_instance_lock WHERE guid={playerGuid.GetCounter()} AND mapId={entries.MapDifficulty.MapID} AND lockId={entries.MapDifficulty.LockID}");
            string escapedData = instanceLock.GetData().Data;
            CharacterDatabase.EscapeString(ref escapedData);
            trans.Append($"INSERT INTO character_instance_lock (guid, mapId, lockId, instanceId, difficulty, data, completedEncountersMask, entranceWorldSafeLocId, expiryTime, extended) " +
                $"VALUES ({playerGuid.GetCounter()}, {entries.MapDifficulty.MapID}, {entries.MapDifficulty.LockID}, {instanceLock.GetInstanceId()}, {entries.MapDifficulty.DifficultyID}, \"{escapedData}\", " +
                $"{instanceLock.GetData().CompletedEncountersMask}, {instanceLock.GetData().EntranceWorldSafeLocId}, {Time.DateTimeToUnixTime(instanceLock.GetExpiryTime())}, {(instanceLock.IsExtended() ? 1 : 0)}");

            return instanceLock;
        }

        public void UpdateSharedInstanceLock(SQLTransaction trans, InstanceLockUpdateEvent updateEvent)
        {
            var sharedData = _instanceLockDataById.LookupByKey(updateEvent.InstanceId);
            Cypher.Assert(sharedData != null);
            Cypher.Assert(sharedData.InstanceId == updateEvent.InstanceId);
            sharedData.Data = updateEvent.NewData;
            if (updateEvent.CompletedEncounter != null)
            {
                sharedData.CompletedEncountersMask |= 1u << updateEvent.CompletedEncounter.Bit;
                Log.outDebug(LogFilter.Instance, $"Instance {updateEvent.InstanceId} gains completed encounter [{updateEvent.CompletedEncounter.Id}-{updateEvent.CompletedEncounter.Name[Global.WorldMgr.GetDefaultDbcLocale()]}]");
            }

            if (updateEvent.EntranceWorldSafeLocId.HasValue)
                sharedData.EntranceWorldSafeLocId = updateEvent.EntranceWorldSafeLocId.Value;

            trans.Append($"DELETE FROM instance2 WHERE instanceId={sharedData.InstanceId}");
            string escapedData = sharedData.Data;
            CharacterDatabase.EscapeString(ref escapedData);
            trans.Append($"INSERT INTO instance2 (instanceId, data, completedEncountersMask, entranceWorldSafeLocId) VALUES ({sharedData.InstanceId}, \"{escapedData}\", {sharedData.CompletedEncountersMask}, {sharedData.EntranceWorldSafeLocId})");
        }

        public void OnSharedInstanceLockDataDelete(uint instanceId)
        {
            if (_unloading)
                return;

            _instanceLockDataById.Remove(instanceId);
            DB.Characters.Execute($"DELETE FROM instance2 WHERE instanceId={instanceId}");
            Log.outDebug(LogFilter.Instance, $"Deleting instance {instanceId} as it is no longer referenced by any player");
        }

        public Tuple<DateTime, DateTime> UpdateInstanceLockExtensionForPlayer(ObjectGuid playerGuid, MapDb2Entries entries, bool extended)
        {
            InstanceLock instanceLock = FindActiveInstanceLock(playerGuid, entries, true, false);
            if (instanceLock != null)
            {
                DateTime oldExpiryTime = instanceLock.GetEffectiveExpiryTime();
                instanceLock.SetExtended(extended);
                DB.Characters.Execute($"UPDATE character_instance_lock SET extended = {(extended ? 1 : 0)} WHERE guid = {playerGuid.GetCounter()} AND mapId = {entries.MapDifficulty.MapID} AND lockId = {entries.MapDifficulty.LockID}");
                Log.outDebug(LogFilter.Instance, $"[{entries.Map.Id}-{entries.Map.MapName[Global.WorldMgr.GetDefaultDbcLocale()]} | " +
                    $"{entries.MapDifficulty.DifficultyID}-{CliDB.DifficultyStorage.LookupByKey(entries.MapDifficulty.DifficultyID).Name}] Instance lock for {playerGuid} is {(extended ? "now" : "no longer")} extended");
                return Tuple.Create(oldExpiryTime, instanceLock.GetEffectiveExpiryTime());
            }

            return Tuple.Create(DateTime.MinValue, DateTime.MinValue);
        }

        public DateTime GetNextResetTime(MapDb2Entries entries)
        {
            DateTime dateTime = GameTime.GetDateAndTime();
            int resetHour = ConfigMgr.GetDefaultValue("ResetSchedule.DailyHour", 9);

            int hour = 0;
            int day = 0;
            switch (entries.MapDifficulty.ResetInterval)
            {
                case MapDifficultyResetInterval.Daily:
                {
                    if (dateTime.Hour >= resetHour)
                        day++;

                    hour = resetHour;
                    break;
                }
                case MapDifficultyResetInterval.Weekly:
                {
                    int resetDay = ConfigMgr.GetDefaultValue("ResetSchedule.WeeklyDay", 2);
                    int daysAdjust = resetDay - dateTime.Day;
                    if (dateTime.Day > resetDay || (dateTime.Day == resetDay && dateTime.Hour >= resetHour))
                        daysAdjust += 7; // passed it for current week, grab time from next week

                    hour = resetHour;
                    day += daysAdjust;
                    break;
                }
                default:
                    break;
            }

            return new DateTime(dateTime.Year, dateTime.Month, day, hour, 0, 0);
        }
    }

    public class InstanceLockData
    {
        public string Data;
        public uint CompletedEncountersMask;
        public uint EntranceWorldSafeLocId;
    }

    public class InstanceLock
    {
        uint _mapId;
        Difficulty _difficultyId;
        uint _instanceId;
        DateTime _expiryTime;
        bool _extended;
        InstanceLockData _data;

        public InstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId)
        {
            _mapId = mapId;
            _difficultyId = difficultyId;
            _instanceId = instanceId;
            _expiryTime = expiryTime;
            _extended = false;
        }

        public bool IsExpired()
        {
            return _expiryTime < GameTime.GetSystemTime();
        }

        public DateTime GetEffectiveExpiryTime()
        {
            if (!IsExtended())
                return GetExpiryTime();

            MapDb2Entries entries = new(_mapId, _difficultyId);

            // return next reset time
            if (IsExpired())
                return Global.InstanceLockMgr.GetNextResetTime(entries);

            // if not expired, return expiration time + 1 reset period
            return GetExpiryTime() + TimeSpan.FromSeconds(entries.MapDifficulty.GetRaidDuration());
        }

        public uint GetMapId() { return _mapId; }

        public Difficulty GetDifficultyId() { return _difficultyId; }

        public uint GetInstanceId() { return _instanceId; }

        public virtual void SetInstanceId(uint instanceId) { _instanceId = instanceId; }

        public DateTime GetExpiryTime() { return _expiryTime; }

        public void SetExpiryTime(DateTime expiryTime) { _expiryTime = expiryTime; }

        public bool IsExtended() { return _extended; }

        public void SetExtended(bool extended) { _extended = extended; }

        public InstanceLockData GetData() { return _data; }

        public virtual InstanceLockData GetInstanceInitializationData() { return _data; }
    }

    class SharedInstanceLockData : InstanceLockData
    {
        public uint InstanceId;

        ~SharedInstanceLockData()
        {
            // Cleanup database
            if (InstanceId != 0)
                Global.InstanceLockMgr.OnSharedInstanceLockDataDelete(InstanceId);
        }
    }

    class SharedInstanceLock : InstanceLock
    {

        /// <summary>
        /// Instance id based locks have two states
        /// One shared by everyone, which is the real state used by instance
        /// and one for each player that shows in UI that might have less encounters completed
        /// </summary>
        SharedInstanceLockData _sharedData;

        public SharedInstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId, SharedInstanceLockData sharedData) : base(mapId, difficultyId, expiryTime, instanceId)
        {
            _sharedData = sharedData;            
        }

        public override void SetInstanceId(uint instanceId)
        {
            base.SetInstanceId(instanceId);
            _sharedData.InstanceId = instanceId;
        }

        public override InstanceLockData GetInstanceInitializationData() { return _sharedData; }

        public SharedInstanceLockData GetSharedData() { return _sharedData; }
    }

    public struct MapDb2Entries
    {
        public MapRecord Map;
        public MapDifficultyRecord MapDifficulty;

        public MapDb2Entries(uint mapId, Difficulty difficulty)
        {
            Map = CliDB.MapStorage.LookupByKey(mapId);
            MapDifficulty = Global.DB2Mgr.GetMapDifficultyData(mapId, difficulty);
        }

        public MapDb2Entries(MapRecord map, MapDifficultyRecord mapDifficulty)
        {
            Map = map;
            MapDifficulty = mapDifficulty;
        }

        public InstanceLockKey GetKey()
        {
            return Tuple.Create(MapDifficulty.MapID, (uint)MapDifficulty.LockID);
        }

        public bool IsInstanceIdBound()
        {
            return !Map.IsFlexLocking() && !MapDifficulty.IsUsingEncounterLocks();
        }
    }

    public struct InstanceLockUpdateEvent
    {
        public uint InstanceId;
        public string NewData;
        public uint InstanceCompletedEncountersMask;
        public DungeonEncounterRecord CompletedEncounter;
        public uint? EntranceWorldSafeLocId;

        public InstanceLockUpdateEvent(uint instanceId, string newData, uint instanceCompletedEncountersMask, DungeonEncounterRecord completedEncounter, uint? entranceWorldSafeLocId)
        {
            InstanceId = instanceId;
            NewData = newData;
            InstanceCompletedEncountersMask = instanceCompletedEncountersMask;
            CompletedEncounter = completedEncounter;
            EntranceWorldSafeLocId = entranceWorldSafeLocId;
        }
    }
}
