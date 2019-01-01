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
    public class Cell
    {
        public Cell(ICoord p)
        {
            data.grid_x = p.x_coord / MapConst.MaxCells;
            data.grid_y = p.y_coord / MapConst.MaxCells;
            data.cell_x = p.x_coord % MapConst.MaxCells;
            data.cell_y = p.y_coord % MapConst.MaxCells;
        }

        public Cell(float x, float y)
        {
            ICoord p = GridDefines.ComputeCellCoord(x, y);
            data.grid_x = p.x_coord / MapConst.MaxCells;
            data.grid_y = p.y_coord / MapConst.MaxCells;
            data.cell_x = p.x_coord % MapConst.MaxCells;
            data.cell_y = p.y_coord % MapConst.MaxCells;
        }

        public Cell(Cell cell) { data = cell.data; }

        public bool IsCellValid()
        {
            return data.cell_x < MapConst.MaxCells && data.cell_y < MapConst.MaxCells;
        }

        public uint GetId()
        {
            return data.grid_x * MapConst.MaxGrids + data.grid_y;
        }

        public uint GetCellX() { return data.cell_x; }
        public uint GetCellY() { return data.cell_y; }
        public uint GetGridX() { return data.grid_x; }
        public uint GetGridY() { return data.grid_y; }
        public bool NoCreate() { return data.nocreate; }
        public void SetNoCreate() { data.nocreate = true; }

        public static bool operator ==(Cell left, Cell right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
                return false;

            return left.data.cell_x == right.data.cell_x && left.data.cell_y == right.data.cell_y
                && left.data.grid_x == right.data.grid_x && left.data.grid_y == right.data.grid_y;
        }
        public static bool operator !=(Cell left, Cell right) { return !(left == right); }

        public override bool Equals(object obj)
        {
            return obj is Cell && this == (Cell)obj;
        }

        public override int GetHashCode()
        {
            return (int)(data.cell_x ^ data.cell_y ^ data.grid_x ^ data.grid_y);
        }

        public override string ToString()
        {
            return $"grid[{GetGridX()}, {GetGridY()}]cell[{GetCellX()}, {GetCellY()}]";
        }

        public struct Data
        {
            public uint grid_x;
            public uint grid_y;
            public uint cell_x;
            public uint cell_y;
            public bool nocreate;
        }
        public Data data;

        public CellCoord GetCellCoord()
        {
            return new CellCoord(
                data.grid_x * MapConst.MaxCells + data.cell_x,
                data.grid_y * MapConst.MaxCells + data.cell_y);
        }

        public bool DiffCell(Cell cell)
        {
            return (data.cell_x != cell.data.cell_x ||
                data.cell_y != cell.data.cell_y);
        }

        public bool DiffGrid(Cell cell)
        {
            return (data.grid_x != cell.data.grid_x ||
                data.grid_y != cell.data.grid_y);
        }

        public void Visit(CellCoord standing_cell, Visitor visitor, Map map, WorldObject obj, float radius)
        {
            //we should increase search radius by object's radius, otherwise
            //we could have problems with huge creatures, which won't attack nearest players etc
            Visit(standing_cell, visitor, map, obj.GetPositionX(), obj.GetPositionY(), radius + obj.GetObjectSize());
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
            if ((area.high_bound.x_coord > (area.low_bound.x_coord + 4)) && (area.high_bound.y_coord > (area.low_bound.y_coord + 4)))
            {
                VisitCircle(visitor, map, area.low_bound, area.high_bound);
                return;
            }

            //ALWAYS visit standing cell first!!! Since we deal with small radiuses
            //it is very essential to call visitor for standing cell firstly...
            map.Visit(this, visitor);

            // loop the cell range
            for (uint x = area.low_bound.x_coord; x <= area.high_bound.x_coord; ++x)
            {
                for (uint y = area.low_bound.y_coord; y <= area.high_bound.y_coord; ++y)
                {
                    CellCoord cellCoord = new CellCoord(x, y);
                    //lets skip standing cell since we already visited it
                    if (cellCoord != standing_cell)
                    {
                        Cell r_zone = new Cell(cellCoord);
                        r_zone.data.nocreate = data.nocreate;
                        map.Visit(r_zone, visitor);
                    }
                }
            }
        }

        void VisitCircle(Visitor visitor, Map map, ICoord begin_cell, ICoord end_cell)
        {
            //here is an algorithm for 'filling' circum-squared octagon
            uint x_shift = (uint)Math.Ceiling((end_cell.x_coord - begin_cell.x_coord) * 0.3f - 0.5f);
            //lets calculate x_start/x_end coords for central strip...
            uint x_start = begin_cell.x_coord + x_shift;
            uint x_end = end_cell.x_coord - x_shift;

            //visit central strip with constant width...
            for (uint x = x_start; x <= x_end; ++x)
            {
                for (uint y = begin_cell.y_coord; y <= end_cell.y_coord; ++y)
                {
                    CellCoord cellCoord = new CellCoord(x, y);
                    Cell r_zone = new Cell(cellCoord);
                    r_zone.data.nocreate = data.nocreate;
                    map.Visit(r_zone, visitor);
                }
            }

            //if x_shift == 0 then we have too small cell area, which were already
            //visited at previous step, so just return from procedure...
            if (x_shift == 0)
                return;

            uint y_start = end_cell.y_coord;
            uint y_end = begin_cell.y_coord;
            //now we are visiting borders of an octagon...
            for (uint step = 1; step <= (x_start - begin_cell.x_coord); ++step)
            {
                //each step reduces strip height by 2 cells...
                y_end += 1;
                y_start -= 1;
                for (uint y = y_start; y >= y_end; --y)
                {
                    //we visit cells symmetrically from both sides, heading from center to sides and from up to bottom
                    //e.g. filling 2 trapezoids after filling central cell strip...
                    CellCoord cellCoord_left = new CellCoord(x_start - step, y);
                    Cell r_zone_left = new Cell(cellCoord_left);
                    r_zone_left.data.nocreate = data.nocreate;
                    map.Visit(r_zone_left, visitor);

                    //right trapezoid cell visit
                    CellCoord cellCoord_right = new CellCoord(x_end + step, y);
                    Cell r_zone_right = new Cell(cellCoord_right);
                    r_zone_right.data.nocreate = data.nocreate;
                    map.Visit(r_zone_right, visitor);
                }
            }
        }

        public static void VisitGridObjects(WorldObject center_obj, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(center_obj.GetPositionX(), center_obj.GetPositionY());
            Cell cell = new Cell(p);
            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new Visitor(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, center_obj.GetMap(), center_obj, radius);
        }

        public static void VisitWorldObjects(WorldObject center_obj, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(center_obj.GetPositionX(), center_obj.GetPositionY());
            Cell cell = new Cell(p);
            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new Visitor(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, gnotifier, center_obj.GetMap(), center_obj, radius);
        }

        public static void VisitAllObjects(WorldObject center_obj, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(center_obj.GetPositionX(), center_obj.GetPositionY());
            Cell cell = new Cell(p);
            if (dont_load)
                cell.SetNoCreate();

            Visitor wnotifier = new Visitor(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, wnotifier, center_obj.GetMap(), center_obj, radius);
            Visitor gnotifier = new Visitor(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, center_obj.GetMap(), center_obj, radius);
        }

        public static void VisitGridObjects(float x, float y, Map map, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(x, y);
            Cell cell = new Cell(p);
            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new Visitor(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, map, x, y, radius);
        }

        public static void VisitWorldObjects(float x, float y, Map map, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(x, y);
            Cell cell = new Cell(p);
            if (dont_load)
                cell.SetNoCreate();

            Visitor gnotifier = new Visitor(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, gnotifier, map, x, y, radius);
        }

        public static void VisitAllObjects(float x, float y, Map map, Notifier visitor, float radius, bool dont_load = true)
        {
            CellCoord p = GridDefines.ComputeCellCoord(x, y);
            Cell cell = new Cell(p);
            if (dont_load)
                cell.SetNoCreate();

            Visitor wnotifier = new Visitor(visitor, GridMapTypeMask.AllWorld);
            cell.Visit(p, wnotifier, map, x, y, radius);
            Visitor gnotifier = new Visitor(visitor, GridMapTypeMask.AllGrid);
            cell.Visit(p, gnotifier, map, x, y, radius);
        }

        public static CellArea CalculateCellArea(float x, float y, float radius)
        {
            if (radius <= 0.0f)
            {
                CellCoord center = (CellCoord)GridDefines.ComputeCellCoord(x, y).normalize();
                return new CellArea(center, center);
            }

            CellCoord centerX = (CellCoord)GridDefines.ComputeCellCoord(x - radius, y - radius).normalize();
            CellCoord centerY = (CellCoord)GridDefines.ComputeCellCoord(x + radius, y + radius).normalize();

            return new CellArea(centerX, centerY);
        }
    }

    public class CellArea
    {
        public CellArea() { }
        public CellArea(CellCoord low, CellCoord high)
        {
            low_bound = low;
            high_bound = high;
        }

        void ResizeBorders(ref ICoord begin_cell, ref ICoord end_cell)
        {
            begin_cell = low_bound;
            end_cell = high_bound;
        }

        public bool Check()
        {
           return low_bound == high_bound;
        }

        public ICoord low_bound;
        public ICoord high_bound;
    }
}
