// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.GameMath;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Collision
{
    public class WorkerCallback
    {
        public virtual void Invoke(Vector3 point, uint entry) { }
        public virtual void Invoke(Vector3 point, GameObjectModel obj) { }
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

            lo = Vector3.Min(Vector3.Min(lo, vertices[(int)tri.idx1]), vertices[(int)tri.idx2]);
            hi = Vector3.Max(Vector3.Max(hi, vertices[(int)tri.idx1]), vertices[(int)tri.idx2]);

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
            hit = IntersectTriangle(triangles[(int)entry], vertices, ray, ref distance) || hit;
            return hit;
        }

        bool IntersectTriangle(MeshTriangle tri, List<Vector3> points, Ray ray, ref float distance)
        {
            const float EPS = 1e-5f;

            // See RTR2 ch. 13.7 for the algorithm.

            Vector3 e1 = points[(int)tri.idx1] - points[(int)tri.idx0];
            Vector3 e2 = points[(int)tri.idx2] - points[(int)tri.idx0];
            Vector3 p = Vector3.Cross(ray.Direction, e2);
            float a = Vector3.Dot(e1, p);

            if (Math.Abs(a) < EPS)
            {
                // Determinant is ill-conditioned; abort early
                return false;
            }

            float f = 1.0f / a;
            Vector3 s = ray.Origin - points[(int)tri.idx0];
            float u = f * Vector3.Dot(s, p);

            if ((u < 0.0f) || (u > 1.0f))
            {
                // We hit the plane of the m_geometry, but outside the m_geometry
                return false;
            }

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.Direction, q);

            if ((v < 0.0f) || ((u + v) > 1.0f))
            {
                // We hit the plane of the triangle, but outside the triangle
                return false;
            }

            float t = f * Vector3.Dot(e2, q);

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
            bool result = prims[entry].IntersectRay(ray, ref distance, pStopAtFirstHit, flags);
            if (result)
                hit = true;
            return result;
        }
        public bool DidHit() { return hit; }

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

            prims[entry].IntersectPoint(point, aInfo);
        }

        ModelInstance[] prims;
        public AreaInfo aInfo = new();
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

        public bool DidHit() { return _didHit; }

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

        public override void Invoke(Vector3 p, GameObjectModel obj)
        {
            obj.IntersectPoint(p, _areaInfo, _phaseShift);
        }

        public AreaInfo GetAreaInfo() { return _areaInfo; }

        PhaseShift _phaseShift;
        AreaInfo _areaInfo;
    }

    public class DynamicTreeLocationInfoCallback : WorkerCallback
    {
        public DynamicTreeLocationInfoCallback(PhaseShift phaseShift)
        {
            _phaseShift = phaseShift;
        }

        public override void Invoke(Vector3 p, GameObjectModel obj)
        {
            if (obj.GetLocationInfo(p, _locationInfo, _phaseShift))
                _hitModel = obj;
        }

        public LocationInfo GetLocationInfo() { return _locationInfo; }
        public GameObjectModel GetHitModel() { return _hitModel; }

        PhaseShift _phaseShift;
        LocationInfo _locationInfo = new();
        GameObjectModel _hitModel = new();
    }
}
