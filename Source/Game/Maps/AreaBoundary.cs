// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using System.Collections.Generic;

namespace Game.Maps
{
    public class AreaBoundary
    {
        public AreaBoundary(bool isInverted)
        {
            _isInvertedBoundary = isInverted;
        }

        public bool IsWithinBoundary(Position pos) { return pos != null && (IsWithinBoundaryArea(pos) != _isInvertedBoundary); }

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
            return !(pos.GetPositionX() < _minX || pos.GetPositionX() > _maxX || pos.GetPositionY() < _minY || pos.GetPositionY() > _maxY);
        }

        float _minX;
        float _maxX;
        float _minY;
        float _maxY;
    }

    public class CircleBoundary : AreaBoundary
    {
        public CircleBoundary(Position center, float radius, bool isInverted = false) : base(isInverted)
        {
            _center = new Position(center);
            _radiusSq = radius * radius;
        }
        public CircleBoundary(Position center, Position pointOnCircle, bool isInverted = false) : base(isInverted)
        {
            _center = new Position(center);
            _radiusSq = _center.GetExactDist2dSq(new Position(pointOnCircle));
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            return _center.GetExactDistSq(pos) <= _radiusSq;
        }

        Position _center;
        float _radiusSq;
    }

    public class EllipseBoundary : AreaBoundary
    {
        public EllipseBoundary(Position center, float radiusX, float radiusY, bool isInverted = false) : base(isInverted)
        {
            _center = new Position(center);
            _radiusYSq = radiusY * radiusY;
            _scaleXSq = _radiusYSq / (radiusX * radiusX);
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            float offX = _center.GetPositionX() - pos.GetPositionX();
            float offY = _center.GetPositionY() - pos.GetPositionY();
            return (offX * offX) * _scaleXSq + (offY * offY) <= _radiusYSq;
        }

        Position _center;
        float _radiusYSq;
        float _scaleXSq;
    }

    public class TriangleBoundary : AreaBoundary
    {
        public TriangleBoundary(Position pointA, Position pointB, Position pointC, bool isInverted = false) : base(isInverted)
        {
            _a = new Position(pointA);
            _b = new Position(pointB);
            _c = new Position(pointC);

            _abx = _b.GetPositionX() - _a.GetPositionX();
            _bcx = _c.GetPositionX() - _b.GetPositionX();
            _cax = _a.GetPositionX() - _c.GetPositionX();
            _aby = _b.GetPositionY() - _a.GetPositionY();
            _bcy = _c.GetPositionY() - _b.GetPositionY();
            _cay = _a.GetPositionY() - _c.GetPositionY();
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            // half-plane signs
            bool sign1 = ((-_b.GetPositionX() + pos.GetPositionX()) * _aby - (-_b.GetPositionY() + pos.GetPositionY()) * _abx) < 0;
            bool sign2 = ((-_c.GetPositionX() + pos.GetPositionX()) * _bcy - (-_c.GetPositionY() + pos.GetPositionY()) * _bcx) < 0;
            bool sign3 = ((-_a.GetPositionX() + pos.GetPositionX()) * _cay - (-_a.GetPositionY() + pos.GetPositionY()) * _cax) < 0;

            // if all signs are the same, the point is inside the triangle
            return ((sign1 == sign2) && (sign2 == sign3));
        }

        Position _a;
        Position _b;
        Position _c;
        float _abx;
        float _bcx;
        float _cax;
        float _aby;
        float _bcy;
        float _cay;
    }

    public class ParallelogramBoundary : AreaBoundary
    {
        // Note: AB must be orthogonal to AD
        public ParallelogramBoundary(Position cornerA, Position cornerB, Position cornerD, bool isInverted = false) : base(isInverted)
        {
            _a = new Position(cornerA);
            _b = new Position(cornerB);
            _d = new Position(cornerD);
            _c = new Position(_d.GetPositionX() + (_b.GetPositionX() - _a.GetPositionX()), _d.GetPositionY() + (_b.GetPositionY() - _a.GetPositionY()));
            _abx = _b.GetPositionX() - _a.GetPositionX();
            _dax = _a.GetPositionX() - _d.GetPositionX();
            _aby = _b.GetPositionY() - _a.GetPositionY();
            _day = _a.GetPositionY() - _d.GetPositionY();
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            // half-plane signs
            bool sign1 = ((-_b.GetPositionX() + pos.GetPositionX()) * _aby - (-_b.GetPositionY() + pos.GetPositionY()) * _abx) < 0;
            bool sign2 = ((-_a.GetPositionX() + pos.GetPositionX()) * _day - (-_a.GetPositionY() + pos.GetPositionY()) * _dax) < 0;
            bool sign3 = ((-_d.GetPositionY() + pos.GetPositionY()) * _abx - (-_d.GetPositionX() + pos.GetPositionX()) * _aby) < 0; // AB = -CD
            bool sign4 = ((-_c.GetPositionY() + pos.GetPositionY()) * _dax - (-_c.GetPositionX() + pos.GetPositionX()) * _day) < 0; // DA = -BC

            // if all signs are equal, the point is inside
            return ((sign1 == sign2) && (sign2 == sign3) && (sign3 == sign4));
        }

        Position _a;
        Position _b;
        Position _d;
        Position _c;
        float _abx;
        float _dax;
        float _aby;
        float _day;
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
            return pos.GetPositionZ() >= _minZ && pos.GetPositionZ() <= _maxZ;
        }

        float _minZ;
        float _maxZ;
    }

    class PolygonBoundary : AreaBoundary
    {
        Position _origin;
        List<Position> _vertices;

        public PolygonBoundary(Position origin, List<Position> vertices, bool isInverted = false) : base(isInverted)
        {
            _origin = origin;
            _vertices = vertices;
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            return pos.IsInPolygon2D(_origin, _vertices);
        }
    }

    class BoundaryUnionBoundary : AreaBoundary
    {
        public BoundaryUnionBoundary(AreaBoundary b1, AreaBoundary b2, bool isInverted = false) : base(isInverted)
        {
            _b1 = b1;
            _b2 = b2;
        }

        public override bool IsWithinBoundaryArea(Position pos)
        {
            return _b1.IsWithinBoundary(pos) || _b2.IsWithinBoundary(pos);
        }

        AreaBoundary _b1;
        AreaBoundary _b2;
    }
}
