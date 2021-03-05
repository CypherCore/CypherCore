﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using System;
using System.Collections.Generic;

namespace Game.Collision
{
    public class WorkerCallback
    {
        public virtual void Invoke(Vector3 point, uint entry) { }
        public virtual void Invoke(Vector3 point, IModel entry) { }
        public virtual bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit) { return false; }
        public virtual bool Invoke(Ray r, IModel obj, ref float distance) { return false; }
    }

    public class TriBoundFunc
    {
        public TriBoundFunc(List<Vector3> vert)
        {
            vertices = vert;
        }

        public void Invoke(MeshTriangle tri, out AxisAlignedBox value)
        {
            var lo = vertices[(int)tri.idx0];
            var hi = lo;

            lo = (lo.Min(vertices[(int)tri.idx1])).Min(vertices[(int)tri.idx2]);
            hi = (hi.Max(vertices[(int)tri.idx1])).Max(vertices[(int)tri.idx2]);

            value = new AxisAlignedBox(lo, hi);
        }

        private List<Vector3> vertices;
    }

    public class WModelAreaCallback : WorkerCallback
    {
        public WModelAreaCallback(List<GroupModel> vals, Vector3 down)
        {
            prims = vals;
            hit = null;
            zDist = float.PositiveInfinity;
            zVec = down;
        }

        private List<GroupModel> prims;
        public GroupModel hit;
        public float zDist;
        private Vector3 zVec;
        public override void Invoke(Vector3 point, uint entry)
        {
            float group_Z;
            if (prims[(int)entry].IsInsideObject(point, zVec, out group_Z))
            {
                if (group_Z < zDist)
                {
                    zDist = group_Z;
                    hit = prims[(int)entry];
                }
            }
        }
    }

    public class WModelRayCallBack : WorkerCallback
    {
        public WModelRayCallBack(List<GroupModel> mod)
        {
            models = mod;
            hit = false;
        }
        public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
        {
            var result = models[(int)entry].IntersectRay(ray, ref distance, pStopAtFirstHit);
            if (result) hit = true;
            return hit;
        }

        private List<GroupModel> models;
        public bool hit;
    }

    public class GModelRayCallback : WorkerCallback
    {
        public GModelRayCallback(List<MeshTriangle> tris, List<Vector3> vert)
        {
            vertices = vert;
            triangles = tris;
            hit = false;
        }
        public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
        {
            var result = IntersectTriangle(triangles[(int)entry], vertices, ray, ref distance);
            if (result)
                hit = true;

            return hit;
        }

        private bool IntersectTriangle(MeshTriangle tri, List<Vector3> points, Ray ray, ref float distance)
        {
            const float EPS = 1e-5f;

            // See RTR2 ch. 13.7 for the algorithm.

            var e1 = points[(int)tri.idx1] - points[(int)tri.idx0];
            var e2 = points[(int)tri.idx2] - points[(int)tri.idx0];
            var p = new Vector3(ray.Direction.cross(e2));
            var a = e1.dot(p);

            if (Math.Abs(a) < EPS)
            {
                // Determinant is ill-conditioned; abort early
                return false;
            }

            var f = 1.0f / a;
            var s = new Vector3(ray.Origin - points[(int)tri.idx0]);
            var u = f * s.dot(p);

            if ((u < 0.0f) || (u > 1.0f))
            {
                // We hit the plane of the m_geometry, but outside the m_geometry
                return false;
            }

            var q = new Vector3(s.cross(e1));
            var v = f * ray.Direction.dot(q);

            if ((v < 0.0f) || ((u + v) > 1.0f))
            {
                // We hit the plane of the triangle, but outside the triangle
                return false;
            }

            var t = f * e2.dot(q);

            if ((t > 0.0f) && (t < distance))
            {
                // This is a new hit, closer than the previous one
                distance = t;
                return true;
            }
            // This hit is after the previous hit, so ignore it
            return false;
        }

        private List<Vector3> vertices;
        private List<MeshTriangle> triangles;
        public bool hit;
    }

    public class MapRayCallback : WorkerCallback
    {
        public MapRayCallback(ModelInstance[] val, ModelIgnoreFlags ignoreFlags)
        {
            prims = val;
            hit = false;
            flags = ignoreFlags;
        }
        public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit = true)
        {
            if (prims[entry] == null)
                return false;
            var result = prims[entry].IntersectRay(ray, ref distance, pStopAtFirstHit, flags);
            if (result)
                hit = true;
            return result;
        }
        public bool DidHit() { return hit; }

        private ModelInstance[] prims;
        private bool hit;
        private ModelIgnoreFlags flags;
    }

    public class AreaInfoCallback : WorkerCallback
    {
        public AreaInfoCallback(ModelInstance[] val)
        {
            prims = val;
        }
        public override void Invoke(Vector3 point, uint entry)
        {
            if (prims[entry] == null)
                return;

            prims[entry].IntersectPoint(point, aInfo);
        }

        private ModelInstance[] prims;
        public AreaInfo aInfo = new AreaInfo();
    }

    public class LocationInfoCallback : WorkerCallback
    {
        public LocationInfoCallback(ModelInstance[] val, LocationInfo info)
        {
            prims = val;
            locInfo = info;
            result = false;
        }

        public override void Invoke(Vector3 point, uint entry)
        {
            if (prims[entry] != null && prims[entry].GetLocationInfo(point, locInfo))
                result = true;
        }

        private ModelInstance[] prims;
        private LocationInfo locInfo;
        public bool result;
    }

    public class DynamicTreeIntersectionCallback : WorkerCallback
    {
        public DynamicTreeIntersectionCallback(PhaseShift phaseShift)
        {
            _didHit = false;
            _phaseShift = phaseShift;
        }

        public override bool Invoke(Ray r, IModel obj, ref float distance)
        {
            _didHit = obj.IntersectRay(r, ref distance, true, _phaseShift, ModelIgnoreFlags.Nothing);
            return _didHit;
        }

        public bool DidHit() { return _didHit; }

        private bool _didHit;
        private PhaseShift _phaseShift;
    }

    public class DynamicTreeAreaInfoCallback : WorkerCallback
    {
        public DynamicTreeAreaInfoCallback(PhaseShift phaseShift)
        {
            _phaseShift = phaseShift;
            _areaInfo = new AreaInfo();
        }

        public override void Invoke(Vector3 p, IModel obj)
        {
            obj.IntersectPoint(p, _areaInfo, _phaseShift);
        }

        public AreaInfo GetAreaInfo() { return _areaInfo; }

        private PhaseShift _phaseShift;
        private AreaInfo _areaInfo;
    }
}
