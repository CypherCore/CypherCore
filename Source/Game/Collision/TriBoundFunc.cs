// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.GameMath;

namespace Game.Collision
{
    public class TriBoundFunc
    {
        private readonly List<Vector3> _vertices;

        public TriBoundFunc(List<Vector3> vert)
        {
            _vertices = vert;
        }

        public void Invoke(MeshTriangle tri, out AxisAlignedBox value)
        {
            Vector3 lo = _vertices[(int)tri.idx0];
            Vector3 hi = lo;

            lo = Vector3.Min(Vector3.Min(lo, _vertices[(int)tri.idx1]), _vertices[(int)tri.idx2]);
            hi = Vector3.Max(Vector3.Max(hi, _vertices[(int)tri.idx1]), _vertices[(int)tri.idx2]);

            value = new AxisAlignedBox(lo, hi);
        }
    }
}