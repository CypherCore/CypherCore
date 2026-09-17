// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Framework.GameMath
{
    internal class LineSegment
    {
        Vector3 point;

        /** Not normalized */
        Vector3 direction;

        public LineSegment(Vector3 _point, Vector3 _direction)
        {
            point = _point;
            direction = _direction;
        }

        public static LineSegment FromTwoPoints(Vector3 point1, Vector3 point2)
        {
            return new LineSegment(point1, point2 - point1);
        }

        public double DistanceSquared(Vector3 p)
        {
            return (closestPoint(p) - p).LengthSquared();
        }

        Vector3 closestPoint(Vector3 p)
        {

            // The vector from the end of the capsule to the point in question.
            Vector3 v = p - point;

            // Projection of v onto the line segment scaled by 
            // the length of direction.
            float t = Vector3.Dot(direction, v);

            // Avoid some square roots.  Derivation:
            //    t/direction.length() <= direction.length()
            //      t <= direction.squaredLength()

            if ((t >= 0) && (t <= direction.LengthSquared()))
            {

                // The point falls within the segment.  Normalize direction,
                // divide t by the length of direction.
                return point + direction * t / direction.LengthSquared();

            }
            else
            {

                // The point does not fall within the segment; see which end is closer.

                // Distance from 0, squared
                float d0Squared = v.LengthSquared();

                // Distance from 1, squared
                float d1Squared = (v - direction).LengthSquared();

                if (d0Squared < d1Squared)
                {

                    // Point 0 is closer
                    return point;

                }
                else
                {

                    // Point 1 is closer
                    return point + direction;

                }
            }

        }
    }
}
