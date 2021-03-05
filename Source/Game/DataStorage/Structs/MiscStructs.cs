﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Game.DataStorage
{
    public struct WMOAreaTableTripple
    {
        public WMOAreaTableTripple(int r, int a, int g)
        {
            groupId = g;
            rootId = r;
            adtId = a;
        }

        // ordered by entropy; that way memcmp will have a minimal medium runtime
        private int groupId;
        private int rootId;
        private int adtId;
    }

    public class TaxiPathBySourceAndDestination
    {
        public TaxiPathBySourceAndDestination(uint _id, uint _price)
        {
            Id = _id;
            price = _price;
        }

        public uint Id;
        public uint price;
    }
}
