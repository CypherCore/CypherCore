// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;

namespace Framework.GameMath
{
    class CollisionDetection
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
    }
}
