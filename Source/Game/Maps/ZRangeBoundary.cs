// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public class ZRangeBoundary : AreaBoundary
    {
        private readonly float _maxZ;

        private readonly float _minZ;

        public ZRangeBoundary(float minZ, float maxZ, bool isInverted = false) : base(isInverted)
        {
            _minZ = minZ;
            _maxZ = maxZ;
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            return (_minZ <= pos.GetPositionZ() && pos.GetPositionZ() <= _maxZ);
        }
    }
}