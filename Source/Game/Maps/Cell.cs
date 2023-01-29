// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps
{
    public class Cell
    {
        public struct CellData
        {
            public uint Grid_x;
            public uint Grid_y;
            public uint Cell_x;
            public uint Cell_y;
            public bool Nocreate;
        }

        public CellData Data;

        public Cell(ICoord p)
        {
            Data.Grid_x = p.X_coord / MapConst.MaxCells;
            Data.Grid_y = p.Y_coord / MapConst.MaxCells;
            Data.Cell_x = p.X_coord % MapConst.MaxCells;
            Data.Cell_y = p.Y_coord % MapConst.MaxCells;
        }

        public Cell(float x, float y)
        {
            ICoord p = GridDefines.ComputeCellCoord(x, y);
            Data.Grid_x = p.X_coord / MapConst.MaxCells;
            Data.Grid_y = p.Y_coord / MapConst.MaxCells;
            Data.Cell_x = p.X_coord % MapConst.MaxCells;
            Data.Cell_y = p.Y_coord % MapConst.MaxCells;
        }

        public Cell(Cell cell)
        {
            Data = cell.Data;
        }

        public bool IsCellValid()
        {
            return Data.Cell_x < MapConst.MaxCells && Data.Cell_y < MapConst.MaxCells;
        }

        public uint GetId()
        {
            return Data.Grid_x * MapConst.MaxGrids + Data.Grid_y;
        }

        public uint GetCellX()
        {
            return Data.Cell_x;
        }

        public uint GetCellY()
        {
            return Data.Cell_y;
        }

        public uint GetGridX()
        {
            return Data.Grid_x;
        }

        public uint GetGridY()
        {
            return Data.Grid_y;
        }

        public bool NoCreate()
        {
            return Data.Nocreate;
        }

        public void SetNoCreate()
        {
            Data.Nocreate = true;
        }

        public static bool operator ==(Cell left, Cell right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left is null ||
                right is null)
                return false;

            return left.Data.Cell_x == right.Data.Cell_x && left.Data.Cell_y == right.Data.Cell_y && left.Data.Grid_x == right.Data.Grid_x && left.Data.Grid_y == right.Data.Grid_y;
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Cell && this == (Cell)obj;
        }

        public override int GetHashCode()
        {
            return (int)(Data.Cell_x ^ Data.Cell_y ^ Data.Grid_x ^ Data.Grid_y);
        }

        public override string ToString()
        {
            return $"grid[{GetGridX()}, {GetGridY()}]cell[{GetCellX()}, {GetCellY()}]";
        }

        public CellCoord GetCellCoord()
        {
            return new CellCoord(Data.Grid_x * MapConst.MaxCells + Data.Cell_x,
                                 Data.Grid_y * MapConst.MaxCells + Data.Cell_y);
        }

        public bool DiffCell(Cell cell)
        {
            return (Data.Cell_x != cell.Data.Cell_x ||
                    Data.Cell_y != cell.Data.Cell_y);
        }

        public bool DiffGrid(Cell cell)
        {
            return (Data.Grid_x != cell.Data.Grid_x ||
                    Data.Grid_y != cell.Data.Grid_y);
        }

        public void Visit(CellCoord standing_cell, Visitor visitor, Map map, WorldObject obj, float radius)
        {
            //we should increase search radius by object's radius, otherwise
            //we could have problems with huge creatures, which won't attack nearest players etc
            Visit(standing_cell, visitor, map, obj.GetPositionX(), obj.GetPositionY(), radius + obj.GetCombatReach());
        }

        public void Visit(CellCoord standing_cell, Visitor visitor, Map map, float x_off, float y_off, float radius)
        {
            if (!standing_cell.IsCoordValid())
                return;

            //no jokes here... Actually placing ASSERT() here was good idea, but
            //we had some problems with DynamicObjects, which pass radius = 0.0f (DB issue?)
            //maybe it is better to just return when radius <= 0.0f?
            if (radius <= 0.0f)
            {
                map.Visit(this, visitor);

                return;
            }

            //lets limit the upper value for search radius
            if (radius > MapConst.SizeofGrids)
                radius = MapConst.SizeofGrids;

            //lets calculate object coord offsets from cell borders.
            CellArea area = CalculateCellArea(x_off, y_off, radius);

            //if radius fits inside standing cell
            if (area == null)
            {
                map.Visit(this, visitor);

                return;
            }

            //visit all cells, found in CalculateCellArea()
            //if radius is known to reach cell area more than 4x4 then we should call optimized VisitCircle
            //currently this technique works with MAX_NUMBER_OF_CELLS 16 and higher, with lower values
            //there are nothing to optimize because SIZE_OF_GRID_CELL is too big...
            if ((area.High_bound.X_coord > (area.Low_bound.X_coord + 4)) &&
                (area.High_bound.Y_coord > (area.Low_bound.Y_coord + 4)))
            {
                VisitCircle(visitor, map, area.Low_bound, area.High_bound);

                return;
            }

            //ALWAYS visit standing cell first!!! Since we deal with small radiuses
            //it is very essential to call visitor for standing cell firstly...
            map.Visit(this, visitor);

            // loop the cell range
            for (uint x = area.Low_bound.X_coord; x <= area.High_bound.X_coord; ++x)
            {
                for (uint y = area.Low_bound.Y_coord; y <= area.High_bound.Y_coord; ++y)
                {
                    CellCoord cellCoord = new(x, y);

                    //lets skip standing cell since we already visited it
                    if (cellCoord != standing_cell)
                    {
                        Cell r_zone = new(cellCoord);
                        r_zone.Data.Nocreate = Data.Nocreate;
                        map.Visit(r_zone, visitor);
                    }
                }
            }
        }

        public static void VisitGridObjects(WorldObject center_obj, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(center_obj.GetPositionX(), center_obj.GetPositionY());
            Cell cell = new(p);

            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, center_obj.GetMap(), center_obj, radius);
        }

        public static void VisitWorldObjects(WorldObject center_obj, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(center_obj.GetPositionX(), center_obj.GetPositionY());
            Cell cell = new(p);

            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, gnotifier, center_obj.GetMap(), center_obj, radius);
        }

        public static void VisitAllObjects(WorldObject center_obj, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(center_obj.GetPositionX(), center_obj.GetPositionY());
            Cell cell = new(p);

            if (dont_load)
                cell.SetNoCreate();

            Visitor wnotifier = new(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, wnotifier, center_obj.GetMap(), center_obj, radius);
            Visitor gnotifier = new(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, center_obj.GetMap(), center_obj, radius);
        }

        public static void VisitGridObjects(float x, float y, Map map, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(x, y);
            Cell cell = new(p);

            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, map, x, y, radius);
        }

        public static void VisitWorldObjects(float x, float y, Map map, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(x, y);
            Cell cell = new(p);

            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, gnotifier, map, x, y, radius);
        }

        public static void VisitAllObjects(float x, float y, Map map, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(x, y);
            Cell cell = new(p);

            if (dont_load)
                cell.SetNoCreate();

            Visitor wnotifier = new(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, wnotifier, map, x, y, radius);
            Visitor gnotifier = new(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, map, x, y, radius);
        }

        public static CellArea CalculateCellArea(float x, float y, float radius)
        {
            if (radius <= 0.0f)
            {
                CellCoord center = (CellCoord)GridDefines.ComputeCellCoord(x, y).Normalize();

                return new CellArea(center, center);
            }

            CellCoord centerX = (CellCoord)GridDefines.ComputeCellCoord(x - radius, y - radius).Normalize();
            CellCoord centerY = (CellCoord)GridDefines.ComputeCellCoord(x + radius, y + radius).Normalize();

            return new CellArea(centerX, centerY);
        }

        private void VisitCircle(Visitor visitor, Map map, ICoord begin_cell, ICoord end_cell)
        {
            //here is an algorithm for 'filling' circum-squared octagon
            uint x_shift = (uint)Math.Ceiling((end_cell.X_coord - begin_cell.X_coord) * 0.3f - 0.5f);
            //lets calculate x_start/x_end coords for central strip...
            uint x_start = begin_cell.X_coord + x_shift;
            uint x_end = end_cell.X_coord - x_shift;

            //visit central strip with constant width...
            for (uint x = x_start; x <= x_end; ++x)
            {
                for (uint y = begin_cell.Y_coord; y <= end_cell.Y_coord; ++y)
                {
                    CellCoord cellCoord = new(x, y);
                    Cell r_zone = new(cellCoord);
                    r_zone.Data.Nocreate = Data.Nocreate;
                    map.Visit(r_zone, visitor);
                }
            }

            //if x_shift == 0 then we have too small cell area, which were already
            //visited at previous step, so just return from procedure...
            if (x_shift == 0)
                return;

            uint y_start = end_cell.Y_coord;
            uint y_end = begin_cell.Y_coord;

            //now we are visiting borders of an octagon...
            for (uint step = 1; step <= (x_start - begin_cell.X_coord); ++step)
            {
                //each step reduces strip height by 2 cells...
                y_end += 1;
                y_start -= 1;

                for (uint y = y_start; y >= y_end; --y)
                {
                    //we visit cells symmetrically from both sides, heading from center to sides and from up to bottom
                    //e.g. filling 2 trapezoids after filling central cell strip...
                    CellCoord cellCoord_left = new(x_start - step, y);
                    Cell r_zone_left = new(cellCoord_left);
                    r_zone_left.Data.Nocreate = Data.Nocreate;
                    map.Visit(r_zone_left, visitor);

                    //right trapezoid cell visit
                    CellCoord cellCoord_right = new(x_end + step, y);
                    Cell r_zone_right = new(cellCoord_right);
                    r_zone_right.Data.Nocreate = Data.Nocreate;
                    map.Visit(r_zone_right, visitor);
                }
            }
        }
    }
}