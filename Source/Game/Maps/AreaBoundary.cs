// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.Maps
{
    public class AreaBoundary
    {
        public class DoublePosition : Position
        {
            private readonly double _doublePosX;
            private readonly double _doublePosY;
            private readonly double _doublePosZ;

            public DoublePosition(double x = 0.0, double y = 0.0, double z = 0.0, float o = 0f) : base((float)x, (float)y, (float)z, o)
            {
                _doublePosX = x;
                _doublePosY = y;
                _doublePosZ = z;
            }

            public DoublePosition(float x, float y = 0f, float z = 0f, float o = 0f) : base(x, y, z, o)
            {
                _doublePosX = x;
                _doublePosY = y;
                _doublePosZ = z;
            }

            public DoublePosition(Position pos) : this(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation())
            {
            }

            public double GetDoublePositionX()
            {
                return _doublePosX;
            }

            public double GetDoublePositionY()
            {
                return _doublePosY;
            }

            public double GetDoublePositionZ()
            {
                return _doublePosZ;
            }

            public double GetDoubleExactDist2dSq(DoublePosition pos)
            {
                double offX = GetDoublePositionX() - pos.GetDoublePositionX();
                double offY = GetDoublePositionY() - pos.GetDoublePositionY();

                return (offX * offX) + (offY * offY);
            }

            public Position Sync()
            {
                X = (float)_doublePosX;
                Y = (float)_doublePosY;
                Z = (float)_doublePosZ;

                return this;
            }
        }

        private readonly bool _isInvertedBoundary;

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
    }
}