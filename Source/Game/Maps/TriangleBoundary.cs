// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public class TriangleBoundary : AreaBoundary
    {
        private readonly DoublePosition _a;
        private readonly double _abx;
        private readonly double _aby;
        private readonly DoublePosition _b;
        private readonly double _bcx;
        private readonly double _bcy;
        private readonly DoublePosition _c;
        private readonly double _cax;
        private readonly double _cay;

        public TriangleBoundary(Position pointA, Position pointB, Position pointC, bool isInverted = false) : base(isInverted)
        {
            _a = new DoublePosition(pointA);
            _b = new DoublePosition(pointB);
            _c = new DoublePosition(pointC);

            _abx = _b.GetDoublePositionX() - _a.GetDoublePositionX();
            _bcx = _c.GetDoublePositionX() - _b.GetDoublePositionX();
            _cax = _a.GetDoublePositionX() - _c.GetDoublePositionX();
            _aby = _b.GetDoublePositionY() - _a.GetDoublePositionY();
            _bcy = _c.GetDoublePositionY() - _b.GetDoublePositionY();
            _cay = _a.GetDoublePositionY() - _c.GetDoublePositionY();
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            // half-plane signs
            bool sign1 = ((-_b.GetDoublePositionX() + pos.GetPositionX()) * _aby - (-_b.GetDoublePositionY() + pos.GetPositionY()) * _abx) < 0;
            bool sign2 = ((-_c.GetDoublePositionX() + pos.GetPositionX()) * _bcy - (-_c.GetDoublePositionY() + pos.GetPositionY()) * _bcx) < 0;
            bool sign3 = ((-_a.GetDoublePositionX() + pos.GetPositionX()) * _cay - (-_a.GetDoublePositionY() + pos.GetPositionY()) * _cax) < 0;

            // if all signs are the same, the point is inside the triangle
            return ((sign1 == sign2) && (sign2 == sign3));
        }
    }
}