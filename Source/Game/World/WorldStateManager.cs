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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class WorldStateManager : Singleton<WorldStateManager>
    {
        Dictionary<int, WorldStateTemplate> _worldStateTemplates = new();
        Dictionary<int, int> _realmWorldStateValues = new();
        Dictionary<uint, Dictionary<int, int>> _worldStatesByMap = new();

        WorldStateManager() { }

        public void LoadFromDB()
        {
            uint oldMSTime = Time.GetMSTime();

            //                                         0   1             2       3        4
            SQLResult result = DB.World.Query("SELECT ID, DefaultValue, MapIDs, AreaIDs, ScriptName FROM world_state");
            if (result.IsEmpty())
                return;

            do
            {
                int id = result.Read<int>(0);
                WorldStateTemplate worldState = new();
                worldState.Id = id;
                worldState.DefaultValue = result.Read<int>(1);

                string mapIds = result.Read<string>(2);
                foreach (string mapIdToken in new StringArray(mapIds, ','))
                {
                    if (!uint.TryParse(mapIdToken, out uint mapId))
                    {
                        Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with non-integer MapID ({mapIdToken}), map ignored");
                        continue;
                    }

                    if (!CliDB.MapStorage.ContainsKey(mapId))
                    {
                        Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with invalid MapID ({mapId}), map ignored");
                        continue;
                    }

                    worldState.MapIds.Add(mapId);
                }

                if (!mapIds.IsEmpty() && worldState.MapIds.Empty())
                {
                    Log.outError(LogFilter.Sql, "Table `world_state` contains a world state {id} with nonempty MapIDs ({mapIds}) but no valid map id was found, ignored");
                    continue;
                }

                string areaIds = result.Read<string>(3);
                if (!areaIds.IsEmpty() && !worldState.MapIds.Empty())
                {
                    foreach (string areaIdToken in new StringArray(areaIds, ','))
                    {
                        if (!uint.TryParse(areaIdToken, out uint areaId))
                        {
                            Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with non-integer AreaID ({areaIdToken}), area ignored");
                            continue;
                        }

                        var areaTableEntry = CliDB.AreaTableStorage.LookupByKey(areaId);
                        if (areaTableEntry == null)
                        {
                            Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with invalid AreaID ({areaId}), area ignored");
                            continue;
                        }

                        if (!worldState.MapIds.Contains(areaTableEntry.ContinentID))
                        {
                            Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with AreaID ({areaId}) not on any of required maps, area ignored");
                            continue;
                        }

                        worldState.AreaIds.Add(areaId);
                    }

                    if (!areaIds.IsEmpty() && worldState.AreaIds.Empty())
                    {
                        Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with nonempty AreaIDs ({areaIds}) but no valid area id was found, ignored");
                        continue;
                    }
                }
                else if (!areaIds.IsEmpty())
                {
                    Log.outError(LogFilter.Sql, $"Table `world_state` contains a world state {id} with nonempty AreaIDs ({areaIds}) but is a realm wide world state, area requirement ignored");
                }

                worldState.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(4));

                if (!worldState.MapIds.Empty())
                {
                    foreach (uint mapId in worldState.MapIds)
                    {
                        if (!_worldStatesByMap.ContainsKey(mapId))
                            _worldStatesByMap[mapId] = new();

                        _worldStatesByMap[mapId][id] = worldState.DefaultValue;
                    }
                }
                else
                    _realmWorldStateValues[id] = worldState.DefaultValue;

                _worldStateTemplates[id] = worldState;

            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_worldStateTemplates.Count} world state templates {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public WorldStateTemplate GetWorldStateTemplate(int worldStateId)
        {
            return _worldStateTemplates.LookupByKey(worldStateId);
        }

        public int GetValue(int worldStateId, Map map)
        {
            WorldStateTemplate worldStateTemplate = GetWorldStateTemplate(worldStateId);
            if (worldStateTemplate == null || worldStateTemplate.MapIds.Empty())
                return _realmWorldStateValues.LookupByKey(worldStateId);

            if (!worldStateTemplate.MapIds.Contains(map.GetId()))
                return 0;

            return map.GetWorldStateValue(worldStateId);
        }

        public void SetValue(WorldStates worldStateId, int value, Map map)
        {
            SetValue((int)worldStateId, value, map);
        }

        public void SetValue(uint worldStateId, int value, Map map)
        {
            SetValue((int)worldStateId, value, map);
        }

        public void SetValue(int worldStateId, int value, Map map)
        {
            WorldStateTemplate worldStateTemplate = GetWorldStateTemplate(worldStateId);
            if (worldStateTemplate == null || worldStateTemplate.MapIds.Empty())
            {
                int oldValue = _realmWorldStateValues.LookupByKey(worldStateId);
                _realmWorldStateValues[worldStateId] = value;

                if (worldStateTemplate != null)
                    Global.ScriptMgr.OnWorldStateValueChange(worldStateTemplate, oldValue, value, null);

                // Broadcast update to all players on the server
                UpdateWorldState updateWorldState = new();
                updateWorldState.VariableID = (uint)worldStateId;
                updateWorldState.Value = value;
                Global.WorldMgr.SendGlobalMessage(updateWorldState);
                return;
            }

            if (!worldStateTemplate.MapIds.Contains(map.GetId()))
                return;

            map.SetWorldStateValue(worldStateId, value);
        }

        public Dictionary<int, int> GetInitialWorldStatesForMap(Map map)
        {
            if (_worldStatesByMap.TryGetValue(map.GetId(), out Dictionary<int, int> initialValues))
                return initialValues;

            return new Dictionary<int, int>();
        }

        public void FillInitialWorldStates(InitWorldStates initWorldStates, Map map, uint playerAreaId)
        {
            foreach (var (worldStateId, value) in _realmWorldStateValues)
                initWorldStates.AddState(worldStateId, value);

            foreach (var (worldStateId, value) in map.GetWorldStateValues())
            {
                WorldStateTemplate worldStateTemplate = GetWorldStateTemplate(worldStateId);
                if (worldStateTemplate != null && !worldStateTemplate.AreaIds.Empty())
                {
                    bool isInAllowedArea = worldStateTemplate.AreaIds.Any(requiredAreaId => Global.DB2Mgr.IsInArea(playerAreaId, requiredAreaId));
                    if (!isInAllowedArea)
                        continue;
                }

                initWorldStates.AddState(worldStateId, value);
            }
        }
    }

    public class WorldStateTemplate
    {
        public int Id;
        public int DefaultValue;
        public uint ScriptId;

        public List<uint> MapIds = new();
        public List<uint> AreaIds = new();
    }
}
