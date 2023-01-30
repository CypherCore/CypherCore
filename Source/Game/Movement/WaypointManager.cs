// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Database;
using Game.Maps;

namespace Game
{
    public sealed class WaypointManager : Singleton<WaypointManager>
    {
        private readonly Dictionary<uint, WaypointPath> _waypointStore = new();

        private WaypointManager()
        {
        }

        public void Load()
        {
            var oldMSTime = Time.GetMSTime();

            //                                          0    1         2           3          4            5           6        7      8           9
            SQLResult result = DB.World.Query("SELECT Id, point, position_x, position_y, position_z, orientation, move_type, delay, Action, action_chance FROM waypoint_data ORDER BY Id, point");

            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 waypoints. DB table `waypoint_data` is empty!");

                return;
            }

            uint count = 0;

            do
            {
                uint pathId = result.Read<uint>(0);

                float x = result.Read<float>(2);
                float y = result.Read<float>(3);
                float z = result.Read<float>(4);
                float? o = null;

                if (!result.IsNull(5))
                    o = result.Read<float>(5);

                x = GridDefines.NormalizeMapCoord(x);
                y = GridDefines.NormalizeMapCoord(y);

                WaypointNode waypoint = new();
                waypoint.Id = result.Read<uint>(1);
                waypoint.X = x;
                waypoint.Y = y;
                waypoint.Z = z;
                waypoint.Orientation = o;
                waypoint.MoveType = (WaypointMoveType)result.Read<uint>(6);

                if (waypoint.MoveType >= WaypointMoveType.Max)
                {
                    Log.outError(LogFilter.Sql, $"Waypoint {waypoint.Id} in waypoint_data has invalid move_type, ignoring");

                    continue;
                }

                waypoint.Delay = result.Read<uint>(7);
                waypoint.EventId = result.Read<uint>(8);
                waypoint.EventChance = result.Read<byte>(9);

                if (!_waypointStore.ContainsKey(pathId))
                    _waypointStore[pathId] = new WaypointPath();

                WaypointPath path = _waypointStore[pathId];
                path.Id = pathId;
                path.Nodes.Add(waypoint);

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

            List<WaypointNode> values = new();

            do
            {
                float x = result.Read<float>(1);
                float y = result.Read<float>(2);
                float z = result.Read<float>(3);
                float? o = null;

                if (!result.IsNull(4))
                    o = result.Read<float>(4);

                x = GridDefines.NormalizeMapCoord(x);
                y = GridDefines.NormalizeMapCoord(y);

                WaypointNode waypoint = new();
                waypoint.Id = result.Read<uint>(0);
                waypoint.X = x;
                waypoint.Y = y;
                waypoint.Z = z;
                waypoint.Orientation = o;
                waypoint.MoveType = (WaypointMoveType)result.Read<uint>(5);

                if (waypoint.MoveType >= WaypointMoveType.Max)
                {
                    Log.outError(LogFilter.Sql, $"Waypoint {waypoint.Id} in waypoint_data has invalid move_type, ignoring");

                    continue;
                }

                waypoint.Delay = result.Read<uint>(6);
                waypoint.EventId = result.Read<uint>(7);
                waypoint.EventChance = result.Read<byte>(8);

                values.Add(waypoint);
            } while (result.NextRow());

            _waypointStore[id] = new WaypointPath(id, values);
        }

        public WaypointPath GetPath(uint id)
        {
            return _waypointStore.LookupByKey(id);
        }
    }
}