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

namespace Game.DataStorage
{
    public sealed class HeirloomRecord
    {
        public string SourceText;
        public uint Id;
        public uint ItemID;
        public int LegacyUpgradedItemID;
        public uint StaticUpgradedItemID;
        public sbyte SourceTypeEnum;
        public byte Flags;
        public int LegacyItemID;
        public int[] UpgradeItemID = new int[3];
        public ushort[] UpgradeItemBonusListID = new ushort[3];
    }

    public sealed class HolidaysRecord
    {
        public uint Id;
        public ushort Region;
        public byte Looping;
        public uint HolidayNameID;
        public uint HolidayDescriptionID;
        public byte Priority;
        public sbyte CalendarFilterType;
        public byte Flags;
        public ushort[] Duration = new ushort[SharedConst.MaxHolidayDurations];
        public uint[] Date = new uint[SharedConst.MaxHolidayDates];                                 // dates in unix time starting at January, 1, 2000
        public byte[] CalendarFlags = new byte[SharedConst.MaxHolidayFlags];
        public int[] TextureFileDataID = new int[3];
    }
}
