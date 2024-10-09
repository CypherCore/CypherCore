// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Garrisons;
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class MapManager : Singleton<MapManager>
    {
        MapManager()
        {
            i_gridCleanUpDelay = WorldConfig.GetUIntValue(WorldCfg.IntervalGridclean);
            i_timer.SetInterval(WorldConfig.GetIntValue(WorldCfg.IntervalMapupdate));
        }

        public void Initialize()
        {
            //todo needs alot of support for threadsafe.
            int num_threads = WorldConfig.GetIntValue(WorldCfg.Numthreads);
            // Start mtmaps if needed.
            if (num_threads > 0)
                m_updater = new MapUpdater(WorldConfig.GetIntValue(WorldCfg.Numthreads));
        }

        public void InitializeVisibilityDistanceInfo()
        {
            foreach (var pair in i_maps)
                pair.Value.InitVisibilityDistance();
        }

        Map FindMap_i(uint mapId, uint instanceId)
        {
            return i_maps.LookupByKey((mapId, instanceId));
        }

        Map CreateWorldMap(uint mapId, uint instanceId)
        {
            Map map = new Map(mapId, i_gridCleanUpDelay, instanceId, Difficulty.None);
            map.LoadRespawnTimes();
            map.LoadCorpseData();
            map.InitSpawnGroupState();

            if (WorldConfig.GetBoolValue(WorldCfg.BasemapLoadGrids))
                map.LoadAllCells();

            return map;
        }

        InstanceMap CreateInstance(uint mapId, uint instanceId, InstanceLock instanceLock, Difficulty difficulty, int team, Group group)
        {
            // make sure we have a valid map id
            var entry = CliDB.MapStorage.LookupByKey(mapId);
            if (entry == null)
            {
                Log.outError(LogFilter.Maps, $"CreateInstance: no entry for map {mapId}");
                //ABORT();
                return null;
            }

            // some instances only have one difficulty
            Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty);

            Log.outDebug(LogFilter.Maps, $"MapInstanced::CreateInstance: {(instanceLock?.IsNew() == true ? "new" : " ")} map instance {instanceId} for {mapId} created with difficulty {difficulty}");

            InstanceMap map = new InstanceMap(mapId, i_gridCleanUpDelay, instanceId, difficulty, team, instanceLock);
            Cypher.Assert(map.IsDungeon());

            map.LoadRespawnTimes();
            map.LoadCorpseData();
            if (group != null)
                map.TrySetOwningGroup(group);

            map.CreateInstanceData();
            map.SetInstanceScenario(Global.ScenarioMgr.CreateInstanceScenario(map, team));
            map.InitSpawnGroupState();

            if (WorldConfig.GetBoolValue(WorldCfg.InstancemapLoadGrids))
                map.LoadAllCells();

            return map;
        }

        BattlegroundMap CreateBattleground(uint mapId, uint instanceId, Battleground bg)
        {
            Log.outDebug(LogFilter.Maps, $"MapInstanced::CreateBattleground: map bg {instanceId} for {mapId} created.");

            BattlegroundMap map = new BattlegroundMap(mapId, i_gridCleanUpDelay, instanceId, Difficulty.None);
            Cypher.Assert(map.IsBattlegroundOrArena());
            map.SetBG(bg);
            bg.SetBgMap(map);
            map.InitScriptData();
            map.InitSpawnGroupState();

            if (WorldConfig.GetBoolValue(WorldCfg.BattlegroundMapLoadGrids))
                map.LoadAllCells();

            return map;
        }

        GarrisonMap CreateGarrison(uint mapId, uint instanceId, Player owner)
        {
            GarrisonMap map = new GarrisonMap(mapId, i_gridCleanUpDelay, instanceId, owner.GetGUID());
            Cypher.Assert(map.IsGarrison());
            map.InitSpawnGroupState();
            return map;
        }

        /// <summary>
        /// create the instance if it's not created already
        /// the player is not actually added to the instance(only in InstanceMap::Add)
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="player"></param>
        /// <param name="loginInstanceId"></param>
        /// <returns>the right instance for the object, based on its InstanceId</returns>
        public Map CreateMap(uint mapId, Player player)
        {
            if (player == null)
                return null;

            var entry = CliDB.MapStorage.LookupByKey(mapId);
            if (entry == null)
                return null;

            lock (_mapsLock)
            {
                Map map = null;
                uint newInstanceId = 0;                       // instanceId of the resulting map

                if (entry.IsBattlegroundOrArena())
                {
                    // instantiate or find existing bg map for player
                    // the instance id is set in battlegroundid
                    newInstanceId = player.GetBattlegroundId();
                    if (newInstanceId == 0)
                        return null;

                    map = FindMap_i(mapId, newInstanceId);
                    if (map == null)
                    {
                        Battleground bg = player.GetBattleground();
                        if (bg != null)
                            map = CreateBattleground(mapId, newInstanceId, bg);
                        else
                        {
                            player.TeleportToBGEntryPoint();
                            return null;
                        }
                    }
                }
                else if (entry.IsDungeon())
                {
                    Group group = player.GetGroup();
                    Difficulty difficulty = group != null ? group.GetDifficultyID(entry) : player.GetDifficultyID(entry);
                    MapDb2Entries entries = new(entry, Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty));
                    ObjectGuid instanceOwnerGuid = group != null ? group.GetRecentInstanceOwner(mapId) : player.GetGUID();
                    InstanceLock instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(instanceOwnerGuid, entries);
                    if (instanceLock != null)
                    {
                        newInstanceId = instanceLock.GetInstanceId();

                        // Reset difficulty to the one used in instance lock
                        if (!entries.Map.IsFlexLocking())
                            difficulty = instanceLock.GetDifficultyId();
                    }
                    else
                    {
                        // Try finding instance id for normal dungeon
                        if (!entries.MapDifficulty.HasResetSchedule())
                            newInstanceId = group != null ? group.GetRecentInstanceId(mapId) : player.GetRecentInstanceId(mapId);

                        // If not found or instance is not a normal dungeon, generate new one
                        if (newInstanceId == 0)
                            newInstanceId = GenerateInstanceId();
                        instanceLock = Global.InstanceLockMgr.CreateInstanceLockForNewInstance(instanceOwnerGuid, entries, newInstanceId);
                    }

                    // it is possible that the save exists but the map doesn't
                    map = FindMap_i(mapId, newInstanceId);

                    // is is also possible that instance id is already in use by another group for boss-based locks
                    if (!entries.IsInstanceIdBound() && instanceLock != null && map != null && map.ToInstanceMap().GetInstanceLock() != instanceLock)
                    {
                        newInstanceId = GenerateInstanceId();
                        instanceLock.SetInstanceId(newInstanceId);
                        map = null;
                    }

                    if (map == null)
                    {
                        map = CreateInstance(mapId, newInstanceId, instanceLock, difficulty, SharedConst.GetTeamIdForTeam(Global.CharacterCacheStorage.GetCharacterTeamByGuid(instanceOwnerGuid)), group);
                        if (group != null)
                            group.SetRecentInstance(mapId, instanceOwnerGuid, newInstanceId);
                        else
                            player.SetRecentInstance(mapId, newInstanceId);
                    }
                }
                else if (entry.IsGarrison())
                {
                    newInstanceId = (uint)player.GetGUID().GetCounter();
                    map = FindMap_i(mapId, newInstanceId);
                    if (map == null)
                        map = CreateGarrison(mapId, newInstanceId, player);
                }
                else
                {
                    newInstanceId = 0;
                    if (entry.IsSplitByFaction())
                        newInstanceId = (uint)player.GetTeamId();

                    map = FindMap_i(mapId, newInstanceId);
                    if (map == null)
                        map = CreateWorldMap(mapId, newInstanceId);
                }

                if (map != null)
                {
                    i_maps[(map.GetId(), map.GetInstanceId())] = map;

                    Global.ScriptMgr.OnCreateMap(map);
                    Global.OutdoorPvPMgr.CreateOutdoorPvPForMap(map);
                    Global.BattleFieldMgr.CreateBattlefieldsForMap(map);
                }
                
                return map;
            }
        }

        public Map FindMap(uint mapId, uint instanceId)
        {
            lock (_mapsLock)
                return FindMap_i(mapId, instanceId);
        }

        public uint FindInstanceIdForPlayer(uint mapId, Player player)
        {
            MapRecord entry = CliDB.MapStorage.LookupByKey(mapId);
            if (entry == null)
                return 0;

            if (entry.IsBattlegroundOrArena())
                return player.GetBattlegroundId();
            else if (entry.IsDungeon())
            {
                Group group = player.GetGroup();
                Difficulty difficulty = group != null ? group.GetDifficultyID(entry) : player.GetDifficultyID(entry);
                MapDb2Entries entries = new(entry, Global.DB2Mgr.GetDownscaledMapDifficultyData(mapId, ref difficulty));

                ObjectGuid instanceOwnerGuid = group != null ? group.GetRecentInstanceOwner(mapId) : player.GetGUID();
                InstanceLock instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(instanceOwnerGuid, entries);
                uint newInstanceId = 0;
                if (instanceLock != null)
                    newInstanceId = instanceLock.GetInstanceId();
                else if (!entries.MapDifficulty.HasResetSchedule()) // Try finding instance id for normal dungeon
                    newInstanceId = group != null ? group.GetRecentInstanceId(mapId) : player.GetRecentInstanceId(mapId);

                if (newInstanceId == 0)
                    return 0;

                Map map = FindMap(mapId, newInstanceId);

                // is is possible that instance id is already in use by another group for boss-based locks
                if (!entries.IsInstanceIdBound() && instanceLock != null && map != null && map.ToInstanceMap().GetInstanceLock() != instanceLock)
                    return 0;

                return newInstanceId;
            }
            else if (entry.IsGarrison())
                return (uint)player.GetGUID().GetCounter();
            else
            {
                if (entry.IsSplitByFaction())
                    return (uint)player.GetTeamId();

                return 0;
            }
        }

        public void Update(uint diff)
        {
            i_timer.Update(diff);
            if (!i_timer.Passed())
                return;

            var time = (uint)i_timer.GetCurrent();
            foreach (var (key, map) in i_maps)
            {
                if (map.CanUnload((uint)i_timer.GetCurrent()))
                {
                    if (DestroyMap(map))
                        i_maps.Remove(key);

                    continue;
                }

                if (m_updater != null)
                    m_updater.ScheduleUpdate(map, (uint)i_timer.GetCurrent());
                else
                    map.Update(time);
            }

            if (m_updater != null)
                m_updater.Wait();

            foreach (var map in i_maps)
                map.Value.DelayedUpdate(time);

            i_timer.SetCurrent(0);
        }

        bool DestroyMap(Map map)
        {
            map.RemoveAllPlayers();
            if (map.HavePlayers())
                return false;

            map.UnloadAll();

            Global.OutdoorPvPMgr.DestroyOutdoorPvPForMap(map);
            Global.BattleFieldMgr.DestroyBattlefieldsForMap(map);
            Global.ScriptMgr.OnDestroyMap(map);

            // Free up the instance id and allow it to be reused for normal dungeons, bgs and arenas
            if (map.IsBattlegroundOrArena() || (map.IsDungeon() && !map.GetMapDifficulty().HasResetSchedule()))
                FreeInstanceId(map.GetInstanceId());

            // erase map
            map.Dispose();
            return true;
        }

        public bool IsValidMAP(uint mapId)
        {
            return CliDB.MapStorage.ContainsKey(mapId);
        }

        public void UnloadAll()
        {
            // first unload maps
            foreach (var (_, map) in i_maps)
            {
                map.UnloadAll();

                Global.OutdoorPvPMgr.DestroyOutdoorPvPForMap(map);
                Global.BattleFieldMgr.DestroyBattlefieldsForMap(map);
                Global.ScriptMgr.OnDestroyMap(map);
            }

            foreach (var (_, map) in i_maps)
                map.Dispose();

            i_maps.Clear();

            if (m_updater != null)
                m_updater.Deactivate();
        }

        public uint GetNumInstances()
        {
            lock (_mapsLock)
                return (uint)i_maps.Count(pair => pair.Value.IsDungeon());
        }

        public uint GetNumPlayersInInstances()
        {
            lock (_mapsLock)
                return (uint)i_maps.Sum(pair => pair.Value.IsDungeon() ? pair.Value.GetPlayers().Count : 0);
        }

        public void InitInstanceIds()
        {
            _nextInstanceId = 1;

            ulong maxExistingInstanceId = 0;
            SQLResult result = DB.Characters.Query("SELECT IFNULL(MAX(instanceId), 0) FROM instance");
            if (!result.IsEmpty())
                maxExistingInstanceId = Math.Max(maxExistingInstanceId, result.Read<ulong>(0));

            result = DB.Characters.Query("SELECT IFNULL(MAX(instanceId), 0) FROM character_instance_lock");
            if (!result.IsEmpty())
                maxExistingInstanceId = Math.Max(maxExistingInstanceId, result.Read<ulong>(0));

            _freeInstanceIds.Length = (int)(maxExistingInstanceId + 2); // make space for one extra to be able to access [_nextInstanceId] index in case all slots are taken

            // never allow 0 id
            _freeInstanceIds[0] = false;
        }

        public void RegisterInstanceId(uint instanceId)
        {
            _freeInstanceIds[(int)instanceId] = false;

            // Instances are pulled in ascending order from db and nextInstanceId is initialized with 1,
            // so if the instance id is used, increment until we find the first unused one for a potential new instance
            if (_nextInstanceId == instanceId)
                ++_nextInstanceId;
        }

        public uint GenerateInstanceId()
        {
            if (_nextInstanceId == 0xFFFFFFFF)
            {
                Log.outError(LogFilter.Maps, "Instance ID overflow!! Can't continue, shutting down server. ");
                Global.WorldMgr.StopNow();
                return _nextInstanceId;
            }

            uint newInstanceId = _nextInstanceId;
            Cypher.Assert(newInstanceId < _freeInstanceIds.Length);
            _freeInstanceIds[(int)newInstanceId] = false;

            // Find the lowest available id starting from the current NextInstanceId (which should be the lowest according to the logic in FreeInstanceId()
            int nextFreeId = -1;
            for (var i = (int)_nextInstanceId++; i < _freeInstanceIds.Length; i++)
            {
                if (_freeInstanceIds[i])
                {
                    nextFreeId = i;
                    break;
                }
            }

            if (nextFreeId == -1)
            {
                _nextInstanceId = (uint)_freeInstanceIds.Length;
                _freeInstanceIds.Length += 1;
                _freeInstanceIds[(int)_nextInstanceId] = true;
            }
            else
                _nextInstanceId = (uint)nextFreeId;

            return newInstanceId;
        }

        public void FreeInstanceId(uint instanceId)
        {
            // If freed instance id is lower than the next id available for new instances, use the freed one instead
            _nextInstanceId = Math.Min(instanceId, _nextInstanceId);
            _freeInstanceIds[(int)instanceId] = true;
        }

        public void SetGridCleanUpDelay(uint t)
        {
            if (t < MapConst.MinGridDelay)
                i_gridCleanUpDelay = MapConst.MinGridDelay;
            else
                i_gridCleanUpDelay = t;
        }

        public void SetMapUpdateInterval(int t)
        {
            if (t < MapConst.MinMapUpdateDelay)
                t = MapConst.MinMapUpdateDelay;

            i_timer.SetInterval(t);
            i_timer.Reset();
        }

        public uint GetNextInstanceId() { return _nextInstanceId; }

        public void SetNextInstanceId(uint nextInstanceId) { _nextInstanceId = nextInstanceId; }

        public void DoForAllMaps(Action<Map> worker)
        {
            lock (_mapsLock)
            {
                foreach (var (_, map) in i_maps)
                    worker(map);
            }
        }

        public void DoForAllMapsWithMapId(uint mapId, Action<Map> worker)
        {
            lock (_mapsLock)
            {
                var list = i_maps.Where(pair => pair.Key.mapId == mapId && pair.Key.instanceId >= 0);
                foreach (var (_, map) in list)
                    worker(map);
            }
        }

        public void AddSC_BuiltInScripts()
        {
            foreach (var (_, mapEntry) in CliDB.MapStorage)
                if (mapEntry.IsWorldMap() && mapEntry.IsSplitByFaction())
                    new SplitByFactionMapScript($"world_map_set_faction_worldstates_{mapEntry.Id}", mapEntry.Id);
        }

        public void IncreaseScheduledScriptsCount() { ++_scheduledScripts; }
        public void DecreaseScheduledScriptCount() { --_scheduledScripts; }
        public void DecreaseScheduledScriptCount(uint count) { _scheduledScripts -= count; }
        public bool IsScriptScheduled() { return _scheduledScripts > 0; }

        Dictionary<(uint mapId, uint instanceId), Map> i_maps = new();
        IntervalTimer i_timer = new();
        object _mapsLock = new();
        uint i_gridCleanUpDelay;
        BitSet _freeInstanceIds = new(1);
        uint _nextInstanceId;
        MapUpdater m_updater;
        uint _scheduledScripts;
    }

    // hack to allow conditions to access what faction owns the map (these worldstates should not be set on these maps)
    class SplitByFactionMapScript : WorldMapScript
    {
        public SplitByFactionMapScript(string name, uint mapId) : base(name, mapId) { }

        public override void OnCreate(Map map)
        {
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, map.GetInstanceId() == BattleGroundTeamId.Alliance ? 1 : 0, false, map);
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, map.GetInstanceId() == BattleGroundTeamId.Horde ? 1 : 0, false, map);
        }
    }
}
