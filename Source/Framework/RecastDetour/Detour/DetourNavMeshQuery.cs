// @class dtQueryFilter
//
// <b>The Default Implementation</b>
// 
// At construction: All area costs default to 1.0.  All flags are included
// and none are excluded.
// 
// If a polygon has both an include and an exclude flag, it will be excluded.
// 
// The way filtering works, a navigation mesh polygon must have at least one flag 
// set to ever be considered by a query. So a polygon with no flags will never
// be considered.
//
// Setting the include flags to 0 will result in all polygons being excluded.
//
// <b>Custom Implementations</b>
// 
// DT_VIRTUAL_QUERYFILTER must be defined in order to extend this class.
// 
// Implement a custom query filter by overriding the virtual passFilter() 
// and getCost() functions. If this is done, both functions should be as 
// fast as possible. Use cached local copies of data rather than accessing 
// your own objects where possible.
// 
// Custom implementations do not need to adhere to the flags or cost logic 
// used by the default implementation.  
// 
// In order for A* searches to work properly, the cost should be proportional to
// the travel distance. Implementing a cost modifier less than 1.0 is likely 
// to lead to problems during pathfinding.
//
// @see dtNavMeshQuery
// 

using System;
using System.Diagnostics;
using dtPolyRef = System.UInt64;
using dtStatus = System.UInt32;
using System.Collections.Generic;

// Define DT_VIRTUAL_QUERYFILTER if you wish to derive a custom filter from dtQueryFilter.
// On certain platforms indirect or virtual function call is expensive. The default
// setting is to use non-virtual functions, the actual implementations of the functions
// are declared as inline for maximum speed. 

//#define DT_VIRTUAL_QUERYFILTER 1

public static partial class Detour
{
    const float H_SCALE = 0.999f; // Search heuristic scale.

    /// Defines polygon filtering and traversal costs for navigation mesh query operations.
    // @ingroup detour
    public class dtQueryFilter
    {
        public float[] m_areaCost = new float[DT_MAX_AREAS];		//< Cost per area type. (Used by default implementation.)
        public ushort m_includeFlags;		//< Flags for polygons that can be visited. (Used by default implementation.)
        public ushort m_excludeFlags;		//< Flags for polygons that should not be visted. (Used by default implementation.)

        public dtQueryFilter()
        {
            m_includeFlags = 0xffff;
            m_excludeFlags = 0;
            for (int i = 0; i < DT_MAX_AREAS; ++i)
                m_areaCost[i] = 1.0f;
        }

        /// Returns true if the polygon can be visited.  (I.e. Is traversable.)
        ///  @param[in]		ref		The reference id of the polygon test.
        ///  @param[in]		tile	The tile containing the polygon.
        ///  @param[in]		poly  The polygon to test.
        public bool passFilter(dtPolyRef polyRef, dtMeshTile tile, dtPoly poly)
        {
            return (poly.flags & m_includeFlags) != 0 && (poly.flags & m_excludeFlags) == 0;
        }

        /// Returns cost to move from the beginning to the end of a line segment
        /// that is fully contained within a polygon.
        ///  @param[in]		pa			The start position on the edge of the previous and current polygon. [(x, y, z)]
        ///  @param[in]		pb			The end position on the edge of the current and next polygon. [(x, y, z)]
        ///  @param[in]		prevRef		The reference id of the previous polygon. [opt]
        ///  @param[in]		prevTile	The tile containing the previous polygon. [opt]
        ///  @param[in]		prevPoly	The previous polygon. [opt]
        ///  @param[in]		curRef		The reference id of the current polygon.
        ///  @param[in]		curTile		The tile containing the current polygon.
        ///  @param[in]		curPoly		The current polygon.
        ///  @param[in]		nextRef		The refernece id of the next polygon. [opt]
        ///  @param[in]		nextTile	The tile containing the next polygon. [opt]
        ///  @param[in]		nextPoly	The next polygon. [opt]
        public float getCost(float[] pa, float[] pb, dtPolyRef prevRef, dtMeshTile prevTile, dtPoly prevPoly, dtPolyRef curRef, dtMeshTile curTile, dtPoly curPoly, dtPolyRef nextRef, dtMeshTile nextTile, dtPoly nextPoly)
        {
            return dtVdist(pa, pb) * m_areaCost[curPoly.getArea()];
        }

        // @name Getters and setters for the default implementation data.
        ///@{

        /// Returns the traversal cost of the area.
        ///  @param[in]		i		The id of the area.
        // @returns The traversal cost of the area.
        public float getAreaCost(int i)
        {
            return m_areaCost[i];
        }

        /// Sets the traversal cost of the area.
        ///  @param[in]		i		The id of the area.
        ///  @param[in]		cost	The new cost of traversing the area.
        public void setAreaCost(int i, float cost)
        {
            m_areaCost[i] = cost;
        }

        /// Returns the include flags for the filter.
        /// Any polygons that include one or more of these flags will be
        /// included in the operation.
        public ushort getIncludeFlags()
        {
            return m_includeFlags;
        }

        /// Sets the include flags for the filter.
        // @param[in]		flags	The new flags.
        public void setIncludeFlags(ushort flags)
        {
            m_includeFlags = flags;
        }

        /// Returns the exclude flags for the filter.
        /// Any polygons that include one ore more of these flags will be
        /// excluded from the operation.
        public ushort getExcludeFlags()
        {
            return m_excludeFlags;
        }

        /// Sets the exclude flags for the filter.
        // @param[in]		flags		The new flags.
        public void setExcludeFlags(ushort flags)
        {
            m_excludeFlags = flags;
        }
    }

    /// Provides information about raycast hit
    /// filled by dtNavMeshQuery::raycast
    /// @ingroup detour
    public class dtRaycastHit
    {
        /// The hit parameter. (FLT_MAX if no wall hit.)
        public float t;

        /// hitNormal	The normal of the nearest wall hit. [(x, y, z)]
        public float[] hitNormal = new float[3];

        /// The index of the edge on the final polygon where the wall was hit.
        public int hitEdgeIndex;

        /// Pointer to an array of reference ids of the visited polygons. [opt]
        public dtPolyRef[] path;

        /// The number of visited polygons. [opt]
        public int pathCount;

        /// The maximum number of polygons the @p path array can hold.
        public int maxPath;

        ///  The cost of the path until hit.
        public float pathCost;
    };

    //////////////////////////////////////////////////////////////////////////////////////////

    // @class dtNavMeshQuery
    /// Provides the ability to perform pathfinding related queries against
    /// a navigation mesh.
    /// 
    /// For methods that support undersized buffers, if the buffer is too small 
    /// to hold the entire result set the return status of the method will include 
    /// the #DT_BUFFER_TOO_SMALL flag.
    ///
    /// Constant member functions can be used by multiple clients without side
    /// effects. (E.g. No change to the closed list. No impact on an in-progress
    /// sliced path query. Etc.)
    /// 
    /// Walls and portals: A @e wall is a polygon segment that is 
    /// considered impassable. A @e portal is a passable segment between polygons.
    /// A portal may be treated as a wall based on the dtQueryFilter used for a query.
    ///
    // @see dtNavMesh, dtQueryFilter, #dtAllocNavMeshQuery(), #dtAllocNavMeshQuery()
    // @ingroup detour
    public class dtNavMeshQuery
    {
        private dtNavMesh m_nav;				//< Pointer to navmesh data.

        private class dtQueryData
        {
            public dtStatus status;
            public dtNode lastBestNode;
            public float lastBestNodeCost;
            public dtPolyRef startRef;
            public dtPolyRef endRef;
            public float[] startPos = new float[3];
            public float[] endPos = new float[3];
            public dtQueryFilter filter;
            public uint options;
            public float raycastLimitSqr;

            public void dtcsClear()
            {
                status = 0;
                lastBestNode = null;
                lastBestNodeCost = .0f;
                startRef = 0;
                endRef = 0;
                for (int i = 0; i < 3; ++i)
                {
                    startPos[i] = 0f;
                    endPos[i] = 0f;
                }
                filter = null;
                options = 0;
                raycastLimitSqr = 0f;
            }
        }
        private dtQueryData m_query = new();				//< Sliced query state.

        private dtNodePool m_tinyNodePool;	//< Pointer to small node pool.
        private dtNodePool m_nodePool;		//< Pointer to node pool.
        private dtNodeQueue m_openList;		//< Pointer to open list queue.

        public dtNavMeshQuery()
        {
        }

        // @par 
        ///
        /// Must be the first function called after construction, before other
        /// functions are used.
        ///
        /// This function can be used multiple times.
        /// Initializes the query object.
        ///  @param[in]		nav			Pointer to the dtNavMesh object to use for all queries.
        ///  @param[in]		maxNodes	Maximum number of search nodes. [Limits: 0 &lt; value &lt;= 65536]
        // @returns The status flags for the query.
        public dtStatus init(dtNavMesh nav, int maxNodes)
        {
            m_nav = nav;

            if (m_nodePool == null || m_nodePool.getMaxNodes() < maxNodes)
            {
                if (m_nodePool != null)
                {
                    //m_nodePool.~dtNodePool();
                    //dtFree(m_nodePool);
                    m_nodePool = null;
                }
                m_nodePool = new dtNodePool(maxNodes, (int)dtNextPow2((uint)(maxNodes / 4)));//(dtAlloc(sizeof(dtNodePool), DT_ALLOC_PERM)) dtNodePool(maxNodes, dtNextPow2(maxNodes/4));
                if (m_nodePool == null)
                    return DT_FAILURE | DT_OUT_OF_MEMORY;
            }
            else
            {
                m_nodePool.clear();
            }

            if (m_tinyNodePool == null)
            {
                m_tinyNodePool = new dtNodePool(64, 32);//(dtAlloc(sizeof(dtNodePool), DT_ALLOC_PERM)) dtNodePool(64, 32);
                if (m_tinyNodePool == null)
                    return DT_FAILURE | DT_OUT_OF_MEMORY;
            }
            else
            {
                m_tinyNodePool.clear();
            }

            // TODO: check the open list size too.
            if (m_openList == null || m_openList.getCapacity() < maxNodes)
            {
                if (m_openList != null)
                {
                    //m_openList.~dtNodeQueue();
                    //dtFree(m_openList);
                    m_openList = null;
                }
                m_openList = new dtNodeQueue(maxNodes);//(dtAlloc(sizeof(dtNodeQueue), DT_ALLOC_PERM)) dtNodeQueue(maxNodes);
                if (m_openList == null)
                    return DT_FAILURE | DT_OUT_OF_MEMORY;
            }
            else
            {
                m_openList.clear();
            }

            return DT_SUCCESS;
        }

        // @name Standard Pathfinding Functions

        public delegate float randomFloatGenerator();

        /// Returns random location on navmesh.
        /// Polygons are chosen weighted by area. The search runs in linear related to number of polygon.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[in]		frand			Function returning a random number [0..1).
        ///  @param[out]	randomRef		The reference id of the random location.
        ///  @param[out]	randomPt		The random location. 
        // @returns The status flags for the query.
        public dtStatus findRandomPoint(dtQueryFilter filter, randomFloatGenerator frand, ref dtPolyRef randomRef, ref float[] randomPt)
        {
            Debug.Assert(m_nav != null);

            if (filter == null || frand == null || randomPt == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            // Randomly pick one tile. Assume that all tiles cover roughly the same area.
            dtMeshTile tile = null;
            float tsum = 0.0f;
            for (int i = 0; i < m_nav.getMaxTiles(); i++)
            {
                dtMeshTile curTile = m_nav.getTile(i);
                if (curTile == null || curTile.header == null) continue;

                // Choose random tile using reservoi sampling.
                const float area = 1.0f; // Could be tile area too.
                tsum += area;
                float u = frand();
                if (u * tsum <= area)
                    tile = curTile;
            }
            if (tile == null)
                return DT_FAILURE;

            // Randomly pick one polygon weighted by polygon area.
            dtPoly poly = null;
            dtPolyRef polyRef = 0;
            dtPolyRef polyRefBase = m_nav.getPolyRefBase(tile);

            float areaSum = 0.0f;
            for (int i = 0; i < tile.header.polyCount; ++i)
            {
                dtPoly p = tile.polys[i];
                // Do not return off-mesh connection polygons.
                if (p.getType() != (byte)dtPolyTypes.DT_POLYTYPE_GROUND)
                    continue;
                // Must pass filter
                dtPolyRef pRef = polyRefBase | (uint)i;
                if (!filter.passFilter(pRef, tile, p))
                    continue;

                // Calc area of the polygon.
                float polyArea = 0.0f;
                for (int j = 2; j < p.vertCount; ++j)
                {
                    //float* va = &tile.verts[p.verts[0]*3];
                    //float* vb = &tile.verts[p.verts[j-1]*3];
                    //float* vc = &tile.verts[p.verts[j]*3];

                    polyArea += Detour.dtTriArea2D(tile.verts, p.verts[0] * 3, tile.verts, p.verts[j - 1] * 3, tile.verts, p.verts[j] * 3);
                }

                // Choose random polygon weighted by area, using reservoi sampling.
                areaSum += polyArea;
                float u = frand();
                if (u * areaSum <= polyArea)
                {
                    poly = p;
                    polyRef = pRef;
                }
            }

            if (poly == null)
                return DT_FAILURE;

            // Randomly pick point on polygon.
            //const float* v = &tile.verts[poly.verts[0]*3];
            int vStart = poly.verts[0] * 3;
            float[] verts = new float[3 * DT_VERTS_PER_POLYGON];
            float[] areas = new float[DT_VERTS_PER_POLYGON];
            Detour.dtVcopy(verts, 0 * 3, tile.verts, vStart);
            for (int j = 1; j < poly.vertCount; ++j)
            {
                //v = &tile.verts[poly.verts[j]*3];
                Detour.dtVcopy(verts, j * 3, tile.verts, poly.verts[j] * 3);
            }

            float s = frand();
            float t = frand();

            float[] pt = new float[3];
            dtRandomPointInConvexPoly(verts, poly.vertCount, areas, s, t, pt);

            float h = 0.0f;
            dtStatus status = getPolyHeight(polyRef, pt, ref h);
            if (dtStatusFailed(status))
                return status;
            pt[1] = h;

            Detour.dtVcopy(randomPt, 0, pt, 0);
            randomRef = polyRef;

            return DT_SUCCESS;
        }

        /// Returns random location on navmesh within the reach of specified location.
        /// Polygons are chosen weighted by area. The search runs in linear related to number of polygon.
        /// The location is not exactly constrained by the circle, but it limits the visited polygons.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[in]		frand			Function returning a random number [0..1).
        ///  @param[out]	randomRef		The reference id of the random location.
        ///  @param[out]	randomPt		The random location. [(x, y, z)]
        // @returns The status flags for the query.
        public dtStatus findRandomPointAroundCircle(dtPolyRef startRef, float[] centerPos, float maxRadius, dtQueryFilter filter, randomFloatGenerator frand, ref dtPolyRef randomRef, ref float[] randomPt)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_nodePool != null);
            Debug.Assert(m_openList != null);

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) ||
                centerPos == null || !dtVisfinite(centerPos) ||
                maxRadius < 0 || !float.IsFinite(maxRadius) ||
                filter == null || frand == null || randomPt == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            dtMeshTile startTile = null;
            dtPoly startPoly = null;
            m_nav.getTileAndPolyByRefUnsafe(startRef, ref startTile, ref startPoly);
            if (!filter.passFilter(startRef, startTile, startPoly))
                return DT_FAILURE | DT_INVALID_PARAM;

            m_nodePool.clear();
            m_openList.clear();

            dtNode startNode = m_nodePool.getNode(startRef);
            dtVcopy(startNode.pos, centerPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_openList.push(startNode);

            dtStatus status = DT_SUCCESS;

            float radiusSqr = dtSqr(maxRadius);
            float areaSum = 0.0f;

            dtMeshTile randomTile = null;
            dtPoly randomPoly = null;
            dtPolyRef randomPolyRef = 0;

            while (!m_openList.empty())
            {
                dtNode bestNode = m_openList.pop();
                unchecked
                {
                    bestNode.flags &= (byte)(~dtNodeFlags.DT_NODE_OPEN);
                }
                bestNode.flags |= (byte)dtNodeFlags.DT_NODE_CLOSED;

                // Get poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef bestRef = bestNode.id;
                dtMeshTile bestTile = null;
                dtPoly bestPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(bestRef, ref bestTile, ref bestPoly);

                // Place random locations on on ground.
                if (bestPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_GROUND)
                {
                    // Calc area of the polygon.
                    float polyArea = 0.0f;
                    for (int j = 2; j < bestPoly.vertCount; ++j)
                    {
                        //const float* va = &bestTile.verts[bestPoly.verts[0]*3];
                        //const float* vb = &bestTile.verts[bestPoly.verts[j-1]*3];
                        //const float* vc = &bestTile.verts[bestPoly.verts[j]*3];
                        polyArea += dtTriArea2D(bestTile.verts, bestPoly.verts[0] * 3, bestTile.verts, bestPoly.verts[j - 1] * 3, bestTile.verts, bestPoly.verts[j] * 3);
                    }
                    // Choose random polygon weighted by area, using reservoi sampling.
                    areaSum += polyArea;
                    float u = frand();
                    if (u * areaSum <= polyArea)
                    {
                        randomTile = bestTile;
                        randomPoly = bestPoly;
                        randomPolyRef = bestRef;
                    }
                }


                // Get parent poly and tile.
                dtPolyRef parentRef = 0;
                dtMeshTile parentTile = null;
                dtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                    parentRef = m_nodePool.getNodeAtIdx(bestNode.pidx).id;
                if (parentRef != 0)
                    m_nav.getTileAndPolyByRefUnsafe(parentRef, ref parentTile, ref parentPoly);

                for (uint i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    dtLink link = bestTile.links[i];
                    dtPolyRef neighbourRef = link.polyRef;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                        continue;

                    // Expand to neighbour
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    // Find edge and calc distance to the edge.
                    float[] va = new float[3];//, vb[3];
                    float[] vb = new float[3];
                    if (getPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, va, vb) == 0)
                        continue;

                    // If the circle is not touching the next polygon, skip it.
                    float tseg = .0f;
                    float distSqr = dtDistancePtSegSqr2D(centerPos, 0, va, 0, vb, 0, ref tseg);
                    if (distSqr > radiusSqr)
                        continue;

                    dtNode neighbourNode = m_nodePool.getNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DT_OUT_OF_NODES;
                        continue;
                    }

                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_CLOSED) != 0)
                        continue;

                    // Cost
                    if (neighbourNode.flags == 0)
                    {
                        dtVlerp(neighbourNode.pos, va, vb, 0.5f);
                    }

                    float total = bestNode.total + dtVdist(bestNode.pos, neighbourNode.pos);

                    // The node is already in open list and the new result is worse, skip.
                    if (((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_OPEN) != 0) && total >= neighbourNode.total)
                        continue;

                    neighbourNode.id = neighbourRef;
                    unchecked
                    {
                        neighbourNode.flags = (byte)(neighbourNode.flags & (byte)(~dtNodeFlags.DT_NODE_CLOSED));
                    }
                    neighbourNode.pidx = m_nodePool.getNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        m_openList.modify(neighbourNode);
                    }
                    else
                    {
                        neighbourNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
                        m_openList.push(neighbourNode);
                    }
                }
            }

            if (randomPoly == null)
                return DT_FAILURE;

            // Randomly pick point on polygon.
            //float* v = &randomTile.verts[randomPoly.verts[0]*3];
            float[] verts = new float[3 * DT_VERTS_PER_POLYGON];
            float[] areas = new float[DT_VERTS_PER_POLYGON];
            dtVcopy(verts, 0 * 3, randomTile.verts, 0);
            for (int j = 1; j < randomPoly.vertCount; ++j)
            {
                //v = &randomTile.verts[randomPoly.verts[j]*3];
                dtVcopy(verts, j * 3, randomTile.verts, randomPoly.verts[j] * 3);
            }

            float s = frand();
            float t = frand();

            float[] pt = new float[3];
            dtRandomPointInConvexPoly(verts, randomPoly.vertCount, areas, s, t, pt);

            float h = 0.0f;
            dtStatus stat = getPolyHeight(randomPolyRef, pt, ref h);
            if (dtStatusFailed(status))
                return stat;
            pt[1] = h;

            dtVcopy(randomPt, pt);
            randomRef = randomPolyRef;

            return DT_SUCCESS;
        }

        //////////////////////////////////////////////////////////////////////////////////////////

        /// Finds the closest point on the specified polygon.
        ///  @param[in]		ref			The reference id of the polygon.
        ///  @param[in]		pos			The position to check. [(x, y, z)]
        ///  @param[out]	closest		The closest point on the polygon. [(x, y, z)]
        ///  @param[out]	posOverPoly	True of the position is over the polygon.
        // @returns The status flags for the query.
        // @par
        ///
        /// Uses the detail polygons to find the surface height. (Most accurate.)
        ///
        // @p pos does not have to be within the bounds of the polygon or navigation mesh.
        ///
        /// See closestPointOnPolyBoundary() for a limited but faster option.
        ///
        public dtStatus closestPointOnPoly(dtPolyRef polyRef, float[] pos, float[] closest, ref bool posOverPoly)
        {
            Debug.Assert(m_nav != null);
            
            if (!m_nav.isValidPolyRef(polyRef) ||
                pos == null || !dtVisfinite(pos) ||
                closest == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            m_nav.closestPointOnPoly(polyRef, pos, closest, ref posOverPoly);

            return DT_SUCCESS;
        }

        /// Returns a point on the boundary closest to the source point if the source point is outside the 
        /// polygon's xz-bounds.
        ///  @param[in]		ref			The reference id to the polygon.
        ///  @param[in]		pos			The position to check. [(x, y, z)]
        ///  @param[out]	closest		The closest point. [(x, y, z)]
        // @returns The status flags for the query.
        // @par
        ///
        /// Much faster than closestPointOnPoly().
        ///
        /// If the provided position lies within the polygon's xz-bounds (above or below), 
        /// then @p pos and @p closest will be equal.
        ///
        /// The height of @p closest will be the polygon boundary.  The height detail is not used.
        /// 
        // @p pos does not have to be within the bounds of the polybon or the navigation mesh.
        /// 
		public dtStatus closestPointOnPolyBoundary(dtPolyRef polyRef, float[] pos, float[] closest)
        {
            Debug.Assert(m_nav != null);

            dtMeshTile tile = null;
            dtPoly poly = null;
            if (dtStatusFailed(m_nav.getTileAndPolyByRef(polyRef, ref tile, ref poly)))
                return DT_FAILURE | DT_INVALID_PARAM;

            if (pos == null || !dtVisfinite(pos) || closest == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            // Collect vertices.
            float[] verts = new float[DT_VERTS_PER_POLYGON * 3];
            float[] edged = new float[DT_VERTS_PER_POLYGON];
            float[] edget = new float[DT_VERTS_PER_POLYGON];
            int nv = 0;
            for (int i = 0; i < (int)poly.vertCount; ++i)
            {
                dtVcopy(verts, nv * 3, tile.verts, poly.verts[i] * 3);
                nv++;
            }

            bool inside = dtDistancePtPolyEdgesSqr(pos, 0, verts, nv, edged, edget);
            if (inside)
            {
                // Point is inside the polygon, return the point.
                dtVcopy(closest, pos);
            }
            else
            {
                // Point is outside the polygon, dtClamp to nearest edge.
                float dmin = float.MaxValue;
                int imin = -1;
                for (int i = 0; i < nv; ++i)
                {
                    if (edged[i] < dmin)
                    {
                        dmin = edged[i];
                        imin = i;
                    }
                }
                //const float* va = &verts[imin*3];
                //const float* vb = &verts[((imin+1)%nv)*3];
                int vaStart = imin * 3;
                int vbStart = ((imin + 1) % nv) * 3;
                dtVlerp(closest, 0, verts, vaStart, verts, vbStart, edget[imin]);
            }

            return DT_SUCCESS;
        }

        /// Gets the height of the polygon at the provided position using the height detail. (Most accurate.)
        ///  @param[in]		ref			The reference id of the polygon.
        ///  @param[in]		pos			A position within the xz-bounds of the polygon. [(x, y, z)]
        ///  @param[out]	height		The height at the surface of the polygon.
        // @returns The status flags for the query.
        // @par
        ///
        /// Will return #DT_FAILURE | DT_INVALID_PARAM if the provided position is outside the xz-bounds 
        /// of the polygon.
        /// 
        public dtStatus getPolyHeight(dtPolyRef polyRef, float[] pos, ref float height)
        {
            Debug.Assert(m_nav != null);

            dtMeshTile tile = null;
            dtPoly poly = null;
            uint ip = 0;
            if (dtStatusFailed(m_nav.getTileAndPolyByRef(polyRef, ref tile, ref poly, ref ip)))
                return DT_FAILURE | DT_INVALID_PARAM;

            if (pos == null || !dtVisfinite2D(pos))
                return DT_FAILURE | DT_INVALID_PARAM;

            // We used to return success for offmesh connections, but the
            // getPolyHeight in DetourNavMesh does not do this, so special
            // case it here.
            if (poly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                //const float* v0 = &tile.verts[poly.verts[0]*3];
                //const float* v1 = &tile.verts[poly.verts[1]*3];
                int v0Start = poly.verts[0] * 3;
                int v1Start = poly.verts[1] * 3;
                float t = 0;
                dtDistancePtSegSqr2D(pos, 0, tile.verts, v0Start, tile.verts, v1Start, ref t);
                //if (height)
                height = tile.verts[v0Start + 1] + (tile.verts[v1Start + 1] - tile.verts[v0Start + 1]) * t;
                return DT_SUCCESS;
            }

            return m_nav.getPolyHeight(tile, poly, pos, ref height) ? DT_SUCCESS : DT_FAILURE | DT_INVALID_PARAM;
        }

        // @}
        // @name Local Query Functions
        ///@{

        /// Finds the polygon nearest to the specified center point.
        ///  @param[in]		center		The center of the search box. [(x, y, z)]
        ///  @param[in]		halfExtents	The search distance along each axis. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	nearestRef	The reference id of the nearest polygon.
        ///  @param[out]	nearestPt	The nearest point on the polygon. [opt] [(x, y, z)]
        // @returns The status flags for the query.
        // @par 
        ///
        // @note If the search box does not intersect any polygons the search will 
        /// return #DT_SUCCESS, but @p nearestRef will be zero. So if in doubt, check 
        // @p nearestRef before using @p nearestPt.
        ///
        // @warning This function is not suitable for large area searches.  If the search
        /// extents overlaps more than 128 polygons it may return an invalid result.
        ///
		public dtStatus findNearestPoly(float[] center, float[] halfExtents, dtQueryFilter filter, ref dtPolyRef nearestRef, ref float[] nearestPt)
        {
            Debug.Assert(m_nav != null);

            nearestRef = 0;

            // queryPolygons below will check rest of params

            dtFindNearestPolyQuery query = new(this, center);

            dtStatus status = queryPolygons(center, halfExtents, filter, query);
            if (dtStatusFailed(status))
                return status;

            nearestRef = query.nearestRef();
            // Only override nearestPt if we actually found a poly so the nearest point
            // is valid.
            if (nearestRef != 0)
                dtVcopy(nearestPt, query.nearestPoint());

            return DT_SUCCESS;
        }

        /// Queries polygons within a tile.
		public void queryPolygonsInTile(dtMeshTile tile, float[] qmin, float[] qmax, dtQueryFilter filter, dtFindNearestPolyQuery query)
        {
            Debug.Assert(m_nav != null);

            const int batchSize = 32;
            dtPolyRef[] polyRefs = new dtPolyRef[batchSize];
            dtPoly[] polys = new dtPoly[batchSize];
            int n = 0;

            if (tile.bvTree != null)
            {
                dtBVNode node = tile.bvTree[0];
                //dtBVNode* end = &tile.bvTree[tile.header.bvNodeCount];
                int endIndex = tile.header.bvNodeCount;
                float[] tbmin = tile.header.bmin;
                float[] tbmax = tile.header.bmax;
                float qfac = tile.header.bvQuantFactor;

                // Calculate quantized box
                ushort[] bmin = new ushort[3];
                ushort[] bmax = new ushort[3];
                // dtClamp query box to world box.
                float minx = dtClamp(qmin[0], tbmin[0], tbmax[0]) - tbmin[0];
                float miny = dtClamp(qmin[1], tbmin[1], tbmax[1]) - tbmin[1];
                float minz = dtClamp(qmin[2], tbmin[2], tbmax[2]) - tbmin[2];
                float maxx = dtClamp(qmax[0], tbmin[0], tbmax[0]) - tbmin[0];
                float maxy = dtClamp(qmax[1], tbmin[1], tbmax[1]) - tbmin[1];
                float maxz = dtClamp(qmax[2], tbmin[2], tbmax[2]) - tbmin[2];
                // Quantize
                bmin[0] = (ushort)((int)(qfac * minx) & 0xfffe);
                bmin[1] = (ushort)((int)(qfac * miny) & 0xfffe);
                bmin[2] = (ushort)((int)(qfac * minz) & 0xfffe);
                bmax[0] = (ushort)((int)(qfac * maxx + 1) | 1);
                bmax[1] = (ushort)((int)(qfac * maxy + 1) | 1);
                bmax[2] = (ushort)((int)(qfac * maxz + 1) | 1);

                // Traverse tree
                dtPolyRef polyRefBase = m_nav.getPolyRefBase(tile);
                int nodeIndex = 0;
                while (nodeIndex < endIndex)
                {
                    node = tile.bvTree[nodeIndex];

                    bool overlap = dtOverlapQuantBounds(bmin, bmax, node.bmin, node.bmax);
                    bool isLeafNode = node.i >= 0;

                    if (isLeafNode && overlap)
                    {
                        dtPolyRef polyRef = polyRefBase | (uint)node.i;
                        if (filter.passFilter(polyRef, tile, tile.polys[node.i]))
                        {
                            polyRefs[n] = polyRef;
                            polys[n] = tile.polys[node.i];

                            if (n == batchSize - 1)
                            {
                                query.process(tile, polys, polyRefs, batchSize);
                                n = 0;
                            }
                            else
                            {
                                n++;
                            }
                        }
                    }

                    if (overlap || isLeafNode)
                    {
                        nodeIndex++;
                    }
                    else
                    {
                        int escapeIndex = -node.i;
                        nodeIndex += escapeIndex;
                    }
                }
            }
            else
            {
                float[] bmin = new float[3];
                float[] bmax = new float[3];
                dtPolyRef polyRefBase = m_nav.getPolyRefBase(tile);
                for (int i = 0; i < tile.header.polyCount; ++i)
                {
                    dtPoly p = tile.polys[i];
                    // Do not return off-mesh connection polygons.
                    if (p.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                        continue;
                    // Must pass filter
                    dtPolyRef polyRef = polyRefBase | (uint)i;
                    if (!filter.passFilter(polyRef, tile, p))
                        continue;
                    // Calc polygon bounds.
                    //const float* v = &tile.verts[p.verts[0]*3];
                    int vStart = p.verts[0] * 3;
                    dtVcopy(bmin, 0, tile.verts, vStart);
                    dtVcopy(bmax, 0, tile.verts, vStart);
                    for (int j = 1; j < p.vertCount; ++j)
                    {
                        //v = &tile.verts[p.verts[j]*3];
                        vStart = p.verts[j] * 3;
                        dtVmin(bmin, 0, tile.verts, vStart);
                        dtVmax(bmax, 0, tile.verts, vStart);
                    }
                    if (dtOverlapBounds(qmin, qmax, bmin, bmax))
                    {
                        polyRefs[n] = polyRef;
                        polys[n] = p;

                        if (n == batchSize - 1)
                        {
                            query.process(tile, polys, polyRefs, batchSize);
                            n = 0;
                        }
                        else
                        {
                            n++;
                        }
                    }
                }
            }


            // Process the last polygons that didn't make a full batch.
            if (n > 0)
                query.process(tile, polys, polyRefs, n);
        }

        /// Finds polygons that overlap the search box.
        ///  @param[in]		center		The center of the search box. [(x, y, z)]
        ///  @param[in]		halfExtents The search distance along each axis. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	polys		The reference ids of the polygons that overlap the query box.
        ///  @param[out]	polyCount	The number of polygons in the search result.
        ///  @param[in]		maxPolys	The maximum number of polygons the search result can hold.
        // @returns The status flags for the query.
        // @par 
        ///
        /// If no polygons are found, the function will return #DT_SUCCESS with a
        // @p polyCount of zero.
        ///
        /// If @p polys is too small to hold the entire result set, then the array will 
        /// be filled to capacity. The method of choosing which polygons from the 
        /// full set are included in the partial result set is undefined.
        ///
		public dtStatus queryPolygons(float[] center, float[] halfExtents, dtQueryFilter filter, dtFindNearestPolyQuery query)
        {
            Debug.Assert(m_nav != null);

            if (center == null || !dtVisfinite(center) ||
                halfExtents == null || !dtVisfinite(halfExtents) ||
                filter == null || query == null)
            {
                return DT_FAILURE | DT_INVALID_PARAM;
            }

            float[] bmin = new float[3];
            float[] bmax = new float[3];
            dtVsub(bmin, center, halfExtents);
            dtVadd(bmax, center, halfExtents);

            // Find tiles the query touches.
            int minx = 0, miny = 0, maxx = 0, maxy = 0;
            m_nav.calcTileLoc(bmin, ref minx, ref miny);
            m_nav.calcTileLoc(bmax, ref maxx, ref maxy);

            int MAX_NEIS = 32;
            dtMeshTile[] neis = new dtMeshTile[MAX_NEIS];

            for (int y = miny; y <= maxy; ++y)
            {
                for (int x = minx; x <= maxx; ++x)
                {
                    int nneis = m_nav.getTilesAt(x, y, neis, MAX_NEIS);
                    for (int j = 0; j < nneis; ++j)
                    {
                        queryPolygonsInTile(neis[j], bmin, bmax, filter, query);
                    }
                }
            }

            return DT_SUCCESS;
        }

        /// Finds a path from the start polygon to the end polygon.
        ///  @param[in]		startRef	The refrence id of the start polygon.
        ///  @param[in]		endRef		The reference id of the end polygon.
        ///  @param[in]		startPos	A position within the start polygon. [(x, y, z)]
        ///  @param[in]		endPos		A position within the end polygon. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	path		An ordered list of polygon references representing the path. (Start to end.) 
        ///  							[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount	The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath		The maximum number of polygons the @p path array can hold. [Limit: >= 1]
        // @par
        ///
        /// If the end polygon cannot be reached through the navigation graph,
        /// the last polygon in the path will be the nearest the end polygon.
        ///
        /// If the path array is to small to hold the full result, it will be filled as 
        /// far as possible from the start polygon toward the end polygon.
        ///
        /// The start and end positions are used to calculate traversal costs. 
        /// (The y-values impact the result.)
        ///
        public dtStatus findPath(dtPolyRef startRef, dtPolyRef endRef, float[] startPos, float[] endPos, dtQueryFilter filter, dtPolyRef[] path, ref uint pathCount, int maxPath)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_nodePool != null);
            Debug.Assert(m_openList != null);

            pathCount = 0;

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) || !m_nav.isValidPolyRef(endRef) ||
                startPos == null || !dtVisfinite(startPos) ||
                endPos == null || !dtVisfinite(endPos) ||
                filter == null || path == null || maxPath <= 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            if (startRef == endRef)
            {
                path[0] = startRef;
                pathCount = 1;
                return DT_SUCCESS;
            }

            m_nodePool.clear();
            m_openList.clear();

            dtNode startNode = m_nodePool.getNode(startRef);
            dtVcopy(startNode.pos, startPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = dtVdist(startPos, endPos) * H_SCALE;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_openList.push(startNode);

            dtNode lastBestNode = startNode;
            float lastBestNodeCost = startNode.total;

            bool outOfNodes = false;

            while (!m_openList.empty())
            {
                // Remove node from open list and put it in closed list.
                dtNode bestNode = m_openList.pop();
                unchecked
                {
                    bestNode.flags &= (byte)(~dtNodeFlags.DT_NODE_OPEN);
                }
                bestNode.flags |= (byte)dtNodeFlags.DT_NODE_CLOSED;

                // Reached the goal, stop searching.
                if (bestNode.id == endRef)
                {
                    lastBestNode = bestNode;
                    break;
                }

                // Get current poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef bestRef = bestNode.id;
                dtMeshTile bestTile = null;
                dtPoly bestPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(bestRef, ref bestTile, ref bestPoly);

                // Get parent poly and tile.
                dtPolyRef parentRef = 0;
                dtMeshTile parentTile = null;
                dtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                    parentRef = m_nodePool.getNodeAtIdx(bestNode.pidx).id;
                if (parentRef != 0)
                    m_nav.getTileAndPolyByRefUnsafe(parentRef, ref parentTile, ref parentPoly);

                for (uint i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    dtPolyRef neighbourRef = bestTile.links[i].polyRef;

                    // Skip invalid ids and do not expand back to where we came from.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                        continue;

                    // Get neighbour poly and tile.
                    // The API input has been cheked already, skip checking internal data.
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    if (!filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    // deal explicitly with crossing tile boundaries
                    byte crossSide = 0;
                    if (bestTile.links[i].side != 0xff)
                        crossSide = (byte)(bestTile.links[i].side >> 1);

                    dtNode neighbourNode = m_nodePool.getNode(neighbourRef, crossSide);
                    if (neighbourNode == null)
                    {
                        outOfNodes = true;
                        continue;
                    }

                    // If the node is visited the first time, calculate node position.
                    if (neighbourNode.flags == 0)
                    {
                        getEdgeMidPoint(bestRef, bestPoly, bestTile,
                                        neighbourRef, neighbourPoly, neighbourTile,
                                        neighbourNode.pos);
                    }

                    // Calculate cost and heuristic.
                    float cost = 0;
                    float heuristic = 0;

                    // Special case for last node.
                    if (neighbourRef == endRef)
                    {
                        // Cost
                        float curCost = filter.getCost(bestNode.pos, neighbourNode.pos,
                                                              parentRef, parentTile, parentPoly,
                                                              bestRef, bestTile, bestPoly,
                                                              neighbourRef, neighbourTile, neighbourPoly);
                        float endCost = filter.getCost(neighbourNode.pos, endPos,
                                                              bestRef, bestTile, bestPoly,
                                                              neighbourRef, neighbourTile, neighbourPoly,
                                                              0, null, null);

                        cost = bestNode.cost + curCost + endCost;
                        heuristic = 0;
                    }
                    else
                    {
                        // Cost
                        float curCost = filter.getCost(bestNode.pos, neighbourNode.pos,
                                                              parentRef, parentTile, parentPoly,
                                                              bestRef, bestTile, bestPoly,
                                                              neighbourRef, neighbourTile, neighbourPoly);
                        cost = bestNode.cost + curCost;
                        heuristic = dtVdist(neighbourNode.pos, endPos) * H_SCALE;
                    }

                    float total = cost + heuristic;

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                        continue;
                    // The node is already visited and process, and the new result is worse, skip.
                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode.total)
                        continue;

                    // Add or update the node.
                    neighbourNode.pidx = m_nodePool.getNodeIdx(bestNode);
                    neighbourNode.id = neighbourRef;
                    unchecked
                    {
                        neighbourNode.flags = (byte)(neighbourNode.flags & ~(byte)dtNodeFlags.DT_NODE_CLOSED);
                    }
                    neighbourNode.cost = cost;
                    neighbourNode.total = total;

                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        // Already in open, update node location.
                        m_openList.modify(neighbourNode);
                    }
                    else
                    {
                        // Put the node in open list.
                        neighbourNode.flags |= (byte)dtNodeFlags.DT_NODE_OPEN;
                        m_openList.push(neighbourNode);
                    }

                    // Update nearest node to target so far.
                    if (heuristic < lastBestNodeCost)
                    {
                        lastBestNodeCost = heuristic;
                        lastBestNode = neighbourNode;
                    }
                }
            }

            dtStatus status = getPathToNode(lastBestNode, path, ref pathCount, maxPath);

            if (lastBestNode.id != endRef)
                status |= DT_PARTIAL_RESULT;

            if (outOfNodes)
                status |= DT_OUT_OF_NODES;

            return status;
        }

        dtStatus getPathToNode(dtNode endNode, dtPolyRef[] path, ref uint pathCount, int maxPath)
        {
            // Find the length of the entire path.
            dtNode curNode = endNode;
            int length = 0;
            do
            {
                length++;
                curNode = m_nodePool.getNodeAtIdx(curNode.pidx);
            } while (curNode != null);

            // If the path cannot be fully stored then advance to the last node we will be able to store.
            curNode = endNode;
            int writeCount;
            for (writeCount = length; writeCount > maxPath; writeCount--)
            {
                //dtAssert(curNode);

                curNode = m_nodePool.getNodeAtIdx(curNode.pidx);
            }

            // Write path
            for (int i = writeCount - 1; i >= 0; i--)
            {
                //dtAssert(curNode);

                path[i] = curNode.id;
                curNode = m_nodePool.getNodeAtIdx(curNode.pidx);
            }

            //dtAssert(!curNode);


            pathCount = (uint)Math.Min(length, maxPath);

            if (length > maxPath)
                return DT_SUCCESS | DT_BUFFER_TOO_SMALL;

            return DT_SUCCESS;
        }

        ///@}
        // @name Sliced Pathfinding Functions
        /// Common use case:
        ///	-# Call initSlicedFindPath() to initialize the sliced path query.
        ///	-# Call updateSlicedFindPath() until it returns complete.
        ///	-# Call finalizeSlicedFindPath() to get the path.
        ///@{ 

        /// Intializes a sliced path query.
        ///  @param[in]		startRef	The refrence id of the start polygon.
        ///  @param[in]		endRef		The reference id of the end polygon.
        ///  @param[in]		startPos	A position within the start polygon. [(x, y, z)]
        ///  @param[in]		endPos		A position within the end polygon. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        // @returns The status flags for the query.
        // @par
        ///
        // @warning Calling any non-slice methods before calling finalizeSlicedFindPath() 
        /// or finalizeSlicedFindPathPartial() may result in corrupted data!
        ///
        /// The @p filter pointer is stored and used for the duration of the sliced
        /// path query.
        ///
		public dtStatus initSlicedFindPath(dtPolyRef startRef, dtPolyRef endRef, float[] startPos, float[] endPos, dtQueryFilter filter, uint options)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_nodePool != null);
            Debug.Assert(m_openList != null);

            // Init path state.
            //memset(&m_query, 0, sizeof(dtQueryData));

            m_query.status = DT_FAILURE;
            m_query.startRef = startRef;
            m_query.endRef = endRef;
            dtVcopy(m_query.startPos, startPos);
            dtVcopy(m_query.endPos, endPos);
            m_query.filter = filter;
            m_query.options = options;
            m_query.raycastLimitSqr = float.MaxValue;

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) || !m_nav.isValidPolyRef(endRef) ||
                startPos == null || !dtVisfinite(startPos) ||
                endPos == null || !dtVisfinite(endPos) || filter == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            // trade quality with performance?
            if ((options & (int)dtFindPathOptions.DT_FINDPATH_ANY_ANGLE) != 0)
            {
                // limiting to several times the character radius yields nice results. It is not sensitive 
                // so it is enough to compute it from the first tile.
                dtMeshTile tile = m_nav.getTileByRef(startRef);
                float agentRadius = tile.header.walkableRadius;
                m_query.raycastLimitSqr = dtSqr(agentRadius * 50.0f); //DT_RAY_CAST_LIMIT_PROPORTIONS;
            }

            if (startRef == endRef)
            {
                m_query.status = DT_SUCCESS;
                return DT_SUCCESS;
            }

            m_nodePool.clear();
            m_openList.clear();

            dtNode startNode = m_nodePool.getNode(startRef);
            dtVcopy(startNode.pos, startPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = dtVdist(startPos, endPos) * H_SCALE;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_openList.push(startNode);

            m_query.status = DT_IN_PROGRESS;
            m_query.lastBestNode = startNode;
            m_query.lastBestNodeCost = startNode.total;

            return m_query.status;
        }

        /// Updates an in-progress sliced path query.
        ///  @param[in]		maxIter		The maximum number of iterations to perform.
        ///  @param[out]	doneIters	The actual number of iterations completed. [opt]
        // @returns The status flags for the query.
        public dtStatus updateSlicedFindPath(int maxIter, ref int doneIters)
        {
            if (!dtStatusInProgress(m_query.status))
                return m_query.status;

            // Make sure the request is still valid.
            if (!m_nav.isValidPolyRef(m_query.startRef) || !m_nav.isValidPolyRef(m_query.endRef))
            {
                m_query.status = DT_FAILURE;
                return DT_FAILURE;
            }

            dtRaycastHit rayHit = new();
            rayHit.maxPath = 0;

            int iter = 0;
            while (iter < maxIter && !m_openList.empty())
            {
                iter++;

                // Remove node from open list and put it in closed list.
                dtNode bestNode = m_openList.pop();
                bestNode.dtcsClearFlag(dtNodeFlags.DT_NODE_OPEN);
                bestNode.dtcsSetFlag(dtNodeFlags.DT_NODE_CLOSED);

                // Reached the goal, stop searching.
                if (bestNode.id == m_query.endRef)
                {
                    m_query.lastBestNode = bestNode;
                    dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;
                    m_query.status = DT_SUCCESS | details;
                    //if (doneIters)
                    doneIters = iter;
                    return m_query.status;
                }

                // Get current poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef bestRef = bestNode.id;
                dtMeshTile bestTile = null;
                dtPoly bestPoly = null;
                if (dtStatusFailed(m_nav.getTileAndPolyByRef(bestRef, ref bestTile, ref bestPoly)))
                {
                    // The polygon has disappeared during the sliced query, fail.
                    m_query.status = DT_FAILURE;
                    //if (doneIters)
                    doneIters = iter;
                    return m_query.status;
                }

                // Get parent poly and tile.
                dtPolyRef parentRef = 0;
                dtPolyRef grandpaRef = 0;
                dtMeshTile parentTile = null;
                dtPoly parentPoly = null;
                dtNode parentNode = null;
                if (bestNode.pidx != 0)
                {
                    parentNode = m_nodePool.getNodeAtIdx(bestNode.pidx);
                    parentRef = parentNode.id;
                    if (parentNode.pidx != 0)
                        grandpaRef = m_nodePool.getNodeAtIdx(parentNode.pidx).id;
                }
                if (parentRef != 0)
                {
                    bool invalidParent = dtStatusFailed(m_nav.getTileAndPolyByRef(parentRef, ref parentTile, ref parentPoly));
                    if (invalidParent || (grandpaRef != 0 && !m_nav.isValidPolyRef(grandpaRef)))
                    {
                        // The polygon has disappeared during the sliced query, fail.
                        m_query.status = DT_FAILURE;
                        //if (doneIters)
                        doneIters = iter;
                        return m_query.status;
                    }
                }

                // decide whether to test raycast to previous nodes
                bool tryLOS = false;
                if ((m_query.options & (int)dtFindPathOptions.DT_FINDPATH_ANY_ANGLE) != 0)
                {
                    if ((parentRef != 0) && (dtVdistSqr(parentNode.pos, bestNode.pos) < m_query.raycastLimitSqr))
                        tryLOS = true;
                }

                for (uint i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    dtPolyRef neighbourRef = bestTile.links[i].polyRef;

                    // Skip invalid ids and do not expand back to where we came from.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                        continue;

                    // Get neighbour poly and tile.
                    // The API input has been cheked already, skip checking internal data.
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    if (!m_query.filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    dtNode neighbourNode = m_nodePool.getNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        m_query.status |= DT_OUT_OF_NODES;
                        continue;
                    }

                    // do not expand to nodes that were already visited from the same parent
                    if (neighbourNode.pidx != 0 && neighbourNode.pidx == bestNode.pidx)
                        continue;

                    // If the node is visited the first time, calculate node position.
                    if (neighbourNode.flags == 0)
                    {
                        getEdgeMidPoint(bestRef, bestPoly, bestTile,
                                        neighbourRef, neighbourPoly, neighbourTile,
                                        neighbourNode.pos);
                    }

                    // Calculate cost and heuristic.
                    float cost = 0;
                    float heuristic = 0;

                    // raycast parent
                    bool foundShortCut = false;
                    rayHit.pathCost = rayHit.t = 0;
                    if (tryLOS)
                    {
                        raycast(parentRef, parentNode.pos, neighbourNode.pos, m_query.filter, (int)dtRaycastOptions.DT_RAYCAST_USE_COSTS, rayHit, grandpaRef);
                        foundShortCut = rayHit.t >= 1.0f;
                    }

                    // update move cost
                    if (foundShortCut)
                    {
                        // shortcut found using raycast. Using shorter cost instead
                        cost = parentNode.cost + rayHit.pathCost;
                    }
                    else
                    {
                        // No shortcut found.
                        float curCost = m_query.filter.getCost(bestNode.pos, neighbourNode.pos,
                                                                      parentRef, parentTile, parentPoly,
                                                                    bestRef, bestTile, bestPoly,
                                                                    neighbourRef, neighbourTile, neighbourPoly);
                        cost = bestNode.cost + curCost;
                    }

                    // Special case for last node.
                    if (neighbourRef == m_query.endRef)
                    {
                        float endCost = m_query.filter.getCost(neighbourNode.pos, m_query.endPos,
                                                                      bestRef, bestTile, bestPoly,
                                                                      neighbourRef, neighbourTile, neighbourPoly,
                                                                      0, null, null);

                        cost = cost + endCost;
                        heuristic = 0;
                    }
                    else
                    {
                        heuristic = dtVdist(neighbourNode.pos, m_query.endPos) * H_SCALE;
                    }

                    float total = cost + heuristic;

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_OPEN) != 0 && total >= neighbourNode.total)
                        continue;
                    // The node is already visited and process, and the new result is worse, skip.
                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_CLOSED) != 0 && total >= neighbourNode.total)
                        continue;

                    // Add or update the node.
                    neighbourNode.pidx = foundShortCut ? bestNode.pidx : m_nodePool.getNodeIdx(bestNode);
                    neighbourNode.id = neighbourRef;
                    neighbourNode.flags = (byte)(neighbourNode.flags & ~(byte)(dtNodeFlags.DT_NODE_CLOSED | dtNodeFlags.DT_NODE_PARENT_DETACHED));
                    neighbourNode.cost = cost;
                    neighbourNode.total = total;
                    if (foundShortCut)
                        neighbourNode.flags = (byte)(neighbourNode.flags | (byte)dtNodeFlags.DT_NODE_PARENT_DETACHED);

                    if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_OPEN) != 0)
                    {
                        // Already in open, update node location.
                        m_openList.modify(neighbourNode);
                    }
                    else
                    {
                        // Put the node in open list.
                        //neighbourNode.flags |= DT_NODE_OPEN;
                        neighbourNode.dtcsSetFlag(dtNodeFlags.DT_NODE_OPEN);
                        m_openList.push(neighbourNode);
                    }

                    // Update nearest node to target so far.
                    if (heuristic < m_query.lastBestNodeCost)
                    {
                        m_query.lastBestNodeCost = heuristic;
                        m_query.lastBestNode = neighbourNode;
                    }
                }
            }

            // Exhausted all nodes, but could not find path.
            if (m_openList.empty())
            {
                dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;
                m_query.status = DT_SUCCESS | details;
            }

            //if (doneIters)
            doneIters = iter;

            return m_query.status;
        }

        /// Finalizes and returns the results of a sliced path query.
        ///  @param[out]	path		An ordered list of polygon references representing the path. (Start to end.) 
        ///  							[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount	The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath		The max number of polygons the path array can hold. [Limit: >= 1]
        // @returns The status flags for the query.
        public dtStatus finalizeSlicedFindPath(dtPolyRef[] path, ref int pathCount, int maxPath)
        {
            pathCount = 0;

            if (path == null || maxPath <= 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            if (dtStatusFailed(m_query.status))
            {
                // Reset query.
                //memset(&m_query, 0, sizeof(dtQueryData));
                m_query.dtcsClear();
                return DT_FAILURE;
            }

            int n = 0;

            if (m_query.startRef == m_query.endRef)
            {
                // Special case: the search starts and ends at same poly.
                path[n++] = m_query.startRef;
            }
            else
            {
                // Reverse the path.
                Debug.Assert(m_query.lastBestNode != null);

                if (m_query.lastBestNode.id != m_query.endRef)
                    m_query.status |= DT_PARTIAL_RESULT;

                dtNode prev = null;
                dtNode node = m_query.lastBestNode;
                int prevRay = 0;
                do
                {
                    dtNode next = m_nodePool.getNodeAtIdx(node.pidx);
                    node.pidx = m_nodePool.getNodeIdx(prev);
                    prev = node;
                    int nextRay = node.flags & (byte)dtNodeFlags.DT_NODE_PARENT_DETACHED; // keep track of whether parent is not adjacent (i.e. due to raycast shortcut)
                    node.flags = (byte)((node.flags & ~(byte)dtNodeFlags.DT_NODE_PARENT_DETACHED) | prevRay); // and store it in the reversed path's node
                    prevRay = nextRay;
                    node = next;
                }
                while (node != null);

                // Store path
                node = prev;
                do
                {
                    dtNode next = m_nodePool.getNodeAtIdx(node.pidx);
                    dtStatus status = 0;
                    if ((node.flags & (byte)dtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        float t = 0;
                        float[] normal = new float[3];
                        uint m = 0;
                        dtPolyRef[] temp = new dtPolyRef[path.Length];
                        status = raycast(node.id, node.pos, next.pos, m_query.filter, ref t, normal, temp, ref m, maxPath - n);

                        for (var i = 0; i < path.Length - n; ++i)
                            path[n + i] = temp[i];

                        n += (int)m;
                        // raycast ends on poly boundary and the path might include the next poly boundary.
                        if (path[n - 1] == next.id)
                            n--; // remove to avoid duplicates
                    }
                    else
                    {
                        path[n++] = node.id;
                        if (n >= maxPath)
                            status = DT_BUFFER_TOO_SMALL;
                    }

                    if ((status & DT_STATUS_DETAIL_MASK) != 0)
                    {
                        m_query.status |= status & DT_STATUS_DETAIL_MASK;
                        break;
                    }
                    node = next;
                }
                while (node != null);
            }

            dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;

            // Reset query.
            //memset(&m_query, 0, sizeof(dtQueryData));
            m_query.dtcsClear();

            pathCount = n;

            return DT_SUCCESS | details;
        }

        /// Finalizes and returns the results of an incomplete sliced path query, returning the path to the furthest
        /// polygon on the existing path that was visited during the search.
        ///  @param[in]		existing		An array of polygon references for the existing path.
        ///  @param[in]		existingSize	The number of polygon in the @p existing array.
        ///  @param[out]	path			An ordered list of polygon references representing the path. (Start to end.) 
        ///  								[(polyRef) * @p pathCount]
        ///  @param[out]	pathCount		The number of polygons returned in the @p path array.
        ///  @param[in]		maxPath			The max number of polygons the @p path array can hold. [Limit: >= 1]
        // @returns The status flags for the query.
        public dtStatus finalizeSlicedFindPathPartial(dtPolyRef[] existing, int existingSize, dtPolyRef[] path, ref int pathCount, int maxPath)
        {
            pathCount = 0;

            if (existing == null || existingSize <= 0 || path == null || maxPath <= 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            if (dtStatusFailed(m_query.status))
            {
                // Reset query.
                //memset(&m_query, 0, sizeof(dtQueryData));
                m_query.dtcsClear();
                return DT_FAILURE;
            }

            int n = 0;

            if (m_query.startRef == m_query.endRef)
            {
                // Special case: the search starts and ends at same poly.
                path[n++] = m_query.startRef;
            }
            else
            {
                // Find furthest existing node that was visited.
                dtNode prev = null;
                dtNode node = null;
                for (int i = existingSize - 1; i >= 0; --i)
                {
                    node = m_nodePool.findNode(existing[i]);
                    if (node != null)
                        break;
                }

                if (node == null)
                {
                    m_query.status |= DT_PARTIAL_RESULT;
                    Debug.Assert(m_query.lastBestNode != null);
                    node = m_query.lastBestNode;
                }

                // Reverse the path.
                int prevRay = 0;
                do
                {
                    dtNode next = m_nodePool.getNodeAtIdx(node.pidx);
                    node.pidx = m_nodePool.getNodeIdx(prev);
                    prev = node;
                    int nextRay = node.flags & (byte)dtNodeFlags.DT_NODE_PARENT_DETACHED; // keep track of whether parent is not adjacent (i.e. due to raycast shortcut)
                    node.flags = (byte)((node.flags & ~(byte)dtNodeFlags.DT_NODE_PARENT_DETACHED) | prevRay); // and store it in the reversed path's node
                    prevRay = nextRay;
                    node = next;
                }
                while (node != null);

                // Store path
                node = prev;
                do
                {
                    dtNode next = m_nodePool.getNodeAtIdx(node.pidx);
                    dtStatus status = 0;
                    if ((node.flags & (byte)dtNodeFlags.DT_NODE_PARENT_DETACHED) != 0)
                    {
                        float t = 0;
                        float[] normal = new float[3];
                        uint m = 0;
                        dtPolyRef[] temp = new dtPolyRef[path.Length - n];
                        status = raycast(node.id, node.pos, next.pos, m_query.filter, ref t, normal, temp, ref m, maxPath - n);
                        for (var i = 0; i < path.Length - n; ++i)
                            path[n + i] = temp[i];

                        n += (int)m;
                        // raycast ends on poly boundary and the path might include the next poly boundary.
                        if (path[n - 1] == next.id)
                            n--; // remove to avoid duplicates
                    }
                    else
                    {
                        path[n++] = node.id;
                        if (n >= maxPath)
                            status = DT_BUFFER_TOO_SMALL;
                    }

                    if ((status & DT_STATUS_DETAIL_MASK) != 0)
                    {
                        m_query.status |= status & DT_STATUS_DETAIL_MASK;
                        break;
                    }
                    node = next;
                }
                while (node != null);
            }

            dtStatus details = m_query.status & DT_STATUS_DETAIL_MASK;

            // Reset query.
            //memset(&m_query, 0, sizeof(dtQueryData));
            m_query.dtcsClear();

            pathCount = n;

            return DT_SUCCESS | details;
        }

        // Appends vertex to a straight path
        dtStatus appendVertex(float[] pos, byte flags, dtPolyRef polyRef, float[] straightPath, byte[] straightPathFlags, dtPolyRef[] straightPathRefs, ref int straightPathCount, int maxStraightPath)
        {
            if (straightPathCount > 0 && dtVequal(straightPath, (straightPathCount - 1) * 3, pos, 0))
            {
                // The vertices are equal, update flags and poly.
                if (straightPathFlags != null)
                    straightPathFlags[straightPathCount - 1] = flags;
                if (straightPathRefs != null)
                    straightPathRefs[straightPathCount - 1] = polyRef;
            }
            else
            {
                // Append new vertex.
                dtVcopy(straightPath, straightPathCount * 3, pos, 0);
                if (straightPathFlags != null)
                    straightPathFlags[straightPathCount] = flags;
                if (straightPathRefs != null)
                    straightPathRefs[straightPathCount] = polyRef;
                straightPathCount++;

                // If there is no space to append more vertices, return.
                if (straightPathCount >= maxStraightPath)
                {
                    return DT_SUCCESS | DT_BUFFER_TOO_SMALL;
                }

                // If reached end of path or there is no space to append more vertices, return.
                if (flags == (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END)
                {
                    return DT_SUCCESS;
                }
            }
            return DT_IN_PROGRESS;
        }

        // Appends intermediate portal points to a straight path.
        dtStatus appendPortals(int startIdx, int endIdx, float[] endPos, dtPolyRef[] path, float[] straightPath, byte[] straightPathFlags, dtPolyRef[] straightPathRefs, ref int straightPathCount, int maxStraightPath, int options)
        {
            //float* startPos = &straightPath[(*straightPathCount-1)*3];
            int startPosStart = (straightPathCount - 1) * 3;
            // Append or update last vertex
            dtStatus stat = 0;
            for (int i = startIdx; i < endIdx; i++)
            {
                // Calculate portal
                dtPolyRef from = path[i];
                dtMeshTile fromTile = null;
                dtPoly fromPoly = null;
                if (dtStatusFailed(m_nav.getTileAndPolyByRef(from, ref fromTile, ref fromPoly)))
                    return DT_FAILURE | DT_INVALID_PARAM;

                dtPolyRef to = path[i + 1];
                dtMeshTile toTile = null;
                dtPoly toPoly = null;
                if (dtStatusFailed(m_nav.getTileAndPolyByRef(to, ref toTile, ref toPoly)))
                    return DT_FAILURE | DT_INVALID_PARAM;

                float[] left = new float[3];//, right[3];
                float[] right = new float[3];
                if (dtStatusFailed(getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, left, right)))
                    break;

                if ((options & (int)dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS) != 0)
                {
                    // Skip intersection if only area crossings are requested.
                    if (fromPoly.getArea() == toPoly.getArea())
                        continue;
                }

                // Append intersection
                float s = .0f, t = .0f;
                if (dtIntersectSegSeg2D(straightPath, startPosStart, endPos, 0, left, 0, right, 0, ref s, ref t))
                {
                    float[] pt = new float[3];
                    dtVlerp(pt, left, right, t);

                    stat = appendVertex(pt, 0, path[i + 1],
                                        straightPath, straightPathFlags, straightPathRefs,
                                        ref straightPathCount, maxStraightPath);
                    if (stat != DT_IN_PROGRESS)
                        return stat;
                }
            }
            return DT_IN_PROGRESS;
        }

        /// Finds the straight path from the start to the end position within the polygon corridor.
        ///  @param[in]		startPos			Path start position. [(x, y, z)]
        ///  @param[in]		endPos				Path end position. [(x, y, z)]
        ///  @param[in]		path				An array of polygon references that represent the path corridor.
        ///  @param[in]		pathSize			The number of polygons in the @p path array.
        ///  @param[out]	straightPath		Points describing the straight path. [(x, y, z) * @p straightPathCount].
        ///  @param[out]	straightPathFlags	Flags describing each point. (See: #dtStraightPathFlags) [opt]
        ///  @param[out]	straightPathRefs	The reference id of the polygon that is being entered at each point. [opt]
        ///  @param[out]	straightPathCount	The number of points in the straight path.
        ///  @param[in]		maxStraightPath		The maximum number of points the straight path arrays can hold.  [Limit: > 0]
        ///  @param[in]		options				Query options. (see: #dtStraightPathOptions)
        // @returns The status flags for the query.
        // @par
        /// 
        /// This method peforms what is often called 'string pulling'.
        ///
        /// The start position is clamped to the first polygon in the path, and the 
        /// end position is clamped to the last. So the start and end positions should 
        /// normally be within or very near the first and last polygons respectively.
        ///
        /// The returned polygon references represent the reference id of the polygon 
        /// that is entered at the associated path position. The reference id associated 
        /// with the end point will always be zero.  This allows, for example, matching 
        /// off-mesh link points to their representative polygons.
        ///
        /// If the provided result buffers are too small for the entire result set, 
        /// they will be filled as far as possible from the start toward the end 
        /// position.
        ///
        public dtStatus findStraightPath(float[] startPos, float[] endPos, dtPolyRef[] path, int pathSize, float[] straightPath, byte[] straightPathFlags, dtPolyRef[] straightPathRefs, ref int straightPathCount, int maxStraightPath, int options)
        {
            Debug.Assert(m_nav != null);

            straightPathCount = 0;

            if (startPos == null || !dtVisfinite(startPos) ||
                endPos == null || !dtVisfinite(endPos) ||
                path == null || pathSize <= 0 || path[0] == 0 ||
                maxStraightPath <= 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            dtStatus stat = 0;

            // TODO: Should this be callers responsibility?
            float[] closestStartPos = new float[3];
            if (dtStatusFailed(closestPointOnPolyBoundary(path[0], startPos, closestStartPos)))
                return DT_FAILURE | DT_INVALID_PARAM;

            float[] closestEndPos = new float[3];
            if (dtStatusFailed(closestPointOnPolyBoundary(path[pathSize - 1], endPos, closestEndPos)))
                return DT_FAILURE | DT_INVALID_PARAM;

            // Add start point.
            stat = appendVertex(closestStartPos, (byte)dtStraightPathFlags.DT_STRAIGHTPATH_START, path[0],
                                straightPath, straightPathFlags, straightPathRefs,
                                ref straightPathCount, maxStraightPath);
            if (stat != DT_IN_PROGRESS)
                return stat;

            if (pathSize > 1)
            {
                float[] portalApex = new float[3];//, portalLeft[3], portalRight[3];
                float[] portalLeft = new float[3];
                float[] portalRight = new float[3];
                dtVcopy(portalApex, closestStartPos);
                dtVcopy(portalLeft, portalApex);
                dtVcopy(portalRight, portalApex);
                int apexIndex = 0;
                int leftIndex = 0;
                int rightIndex = 0;

                byte leftPolyType = 0;
                byte rightPolyType = 0;

                dtPolyRef leftPolyRef = path[0];
                dtPolyRef rightPolyRef = path[0];

                for (int i = 0; i < pathSize; ++i)
                {
                    float[] left = new float[3];
                    float[] right = new float[3];
                    byte fromType = 0, toType = 0;

                    if (i + 1 < pathSize)
                    {
                        // Next portal.
                        if (dtStatusFailed(getPortalPoints(path[i], path[i + 1], left, right, ref fromType, ref toType)))
                        {
                            // Failed to get portal points, in practice this means that path[i+1] is invalid polygon.
                            // Clamp the end point to path[i], and return the path so far.

                            if (dtStatusFailed(closestPointOnPolyBoundary(path[i], endPos, closestEndPos)))
                            {
                                // This should only happen when the first polygon is invalid.
                                return DT_FAILURE | DT_INVALID_PARAM;
                            }

                            // Apeend portals along the current straight path segment.
                            if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = appendPortals(apexIndex, i, closestEndPos, path,
                                                     straightPath, straightPathFlags, straightPathRefs,
                                                     ref straightPathCount, maxStraightPath, options);
                            }

                            stat = appendVertex(closestEndPos, 0, path[i],
                                                straightPath, straightPathFlags, straightPathRefs,
                                                ref straightPathCount, maxStraightPath);

                            return DT_SUCCESS | DT_PARTIAL_RESULT | ((straightPathCount >= maxStraightPath) ? DT_BUFFER_TOO_SMALL : (uint)0);
                        }

                        // If starting really close the portal, advance.
                        if (i == 0)
                        {
                            float t = 0.0f;
                            if (dtDistancePtSegSqr2D(portalApex, 0, left, 0, right, 0, ref t) < dtSqr(0.001f))
                                continue;
                        }
                    }
                    else
                    {
                        // End of the path.
                        dtVcopy(left, closestEndPos);
                        dtVcopy(right, closestEndPos);

                        toType = (byte)dtPolyTypes.DT_POLYTYPE_GROUND;
                        fromType = (byte)dtPolyTypes.DT_POLYTYPE_GROUND;
                    }

                    // Right vertex.
                    if (dtTriArea2D(portalApex, portalRight, right) <= 0.0f)
                    {
                        if (dtVequal(portalApex, portalRight) || dtTriArea2D(portalApex, portalLeft, right) > 0.0f)
                        {
                            dtVcopy(portalRight, right);
                            rightPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
                            rightPolyType = toType;
                            rightIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment.
                            if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = appendPortals(apexIndex, leftIndex, portalLeft, path,
                                                     straightPath, straightPathFlags, straightPathRefs,
                                                     ref straightPathCount, maxStraightPath, options);
                                if (stat != DT_IN_PROGRESS)
                                    return stat;
                            }

                            dtVcopy(portalApex, portalLeft);
                            apexIndex = leftIndex;

                            byte flags = 0;
                            if (leftPolyRef == 0)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END;
                            else if (leftPolyType == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                                flags = (byte)Detour.dtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION;
                            dtPolyRef polyRef = leftPolyRef;

                            // Append or update vertex
                            stat = appendVertex(portalApex, flags, polyRef,
                                                straightPath, straightPathFlags, straightPathRefs,
                                                ref straightPathCount, maxStraightPath);
                            if (stat != DT_IN_PROGRESS)
                                return stat;

                            dtVcopy(portalLeft, portalApex);
                            dtVcopy(portalRight, portalApex);
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;

                            continue;
                        }
                    }

                    // Left vertex.
                    if (dtTriArea2D(portalApex, portalLeft, left) >= 0.0f)
                    {
                        if (dtVequal(portalApex, portalLeft) || dtTriArea2D(portalApex, portalRight, left) < 0.0f)
                        {
                            dtVcopy(portalLeft, left);
                            leftPolyRef = (i + 1 < pathSize) ? path[i + 1] : 0;
                            leftPolyType = toType;
                            leftIndex = i;
                        }
                        else
                        {
                            // Append portals along the current straight path segment.
                            if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                            {
                                stat = appendPortals(apexIndex, rightIndex, portalRight, path,
                                                     straightPath, straightPathFlags, straightPathRefs,
                                                     ref straightPathCount, maxStraightPath, options);
                                if (stat != DT_IN_PROGRESS)
                                    return stat;
                            }

                            dtVcopy(portalApex, portalRight);
                            apexIndex = rightIndex;

                            byte flags = 0;
                            if (rightPolyRef == 0)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END;
                            else if (rightPolyType == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                                flags = (byte)dtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION;
                            dtPolyRef polyRef = rightPolyRef;

                            // Append or update vertex
                            stat = appendVertex(portalApex, flags, polyRef,
                                                straightPath, straightPathFlags, straightPathRefs,
                                                ref straightPathCount, maxStraightPath);
                            if (stat != DT_IN_PROGRESS)
                                return stat;

                            dtVcopy(portalLeft, portalApex);
                            dtVcopy(portalRight, portalApex);
                            leftIndex = apexIndex;
                            rightIndex = apexIndex;

                            // Restart
                            i = apexIndex;

                            continue;
                        }
                    }
                }

                // Append portals along the current straight path segment.
                if ((options & (int)(dtStraightPathOptions.DT_STRAIGHTPATH_AREA_CROSSINGS | dtStraightPathOptions.DT_STRAIGHTPATH_ALL_CROSSINGS)) != 0)
                {
                    stat = appendPortals(apexIndex, pathSize - 1, closestEndPos, path,
                                         straightPath, straightPathFlags, straightPathRefs,
                                         ref straightPathCount, maxStraightPath, options);
                    if (stat != DT_IN_PROGRESS)
                        return stat;
                }
            }

            stat = appendVertex(closestEndPos, (byte)dtStraightPathFlags.DT_STRAIGHTPATH_END, 0,
                                straightPath, straightPathFlags, straightPathRefs,
                                ref straightPathCount, maxStraightPath);

            return DT_SUCCESS | ((straightPathCount >= maxStraightPath) ? DT_BUFFER_TOO_SMALL : 0);
        }

        /// Moves from the start to the end position constrained to the navigation mesh.
        ///  @param[in]		startRef		The reference id of the start polygon.
        ///  @param[in]		startPos		A position of the mover within the start polygon. [(x, y, x)]
        ///  @param[in]		endPos			The desired end position of the mover. [(x, y, z)]
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultPos		The result position of the mover. [(x, y, z)]
        ///  @param[out]	visited			The reference ids of the polygons visited during the move.
        ///  @param[out]	visitedCount	The number of polygons visited during the move.
        ///  @param[in]		maxVisitedSize	The maximum number of polygons the @p visited array can hold.
        // @returns The status flags for the query.
        // @par
        ///
        /// This method is optimized for small delta movement and a small number of 
        /// polygons. If used for too great a distance, the result set will form an 
        /// incomplete path.
        ///
        // @p resultPos will equal the @p endPos if the end is reached. 
        /// Otherwise the closest reachable position will be returned.
        /// 
        // @p resultPos is not projected onto the surface of the navigation 
        /// mesh. Use #getPolyHeight if this is needed.
        ///
        /// This method treats the end position in the same manner as 
        /// the #raycast method. (As a 2D point.) See that method's documentation 
        /// for details.
        /// 
        /// If the @p visited array is too small to hold the entire result set, it will 
        /// be filled as far as possible from the start position toward the end 
        /// position.
        ///
        public dtStatus moveAlongSurface(dtPolyRef startRef, float[] startPos, float[] endPos, dtQueryFilter filter, float[] resultPos, dtPolyRef[] visited, ref int visitedCount, int maxVisitedSize)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_tinyNodePool != null);

            visitedCount = 0;

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) ||
                startPos == null || !dtVisfinite(startPos) ||
                endPos == null || !dtVisfinite(endPos) ||
                filter == null || resultPos == null || visited == null ||
                maxVisitedSize <= 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            dtStatus status = DT_SUCCESS;

            const int MAX_STACK = 48;
            dtNode[] stack = new dtNode[MAX_STACK];
            int nstack = 0;

            m_tinyNodePool.clear();

            dtNode startNode = m_tinyNodePool.getNode(startRef);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_CLOSED;
            stack[nstack++] = startNode;

            float[] bestPos = new float[3];
            float bestDist = float.MaxValue;
            dtNode bestNode = null;
            dtVcopy(bestPos, startPos);

            // Search constraints
            float[] searchPos = new float[3];//, searchRadSqr;
            float searchRadSqr = .0f;
            dtVlerp(searchPos, startPos, endPos, 0.5f);
            searchRadSqr = dtSqr(dtVdist(startPos, endPos) / 2.0f + 0.001f);

            float[] verts = new float[DT_VERTS_PER_POLYGON * 3];

            while (nstack != 0)
            {
                // Pop front.
                dtNode curNode = stack[0];
                for (int i = 0; i < nstack - 1; ++i)
                    stack[i] = stack[i + 1];
                nstack--;

                // Get poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef curRef = curNode.id;
                dtMeshTile curTile = null;
                dtPoly curPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(curRef, ref curTile, ref curPoly);

                // Collect vertices.
                int nverts = curPoly.vertCount;
                for (int i = 0; i < nverts; ++i)
                {
                    dtVcopy(verts, i * 3, curTile.verts, curPoly.verts[i] * 3);
                }

                // If target is inside the poly, stop search.
                if (dtPointInPolygon(endPos, verts, nverts))
                {
                    bestNode = curNode;
                    dtVcopy(bestPos, endPos);
                    break;
                }

                // Find wall edges and find nearest point inside the walls.
                for (int i = 0, j = (int)curPoly.vertCount - 1; i < (int)curPoly.vertCount; j = i++)
                {
                    // Find links to neighbours.
                    const int MAX_NEIS = 8;
                    int nneis = 0;
                    dtPolyRef[] neis = new dtPolyRef[MAX_NEIS];

                    if ((curPoly.neis[j] & DT_EXT_LINK) != 0)
                    {
                        // Tile border.
                        for (uint k = curPoly.firstLink; k != DT_NULL_LINK; k = curTile.links[k].next)
                        {
                            dtLink link = curTile.links[k];
                            if (link.edge == j)
                            {
                                if (link.polyRef != 0)
                                {
                                    dtMeshTile neiTile = null;
                                    dtPoly neiPoly = null;
                                    m_nav.getTileAndPolyByRefUnsafe(link.polyRef, ref neiTile, ref neiPoly);
                                    if (filter.passFilter(link.polyRef, neiTile, neiPoly))
                                    {
                                        if (nneis < MAX_NEIS)
                                            neis[nneis++] = link.polyRef;
                                    }
                                }
                            }
                        }
                    }
                    else if (curPoly.neis[j] != 0)
                    {
                        uint idx = (uint)(curPoly.neis[j] - 1);
                        dtPolyRef polyRef = m_nav.getPolyRefBase(curTile) | idx;
                        if (filter.passFilter(polyRef, curTile, curTile.polys[idx]))
                        {
                            // Internal edge, encode id.
                            neis[nneis++] = polyRef;
                        }
                    }

                    if (nneis == 0)
                    {
                        // Wall edge, calc distance.
                        //const float* vj = &verts[j*3];
                        //const float* vi = &verts[i*3];
                        int vjStart = j * 3;
                        int viStart = i * 3;
                        float tseg = .0f;
                        float distSqr = dtDistancePtSegSqr2D(endPos, 0, verts, vjStart, verts, viStart, ref tseg);
                        if (distSqr < bestDist)
                        {
                            // Update nearest distance.
                            dtVlerp(bestPos, 0, verts, vjStart, verts, viStart, tseg);
                            bestDist = distSqr;
                            bestNode = curNode;
                        }
                    }
                    else
                    {
                        for (int k = 0; k < nneis; ++k)
                        {
                            // Skip if no node can be allocated.
                            dtNode neighbourNode = m_tinyNodePool.getNode(neis[k]);
                            if (neighbourNode == null)
                                continue;
                            // Skip if already visited.
                            if ((neighbourNode.flags & (byte)dtNodeFlags.DT_NODE_CLOSED) != 0)
                                continue;

                            // Skip the link if it is too far from search constraint.
                            // TODO: Maybe should use getPortalPoints(), but this one is way faster.
                            int vjStart = j * 3;
                            int viStart = i * 3;
                            float tseg = .0f;
                            float distSqr = dtDistancePtSegSqr2D(searchPos, 0, verts, vjStart, verts, viStart, ref tseg);
                            if (distSqr > searchRadSqr)
                            {
                                continue;
                            }

                            // Mark as the node as visited and push to queue.
                            if (nstack < MAX_STACK)
                            {
                                neighbourNode.pidx = m_tinyNodePool.getNodeIdx(curNode);
                                neighbourNode.dtcsSetFlag(dtNodeFlags.DT_NODE_CLOSED);
                                stack[nstack++] = neighbourNode;
                            }
                        }
                    }
                }
            }

            int n = 0;
            if (bestNode != null)
            {
                // Reverse the path.
                dtNode prev = null;
                dtNode node = bestNode;
                do
                {
                    dtNode next = m_tinyNodePool.getNodeAtIdx(node.pidx);
                    node.pidx = m_tinyNodePool.getNodeIdx(prev);
                    prev = node;
                    node = next;
                }
                while (node != null);

                // Store result
                node = prev;
                do
                {

                    visited[n++] = node.id;
                    if (n >= maxVisitedSize)
                    {
                        status |= DT_BUFFER_TOO_SMALL;
                        break;
                    }
                    node = m_tinyNodePool.getNodeAtIdx(node.pidx);
                }
                while (node != null);
            }

            dtVcopy(resultPos, bestPos);

            visitedCount = n;

            return status;
        }

        /// Returns portal points between two polygons.
        dtStatus getPortalPoints(dtPolyRef from, dtPolyRef to, float[] left, float[] right, ref byte fromType, ref byte toType)
        {
            Debug.Assert(m_nav != null);

            dtMeshTile fromTile = null;
            dtPoly fromPoly = null;
            if (dtStatusFailed(m_nav.getTileAndPolyByRef(from, ref fromTile, ref fromPoly)))
                return DT_FAILURE | DT_INVALID_PARAM;
            fromType = fromPoly.getType();

            dtMeshTile toTile = null;
            dtPoly toPoly = null;
            if (dtStatusFailed(m_nav.getTileAndPolyByRef(to, ref toTile, ref toPoly)))
                return DT_FAILURE | DT_INVALID_PARAM;
            toType = toPoly.getType();

            return getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, left, right);
        }

        // Returns portal points between two polygons.
        dtStatus getPortalPoints(dtPolyRef from, dtPoly fromPoly, dtMeshTile fromTile, dtPolyRef to, dtPoly toPoly, dtMeshTile toTile, float[] left, float[] right)
        {
            // Find the link that points to the 'to' polygon.
            dtLink link = null;
            for (uint i = fromPoly.firstLink; i != DT_NULL_LINK; i = fromTile.links[i].next)
            {
                if (fromTile.links[i].polyRef == to)
                {
                    link = fromTile.links[i];
                    break;
                }
            }
            if (link == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            // Handle off-mesh connections.
            if (fromPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                // Find link that points to first vertex.
                for (uint i = fromPoly.firstLink; i != DT_NULL_LINK; i = fromTile.links[i].next)
                {
                    if (fromTile.links[i].polyRef == to)
                    {
                        int v = fromTile.links[i].edge;
                        dtVcopy(left, 0, fromTile.verts, fromPoly.verts[v] * 3);
                        dtVcopy(right, 0, fromTile.verts, fromPoly.verts[v] * 3);
                        return DT_SUCCESS;
                    }
                }
                return DT_FAILURE | DT_INVALID_PARAM;
            }

            if (toPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
            {
                for (uint i = toPoly.firstLink; i != DT_NULL_LINK; i = toTile.links[i].next)
                {
                    if (toTile.links[i].polyRef == from)
                    {
                        int v = toTile.links[i].edge;
                        dtVcopy(left, 0, toTile.verts, toPoly.verts[v] * 3);
                        dtVcopy(right, 0, toTile.verts, toPoly.verts[v] * 3);
                        return DT_SUCCESS;
                    }
                }
                return DT_FAILURE | DT_INVALID_PARAM;
            }

            // Find portal vertices.
            int v0 = fromPoly.verts[link.edge];
            int v1 = fromPoly.verts[(link.edge + 1) % (int)fromPoly.vertCount];
            dtVcopy(left, 0, fromTile.verts, v0 * 3);
            dtVcopy(right, 0, fromTile.verts, v1 * 3);

            // If the link is at tile boundary, dtClamp the vertices to
            // the link width.
            if (link.side != 0xff)
            {
                // Unpack portal limits.
                if (link.bmin != 0 || link.bmax != 255)
                {
                    float s = 1.0f / 255.0f;
                    float tmin = link.bmin * s;
                    float tmax = link.bmax * s;
                    dtVlerp(left, 0, fromTile.verts, v0 * 3, fromTile.verts, v1 * 3, tmin);
                    dtVlerp(right, 0, fromTile.verts, v0 * 3, fromTile.verts, v1 * 3, tmax);
                }
            }

            return DT_SUCCESS;
        }

        // Returns edge mid point between two polygons.
        dtStatus getEdgeMidPoint(dtPolyRef from, dtPolyRef to, float[] mid)
        {
            float[] left = new float[3];//, right[3];
            float[] right = new float[3];
            byte fromType = 0, toType = 0;
            if (dtStatusFailed(getPortalPoints(from, to, left, right, ref fromType, ref toType)))
                return DT_FAILURE | DT_INVALID_PARAM;
            mid[0] = (left[0] + right[0]) * 0.5f;
            mid[1] = (left[1] + right[1]) * 0.5f;
            mid[2] = (left[2] + right[2]) * 0.5f;
            return DT_SUCCESS;
        }

        dtStatus getEdgeMidPoint(dtPolyRef from, dtPoly fromPoly, dtMeshTile fromTile, dtPolyRef to, dtPoly toPoly, dtMeshTile toTile, float[] mid)
        {
            float[] left = new float[3];//, right[3];
            float[] right = new float[3];
            if (dtStatusFailed(getPortalPoints(from, fromPoly, fromTile, to, toPoly, toTile, left, right)))
                return DT_FAILURE | DT_INVALID_PARAM;
            mid[0] = (left[0] + right[0]) * 0.5f;
            mid[1] = (left[1] + right[1]) * 0.5f;
            mid[2] = (left[2] + right[2]) * 0.5f;
            return DT_SUCCESS;
        }

        /// @par
        ///
        /// This method is meant to be used for quick, short distance checks.
        ///
        /// If the path array is too small to hold the result, it will be filled as 
        /// far as possible from the start postion toward the end position.
        ///
        /// <b>Using the Hit Parameter (t)</b>
        /// 
        /// If the hit parameter is a very high value (FLT_MAX), then the ray has hit 
        /// the end position. In this case the path represents a valid corridor to the 
        /// end position and the value of @p hitNormal is undefined.
        ///
        /// If the hit parameter is zero, then the start position is on the wall that 
        /// was hit and the value of @p hitNormal is undefined.
        ///
        /// If 0 < t < 1.0 then the following applies:
        ///
        /// @code
        /// distanceToHitBorder = distanceToEndPosition * t
        /// hitPoint = startPos + (endPos - startPos) * t
        /// @endcode
        ///
        /// <b>Use Case Restriction</b>
        ///
        /// The raycast ignores the y-value of the end position. (2D check.) This 
        /// places significant limits on how it can be used. For example:
        ///
        /// Consider a scene where there is a main floor with a second floor balcony 
        /// that hangs over the main floor. So the first floor mesh extends below the 
        /// balcony mesh. The start position is somewhere on the first floor. The end 
        /// position is on the balcony.
        ///
        /// The raycast will search toward the end position along the first floor mesh. 
        /// If it reaches the end position's xz-coordinates it will indicate FLT_MAX
        /// (no wall hit), meaning it reached the end position. This is one example of why
        /// this method is meant for short distance checks.
        ///
        public dtStatus raycast(dtPolyRef startRef, float[] startPos, float[] endPos, dtQueryFilter filter, ref float t, float[] hitNormal, dtPolyRef[] path, ref uint pathCount, int maxPath)
        {
            dtRaycastHit hit = new();
            hit.path = path;
            hit.maxPath = maxPath;

            dtStatus status = raycast(startRef, startPos, endPos, filter, 0, hit);

            t = hit.t;
            dtVcopy(hitNormal, hit.hitNormal);
            pathCount = (uint)hit.pathCount;

            return status;
        }

        /// Casts a 'walkability' ray along the surface of the navigation mesh from 
        /// the start position toward the end position.
        ///  @param[in]		startRef	The reference id of the start polygon.
        ///  @param[in]		startPos	A position within the start polygon representing 
        ///  							the start of the ray. [(x, y, z)]
        ///  @param[in]		endPos		The position to cast the ray toward. [(x, y, z)]
        ///  @param[out]	t			The hit parameter. (FLT_MAX if no wall hit.)
        ///  @param[out]	hitNormal	The normal of the nearest wall hit. [(x, y, z)]
        ///  @param[in]		filter		The polygon filter to apply to the query.
        ///  @param[out]	path		The reference ids of the visited polygons. [opt]
        ///  @param[out]	pathCount	The number of visited polygons. [opt]
        ///  @param[in]		maxPath		The maximum number of polygons the @p path array can hold.
        // @returns The status flags for the query.
        // @par
        ///
        /// This method is meant to be used for quick, short distance checks.
        ///
        /// If the path array is too small to hold the result, it will be filled as 
        /// far as possible from the start postion toward the end position.
        ///
        /// <b>Using the Hit Parameter (t)</b>
        /// 
        /// If the hit parameter is a very high value (FLT_MAX), then the ray has hit 
        /// the end position. In this case the path represents a valid corridor to the 
        /// end position and the value of @p hitNormal is undefined.
        ///
        /// If the hit parameter is zero, then the start position is on the wall that 
        /// was hit and the value of @p hitNormal is undefined.
        ///
        /// If 0 &lt; t &lt; 1.0 then the following applies:
        ///
        // @code
        /// distanceToHitBorder = distanceToEndPosition * t
        /// hitPoint = startPos + (endPos - startPos) * t
        // @endcode
        ///
        /// <b>Use Case Restriction</b>
        ///
        /// The raycast ignores the y-value of the end position. (2D check.) This 
        /// places significant limits on how it can be used. For example:
        ///
        /// Consider a scene where there is a main floor with a second floor balcony 
        /// that hangs over the main floor. So the first floor mesh extends below the 
        /// balcony mesh. The start position is somewhere on the first floor. The end 
        /// position is on the balcony.
        ///
        /// The raycast will search toward the end position along the first floor mesh. 
        /// If it reaches the end position's xz-coordinates it will indicate FLT_MAX
        /// (no wall hit), meaning it reached the end position. This is one example of why
        /// this method is meant for short distance checks.
        ///
        public dtStatus raycast(dtPolyRef startRef, float[] startPos, float[] endPos, dtQueryFilter filter, uint options, dtRaycastHit hit, dtPolyRef prevRef = 0)
        {
            Debug.Assert(m_nav != null);

            if (hit == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            hit.t = 0;
            hit.pathCount = 0;
            hit.pathCost = 0;

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) ||
                startPos == null || !dtVisfinite(startPos) ||
                endPos == null || !dtVisfinite(endPos) ||
                filter == null ||
                (prevRef != 0 && !m_nav.isValidPolyRef(prevRef)))
                return DT_FAILURE | DT_INVALID_PARAM;

            float[] dir = new float[3];
            float[] curPos = new float[3];
            float[] lastPos = new float[3];
            float[] verts = new float[DT_VERTS_PER_POLYGON * 3 + 3];
            int n = 0;

            dtVcopy(curPos, startPos);
            dtVsub(dir, endPos, startPos);
            dtVset(hit.hitNormal, 0, 0, 0);

            dtStatus status = DT_SUCCESS;

            dtMeshTile prevTile = new();
            dtMeshTile nextTile;
            dtPoly prevPoly = new();
            dtPoly nextPoly;
            dtPolyRef curRef;

            // The API input has been checked already, skip checking internal data.
            curRef = startRef;
            dtMeshTile tile = new();
            dtPoly poly = new();
            m_nav.getTileAndPolyByRefUnsafe(curRef, ref tile, ref poly);
            nextTile = prevTile = tile;
            nextPoly = prevPoly = poly;
            if (prevRef != 0)
                m_nav.getTileAndPolyByRefUnsafe(prevRef, ref prevTile, ref prevPoly);

            while (curRef != 0)
            {
                // Cast ray against current polygon.

                // Collect vertices.
                int nv = 0;
                for (int i = 0; i < (int)poly.vertCount; ++i)
                {
                    dtVcopy(verts, nv * 3, tile.verts, poly.verts[i] * 3);
                    nv++;
                }

                float tmin = 0, tmax = 0;
                int segMin = 0, segMax = 0;
                if (!dtIntersectSegmentPoly2D(startPos, endPos, verts, nv, ref tmin, ref tmax, ref segMin, ref segMax))
                {
                    // Could not hit the polygon, keep the old t and report hit.
                    hit.pathCount = n;
                    return status;
                }

                hit.hitEdgeIndex = segMax;

                // Keep track of furthest t so far.
                if (tmax > hit.t)
                    hit.t = tmax;

                // Store visited polygons.
                if (n < hit.maxPath)
                    hit.path[n++] = curRef;
                else
                    status |= DT_BUFFER_TOO_SMALL;

                // Ray end is completely inside the polygon.
                if (segMax == -1)
                {
                    hit.t = float.MaxValue;
                    hit.pathCount = n;

                    // add the cost
                    if ((options & (int)dtRaycastOptions.DT_RAYCAST_USE_COSTS) != 0)
                        hit.pathCost += filter.getCost(curPos, endPos, prevRef, prevTile, prevPoly, curRef, tile, poly, curRef, tile, poly);
                    return status;
                }

                // Follow neighbours.
                dtPolyRef nextRef = 0;

                for (uint i = poly.firstLink; i != DT_NULL_LINK; i = tile.links[i].next)
                {
                    dtLink link = tile.links[i];

                    // Find link which contains this edge.
                    if ((int)link.edge != segMax)
                        continue;

                    // Get pointer to the next polygon.
                    nextTile = new dtMeshTile();
                    nextPoly = new dtPoly();
                    m_nav.getTileAndPolyByRefUnsafe(link.polyRef, ref nextTile, ref nextPoly);

                    // Skip off-mesh connections.
                    if (nextPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                        continue;

                    // Skip links based on filter.
                    if (!filter.passFilter(link.polyRef, nextTile, nextPoly))
                        continue;

                    // If the link is internal, just return the ref.
                    if (link.side == 0xff)
                    {
                        nextRef = link.polyRef;
                        break;
                    }

                    // If the link is at tile boundary,

                    // Check if the link spans the whole edge, and accept.
                    if (link.bmin == 0 && link.bmax == 255)
                    {
                        nextRef = link.polyRef;
                        break;
                    }

                    // Check for partial edge links.
                    int v0 = poly.verts[link.edge];
                    int v1 = poly.verts[(link.edge + 1) % poly.vertCount];
                    //const float* left = &tile.verts[v0*3];
                    //const float* right = &tile.verts[v1*3];
                    int leftStart = v0 * 3;
                    int rightStart = v1 * 3;

                    // Check that the intersection lies inside the link portal.
                    if (link.side == 0 || link.side == 4)
                    {
                        // Calculate link size.
                        const float s = 1.0f / 255.0f;
                        float lmin = tile.verts[leftStart + 2] + (tile.verts[rightStart + 2] - tile.verts[leftStart + 2]) * (link.bmin * s);
                        float lmax = tile.verts[leftStart + 2] + (tile.verts[rightStart + 2] - tile.verts[leftStart + 2]) * (link.bmax * s);
                        if (lmin > lmax)
                            dtSwap(ref lmin, ref lmax);

                        // Find Z intersection.
                        float z = startPos[2] + (endPos[2] - startPos[2]) * tmax;
                        if (z >= lmin && z <= lmax)
                        {
                            nextRef = link.polyRef;
                            break;
                        }
                    }
                    else if (link.side == 2 || link.side == 6)
                    {
                        // Calculate link size.
                        const float s = 1.0f / 255.0f;
                        float lmin = tile.verts[leftStart + 0] + (tile.verts[rightStart + 0] - tile.verts[leftStart + 0]) * (link.bmin * s);
                        float lmax = tile.verts[leftStart + 0] + (tile.verts[rightStart + 0] - tile.verts[leftStart + 0]) * (link.bmax * s);
                        if (lmin > lmax)
                            dtSwap(ref lmin, ref lmax);

                        // Find X intersection.
                        float x = startPos[0] + (endPos[0] - startPos[0]) * tmax;
                        if (x >= lmin && x <= lmax)
                        {
                            nextRef = link.polyRef;
                            break;
                        }
                    }
                }

                // add the cost
                if ((options & (int)dtRaycastOptions.DT_RAYCAST_USE_COSTS) != 0)
                {
                    // compute the intersection point at the furthest end of the polygon
                    // and correct the height (since the raycast moves in 2d)
                    dtVcopy(lastPos, curPos);
                    dtVmad(curPos, startPos, dir, hit.t);
                    int e1Start = segMax * 3;
                    int e2Start = ((segMax + 1) % nv) * 3;
                    float[] eDir = new float[3];
                    float[] diff = new float[3];
                    dtVsub(eDir, 0, verts, e2Start, verts, e1Start);
                    dtVsub(diff, 0, curPos, 0, verts, e1Start);
                    float s = dtSqr(eDir[0]) > dtSqr(eDir[2]) ? diff[0] / eDir[0] : diff[2] / eDir[2];
                    curPos[1] = verts[e1Start + 1] + eDir[1] * s;

                    hit.pathCost += filter.getCost(lastPos, curPos, prevRef, prevTile, prevPoly, curRef, tile, poly, nextRef, nextTile, nextPoly);
                }

                if (nextRef == 0)
                {
                    // No neighbour, we hit a wall.

                    // Calculate hit normal.
                    int a = segMax;
                    int b = segMax + 1 < nv ? segMax + 1 : 0;
                    //const float* va = &verts[a*3];
                    //const float* vb = &verts[b*3];
                    int vaStart = a * 3;
                    int vbStart = b * 3;
                    float dx = verts[vbStart + 0] - verts[vaStart + 0];
                    float dz = verts[vbStart + 2] - verts[vaStart + 2];
                    hit.hitNormal[0] = dz;
                    hit.hitNormal[1] = 0;
                    hit.hitNormal[2] = -dx;
                    dtVnormalize(hit.hitNormal);

                    hit.pathCount = n;
                    return status;
                }

                // No hit, advance to neighbour polygon.
                prevRef = curRef;
                curRef = nextRef;
                prevTile = tile;
                tile = nextTile;
                prevPoly = poly;
                poly = nextPoly;
            }

            hit.pathCount = n;

            return status;
        }

        ///@}
        // @name Dijkstra Search Functions
        // @{ 

        /// Finds the polygons along the navigation graph that touch the specified circle.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		radius			The radius of the search circle.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultRef		The reference ids of the polygons touched by the circle. [opt]
        ///  @param[out]	resultParent	The reference ids of the parent polygons for each result. 
        ///  								Zero if a result polygon has no parent. [opt]
        ///  @param[out]	resultCost		The search cost from @p centerPos to the polygon. [opt]
        ///  @param[out]	resultCount		The number of polygons found. [opt]
        ///  @param[in]		maxResult		The maximum number of polygons the result arrays can hold.
        // @returns The status flags for the query.
        // @par
        ///
        /// At least one result array must be provided.
        ///
        /// The order of the result set is from least to highest cost to reach the polygon.
        ///
        /// A common use case for this method is to perform Dijkstra searches. 
        /// Candidate polygons are found by searching the graph beginning at the start polygon.
        ///
        /// If a polygon is not found via the graph search, even if it intersects the 
        /// search circle, it will not be included in the result set. For example:
        ///
        /// polyA is the start polygon.
        /// polyB shares an edge with polyA. (Is adjacent.)
        /// polyC shares an edge with polyB, but not with polyA
        /// Even if the search circle overlaps polyC, it will not be included in the 
        /// result set unless polyB is also in the set.
        /// 
        /// The value of the center point is used as the start position for cost 
        /// calculations. It is not projected onto the surface of the mesh, so its 
        /// y-value will effect the costs.
        ///
        /// Intersection tests occur in 2D. All polygons and the search circle are 
        /// projected onto the xz-plane. So the y-value of the center point does not 
        /// effect intersection tests.
        ///
        /// If the result arrays are to small to hold the entire result set, they will be 
        /// filled to capacity.
        /// 
        dtStatus findPolysAroundCircle(dtPolyRef startRef, float[] centerPos, float radius, dtQueryFilter filter, dtPolyRef[] resultRef, dtPolyRef[] resultParent, float[] resultCost, ref int resultCount, int maxResult)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_nodePool != null);
            Debug.Assert(m_openList != null);

            resultCount = 0;

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) ||
                centerPos == null || !dtVisfinite(centerPos) ||
                radius < 0 || !float.IsFinite(radius) ||
                filter == null || maxResult < 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            m_nodePool.clear();
            m_openList.clear();

            dtNode startNode = m_nodePool.getNode(startRef);
            dtVcopy(startNode.pos, centerPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_openList.push(startNode);

            dtStatus status = DT_SUCCESS;

            int n = 0;
            if (n < maxResult)
            {
                if (resultRef != null)
                    resultRef[n] = startNode.id;
                if (resultParent != null)
                    resultParent[n] = 0;
                if (resultCost != null)
                    resultCost[n] = 0;
                ++n;
            }
            else
            {
                status |= DT_BUFFER_TOO_SMALL;
            }

            float radiusSqr = dtSqr(radius);

            while (!m_openList.empty())
            {
                dtNode bestNode = m_openList.pop();
                //bestNode.flags &= ~DT_NODE_OPEN;
                //bestNode.flags |= DT_NODE_CLOSED;
                bestNode.dtcsClearFlag(dtNodeFlags.DT_NODE_OPEN);
                bestNode.dtcsSetFlag(dtNodeFlags.DT_NODE_CLOSED);

                // Get poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef bestRef = bestNode.id;
                dtMeshTile bestTile = null;
                dtPoly bestPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(bestRef, ref bestTile, ref bestPoly);

                // Get parent poly and tile.
                dtPolyRef parentRef = 0;
                dtMeshTile parentTile = null;
                dtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                    parentRef = m_nodePool.getNodeAtIdx(bestNode.pidx).id;
                if (parentRef != 0)
                    m_nav.getTileAndPolyByRefUnsafe(parentRef, ref parentTile, ref parentPoly);

                for (uint i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    dtLink link = bestTile.links[i];
                    dtPolyRef neighbourRef = link.polyRef;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                        continue;

                    // Expand to neighbour
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    // Find edge and calc distance to the edge.
                    float[] va = new float[3];//, vb[3];
                    float[] vb = new float[3];
                    if (getPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, va, vb) == 0)
                        continue;

                    // If the circle is not touching the next polygon, skip it.
                    float tseg = 0.0f;
                    float distSqr = dtDistancePtSegSqr2D(centerPos, 0, va, 0, vb, 0, ref tseg);
                    if (distSqr > radiusSqr)
                        continue;

                    dtNode neighbourNode = m_nodePool.getNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DT_OUT_OF_NODES;
                        continue;
                    }

                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_CLOSED))
                        continue;

                    // Cost
                    if (neighbourNode.flags == 0)
                        dtVlerp(neighbourNode.pos, va, vb, 0.5f);

                    float total = bestNode.total + dtVdist(bestNode.pos, neighbourNode.pos);

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_OPEN)) && total >= neighbourNode.total)
                        continue;

                    neighbourNode.id = neighbourRef;
                    //neighbourNode.flags = (neighbourNode.flags & ~DT_NODE_CLOSED);
                    neighbourNode.dtcsClearFlag(dtNodeFlags.DT_NODE_CLOSED);
                    neighbourNode.pidx = m_nodePool.getNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_OPEN))
                    {
                        m_openList.modify(neighbourNode);
                    }
                    else
                    {
                        if (n < maxResult)
                        {
                            if (resultRef != null)
                                resultRef[n] = neighbourNode.id;
                            if (resultParent != null)
                                resultParent[n] = m_nodePool.getNodeAtIdx(neighbourNode.pidx).id;
                            if (resultCost != null)
                                resultCost[n] = neighbourNode.total;
                            ++n;
                        }
                        else
                        {
                            status |= DT_BUFFER_TOO_SMALL;
                        }
                        neighbourNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
                        m_openList.push(neighbourNode);
                    }
                }
            }

            resultCount = n;

            return status;
        }

        /// Finds the polygons along the naviation graph that touch the specified convex polygon.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		verts			The vertices describing the convex polygon. (CCW) 
        ///  								[(x, y, z) * @p nverts]
        ///  @param[in]		nverts			The number of vertices in the polygon.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultRef		The reference ids of the polygons touched by the search polygon. [opt]
        ///  @param[out]	resultParent	The reference ids of the parent polygons for each result. Zero if a 
        ///  								result polygon has no parent. [opt]
        ///  @param[out]	resultCost		The search cost from the centroid point to the polygon. [opt]
        ///  @param[out]	resultCount		The number of polygons found.
        ///  @param[in]		maxResult		The maximum number of polygons the result arrays can hold.
        // @returns The status flags for the query.
        // @par
        ///
        /// The order of the result set is from least to highest cost.
        /// 
        /// At least one result array must be provided.
        ///
        /// A common use case for this method is to perform Dijkstra searches. 
        /// Candidate polygons are found by searching the graph beginning at the start 
        /// polygon.
        /// 
        /// The same intersection test restrictions that apply to findPolysAroundCircle()
        /// method apply to this method.
        /// 
        /// The 3D centroid of the search polygon is used as the start position for cost 
        /// calculations.
        /// 
        /// Intersection tests occur in 2D. All polygons are projected onto the 
        /// xz-plane. So the y-values of the vertices do not effect intersection tests.
        /// 
        /// If the result arrays are is too small to hold the entire result set, they will 
        /// be filled to capacity.
        ///
        dtStatus findPolysAroundShape(dtPolyRef startRef, float[] verts, int nverts, dtQueryFilter filter, dtPolyRef[] resultRef, dtPolyRef[] resultParent, float[] resultCost, ref int resultCount, int maxResult)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_nodePool != null);
            Debug.Assert(m_openList != null);

            resultCount = 0;

            if (!m_nav.isValidPolyRef(startRef) ||
                verts == null || nverts < 3 ||
                filter == null || maxResult < 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            // Validate input
            if (startRef == 0 || !m_nav.isValidPolyRef(startRef))
                return DT_FAILURE | DT_INVALID_PARAM;

            m_nodePool.clear();
            m_openList.clear();

            float[] centerPos = new float[] { 0, 0, 0 };
            for (int i = 0; i < nverts; ++i)
            {
                dtVadd(centerPos, 0, centerPos, 0, verts, i * 3);
            }
            dtVscale(centerPos, centerPos, 1.0f / nverts);

            dtNode startNode = m_nodePool.getNode(startRef);
            dtVcopy(startNode.pos, centerPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_openList.push(startNode);

            dtStatus status = DT_SUCCESS;

            int n = 0;
            if (n < maxResult)
            {
                if (resultRef != null)
                    resultRef[n] = startNode.id;
                if (resultParent != null)
                    resultParent[n] = 0;
                if (resultCost != null)
                    resultCost[n] = 0;
                ++n;
            }
            else
            {
                status |= DT_BUFFER_TOO_SMALL;
            }

            while (!m_openList.empty())
            {
                dtNode bestNode = m_openList.pop();
                //bestNode.flags &= ~DT_NODE_OPEN;
                //bestNode.flags |= DT_NODE_CLOSED;
                bestNode.dtcsClearFlag(dtNodeFlags.DT_NODE_OPEN);
                bestNode.dtcsSetFlag(dtNodeFlags.DT_NODE_CLOSED);

                // Get poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef bestRef = bestNode.id;
                dtMeshTile bestTile = null;
                dtPoly bestPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(bestRef, ref bestTile, ref bestPoly);

                // Get parent poly and tile.
                dtPolyRef parentRef = 0;
                dtMeshTile parentTile = null;
                dtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                    parentRef = m_nodePool.getNodeAtIdx(bestNode.pidx).id;
                if (parentRef != 0)
                    m_nav.getTileAndPolyByRefUnsafe(parentRef, ref parentTile, ref parentPoly);

                for (uint i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    dtLink link = bestTile.links[i];
                    dtPolyRef neighbourRef = link.polyRef;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef == 0 || neighbourRef == parentRef)
                        continue;

                    // Expand to neighbour
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    // Find edge and calc distance to the edge.
                    float[] va = new float[3];//, vb[3];
                    float[] vb = new float[3];
                    if (getPortalPoints(bestRef, bestPoly, bestTile, neighbourRef, neighbourPoly, neighbourTile, va, vb) == 0)
                        continue;

                    // If the poly is not touching the edge to the next polygon, skip the connection it.
                    float tmin = 0, tmax = 0;
                    int segMin = 0, segMax = 0;
                    if (dtIntersectSegmentPoly2D(va, vb, verts, nverts, ref tmin, ref tmax, ref segMin, ref segMax))
                        continue;
                    if (tmin > 1.0f || tmax < 0.0f)
                        continue;

                    dtNode neighbourNode = m_nodePool.getNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DT_OUT_OF_NODES;
                        continue;
                    }

                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_CLOSED))
                        continue;

                    // Cost
                    if (neighbourNode.flags == 0)
                        dtVlerp(neighbourNode.pos, va, vb, 0.5f);

                    float total = bestNode.total + dtVdist(bestNode.pos, neighbourNode.pos);

                    // The node is already in open list and the new result is worse, skip.
                    if ((neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_OPEN)) && total >= neighbourNode.total)
                        continue;

                    neighbourNode.id = neighbourRef;
                    neighbourNode.dtcsClearFlag(dtNodeFlags.DT_NODE_CLOSED);// = (neighbourNode.flags & ~DT_NODE_CLOSED);
                    neighbourNode.pidx = m_nodePool.getNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_OPEN))// .flags & DT_NODE_OPEN)
                    {
                        m_openList.modify(neighbourNode);
                    }
                    else
                    {
                        if (n < maxResult)
                        {
                            if (resultRef != null)
                                resultRef[n] = neighbourNode.id;
                            if (resultParent != null)
                                resultParent[n] = m_nodePool.getNodeAtIdx(neighbourNode.pidx).id;
                            if (resultCost != null)
                                resultCost[n] = neighbourNode.total;
                            ++n;
                        }
                        else
                        {
                            status |= DT_BUFFER_TOO_SMALL;
                        }
                        neighbourNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
                        m_openList.push(neighbourNode);
                    }
                }
            }

            resultCount = n;

            return status;
        }

        /// Finds the non-overlapping navigation polygons in the local neighbourhood around the center position.
        ///  @param[in]		startRef		The reference id of the polygon where the search starts.
        ///  @param[in]		centerPos		The center of the query circle. [(x, y, z)]
        ///  @param[in]		radius			The radius of the query circle.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	resultRef		The reference ids of the polygons touched by the circle.
        ///  @param[out]	resultParent	The reference ids of the parent polygons for each result. 
        ///  								Zero if a result polygon has no parent. [opt]
        ///  @param[out]	resultCount		The number of polygons found.
        ///  @param[in]		maxResult		The maximum number of polygons the result arrays can hold.
        // @returns The status flags for the query.
        // @par
        ///
        /// This method is optimized for a small search radius and small number of result 
        /// polygons.
        ///
        /// Candidate polygons are found by searching the navigation graph beginning at 
        /// the start polygon.
        ///
        /// The same intersection test restrictions that apply to the findPolysAroundCircle 
        /// mehtod applies to this method.
        ///
        /// The value of the center point is used as the start point for cost calculations. 
        /// It is not projected onto the surface of the mesh, so its y-value will effect 
        /// the costs.
        /// 
        /// Intersection tests occur in 2D. All polygons and the search circle are 
        /// projected onto the xz-plane. So the y-value of the center point does not 
        /// effect intersection tests.
        /// 
        /// If the result arrays are is too small to hold the entire result set, they will 
        /// be filled to capacity.
        /// 
        dtStatus findLocalNeighbourhood(dtPolyRef startRef, float[] centerPos, float radius, dtQueryFilter filter, dtPolyRef[] resultRef, dtPolyRef[] resultParent, ref int resultCount, int maxResult)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_tinyNodePool != null);

            resultCount = 0;

            if (!m_nav.isValidPolyRef(startRef) ||
                centerPos == null || !dtVisfinite(centerPos) ||
                radius < 0 || !float.IsFinite(radius) ||
                filter == null || maxResult < 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            const int MAX_STACK = 48;
            dtNode[] stack = new dtNode[MAX_STACK];
            dtcsArrayItemsCreate(stack);
            int nstack = 0;

            m_tinyNodePool.clear();

            dtNode startNode = m_tinyNodePool.getNode(startRef);
            startNode.pidx = 0;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_CLOSED;
            stack[nstack++] = startNode;

            float radiusSqr = dtSqr(radius);

            float[] pa = new float[DT_VERTS_PER_POLYGON * 3];
            float[] pb = new float[DT_VERTS_PER_POLYGON * 3];

            dtStatus status = DT_SUCCESS;

            int n = 0;
            if (n < maxResult)
            {
                resultRef[n] = startNode.id;
                if (resultParent != null)
                    resultParent[n] = 0;
                ++n;
            }
            else
            {
                status |= DT_BUFFER_TOO_SMALL;
            }

            while (nstack != 0)
            {
                // Pop front.
                dtNode curNode = stack[0];
                for (int i = 0; i < nstack - 1; ++i)
                    stack[i] = stack[i + 1];
                nstack--;

                // Get poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef curRef = curNode.id;
                dtMeshTile curTile = null;
                dtPoly curPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(curRef, ref curTile, ref curPoly);

                for (uint i = curPoly.firstLink; i != DT_NULL_LINK; i = curTile.links[i].next)
                {
                    dtLink link = curTile.links[i];
                    dtPolyRef neighbourRef = link.polyRef;
                    // Skip invalid neighbours.
                    if (neighbourRef == 0)
                        continue;

                    // Skip if cannot alloca more nodes.
                    dtNode neighbourNode = m_tinyNodePool.getNode(neighbourRef);
                    if (neighbourNode == null)
                        continue;
                    // Skip visited.
                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_CLOSED))// .flags & DT_NODE_CLOSED)
                        continue;

                    // Expand to neighbour
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    // Skip off-mesh connections.
                    if (neighbourPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                        continue;

                    // Do not advance if the polygon is excluded by the filter.
                    if (!filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    // Find edge and calc distance to the edge.
                    float[] va = new float[3];//, vb[3];
                    float[] vb = new float[3];
                    if (getPortalPoints(curRef, curPoly, curTile, neighbourRef, neighbourPoly, neighbourTile, va, vb) == 0)
                        continue;

                    // If the circle is not touching the next polygon, skip it.
                    float tseg = .0f;
                    float distSqr = dtDistancePtSegSqr2D(centerPos, 0, va, 0, vb, 0, ref tseg);
                    if (distSqr > radiusSqr)
                        continue;

                    // Mark node visited, this is done before the overlap test so that
                    // we will not visit the poly again if the test fails.
                    //neighbourNode.flags |= DT_NODE_CLOSED;
                    neighbourNode.dtcsSetFlag(dtNodeFlags.DT_NODE_CLOSED);
                    neighbourNode.pidx = m_tinyNodePool.getNodeIdx(curNode);

                    // Check that the polygon does not collide with existing polygons.

                    // Collect vertices of the neighbour poly.
                    int npa = neighbourPoly.vertCount;
                    for (int k = 0; k < npa; ++k)
                    {
                        dtVcopy(pa, k * 3, neighbourTile.verts, neighbourPoly.verts[k] * 3);
                    }

                    bool overlap = false;
                    for (int j = 0; j < n; ++j)
                    {
                        dtPolyRef pastRef = resultRef[j];

                        // Connected polys do not overlap.
                        bool connected = false;
                        for (uint k = curPoly.firstLink; k != DT_NULL_LINK; k = curTile.links[k].next)
                        {
                            if (curTile.links[k].polyRef == pastRef)
                            {
                                connected = true;
                                break;
                            }
                        }
                        if (connected)
                            continue;

                        // Potentially overlapping.
                        dtMeshTile pastTile = null;
                        dtPoly pastPoly = null;
                        m_nav.getTileAndPolyByRefUnsafe(pastRef, ref pastTile, ref pastPoly);

                        // Get vertices and test overlap
                        int npb = pastPoly.vertCount;
                        for (int k = 0; k < npb; ++k)
                        {
                            dtVcopy(pb, k * 3, pastTile.verts, pastPoly.verts[k] * 3);
                        }

                        if (dtOverlapPolyPoly2D(pa, npa, pb, npb))
                        {
                            overlap = true;
                            break;
                        }
                    }
                    if (overlap)
                        continue;

                    // This poly is fine, store and advance to the poly.
                    if (n < maxResult)
                    {
                        resultRef[n] = neighbourRef;
                        if (resultParent != null)
                            resultParent[n] = curRef;
                        ++n;
                    }
                    else
                    {
                        status |= DT_BUFFER_TOO_SMALL;
                    }

                    if (nstack < MAX_STACK)
                    {
                        stack[nstack++] = neighbourNode;
                    }
                }
            }

            resultCount = n;

            return status;
        }

        class dtSegInterval
        {
            public dtPolyRef polyRef;
            public short tmin;
            public short tmax;
        };

        static void insertInterval(dtSegInterval[] ints, ref int nints, int maxInts, short tmin, short tmax, dtPolyRef polyRef)
        {
            if (nints + 1 > maxInts)
                return;
            // Find insertion point.
            int idx = 0;
            while (idx < nints)
            {
                if (tmax <= ints[idx].tmin)
                    break;
                idx++;
            }
            // Move current results.
            if (nints - idx != 0)
            {
                //memmove(ints+idx+1, ints+idx, sizeof(dtSegInterval)*(nints-idx));
                for (int i = 0; i < (nints - idx); ++i)
                {
                    ints[idx + 1 + i] = ints[idx + i];
                }
            }
            // Store
            ints[idx].polyRef = polyRef;
            ints[idx].tmin = tmin;
            ints[idx].tmax = tmax;
            nints++;
        }

        /// Returns the segments for the specified polygon, optionally including portals.
        ///  @param[in]		ref				The reference id of the polygon.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	segmentVerts	The segments. [(ax, ay, az, bx, by, bz) * segmentCount]
        ///  @param[out]	segmentRefs		The reference ids of each segment's neighbor polygon. 
        ///  								Or zero if the segment is a wall. [opt] [(parentRef) * @p segmentCount] 
        ///  @param[out]	segmentCount	The number of segments returned.
        ///  @param[in]		maxSegments		The maximum number of segments the result arrays can hold.
        // @returns The status flags for the query.
        // @par
        ///
        /// If the @p segmentRefs parameter is provided, then all polygon segments will be returned. 
        /// Otherwise only the wall segments are returned.
        /// 
        /// A segment that is normally a portal will be included in the result set as a 
        /// wall if the @p filter results in the neighbor polygon becoomming impassable.
        /// 
        /// The @p segmentVerts and @p segmentRefs buffers should normally be sized for the 
        /// maximum segments per polygon of the source navigation mesh.
        /// 
        dtStatus getPolyWallSegments(dtPolyRef polyRef, dtQueryFilter filter, float[] segmentVerts, dtPolyRef[] segmentRefs, ref int segmentCount, int maxSegments)
        {
            Debug.Assert(m_nav != null);

            segmentCount = 0;

            dtMeshTile tile = null;
            dtPoly poly = null;
            if (dtStatusFailed(m_nav.getTileAndPolyByRef(polyRef, ref tile, ref poly)))
                return DT_FAILURE | DT_INVALID_PARAM;

            if (filter == null || segmentVerts == null || maxSegments < 0)
                return DT_FAILURE | DT_INVALID_PARAM;

            int n = 0;
            const int MAX_INTERVAL = 16;
            dtSegInterval[] ints = new dtSegInterval[MAX_INTERVAL];
            dtcsArrayItemsCreate(ints);
            int nints;

            bool storePortals = segmentRefs != null;

            dtStatus status = DT_SUCCESS;

            for (int i = 0, j = (int)poly.vertCount - 1; i < (int)poly.vertCount; j = i++)
            {
                // Skip non-solid edges.
                nints = 0;
                if ((poly.neis[j] & DT_EXT_LINK) != 0)
                {
                    // Tile border.
                    for (uint k = poly.firstLink; k != DT_NULL_LINK; k = tile.links[k].next)
                    {
                        dtLink link = tile.links[k];
                        if (link.edge == j)
                        {
                            if (link.polyRef != 0)
                            {
                                dtMeshTile neiTile = null;
                                dtPoly neiPoly = null;
                                m_nav.getTileAndPolyByRefUnsafe(link.polyRef, ref neiTile, ref neiPoly);
                                if (filter.passFilter(link.polyRef, neiTile, neiPoly))
                                {
                                    insertInterval(ints, ref nints, MAX_INTERVAL, link.bmin, link.bmax, link.polyRef);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Internal edge
                    dtPolyRef neiRef = 0;
                    if (poly.neis[j] != 0)
                    {
                        uint idx = (uint)(poly.neis[j] - 1);
                        neiRef = m_nav.getPolyRefBase(tile) | idx;
                        if (!filter.passFilter(neiRef, tile, tile.polys[idx]))
                            neiRef = 0;
                    }

                    // If the edge leads to another polygon and portals are not stored, skip.
                    if (neiRef != 0 && !storePortals)
                        continue;

                    if (n < maxSegments)
                    {
                        //const float* vj = &tile.verts[poly.verts[j]*3];
                        //const float* vi = &tile.verts[poly.verts[i]*3];
                        //float* seg = &segmentVerts[n*6];
                        int vjStart = poly.verts[j] * 3;
                        int viStart = poly.verts[i] * 3;
                        int segStart = n * 6;
                        dtVcopy(segmentVerts, segStart, tile.verts, vjStart);
                        dtVcopy(segmentVerts, segStart + 3, tile.verts, viStart);
                        if (segmentRefs != null)
                            segmentRefs[n] = neiRef;
                        n++;
                    }
                    else
                    {
                        status |= DT_BUFFER_TOO_SMALL;
                    }

                    continue;
                }

                // Add sentinels
                insertInterval(ints, ref nints, MAX_INTERVAL, -1, 0, 0);
                insertInterval(ints, ref nints, MAX_INTERVAL, 255, 256, 0);

                // Store segments.
                //const float* vj = &tile.verts[poly.verts[j]*3];
                //const float* vi = &tile.verts[poly.verts[i]*3];
                int vjStart2 = poly.verts[j] * 3;
                int viStart2 = poly.verts[i] * 3;
                for (int k = 1; k < nints; ++k)
                {
                    // Portal segment.
                    if (storePortals && ints[k].polyRef != 0)
                    {
                        float tmin = ints[k].tmin / 255.0f;
                        float tmax = ints[k].tmax / 255.0f;
                        if (n < maxSegments)
                        {
                            //float* seg = &segmentVerts[n*6];
                            int segStart = n * 6;
                            dtVlerp(segmentVerts, segStart, tile.verts, vjStart2, tile.verts, viStart2, tmin);
                            dtVlerp(segmentVerts, segStart + 3, tile.verts, vjStart2, tile.verts, viStart2, tmax);
                            if (segmentRefs != null)
                                segmentRefs[n] = ints[k].polyRef;
                            n++;
                        }
                        else
                        {
                            status |= DT_BUFFER_TOO_SMALL;
                        }
                    }

                    // Wall segment.
                    int imin = ints[k - 1].tmax;
                    int imax = ints[k].tmin;
                    if (imin != imax)
                    {
                        float tmin = imin / 255.0f;
                        float tmax = imax / 255.0f;
                        if (n < maxSegments)
                        {
                            //float* seg = &segmentVerts[n*6];
                            int segStart = n * 6;
                            dtVlerp(segmentVerts, segStart, tile.verts, vjStart2, tile.verts, viStart2, tmin);
                            dtVlerp(segmentVerts, segStart + 3, tile.verts, vjStart2, tile.verts, viStart2, tmax);
                            if (segmentRefs != null)
                                segmentRefs[n] = 0;
                            n++;
                        }
                        else
                        {
                            status |= DT_BUFFER_TOO_SMALL;
                        }
                    }
                }
            }

            segmentCount = n;

            return status;
        }

        /// Finds the distance from the specified position to the nearest polygon wall.
        ///  @param[in]		startRef		The reference id of the polygon containing @p centerPos.
        ///  @param[in]		centerPos		The center of the search circle. [(x, y, z)]
        ///  @param[in]		maxRadius		The radius of the search circle.
        ///  @param[in]		filter			The polygon filter to apply to the query.
        ///  @param[out]	hitDist			The distance to the nearest wall from @p centerPos.
        ///  @param[out]	hitPos			The nearest position on the wall that was hit. [(x, y, z)]
        ///  @param[out]	hitNormal		The normalized ray formed from the wall point to the 
        ///  								source point. [(x, y, z)]
        // @returns The status flags for the query.
        // @par
        ///
        // @p hitPos is not adjusted using the height detail data.
        ///
        // @p hitDist will equal the search radius if there is no wall within the 
        /// radius. In this case the values of @p hitPos and @p hitNormal are
        /// undefined.
        ///
        /// The normal will become unpredicable if @p hitDist is a very small number.
        ///
        dtStatus findDistanceToWall(dtPolyRef startRef, float[] centerPos, float maxRadius, dtQueryFilter filter, ref float hitDist, float[] hitPos, float[] hitNormal)
        {
            Debug.Assert(m_nav != null);
            Debug.Assert(m_nodePool != null);
            Debug.Assert(m_openList != null);

            // Validate input
            if (!m_nav.isValidPolyRef(startRef) ||
                centerPos == null || !dtVisfinite(centerPos) ||
                maxRadius < 0 || !float.IsFinite(maxRadius) ||
                filter == null || hitPos == null || hitNormal == null)
                return DT_FAILURE | DT_INVALID_PARAM;

            m_nodePool.clear();
            m_openList.clear();

            dtNode startNode = m_nodePool.getNode(startRef);
            dtVcopy(startNode.pos, centerPos);
            startNode.pidx = 0;
            startNode.cost = 0;
            startNode.total = 0;
            startNode.id = startRef;
            startNode.flags = (byte)dtNodeFlags.DT_NODE_OPEN;
            m_openList.push(startNode);

            float radiusSqr = dtSqr(maxRadius);

            dtStatus status = DT_SUCCESS;

            while (!m_openList.empty())
            {
                dtNode bestNode = m_openList.pop();
                //bestNode.flags &= ~DT_NODE_OPEN;
                //bestNode.flags |= DT_NODE_CLOSED;
                bestNode.dtcsClearFlag(dtNodeFlags.DT_NODE_OPEN);
                bestNode.dtcsSetFlag(dtNodeFlags.DT_NODE_CLOSED);

                // Get poly and tile.
                // The API input has been cheked already, skip checking internal data.
                dtPolyRef bestRef = bestNode.id;
                dtMeshTile bestTile = null;
                dtPoly bestPoly = null;
                m_nav.getTileAndPolyByRefUnsafe(bestRef, ref bestTile, ref bestPoly);

                // Get parent poly and tile.
                dtPolyRef parentRef = 0;
                dtMeshTile parentTile = null;
                dtPoly parentPoly = null;
                if (bestNode.pidx != 0)
                    parentRef = m_nodePool.getNodeAtIdx(bestNode.pidx).id;
                if (parentRef != 0)
                    m_nav.getTileAndPolyByRefUnsafe(parentRef, ref parentTile, ref parentPoly);

                // Hit test walls.
                for (int i = 0, j = (int)bestPoly.vertCount - 1; i < (int)bestPoly.vertCount; j = i++)
                {
                    // Skip non-solid edges.
                    if ((bestPoly.neis[j] & DT_EXT_LINK) != 0)
                    {
                        // Tile border.
                        bool solid = true;
                        for (uint k = bestPoly.firstLink; k != DT_NULL_LINK; k = bestTile.links[k].next)
                        {
                            dtLink link = bestTile.links[k];
                            if (link.edge == j)
                            {
                                if (link.polyRef != 0)
                                {
                                    dtMeshTile neiTile = null;
                                    dtPoly neiPoly = null;
                                    m_nav.getTileAndPolyByRefUnsafe(link.polyRef, ref neiTile, ref neiPoly);
                                    if (filter.passFilter(link.polyRef, neiTile, neiPoly))
                                        solid = false;
                                }
                                break;
                            }
                        }
                        if (!solid) continue;
                    }
                    else if (bestPoly.neis[j] != 0)
                    {
                        // Internal edge
                        uint idx = (uint)(bestPoly.neis[j] - 1);
                        dtPolyRef polyRef = m_nav.getPolyRefBase(bestTile) | idx;
                        if (filter.passFilter(polyRef, bestTile, bestTile.polys[idx]))
                            continue;
                    }

                    // Calc distance to the edge.
                    //const float* vj = &bestTile.verts[bestPoly.verts[j]*3];
                    //const float* vi = &bestTile.verts[bestPoly.verts[i]*3];
                    int vjStart = bestPoly.verts[j] * 3;
                    int viStart = bestPoly.verts[i] * 3;
                    float tseg = .0f;
                    float distSqr = dtDistancePtSegSqr2D(centerPos, 0, bestTile.verts, vjStart, bestTile.verts, viStart, ref tseg);

                    // Edge is too far, skip.
                    if (distSqr > radiusSqr)
                        continue;

                    // Hit wall, update radius.
                    radiusSqr = distSqr;
                    // Calculate hit pos.
                    hitPos[0] = bestTile.verts[vjStart + 0] + (bestTile.verts[viStart + 0] - bestTile.verts[vjStart + 0]) * tseg;
                    hitPos[1] = bestTile.verts[vjStart + 1] + (bestTile.verts[viStart + 1] - bestTile.verts[vjStart + 1]) * tseg;
                    hitPos[2] = bestTile.verts[vjStart + 2] + (bestTile.verts[viStart + 2] - bestTile.verts[vjStart + 2]) * tseg;
                }

                for (uint i = bestPoly.firstLink; i != DT_NULL_LINK; i = bestTile.links[i].next)
                {
                    dtLink link = bestTile.links[i];
                    dtPolyRef neighbourRef = link.polyRef;
                    // Skip invalid neighbours and do not follow back to parent.
                    if (neighbourRef != 0 || neighbourRef == parentRef)
                        continue;

                    // Expand to neighbour.
                    dtMeshTile neighbourTile = null;
                    dtPoly neighbourPoly = null;
                    m_nav.getTileAndPolyByRefUnsafe(neighbourRef, ref neighbourTile, ref neighbourPoly);

                    // Skip off-mesh connections.
                    if (neighbourPoly.getType() == (byte)dtPolyTypes.DT_POLYTYPE_OFFMESH_CONNECTION)
                        continue;

                    // Calc distance to the edge.
                    //const float* va = &bestTile.verts[bestPoly.verts[link.edge]*3];
                    //const float* vb = &bestTile.verts[bestPoly.verts[(link.edge+1) % bestPoly.vertCount]*3];
                    int vaStart = bestPoly.verts[link.edge] * 3;
                    int vbStart = bestPoly.verts[(link.edge + 1) % bestPoly.vertCount] * 3;

                    float tseg = .0f;
                    float distSqr = dtDistancePtSegSqr2D(centerPos, 0, bestTile.verts, vaStart, bestTile.verts, vbStart, ref tseg);

                    // If the circle is not touching the next polygon, skip it.
                    if (distSqr > radiusSqr)
                        continue;

                    if (!filter.passFilter(neighbourRef, neighbourTile, neighbourPoly))
                        continue;

                    dtNode neighbourNode = m_nodePool.getNode(neighbourRef);
                    if (neighbourNode == null)
                    {
                        status |= DT_OUT_OF_NODES;
                        continue;
                    }

                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_CLOSED))// .flags & DT_NODE_CLOSED)
                        continue;

                    // Cost
                    if (neighbourNode.flags == 0)
                    {
                        getEdgeMidPoint(bestRef, bestPoly, bestTile,
                                        neighbourRef, neighbourPoly, neighbourTile, neighbourNode.pos);
                    }

                    float total = bestNode.total + dtVdist(bestNode.pos, neighbourNode.pos);

                    // The node is already in open list and the new result is worse, skip.
                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_OPEN) && total >= neighbourNode.total)
                        continue;

                    neighbourNode.id = neighbourRef;
                    //neighbourNode.flags = (neighbourNode.flags & ~DT_NODE_CLOSED);
                    neighbourNode.dtcsClearFlag(dtNodeFlags.DT_NODE_CLOSED);
                    neighbourNode.pidx = m_nodePool.getNodeIdx(bestNode);
                    neighbourNode.total = total;

                    if (neighbourNode.dtcsTestFlag(dtNodeFlags.DT_NODE_OPEN))// .flags & DT_NODE_OPEN)
                    {
                        m_openList.modify(neighbourNode);
                    }
                    else
                    {
                        //neighbourNode.flags |= DT_NODE_OPEN;
                        neighbourNode.dtcsSetFlag(dtNodeFlags.DT_NODE_OPEN);
                        m_openList.push(neighbourNode);
                    }
                }
            }

            // Calc hit normal.
            dtVsub(hitNormal, centerPos, hitPos);
            dtVnormalize(hitNormal);

            hitDist = (float)Math.Sqrt(radiusSqr);

            return status;
        }

        // @}
        // @name Miscellaneous Functions
        // @{

        /// Returns true if the polygon reference is valid and passes the filter restrictions.
        ///  @param[in]		ref			The polygon reference to check.
        ///  @param[in]		filter		The filter to apply.
        bool isValidPolyRef(dtPolyRef polyRef, dtQueryFilter filter)
        {
            dtMeshTile tile = null;
            dtPoly poly = null;
            dtStatus status = m_nav.getTileAndPolyByRef(polyRef, ref tile, ref poly);
            // If cannot get polygon, assume it does not exists and boundary is invalid.
            if (dtStatusFailed(status))
                return false;
            // If cannot pass filter, assume flags has changed and boundary is invalid.
            if (!filter.passFilter(polyRef, tile, poly))
                return false;
            return true;
        }

        /// Returns true if the polygon reference is in the closed list. 
        ///  @param[in]		ref		The reference id of the polygon to check.
        // @returns True if the polygon is in closed list.
        // @par
        ///
        /// The closed list is the list of polygons that were fully evaluated during 
        /// the last navigation graph search. (A* or Dijkstra)
        /// 
        bool isInClosedList(dtPolyRef polyRef)
        {
            if (m_nodePool == null) return false;
            dtNode node = m_nodePool.findNode(polyRef);
            return node != null && node.dtcsTestFlag(dtNodeFlags.DT_NODE_CLOSED);// .flags & DT_NODE_CLOSED;
        }

        /// Gets the navigation mesh the query object is using.
        // @return The navigation mesh the query object is using.
        public dtNavMesh getAttachedNavMesh()
        {
            return m_nav;
        }
    }

    public class dtFindNearestPolyQuery
    {
        dtNavMeshQuery m_query;
        float[] m_center;
        float m_nearestDistanceSqr;
        dtPolyRef m_nearestRef;
        float[] m_nearestPoint = new float[3];

        public dtFindNearestPolyQuery(dtNavMeshQuery query, float[] center)
        {
            m_query = query;
            m_center = center;
            m_nearestDistanceSqr = float.MaxValue;
            m_nearestRef = 0;
        }

        public dtPolyRef nearestRef() { return m_nearestRef; }
        public float[] nearestPoint() { return m_nearestPoint; }

        public void process(dtMeshTile tile, dtPoly[] polys, dtPolyRef[] refs, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                dtPolyRef refe = refs[i];
                float[] closestPtPoly = new float[3];
                float[] diff = new float[3];
                bool posOverPoly = false;
                float d;
                m_query.closestPointOnPoly(refe, m_center, closestPtPoly, ref posOverPoly);

                // If a point is directly over a polygon and closer than
                // climb height, favor that instead of straight line nearest point.
                dtVsub(diff, m_center, closestPtPoly);
                if (posOverPoly)
                {
                    d = Math.Abs(diff[1]) - tile.header.walkableClimb;
                    d = d > 0 ? d * d : 0;
                }
                else
                {
                    d = dtVlenSqr(diff);
                }

                if (d < m_nearestDistanceSqr)
                {
                    dtVcopy(m_nearestPoint, closestPtPoly);

                    m_nearestDistanceSqr = d;
                    m_nearestRef = refe;
                }
            }
        }
    }
}



















