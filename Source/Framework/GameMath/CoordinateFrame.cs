// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Framework.GameMath
{
    public class CoordinateFrame
    {
        //Takes object space points to world space.
        Matrix4x4 rotation;

        //The origin of this coordinate frame in world space (or its parent's space, if nested).
        Vector3 translation;

        public CoordinateFrame(Matrix4x4 _rotation, Vector3 _translation)
        {
            rotation = _rotation;
            translation = _translation;
        }

        public Vector3 PointToWorldSpace(Vector3 v)
        {
            return new Vector3
            (rotation.M11 * v[0] + rotation.M12 * v[1] + rotation.M13 * v[2] + translation[0],
             rotation.M21 * v[0] + rotation.M22 * v[1] + rotation.M23 * v[2] + translation[1],
             rotation.M31 * v[0] + rotation.M32 * v[1] + rotation.M33 * v[2] + translation[2]);
        }

        public Cylinder ToWorldSpace(Cylinder c)
        {
            return new Cylinder(PointToWorldSpace(c.GetPoint(0)), PointToWorldSpace(c.GetPoint(1)), c.GetRadius());
        }

        public Box ToWorldSpace(AxisAlignedBox b)
        {
            Box b2 = new(b);
            return ToWorldSpace(b2);
        }

        public Box ToWorldSpace(Box b)
        {
            if (!b.isFinite())
            {
                return b;
            }
            Box outBox = new(b);
            outBox._center = PointToWorldSpace(b._center);
            for (int i = 0; i < 3; ++i)
            {
                outBox._edgeVector[i] = VectorToWorldSpace(outBox._edgeVector[i]);
            }

            outBox._area = b._area;
            outBox._volume = b._volume;

            return outBox;
        }

        public Vector3 VectorToWorldSpace(Vector3 v) => rotation.Multiply(v);
    }
}
