// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public class ParallelogramBoundary : AreaBoundary
    {
        private readonly DoublePosition _a;
        private readonly double _abx;
        private readonly double _aby;
        private readonly DoublePosition _b;
        private readonly DoublePosition _c;
        private readonly DoublePosition _d;
        private readonly double _dax;

        private readonly double _day;

        // Note: AB must be orthogonal to AD
        public ParallelogramBoundary(Position cornerA, Position cornerB, Position cornerD, bool isInverted = false) : base(isInverted)
        {
            _a = new DoublePosition(cornerA);
            _b = new DoublePosition(cornerB);
            _d = new DoublePosition(cornerD);
            _c = new DoublePosition(_d.GetDoublePositionX() + (_b.GetDoublePositionX() - _a.GetDoublePositionX()), _d.GetDoublePositionY() + (_b.GetDoublePositionY() - _a.GetDoublePositionY()));
            _abx = _b.GetDoublePositionX() - _a.GetDoublePositionX();
            _dax = _a.GetDoublePositionX() - _d.GetDoublePositionX();
            _aby = _b.GetDoublePositionY() - _a.GetDoublePositionY();
            _day = _a.GetDoublePositionY() - _d.GetDoublePositionY();
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            // half-plane signs
            bool sign1 = ((-_b.GetDoublePositionX() + pos.GetPositionX()) * _aby - (-_b.GetDoublePositionY() + pos.GetPositionY()) * _abx) < 0;
            bool sign2 = ((-_a.GetDoublePositionX() + pos.GetPositionX()) * _day - (-_a.GetDoublePositionY() + pos.GetPositionY()) * _dax) < 0;
            bool sign3 = ((-_d.GetDoublePositionY() + pos.GetPositionY()) * _abx - (-_d.GetDoublePositionX() + pos.GetPositionX()) * _aby) < 0; // AB = -CD
            bool sign4 = ((-_c.GetDoublePositionY() + pos.GetPositionY()) * _dax - (-_c.GetDoublePositionX() + pos.GetPositionX()) * _day) < 0; // DA = -BC

            // if all signs are equal, the point is inside
            return ((sign1 == sign2) && (sign2 == sign3) && (sign3 == sign4));
        }
    }
}