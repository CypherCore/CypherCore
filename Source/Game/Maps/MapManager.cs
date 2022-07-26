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

            if (WorldConfig.GetBoolValue(WorldCfg.BasemapLoadGrids))
                map.LoadAllCells();

            return map;
        }

        InstanceMap CreateInstance(uint mapId, uint instanceId, InstanceSave save, Difficulty difficulty, int team)
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

            Log.outDebug(LogFilter.Maps, $"MapInstanced::CreateInstance: {(save != null ? "" : "new ")} map instance {instanceId} for {mapId} created with difficulty {difficulty}");

            InstanceMap map = new InstanceMap(mapId, i_gridCleanUpDelay, instanceId, difficulty, team);
            Cypher.Assert(map.IsDungeon());

            map.LoadRespawnTimes();
            map.LoadCorpseData();

            bool load_data = save != null;
            map.CreateInstanceData(load_data);
            var instanceScenario = Global.ScenarioMgr.CreateInstanceScenario(map, team);
            if (instanceScenario != null)
                map.SetInstanceScenario(instanceScenario);

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
            return map;
        }

        GarrisonMap CreateGarrison(uint mapId, uint instanceId, Player owner)
        {
            GarrisonMap map = new GarrisonMap(mapId, i_gridCleanUpDelay, instanceId, owner.GetGUID());
            Cypher.Assert(map.IsGarrison());
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
        public Map CreateMap(uint mapId, Player player, uint loginInstanceId = 0)
        {
            if (!player)
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
                    if (!map)
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
                    InstanceBind pBind = player.GetBoundInstance(mapId, player.GetDifficultyID(entry));
                    InstanceSave pSave = pBind != null ? pBind.save : null;

                    // priority:
                    // 1. player's permanent bind
                    // 2. player's current instance id if this is at login
                    // 3. group's current bind
                    // 4. player's current bind
                    if (pBind == null || !pBind.perm)
                    {
                        if (loginInstanceId != 0) // if the player has a saved instance id on login, we either use this instance or relocate him out (return null)
                        {
                            map = FindMap_i(mapId, loginInstanceId);
                            if (map == null && pSave != null && pSave.GetInstanceId() == loginInstanceId)
                                map = CreateInstance(mapId, loginInstanceId, pSave, pSave.GetDifficultyID(), player.GetTeamId());
                            return map;
                        }

                        InstanceBind groupBind = null;
                        Group group = player.GetGroup();
                        // use the player's difficulty setting (it may not be the same as the group's)
                        if (group != null)
                        {
                            groupBind = group.GetBoundInstance(entry);
                            if (groupBind != null)
                            {
                                // solo saves should be reset when entering a group's instance
                                player.UnbindInstance(mapId, player.GetDifficultyID(entry));
                                pSave = groupBind.save;
                            }
                        }
                    }
                    if (pSave != null)
                    {
                        // solo/perm/group
                        newInstanceId = pSave.GetInstanceId();
                        map = FindMap_i(mapId, newInstanceId);
                        // it is possible that the save exists but the map doesn't
                        if (!map)
                            map = CreateInstance(mapId, newInstanceId, pSave, pSave.GetDifficultyID(), player.GetTeamId());
                    }
                    else
                    {
                        Difficulty diff = player.GetGroup() ? player.GetGroup().GetDifficultyID(entry) : player.GetDifficultyID(entry);

                        // if no instanceId via group members or instance saves is found
                        // the instance will be created for the first time
                        newInstanceId = GenerateInstanceId();

                        //Seems it is now possible, but I do not know if it should be allowed
                        //ASSERT(!FindInstanceMap(NewInstanceId));
                        map = FindMap_i(mapId, newInstanceId);
                        if (!map)
                            map = CreateInstance(mapId, newInstanceId, null, diff, player.GetTeamId());
                    }
                }
                else if (entry.IsGarrison())
                {
                    newInstanceId = (uint)player.GetGUID().GetCounter();
                    map = FindMap_i(mapId, newInstanceId);
                    if (!map)
                        map = CreateGarrison(mapId, newInstanceId, player);
                }
                else
                {
                    newInstanceId = 0;
                    if (entry.IsSplitByFaction())
                        newInstanceId = (uint)player.GetTeamId();

                    map = FindMap_i(mapId, newInstanceId);
                    if (!map)
                        map = CreateWorldMap(mapId, newInstanceId);
                }

                if (map)
                    i_maps[(map.GetId(), map.GetInstanceId())] = map;

                return map;
            }
        }

        public Map FindMap(uint mapId, uint instanceId)
        {
            lock (_mapsLock)
                return FindMap_i(mapId, instanceId);
        }

        public void Update(uint diff)
        {
            i_timer.Update(diff);
            if (!i_timer.Passed())
                return;

            var time = (uint)i_timer.GetCurrent();
            foreach (var (key, map) in i_maps)
            {
                if (map.CanUnload(diff))
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

            // Free up the instance id and allow it to be reused for bgs and arenas (other instances are handled in the InstanceSaveMgr)
            if (map.IsBattlegroundOrArena())
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
            foreach (var pair in i_maps)
                pair.Value.UnloadAll();

            foreach (var pair in i_maps)
                pair.Value.Dispose();

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

            SQLResult result = DB.Characters.Query("SELECT IFNULL(MAX(id), 0) FROM instance");
            if (!result.IsEmpty())
                _freeInstanceIds = new BitSet(result.Read<int>(0) + 2, true); // make space for one extra to be able to access [_nextInstanceId] index in case all slots are taken
            else
                _freeInstanceIds = new BitSet((int)_nextInstanceId + 1, true);

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
        BitSet _freeInstanceIds;
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
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, map.GetInstanceId() == TeamId.Alliance ? 1 : 0, false, map);
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, map.GetInstanceId() == TeamId.Horde ? 1 : 0, false, map);
        }
    }
}
