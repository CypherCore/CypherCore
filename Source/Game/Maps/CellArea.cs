// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Maps
{
    public class CellArea
    {
        public ICoord High_bound;

        public ICoord Low_bound;

        public CellArea()
        {
        }

        public CellArea(CellCoord low, CellCoord high)
        {
            Low_bound = low;
            High_bound = high;
        }

        public bool Check()
        {
            return Low_bound == High_bound;
        }

        private void ResizeBorders(ref ICoord begin_cell, ref ICoord end_cell)
        {
            begin_cell = Low_bound;
            end_cell = High_bound;
        }
    }
}