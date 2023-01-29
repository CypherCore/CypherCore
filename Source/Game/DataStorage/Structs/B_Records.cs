// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class BankBagSlotPricesRecord
    {
        public uint Cost;
        public uint Id;
    }

    public sealed class BannedAddonsRecord
    {
        public byte Flags;
        public uint Id;
        public string Name;
        public string Version;
    }

    public sealed class BarberShopStyleRecord
    {
        public float CostModifier;
        public byte Data; // real ID to hair/facial hair
        public string Description;
        public string DisplayName;
        public uint Id;
        public byte Race;
        public byte Sex;
        public byte Type; // value 0 . hair, value 2 . facialhair
    }

    public sealed class BattlePetBreedQualityRecord
    {
        public uint Id;
        public int MaxQualityRoll;
        public sbyte QualityEnum;
        public float StateMultiplier;
    }

    public sealed class BattlePetBreedStateRecord
    {
        public uint BattlePetBreedID;
        public int BattlePetStateID;
        public uint Id;
        public ushort Value;
    }

    public sealed class BattlePetSpeciesRecord
    {
        public int CardUIModelSceneID;
        public int CovenantID;
        public uint CreatureID;
        public string Description;
        public int Flags;
        public int IconFileDataID;
        public uint Id;
        public int LoadoutUIModelSceneID;
        public sbyte PetTypeEnum;
        public string SourceText;
        public sbyte SourceTypeEnum;
        public uint SummonSpellID;

        public BattlePetSpeciesFlags GetFlags()
        {
            return (BattlePetSpeciesFlags)Flags;
        }
    }

    public sealed class BattlePetSpeciesStateRecord
    {
        public uint BattlePetSpeciesID;
        public ushort BattlePetStateID;
        public uint Id;
        public int Value;
    }

    public sealed class BattlemasterListRecord
    {
        public BattlemasterListFlags Flags;
        public string GameType;
        public sbyte GroupsAllowed;
        public ushort HolidayWorldState;
        public int IconFileDataID;
        public uint Id;
        public sbyte InstanceType;
        public string LongDescription;
        public short[] MapId = new short[16];
        public sbyte MaxGroupSize;
        public byte MaxLevel;
        public int MaxPlayers;
        public byte MinLevel;
        public sbyte MinPlayers;
        public LocalizedString Name;
        public sbyte RatedPlayers;
        public int RequiredPlayerConditionID;
        public string ShortDescription;
    }

    public sealed class BroadcastTextRecord
    {
        public uint ChatBubbleDurationMs;
        public int ConditionID;
        public ushort[] EmoteDelay = new ushort[3];
        public ushort[] EmoteID = new ushort[3];
        public ushort EmotesID;
        public byte Flags;
        public uint Id;
        public int LanguageID;
        public uint[] SoundKitID = new uint[2];
        public LocalizedString Text;
        public LocalizedString Text1;
        public int VoiceOverPriorityID;
    }

    public sealed class BroadcastTextDurationRecord
    {
        public int BroadcastTextID;
        public int Duration;
        public uint Id;
        public int Locale;
    }
}