// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;

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

        public static float NormalizeMapCoord(float c)
        {
            if (c > MapConst.MapHalfSize - 0.5f)
                return MapConst.MapHalfSize - 0.5f;

            if (c < -(MapConst.MapHalfSize - 0.5f))
                return -(MapConst.MapHalfSize - 0.5f);

            return c;
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
}