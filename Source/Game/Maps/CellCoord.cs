// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;

namespace Game.Maps
{
    public class CellCoord : ICoord
    {
        private const int LIMIT = MapConst.TotalCellsPerMap;

        public CellCoord(uint x, uint y)
        {
            X_coord = x;
            Y_coord = y;
        }

        public CellCoord(CellCoord obj)
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

        public static bool operator ==(CellCoord p1, CellCoord p2)
        {
            return (p1.X_coord == p2.X_coord && p1.Y_coord == p2.Y_coord);
        }

        public static bool operator !=(CellCoord p1, CellCoord p2)
        {
            return !(p1 == p2);
        }

        public override bool Equals(object obj)
        {
            if (obj is CellCoord)
                return (CellCoord)obj == this;

            return false;
        }

        public override int GetHashCode()
        {
            return X_coord.GetHashCode() ^ Y_coord.GetHashCode();
        }
    }
}