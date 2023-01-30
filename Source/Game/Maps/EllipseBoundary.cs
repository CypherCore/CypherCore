// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public class EllipseBoundary : AreaBoundary
    {
        private readonly DoublePosition _center;
        private readonly double _radiusYSq;
        private readonly double _scaleXSq;

        public EllipseBoundary(Position center, double radiusX, double radiusY, bool isInverted = false) : base(isInverted)
        {
            _center = new DoublePosition(center);
            _radiusYSq = radiusY * radiusY;
            _scaleXSq = _radiusYSq / (radiusX * radiusX);
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            double offX = _center.GetDoublePositionX() - pos.GetPositionX();
            double offY = _center.GetDoublePositionY() - pos.GetPositionY();

            return (offX * offX) * _scaleXSq + (offY * offY) <= _radiusYSq;
        }
    }
}