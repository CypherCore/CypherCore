// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Maps
{
    public class GridDefines
    {
        public static bool IsValidMapCoord(float c)
        {
            return float.IsFinite(c) && (Math.Abs(c) <= (MapConst.MapHalfSize - 0.5f));
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
            return IsValidMapCoord(x, y, z) && float.IsFinite(o);
        }
        
        public static bool IsValidMapCoord(uint mapid, float x, float y)
        {
            return Global.MapMgr.IsValidMAP(mapid) && IsValidMapCoord(x, y);
        }

        public static bool IsValidMapCoord(uint mapid, float x, float y, float z)
        {
            return Global.MapMgr.IsValidMAP(mapid) && IsValidMapCoord(x, y, z);
        }

        public static bool IsValidMapCoord(uint mapid, float x, float y, float z, float o)
        {
            return Global.MapMgr.IsValidMAP(mapid) && IsValidMapCoord(x, y, z, o);
        }

        public static bool IsValidMapCoord(uint mapid, Position pos)
        {
            return IsValidMapCoord(mapid, pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation());
        }

        public static bool IsValidMapCoord(WorldLocation loc)
        {
            return IsValidMapCoord(loc.GetMapId(), loc);
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

        public static GridCoord ComputeGridCoordSimple(float x, float y)
        {
            int gx = (int)(MapConst.CenterGridId - x / MapConst.SizeofGrids);
            int gy = (int)(MapConst.CenterGridId - y / MapConst.SizeofGrids);
            return new GridCoord((uint)((MapConst.MaxGrids - 1) - gx), (uint)((MapConst.MaxGrids - 1) - gy));
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
            return X_coord < Limit && Y_coord < Limit;
        }
        public ICoord Normalize()
        {
            X_coord = Math.Min(X_coord, Limit - 1);
            Y_coord = Math.Min(Y_coord, Limit - 1);
            return this;
        }
        public uint GetId()
        {
            return Y_coord * Limit + X_coord;
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
            if (X_coord + val < Limit)
                X_coord += val;
            else
                X_coord = Limit - 1;
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
            if (Y_coord + val < Limit)
                Y_coord += val;
            else
                Y_coord = Limit - 1;
        }

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

        public uint X_coord { get; set; }
        public uint Y_coord { get; set; }
    }

    public class GridCoord : ICoord
    {
        const int Limit = MapConst.MaxGrids;

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
            return X_coord < Limit && Y_coord < Limit;
        }
        public ICoord Normalize()
        {
            X_coord = Math.Min(X_coord, Limit - 1);
            Y_coord = Math.Min(Y_coord, Limit - 1);
            return this;
        }
        public uint GetId()
        {
            return Y_coord * Limit + X_coord;
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
            if (X_coord + val < Limit)
                X_coord += val;
            else
                X_coord = Limit - 1;
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
            if (Y_coord + val < Limit)
                Y_coord += val;
            else
                Y_coord = Limit - 1;
        }

        public static bool operator ==(GridCoord first, GridCoord other)
        {
            if (ReferenceEquals(first, other))
                return true;

            if (ReferenceEquals(first, null) || ReferenceEquals(other, null))
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
            return new { X_coord, Y_coord }.GetHashCode();
        }


        public uint X_coord { get; set; }
        public uint Y_coord { get; set; }
    }

    public interface ICoord
    {
        bool IsCoordValid();
        ICoord Normalize();
        uint GetId();
        void Dec_x(uint val);
        void Inc_x(uint val);
        void Dec_y(uint val);
        void Inc_y(uint val);

        uint X_coord { get; set; }
        uint Y_coord { get; set; }
    }
}
