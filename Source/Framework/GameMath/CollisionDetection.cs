// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;

namespace Framework.GameMath
{
    public class CollisionDetection
    {
        public static float collisionTimeForMovingPointFixedAABox(Vector3 origin, Vector3 dir, AxisAlignedBox box, ref Vector3 location, out bool Inside)
        {
            Vector3 normal = Vector3.Zero;
            if (collisionLocationForMovingPointFixedAABox(origin, dir, box, ref location, out Inside, ref normal))
            {
                return Vector3.Distance(location, origin);
            }
            else
            {
                return float.PositiveInfinity;
            }
        }

        public static bool collisionLocationForMovingPointFixedAABox(Vector3 origin, Vector3 dir, AxisAlignedBox box, ref Vector3 location, out bool Inside, ref Vector3 normal)
        {
            Inside = true;
            Vector3 MinB = box.Lo;
            Vector3 MaxB = box.Hi;
            Vector3 MaxT = new(-1.0f, -1.0f, -1.0f);

            // Find candidate planes.
            for (int i = 0; i < 3; ++i)
            {
                if (origin.GetAt(i) < MinB.GetAt(i))
                {
                    location.SetAt(MinB.GetAt(i), i);
                    Inside = false;

                    // Calculate T distances to candidate planes
                    if ((uint)dir.GetAt(i) != 0)
                    {
                        MaxT.SetAt((MinB.GetAt(i) - origin.GetAt(i)) / dir.GetAt(i), i);
                    }
                }
                else if (origin.GetAt(i) > MaxB.GetAt(i))
                {
                    location.SetAt(MaxB.GetAt(i), i);
                    Inside = false;

                    // Calculate T distances to candidate planes
                    if ((uint)dir.GetAt(i) != 0)
                    {
                        MaxT.SetAt((MaxB.GetAt(i) - origin.GetAt(i)) / dir.GetAt(i), i);
                    }
                }
            }

            if (Inside)
            {
                // Ray origin inside bounding box
                location = origin;
                return false;
            }

            // Get largest of the maxT's for final choice of intersection
            int WhichPlane = 0;
            if (MaxT.Y > MaxT.GetAt(WhichPlane))
            {
                WhichPlane = 1;
            }

            if (MaxT.Z > MaxT.GetAt(WhichPlane))
            {
                WhichPlane = 2;
            }

            // Check final candidate actually inside box
            if (Convert.ToBoolean((uint)MaxT.GetAt(WhichPlane) & 0x80000000))
            {
                // Miss the box
                return false;
            }

            for (int i = 0; i < 3; ++i)
            {
                if (i != WhichPlane)
                {
                    location.SetAt(origin.GetAt(i) + MaxT.GetAt(WhichPlane) * dir.GetAt(i), i);
                    if ((location.GetAt(i) < MinB.GetAt(i)) ||
                        (location.GetAt(i) > MaxB.GetAt(i)))
                    {
                        // On this plane we're outside the box extents, so
                        // we miss the box
                        return false;
                    }
                }
            }

            // Choose the normal to be the plane normal facing into the ray
            normal = Vector3.Zero;
            normal.SetAt((float)((dir.GetAt(WhichPlane) > 0) ? -1.0 : 1.0), WhichPlane);

            return true;
        }

        public static Vector3 closestPointToRectangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 point)
        {
            Plane plane = Plane.CreateFromVertices(v0, v1, v2);

            // Project the point into the plane
            double a = plane.Normal.X, b = plane.Normal.Y, c = plane.Normal.Z, d = plane.D;

            double distance = a * point.X + b * point.Y + c * point.Z + d;
            Vector3 planePoint = point - Vector3.Multiply(plane.Normal, (float)distance);

            if (isPointInsideRectangle(v0, v1, v2, v3, plane.Normal, planePoint))
            {
                return planePoint;
            }
            else
            {
                return closestPointToRectanglePerimeter(v0, v1, v2, v3, planePoint);
            }
        }

        public static bool isPointInsideRectangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal, Vector3 point)
        {
            return isPointInsideTriangle(v0, v1, v2, normal, point) ||
                   isPointInsideTriangle(v2, v3, v0, normal, point);
        }

        public static bool isPointInsideTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal, Vector3 point, int primaryAxis = -1)
        {
            float[] b = new float[3];
            return isPointInsideTriangle(v0, v1, v2, normal, point, b, primaryAxis);
        }

        public static bool isPointInsideTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal, Vector3 point, float[] b, int primaryAxis)
        {
            if (primaryAxis == -1)
            {
                primaryAxis = normal.primaryAxis();
            }

            // Check that the point is within the triangle using a Barycentric
            // coordinate test on a two dimensional plane.
            int i, j;

            switch (primaryAxis)
            {
                case 0:
                    i = 1;
                    j = 2;
                    break;

                case 1:
                    i = 2;
                    j = 0;
                    break;

                case 2:
                    i = 0;
                    j = 1;
                    break;

                default:
                    // This case is here to supress a warning on Linux
                    i = j = 0;
                    Cypher.Assert(false, "Should not get here.");
                    break;
            }

            // See if all barycentric coordinates are non-negative

            // 2D area via cross product
            float AREA2(Vector3 d, Vector3 e, Vector3 f) => (e[i] - d[i]) * (f[j] - d[j]) - (f[i] - d[i]) * (e[j] - d[j]);

            // Area of the polygon
            float area = AREA2(v0, v1, v2);
            if (area == 0)
            {
                // This triangle has zero area, so the point must not
                // be in it unless the triangle point is the test point.
                return v0 == point;
            }

            Cypher.Assert(area != 0);

            float invArea = 1.0f / area;

            // (avoid normalization until absolutely necessary)
            b[0] = AREA2(point, v1, v2) * invArea;

            if ((b[0] < 0.0f) || (b[0] > 1.0f))
            {
                return false;
            }

            b[1] = AREA2(v0, point, v2) * invArea;
            if ((b[1] < 0.0f) || (b[1] > 1.0f))
            {
                return false;
            }

            b[2] = 1.0f - b[0] - b[1];

            return (b[2] >= 0.0f) && (b[2] <= 1.0f);
        }

        public static Vector3 closestPointToRectanglePerimeter(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 point)
        {
            Vector3 r0 = closestPointOnLineSegment(v0, v1, point);
            Vector3 r1 = closestPointOnLineSegment(v1, v2, point);
            Vector3 r2 = closestPointOnLineSegment(v2, v3, point);
            Vector3 r3 = closestPointOnLineSegment(v3, v0, point);

            double d0 = (r0 - point).LengthSquared();
            double d1 = (r1 - point).LengthSquared();
            double d2 = (r2 - point).LengthSquared();
            double d3 = (r3 - point).LengthSquared();

            if (d0 < d1)
            {
                if (d0 < d2)
                {
                    if (d0 < d3)
                    {
                        return r0;
                    }
                    else
                    {
                        return r3;
                    }
                }
                else
                {
                    if (d2 < d3)
                    {
                        return r2;
                    }
                    else
                    {
                        return r3;
                    }
                }
            }
            else
            {
                if (d1 < d2)
                {
                    if (d1 < d3)
                    {
                        return r1;
                    }
                    else
                    {
                        return r3;
                    }
                }
                else
                {
                    if (d2 < d3)
                    {
                        return r2;
                    }
                    else
                    {
                        return r3;
                    }
                }
            }
        }

        public static Vector3 closestPointOnLineSegment(Vector3 v0, Vector3 v1, Vector3 point)
        {

            Vector3 edge = (v1 - v0);
            float edgeLength = edge.Magnitude();

            if (edgeLength == 0)
            {
                // The line segment is a point
                return v0;
            }

            return closestPointOnLineSegment(v0, v1, edge / edgeLength, edgeLength, point);
        }

        public static Vector3 closestPointOnLineSegment(Vector3 v0, Vector3 v1, Vector3 edgeDirection, float edgeLength, Vector3 point)
        {
            Cypher.Assert((v1 - v0).direction().fuzzyEq(edgeDirection));
            Cypher.Assert(MathFunctions.fuzzyEq((v1 - v0).Magnitude(), edgeLength));

            // Vector towards the point
            Vector3 c = point - v0;

            // Projected onto the edge itself
            float t = Vector3.Dot(edgeDirection, c);

            if (t <= 0)
            {
                // Before the start
                return v0;
            }
            else if (t >= edgeLength)
            {
                // After the end
                return v1;
            }
            else
            {
                // At distance t along the edge
                return v0 + edgeDirection * t;
            }
        }
    }
}
