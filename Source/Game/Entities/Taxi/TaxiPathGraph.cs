// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Algorithms;
using Framework.Collections;
using Framework.Constants;
using Game.DataStorage;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Entities
{
    public class TaxiPathGraph
    {
        static EdgeWeightedDigraph m_graph;
        static List<TaxiNodesRecord> m_nodesByVertex = new();
        static Dictionary<uint, uint> m_verticesByNode = new();

        static void GetTaxiMapPosition(Vector3 position, int mapId, out Vector2 uiMapPosition, out uint uiMapId)
        {
            if (!Global.DB2Mgr.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Adventure, false, out uiMapId, out uiMapPosition))
                Global.DB2Mgr.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId, out uiMapPosition);
        }

        static uint CreateVertexFromFromNodeInfoIfNeeded(TaxiNodesRecord node)
        {
            if (!m_verticesByNode.ContainsKey(node.Id))
            {
                m_verticesByNode.Add(node.Id, (uint)m_nodesByVertex.Count);
                m_nodesByVertex.Add(node);
            }

            return m_verticesByNode[node.Id];
        }

        static void AddVerticeAndEdgeFromNodeInfo(TaxiNodesRecord from, TaxiNodesRecord to, uint pathId, List<Tuple<Tuple<uint, uint>, uint>> edges)
        {
            if (from.Id != to.Id)
            {
                uint fromVertexID = CreateVertexFromFromNodeInfoIfNeeded(from);
                uint toVertexID = CreateVertexFromFromNodeInfoIfNeeded(to);

                float totalDist = 0.0f;
                TaxiPathNodeRecord[] nodes = DB2Manager.TaxiPathNodesByPath[pathId];
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
                    if (nodes[i - 1].HasFlag(TaxiPathNodeFlags.Teleport))
                        continue;

                    uint uiMap1, uiMap2;
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

        static uint GetVertexIDFromNodeID(TaxiNodesRecord node)
        {
            return m_verticesByNode.LookupByKey(node.Id);
        }

        static uint GetNodeIDFromVertexID(uint vertexID)
        {
            if (vertexID < m_nodesByVertex.Count)
                return m_nodesByVertex[(int)vertexID].Id;

            return uint.MaxValue;
        }

        public static void Initialize()
        {
            if (m_graph != null)
                return;

            List<Tuple<Tuple<uint, uint>, uint>> edges = new();

            // Initialize here
            foreach (TaxiPathRecord path in CliDB.TaxiPathStorage.Values)
            {
                TaxiNodesRecord from = CliDB.TaxiNodesStorage.LookupByKey(path.FromTaxiNode);
                TaxiNodesRecord to = CliDB.TaxiNodesStorage.LookupByKey(path.ToTaxiNode);
                if (from != null && to != null && from.IsPartOfTaxiNetwork() && to.IsPartOfTaxiNetwork())
                    AddVerticeAndEdgeFromNodeInfo(from, to, path.Id, edges);
            }

            // create graph
            m_graph = new EdgeWeightedDigraph(m_nodesByVertex.Count);

            for (int j = 0; j < edges.Count; ++j)
            {
                m_graph.AddEdge(new DirectedEdge(edges[j].Item1.Item1, edges[j].Item1.Item2, edges[j].Item2));
            }
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

                uint fromVertexId = GetVertexIDFromNodeID(from);
                uint toVertexId = GetVertexIDFromNodeID(to);
                if (fromVertexId != 0 && toVertexId != 0)
                {
                    // We want to use Dijkstra on this graph
                    DijkstraShortestPath dijkstra = new(m_graph, (int)fromVertexId);
                    var path = dijkstra.PathTo((int)toVertexId);
                    // found a path to the goal
                    shortestPath.Add(from.Id);
                    foreach (var edge in path)
                    {
                        var To = m_nodesByVertex[(int)edge.To];
                        bool isVisibleForFaction = player.GetTeam() switch
                        {
                            Team.Horde => To.HasFlag(TaxiNodeFlags.ShowOnHordeMap),
                            Team.Alliance => To.HasFlag(TaxiNodeFlags.ShowOnAllianceMap),
                            _ => false
                        };

                        if (!isVisibleForFaction)
                            continue;

                        if (!ConditionManager.IsPlayerMeetingCondition(player, To.ConditionID))
                            continue;

                        shortestPath.Add(GetNodeIDFromVertexID(edge.To));
                    }
                }
            }

            return shortestPath.Count;
        }

        //todo test me
        public static void GetReachableNodesMask(TaxiNodesRecord from, byte[] mask)
        {
            uint vertexId = GetVertexIDFromNodeID(from);
            if (vertexId == 0)
                return;

            DepthFirstSearch depthFirst = new(m_graph, vertexId, vertex =>
            {
                TaxiNodesRecord taxiNode = CliDB.TaxiNodesStorage.LookupByKey(GetNodeIDFromVertexID(vertex));
                if (taxiNode != null)
                    mask[(taxiNode.Id - 1) / 8] |= (byte)(1 << (int)((taxiNode.Id - 1) % 8));
            });
        }
    }
}

