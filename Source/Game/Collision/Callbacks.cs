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

using Framework.GameMath;
using System;
using System.Collections.Generic;
using Framework.Constants;

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
            Vector3 lo = vertices[(int)tri.idx0];
            Vector3 hi = lo;

            lo = (lo.Min(vertices[(int)tri.idx1])).Min(vertices[(int)tri.idx2]);
            hi = (hi.Max(vertices[(int)tri.idx1])).Max(vertices[(int)tri.idx2]);

            value = new AxisAlignedBox(lo, hi);
        }

        List<Vector3> vertices;
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

        List<GroupModel> prims;
        public GroupModel hit;
        public float zDist;
        Vector3 zVec;
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
            bool result = models[(int)entry].IntersectRay(ray, ref distance, pStopAtFirstHit);
            if (result) hit = true;
            return hit;
        }
        List<GroupModel> models;
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
            bool result = IntersectTriangle(triangles[(int)entry], vertices, ray, ref distance);
            if (result)
                hit = true;

            return hit;
        }

        bool IntersectTriangle(MeshTriangle tri, List<Vector3> points, Ray ray, ref float distance)
        {
            const float EPS = 1e-5f;

            // See RTR2 ch. 13.7 for the algorithm.

            Vector3 e1 = points[(int)tri.idx1] - points[(int)tri.idx0];
            Vector3 e2 = points[(int)tri.idx2] - points[(int)tri.idx0];
            Vector3 p = new Vector3(ray.Direction.cross(e2));
            float a = e1.dot(p);

            if (Math.Abs(a) < EPS)
            {
                // Determinant is ill-conditioned; abort early
                return false;
            }

            float f = 1.0f / a;
            Vector3 s = new Vector3(ray.Origin - points[(int)tri.idx0]);
            float u = f * s.dot(p);

            if ((u < 0.0f) || (u > 1.0f))
            {
                // We hit the plane of the m_geometry, but outside the m_geometry
                return false;
            }

            Vector3 q = new Vector3(s.cross(e1));
            float v = f * ray.Direction.dot(q);

            if ((v < 0.0f) || ((u + v) > 1.0f))
            {
                // We hit the plane of the triangle, but outside the triangle
                return false;
            }

            float t = f * e2.dot(q);

            if ((t > 0.0f) && (t < distance))
            {
                // This is a new hit, closer than the previous one
                distance = t;
                return true;
            }
            // This hit is after the previous hit, so ignore it
            return false;
        }

        List<Vector3> vertices;
        List<MeshTriangle> triangles;
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
            bool result = prims[entry].intersectRay(ray, ref distance, pStopAtFirstHit, flags);
            if (result)
                hit = true;
            return result;
        }
        public bool didHit() { return hit; }

        ModelInstance[] prims;
        bool hit;
        ModelIgnoreFlags flags;
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

            prims[entry].intersectPoint(point, aInfo);
        }

        ModelInstance[] prims;
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

        ModelInstance[] prims;
        LocationInfo locInfo;
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

        public bool didHit() { return _didHit; }

        bool _didHit;
        PhaseShift _phaseShift;
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

        PhaseShift _phaseShift;
        AreaInfo _areaInfo;
    }
}
