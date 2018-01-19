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

using Framework.Constants;
using Framework.GameMath;
using Game.DataStorage;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public class TaxiPathGraph : Singleton<TaxiPathGraph>
    {
        TaxiPathGraph() { }

        public void Initialize()
        {
            if (GetVertexCount() > 0)
                return;

            List<Tuple<Tuple<uint, uint>, uint>> edges = new List<Tuple<Tuple<uint, uint>, uint>>();

            // Initialize here
            foreach (TaxiPathRecord path in CliDB.TaxiPathStorage.Values)
            {
                TaxiNodesRecord from = CliDB.TaxiNodesStorage.LookupByKey(path.From);
                TaxiNodesRecord to = CliDB.TaxiNodesStorage.LookupByKey(path.To);
                if (from != null && to != null && from.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde) && to.Flags.HasAnyFlag(TaxiNodeFlags.Alliance | TaxiNodeFlags.Horde))
                    AddVerticeAndEdgeFromNodeInfo(from, to, path.Id, edges);
            }

            // create graph
            m_graph = new EdgeWeightedDigraph(GetVertexCount());

            for (int j = 0; j < edges.Count; ++j)
            {
                m_graph.AddEdge(new DirectedEdge(edges[j].Item1.Item1, edges[j].Item1.Item2, edges[j].Item2));
            }
        }

        uint GetNodeIDFromVertexID(uint vertexID)
        {
            if (vertexID < m_vertices.Length)
                return m_vertices[vertexID].Id;

            return uint.MaxValue;
        }

        uint GetVertexIDFromNodeID(TaxiNodesRecord node)
        {
            return node.LearnableIndex;
        }

        int GetVertexCount()
        {
            if (m_graph == null)
                return m_vertices.Length;

            //So we can use this function for readability, we define either max defined vertices or already loaded in graph count
            return m_vertices.Length;// Math.Max(m_graph.getNumberOfVertices(), m_vertices.Length);
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

                    uint map1, map2;
                    Vector2 pos1, pos2;

                    Global.DB2Mgr.DeterminaAlternateMapPosition(nodes[i - 1].MapID, nodes[i - 1].Loc.X, nodes[i - 1].Loc.Y, nodes[i - 1].Loc.Z, out map1, out pos1);
                    Global.DB2Mgr.DeterminaAlternateMapPosition(nodes[i].MapID, nodes[i].Loc.X, nodes[i].Loc.Y, nodes[i].Loc.Z, out map2, out pos2);

                    if (map1 != map2)
                        continue;

                    totalDist += (float)Math.Sqrt((float)Math.Pow(pos2.X - pos1.X, 2) + (float)Math.Pow(pos2.Y - pos1.Y, 2) + (float)Math.Pow(nodes[i].Loc.Z - nodes[i - 1].Loc.Z, 2));
                }

                uint dist = (uint)totalDist;
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
                    var To = m_vertices[edge.To];
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

        uint CreateVertexFromFromNodeInfoIfNeeded(TaxiNodesRecord node)
        {
            //Check if we need a new one or if it may be already created
            if (m_vertices.Length <= node.LearnableIndex)
                Array.Resize(ref m_vertices, (int)node.LearnableIndex + 1);

            m_vertices[node.LearnableIndex] = node;
            return node.LearnableIndex;
        }

        TaxiNodesRecord[] m_vertices = new TaxiNodesRecord[0];
        EdgeWeightedDigraph m_graph;
    }
}
    

