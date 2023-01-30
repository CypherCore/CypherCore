// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps
{
    public class GridCoord : ICoord
    {
        private const int LIMIT = MapConst.MaxGrids;

        public GridCoord(uint x, uint y)
        {
            X_coord = x;
            Y_coord = y;
        }

        public GridCoord(GridCoord obj)
        {
            X_coord = obj.X_coord;
            Y_coord = obj.Y_coord;
        }

        public bool IsCoordValid()
        {
            return X_coord < LIMIT && Y_coord < LIMIT;
        }

        public ICoord Normalize()
        {
            X_coord = Math.Min(X_coord, LIMIT - 1);
            Y_coord = Math.Min(Y_coord, LIMIT - 1);

            return this;
        }

        public uint GetId()
        {
            return Y_coord * LIMIT + X_coord;
        }

        public void Dec_x(uint val)
        {
            if (X_coord > val)
                X_coord -= val;
            else
                X_coord = 0;
        }

        public void Inc_x(uint val)
        {
            if (X_coord + val < LIMIT)
                X_coord += val;
            else
                X_coord = LIMIT - 1;
        }

        public void Dec_y(uint val)
        {
            if (Y_coord > val)
                Y_coord -= val;
            else
                Y_coord = 0;
        }

        public void Inc_y(uint val)
        {
            if (Y_coord + val < LIMIT)
                Y_coord += val;
            else
                Y_coord = LIMIT - 1;
        }


        public uint X_coord { get; set; }
        public uint Y_coord { get; set; }

        public static bool operator ==(GridCoord first, GridCoord other)
        {
            if (ReferenceEquals(first, other))
                return true;

            if (first is null ||
                other is null)
                return false;

            return first.Equals(other);
        }

        public static bool operator !=(GridCoord first, GridCoord other)
        {
            return !(first == other);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is ObjectGuid && Equals((ObjectGuid)obj);
        }

        public bool Equals(GridCoord other)
        {
            return other.X_coord == X_coord && other.Y_coord == Y_coord;
        }

        public override int GetHashCode()
        {
            return new
            {
                X_coord,
                Y_coord
            }.GetHashCode();
        }
    }
}