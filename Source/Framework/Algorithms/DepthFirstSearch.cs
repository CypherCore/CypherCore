using System;
using System.Collections.Generic;
using System.Text;
using Framework.Collections;

namespace Framework.Algorithms
{
    public class DepthFirstSearch
    {
        private bool[] marked;    // marked[v] = is there an s-v path?
        private int count;           // number of vertices connected to s

        /**
         * Computes the vertices in graph {@code G} that are
         * connected to the source vertex {@code s}.
         * @param G the graph
         * @param s the source vertex
         * @throws IllegalArgumentException unless {@code 0 <= s < V}
         */
        public DepthFirstSearch(EdgeWeightedDigraph G, uint s, Action<uint> action)
        {
            marked = new bool[G.NumberOfVertices];
            //validateVertex(s);
            dfs(G, s, action);
        }

        // depth first search from v
        private void dfs(EdgeWeightedDigraph G, uint v, Action<uint> action)
        {
            count++;
            marked[v] = true;
            foreach (var w in G.Adjacent((int)v))
            {
                if (!marked[w.To])
                {
                    action(w.To);
                    dfs(G, w.To, action);
                }
            }
        }

        /**
         * Is there a path between the source vertex {@code s} and vertex {@code v}?
         * @param v the vertex
         * @return {@code true} if there is a path, {@code false} otherwise
         * @throws IllegalArgumentException unless {@code 0 <= v < V}
         */
        public bool Marked(int v)
        {
            //validateVertex(v);
            return marked[v];
        }

        /**
         * Returns the number of vertices connected to the source vertex {@code s}.
         * @return the number of vertices connected to the source vertex {@code s}
         */
        public int Count()
        {
            return count;
        }
    }
}
