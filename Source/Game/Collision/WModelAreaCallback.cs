// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;

namespace Game.Collision
{
    public class WModelAreaCallback : WorkerCallback
    {
        private readonly List<GroupModel> _prims;
        private Vector3 _zVec;

        public WModelAreaCallback(List<GroupModel> vals, Vector3 down)
        {
            _prims = vals;
            Hit = null;
            ZDist = float.PositiveInfinity;
            _zVec = down;
        }

        public GroupModel Hit { get; set; }
        public float ZDist { get; set; }

        public override void Invoke(Vector3 point, uint entry)
        {
            float group_Z;

            if (_prims[(int)entry].IsInsideObject(point, _zVec, out group_Z))
                if (group_Z < ZDist)
                {
                    ZDist = group_Z;
                    Hit = _prims[(int)entry];
                }
        }
    }
}