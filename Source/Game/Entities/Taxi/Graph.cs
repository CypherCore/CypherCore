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

using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Entities
{
    /// <summary>
    /// The IndexMinPriorityQueue class represents an indexed priority queue of generic keys.
    /// </summary>
    /// <seealso href="http://algs4.cs.princeton.edu/24pq/IndexMinPQ.java.html">IndexMinPQ class from Princeton University's Java Algorithms</seealso>
    /// <typeparam name="T">Type must implement IComparable interface</typeparam>
    public class IndexMinPriorityQueue<T> where T : IComparable<T>
    {
        private readonly T[] _keys;
        private readonly int _maxSize;
        private readonly int[] _pq;
        private readonly int[] _qp;
        /// <summary>
        /// Constructs an empty indexed priority queue with indices between 0 and the specified maxSize - 1
        /// </summary>
        /// <param name="maxSize">The maximum size of the indexed priority queue</param>
        public IndexMinPriorityQueue(int maxSize)
        {
            _maxSize = maxSize;
            Size = 0;
            _keys = new T[_maxSize + 1];
            _pq = new int[_maxSize + 1];
            _qp = new int[_maxSize + 1];
            for (int i = 0; i < _maxSize; i++)
            {
                _qp[i] = -1;
            }
        }
        /// <summary>
        /// The number of keys on this indexed priority queue
        /// </summary>
        public int Size { get; private set; }
        /// <summary>
        /// Is the indexed priority queue empty?
        /// </summary>
        /// <returns>True if the indexed priority queue is empty, false otherwise</returns>
        public bool IsEmpty()
        {
            return Size == 0;
        }
        /// <summary>
        /// Is the specified parameter i an index on the priority queue?
        /// </summary>
        /// <param name="i">An index to check for on the priority queue</param>
        /// <returns>True if the specified parameter i is an index on the priority queue, false otherwise</returns>
        public bool Contains(int i)
        {
            return _qp[i] != -1;
        }
        /// <summary>
        /// Associates the specified key with the specified index
        /// </summary>
        /// <param name="index">The index to associate the key with</param>
        /// <param name="key">The key to associate with the index</param>
        public void Insert(int index, T key)
        {
            Size++;
            _qp[index] = Size;
            _pq[Size] = index;
            _keys[index] = key;
            Swim(Size);
        }
        /// <summary>
        /// Returns an index associated with a minimum key
        /// </summary>
        /// <returns>An index associated with a minimum key</returns>
        public int MinIndex()
        {
            return _pq[1];
        }
        /// <summary>
        /// Returns a minimum key
        /// </summary>
        /// <returns>A minimum key</returns>
        public T MinKey()
        {
            return _keys[_pq[1]];
        }
        /// <summary>
        /// Removes a minimum key and returns its associated index
        /// </summary>
        /// <returns>An index associated with a minimum key that was removed</returns>
        public int DeleteMin()
        {
            int min = _pq[1];
            Exchange(1, Size--);
            Sink(1);
            _qp[min] = -1;
            _keys[_pq[Size + 1]] = default(T);
            _pq[Size + 1] = -1;
            return min;
        }
        /// <summary>
        /// Returns the key associated with the specified index
        /// </summary>
        /// <param name="index">The index of the key to return</param>
        /// <returns>The key associated with the specified index</returns>
        public T KeyAt(int index)
        {
            return _keys[index];
        }
        /// <summary>
        /// Change the key associated with the specified index to the specified value
        /// </summary>
        /// <param name="index">The index of the key to change</param>
        /// <param name="key">Change the key associated with the specified index to this key</param>
        public void ChangeKey(int index, T key)
        {
            _keys[index] = key;
            Swim(_qp[index]);
            Sink(_qp[index]);
        }
        /// <summary>
        /// Decrease the key associated with the specified index to the specified value
        /// </summary>
        /// <param name="index">The index of the key to decrease</param>
        /// <param name="key">Decrease the key associated with the specified index to this key</param>
        public void DecreaseKey(int index, T key)
        {
            _keys[index] = key;
            Swim(_qp[index]);
        }
        /// <summary>
        /// Increase the key associated with the specified index to the specified value
        /// </summary>
        /// <param name="index">The index of the key to increase</param>
        /// <param name="key">Increase the key associated with the specified index to this key</param>
        public void IncreaseKey(int index, T key)
        {
            _keys[index] = key;
            Sink(_qp[index]);
        }
        /// <summary>
        /// Remove the key associated with the specified index
        /// </summary>
        /// <param name="index">The index of the key to remove</param>
        public void Delete(int index)
        {
            int i = _qp[index];
            Exchange(i, Size--);
            Swim(i);
            Sink(i);
            _keys[index] = default(T);
            _qp[index] = -1;
        }
        private bool Greater(int i, int j)
        {
            return _keys[_pq[i]].CompareTo(_keys[_pq[j]]) > 0;
        }
        private void Exchange(int i, int j)
        {
            int swap = _pq[i];
            _pq[i] = _pq[j];
            _pq[j] = swap;
            _qp[_pq[i]] = i;
            _qp[_pq[j]] = j;
        }
        private void Swim(int k)
        {
            while (k > 1 && Greater(k / 2, k))
            {
                Exchange(k, k / 2);
                k = k / 2;
            }
        }
        private void Sink(int k)
        {
            while (2 * k <= Size)
            {
                int j = 2 * k;
                if (j < Size && Greater(j, j + 1))
                {
                    j++;
                }
                if (!Greater(k, j))
                {
                    break;
                }
                Exchange(k, j);
                k = j;
            }
        }
    }

    /// <summary>
    /// The EdgeWeightedDigrpah class represents an edge-weighted directed graph of vertices named 0 through V-1, where each directed edge
    /// is of type DirectedEdge and has real-valued weight.
    /// </summary>
    /// <seealso href="http://algs4.cs.princeton.edu/44sp/EdgeWeightedDigraph.java.html">EdgeWeightedDigraph class from Princeton University's Java Algorithms</seealso>
    public class EdgeWeightedDigraph
    {
        private readonly LinkedList<DirectedEdge>[] _adjacent;
        /// <summary>
        /// Constructs an empty edge-weighted digraph with the specified number of vertices and 0 edges
        /// </summary>
        /// <param name="vertices">Number of vertices in the Graph</param>
        public EdgeWeightedDigraph(int vertices)
        {
            NumberOfVertices = vertices;
            NumberOfEdges = 0;
            _adjacent = new LinkedList<DirectedEdge>[NumberOfVertices];
            for (int v = 0; v < NumberOfVertices; v++)
            {
                _adjacent[v] = new LinkedList<DirectedEdge>();
            }
        }
        /// <summary>
        /// The number of vertices in the edge-weighted digraph
        /// </summary>
        public int NumberOfVertices { get; private set; }
        /// <summary>
        /// The number of edges in the edge-weighted digraph
        /// </summary>
        public int NumberOfEdges { get; private set; }
        /// <summary>
        /// Adds the specified directed edge to the edge-weighted digraph
        /// </summary>
        /// <param name="edge">The DirectedEdge to add</param>
        /// <exception cref="ArgumentNullException">DirectedEdge cannot be null</exception>
        public void AddEdge(DirectedEdge edge)
        {
            if (edge == null)
            {
                throw new ArgumentNullException("edge", "DirectedEdge cannot be null");
            }

            _adjacent[edge.From].AddLast(edge);
        }
        /// <summary>
        /// Returns an IEnumerable of the DirectedEdges incident from the specified vertex
        /// </summary>
        /// <param name="vertex">The vertex to find incident DirectedEdges from</param>
        /// <returns>IEnumerable of the DirectedEdges incident from the specified vertex</returns>
        public IEnumerable<DirectedEdge> Adjacent(int vertex)
        {
            return _adjacent[vertex];
        }
        /// <summary>
        /// Returns an IEnumerable of all directed edges in the edge-weighted digraph
        /// </summary>
        /// <returns>IEnumerable of of all directed edges in the edge-weighted digraph</returns>
        public IEnumerable<DirectedEdge> Edges()
        {
            for (int v = 0; v < NumberOfVertices; v++)
            {
                foreach (DirectedEdge edge in _adjacent[v])
                {
                    yield return edge;
                }
            }
        }
        /// <summary>
        /// Returns the number of directed edges incident from the specified vertex
        /// This is known as the outdegree of the vertex
        /// </summary>
        /// <param name="vertex">The vertex to find find the outdegree of</param>
        /// <returns>The number of directed edges incident from the specified vertex</returns>
        public int OutDegree(int vertex)
        {
            return _adjacent[vertex].Count;
        }
        /// <summary>
        /// Returns a string that represents the current edge-weighted digraph
        /// </summary>
        /// <returns>
        /// A string that represents the current edge-weighted digraph
        /// </returns>
        public override string ToString()
        {
            var formattedString = new StringBuilder();
            formattedString.AppendFormat("{0} vertices, {1} edges {2}", NumberOfVertices, NumberOfEdges, Environment.NewLine);
            for (int v = 0; v < NumberOfVertices; v++)
            {
                formattedString.AppendFormat("{0}: ", v);
                foreach (DirectedEdge edge in _adjacent[v])
                {
                    formattedString.AppendFormat("{0} ", edge.To);
                }
                formattedString.AppendLine();
            }
            return formattedString.ToString();
        }
    }

    /// <summary>
    /// The DirectedEdge class represents a weighted edge in an edge-weighted directed graph. 
    /// </summary>
    /// <seealso href="http://algs4.cs.princeton.edu/44sp/DirectedEdge.java.html">DirectedEdge class from Princeton University's Java Algorithms</seealso>
    public class DirectedEdge
    {
        /// <summary>
        /// Constructs a directed edge from one specified vertex to another with the given weight
        /// </summary>
        /// <param name="from">The start vertex</param>
        /// <param name="to">The destination vertex</param>
        /// <param name="weight">The weight of the DirectedEdge</param>
        public DirectedEdge(uint from, uint to, double weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }
        /// <summary>
        /// Returns the destination vertex of the DirectedEdge
        /// </summary>
        public uint From { get; private set; }
        /// <summary>
        /// Returns the start vertex of the DirectedEdge
        /// </summary>
        public uint To { get; private set; }
        /// <summary>
        /// Returns the weight of the DirectedEdge
        /// </summary>
        public double Weight { get; private set; }
        /// <summary>
        /// Returns a string that represents the current DirectedEdge
        /// </summary>
        /// <returns>
        /// A string that represents the current DirectedEdge
        /// </returns>
        public override string ToString()
        {
            return $"From: {From}, To: {To}, Weight: {Weight}";
        }
    }

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
