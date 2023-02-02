﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Collections;

namespace Framework.Algorithms
{
    /// <summary>
    /// The DijkstraShortestPath class represents a data type for solving the single-source shortest paths problem
    /// in edge-weighted digraphs where the edge weights are non-negative
    /// </summary>
    /// <seealso href="http://algs4.cs.princeton.edu/44sp/DijkstraSP.java.html">DijkstraSP class from Princeton University's Java Algorithms</seealso>
    public class DijkstraShortestPath
    {
        private readonly double[] _distanceTo;
        private readonly DirectedEdge[] _edgeTo;
        private readonly IndexMinPriorityQueue<double> _priorityQueue;
        /// <summary>
        /// Computes a shortest paths tree from the specified sourceVertex to every other vertex in the edge-weighted directed graph
        /// </summary>
        /// <param name="graph">The edge-weighted directed graph</param>
        /// <param name="sourceVertex">The source vertex to compute the shortest paths tree from</param>
        /// <exception cref="ArgumentOutOfRangeException">Throws an ArgumentOutOfRangeException if an edge weight is negative</exception>
        /// <exception cref="ArgumentNullException">Thrown if EdgeWeightedDigraph is null</exception>
        public DijkstraShortestPath(EdgeWeightedDigraph graph, int sourceVertex)
        {
            if (graph == null)
            {
                throw new ArgumentNullException("graph", "EdgeWeightedDigraph cannot be null");
            }

            foreach (DirectedEdge edge in graph.Edges())
            {
                if (edge.Weight < 0)
                {
                    throw new ArgumentOutOfRangeException($"Edge: '{edge}' has negative weight");
                }
            }

            _distanceTo = new double[graph.NumberOfVertices];
            _edgeTo = new DirectedEdge[graph.NumberOfVertices];
            for (int v = 0; v < graph.NumberOfVertices; v++)
            {
                _distanceTo[v] = double.PositiveInfinity;
            }
            _distanceTo[sourceVertex] = 0.0;

            _priorityQueue = new IndexMinPriorityQueue<double>(graph.NumberOfVertices);
            _priorityQueue.Insert(sourceVertex, _distanceTo[sourceVertex]);
            while (!_priorityQueue.IsEmpty())
            {
                int v = _priorityQueue.DeleteMin();
                foreach (DirectedEdge edge in graph.Adjacent(v))
                {
                    Relax(edge);
                }
            }
        }
        private void Relax(DirectedEdge edge)
        {
            uint v = edge.From;
            uint w = edge.To;
            if (_distanceTo[w] > _distanceTo[v] + edge.Weight)
            {
                _distanceTo[w] = _distanceTo[v] + edge.Weight;
                _edgeTo[w] = edge;
                if (_priorityQueue.Contains((int)w))
                {
                    _priorityQueue.DecreaseKey((int)w, _distanceTo[w]);
                }
                else
                {
                    _priorityQueue.Insert((int)w, _distanceTo[w]);
                }
            }
        }
        /// <summary>
        /// Returns the length of a shortest path from the sourceVertex to the specified destinationVertex
        /// </summary>
        /// <param name="destinationVertex">The destination vertex to find a shortest path to</param>
        /// <returns>The length of a shortest path from the sourceVertex to the specified destinationVertex or double.PositiveInfinity if no such path exists</returns>
        public double DistanceTo(int destinationVertex)
        {
            return _distanceTo[destinationVertex];
        }
        /// <summary>
        /// Is there a path from the sourceVertex to the specified destinationVertex?
        /// </summary>
        /// <param name="destinationVertex">The destination vertex to see if there is a path to</param>
        /// <returns>True if there is a path from the sourceVertex to the specified destinationVertex, false otherwise</returns>
        public bool HasPathTo(int destinationVertex)
        {
            return _distanceTo[destinationVertex] < double.PositiveInfinity;
        }
        /// <summary>
        /// Returns an IEnumerable of DirectedEdges representing a shortest path from the sourceVertex to the specified destinationVertex
        /// </summary>
        /// <param name="destinationVertex">The destination vertex to find a shortest path to</param>
        /// <returns>IEnumerable of DirectedEdges representing a shortest path from the sourceVertex to the specified destinationVertex</returns>
        public IEnumerable<DirectedEdge> PathTo(int destinationVertex)
        {
            if (!HasPathTo(destinationVertex))
            {
                return null;
            }
            var path = new Stack<DirectedEdge>();
            for (DirectedEdge edge = _edgeTo[destinationVertex]; edge != null; edge = _edgeTo[edge.From])
            {
                path.Push(edge);
            }
            return path;
        }
        // TODO: This method should be private and should be called from the bottom of the constructor
        /// <summary>
        /// check optimality conditions:
        /// </summary>
        /// <param name="graph">The edge-weighted directed graph</param>
        /// <param name="sourceVertex">The source vertex to check optimality conditions from</param>
        /// <returns>True if all optimality conditions are met, false otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown on null EdgeWeightedDigraph</exception>
        public bool Check(EdgeWeightedDigraph graph, int sourceVertex)
        {
            if (graph == null)
            {
                throw new ArgumentNullException("graph", "EdgeWeightedDigraph cannot be null");
            }

            if (_distanceTo[sourceVertex] != 0.0 || _edgeTo[sourceVertex] != null)
            {
                return false;
            }
            for (int v = 0; v < graph.NumberOfVertices; v++)
            {
                if (v == sourceVertex)
                {
                    continue;
                }
                if (_edgeTo[v] == null && _distanceTo[v] != double.PositiveInfinity)
                {
                    return false;
                }
            }
            for (int v = 0; v < graph.NumberOfVertices; v++)
            {
                foreach (DirectedEdge edge in graph.Adjacent(v))
                {
                    uint w = edge.To;
                    if (_distanceTo[v] + edge.Weight < _distanceTo[w])
                    {
                        return false;
                    }
                }
            }
            for (int w = 0; w < graph.NumberOfVertices; w++)
            {
                if (_edgeTo[w] == null)
                {
                    continue;
                }
                DirectedEdge edge = _edgeTo[w];
                uint v = edge.From;
                if (w != edge.To)
                {
                    return false;
                }
                if (_distanceTo[v] + edge.Weight != _distanceTo[w])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
