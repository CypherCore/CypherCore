// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
	public sealed class AchievementRecord
	{
		public ushort Category;
		public int CovenantID;
		public uint CriteriaTree;
		public string Description;
		public AchievementFaction Faction;
		public AchievementFlags Flags;
		public uint IconFileID;
		public uint Id;
		public short InstanceID;
		public byte MinimumCriteria;
		public byte Points;
		public string Reward;
		public int RewardItemID;
		public ushort SharesCriteria;
		public ushort Supercedes;
		public string Title;
		public ushort UiOrder;
	}

	public sealed class AchievementCategoryRecord
	{
		public uint Id;
		public LocalizedString Name;
		public short Parent;
		public sbyte UiOrder;
	}

	public sealed class AdventureJournalRecord
	{
		public ushort BattleMasterListID;
		public uint[] BonusPlayerConditionID = new uint[2];
		public byte[] BonusValue = new byte[2];
		public byte ButtonActionType;
		public string ButtonText;
		public string ContinueDescription;
		public uint CurrencyQuantity;
		public ushort CurrencyType;
		public string Description;
		public int Flags;
		public uint Id;
		public int ItemID;
		public uint ItemQuantity;
		public ushort LfgDungeonID;
		public LocalizedString Name;
		public uint PlayerConditionID;
		public byte PriorityMax;
		public byte PriorityMin;
		public uint QuestID;
		public string RewardDescription;
		public int TextureFileDataID;
		public byte Type;
		public ushort UiMapID;
	}

	public sealed class AdventureMapPOIRecord
	{
		public uint AreaTableID;
		public string Description;
		public uint Id;
		public uint LfgDungeonID;
		public int MapID;
		public uint PlayerConditionID;
		public uint QuestID;
		public int RewardItemID;
		public LocalizedString Title;
		public sbyte Type;
		public uint UiTextureAtlasMemberID;
		public uint UiTextureKitID;
		public Vector2 WorldPosition;
	}

	public sealed class AnimationDataRecord
	{
		public int BehaviorID;
		public byte BehaviorTier;
		public ushort Fallback;
		public int[] Flags = new int[2];
		public uint Id;
	}

	public sealed class AnimKitRecord
	{
		public uint Id;
		public ushort LowDefAnimKitID;
		public uint OneShotDuration;
		public ushort OneShotStopAnimKitID;
	}

	public sealed class AreaGroupMemberRecord
	{
		public uint AreaGroupID;
		public ushort AreaID;
		public uint Id;
	}

	public sealed class AreaTableRecord
	{
		public ushort AmbienceID;
		public float AmbientMultiplier;
		public short AreaBit;
		public LocalizedString AreaName;
		public uint ContentTuningID;
		public ushort ContinentID;
		public byte FactionGroupMask;
		public uint[] Flags = new uint[2];
		public uint Id;
		public ushort IntroSound;
		public ushort[] LiquidTypeID = new ushort[4];
		public byte MountFlags;
		public ushort ParentAreaID;
		public short PvpCombatWorldStateID;
		public byte SoundProviderPref;
		public byte SoundProviderPrefUnderwater;
		public ushort UwAmbience;
		public uint UwIntroSound;
		public ushort UwZoneMusic;
		public byte WildBattlePetLevelMax;
		public byte WildBattlePetLevelMin;
		public byte WindSettingsID;
		public ushort ZoneMusic;
		public string ZoneName;

		public bool IsSanctuary()
		{
			return HasFlag(AreaFlags.Sanctuary);
		}

		public bool IsFlyable()
		{
			if (HasFlag(AreaFlags.Outland))
				if (!HasFlag(AreaFlags.NoFlyZone))
					return true;

			return false;
		}

		public bool HasFlag(AreaFlags flag)
		{
			return Flags[0].HasAnyFlag((uint)flag);
		}

		public bool HasFlag2(AreaFlags2 flag)
		{
			return Flags[1].HasAnyFlag((uint)flag);
		}
	}

	public sealed class AreaTriggerRecord
	{
		public short AreaTriggerActionSetID;
		public float BoxHeight;
		public float BoxLength;
		public float BoxWidth;
		public float BoxYaw;
		public ushort ContinentID;
		public sbyte Flags;
		public uint Id;
		public ushort PhaseGroupID;
		public ushort PhaseID;
		public sbyte PhaseUseFlags;
		public Vector3 Pos;
		public float Radius;
		public short ShapeID;
		public sbyte ShapeType;
	}

	public sealed class ArmorLocationRecord
	{
		public float Chainmodifier;
		public float Clothmodifier;
		public uint Id;
		public float Leathermodifier;
		public float Modifier;
		public float Platemodifier;
	}

	public sealed class ArtifactRecord
	{
		public byte ArtifactCategoryID;
		public ushort ChrSpecializationID;
		public byte Flags;
		public uint Id;
		public string Name;
		public uint SpellVisualKitID;
		public int UiBarBackgroundColor;
		public int UiBarOverlayColor;
		public uint UiModelSceneID;
		public int UiNameColor;
		public ushort UiTextureKitID;
	}

	public sealed class ArtifactAppearanceRecord
	{
		public ushort ArtifactAppearanceSetID;
		public byte DisplayIndex;
		public byte Flags;
		public uint Id;
		public byte ItemAppearanceModifierID;
		public string Name;
		public uint OverrideShapeshiftDisplayID;
		public byte OverrideShapeshiftFormID;
		public uint UiAltItemAppearanceID;
		public ushort UiCameraID;
		public uint UiItemAppearanceID;
		public float UiModelOpacity;
		public float UiModelSaturation;
		public int UiSwatchColor;
		public uint UnlockPlayerConditionID;
		public uint UsablePlayerConditionID;
	}

	public sealed class ArtifactAppearanceSetRecord
	{
		public ushort AltHandUICameraID;
		public uint ArtifactID;
		public string Description;
		public byte DisplayIndex;
		public byte Flags;
		public sbyte ForgeAttachmentOverride;
		public uint Id;
		public string Name;
		public ushort UiCameraID;
	}

	public sealed class ArtifactCategoryRecord
	{
		public uint Id;
		public short XpMultCurrencyID;
		public short XpMultCurveID;
	}

	public sealed class ArtifactPowerRecord
	{
		public byte ArtifactID;
		public Vector2 DisplayPos;
		public ArtifactPowerFlag Flags;
		public uint Id;
		public int Label;
		public byte MaxPurchasableRank;
		public byte Tier;
	}

	public sealed class ArtifactPowerLinkRecord
	{
		public uint Id;
		public ushort PowerA;
		public ushort PowerB;
	}

	public sealed class ArtifactPowerPickerRecord
	{
		public uint Id;
		public uint PlayerConditionID;
	}

	public sealed class ArtifactPowerRankRecord
	{
		public uint ArtifactPowerID;
		public float AuraPointsOverride;
		public uint Id;
		public ushort ItemBonusListID;
		public byte RankIndex;
		public uint SpellID;
	}

	public sealed class ArtifactQuestXPRecord
	{
		public uint[] Difficulty = new uint[10];
		public uint Id;
	}

	public sealed class ArtifactTierRecord
	{
		public uint ArtifactTier;
		public uint Id;
		public uint KnowledgePlayerCondition;
		public uint MaxArtifactKnowledge;
		public uint MaxNumTraits;
		public uint MinimumEmpowerKnowledge;
	}

	public sealed class ArtifactUnlockRecord
	{
		public uint ArtifactID;
		public uint Id;
		public ushort ItemBonusListID;
		public uint PlayerConditionID;
		public uint PowerID;
		public byte PowerRank;
	}

	public sealed class AuctionHouseRecord
	{
		public byte ConsignmentRate;
		public byte DepositRate;
		public ushort FactionID; // Id of faction.dbc for player factions associated with city
		public uint Id;
		public string Name;
	}

	public sealed class AzeriteEmpoweredItemRecord
	{
		public uint AzeritePowerSetID;
		public uint AzeriteTierUnlockSetID;
		public uint Id;
		public uint ItemID;
	}

	public sealed class AzeriteEssenceRecord
	{
		public string Description;
		public uint Id;
		public string Name;
		public int SpecSetID;
	}

	public sealed class AzeriteEssencePowerRecord
	{
		public int AzeriteEssenceID;
		public uint Id;
		public uint MajorPowerActual;
		public uint MajorPowerDescription;
		public uint MinorPowerActual;
		public uint MinorPowerDescription;
		public string SourceAlliance;
		public string SourceHorde;
		public byte Tier;
	}

	public sealed class AzeriteItemRecord
	{
		public uint Id;
		public uint ItemID;
	}

	public sealed class AzeriteItemMilestonePowerRecord
	{
		public int AutoUnlock;
		public int AzeritePowerID;
		public uint Id;
		public int RequiredLevel;
		public int Type;
	}

	public sealed class AzeriteKnowledgeMultiplierRecord
	{
		public uint Id;
		public float Multiplier;
	}

	public sealed class AzeriteLevelInfoRecord
	{
		public ulong BaseExperienceToNextLevel;
		public uint Id;
		public uint ItemLevel;
		public ulong MinimumExperienceToNextLevel;
	}

	public sealed class AzeritePowerRecord
	{
		public int Flags;
		public uint Id;
		public uint ItemBonusListID;
		public int SpecSetID;
		public uint SpellID;
	}

	public sealed class AzeritePowerSetMemberRecord
	{
		public int AzeritePowerID;
		public uint AzeritePowerSetID;
		public int Class;
		public uint Id;
		public uint OrderIndex;
		public byte Tier;
	}

	public sealed class AzeriteTierUnlockRecord
	{
		public byte AzeriteLevel;
		public uint AzeriteTierUnlockSetID;
		public uint Id;
		public byte ItemCreationContext;
		public byte Tier;
	}

	public sealed class AzeriteTierUnlockSetRecord
	{
		public AzeriteTierUnlockSetFlags Flags;
		public uint Id;
	}

	public sealed class AzeriteUnlockMappingRecord
	{
		public uint AzeriteUnlockMappingSetID;
		public uint Id;
		public uint ItemBonusListChest;
		public uint ItemBonusListHead;
		public uint ItemBonusListShoulders;
		public int ItemLevel;
	}
}