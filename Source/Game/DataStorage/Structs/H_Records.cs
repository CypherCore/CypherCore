// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        public int Flags;
        public int LegacyItemID;
        public int[] UpgradeItemID = new int[6];
        public ushort[] UpgradeItemBonusListID = new ushort[6];
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
