// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class QuestFactionRewardRecord
    {
        public short[] Difficulty = new short[10];
        public uint Id;
    }

    public sealed class QuestInfoRecord
    {
        public uint Id;
        public LocalizedString InfoName;
        public int Modifiers;
        public int Profession;
        public sbyte Type;
    }

    public sealed class QuestLineXQuestRecord
    {
        public int Flags;
        public uint Id;
        public uint OrderIndex;
        public uint QuestID;
        public uint QuestLineID;
    }

    public sealed class QuestMoneyRewardRecord
    {
        public uint[] Difficulty = new uint[10];
        public uint Id;
    }

    public sealed class QuestPackageItemRecord
    {
        public QuestPackageFilter DisplayType;
        public uint Id;
        public uint ItemID;
        public byte ItemQuantity;
        public ushort PackageID;
    }

    public sealed class QuestSortRecord
    {
        public uint Id;
        public string SortName;
        public byte UiOrderIndex;
    }

    public sealed class QuestV2Record
    {
        public uint Id;
        public int UiQuestDetailsTheme;
        public ushort UniqueBitFlag;
    }

    public sealed class QuestXPRecord
    {
        public ushort[] Difficulty = new ushort[10];
        public uint Id;
    }
}