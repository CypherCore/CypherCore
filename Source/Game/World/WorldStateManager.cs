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

using Framework.Database;
using Game.Maps;
using Game.Networking.Packets;
using System.Collections.Generic;

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

            //                                         0   1             2      3
            SQLResult result = DB.World.Query("SELECT ID, DefaultValue, MapID, ScriptName FROM world_state");
            if (result.IsEmpty())
                return;

            do
            {
                int id = result.Read<int>(0);
                WorldStateTemplate worldState = new();
                worldState.Id = id;
                worldState.DefaultValue = result.Read<int>(1);
                if (!result.IsNull(2))
                    worldState.MapId = result.Read<uint>(2);

                worldState.ScriptId = Global.ObjectMgr.GetScriptId(result.Read<string>(3));

                if (worldState.MapId.HasValue)
                {
                    if (!_worldStatesByMap.ContainsKey(worldState.MapId.Value))
                        _worldStatesByMap[worldState.MapId.Value] = new();

                    _worldStatesByMap[worldState.MapId.Value][id] = worldState.DefaultValue;
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
            if (worldStateTemplate == null || !worldStateTemplate.MapId.HasValue)
                return _realmWorldStateValues.LookupByKey(worldStateId);

            if (map.GetId() != worldStateTemplate.MapId)
                return 0;

            return map.GetWorldStateValue(worldStateId);
        }

        public void SetValue(int worldStateId, int value, Map map)
        {
            WorldStateTemplate worldStateTemplate = GetWorldStateTemplate(worldStateId);
            if (worldStateTemplate == null || !worldStateTemplate.MapId.HasValue)
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

            if (map.GetId() != worldStateTemplate.MapId)
                return;

            map.SetWorldStateValue(worldStateId, value);
        }

        public Dictionary<int, int> GetInitialWorldStatesForMap(Map map)
        {
            if (_worldStatesByMap.TryGetValue(map.GetId(), out Dictionary<int, int> initialValues))
                return initialValues;

            return new Dictionary<int, int>();
        }

        public void FillInitialWorldStates(InitWorldStates initWorldStates, Map map)
        {
            foreach (var (worldStateId, value) in _realmWorldStateValues)
                initWorldStates.AddState(worldStateId, value);

            foreach (var (worldStateId, value) in map.GetWorldStateValues())
                initWorldStates.AddState(worldStateId, value);
        }
    }

    public class WorldStateTemplate
    {
        public int Id;
        public int DefaultValue;
        public uint ScriptId;

        public uint? MapId;
    }
}
