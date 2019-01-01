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
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Garrisons;
using Game.Groups;
using Game.Scenarios;
using System.Collections.Generic;
using System.Linq;

namespace Game.Maps
{
    public class MapInstanced : Map
    {
        public MapInstanced(uint id, uint expiry) : base(id, expiry, 0, Difficulty.Normal) { }

        public override void InitVisibilityDistance()
        {
            if (m_InstancedMaps.Empty())
                return;
            //initialize visibility distances for all instance copies
            foreach (var i in m_InstancedMaps)
                i.Value.InitVisibilityDistance();
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            foreach (var pair in m_InstancedMaps.ToList())
            {
                if (pair.Value.CanUnload(diff))
                {
                    if (!DestroyInstance(pair))
                    {
                        //m_unloadTimer
                    }
                }
                else
                    pair.Value.Update(diff);
            }
        }

        public override void DelayedUpdate(uint t_diff)
        {
            foreach (var i in m_InstancedMaps)
                i.Value.DelayedUpdate(t_diff);

            base.DelayedUpdate(t_diff);
        }

        public override void UnloadAll()
        {
            // Unload instanced maps
            foreach (var i in m_InstancedMaps)
                i.Value.UnloadAll();

            m_InstancedMaps.Clear();

            base.UnloadAll();
        }

        public Map CreateInstanceForPlayer(uint mapId, Player player, uint loginInstanceId = 0)
        {
            if (GetId() != mapId || player == null)
                return null;

            Map map = null;
            uint newInstanceId = 0;                       // instanceId of the resulting map

            if (IsBattlegroundOrArena())
            {
                // instantiate or find existing bg map for player
                // the instance id is set in Battlegroundid
                newInstanceId = player.GetBattlegroundId();
                if (newInstanceId == 0)
                    return null;

                map = Global.MapMgr.FindMap(mapId, newInstanceId);
                if (map == null)
                {
                    Battleground bg = player.GetBattleground();
                    if (bg)
                        map = CreateBattleground(newInstanceId, bg);
                    else
                    {
                        player.TeleportToBGEntryPoint();
                        return null;
                    }
                }
            }
            else if(!IsGarrison())
            {
                InstanceBind pBind = player.GetBoundInstance(GetId(), player.GetDifficultyID(GetEntry()));
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
                        map = FindInstanceMap(loginInstanceId);
                        return (map && map.GetId() == GetId()) ? map : null; // is this check necessary? or does MapInstanced only find instances of itself?
                    }

                    InstanceBind groupBind = null;
                    Group group = player.GetGroup();
                    // use the player's difficulty setting (it may not be the same as the group's)
                    if (group)
                    {
                        groupBind = group.GetBoundInstance(this);
                        if (groupBind != null)
                        { 
                            // solo saves should be reset when entering a group's instance
                            player.UnbindInstance(GetId(), player.GetDifficultyID(GetEntry()));
                            pSave = groupBind.save;
                        }
                    }
                }
                if (pSave != null)
                {
                    // solo/perm/group
                    newInstanceId = pSave.GetInstanceId();
                    map = FindInstanceMap(newInstanceId);
                    // it is possible that the save exists but the map doesn't
                    if (map == null)
                        map = CreateInstance(newInstanceId, pSave, pSave.GetDifficultyID(), player.GetTeamId());
                }
                else
                {
                    // if no instanceId via group members or instance saves is found
                    // the instance will be created for the first time
                    newInstanceId = Global.MapMgr.GenerateInstanceId();

                    Difficulty diff = player.GetGroup() != null ? player.GetGroup().GetDifficultyID(GetEntry()) : player.GetDifficultyID(GetEntry());
                    //Seems it is now possible, but I do not know if it should be allowed
                    map = FindInstanceMap(newInstanceId);
                    if (map == null)
                        map = CreateInstance(newInstanceId, null, diff, player.GetTeamId());
                }
            }
            else
            {
                newInstanceId = (uint)player.GetGUID().GetCounter();
                map = FindInstanceMap(newInstanceId);
                if (!map)
                    map = CreateGarrison(newInstanceId, player);
            }

            return map;
        }

        InstanceMap CreateInstance(uint InstanceId, InstanceSave save, Difficulty difficulty, int teamId)
        {
            lock (_mapLock)
            {
                // make sure we have a valid map id
                MapRecord entry = CliDB.MapStorage.LookupByKey(GetId());
                if (entry == null)
                {
                    Log.outError(LogFilter.Maps, "CreateInstance: no record for map {0}", GetId());
                    Cypher.Assert(false);
                }
                InstanceTemplate iTemplate = Global.ObjectMgr.GetInstanceTemplate(GetId());
                if (iTemplate == null)
                {
                    Log.outError(LogFilter.Maps, "CreateInstance: no instance template for map {0}", GetId());
                    Cypher.Assert(false);
                }

                // some instances only have one difficulty
                Global.DB2Mgr.GetDownscaledMapDifficultyData(GetId(), ref difficulty);

                Log.outDebug(LogFilter.Maps, "MapInstanced.CreateInstance: {0} map instance {1} for {2} created with difficulty {3}", save != null ? "" : "new ", InstanceId, GetId(), difficulty);

                InstanceMap map = new InstanceMap(GetId(), GetGridExpiry(), InstanceId, difficulty, this);
                Cypher.Assert(map.IsDungeon());

                map.LoadRespawnTimes();
                map.LoadCorpseData();

                bool load_data = save != null;
                map.CreateInstanceData(load_data);
                InstanceScenario instanceScenario = Global.ScenarioMgr.CreateInstanceScenario(map, teamId);
                if (instanceScenario != null)
                    map.SetInstanceScenario(instanceScenario);

                if (WorldConfig.GetBoolValue(WorldCfg.InstancemapLoadGrids))
                    map.LoadAllCells();

                m_InstancedMaps[InstanceId] = map;
                return map;
            }
        }

        BattlegroundMap CreateBattleground(uint InstanceId, Battleground bg)
        {
            lock (_mapLock)
            {
                Log.outDebug(LogFilter.Maps, "MapInstanced.CreateBattleground: map bg {0} for {1} created.", InstanceId, GetId());

                BattlegroundMap map = new BattlegroundMap(GetId(), (uint)GetGridExpiry(), InstanceId, this, Difficulty.None);
                Cypher.Assert(map.IsBattlegroundOrArena());
                map.SetBG(bg);
                bg.SetBgMap(map);

                m_InstancedMaps[InstanceId] = map;
                return map;
            }
        }

        GarrisonMap CreateGarrison(uint instanceId, Player owner)
        {
            lock (_mapLock)
            {
                GarrisonMap map = new GarrisonMap(GetId(), GetGridExpiry(), instanceId, this, owner.GetGUID());
                Cypher.Assert(map.IsGarrison());

                m_InstancedMaps[instanceId] = map;
                return map;
            }
        }

        bool DestroyInstance(KeyValuePair<uint, Map> pair)
        {
            pair.Value.RemoveAllPlayers();
            if (pair.Value.HavePlayers())
                return false;

            pair.Value.UnloadAll();
            // should only unload VMaps if this is the last instance and grid unloading is enabled
            if (m_InstancedMaps.Count <= 1 && WorldConfig.GetBoolValue(WorldCfg.GridUnload))
            {
                Global.VMapMgr.unloadMap(pair.Value.GetId());
                Global.MMapMgr.unloadMap(pair.Value.GetId());
                // in that case, unload grids of the base map, too
                // so in the next map creation, (EnsureGridCreated actually) VMaps will be reloaded
                base.UnloadAll();
            }

            // Free up the instance id and allow it to be reused for bgs and arenas (other instances are handled in the InstanceSaveMgr)
            if (pair.Value.IsBattlegroundOrArena())
                Global.MapMgr.FreeInstanceId(pair.Value.GetInstanceId());

            // erase map
            pair.Value.Dispose();
            m_InstancedMaps.Remove(pair.Key);

            return true;
        }

        public override EnterState CannotEnter(Player player) { return EnterState.CanEnter; }

        public Map FindInstanceMap(uint instanceId)
        {
            return m_InstancedMaps.LookupByKey(instanceId);
        }

        public Dictionary<uint, Map> GetInstancedMaps() { return m_InstancedMaps; }

        Dictionary<uint, Map> m_InstancedMaps = new Dictionary<uint, Map>();
    }

    public class InstanceTemplate
    {
        public uint Parent;
        public uint ScriptId;
        public bool AllowMount;
    }

    public class InstanceBind
    {
        public InstanceSave save;
        public bool perm;
        public BindExtensionState extendState;
    }
}
