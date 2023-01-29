// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class HeirloomRecord
    {
        public byte Flags;
        public uint Id;
        public uint ItemID;
        public int LegacyItemID;
        public int LegacyUpgradedItemID;
        public string SourceText;
        public sbyte SourceTypeEnum;
        public uint StaticUpgradedItemID;
        public ushort[] UpgradeItemBonusListID = new ushort[6];
        public int[] UpgradeItemID = new int[6];
    }

    public sealed class HolidaysRecord
    {
        public sbyte CalendarFilterType;
        public byte[] CalendarFlags = new byte[SharedConst.MaxHolidayFlags];
        public uint[] Date = new uint[SharedConst.MaxHolidayDates]; // dates in unix Time starting at January, 1, 2000
        public ushort[] Duration = new ushort[SharedConst.MaxHolidayDurations];
        public byte Flags;
        public uint HolidayDescriptionID;
        public uint HolidayNameID;
        public uint Id;
        public byte Looping;
        public byte Priority;
        public ushort Region;
        public int[] TextureFileDataID = new int[3];
    }
}