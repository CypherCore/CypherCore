// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class ParagonReputationRecord
    {
        public uint Id;
        public uint FactionID;
        public int LevelThreshold;
        public int QuestID;
    }

    public sealed class PathRecord
    {
        public uint Id;
        public byte Type;
        public byte SplineType;
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte Alpha;
        public int Flags;
    }

    public sealed class PathNodeRecord
    {
        public uint Id;
        public ushort PathID;
        public short Sequence;
        public int LocationID;
    }

    public sealed class PathPropertyRecord
    {
        public uint Id;
        public ushort PathID;
        public byte PropertyIndex;
        public int Value;

        public PathPropertyIndex GetPropertyIndex() { return (PathPropertyIndex)PropertyIndex; }
    }

    public sealed class PerksActivityRecord
    {
        public LocalizedString ActivityName;
        public LocalizedString Description;
        public uint Id;
        public int CriteriaTreeID;
        public int ThresholdContributionAmount;
        public int Supersedes;
        public int Priority;
    }

    public sealed class PhaseRecord
    {
        public uint Id;
        public int Flags;

        public bool HasFlag(PhaseEntryFlags phaseEntryFlags) { return (Flags & (int)phaseEntryFlags) != 0; }
    }

    public sealed class PhaseXPhaseGroupRecord
    {
        public uint Id;
        public ushort PhaseId;
        public uint PhaseGroupID;
    }

    public sealed class PlayerConditionRecord
    {
        public uint Id;
        public long RaceMask;
        public string FailureDescription;
        public ushort MinLevel;
        public ushort MaxLevel;
        public int ClassMask;
        public uint SkillLogic;
        public int LanguageID;
        public byte MinLanguage;
        public int MaxLanguage;
        public ushort MaxFactionID;
        public byte MaxReputation;
        public uint ReputationLogic;
        public sbyte CurrentPvpFaction;
        public byte PvpMedal;
        public uint PrevQuestLogic;
        public uint CurrQuestLogic;
        public uint CurrentCompletedQuestLogic;
        public uint SpellLogic;
        public uint ItemLogic;
        public int ItemFlags;
        public uint AuraSpellLogic;
        public ushort WorldStateExpressionID;
        public int WeatherID;
        public byte PartyStatus;
        public sbyte LifetimeMaxPVPRank;
        public uint AchievementLogic;
        public sbyte Gender;
        public sbyte NativeGender;
        public uint AreaLogic;
        public uint LfgLogic;
        public uint CurrencyLogic;
        public uint QuestKillID;
        public uint QuestKillLogic;
        public sbyte MinExpansionLevel;
        public sbyte MaxExpansionLevel;
        public int MinAvgItemLevel;
        public int MaxAvgItemLevel;
        public ushort MinAvgEquippedItemLevel;
        public ushort MaxAvgEquippedItemLevel;
        public int PhaseUseFlags;
        public ushort PhaseID;
        public uint PhaseGroupID;
        public int Flags;
        public sbyte ChrSpecializationIndex;
        public sbyte ChrSpecializationRole;
        public uint ModifierTreeID;
        public sbyte PowerType;
        public byte PowerTypeComp;
        public byte PowerTypeValue;
        public int WeaponSubclassMask;
        public byte MaxGuildLevel;
        public byte MinGuildLevel;
        public sbyte MaxExpansionTier;
        public sbyte MinExpansionTier;
        public sbyte MinPVPRank;
        public sbyte MaxPVPRank;
        public uint ContentTuningID;
        public int CovenantID;
        public uint TraitNodeEntryLogic;
        public ushort[] SkillID = new ushort[4];
        public ushort[] MinSkill = new ushort[4];
        public ushort[] MaxSkill = new ushort[4];
        public uint[] MinFactionID = new uint[3];
        public byte[] MinReputation = new byte[3];
        public uint[] PrevQuestID = new uint[4];
        public uint[] CurrQuestID = new uint[4];
        public uint[] CurrentCompletedQuestID = new uint[4];
        public uint[] SpellID = new uint[4];
        public uint[] ItemID = new uint[4];
        public uint[] ItemCount = new uint[4];
        public ushort[] Explored = new ushort[2];
        public uint[] Time = new uint[2];
        public uint[] AuraSpellID = new uint[4];
        public byte[] AuraStacks = new byte[4];
        public ushort[] Achievement = new ushort[4];
        public ushort[] AreaID = new ushort[4];
        public byte[] LfgStatus = new byte[4];
        public byte[] LfgCompare = new byte[4];
        public uint[] LfgValue = new uint[4];
        public uint[] CurrencyID = new uint[4];
        public uint[] CurrencyCount = new uint[4];
        public uint[] QuestKillMonster = new uint[6];
        public int[] MovementFlags = new int[2];
        public int[] TraitNodeEntryID = new int[4];
        public ushort[] TraitNodeEntryMinRank = new ushort[4];
        public ushort[] TraitNodeEntryMaxRank = new ushort[4];
    }

    public sealed class PlayerDataElementAccountRecord
    {
        public uint Id;
        public int StorageIndex;
        public int Type;

        public PlayerDataElementType GetElementType() { return (PlayerDataElementType)Type; }
    }

    public sealed class PlayerDataElementCharacterRecord
    {
        public uint Id;
        public int StorageIndex;
        public int Type;

        public PlayerDataElementType GetElementType() { return (PlayerDataElementType)Type; }
    }

    public sealed class PlayerDataFlagAccountRecord
    {
        public uint Id;
        public int StorageIndex;
        public int Unknown1107;
    }

    public sealed class PlayerDataFlagCharacterRecord
    {
        public uint Id;
        public int StorageIndex;
        public int Unknown1107;
    }

    public sealed class PowerDisplayRecord
    {
        public uint Id;
        public string GlobalStringBaseTag;
        public sbyte ActualType;
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    public sealed class PowerTypeRecord
    {
        public string NameGlobalStringTag;
        public string CostGlobalStringTag;
        public uint Id;
        public PowerType PowerTypeEnum;
        public int MinPower;
        public int MaxBasePower;
        public int CenterPower;
        public int DefaultPower;
        public int DisplayModifier;
        public int RegenInterruptTimeMS;
        public float RegenPeace;
        public float RegenCombat;
        public int Flags;

        public bool HasFlag(PowerTypeFlags powerTypeFlags) { return (Flags & (int)powerTypeFlags) != 0; }
    }

    public sealed class PrestigeLevelInfoRecord
    {
        public uint Id;
        public string Name;
        public int PrestigeLevel;
        public int BadgeTextureFileDataID;
        public byte Flags;
        public int AwardedAchievementID;

        public bool HasFlag(PrestigeLevelInfoFlags prestigeLevelInfoFlags) { return (Flags & (byte)prestigeLevelInfoFlags) != 0; }
        public bool IsDisabled() { return HasFlag(PrestigeLevelInfoFlags.Disabled); }
    }

    public sealed class PvpDifficultyRecord
    {
        public uint Id;
        public byte RangeIndex;
        public byte MinLevel;
        public byte MaxLevel;
        public uint MapID;

        // helpers
        public BattlegroundBracketId GetBracketId()
        {
            return (BattlegroundBracketId)RangeIndex;
        }
    }

    public sealed class PvpItemRecord
    {
        public uint Id;
        public uint ItemID;
        public byte ItemLevelDelta;
    }

    public sealed class PvpStatRecord
    {
        public LocalizedString Description;
        public uint Id;
        public uint MapID;
    }

    public sealed class PvpSeasonRecord
    {
        public uint Id;
        public int MilestoneSeason;
        public int AllianceAchievementID;
        public int HordeAchievementID;
    }

    public sealed class PvpTalentRecord
    {
        public string Description;
        public uint Id;
        public uint SpecID;
        public uint SpellID;
        public uint OverridesSpellID;
        public int Flags;
        public int ActionBarSpellID;
        public int PvpTalentCategoryID;
        public int LevelRequired;
        public int PlayerConditionID;
    }

    public sealed class PvpTalentCategoryRecord
    {
        public uint Id;
        public byte TalentSlotMask;
    }

    public sealed class PvpTalentSlotUnlockRecord
    {
        public uint Id;
        public sbyte Slot;
        public uint LevelRequired;
        public uint DeathKnightLevelRequired;
        public uint DemonHunterLevelRequired;
    }

    public sealed class PvpTierRecord
    {
        public LocalizedString Name;
        public uint Id;
        public short MinRating;
        public short MaxRating;
        public int PrevTier;
        public int NextTier;
        public byte BracketID;
        public sbyte Rank;
        public int RankIconFileDataID;
    }
}
