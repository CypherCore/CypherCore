// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.DataStorage
{
    public sealed class RandPropPointsRecord
    {
        public int DamageReplaceStat;
        public float DamageReplaceStatF;
        public int DamageSecondary;
        public float DamageSecondaryF;
        public uint[] Epic = new uint[5];
        public float[] EpicF = new float[5];
        public uint[] Good = new uint[5];
        public float[] GoodF = new float[5];
        public uint Id;
        public uint[] Superior = new uint[5];
        public float[] SuperiorF = new float[5];
    }

    public sealed class RewardPackRecord
    {
        public byte ArtifactXPCategoryID;
        public byte ArtifactXPDifficulty;
        public float ArtifactXPMultiplier;
        public ushort CharTitleID;
        public uint Id;
        public uint Money;
        public uint TreasurePickerID;
    }

    public sealed class RewardPackXCurrencyTypeRecord
    {
        public uint CurrencyTypeID;
        public uint Id;
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
}