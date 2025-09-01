// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class BankTabRecord
    {
        public uint Id;
        public ulong Cost;
        public byte BankType;
        public sbyte OrderIndex;
        public int PlayerConditionID;
        public int PurchasePromptTitle;
        public int PurchasePromptBody;
        public int PurchasePromptConfirmation;
        public int TabCleanupConfirmation;
        public int TabNameEditBoxHeader;
    }

    public sealed class BannedAddonsRecord
    {
        public uint Id;
        public string Name;
        public string Version;
        public int Flags;
    }

    public sealed class BarberShopStyleRecord
    {
        public uint Id;
        public string DisplayName;
        public string Description;
        public byte Type;                                                     // value 0 . hair, value 2 . facialhair
        public float CostModifier;
        public sbyte Race;
        public sbyte Sex;
        public byte Data;                                                     // real ID to hair/facial hair
    }

    public sealed class BattlePetAbilityRecord
    {
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Description;
        public int IconFileDataID;
        public sbyte PetTypeEnum;
        public uint Cooldown;
        public ushort BattlePetVisualID;
        public byte Flags;
    }

    public sealed class BattlePetBreedQualityRecord
    {
        public uint Id;
        public int MaxQualityRoll;
        public float StateMultiplier;
        public byte QualityEnum;
    }

    public sealed class BattlePetBreedStateRecord
    {
        public uint Id;
        public int BattlePetStateID;
        public ushort Value;
        public uint BattlePetBreedID;
    }

    public sealed class BattlePetSpeciesRecord
    {
        public string Description;
        public string SourceText;
        public uint Id;
        public uint CreatureID;
        public uint SummonSpellID;
        public int IconFileDataID;
        public sbyte PetTypeEnum;
        public int Flags;
        public sbyte SourceTypeEnum;
        public int CardUIModelSceneID;
        public int LoadoutUIModelSceneID;
        public int CovenantID;

        public bool HasFlag(BattlePetSpeciesFlags battlePetSpeciesFlags) { return (Flags & (int)battlePetSpeciesFlags) != 0; }
    }

    public sealed class BattlePetSpeciesStateRecord
    {
        public uint Id;
        public ushort BattlePetStateID;
        public int Value;
        public uint BattlePetSpeciesID;
    }

    public sealed class BattlemasterListRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string GameType;
        public string ShortDescription;
        public string LongDescription;
        public int PvpType;
        public byte MinLevel;
        public byte MaxLevel;
        public sbyte RatedPlayers;
        public sbyte MinPlayers;
        public int MaxPlayers;
        public sbyte GroupsAllowed;
        public sbyte MaxGroupSize;
        public ushort HolidayWorldState;
        public int Flags;
        public int IconFileDataID;
        public int RequiredPlayerConditionID;

        public BattlemasterType GetPvpType() { return (BattlemasterType)PvpType; }
        public bool HasFlag(BattlemasterListFlags battlemasterListFlags) { return (Flags & (int)battlemasterListFlags) != 0; }
    }

    public sealed class BattlemasterListXMapRecord
    {
        public uint ID;
        public int MapID;
        public uint BattlemasterListID;
    }

    public sealed class BroadcastTextRecord
    {
        public LocalizedString Text;
        public LocalizedString Text1;
        public uint Id;
        public int LanguageID;
        public int ConditionID;
        public ushort EmotesID;
        public ushort Flags;
        public uint ChatBubbleDurationMs;
        public int VoiceOverPriorityID;
        public uint[] SoundKitID = new uint[2];
        public ushort[] EmoteID = new ushort[3];
        public ushort[] EmoteDelay = new ushort[3];
    }

    public sealed class BroadcastTextDurationRecord
    {
        public uint Id;
        public int Locale;
        public int Duration;
        public uint BroadcastTextID;
    }
}
