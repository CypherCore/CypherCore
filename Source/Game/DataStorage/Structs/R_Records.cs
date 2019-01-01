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

namespace Game.DataStorage
{
    public sealed class RandPropPointsRecord
    {
        public uint Id;
        public int DamageReplaceStat;
        public uint[] Epic = new uint[5];
        public uint[] Superior = new uint[5];
        public uint[] Good = new uint[5];
    }

    public sealed class RewardPackRecord
    {
        public uint Id;
        public ushort CharTitleID;
        public uint Money;
        public byte ArtifactXPDifficulty;
        public float ArtifactXPMultiplier;
        public byte ArtifactXPCategoryID;
        public uint TreasurePickerID;
    }

    public sealed class RewardPackXCurrencyTypeRecord
    {
        public uint Id;
        public uint CurrencyTypeID;
        public int Quantity;
        public uint RewardPackID;
    }

    public sealed class RewardPackXItemRecord
    {
        public uint Id;
        public uint ItemID;
        public uint ItemQuantity;
        public uint RewardPackID;
    }

    public sealed class RulesetItemUpgradeRecord
    {
        public uint Id;
        public uint ItemID;
        public ushort ItemUpgradeID;
    }
}
