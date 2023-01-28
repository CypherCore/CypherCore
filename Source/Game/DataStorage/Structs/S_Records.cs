// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Framework.Dynamic;

namespace Game.DataStorage
{
	public sealed class ScenarioRecord
	{
		public ushort AreaTableID;
		public byte Flags;
		public uint Id;
		public string Name;
		public byte Type;
		public uint UiTextureKitID;
	}

	public sealed class ScenarioStepRecord
	{
		public uint CriteriaTreeId;
		public string Description;
		public ScenarioStepFlags Flags;
		public uint Id;
		public byte OrderIndex;
		public int RelatedStep; // Bonus step can only be completed if scenario is in the step specified in this field
		public uint RewardQuestID;
		public ushort ScenarioID;
		public ushort Supersedes; // Used in conjunction with Proving Grounds scenarios, when sequencing steps (Not using step order?)
		public string Title;
		public uint VisibilityPlayerConditionID;
		public ushort WidgetSetID;

		// helpers
		public bool IsBonusObjective()
		{
			return Flags.HasAnyFlag(ScenarioStepFlags.BonusObjective);
		}
	}

	public sealed class SceneScriptRecord
	{
		public ushort FirstSceneScriptID;
		public uint Id;
		public ushort NextSceneScriptID;
		public int Unknown915;
	}

	public sealed class SceneScriptGlobalTextRecord
	{
		public uint Id;
		public string Name;
		public string Script;
	}

	public sealed class SceneScriptPackageRecord
	{
		public uint Id;
		public string Name;
		public int Unknown915;
	}

	public sealed class SceneScriptTextRecord
	{
		public uint Id;
		public string Name;
		public string Script;
	}

	public sealed class SkillLineRecord
	{
		public string AlternateVerb;
		public sbyte CanLink;
		public SkillCategory CategoryID;
		public string Description;
		public LocalizedString DisplayName;
		public int ExpansionNameSharedStringID;
		public ushort Flags;
		public string HordeDisplayName;
		public int HordeExpansionNameSharedStringID;
		public uint Id;
		public string OverrideSourceInfoDisplayName;
		public uint ParentSkillLineID;
		public int ParentTierIndex;
		public int SpellBookSpellID;
		public int SpellIconFileID;

		public SkillLineFlags GetFlags()
		{
			return (SkillLineFlags)Flags;
		}
	}

	public sealed class SkillLineAbilityRecord
	{
		public string AbilityAllVerb;
		public string AbilityVerb;
		public AbilityLearnType AcquireMethod;
		public int ClassMask;
		public SkillLineAbilityFlags Flags;
		public uint Id;
		public short MinSkillLineRank;
		public byte NumSkillUps;
		public long RaceMask;
		public ushort SkillLine;
		public ushort SkillupSkillLineID;
		public uint Spell;
		public uint SupercedesSpell;
		public short TradeSkillCategoryID;
		public ushort TrivialSkillLineRankHigh;
		public ushort TrivialSkillLineRankLow;
		public short UniqueBit;
	}

	public sealed class SkillLineXTraitTreeRecord
	{
		public uint Id;
		public int OrderIndex;
		public int SkillLineID;
		public int TraitTreeID;
	}

	public sealed class SkillRaceClassInfoRecord
	{
		public sbyte Availability;
		public int ClassMask;
		public SkillRaceClassInfoFlags Flags;
		public uint Id;
		public sbyte MinLevel;
		public long RaceMask;
		public ushort SkillID;
		public ushort SkillTierID;
	}

	public sealed class SoulbindConduitRankRecord
	{
		public float AuraPointsOverride;
		public uint Id;
		public int RankIndex;
		public uint SoulbindConduitID;
		public int SpellID;
	}

	public sealed class SoundKitRecord
	{
		public ushort BusOverwriteID;
		public sbyte DialogType;
		public float DistanceCutoff;
		public byte EAXDef;
		public int Flags;
		public uint Id;
		public byte MaxInstances;
		public float MinDistance;
		public float PitchAdjust;
		public float PitchVariationMinus;
		public float PitchVariationPlus;
		public uint SoundKitAdvancedID;
		public uint SoundMixGroupID;
		public uint SoundType;
		public float VolumeFloat;
		public float VolumeVariationMinus;
		public float VolumeVariationPlus;
	}

	public sealed class SpecializationSpellsRecord
	{
		public string Description;
		public byte DisplayOrder;
		public uint Id;
		public uint OverridesSpellID;
		public ushort SpecID;
		public uint SpellID;
	}

	public sealed class SpecSetMemberRecord
	{
		public uint ChrSpecializationID;
		public uint Id;
		public uint SpecSetID;
	}

	public sealed class SpellAuraOptionsRecord
	{
		public ushort CumulativeAura;
		public byte DifficultyID;
		public uint Id;
		public uint ProcCategoryRecovery;
		public byte ProcChance;
		public int ProcCharges;
		public int[] ProcTypeMask = new int[2];
		public uint SpellID;
		public ushort SpellProcsPerMinuteID;
	}

	public sealed class SpellAuraRestrictionsRecord
	{
		public uint CasterAuraSpell;
		public int CasterAuraState;
		public int CasterAuraType;
		public uint DifficultyID;
		public uint ExcludeCasterAuraSpell;
		public int ExcludeCasterAuraState;
		public int ExcludeCasterAuraType;
		public uint ExcludeTargetAuraSpell;
		public int ExcludeTargetAuraState;
		public int ExcludeTargetAuraType;
		public uint Id;
		public uint SpellID;
		public uint TargetAuraSpell;
		public int TargetAuraState;
		public int TargetAuraType;
	}

	public sealed class SpellCastTimesRecord
	{
		public int Base;
		public uint Id;
		public int Minimum;
	}

	public sealed class SpellCastingRequirementsRecord
	{
		public byte FacingCasterFlags;
		public uint Id;
		public ushort MinFactionID;
		public int MinReputation;
		public ushort RequiredAreasID;
		public byte RequiredAuraVision;
		public ushort RequiresSpellFocus;
		public uint SpellID;
	}

	public sealed class SpellCategoriesRecord
	{
		public ushort Category;
		public ushort ChargeCategory;
		public sbyte DefenseType;
		public byte DifficultyID;
		public sbyte DispelType;
		public uint Id;
		public sbyte Mechanic;
		public sbyte PreventionType;
		public uint SpellID;
		public ushort StartRecoveryCategory;
	}

	public sealed class SpellCategoryRecord
	{
		public int ChargeRecoveryTime;
		public SpellCategoryFlags Flags;
		public uint Id;
		public byte MaxCharges;
		public string Name;
		public int TypeMask;
		public byte UsesPerWeek;
	}

	public sealed class SpellClassOptionsRecord
	{
		public uint Id;
		public uint ModalNextSpell;
		public FlagArray128 SpellClassMask;
		public byte SpellClassSet;
		public uint SpellID;
	}

	public sealed class SpellCooldownsRecord
	{
		public uint AuraSpellID;
		public uint CategoryRecoveryTime;
		public byte DifficultyID;
		public uint Id;
		public uint RecoveryTime;
		public uint SpellID;
		public uint StartRecoveryTime;
	}

	public sealed class SpellDurationRecord
	{
		public int Duration;
		public uint Id;
		public int MaxDuration;
	}

	public sealed class SpellEffectRecord
	{
		public float BonusCoefficientFromAP;
		public float Coefficient;
		public uint DifficultyID;
		public uint Effect;
		public float EffectAmplitude;
		public SpellEffectAttributes EffectAttributes;
		public short EffectAura;
		public uint EffectAuraPeriod;
		public float EffectBasePoints;
		public float EffectBonusCoefficient;
		public float EffectChainAmplitude;
		public int EffectChainTargets;
		public int EffectIndex;
		public uint EffectItemType;
		public int EffectMechanic;
		public int[] EffectMiscValue = new int[2];
		public float EffectPointsPerResource;
		public float EffectPosFacing;
		public uint[] EffectRadiusIndex = new uint[2];
		public float EffectRealPointsPerLevel;
		public FlagArray128 EffectSpellClassMask;
		public uint EffectTriggerSpell;
		public float GroupSizeBasePointsCoefficient;
		public uint Id;
		public short[] ImplicitTarget = new short[2];
		public float PvpMultiplier;
		public float ResourceCoefficient;
		public int ScalingClass;
		public uint SpellID;
		public float Variance;
	}

	public sealed class SpellEquippedItemsRecord
	{
		public sbyte EquippedItemClass;
		public int EquippedItemInvTypes;
		public int EquippedItemSubclass;
		public uint Id;
		public uint SpellID;
	}

	public sealed class SpellFocusObjectRecord
	{
		public uint Id;
		public string Name;
	}

	public sealed class SpellInterruptsRecord
	{
		public int[] AuraInterruptFlags = new int[2];
		public int[] ChannelInterruptFlags = new int[2];
		public byte DifficultyID;
		public uint Id;
		public short InterruptFlags;
		public uint SpellID;
	}

	public sealed class SpellItemEnchantmentRecord
	{
		public byte Charges;
		public byte ConditionID;
		public int Duration;
		public ItemEnchantmentType[] Effect = new ItemEnchantmentType[ItemConst.MaxItemEnchantmentEffects];
		public uint[] EffectArg = new uint[ItemConst.MaxItemEnchantmentEffects];
		public ushort[] EffectPointsMin = new ushort[ItemConst.MaxItemEnchantmentEffects];
		public float[] EffectScalingPoints = new float[ItemConst.MaxItemEnchantmentEffects];
		public ushort Flags;
		public string HordeName;
		public uint IconFileDataID;
		public uint Id;
		public ushort ItemLevel;
		public ushort ItemVisual;
		public int MaxItemLevel;
		public byte MaxLevel;
		public int MinItemLevel;
		public byte MinLevel;
		public string Name;
		public ushort RequiredSkillID;
		public ushort RequiredSkillRank;
		public sbyte ScalingClass;
		public sbyte ScalingClassRestricted;
		public uint TransmogCost;
		public uint TransmogUseConditionID;

		public SpellItemEnchantmentFlags GetFlags()
		{
			return (SpellItemEnchantmentFlags)Flags;
		}
	}

	public sealed class SpellItemEnchantmentConditionRecord
	{
		public uint Id;
		public byte[] Logic = new byte[5];
		public uint[] LtOperand = new uint[5];
		public byte[] LtOperandType = new byte[5];
		public byte[] Operator = new byte[5];
		public byte[] RtOperand = new byte[5];
		public byte[] RtOperandType = new byte[5];
	}

	public sealed class SpellLabelRecord
	{
		public uint Id;
		public uint LabelID;
		public uint SpellID;
	}

	public sealed class SpellLearnSpellRecord
	{
		public uint Id;
		public uint LearnSpellID;
		public uint OverridesSpellID;
		public uint SpellID;
	}

	public sealed class SpellLevelsRecord
	{
		public ushort BaseLevel;
		public byte DifficultyID;
		public uint Id;
		public ushort MaxLevel;
		public byte MaxPassiveAuraLevel;
		public uint SpellID;
		public ushort SpellLevel;
	}

	public sealed class SpellMiscRecord
	{
		public uint ActiveIconFileDataID;
		public int ActiveSpellVisualScript;
		public int[] Attributes = new int[15];
		public ushort CastingTimeIndex;
		public uint ContentTuningID;
		public byte DifficultyID;
		public ushort DurationIndex;
		public uint Id;
		public float LaunchDelay;
		public float MinDuration;
		public ushort RangeIndex;
		public byte SchoolMask;
		public int ShowFutureSpellPlayerConditionID;
		public float Speed;
		public uint SpellIconFileDataID;
		public uint SpellID;
		public int SpellVisualScript;
	}

	public sealed class SpellNameRecord
	{
		public uint Id; // SpellID
		public LocalizedString Name;
	}

	public sealed class SpellPowerRecord
	{
		public int AltPowerBarID;
		public uint Id;
		public int ManaCost;
		public int ManaCostPerLevel;
		public int ManaPerSecond;
		public uint OptionalCost; // Spell uses [ManaCost, ManaCost+ManaCostAdditional] power - affects tooltip parsing as Multiplier on SpellEffectEntry::EffectPointsPerResource
		public float OptionalCostPct;
		public byte OrderIndex;
		public float PowerCostMaxPct;
		public float PowerCostPct;
		public uint PowerDisplayID;
		public float PowerPctPerSecond;
		public PowerType PowerType;

		public uint RequiredAuraSpellID;

		//   only SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL, SPELL_EFFECT_WEAPON_PERCENT_DAMAGE, SPELL_EFFECT_WEAPON_DAMAGE, SPELL_EFFECT_NORMALIZED_WEAPON_DMG
		public uint SpellID;
	}

	public sealed class SpellPowerDifficultyRecord
	{
		public byte DifficultyID;
		public uint Id;
		public byte OrderIndex;
	}

	public sealed class SpellProcsPerMinuteRecord
	{
		public float BaseProcRate;
		public byte Flags;
		public uint Id;
	}

	public sealed class SpellProcsPerMinuteModRecord
	{
		public float Coeff;
		public uint Id;
		public uint Param;
		public uint SpellProcsPerMinuteID;
		public SpellProcsPerMinuteModType Type;
	}

	public sealed class SpellRadiusRecord
	{
		public uint Id;
		public float Radius;
		public float RadiusMax;
		public float RadiusMin;
		public float RadiusPerLevel;
	}

	public sealed class SpellRangeRecord
	{
		public string DisplayName;
		public string DisplayNameShort;
		public SpellRangeFlag Flags;
		public uint Id;
		public float[] RangeMax = new float[2];
		public float[] RangeMin = new float[2];
	}

	public sealed class SpellReagentsRecord
	{
		public uint Id;
		public int[] Reagent = new int[SpellConst.MaxReagents];
		public ushort[] ReagentCount = new ushort[SpellConst.MaxReagents];
		public short[] ReagentRecraftCount = new short[SpellConst.MaxReagents];
		public byte[] ReagentSource = new byte[SpellConst.MaxReagents];
		public uint SpellID;
	}

	public sealed class SpellReagentsCurrencyRecord
	{
		public ushort CurrencyCount;
		public ushort CurrencyTypesID;
		public uint Id;
		public int SpellID;
	}

	public sealed class SpellScalingRecord
	{
		public uint Id;
		public uint MaxScalingLevel;
		public uint MinScalingLevel;
		public ushort ScalesFromItemLevel;
		public uint SpellID;
	}

	public sealed class SpellShapeshiftRecord
	{
		public uint Id;
		public uint[] ShapeshiftExclude = new uint[2];
		public uint[] ShapeshiftMask = new uint[2];
		public uint SpellID;
		public sbyte StanceBarOrder;
	}

	public sealed class SpellShapeshiftFormRecord
	{
		public int AttackIconFileID;
		public sbyte BonusActionBar;
		public ushort CombatRoundTime;
		public uint[] CreatureDisplayID = new uint[4];
		public sbyte CreatureType;
		public float DamageVariance;
		public SpellShapeshiftFormFlags Flags;
		public uint Id;
		public ushort MountTypeID;
		public string Name;
		public uint[] PresetSpellID = new uint[SpellConst.MaxShapeshift];
	}

	public sealed class SpellTargetRestrictionsRecord
	{
		public float ConeDegrees;
		public byte DifficultyID;
		public uint Id;
		public uint MaxTargetLevel;
		public byte MaxTargets;
		public uint SpellID;
		public ushort TargetCreatureType;
		public int Targets;
		public float Width;
	}

	public sealed class SpellTotemsRecord
	{
		public uint Id;
		public ushort[] RequiredTotemCategoryID = new ushort[SpellConst.MaxTotems];
		public uint SpellID;
		public uint[] Totem = new uint[SpellConst.MaxTotems];
	}

	public sealed class SpellVisualRecord
	{
		public uint AnimEventSoundID;
		public uint CasterSpellVisualID;
		public ushort DamageNumberDelay;
		public int Flags;
		public uint HostileSpellVisualID;
		public uint Id;
		public uint LowViolenceSpellVisualID;
		public sbyte MissileAttachment;
		public float[] MissileCastOffset = new float[3];
		public uint MissileCastPositionerID;
		public sbyte MissileDestinationAttachment;
		public float[] MissileImpactOffset = new float[3];
		public uint MissileImpactPositionerID;
		public int MissileTargetingKit;
		public uint RaidSpellVisualMissileSetID;
		public int ReducedUnexpectedCameraMovementSpellVisualID;
		public ushort SpellVisualMissileSetID;
	}

	public sealed class SpellVisualEffectNameRecord
	{
		public float Alpha;
		public float BaseMissileSpeed;
		public int DissolveEffectID;
		public float EffectRadius;
		public uint Flags;
		public int GenericID;
		public uint Id;
		public float MaxAllowedScale;
		public float MinAllowedScale;
		public int ModelFileDataID;
		public int ModelPosition;
		public uint RibbonQualityID;
		public float Scale;
		public int TextureFileDataID;
		public uint Type;
		public sbyte Unknown901;
	}

	public sealed class SpellVisualMissileRecord
	{
		public uint AnimKitID;
		public sbyte Attachment;
		public float[] CastOffset = new float[3];
		public ushort CastPositionerID;
		public sbyte ClutterLevel;
		public int DecayTimeAfterImpact;
		public sbyte DestinationAttachment;
		public uint Flags;
		public ushort FollowGroundApproach;
		public uint FollowGroundDropSpeed;
		public int FollowGroundHeight;
		public uint Id;
		public float[] ImpactOffset = new float[3];
		public ushort ImpactPositionerID;
		public uint SoundEntriesID;
		public ushort SpellMissileMotionID;
		public ushort SpellVisualEffectNameID;
		public uint SpellVisualMissileSetID;
	}

	public sealed class SpellVisualKitRecord
	{
		public ushort DelayMax;
		public ushort DelayMin;
		public sbyte FallbackPriority;
		public int FallbackSpellVisualKitId;
		public int[] Flags = new int[2];
		public uint Id;
	}

	public sealed class SpellXSpellVisualRecord
	{
		public int ActiveIconFileID;
		public uint CasterPlayerConditionID;
		public ushort CasterUnitConditionID;
		public byte DifficultyID;
		public int Flags;
		public uint Id;
		public int Priority;
		public float Probability;
		public int SpellIconFileID;
		public uint SpellID;
		public uint SpellVisualID;
		public uint ViewerPlayerConditionID;
		public ushort ViewerUnitConditionID;
	}

	public sealed class SummonPropertiesRecord
	{
		public SummonCategory Control;
		public uint Faction;
		public uint[] Flags = new uint[2];
		public uint Id;
		public int Slot;
		public SummonTitle Title;

		public SummonPropertiesFlags GetFlags()
		{
			return (SummonPropertiesFlags)Flags[0];
		}
	}
}