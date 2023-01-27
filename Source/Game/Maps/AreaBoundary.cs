// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
	public class AreaBoundary
	{
		private bool _isInvertedBoundary;

		public AreaBoundary(bool isInverted)
		{
			_isInvertedBoundary = isInverted;
		}

		public bool IsWithinBoundary(Position pos)
		{
			return pos != null && (IsWithinBoundaryArea(pos) != _isInvertedBoundary);
		}

		public virtual bool IsWithinBoundaryArea(Position pos)
		{
			return false;
		}

		public class DoublePosition : Position
		{
			private double doublePosX;
			private double doublePosY;
			private double doublePosZ;

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

			public DoublePosition(Position pos) : this(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation())
			{
			}

			public double GetDoublePositionX()
			{
				return doublePosX;
			}

			public double GetDoublePositionY()
			{
				return doublePosY;
			}

			public double GetDoublePositionZ()
			{
				return doublePosZ;
			}

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
		}
	}

	public class RectangleBoundary : AreaBoundary
	{
		private float _maxX;
		private float _maxY;

		private float _minX;

		private float _minY;

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
	}

	public class CircleBoundary : AreaBoundary
	{
		private DoublePosition _center;
		private double _radiusSq;

		public CircleBoundary(Position center, double radius, bool isInverted = false) : base(isInverted)
		{
			_center   = new DoublePosition(center);
			_radiusSq = radius * radius;
		}

		public CircleBoundary(Position center, Position pointOnCircle, bool isInverted = false) : base(isInverted)
		{
			_center   = new DoublePosition(center);
			_radiusSq = _center.GetDoubleExactDist2dSq(new DoublePosition(pointOnCircle));
		}

		public override bool IsWithinBoundaryArea(Position pos)
		{
			double offX = _center.GetDoublePositionX() - pos.GetPositionX();
			double offY = _center.GetDoublePositionY() - pos.GetPositionY();

			return offX * offX + offY * offY <= _radiusSq;
		}
	}

	public class EllipseBoundary : AreaBoundary
	{
		private DoublePosition _center;
		private double _radiusYSq;
		private double _scaleXSq;

		public EllipseBoundary(Position center, double radiusX, double radiusY, bool isInverted = false) : base(isInverted)
		{
			_center    = new DoublePosition(center);
			_radiusYSq = radiusY * radiusY;
			_scaleXSq  = _radiusYSq / (radiusX * radiusX);
		}

		public override bool IsWithinBoundaryArea(Position pos)
		{
			double offX = _center.GetDoublePositionX() - pos.GetPositionX();
			double offY = _center.GetDoublePositionY() - pos.GetPositionY();

			return (offX * offX) * _scaleXSq + (offY * offY) <= _radiusYSq;
		}
	}

	public class TriangleBoundary : AreaBoundary
	{
		private DoublePosition _a;
		private double _abx;
		private double _aby;
		private DoublePosition _b;
		private double _bcx;
		private double _bcy;
		private DoublePosition _c;
		private double _cax;
		private double _cay;

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

	public class ParallelogramBoundary : AreaBoundary
	{
		private DoublePosition _a;
		private double _abx;
		private double _aby;
		private DoublePosition _b;
		private DoublePosition _c;
		private DoublePosition _d;
		private double _dax;

		private double _day;

		// Note: AB must be orthogonal to AD
		public ParallelogramBoundary(Position cornerA, Position cornerB, Position cornerD, bool isInverted = false) : base(isInverted)
		{
			_a   = new DoublePosition(cornerA);
			_b   = new DoublePosition(cornerB);
			_d   = new DoublePosition(cornerD);
			_c   = new DoublePosition(_d.GetDoublePositionX() + (_b.GetDoublePositionX() - _a.GetDoublePositionX()), _d.GetDoublePositionY() + (_b.GetDoublePositionY() - _a.GetDoublePositionY()));
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

	public class ZRangeBoundary : AreaBoundary
	{
		private float _maxZ;

		private float _minZ;

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

	internal class BoundaryUnionBoundary : AreaBoundary
	{
		private AreaBoundary _b1;
		private AreaBoundary _b2;

		public BoundaryUnionBoundary(AreaBoundary b1, AreaBoundary b2, bool isInverted = false) : base(isInverted)
		{
			_b1 = b1;
			_b2 = b2;
		}

		public override bool IsWithinBoundaryArea(Position pos)
		{
			return _b1.IsWithinBoundary(pos) || _b2.IsWithinBoundary(pos);
		}
	}
}