/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Game.Entities;

namespace Game.Maps
{
    public class AreaBoundary
    {
        public AreaBoundary(bool isInverted)
        {
            _isInvertedBoundary = isInverted;
        }

        public bool IsWithinBoundary(Position pos) { return IsWithinBoundaryArea(pos) != _isInvertedBoundary; }

        public virtual bool IsWithinBoundaryArea(Position pos) { return false; }

        bool _isInvertedBoundary;

        public class DoublePosition : Position
        {
            public DoublePosition(double x = 0.0, double y = 0.0, double z = 0.0, float o = 0f) : base((float)x, (float)y, (float)z, o)
            {
                doublePosX = x;
                doublePosY = y;
                doublePosZ = z;
            }
            public DoublePosition(float x, float y = 0f, float z = 0f, float o = 0f) : base(x, y, z, o)
            {
                doublePosX = x;
                doublePosY = y;
                doublePosZ = z;
            }
            public DoublePosition(Position pos) : this(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation()) { }

            public double GetDoublePositionX() { return doublePosX; }
            public double GetDoublePositionY() { return doublePosY; }
            public double GetDoublePositionZ() { return doublePosZ; }

            public double GetDoubleExactDist2dSq(DoublePosition pos)
            {
                double offX = GetDoublePositionX() - pos.GetDoublePositionX();
                double offY = GetDoublePositionY() - pos.GetDoublePositionY();
                return (offX * offX) + (offY * offY);
            }

            public Position Sync()
            {
                posX = (float)doublePosX;
                posY = (float)doublePosY;
                posZ = (float)doublePosZ;
                return this;
            }

            double doublePosX;
            double doublePosY;
            double doublePosZ;
        }
    }

    public class RectangleBoundary : AreaBoundary
    {
        // X axis is north/south, Y axis is east/west, larger values are northwest
        public RectangleBoundary(float southX, float northX, float eastY, float westY, bool isInverted = false) : base(isInverted)
        {
            _minX = southX;
            _maxX = northX;
            _minY = eastY;
            _maxY = westY;
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            if (pos == null)
                return false;

            return !(pos.GetPositionX() < _minX || pos.GetPositionX() > _maxX || pos.GetPositionY() < _minY || pos.GetPositionY() > _maxY);
        }

        float _minX;
        float _maxX;
        float _minY;
        float _maxY;
    }

    public class CircleBoundary : AreaBoundary
    {
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
            if (pos == null)
                return false;

            double offX = _center.GetDoublePositionX() - pos.GetPositionX();
            double offY = _center.GetDoublePositionY() - pos.GetPositionY();
            return offX * offX + offY * offY <= _radiusSq;
        }

        DoublePosition _center;
        double _radiusSq;
    }

    public class EllipseBoundary : AreaBoundary
    {
        public EllipseBoundary(Position center, double radiusX, double radiusY, bool isInverted = false) : base(isInverted)
        {
            _center = new DoublePosition(center);
            _radiusYSq = radiusY * radiusY;
            _scaleXSq = _radiusYSq / (radiusX * radiusX);
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            if (pos == null)
                return false;

            double offX = _center.GetDoublePositionX() - pos.GetPositionX(), offY = _center.GetDoublePositionY() - pos.GetPositionY();
            return (offX * offX) * _scaleXSq + (offY * offY) <= _radiusYSq;
        }

        DoublePosition _center;
        double _radiusYSq;
        double _scaleXSq;
    }

    public class TriangleBoundary : AreaBoundary
    {
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
            if (pos == null)
                return false;

            // half-plane signs
            bool sign1 = ((-_b.GetDoublePositionX() + pos.GetPositionX()) * _aby - (-_b.GetDoublePositionY() + pos.GetPositionY()) * _abx) < 0;
            bool sign2 = ((-_c.GetDoublePositionX() + pos.GetPositionX()) * _bcy - (-_c.GetDoublePositionY() + pos.GetPositionY()) * _bcx) < 0;
            bool sign3 = ((-_a.GetDoublePositionX() + pos.GetPositionX()) * _cay - (-_a.GetDoublePositionY() + pos.GetPositionY()) * _cax) < 0;

            // if all signs are the same, the point is inside the triangle
            return ((sign1 == sign2) && (sign2 == sign3));
        }

        DoublePosition _a;
        DoublePosition _b;
        DoublePosition _c;
        double _abx;
        double _bcx;
        double _cax;
        double _aby;
        double _bcy;
        double _cay;
    }

    public class ParallelogramBoundary : AreaBoundary
    {
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
            if (pos == null)
                return false;

            // half-plane signs
            bool sign1 = ((-_b.GetDoublePositionX() + pos.GetPositionX()) * _aby - (-_b.GetDoublePositionY() + pos.GetPositionY()) * _abx) < 0;
            bool sign2 = ((-_a.GetDoublePositionX() + pos.GetPositionX()) * _day - (-_a.GetDoublePositionY() + pos.GetPositionY()) * _dax) < 0;
            bool sign3 = ((-_d.GetDoublePositionY() + pos.GetPositionY()) * _abx - (-_d.GetDoublePositionX() + pos.GetPositionX()) * _aby) < 0; // AB = -CD
            bool sign4 = ((-_c.GetDoublePositionY() + pos.GetPositionY()) * _dax - (-_c.GetDoublePositionX() + pos.GetPositionX()) * _day) < 0; // DA = -BC

            // if all signs are equal, the point is inside
            return ((sign1 == sign2) && (sign2 == sign3) && (sign3 == sign4));
        }

        DoublePosition _a;
        DoublePosition _b;
        DoublePosition _d;
        DoublePosition _c;
        double _abx;
        double _dax;
        double _aby;
        double _day;
    }

    public class ZRangeBoundary : AreaBoundary
    {
        public ZRangeBoundary(float minZ, float maxZ, bool isInverted = false) : base(isInverted)
        {
            _minZ = minZ;
            _maxZ = maxZ;
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            if (pos == null)
                return false;

            return !(pos.GetPositionZ() < _minZ || pos.GetPositionZ() > _maxZ);
        }

        float _minZ;
        float _maxZ;
    }
}
