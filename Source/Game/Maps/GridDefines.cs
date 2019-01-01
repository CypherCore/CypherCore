/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Maps
{
    public class GridDefines
    {
        public static bool IsValidMapCoord(float c)
        {
            return !float.IsInfinity(c) && (Math.Abs(c) <= (MapConst.MapHalfSize - 0.5f));
        }

        public static bool IsValidMapCoord(float x, float y)
        {
            return (IsValidMapCoord(x) && IsValidMapCoord(y));
        }

        public static bool IsValidMapCoord(float x, float y, float z)
        {
            return IsValidMapCoord(x, y) && IsValidMapCoord(z);
        }

        public static bool IsValidMapCoord(float x, float y, float z, float o)
        {
            return IsValidMapCoord(x, y, z) && !float.IsInfinity(o);
        }

        public static bool IsValidMapCoord(uint mapid, float x, float y)
        {
            return Global.MapMgr.IsValidMAP(mapid, false) && IsValidMapCoord(x, y);
        }

        public static bool IsValidMapCoord(uint mapid, float x, float y, float z)
        {
            return Global.MapMgr.IsValidMAP(mapid, false) && IsValidMapCoord(x, y, z);
        }

        public static bool IsValidMapCoord(uint mapid, float x, float y, float z, float o)
        {
            return Global.MapMgr.IsValidMAP(mapid, false) && IsValidMapCoord(x, y, z, o);
        }

        public static bool IsValidMapCoord(WorldLocation loc)
        {
            return IsValidMapCoord(loc.GetMapId(), loc.GetPositionX(), loc.GetPositionY(), loc.GetPositionZ(), loc.GetOrientation());
        }

        public static void NormalizeMapCoord(ref float c)
        {
            if (c > MapConst.MapHalfSize - 0.5f)
                c = MapConst.MapHalfSize - 0.5f;
            else if (c < -(MapConst.MapHalfSize - 0.5f))
                c = -(MapConst.MapHalfSize - 0.5f);
        }

        public static GridCoord ComputeGridCoord(float x, float y)
        {
            double x_offset = ((double)x - MapConst.CenterGridOffset) / MapConst.SizeofGrids;
            double y_offset = ((double)y - MapConst.CenterGridOffset) / MapConst.SizeofGrids;

            uint x_val = (uint)(x_offset + MapConst.CenterGridId + 0.5f);
            uint y_val = (uint)(y_offset + MapConst.CenterGridId + 0.5f);
            return new GridCoord(x_val, y_val);
        }

        public static CellCoord ComputeCellCoord(float x, float y)
        {
            double x_offset = ((double)x - MapConst.CenterGridCellOffset) / MapConst.SizeofCells;
            double y_offset = ((double)y - MapConst.CenterGridCellOffset) / MapConst.SizeofCells;

            uint x_val = (uint)(x_offset + MapConst.CenterGridCellId + 0.5f);
            uint y_val = (uint)(y_offset + MapConst.CenterGridCellId + 0.5f);
            return new CellCoord(x_val, y_val);
        }
    }

    public class CellCoord : ICoord
    {
        const int Limit = MapConst.TotalCellsPerMap;

        public CellCoord(uint x, uint y)
        {
            x_coord = x;
            y_coord = y;
        }
        public CellCoord(CellCoord obj)
        {
            x_coord = obj.x_coord;
            y_coord = obj.y_coord;
        }

        public bool IsCoordValid()
        {
            return x_coord < Limit && y_coord < Limit;
        }
        public ICoord normalize()
        {
            x_coord = Math.Min(x_coord, Limit - 1);
            y_coord = Math.Min(y_coord, Limit - 1);
            return this;
        }
        public uint GetId()
        {
            return y_coord * Limit + x_coord;
        }

        public void dec_x(uint val)
        {
            if (x_coord > val)
                x_coord -= val;
            else
                x_coord = 0;
        }
        public void inc_x(uint val)
        {
            if (x_coord + val < Limit)
                x_coord += val;
            else
                x_coord = Limit - 1;
        }

        public void dec_y(uint val)
        {
            if (y_coord > val)
                y_coord -= val;
            else
                y_coord = 0;
        }
        public void inc_y(uint val)
        {
            if (y_coord + val < Limit)
                y_coord += val;
            else
                y_coord = Limit - 1;
        }

        public static bool operator ==(CellCoord p1, CellCoord p2)
        {
            return (p1.x_coord == p2.x_coord && p1.y_coord == p2.y_coord);
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
            return x_coord.GetHashCode() ^ y_coord.GetHashCode();
        }

        public uint x_coord { get; set; }
        public uint y_coord { get; set; }
    }

    public class GridCoord : ICoord
    {
        const int Limit = MapConst.MaxGrids;

        public GridCoord(uint x, uint y)
        {
            x_coord = x;
            y_coord = y;
        }
        public GridCoord(GridCoord obj)
        {
            x_coord = obj.x_coord;
            y_coord = obj.y_coord;
        }

        public bool IsCoordValid()
        {
            return x_coord < Limit && y_coord < Limit;
        }
        public ICoord normalize()
        {
            x_coord = Math.Min(x_coord, Limit - 1);
            y_coord = Math.Min(y_coord, Limit - 1);
            return this;
        }
        public uint GetId()
        {
            return y_coord * Limit + x_coord;
        }
        
        public void dec_x(uint val)
        {
            if (x_coord > val)
                x_coord -= val;
            else
                x_coord = 0;
        }
        public void inc_x(uint val)
        {
            if (x_coord + val < Limit)
                x_coord += val;
            else
                x_coord = Limit - 1;
        }

        public void dec_y(uint val)
        {
            if (y_coord > val)
                y_coord -= val;
            else
                y_coord = 0;
        }
        public void inc_y(uint val)
        {
            if (y_coord + val < Limit)
                y_coord += val;
            else
                y_coord = Limit - 1;
        }

        public static bool operator ==(GridCoord first, GridCoord other)
        {
            if (ReferenceEquals(first, other))
                return true;

            if ((object)first == null || (object)other == null)
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
            return other.x_coord == x_coord && other.y_coord == y_coord;
        }

        public override int GetHashCode()
        {
            return new { x_coord, y_coord }.GetHashCode();
        }


        public uint x_coord { get; set; }
        public uint y_coord { get; set; }
    }

    public interface ICoord
    {
        bool IsCoordValid();
        ICoord normalize();
        uint GetId();
        void dec_x(uint val);
        void inc_x(uint val);
        void dec_y(uint val);
        void inc_y(uint val);

        uint x_coord { get; set; }
        uint y_coord { get; set; }
    }
}
