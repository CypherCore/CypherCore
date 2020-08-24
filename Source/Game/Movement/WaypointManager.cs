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
using System.Collections.Generic;

namespace Game
{
    public sealed class WaypointManager : Singleton<WaypointManager>
    {
        WaypointManager() { }

        public void Load()
        {
            var oldMSTime = Time.GetMSTime();

            //                                          0    1         2           3          4            5           6        7      8           9
            SQLResult result = DB.World.Query("SELECT id, point, position_x, position_y, position_z, orientation, move_type, delay, action, action_chance FROM waypoint_data ORDER BY id, point");

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 waypoints. DB table `waypoint_data` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                uint pathId = result.Read<uint>(0);

                float x = result.Read<float>(2);
                float y = result.Read<float>(3);
                float z = result.Read<float>(4);
                float o = result.Read<float>(5);

                GridDefines.NormalizeMapCoord(ref x);
                GridDefines.NormalizeMapCoord(ref y);

                WaypointNode waypoint = new WaypointNode();
                waypoint.id = result.Read<uint>(1);
                waypoint.x = x;
                waypoint.y = y;
                waypoint.z = z;
                waypoint.orientation = o;
                waypoint.moveType = (WaypointMoveType)result.Read<uint>(6);

                if (waypoint.moveType >= WaypointMoveType.Max)
                {
                    Log.outError(LogFilter.Sql, $"Waypoint {waypoint.id} in waypoint_data has invalid move_type, ignoring");
                    continue;
                }

                waypoint.delay = result.Read<uint>(7);
                waypoint.eventId = result.Read<uint>(8);
                waypoint.eventChance = result.Read<byte>(9);

                if (!_waypointStore.ContainsKey(pathId))
                    _waypointStore[pathId] = new WaypointPath();

                WaypointPath path = _waypointStore[pathId];
                path.id = pathId;
                path.nodes.Add(waypoint);

                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} waypoints in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void ReloadPath(uint id)
        {
            _waypointStore.Remove(id);

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_ID);
            stmt.AddValue(0, id);
            SQLResult result = DB.World.Query(stmt);

            if (result.IsEmpty())
                return;

            List<WaypointNode> values = new List<WaypointNode>();
            do
            {
                float x = result.Read<float>(1);
                float y = result.Read<float>(2);
                float z = result.Read<float>(3);
                float o = result.Read<float>(4);

                GridDefines.NormalizeMapCoord(ref x);
                GridDefines.NormalizeMapCoord(ref y);

                WaypointNode waypoint = new WaypointNode();
                waypoint.id = result.Read<uint>(0);
                waypoint.x = x;
                waypoint.y = y;
                waypoint.z = z;
                waypoint.orientation = o;
                waypoint.moveType = (WaypointMoveType)result.Read<uint>(5);

                if (waypoint.moveType >= WaypointMoveType.Max)
                {
                    Log.outError(LogFilter.Sql, $"Waypoint {waypoint.id} in waypoint_data has invalid move_type, ignoring");
                    continue;
                }

                waypoint.delay = result.Read<uint>(6);
                waypoint.eventId = result.Read<uint>(7);
                waypoint.eventChance = result.Read<byte>(8);

                values.Add(waypoint);
            }
            while (result.NextRow());

            _waypointStore[id] = new WaypointPath(id, values);
        }

        public WaypointPath GetPath(uint id)
        {
            return _waypointStore.LookupByKey(id);
        }

        Dictionary<uint, WaypointPath> _waypointStore = new Dictionary<uint, WaypointPath>();
    }

    public class WaypointNode
    {
        public WaypointNode() { moveType = WaypointMoveType.Run; }
        public WaypointNode(uint _id, float _x, float _y, float _z, float _orientation = 0.0f, uint _delay = 0)
        {
            id = _id;
            x = _x;
            y = _y;
            z = _z;
            orientation = _orientation;
            delay = _delay;
            eventId = 0;
            moveType = WaypointMoveType.Walk;
            eventChance = 100;
        }

        public uint id;
        public float x, y, z, orientation;
        public uint delay;
        public uint eventId;
        public WaypointMoveType moveType;
        public byte eventChance;
    }

    public class WaypointPath
    {
        public WaypointPath() { }
        public WaypointPath(uint _id, List<WaypointNode> _nodes)
        {
            id = _id;
            nodes = _nodes;
        }

        public List<WaypointNode> nodes = new List<WaypointNode>();
        public uint id;
    }

    public enum WaypointMoveType
    {
        Walk,
        Run,
        Land,
        Takeoff,

        Max
    }
}
