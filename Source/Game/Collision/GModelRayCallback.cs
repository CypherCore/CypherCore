// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.GameMath;

namespace Game.Collision
{
    public class GModelRayCallback : WorkerCallback
    {
        public bool Hit { get; set; }
        private readonly List<MeshTriangle> _triangles;

        private readonly List<Vector3> _vertices;

        public GModelRayCallback(List<MeshTriangle> tris, List<Vector3> vert)
        {
            _vertices = vert;
            _triangles = tris;
            Hit = false;
        }

        public override bool Invoke(Ray ray, uint entry, ref float distance, bool pStopAtFirstHit)
        {
            Hit = IntersectTriangle(_triangles[(int)entry], _vertices, ray, ref distance) || Hit;

            return Hit;
        }

        private bool IntersectTriangle(MeshTriangle tri, List<Vector3> points, Ray ray, ref float distance)
        {
            const float EPS = 1e-5f;

            // See RTR2 ch. 13.7 for the algorithm.

            Vector3 e1 = points[(int)tri.idx1] - points[(int)tri.idx0];
            Vector3 e2 = points[(int)tri.idx2] - points[(int)tri.idx0];
            Vector3 p = Vector3.Cross(ray.Direction, e2);
            float a = Vector3.Dot(e1, p);

            if (Math.Abs(a) < EPS)
                // Determinant is ill-conditioned; abort early
                return false;

            float f = 1.0f / a;
            Vector3 s = ray.Origin - points[(int)tri.idx0];
            float u = f * Vector3.Dot(s, p);

            if ((u < 0.0f) ||
                (u > 1.0f))
                // We hit the plane of the _geometry, but outside the _geometry
                return false;

            Vector3 q = Vector3.Cross(s, e1);
            float v = f * Vector3.Dot(ray.Direction, q);

            if ((v < 0.0f) ||
                ((u + v) > 1.0f))
                // We hit the plane of the triangle, but outside the triangle
                return false;

            float t = f * Vector3.Dot(e2, q);

            if ((t > 0.0f) &&
                (t < distance))
            {
                // This is a new hit, closer than the previous one
                distance = t;

                return true;
            }

            // This hit is after the previous hit, so ignore it
            return false;
        }
    }
}