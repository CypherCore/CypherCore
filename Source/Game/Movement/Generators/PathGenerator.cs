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

using Framework.Constants;
using Framework.GameMath;
using Game.Entities;
using Game.Maps;
using System;
using System.Linq;

namespace Game.Movement
{
    public class PathGenerator
    {
        public PathGenerator(Unit owner)
        {
            _polyLength = 0;
            pathType = PathType.Blank;
            _useStraightPath = false;
            _forceDestination = false;
            _pointPathLimit = 74;
            _endPosition = Vector3.Zero;
            _sourceUnit = owner;
            _navMesh = null;
            _navMeshQuery = null;
            Log.outDebug(LogFilter.Maps, "PathGenerator:PathGenerator for {0}", _sourceUnit.GetGUID().ToString());

            uint mapId = PhasingHandler.GetTerrainMapId(_sourceUnit.GetPhaseShift(), _sourceUnit.GetMap(), _sourceUnit.GetPositionX(), _sourceUnit.GetPositionY());
            if (Global.DisableMgr.IsPathfindingEnabled(_sourceUnit.GetMapId()))
            {
                _navMesh = Global.MMapMgr.GetNavMesh(mapId);
                _navMeshQuery = Global.MMapMgr.GetNavMeshQuery(mapId, _sourceUnit.GetInstanceId());
            }
            CreateFilter();
        }

        public bool CalculatePath(float destX, float destY, float destZ, bool forceDest = false, bool straightLine = false)
        {
            float x, y, z;
            _sourceUnit.GetPosition(out x, out y, out z);

            if (!GridDefines.IsValidMapCoord(destX, destY, destZ) || !GridDefines.IsValidMapCoord(x, y, z))
                return false;

            Vector3 dest = new Vector3(destX, destY, destZ);
            SetEndPosition(dest);

            Vector3 start = new Vector3(x, y, z);
            SetStartPosition(start);

            _forceDestination = forceDest;
            _straightLine = straightLine;

            Log.outDebug(LogFilter.Maps, "PathGenerator.CalculatePath() for {0} \n", _sourceUnit.GetGUID().ToString());

            // make sure navMesh works - we can run on map w/o mmap
            // check if the start and end point have a .mmtile loaded (can we pass via not loaded tile on the way?)
            if (_navMesh == null || _navMeshQuery == null || _sourceUnit.HasUnitState(UnitState.IgnorePathfinding)
                || !HaveTile(start) || !HaveTile(dest))
            {
                BuildShortcut();
                pathType = PathType.Normal | PathType.NotUsingPath;
                return true;
            }

            UpdateFilter();
            BuildPolyPath(start, dest);
            return true;
        }

        ulong GetPathPolyByPosition(ulong[] polyPath, uint polyPathSize, float[] point, ref float distance)
        {
            if (polyPath == null || polyPathSize == 0)
                return 0;

            ulong nearestPoly = 0;
            float minDist2d = float.MaxValue;
            float minDist3d = 0.0f;

            for (uint i = 0; i < polyPathSize; ++i)
            {
                float[] closestPoint = new float[3];
                bool posOverPoly = false;
                if (Detour.dtStatusFailed(_navMeshQuery.closestPointOnPoly(polyPath[i], point, closestPoint, ref posOverPoly)))
                    continue;

                float d = Detour.dtVdist2DSqr(point, closestPoint);
                if (d < minDist2d)
                {
                    minDist2d = d;
                    nearestPoly = polyPath[i];
                    minDist3d = Detour.dtVdistSqr(point, closestPoint);
                }

                if (minDist2d < 1.0f) // shortcut out - close enough for us
                    break;
            }

            distance = (float)Math.Sqrt(minDist3d);

            return (minDist2d < 3.0f) ? nearestPoly : 0u;
        }

        ulong GetPolyByLocation(float[] point, ref float distance)
        {
            // first we check the current path
            // if the current path doesn't contain the current poly,
            // we need to use the expensive navMesh.findNearestPoly
            ulong polyRef = GetPathPolyByPosition(_pathPolyRefs, _polyLength, point, ref distance);
            if (polyRef != 0)
                return polyRef;

            // we don't have it in our old path
            // try to get it by findNearestPoly()
            // first try with low search box
            float[] extents = { 3.0f, 5.0f, 3.0f };    // bounds of poly search area
            float[] closestPoint = { 0.0f, 0.0f, 0.0f };
            if (Detour.dtStatusSucceed(_navMeshQuery.findNearestPoly(point, extents, _filter, ref polyRef, ref closestPoint)) && polyRef != 0)
            {
                distance = Detour.dtVdist(closestPoint, point);
                return polyRef;
            }

            // still nothing ..
            // try with bigger search box
            // Note that the extent should not overlap more than 128 polygons in the navmesh (see dtNavMeshQuery.findNearestPoly)
            extents[1] = 50.0f;
            if (Detour.dtStatusSucceed(_navMeshQuery.findNearestPoly(point, extents, _filter, ref polyRef, ref closestPoint)) && polyRef != 0)
            {
                distance = Detour.dtVdist(closestPoint, point);
                return polyRef;
            }

            return 0;
        }

        void BuildPolyPath(Vector3 startPos, Vector3 endPos)
        {
            // *** getting start/end poly logic ***

            float distToStartPoly = 0;
            float distToEndPoly = 0;
            float[] startPoint = { startPos.Y, startPos.Z, startPos.X };
            float[] endPoint = { endPos.Y, endPos.Z, endPos.X };

            ulong startPoly = GetPolyByLocation(startPoint, ref distToStartPoly);
            ulong endPoly = GetPolyByLocation(endPoint, ref distToEndPoly);

            // we have a hole in our mesh
            // make shortcut path and mark it as NOPATH ( with flying and swimming exception )
            // its up to caller how he will use this info
            if (startPoly == 0 || endPoly == 0)
            {
                Log.outDebug(LogFilter.Maps, "++ BuildPolyPath . (startPoly == 0 || endPoly == 0)\n");
                BuildShortcut();
                bool path = _sourceUnit.IsTypeId(TypeId.Unit) && _sourceUnit.ToCreature().CanFly();

                bool waterPath = _sourceUnit.IsTypeId(TypeId.Unit) && _sourceUnit.ToCreature().CanSwim();
                if (waterPath)
                {
                    // Check both start and end points, if they're both in water, then we can *safely* let the creature move
                    for (uint i = 0; i < _pathPoints.Length; ++i)
                    {
                        ZLiquidStatus status = _sourceUnit.GetMap().getLiquidStatus(_sourceUnit.GetPhaseShift(), _pathPoints[i].X, _pathPoints[i].Y, _pathPoints[i].Z, MapConst.MapAllLiquidTypes);
                        // One of the points is not in the water, cancel movement.
                        if (status == ZLiquidStatus.NoWater)
                        {
                            waterPath = false;
                            break;
                        }
                    }
                }

                pathType = (path || waterPath) ? (PathType.Normal | PathType.NotUsingPath) : PathType.NoPath;
                return;
            }

            // we may need a better number here
            bool farFromPoly = (distToStartPoly > 7.0f || distToEndPoly > 7.0f);
            if (farFromPoly)
            {
                Log.outDebug(LogFilter.Maps, "++ BuildPolyPath . farFromPoly distToStartPoly={0:F3} distToEndPoly={1:F3}\n", distToStartPoly, distToEndPoly);

                bool buildShotrcut = false;
                if (_sourceUnit.IsTypeId(TypeId.Unit))
                {
                    Creature owner = _sourceUnit.ToCreature();

                    Vector3 p = (distToStartPoly > 7.0f) ? startPos : endPos;
                    if (_sourceUnit.GetMap().IsUnderWater(_sourceUnit.GetPhaseShift(), p.X, p.Y, p.Z))
                    {
                        Log.outDebug(LogFilter.Maps, "++ BuildPolyPath . underWater case\n");
                        if (owner.CanSwim())
                            buildShotrcut = true;
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Maps, "++ BuildPolyPath . flying case\n");
                        if (owner.CanFly())
                            buildShotrcut = true;
                    }
                }

                if (buildShotrcut)
                {
                    BuildShortcut();
                    pathType = (PathType.Normal | PathType.NotUsingPath);
                    return;
                }
                else
                {
                    float[] closestPoint = new float[3];
                    // we may want to use closestPointOnPolyBoundary instead
                    bool posOverPoly = false;
                    if (Detour.dtStatusSucceed(_navMeshQuery.closestPointOnPoly(endPoly, endPoint, closestPoint, ref posOverPoly)))
                    {
                        Detour.dtVcopy(endPoint, closestPoint);
                        SetActualEndPosition(new Vector3(endPoint[2], endPoint[0], endPoint[1]));
                    }

                    pathType = PathType.Incomplete;
                }
            }

            // *** poly path generating logic ***

            // start and end are on same polygon
            // just need to move in straight line
            if (startPoly == endPoly)
            {
                Log.outDebug(LogFilter.Maps, "++ BuildPolyPath . (startPoly == endPoly)\n");

                BuildShortcut();

                _pathPolyRefs[0] = startPoly;
                _polyLength = 1;

                pathType = farFromPoly ? PathType.Incomplete : PathType.Normal;
                Log.outDebug(LogFilter.Maps, "BuildPolyPath . path type {0}\n", pathType);
                return;
            }

            // look for startPoly/endPoly in current path
            // @todo we can merge it with getPathPolyByPosition() loop
            bool startPolyFound = false;
            bool endPolyFound = false;
            uint pathStartIndex = 0;
            uint pathEndIndex = 0;

            if (_polyLength != 0)
            {
                for (; pathStartIndex < _polyLength; ++pathStartIndex)
                {
                    // here to carch few bugs
                    if (_pathPolyRefs[pathStartIndex] == 0)
                    {
                        Log.outError(LogFilter.Maps, "Invalid poly ref in BuildPolyPath. _polyLength: {0}, pathStartIndex: {1}," +
                            " startPos: {2}, endPos: {3}, mapid: {4}", _polyLength, pathStartIndex, startPos, endPos, _sourceUnit.GetMapId());
                        break;
                    }

                    if (_pathPolyRefs[pathStartIndex] == startPoly)
                    {
                        startPolyFound = true;
                        break;
                    }
                }

                for (pathEndIndex = _polyLength - 1; pathEndIndex > pathStartIndex; --pathEndIndex)
                    if (_pathPolyRefs[pathEndIndex] == endPoly)
                    {
                        endPolyFound = true;
                        break;
                    }
            }

            if (startPolyFound && endPolyFound)
            {
                Log.outDebug(LogFilter.Maps, "BuildPolyPath : (startPolyFound && endPolyFound)\n");

                // we moved along the path and the target did not move out of our old poly-path
                // our path is a simple subpath case, we have all the data we need
                // just "cut" it out

                _polyLength = pathEndIndex - pathStartIndex + 1;
                Array.Copy(_pathPolyRefs, pathStartIndex, _pathPolyRefs, 0, _polyLength);
            }
            else if (startPolyFound && !endPolyFound)
            {
                Log.outDebug(LogFilter.Maps, "BuildPolyPath : (startPolyFound && !endPolyFound)\n");

                // we are moving on the old path but target moved out
                // so we have atleast part of poly-path ready

                _polyLength -= pathStartIndex;

                // try to adjust the suffix of the path instead of recalculating entire length
                // at given interval the target cannot get too far from its last location
                // thus we have less poly to cover
                // sub-path of optimal path is optimal

                // take ~80% of the original length
                // @todo play with the values here
                uint prefixPolyLength = (uint)(_polyLength * 0.8f + 0.5f);
                Array.Copy(_pathPolyRefs, pathStartIndex, _pathPolyRefs, 0, prefixPolyLength);

                ulong suffixStartPoly = _pathPolyRefs[prefixPolyLength - 1];

                // we need any point on our suffix start poly to generate poly-path, so we need last poly in prefix data
                float[] suffixEndPoint = new float[3];
                bool posOverPoly = false;
                if (Detour.dtStatusFailed(_navMeshQuery.closestPointOnPoly(suffixStartPoly, endPoint, suffixEndPoint, ref posOverPoly)))
                {
                    // we can hit offmesh connection as last poly - closestPointOnPoly() don't like that
                    // try to recover by using prev polyref
                    --prefixPolyLength;
                    suffixStartPoly = _pathPolyRefs[prefixPolyLength - 1];
                    if (Detour.dtStatusFailed(_navMeshQuery.closestPointOnPoly(suffixStartPoly, endPoint, suffixEndPoint, ref posOverPoly)))
                    {
                        // suffixStartPoly is still invalid, error state
                        BuildShortcut();
                        pathType = PathType.NoPath;
                        return;
                    }
                }

                // generate suffix
                uint suffixPolyLength = 0;
                ulong[] tempPolyRefs = new ulong[_pathPolyRefs.Length];

                uint dtResult;
                if (_straightLine)
                {
                    float hit = 0;
                    float[] hitNormal = new float[3];

                    dtResult = _navMeshQuery.raycast(
                        suffixStartPoly,
                        suffixEndPoint,
                        endPoint,
                        _filter,
                        ref hit,
                        hitNormal,
                        tempPolyRefs,
                        ref suffixPolyLength,
                        74 - (int)prefixPolyLength);

                    // raycast() sets hit to FLT_MAX if there is a ray between start and end
                    if (hit != float.MaxValue)
                    {
                        // the ray hit something, return no path instead of the incomplete one
                        pathType = PathType.NoPath;
                        return;
                    }
                }
                else
                {
                    dtResult = _navMeshQuery.findPath(
                        suffixStartPoly,    // start polygon
                        endPoly,            // end polygon
                        suffixEndPoint,     // start position
                        endPoint,           // end position
                        _filter,            // polygon search filter
                        tempPolyRefs,
                        ref suffixPolyLength,
                        74 - (int)prefixPolyLength);
                }

                if (suffixPolyLength == 0 || Detour.dtStatusFailed(dtResult))
                {
                    // this is probably an error state, but we'll leave it
                    // and hopefully recover on the next Update
                    // we still need to copy our preffix
                    Log.outError(LogFilter.Maps, "{0}'s Path Build failed: 0 length path", _sourceUnit.GetGUID().ToString());
                }

                Log.outDebug(LogFilter.Maps, "m_polyLength={0} prefixPolyLength={1} suffixPolyLength={2} \n", _polyLength, prefixPolyLength, suffixPolyLength);

                for (var i = 0; i < _pathPolyRefs.Length - (prefixPolyLength - 1); ++i)
                    _pathPolyRefs[(prefixPolyLength - 1) + i] = tempPolyRefs[i];

                // new path = prefix + suffix - overlap
                _polyLength = prefixPolyLength + suffixPolyLength - 1;
            }
            else
            {
                Log.outDebug(LogFilter.Maps, "++ BuildPolyPath . (!startPolyFound && !endPolyFound)\n");

                // either we have no path at all . first run
                // or something went really wrong . we aren't moving along the path to the target
                // just generate new path

                // free and invalidate old path data
                Clear();

                uint dtResult;
                if (_straightLine)
                {
                    float hit = 0;
                    float[] hitNormal = new float[3];

                    dtResult = _navMeshQuery.raycast(
                                    startPoly,
                                    startPoint,
                                    endPoint,
                                    _filter,
                                    ref hit,
                                    hitNormal,
                                    _pathPolyRefs,
                                    ref _polyLength,
                                    74);

                    // raycast() sets hit to FLT_MAX if there is a ray between start and end
                    if (hit != float.MaxValue)
                    {
                        // the ray hit something, return no path instead of the incomplete one
                        pathType = PathType.NoPath;
                        return;
                    }
                }
                else
                {
                    dtResult = _navMeshQuery.findPath(
                        startPoly,          // start polygon
                        endPoly,            // end polygon
                        startPoint,         // start position
                        endPoint,           // end position
                        _filter,           // polygon search filter
                        _pathPolyRefs,     // [out] path
                        ref _polyLength,
                        74);   // max number of polygons in output path
                }

                if (_polyLength == 0 || Detour.dtStatusFailed(dtResult))
                {
                    // only happens if we passed bad data to findPath(), or navmesh is messed up
                    Log.outError(LogFilter.Maps, "{0}'s Path Build failed: 0 length path", _sourceUnit.GetGUID().ToString());
                    BuildShortcut();
                    pathType = PathType.NoPath;
                    return;
                }
            }

            // by now we know what type of path we can get
            if (_pathPolyRefs[_polyLength - 1] == endPoly && !pathType.HasAnyFlag(PathType.Incomplete))
                pathType = PathType.Normal;
            else
                pathType = PathType.Incomplete;

            // generate the point-path out of our up-to-date poly-path
            BuildPointPath(startPoint, endPoint);
        }

        void BuildPointPath(float[] startPoint, float[] endPoint)
        {
            float[] pathPoints = new float[74 * 3];
            int pointCount = 0;
            uint dtResult = Detour.DT_FAILURE;

            if (_straightLine)
            {
                dtResult = Detour.DT_SUCCESS;
                pointCount = 1;
                Array.Copy(startPoint, pathPoints, 3); // first point

                // path has to be split into polygons with dist SMOOTH_PATH_STEP_SIZE between them
                Vector3 startVec = new Vector3(startPoint[0], startPoint[1], startPoint[2]);
                Vector3 endVec = new Vector3(endPoint[0], endPoint[1], endPoint[2]);
                Vector3 diffVec = (endVec - startVec);
                Vector3 prevVec = startVec;
                float len = diffVec.GetLength();
                diffVec *= 4.0f / len;
                while (len > 4.0f)
                {
                    len -= 4.0f;
                    prevVec += diffVec;
                    pathPoints[3 * pointCount + 0] = prevVec.X;
                    pathPoints[3 * pointCount + 1] = prevVec.Y;
                    pathPoints[3 * pointCount + 2] = prevVec.Z;
                    ++pointCount;
                }

                Array.Copy(endPoint, 0, pathPoints, 3 * pointCount, 3); // last point
                ++pointCount;
            }
            else if (_useStraightPath)
            {
                dtResult = _navMeshQuery.findStraightPath(
                    startPoint,         // start position
                    endPoint,           // end position
                    _pathPolyRefs,
                    (int)_polyLength,
                    pathPoints,         // [out] path corner points
                    null,               // [out] flags
                    null,               // [out] shortened path
                    ref pointCount,
                    (int)_pointPathLimit,
                    0);   // maximum number of points/polygons to use
            }
            else
            {
                dtResult = FindSmoothPath(
                    startPoint,         // start position
                    endPoint,           // end position
                    _pathPolyRefs,     // current path
                    _polyLength,       // length of current path
                    out pathPoints,         // [out] path corner points
                    out pointCount,
                    _pointPathLimit);    // maximum number of points
            }

            if (pointCount < 2 || Detour.dtStatusFailed(dtResult))
            {
                // only happens if pass bad data to findStraightPath or navmesh is broken
                // single point paths can be generated here
                // @todo check the exact cases
                Log.outDebug(LogFilter.Maps, "++ PathGenerator.BuildPointPath FAILED! path sized {0} returned\n", pointCount);
                BuildShortcut();
                pathType = PathType.NoPath;
                return;
            }
            else if (pointCount == _pointPathLimit)
            {
                Log.outDebug(LogFilter.Maps, "++ PathGenerator.BuildPointPath FAILED! path sized {0} returned, lower than limit set to {1}\n", pointCount, _pointPathLimit);
                BuildShortcut();
                pathType = PathType.Short;
                return;
            }

            _pathPoints = new Vector3[pointCount];
            for (uint i = 0; i < pointCount; ++i)
                _pathPoints[i] = new Vector3(pathPoints[i * 3 + 2], pathPoints[i * 3], pathPoints[i * 3 + 1]);

            NormalizePath();

            // first point is always our current location - we need the next one
            SetActualEndPosition(_pathPoints[pointCount - 1]);

            // force the given destination, if needed
            if (_forceDestination && (!pathType.HasAnyFlag(PathType.Normal) || !InRange(GetEndPosition(), GetActualEndPosition(), 1.0f, 1.0f)))
            {
                // we may want to keep partial subpath
                if (Dist3DSqr(GetActualEndPosition(), GetEndPosition()) < 0.3f * Dist3DSqr(GetStartPosition(), GetEndPosition()))
                {
                    SetActualEndPosition(GetEndPosition());
                    _pathPoints[_pathPoints.Length - 1] = GetEndPosition();
                }
                else
                {
                    SetActualEndPosition(GetEndPosition());
                    BuildShortcut();
                }

                pathType = (PathType.Normal | PathType.NotUsingPath);
            }
            Log.outDebug(LogFilter.Maps, "PathGenerator.BuildPointPath path type {0} size {1} poly-size {2}\n", pathType, pointCount, _polyLength);
        }

        uint FixupCorridor(ulong[] path, uint npath, uint maxPath, ulong[] visited, int nvisited)
        {
            int furthestPath = -1;
            int furthestVisited = -1;

            // Find furthest common polygon.
            for (int i = (int)npath - 1; i >= 0; --i)
            {
                bool found = false;
                for (int j = (int)nvisited - 1; j >= 0; --j)
                {
                    if (path[i] == visited[j])
                    {
                        furthestPath = i;
                        furthestVisited = j;
                        found = true;
                    }
                }
                if (found)
                    break;
            }

            // If no intersection found just return current path.
            if (furthestPath == -1 || furthestVisited == -1)
                return npath;

            // Concatenate paths.

            // Adjust beginning of the buffer to include the visited.
            uint req = (uint)(nvisited - furthestVisited);
            uint orig = (uint)((furthestPath + 1) < npath ? furthestPath + 1 : (int)npath);
            uint size = npath > orig ? npath - orig : 0;
            if (req + size > maxPath)
                size = maxPath - req;

            if (size != 0)
                Array.Copy(path, (int)orig, path, (int)req, (int)size);

            // Store visited
            for (uint i = 0; i < req; ++i)
                path[i] = visited[(nvisited - 1) - i];

            return req + size;
        }

        bool GetSteerTarget(float[] startPos, float[] endPos, float minTargetDist, ulong[] path, uint pathSize, out float[] steerPos, out Detour.dtStraightPathFlags steerPosFlag, out ulong steerPosRef)
        {
            steerPosRef = 0;
            steerPos = new float[3];
            steerPosFlag = 0;

            // Find steer target.
            float[] steerPath = new float[3 * 3];
            byte[] steerPathFlags = new byte[3];
            ulong[] steerPathPolys = new ulong[3];
            int nsteerPath = 0;
            uint dtResult = _navMeshQuery.findStraightPath(startPos, endPos, path, (int)pathSize, steerPath, steerPathFlags, steerPathPolys, ref nsteerPath, 3, 0);
            if (nsteerPath == 0 || Detour.dtStatusFailed(dtResult))
                return false;

            // Find vertex far enough to steer to.
            uint ns = 0;
            while (ns < nsteerPath)
            {
                Span<float> span = steerPath;
                // Stop at Off-Mesh link or when point is further than slop away.
                if ((steerPathFlags[ns].HasAnyFlag((byte)Detour.dtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION) ||
                    !InRangeYZX(span.Slice((int)ns * 3).ToArray(), startPos, minTargetDist, 1000.0f)))
                    break;
                ns++;
            }
            // Failed to find good point to steer to.
            if (ns >= nsteerPath)
                return false;

            Detour.dtVcopy(steerPos, 0, steerPath, (int)ns * 3);
            steerPos[1] = startPos[1];  // keep Z value
            steerPosFlag = (Detour.dtStraightPathFlags)steerPathFlags[ns];
            steerPosRef = steerPathPolys[ns];

            return true;
        }

        uint FindSmoothPath(float[] startPos, float[] endPos, ulong[] polyPath, uint polyPathSize, out float[] smoothPath, out int smoothPathSize, uint maxSmoothPathSize)
        {
            smoothPathSize = 0;
            int nsmoothPath = 0;
            smoothPath = new float[74 * 3];

            ulong[] polys = new ulong[74];
            Array.Copy(polyPath, polys, polyPathSize);
            uint npolys = polyPathSize;

            float[] iterPos = new float[3];
            float[] targetPos = new float[3];
            if (Detour.dtStatusFailed(_navMeshQuery.closestPointOnPolyBoundary(polys[0], startPos, iterPos)))
                return Detour.DT_FAILURE;

            if (Detour.dtStatusFailed(_navMeshQuery.closestPointOnPolyBoundary(polys[npolys - 1], endPos, targetPos)))
                return Detour.DT_FAILURE;

            Detour.dtVcopy(smoothPath, nsmoothPath * 3, iterPos, 0);
            nsmoothPath++;

            // Move towards target a small advancement at a time until target reached or
            // when ran out of memory to store the path.
            while (npolys != 0 && nsmoothPath < maxSmoothPathSize)
            {
                // Find location to steer towards.
                float[] steerPos;
                Detour.dtStraightPathFlags steerPosFlag;
                ulong steerPosRef = 0;

                if (!GetSteerTarget(iterPos, targetPos, 0.3f, polys, npolys, out steerPos, out steerPosFlag, out steerPosRef))
                    break;

                bool endOfPath = steerPosFlag.HasAnyFlag(Detour.dtStraightPathFlags.DT_STRAIGHTPATH_END);
                bool offMeshConnection = steerPosFlag.HasAnyFlag(Detour.dtStraightPathFlags.DT_STRAIGHTPATH_OFFMESH_CONNECTION);

                // Find movement delta.
                float[] delta = new float[3];
                Detour.dtVsub(delta, steerPos, iterPos);
                float len = (float)Math.Sqrt(Detour.dtVdot(delta, delta));
                // If the steer target is end of path or off-mesh link, do not move past the location.
                if ((endOfPath || offMeshConnection) && len < 4.0f)
                    len = 1.0f;
                else
                    len = 4.0f / len;

                float[] moveTgt = new float[3];
                Detour.dtVmad(moveTgt, iterPos, delta, len);

                // Move
                float[] result = new float[3];
                int MAX_VISIT_POLY = 16;
                ulong[] visited = new ulong[MAX_VISIT_POLY];

                int nvisited = 0;
                _navMeshQuery.moveAlongSurface(polys[0], iterPos, moveTgt, _filter, result, visited, ref nvisited, MAX_VISIT_POLY);
                npolys = FixupCorridor(polys, npolys, 74, visited, nvisited);

                _navMeshQuery.getPolyHeight(polys[0], result, ref result[1]);
                result[1] += 0.5f;
                Detour.dtVcopy(iterPos, result);

                // Handle end of path and off-mesh links when close enough.
                if (endOfPath && InRangeYZX(iterPos, steerPos, 0.3f, 1.0f))
                {
                    // Reached end of path.
                    Detour.dtVcopy(iterPos, targetPos);
                    if (nsmoothPath < maxSmoothPathSize)
                    {
                        Detour.dtVcopy(smoothPath, nsmoothPath * 3, iterPos, 0);
                        nsmoothPath++;
                    }
                    break;
                }
                else if (offMeshConnection && InRangeYZX(iterPos, steerPos, 0.3f, 1.0f))
                {
                    // Advance the path up to and over the off-mesh connection.
                    ulong prevRef = 0;
                    ulong polyRef = polys[0];
                    uint npos = 0;
                    while (npos < npolys && polyRef != steerPosRef)
                    {
                        prevRef = polyRef;
                        polyRef = polys[npos];
                        npos++;
                    }

                    for (uint i = npos; i < npolys; ++i)
                        polys[i - npos] = polys[i];

                    npolys -= npos;

                    // Handle the connection.
                    float[] connectionStartPos = new float[3];
                    float[] connectionEndPos = new float[3];
                    if (Detour.dtStatusSucceed(_navMesh.getOffMeshConnectionPolyEndPoints(prevRef, polyRef, connectionStartPos, connectionEndPos)))
                    {
                        if (nsmoothPath < maxSmoothPathSize)
                        {
                            Detour.dtVcopy(smoothPath, nsmoothPath * 3, connectionStartPos, 0);
                            nsmoothPath++;
                        }
                        // Move position at the other side of the off-mesh link.
                        Detour.dtVcopy(iterPos, connectionEndPos);
                        _navMeshQuery.getPolyHeight(polys[0], iterPos, ref iterPos[1]);
                        iterPos[1] += 0.5f;
                    }
                }

                // Store results.
                if (nsmoothPath < maxSmoothPathSize)
                {
                    Detour.dtVcopy(smoothPath, nsmoothPath * 3, iterPos, 0);
                    nsmoothPath++;
                }
            }

            smoothPathSize = nsmoothPath;

            // this is most likely a loop
            return nsmoothPath < 74 ? Detour.DT_SUCCESS : Detour.DT_FAILURE;
        }

        void NormalizePath()
        {
            for (uint i = 0; i < _pathPoints.Length; ++i)
                _sourceUnit.UpdateAllowedPositionZ(_pathPoints[i].X, _pathPoints[i].Y, ref _pathPoints[i].Z);
        }

        void BuildShortcut()
        {
            Log.outDebug(LogFilter.Maps, "BuildShortcut : making shortcut\n");

            Clear();

            // make two point path, our curr pos is the start, and dest is the end
            _pathPoints = new Vector3[2];

            // set start and a default next position
            _pathPoints[0] = GetStartPosition();
            _pathPoints[1] = GetActualEndPosition();

            NormalizePath();

            pathType = PathType.Shortcut;
        }

        void CreateFilter()
        {
            NavTerrainFlag includeFlags = 0;
            NavTerrainFlag excludeFlags = 0;

            if (_sourceUnit.IsTypeId(TypeId.Unit))
            {
                Creature creature = _sourceUnit.ToCreature();
                if (creature.CanWalk())
                    includeFlags |= NavTerrainFlag.Ground;

                // creatures don't take environmental damage
                if (creature.CanSwim())
                    includeFlags |= (NavTerrainFlag.Water | NavTerrainFlag.MagmaSlime);
            }
            else
                includeFlags = (NavTerrainFlag.Ground | NavTerrainFlag.Water | NavTerrainFlag.MagmaSlime);

            _filter.setIncludeFlags((ushort)includeFlags);
            _filter.setExcludeFlags((ushort)excludeFlags);

            UpdateFilter();
        }

        void UpdateFilter()
        {
            // allow creatures to cheat and use different movement types if they are moved
            // forcefully into terrain they can't normally move in
            if (_sourceUnit.IsInWater() || _sourceUnit.IsUnderWater())
            {
                NavTerrainFlag includedFlags = (NavTerrainFlag)_filter.getIncludeFlags();
                includedFlags |= GetNavTerrain(_sourceUnit.GetPositionX(), _sourceUnit.GetPositionY(), _sourceUnit.GetPositionZ());

                _filter.setIncludeFlags((ushort)includedFlags);
            }
        }

        NavTerrainFlag GetNavTerrain(float x, float y, float z)
        {
            LiquidData data;
            ZLiquidStatus liquidStatus = _sourceUnit.GetMap().getLiquidStatus(_sourceUnit.GetPhaseShift(), x, y, z, MapConst.MapAllLiquidTypes, out data);
            if (liquidStatus == ZLiquidStatus.NoWater)
                return NavTerrainFlag.Ground;

            data.type_flags &= ~MapConst.MapLiquidTypeDarkWater;
            switch (data.type_flags)
            {
                case MapConst.MapLiquidTypeWater:
                case MapConst.MapLiquidTypeOcean:
                    return NavTerrainFlag.Water;
                case MapConst.MapLiquidTypeMagma:
                case MapConst.MapLiquidTypeSlime:
                    return NavTerrainFlag.MagmaSlime;
                default:
                    return NavTerrainFlag.Ground;
            }
        }

        bool InRange(Vector3 p1, Vector3 p2, float r, float h)
        {
            Vector3 d = p1 - p2;
            return (d.X * d.X + d.Y * d.Y) < r * r && Math.Abs(d.Z) < h;
        }

        float Dist3DSqr(Vector3 p1, Vector3 p2)
        {
            return (p1 - p2).GetLengthSquared();
        }

        public void ReducePathLenghtByDist(float dist)
        {
            if (GetPathType() == PathType.Blank)
            {
                Log.outError(LogFilter.Maps, "PathGenerator.ReducePathLenghtByDist called before path was built");
                return;
            }

            if (_pathPoints.Length < 2) // path building failure
                return;

            int i = _pathPoints.Length;
            Vector3 nextVec = _pathPoints[--i];
            while (i > 0)
            {
                Vector3 currVec = _pathPoints[--i];
                Vector3 diffVec = (nextVec - currVec);
                float len = diffVec.GetLength();
                if (len > dist)
                {
                    float step = dist / len;
                    // same as nextVec
                    _pathPoints[i + 1] -= diffVec * step;
                    _sourceUnit.UpdateAllowedPositionZ(_pathPoints[i + 1].X, _pathPoints[i + 1].Y, ref _pathPoints[i + 1].Z);
                    Array.Resize(ref _pathPoints, i + 2);
                    break;
                }
                else if (i == 0) // at second point
                {
                    _pathPoints[1] = _pathPoints[0];
                    Array.Resize(ref _pathPoints, 2);
                    break;
                }

                dist -= len;
                nextVec = currVec; // we're going backwards
            }
        }

        public bool IsInvalidDestinationZ(Unit target)
        {
            return (target.GetPositionZ() - GetActualEndPosition().Z) > 5.0f;
        }

        void Clear()
        {
            _polyLength = 0;
            _pathPoints = null;
        }

        bool HaveTile(Vector3 p)
        {
            int tx = -1, ty = -1;
            float[] point = { p.Y, p.Z, p.X };

            _navMesh.calcTileLoc(point, ref tx, ref ty);

            // Workaround
            // For some reason, often the tx and ty variables wont get a valid value
            // Use this check to prevent getting negative tile coords and crashing on getTileAt
            if (tx < 0 || ty < 0)
                return false;

            return (_navMesh.getTileAt(tx, ty, 0) != null);
        }

        bool InRangeYZX(float[] v1, float[] v2, float r, float h)
        {
            float dx = v2[0] - v1[0];
            float dy = v2[1] - v1[1]; // elevation
            float dz = v2[2] - v1[2];
            return (dx * dx + dz * dz) < r * r && Math.Abs(dy) < h;
        }

        public Vector3 GetStartPosition() { return _startPosition; }
        public Vector3 GetEndPosition() { return _endPosition; }
        public Vector3 GetActualEndPosition() { return _actualEndPosition; }

        public Vector3[] GetPath()
        {
            return _pathPoints;
        }

        public PathType GetPathType() { return pathType; }

        void SetStartPosition(Vector3 point) { _startPosition = point; }
        void SetEndPosition(Vector3 point) { _actualEndPosition = point; _endPosition = point; }
        void SetActualEndPosition(Vector3 point) { _actualEndPosition = point; }

        public void SetUseStraightPath(bool useStraightPath) { _useStraightPath = useStraightPath; }

        public void SetPathLengthLimit(float distance) { _pointPathLimit = Math.Min((uint)(distance / 4.0f), 74); }

        ulong[] _pathPolyRefs = new ulong[74];

        uint _polyLength;
        uint _pointPathLimit;
        bool _straightLine;     // use raycast if true for a straight line path
        Unit _sourceUnit;
        bool _forceDestination;
        bool _useStraightPath;
        Vector3[] _pathPoints;

        Vector3 _actualEndPosition;
        Vector3 _startPosition;
        Vector3 _endPosition;
        PathType pathType;

        Detour.dtQueryFilter _filter = new Detour.dtQueryFilter();
        Detour.dtNavMeshQuery _navMeshQuery;
        Detour.dtNavMesh _navMesh;
    }

    public enum PathType
    {
        Blank = 0x00,   // path not built yet
        Normal = 0x01,   // normal path
        Shortcut = 0x02,   // travel through obstacles, terrain, air, etc (old behavior)
        Incomplete = 0x04,   // we have partial path to follow - getting closer to target
        NoPath = 0x08,   // no valid path at all or error in generating one
        NotUsingPath = 0x10,   // used when we are either flying/swiming or on map w/o mmaps
        Short = 0x20,   // path is longer or equal to its limited path length
    }

    public enum NavArea
    {
        Empty = 0,
        // areas 1-60 will be used for destructible areas (currently skipped in vmaps, WMO with flag 1)
        // ground is the highest value to make recast choose ground over water when merging surfaces very close to each other (shallow water would be walkable) 
        MagmaSlime = 61, // don't need to differentiate between them
        Water = 62,
        Ground = 63,
    }

    public enum NavTerrainFlag
    {
        Empty = 0x00,
        Ground = 1 << (63 - NavArea.Ground),
        Water = 1 << (63 - NavArea.Water),
        MagmaSlime = 1 << (63 - NavArea.MagmaSlime)
    }

    public enum PolyFlag
    {
        Walk = 1,
        Swim = 2
    }
}
