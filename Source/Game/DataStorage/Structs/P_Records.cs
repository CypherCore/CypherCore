// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
	public sealed class ParagonReputationRecord
	{
		public uint FactionID;
		public uint Id;
		public int LevelThreshold;
		public int QuestID;
	}

	public sealed class PhaseRecord
	{
		public PhaseEntryFlags Flags;
		public uint Id;
	}

	public sealed class PhaseXPhaseGroupRecord
	{
		public uint Id;
		public uint PhaseGroupID;
		public ushort PhaseId;
	}

	public sealed class PlayerConditionRecord
	{
		public ushort[] Achievement = new ushort[4];
		public uint AchievementLogic;
		public ushort[] AreaID = new ushort[4];
		public uint AreaLogic;
		public uint[] AuraSpellID = new uint[4];
		public uint AuraSpellLogic;
		public byte[] AuraStacks = new byte[4];
		public sbyte ChrSpecializationIndex;
		public sbyte ChrSpecializationRole;
		public int ClassMask;
		public uint ContentTuningID;
		public int CovenantID;
		public uint[] CurrencyCount = new uint[4];
		public uint[] CurrencyID = new uint[4];
		public uint CurrencyLogic;
		public uint[] CurrentCompletedQuestID = new uint[4];
		public uint CurrentCompletedQuestLogic;
		public sbyte CurrentPvpFaction;
		public uint[] CurrQuestID = new uint[4];
		public uint CurrQuestLogic;
		public ushort[] Explored = new ushort[2];
		public string FailureDescription;
		public int Flags;
		public sbyte Gender;
		public uint Id;
		public uint[] ItemCount = new uint[4];
		public byte ItemFlags;
		public uint[] ItemID = new uint[4];
		public uint ItemLogic;
		public int LanguageID;
		public byte[] LfgCompare = new byte[4];
		public uint LfgLogic;
		public byte[] LfgStatus = new byte[4];
		public uint[] LfgValue = new uint[4];
		public byte LifetimeMaxPVPRank;
		public ushort MaxAvgEquippedItemLevel;
		public int MaxAvgItemLevel;
		public sbyte MaxExpansionLevel;
		public sbyte MaxExpansionTier;
		public ushort MaxFactionID;
		public byte MaxGuildLevel;
		public int MaxLanguage;
		public byte MaxPVPRank;
		public byte MaxReputation;
		public ushort[] MaxSkill = new ushort[4];
		public ushort MinAvgEquippedItemLevel;
		public int MinAvgItemLevel;
		public sbyte MinExpansionLevel;
		public sbyte MinExpansionTier;
		public uint[] MinFactionID = new uint[3];
		public byte MinGuildLevel;
		public byte MinLanguage;
		public byte MinPVPRank;
		public byte[] MinReputation = new byte[3];
		public ushort[] MinSkill = new ushort[4];
		public uint ModifierTreeID;
		public int[] MovementFlags = new int[2];
		public sbyte NativeGender;
		public byte PartyStatus;
		public uint PhaseGroupID;
		public ushort PhaseID;
		public byte PhaseUseFlags;
		public sbyte PowerType;
		public byte PowerTypeComp;
		public byte PowerTypeValue;
		public uint[] PrevQuestID = new uint[4];
		public uint PrevQuestLogic;
		public byte PvpMedal;
		public uint QuestKillID;
		public uint QuestKillLogic;
		public uint[] QuestKillMonster = new uint[6];
		public long RaceMask;
		public uint ReputationLogic;
		public ushort[] SkillID = new ushort[4];
		public uint SkillLogic;
		public uint[] SpellID = new uint[4];
		public uint SpellLogic;
		public uint[] Time = new uint[2];
		public int[] TraitNodeEntryID = new int[4];
		public uint TraitNodeEntryLogic;
		public ushort[] TraitNodeEntryMaxRank = new ushort[4];
		public ushort[] TraitNodeEntryMinRank = new ushort[4];
		public int WeaponSubclassMask;
		public int WeatherID;
		public ushort WorldStateExpressionID;
	}

	public sealed class PowerDisplayRecord
	{
		public byte ActualType;
		public byte Blue;
		public string GlobalStringBaseTag;
		public byte Green;
		public uint Id;
		public byte Red;
	}

	public sealed class PowerTypeRecord
	{
		public int CenterPower;
		public string CostGlobalStringTag;
		public int DefaultPower;
		public int DisplayModifier;
		public short Flags;
		public uint Id;
		public int MaxBasePower;
		public int MinPower;
		public string NameGlobalStringTag;
		public PowerType PowerTypeEnum;
		public float RegenCombat;
		public int RegenInterruptTimeMS;
		public float RegenPeace;
	}

	public sealed class PrestigeLevelInfoRecord
	{
		public int AwardedAchievementID;
		public int BadgeTextureFileDataID;
		public PrestigeLevelInfoFlags Flags;
		public uint Id;
		public string Name;
		public int PrestigeLevel;

		public bool IsDisabled()
		{
			return (Flags & PrestigeLevelInfoFlags.Disabled) != 0;
		}
	}

	public sealed class PvpDifficultyRecord
	{
		public uint Id;
		public uint MapID;
		public byte MaxLevel;
		public byte MinLevel;
		public byte RangeIndex;

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

	public sealed class PvpTalentRecord
	{
		public int ActionBarSpellID;
		public string Description;
		public int Flags;
		public uint Id;
		public int LevelRequired;
		public uint OverridesSpellID;
		public int PlayerConditionID;
		public int PvpTalentCategoryID;
		public int SpecID;
		public uint SpellID;
	}

	public sealed class PvpTalentCategoryRecord
	{
		public uint Id;
		public byte TalentSlotMask;
	}

	public sealed class PvpTalentSlotUnlockRecord
	{
		public uint DeathKnightLevelRequired;
		public uint DemonHunterLevelRequired;
		public uint Id;
		public uint LevelRequired;
		public sbyte Slot;
	}

	public sealed class PvpTierRecord
	{
		public sbyte BracketID;
		public uint Id;
		public short MaxRating;
		public short MinRating;
		public LocalizedString Name;
		public int NextTier;
		public int PrevTier;
		public sbyte Rank;
		public int RankIconFileDataID;
	}
}