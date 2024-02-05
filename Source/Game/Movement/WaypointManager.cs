﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Database;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public sealed class WaypointManager : Singleton<WaypointManager>
    {
        WaypointManager() { }

        public void LoadPaths()
        {
            _LoadPaths();
            _LoadPathNodes();
            DoPostLoadingChecks();
        }

        void _LoadPaths()
        {
            _pathStorage.Clear();

            var oldMSTime = Time.GetMSTime();

            //                                          0       1         2
            SQLResult result = DB.World.Query("SELECT  PathId, MoveType, Flags FROM waypoint_path");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 waypoint paths. DB table `waypoint_path` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                LoadPathFromDB(result.GetFields());
                ++count;
            } while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} waypoint paths in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        void _LoadPathNodes()
        {
            uint oldMSTime = Time.GetMSTime();

            //                                         0       1          2          3          4            5      6
            SQLResult result = DB.World.Query("SELECT PathId, NodeId, PositionX, PositionY, PositionZ, Orientation, Delay FROM waypoint_path_node ORDER BY PathId, NodeId");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 waypoint path nodes. DB table `waypoint_path_node` is empty!");
                return;
            }

            uint count = 0;

            do
            {
                LoadPathNodesFromDB(result.GetFields());
                ++count;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} waypoint path nodes in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
            DoPostLoadingChecks();
        }

        void LoadPathFromDB(SQLFields fields)
        {
            uint pathId = fields.Read<uint>(0);

            WaypointPath path = new();
            path.Id = pathId;
            path.MoveType = (WaypointMoveType)fields.Read<byte>(1);

            if (path.MoveType >= WaypointMoveType.Max)
            {
                Log.outError(LogFilter.Sql, $"PathId {pathId} in `waypoint_path` has invalid MoveType {path.MoveType}, ignoring");
                return;
            }
            path.Flags = (WaypointPathFlags)fields.Read<byte>(2);
            path.Nodes.Clear();

            _pathStorage.Add(pathId, path);
        }

        void LoadPathNodesFromDB(SQLFields fields)
        {
            uint pathId = fields.Read<uint>(0);

            if (!_pathStorage.ContainsKey(pathId))
            {
                Log.outError(LogFilter.Sql, $"PathId {pathId} in `waypoint_path_node` does not exist in `waypoint_path`, ignoring");
                return;
            }

            float x = fields.Read<float>(2);
            float y = fields.Read<float>(3);
            float z = fields.Read<float>(4);
            float? o = null;
            if (!fields.IsNull(5))
                o = fields.Read<float>(5);

            GridDefines.NormalizeMapCoord(ref x);
            GridDefines.NormalizeMapCoord(ref y);

            WaypointNode waypoint = new(fields.Read<uint>(1), x, y, z, o, fields.Read<uint>(6));
            _pathStorage[pathId].Nodes.Add(waypoint);
        }

        void DoPostLoadingChecks()
        {
            foreach (var path in _pathStorage)
            {
                WaypointPath pathInfo = path.Value;
                if (pathInfo.Nodes.Empty())
                    Log.outError(LogFilter.Sql, $"PathId {pathInfo.Id} in `waypoint_path` has no assigned nodes in `waypoint_path_node`");

                if (pathInfo.Flags.HasFlag(WaypointPathFlags.FollowPathBackwardsFromEndToStart) && pathInfo.Nodes.Count < 2)
                    Log.outError(LogFilter.Sql, $"PathId {pathInfo.Id} in `waypoint_path` has FollowPathBackwardsFromEndToStart set, but only {pathInfo.Nodes.Count} nodes, requires {2}");
            }
        }

        public void ReloadPath(uint pathId)
        {
            // waypoint_path
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_PATH_BY_PATHID);
            stmt.AddValue(0, pathId);
            SQLResult result = DB.World.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Sql, $"PathId {pathId} in `waypoint_path` not found, ignoring");
                return;
            }

            do
            {
                LoadPathFromDB(result.GetFields());
            } while (result.NextRow());

            // waypoint_path_data
            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_WAYPOINT_PATH_NODE_BY_PATHID);
            stmt.AddValue(0, pathId);

            result = DB.World.Query(stmt);
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Sql, $"PathId {pathId} in `waypoint_path_node` not found, ignoring");
                return;
            }

            do
            {
                LoadPathNodesFromDB(result.GetFields());
            } while (result.NextRow());
        }

        public void VisualizePath(Unit owner, WaypointPath path, uint? displayId)
        {
            foreach (WaypointNode node in path.Nodes)
            {
                var pathNodePair = (path.Id, node.Id);
                if (!_nodeToVisualWaypointGUIDsMap.ContainsKey(pathNodePair))
                    continue;

                TempSummon summon = owner.SummonCreature(1, node.X, node.Y, node.Z, node.Orientation.HasValue ? node.Orientation.Value : 0.0f);
                if (summon == null)
                    continue;

                if (displayId.HasValue)
                {
                    summon.SetDisplayId(displayId.Value, true);
                    summon.SetObjectScale(0.5f);
                }
                _nodeToVisualWaypointGUIDsMap[pathNodePair] = summon.GetGUID();
                _visualWaypointGUIDToNodeMap[summon.GetGUID()] = (path, node);
            }
        }

        public void DevisualizePath(Unit owner, WaypointPath path)
        {
            foreach (WaypointNode node in path.Nodes)
            {
                var pathNodePair = (path.Id, node.Id);
                if (!_nodeToVisualWaypointGUIDsMap.TryGetValue(pathNodePair, out ObjectGuid guid))
                    continue;

                Creature creature = ObjectAccessor.GetCreature(owner, guid);
                if (creature == null)
                    continue;

                _visualWaypointGUIDToNodeMap.Remove(guid);
                _nodeToVisualWaypointGUIDsMap.Remove(pathNodePair);

                creature.DespawnOrUnsummon();
            }
        }

        public void MoveNode(WaypointPath path, WaypointNode node, Position pos)
        {
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_WAYPOINT_PATH_NODE_POSITION);
            stmt.AddValue(0, pos.GetPositionX());
            stmt.AddValue(1, pos.GetPositionY());
            stmt.AddValue(2, pos.GetPositionZ());
            stmt.AddValue(3, pos.GetOrientation());
            stmt.AddValue(4, path.Id);
            stmt.AddValue(5, node.Id);
            DB.World.Execute(stmt);
        }

        public void DeleteNode(WaypointPath path, WaypointNode node)
        {
            PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_WAYPOINT_PATH_NODE);
            stmt.AddValue(0, path.Id);
            stmt.AddValue(1, node.Id);
            DB.World.Execute(stmt);

            stmt = WorldDatabase.GetPreparedStatement(WorldStatements.UPD_WAYPOINT_PATH_NODE);
            stmt.AddValue(0, path.Id);
            stmt.AddValue(1, node.Id);
            DB.World.Execute(stmt);
        }

        void DeleteNode(uint pathId, uint nodeId)
        {
            WaypointPath path = GetPath(pathId);
            if (path == null)
                return;

            WaypointNode node = GetNode(path, nodeId);
            if (node == null)
                return;

            DeleteNode(path, node);
        }

        public WaypointPath GetPath(uint pathId)
        {
            return _pathStorage.LookupByKey(pathId);
        }

        public WaypointNode GetNode(WaypointPath path, uint nodeId)
        {
            return path.Nodes.FirstOrDefault(node => node.Id == nodeId); ;
        }

        public WaypointNode GetNode(uint pathId, uint nodeId)
        {
            WaypointPath path = GetPath(pathId);
            if (path == null)
                return null;

            return GetNode(path.Id, nodeId);
        }

        public WaypointPath GetPathByVisualGUID(ObjectGuid guid)
        {
            if (!_visualWaypointGUIDToNodeMap.TryGetValue(guid, out var pair))
                return null;

            return pair.Item1;
        }

        public WaypointNode GetNodeByVisualGUID(ObjectGuid guid)
        {
            if (!_visualWaypointGUIDToNodeMap.TryGetValue(guid, out var pair))
                return null;

            return pair.Item2;
        }

        public ObjectGuid GetVisualGUIDByNode(uint pathId, uint nodeId)
        {
            if (!_nodeToVisualWaypointGUIDsMap.TryGetValue((pathId, nodeId), out var guid))
                return ObjectGuid.Empty;

            return guid;
        }

        Dictionary<uint /*pathId*/, WaypointPath> _pathStorage = new();

        Dictionary<(uint /*pathId*/, uint /*nodeId*/), ObjectGuid> _nodeToVisualWaypointGUIDsMap = new();
        Dictionary<ObjectGuid, (WaypointPath, WaypointNode)> _visualWaypointGUIDToNodeMap = new();
    }

    public class WaypointNode
    {
        public WaypointNode() { MoveType = WaypointMoveType.Run; }
        public WaypointNode(uint id, float x, float y, float z, float? orientation = null, uint delay = 0)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
            Orientation = orientation;
            Delay = delay;
            MoveType = WaypointMoveType.Walk;
        }

        public uint Id;
        public float X;
        public float Y;
        public float Z;
        public float? Orientation;
        public uint Delay;
        public WaypointMoveType MoveType;
    }

    public class WaypointPath
    {
        public WaypointPath() { }
        public WaypointPath(uint id, List<WaypointNode> nodes)
        {
            Id = id;
            Nodes = nodes;
        }

        public List<WaypointNode> Nodes = new();
        public uint Id;
        public WaypointMoveType MoveType;
        public WaypointPathFlags Flags = WaypointPathFlags.None;
    }

    public enum WaypointMoveType
    {
        Walk = 0,
        Run = 1,
        Land = 2,
        Takeoff = 3,

        Max
    }

    [Flags]
    public enum WaypointPathFlags
    {
        None = 0x00,
        FollowPathBackwardsFromEndToStart = 0x01,
    }
}
