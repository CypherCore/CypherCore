﻿/*
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
using Game.DataStorage;
using Game.Groups;
using Game.Maps;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Game.Entities
{
    public class MapManager : Singleton<MapManager>
    {
        private MapManager()
        {
            i_gridCleanUpDelay = WorldConfig.GetUIntValue(WorldCfg.IntervalGridclean);
            i_timer.SetInterval(WorldConfig.GetIntValue(WorldCfg.IntervalMapupdate));
        }

        public void Initialize()
        {
            //todo needs alot of support for threadsafe.
            var num_threads = WorldConfig.GetIntValue(WorldCfg.Numthreads);
            // Start mtmaps if needed.
            if (num_threads > 0)
                m_updater = new MapUpdater(WorldConfig.GetIntValue(WorldCfg.Numthreads));
        }

        public void InitializeParentMapData(MultiMap<uint, uint> mapData)
        {
            _parentMapData = mapData;
        }

        public void InitializeVisibilityDistanceInfo()
        {
            foreach (var pair in i_maps)
                pair.Value.InitVisibilityDistance();
        }

        public Map CreateBaseMap(uint id)
        {
            var map = FindBaseMap(id);
            if (map == null)
            {
                var entry = CliDB.MapStorage.LookupByKey(id);
                Cypher.Assert(entry != null);
                if (entry.ParentMapID != -1 || entry.CosmeticParentMapID != -1)
                {
                    CreateBaseMap((uint)(entry.ParentMapID != -1 ? entry.ParentMapID : entry.CosmeticParentMapID));

                    // must have been created by parent map
                    map = FindBaseMap(id);
                    return map;
                }

                lock(_mapsLock)
                    map = CreateBaseMap_i(entry);
            }
            Cypher.Assert(map != null);
            return map;
        }

        private Map CreateBaseMap_i(MapRecord mapEntry)
        {
            Map map;
            if (mapEntry.Instanceable())
                map = new MapInstanced(mapEntry.Id, i_gridCleanUpDelay);
            else
                map = new Map(mapEntry.Id, i_gridCleanUpDelay, 0, Difficulty.None);

            map.DiscoverGridMapFiles();

            i_maps[mapEntry.Id] = map;

            foreach (var childMapId in _parentMapData[mapEntry.Id])
                map.AddChildTerrainMap(CreateBaseMap_i(CliDB.MapStorage.LookupByKey(childMapId)));

            if (!mapEntry.Instanceable())
            {
                map.LoadRespawnTimes();
                map.LoadCorpseData();
            }

            return map;
        }

        public Map FindBaseNonInstanceMap(uint mapId)
        {
            var map = FindBaseMap(mapId);
            if (map != null && map.Instanceable())
                return null;
            return map;
        }

        public Map CreateMap(uint id, Player player, uint loginInstanceId = 0)
        {
            var m = CreateBaseMap(id);

            if (m != null && m.Instanceable())
                m = ((MapInstanced)m).CreateInstanceForPlayer(id, player, loginInstanceId);

            return m;
        }

        public Map FindMap(uint mapid, uint instanceId)
        {
            var map = FindBaseMap(mapid);
            if (map == null)
                return null;

            if (!map.Instanceable())
                return instanceId == 0 ? map : null;

            return ((MapInstanced)map).FindInstanceMap(instanceId);
        }

        public EnterState PlayerCannotEnter(uint mapid, Player player, bool loginCheck = false)
        {
            var entry = CliDB.MapStorage.LookupByKey(mapid);
            if (entry == null)
                return EnterState.CannotEnterNoEntry;

            if (!entry.IsDungeon())
                return EnterState.CanEnter;

            InstanceTemplate instance = Global.ObjectMgr.GetInstanceTemplate(mapid);
            if (instance == null)
                return EnterState.CannotEnterUninstancedDungeon;

            Difficulty targetDifficulty = player.GetDifficultyID(entry);
            // Get the highest available difficulty if current setting is higher than the instance allows
            var mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(entry.Id, ref targetDifficulty);
            if (mapDiff == null)
                return EnterState.CannotEnterDifficultyUnavailable;

            //Bypass checks for GMs
            if (player.IsGameMaster())
                return EnterState.CanEnter;

            var mapName = entry.MapName[Global.WorldMgr.GetDefaultDbcLocale()];

            var group = player.GetGroup();
            if (entry.IsRaid() && entry.Expansion() >= (Expansion)WorldConfig.GetIntValue(WorldCfg.Expansion)) // can only enter in a raid group but raids from old expansion don't need a group
                if ((!group || !group.IsRaidGroup()) && WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreRaid))
                    return EnterState.CannotEnterNotInRaid;

            if (!player.IsAlive())
            {
                if (player.HasCorpse())
                {
                    // let enter in ghost mode in instance that connected to inner instance with corpse
                    var corpseMap = player.GetCorpseLocation().GetMapId();
                    do
                    {
                        if (corpseMap == mapid)
                            break;

                        InstanceTemplate corpseInstance = Global.ObjectMgr.GetInstanceTemplate(corpseMap);
                        corpseMap = corpseInstance != null ? corpseInstance.Parent : 0;
                    } while (corpseMap != 0);

                    if (corpseMap == 0)
                        return EnterState.CannotEnterCorpseInDifferentInstance;

                    Log.outDebug(LogFilter.Maps, "MAP: Player '{0}' has corpse in instance '{1}' and can enter.", player.GetName(), mapName);
                }
                else
                    Log.outDebug(LogFilter.Maps, "Map.CanPlayerEnter - player '{0}' is dead but does not have a corpse!", player.GetName());
            }

            //Get instance where player's group is bound & its map
            if (!loginCheck && group)
            {
                InstanceBind boundInstance = group.GetBoundInstance(entry);
                if (boundInstance != null && boundInstance.save != null)
                {
                    var boundMap = FindMap(mapid, boundInstance.save.GetInstanceId());
                    if (boundMap != null)
                    {
                        var denyReason = boundMap.CannotEnter(player);
                        if (denyReason != 0)
                            return denyReason;
                    }
                }
            }
            // players are only allowed to enter 10 instances per hour
            if (entry.IsDungeon() && (player.GetGroup() == null || (player.GetGroup() != null && !player.GetGroup().IsLFGGroup())))
            {
                uint instaceIdToCheck = 0;
                InstanceSave save = player.GetInstanceSave(mapid);
                if (save != null)
                    instaceIdToCheck = save.GetInstanceId();

                // instanceId can never be 0 - will not be found
                if (!player.CheckInstanceCount(instaceIdToCheck) && !player.IsDead())
                    return EnterState.CannotEnterTooManyInstances;
            }

            //Other requirements
            if (player.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapid, targetDifficulty), mapid, true))
                return EnterState.CanEnter;
            else
                return EnterState.CannotEnterUnspecifiedReason;
        }

        public void Update(uint diff)
        {
            i_timer.Update(diff);
            if (!i_timer.Passed())
                return;

            var time = (uint)i_timer.GetCurrent();
            foreach (var map in i_maps.Values)
            {
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

        public bool ExistMapAndVMap(uint mapid, float x, float y)
        {
            var p = GridDefines.ComputeGridCoord(x, y);

            var gx = (MapConst.MaxGrids - 1) - p.X_coord;
            var gy = (MapConst.MaxGrids - 1) - p.Y_coord;

            return Map.ExistMap(mapid, gx, gy) && Map.ExistVMap(mapid, gx, gy);
        }

        public bool IsValidMAP(uint mapid, bool startUp)
        {
            var mEntry = CliDB.MapStorage.LookupByKey(mapid);

            if (startUp)
                return mEntry != null ? true : false;
            else
                return mEntry != null && (!mEntry.IsDungeon() || Global.ObjectMgr.GetInstanceTemplate(mapid) != null);

            // TODO: add check for Battlegroundtemplate
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
            {
                uint ret = 0;
                foreach (var pair in i_maps)
                {
                    var map = pair.Value;
                    if (!map.Instanceable())
                        continue;
                    var maps = ((MapInstanced)map).GetInstancedMaps();
                    foreach (var imap in maps)
                        if (imap.Value.IsDungeon())
                            ret++;
                }
                return ret;
            }
        }

        public uint GetNumPlayersInInstances()
        {
            lock (_mapsLock)
            {
                uint ret = 0;
                foreach (var pair in i_maps)
                {
                    var map = pair.Value;
                    if (!map.Instanceable())
                        continue;
                    var maps = ((MapInstanced)map).GetInstancedMaps();
                    foreach (var imap in maps)
                        if (imap.Value.IsDungeon())
                            ret += (uint)imap.Value.GetPlayers().Count;
                }
                return ret;
            }
        }

        public void InitInstanceIds()
        {
            _nextInstanceId = 1;

            var result = DB.Characters.Query("SELECT IFNULL(MAX(id), 0) FROM instance");
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

            var newInstanceId = _nextInstanceId;
            Cypher.Assert(newInstanceId < _freeInstanceIds.Length);
            _freeInstanceIds[(int)newInstanceId] = false;

            // Find the lowest available id starting from the current NextInstanceId (which should be the lowest according to the logic in FreeInstanceId()
            var nextFreeId = -1;
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

        private Map FindBaseMap(uint mapId)
        {
            return i_maps.LookupByKey(mapId);
        }

        private uint GetAreaId(PhaseShift phaseShift, uint mapid, float x, float y, float z)
        {
            var m = CreateBaseMap(mapid);
            return m.GetAreaId(phaseShift, x, y, z);
        }

        public uint GetZoneId(PhaseShift phaseShift, uint mapid, float x, float y, float z)
        {
            var m = CreateBaseMap(mapid);
            return m.GetZoneId(phaseShift, x, y, z);
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, uint mapid, float x, float y, float z)
        {
            var m = CreateBaseMap(mapid);
            m.GetZoneAndAreaId(phaseShift, out zoneid, out areaid, x, y, z);
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, uint mapid, Position pos)
        {
            GetZoneAndAreaId(phaseShift, out zoneid, out areaid, mapid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ());
        }

        public void GetZoneAndAreaId(PhaseShift phaseShift, out uint zoneid, out uint areaid, WorldLocation loc)
        {
            GetZoneAndAreaId(phaseShift, out zoneid, out areaid, loc.GetMapId(), loc);
        }
        
        public void DoForAllMaps(Action<Map> worker)
        {
            lock (_mapsLock)
            {
                foreach (var map in i_maps.Values)
                {
                    var mapInstanced = map.ToMapInstanced();
                    if (mapInstanced)
                    {
                        var instances = mapInstanced.GetInstancedMaps();
                        foreach (var instance in instances.Values)
                            worker(instance);
                    }
                    else
                        worker(map);
                }
            }
        }

        public void DoForAllMapsWithMapId(uint mapId, Action<Map> worker)
        {
            lock (_mapsLock)
            {
                var map = i_maps.LookupByKey(mapId);
                if (map != null)
                {
                    var mapInstanced = map.ToMapInstanced();
                    if (mapInstanced)
                    {
                        var instances = mapInstanced.GetInstancedMaps();
                        foreach (var instance in instances)
                            worker(instance.Value);
                    }
                    else
                        worker(map);
                }
            }
        }

        public void IncreaseScheduledScriptsCount() { ++_scheduledScripts; }
        public void DecreaseScheduledScriptCount() { --_scheduledScripts; }
        public void DecreaseScheduledScriptCount(uint count) { _scheduledScripts -= count; }
        public bool IsScriptScheduled() { return _scheduledScripts > 0; }

        private Dictionary<uint, Map> i_maps = new Dictionary<uint, Map>();
        private IntervalTimer i_timer = new IntervalTimer();
        private object _mapsLock= new object();
        private uint i_gridCleanUpDelay;
        private BitSet _freeInstanceIds;
        private uint _nextInstanceId;
        private MapUpdater m_updater;
        private uint _scheduledScripts;

        // parent map links
        private MultiMap<uint, uint> _parentMapData;
    }
}
