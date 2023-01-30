// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public class CircleBoundary : AreaBoundary
    {
        private readonly DoublePosition _center;
        private readonly double _radiusSq;

        public CircleBoundary(Position center, double radius, bool isInverted = false) : base(isInverted)
        {
            _center = new DoublePosition(center);
            _radiusSq = radius * radius;
        }

        public CircleBoundary(Position center, Position pointOnCircle, bool isInverted = false) : base(isInverted)
        {
            _center = new DoublePosition(center);
            _radiusSq = _center.GetDoubleExactDist2dSq(new DoublePosition(pointOnCircle));
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            double offX = _center.GetDoublePositionX() - pos.GetPositionX();
            double offY = _center.GetDoublePositionY() - pos.GetPositionY();

            return offX * offX + offY * offY <= _radiusSq;
        }
    }
}