// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;

namespace Framework.GameMath
{
    public class Cylinder
    {
        Vector3 p1;
        Vector3 p2;

        float mRadius;

        public Cylinder(Vector3 _p1, Vector3 _p2, float _r)
        {
            p1 = _p1;
            p2 = _p2;
            mRadius = _r;
        }

        public Vector3 GetPoint(int i) => (i == 0) ? p1 : p2;
        public float GetRadius() => mRadius;

        public bool Contains(Vector3 p)
        {
            return LineSegment.FromTwoPoints(p1, p2).DistanceSquared(p) <= Math.Pow(mRadius, 2);
        }
    }
}
