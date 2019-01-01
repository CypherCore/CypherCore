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

using Framework.Algorithms;
using Framework.Collections;
using Framework.Constants;
using Framework.GameMath;
using Game.DataStorage;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class TaxiPathGraph : Singleton<TaxiPathGraph>
    {
        EdgeWeightedDigraph m_graph;
        List<TaxiNodesRecord> m_nodesByVertex = new List<TaxiNodesRecord>();
        Dictionary<uint, uint> m_verticesByNode = new Dictionary<uint, uint>();

        TaxiPathGraph() { }

        public void Initialize()
        {
            if (m_graph != null)
                return;

            List<Tuple<Tuple<uint, uint>, uint>> edges = new List<Tuple<Tuple<uint, uint>, uint>>();

            // Initialize here
            foreach (TaxiPathRecord path in CliDB.TaxiPathStorage.Values)
            {
                TaxiNodesRecord from = CliDB.TaxiNodesStorage.LookupByKey(path.FromTaxiNode);
                TaxiNodesRecord to = CliDB.TaxiNodesStorage.LookupByKey(path.ToTaxiNode);
                if (from != null && to != null && from.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde) && to.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde))
                    AddVerticeAndEdgeFromNodeInfo(from, to, path.Id, edges);
            }

            // create graph
            m_graph = new EdgeWeightedDigraph(m_nodesByVertex.Count);

            for (int j = 0; j < edges.Count; ++j)
            {
                m_graph.AddEdge(new DirectedEdge(edges[j].Item1.Item1, edges[j].Item1.Item2, edges[j].Item2));
            }
        }

        uint GetNodeIDFromVertexID(uint vertexID)
        {
            if (vertexID < m_nodesByVertex.Count)
                return m_nodesByVertex[(int)vertexID].Id;

            return uint.MaxValue;
        }

        uint GetVertexIDFromNodeID(TaxiNodesRecord node)
        {
            return m_verticesByNode.ContainsKey(node.Id) ? m_verticesByNode[node.Id] : uint.MaxValue;
        }

        void GetTaxiMapPosition(Vector3 position, int mapId, out Vector2 uiMapPosition, out int uiMapId)
        {
            if (!Global.DB2Mgr.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Adventure, false, out uiMapId, out uiMapPosition))
                Global.DB2Mgr.GetUiMapPosition(position.X, position.Y, position.Z, mapId, 0, 0, 0, UiMapSystem.Taxi, false, out uiMapId, out uiMapPosition);
        }

        void AddVerticeAndEdgeFromNodeInfo(TaxiNodesRecord from, TaxiNodesRecord to, uint pathId, List<Tuple<Tuple<uint, uint>, uint>> edges)
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

        public int GetCompleteNodeRoute(TaxiNodesRecord from, TaxiNodesRecord to, Player player, List<uint> shortestPath)
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
            uint pathId, goldCost;
            Global.ObjectMgr.GetTaxiPath(from.Id, to.Id, out pathId, out goldCost);
            if (pathId != 0)
            {
                shortestPath.Add(from.Id);
                shortestPath.Add(to.Id);
            }
            else
            {
                shortestPath.Clear();
                // We want to use Dijkstra on this graph
                DijkstraShortestPath g = new DijkstraShortestPath(m_graph, (int)GetVertexIDFromNodeID(from));
                var path = g.PathTo((int)GetVertexIDFromNodeID(to));
                // found a path to the goal
                shortestPath.Add(from.Id);
                foreach (var edge in path)
                {
                    //todo  test me No clue about this....
                    var To = m_nodesByVertex[(int)edge.To];
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
        public void GetReachableNodesMask(TaxiNodesRecord from, byte[] mask)
        {
            DepthFirstSearch depthFirst = new DepthFirstSearch(m_graph, GetVertexIDFromNodeID(from), vertex =>
            {
                TaxiNodesRecord taxiNode = CliDB.TaxiNodesStorage.LookupByKey(GetNodeIDFromVertexID(vertex));
                if (taxiNode != null)
                    mask[(taxiNode.Id - 1) / 8] |= (byte)(1 << (int)((taxiNode.Id - 1) % 8));
            });
        }

        uint CreateVertexFromFromNodeInfoIfNeeded(TaxiNodesRecord node)
        {
            if (!m_verticesByNode.ContainsKey(node.Id))
            {
                m_verticesByNode.Add(node.Id, (uint)m_nodesByVertex.Count);
                m_nodesByVertex.Add(node);
            }

            return m_verticesByNode[node.Id];
        }
    }
}

