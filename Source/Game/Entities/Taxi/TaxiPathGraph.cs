// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Algorithms;
using Framework.Collections;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities
{
    public class TaxiPathGraph
    {
        private static EdgeWeightedDigraph _graph;
        private static readonly List<TaxiNodesRecord> _nodesByVertex = new();
        private static readonly Dictionary<uint, uint> _verticesByNode = new();

        public static void Initialize()
        {
            if (_graph != null)
                return;

            List<Tuple<Tuple<uint, uint>, uint>> edges = new();

            // Initialize here
            foreach (TaxiPathRecord path in CliDB.TaxiPathStorage.Values)
            {
                TaxiNodesRecord from = CliDB.TaxiNodesStorage.LookupByKey(path.FromTaxiNode);
                TaxiNodesRecord to = CliDB.TaxiNodesStorage.LookupByKey(path.ToTaxiNode);

                if (from != null &&
                    to != null &&
                    from.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde) &&
                    to.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde))
                    AddVerticeAndEdgeFromNodeInfo(from, to, path.Id, edges);
            }

            // create graph
            _graph = new EdgeWeightedDigraph(_nodesByVertex.Count);

            for (int j = 0; j < edges.Count; ++j)
                _graph.AddEdge(new DirectedEdge(edges[j].Item1.Item1, edges[j].Item1.Item2, edges[j].Item2));
        }

        public static int GetCompleteNodeRoute(TaxiNodesRecord from, TaxiNodesRecord to, Player player, List<uint> shortestPath)
        {
            /*
			    Information about node algorithm from client
			    Since client does not give information about *ALL* nodes you have to pass by when going from sourceNodeID to destinationNodeID, we need to use Dijkstra algorithm.
			    Examining several paths I discovered the following algorithm:
			    * If destinationNodeID has is the next destination, connected directly to sourceNodeID, then, client just pick up this route regardless of distance
			    * else we use dijkstra to find the shortest path.
			    * When early landing is requested, according to behavior on retail, you can never end in a node you did not discovered before
			*/

            // Find if we have a direct path
            Global.ObjectMgr.GetTaxiPath(from.Id, to.Id, out uint pathId, out _);

            if (pathId != 0)
            {
                shortestPath.Add(from.Id);
                shortestPath.Add(to.Id);
            }
            else
            {
                shortestPath.Clear();
                // We want to use Dijkstra on this graph
                DijkstraShortestPath g = new(_graph, (int)GetVertexIDFromNodeID(from));
                var path = g.PathTo((int)GetVertexIDFromNodeID(to));
                // found a path to the goal
                shortestPath.Add(from.Id);

                foreach (var edge in path)
                {
                    //todo  test me No clue about this....
                    var To = _nodesByVertex[(int)edge.To];
                    TaxiNodeFlags requireFlag = (player.GetTeam() == Team.Alliance) ? TaxiNodeFlags.Alliance : TaxiNodeFlags.Horde;

                    if (!To.Flags.HasAnyFlag(requireFlag))
                        continue;

                    PlayerConditionRecord condition = CliDB.PlayerConditionStorage.LookupByKey(To.ConditionID);

                    if (condition != null)
                        if (!ConditionManager.IsPlayerMeetingCondition(player, condition))
                            continue;

                    shortestPath.Add(GetNodeIDFromVertexID(edge.To));
                }
            }

            return shortestPath.Count;
        }

        //todo test me
        public static void GetReachableNodesMask(TaxiNodesRecord from, byte[] mask)
        {
            DepthFirstSearch depthFirst = new(_graph,
                                              GetVertexIDFromNodeID(from),
                                              vertex =>
                                              {
                                                  TaxiNodesRecord taxiNode = CliDB.TaxiNodesStorage.LookupByKey(GetNodeIDFromVertexID(vertex));

                                                  if (taxiNode != null)
                                                      mask[(taxiNode.Id - 1) / 8] |= (byte)(1 << (int)((taxiNode.Id - 1) % 8));
                                              });
        }

        private static void GetTaxiMapPosition(Vector3 position, int mapId, out Vector2 uiMapPosition, out int uiMapId)
        {
            if (!Global.DB2Mgr.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Adventure, false, out uiMapId, out uiMapPosition))
                Global.DB2Mgr.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId, out uiMapPosition);
        }

        private static uint CreateVertexFromFromNodeInfoIfNeeded(TaxiNodesRecord node)
        {
            if (!_verticesByNode.ContainsKey(node.Id))
            {
                _verticesByNode.Add(node.Id, (uint)_nodesByVertex.Count);
                _nodesByVertex.Add(node);
            }

            return _verticesByNode[node.Id];
        }

        private static void AddVerticeAndEdgeFromNodeInfo(TaxiNodesRecord from, TaxiNodesRecord to, uint pathId, List<Tuple<Tuple<uint, uint>, uint>> edges)
        {
            if (from.Id != to.Id)
            {
                uint fromVertexID = CreateVertexFromFromNodeInfoIfNeeded(from);
                uint toVertexID = CreateVertexFromFromNodeInfoIfNeeded(to);

                float totalDist = 0.0f;
                TaxiPathNodeRecord[] nodes = CliDB.TaxiPathNodesByPath[pathId];

                if (nodes.Length < 2)
                {
                    edges.Add(Tuple.Create(Tuple.Create(fromVertexID, toVertexID), 0xFFFFu));

                    return;
                }

                int last = nodes.Length;
                int first = 0;

                if (nodes.Length > 2)
                {
                    --last;
                    ++first;
                }

                for (int i = first + 1; i < last; ++i)
                {
                    if (nodes[i - 1].Flags.HasAnyFlag(TaxiPathNodeFlags.Teleport))
                        continue;

                    int uiMap1, uiMap2;
                    Vector2 pos1, pos2;

                    GetTaxiMapPosition(nodes[i - 1].Loc, nodes[i - 1].ContinentID, out pos1, out uiMap1);
                    GetTaxiMapPosition(nodes[i].Loc, nodes[i].ContinentID, out pos2, out uiMap2);

                    if (uiMap1 != uiMap2)
                        continue;

                    totalDist += (float)Math.Sqrt((float)Math.Pow(pos2.X - pos1.X, 2) + (float)Math.Pow(pos2.Y - pos1.Y, 2));
                }

                uint dist = (uint)(totalDist * 32767.0f);

                if (dist > 0xFFFF)
                    dist = 0xFFFF;

                edges.Add(Tuple.Create(Tuple.Create(fromVertexID, toVertexID), dist));
            }
        }

        private static uint GetVertexIDFromNodeID(TaxiNodesRecord node)
        {
            return _verticesByNode.ContainsKey(node.Id) ? _verticesByNode[node.Id] : uint.MaxValue;
        }

        private static uint GetNodeIDFromVertexID(uint vertexID)
        {
            if (vertexID < _nodesByVertex.Count)
                return _nodesByVertex[(int)vertexID].Id;

            return uint.MaxValue;
        }
    }
}