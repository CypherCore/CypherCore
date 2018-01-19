/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
        WaypointManager()
        {
            _waypointStore = new MultiMap<uint, WaypointData>();
        }

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
                WaypointData wp = new WaypointData();

                uint pathId = result.Read<uint>(0);

                float x = result.Read<float>(2);
                float y = result.Read<float>(3);
                float z = result.Read<float>(4);
                float o = result.Read<float>(5);

                GridDefines.NormalizeMapCoord(ref x);
                GridDefines.NormalizeMapCoord(ref y);

                wp.id = result.Read<uint>(1);
                wp.x = x;
                wp.y = y;
                wp.z = z;
                wp.orientation = o;
                wp.movetype = (WaypointMoveType)result.Read<uint>(6);
                if (wp.movetype >= WaypointMoveType.Max)
                {
                    Log.outError(LogFilter.Sql, "Waypoint {0} in waypoint_data has invalid move_type, ignoring", wp.id);
                    continue;
                }

                wp.delay = result.Read<uint>(7);
                wp.event_id = result.Read<uint>(8);
                wp.event_chance = result.Read<byte>(9);

                _waypointStore.Add(pathId, wp);
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} waypoints in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void ReloadPath(uint id)
        {
            if (_waypointStore.ContainsKey(id))
                _waypointStore.Remove(id);

            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_DATA_BY_ID);
            stmt.AddValue(0, id);
            SQLResult result = DB.World.Query(stmt);

            if (result.IsEmpty())
                return;

            do
            {
                WaypointData wp = new WaypointData();

                float x = result.Read<float>(1);
                float y = result.Read<float>(2);
                float z = result.Read<float>(3);
                float o = result.Read<float>(4);

                GridDefines.NormalizeMapCoord(ref x);
                GridDefines.NormalizeMapCoord(ref y);

                wp.id = result.Read<uint>(0);
                wp.x = x;
                wp.y = y;
                wp.z = z;
                wp.orientation = o;
                wp.movetype = (WaypointMoveType)result.Read<uint>(5);

                if (wp.movetype >= WaypointMoveType.Max)
                {
                    Log.outError(LogFilter.Sql, "Waypoint {0} in waypoint_data has invalid move_type, ignoring", wp.id);
                    continue;
                }

                wp.delay = result.Read<uint>(6);
                wp.event_id = result.Read<uint>(7);
                wp.event_chance = result.Read<byte>(8);

                _waypointStore.Add(id, wp);

            }
            while (result.NextRow());
        }

        public List<WaypointData> GetPath(uint id)
        {
            return _waypointStore.LookupByKey(id);
        }

        MultiMap<uint, WaypointData> _waypointStore;
    }

    public struct WaypointData
    {
        public uint id;
        public float x, y, z, orientation;
        public uint delay;
        public uint event_id;
        public WaypointMoveType movetype;
        public byte event_chance;
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
