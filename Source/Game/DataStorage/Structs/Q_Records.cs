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
    public sealed class QuestFactionRewardRecord
    {
        public uint Id;
        public short[] Difficulty = new short[10];
    }

    public sealed class QuestMoneyRewardRecord
    {
        public uint Id;
        public uint[] Difficulty = new uint[10];
    }

    public sealed class QuestPackageItemRecord
    {
        public uint Id;
        public ushort PackageID;
        public uint ItemID;
        public byte ItemQuantity;
        public QuestPackageFilter DisplayType;
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
        public ushort UniqueBitFlag;
    }

    public sealed class QuestXPRecord
    {
        public uint Id;
        public ushort[] Difficulty = new ushort[10];
    }
}
