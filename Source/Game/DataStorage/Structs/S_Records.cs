// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using System;

namespace Game.DataStorage
{
    public sealed class ScenarioRecord
    {
        public uint Id;
        public string Name;
        public ushort AreaTableID;
        public byte Type;
        public byte Flags;
        public uint UiTextureKitID;
    }

    public sealed class ScenarioStepRecord
    {
        public string Description;
        public string Title;
        public uint Id;
        public ushort ScenarioID;
        public uint CriteriaTreeId;
        public uint RewardQuestID;
        public int RelatedStep;                                              // Bonus step can only be completed if scenario is in the step specified in this field
        public ushort Supersedes;                                              // Used in conjunction with Proving Grounds scenarios, when sequencing steps (Not using step order?)
        public byte OrderIndex;
        public byte Flags;
        public uint VisibilityPlayerConditionID;
        public ushort WidgetSetID;

        // helpers
        public bool HasFlag(ScenarioStepFlags scenarioStepFlags) { return (Flags & (byte)scenarioStepFlags) != 0; }
        public bool IsBonusObjective()
        {
            return HasFlag(ScenarioStepFlags.BonusObjective);
        }
    }

    public sealed class SceneScriptRecord
    {
        public uint Id;
        public ushort FirstSceneScriptID;
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

    public sealed class ServerMessagesRecord
    {
        public uint Id;
        public LocalizedString Text;
    }

    public sealed class SkillLineRecord
    {
        public LocalizedString DisplayName;
        public string AlternateVerb;
        public string Description;
        public string HordeDisplayName;
        public string OverrideSourceInfoDisplayName;
        public uint Id;
        public SkillCategory CategoryID;
        public int SpellIconFileID;
        public sbyte CanLink;
        public uint ParentSkillLineID;
        public int ParentTierIndex;
        public int Flags;
        public int SpellBookSpellID;
        public int ExpansionNameSharedStringID;
        public int HordeExpansionNameSharedStringID;

        public bool HasFlag(SkillLineFlags skillLineFlags) { return (Flags & (ushort)skillLineFlags) != 0; }
    }

    public sealed class SkillLineAbilityRecord
    {
        public long RaceMask;
        public string AbilityVerb;
        public string AbilityAllVerb;
        public uint Id;
        public ushort SkillLine;
        public uint Spell;
        public short MinSkillLineRank;
        public int ClassMask;
        public uint SupercedesSpell;
        public AbilityLearnType AcquireMethod;
        public ushort TrivialSkillLineRankHigh;
        public ushort TrivialSkillLineRankLow;
        public int Flags;
        public byte NumSkillUps;
        public short UniqueBit;
        public short TradeSkillCategoryID;
        public ushort SkillupSkillLineID;

        public bool HasFlag(SkillLineAbilityFlags skillLineAbilityFlags) { return (Flags & (int)skillLineAbilityFlags) != 0; }
    }

    public sealed class SkillLineXTraitTreeRecord
    {
        public uint Id;
        public uint SkillLineID;
        public int TraitTreeID;
        public int OrderIndex;
    }

    public sealed class SkillRaceClassInfoRecord
    {
        public uint Id;
        public long RaceMask;
        public ushort SkillID;
        public int ClassMask;
        public int Flags;
        public int Availability;
        public sbyte MinLevel;
        public ushort SkillTierID;

        public bool HasFlag(SkillRaceClassInfoFlags skillRaceClassInfoFlags) { return (Flags & (int)skillRaceClassInfoFlags) != 0; }
    }

    public sealed class SoulbindConduitRankRecord
    {
        public uint Id;
        public int RankIndex;
        public int SpellID;
        public float AuraPointsOverride;
        public uint SoulbindConduitID;
    }

    public sealed class SoundKitRecord
    {
        public uint Id;
        public uint SoundType;
        public float VolumeFloat;
        public int Flags;
        public float MinDistance;
        public float DistanceCutoff;
        public byte EAXDef;
        public uint SoundKitAdvancedID;
        public float VolumeVariationPlus;
        public float VolumeVariationMinus;
        public float PitchVariationPlus;
        public float PitchVariationMinus;
        public sbyte DialogType;
        public float PitchAdjust;
        public ushort BusOverwriteID;
        public byte MaxInstances;
        public uint SoundMixGroupID;
    }

    public sealed class SpecializationSpellsRecord
    {
        public string Description;
        public uint Id;
        public ushort SpecID;
        public uint SpellID;
        public uint OverridesSpellID;
        public byte DisplayOrder;
    }

    public sealed class SpecSetMemberRecord
    {
        public uint Id;
        public uint ChrSpecializationID;
        public uint SpecSetID;
    }

    public sealed class SpellAuraOptionsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public ushort CumulativeAura;
        public uint ProcCategoryRecovery;
        public byte ProcChance;
        public int ProcCharges;
        public ushort SpellProcsPerMinuteID;
        public int[] ProcTypeMask = new int[2];
        public uint SpellID;
    }

    public sealed class SpellAuraRestrictionsRecord
    {
        public uint Id;
        public uint DifficultyID;
        public int CasterAuraState;
        public int TargetAuraState;
        public int ExcludeCasterAuraState;
        public int ExcludeTargetAuraState;
        public uint CasterAuraSpell;
        public uint TargetAuraSpell;
        public uint ExcludeCasterAuraSpell;
        public uint ExcludeTargetAuraSpell;
        public int CasterAuraType;
        public int TargetAuraType;
        public int ExcludeCasterAuraType;
        public int ExcludeTargetAuraType;
        public uint SpellID;
    }

    public sealed class SpellCastTimesRecord
    {
        public uint Id;
        public int Base;
        public int Minimum;
    }

    public sealed class SpellCastingRequirementsRecord
    {
        public uint Id;
        public uint SpellID;
        public byte FacingCasterFlags;
        public ushort MinFactionID;
        public int MinReputation;
        public ushort RequiredAreasID;
        public byte RequiredAuraVision;
        public ushort RequiresSpellFocus;
    }

    public sealed class SpellCategoriesRecord
    {
        public uint Id;
        public byte DifficultyID;
        public ushort Category;
        public sbyte DefenseType;
        public sbyte DispelType;
        public sbyte Mechanic;
        public sbyte PreventionType;
        public ushort StartRecoveryCategory;
        public ushort ChargeCategory;
        public uint SpellID;
    }

    public sealed class SpellCategoryRecord
    {
        public uint Id;
        public string Name;
        public int Flags;
        public byte UsesPerWeek;
        public byte MaxCharges;
        public int ChargeRecoveryTime;
        public int TypeMask;

        public bool HasFlag(SpellCategoryFlags spellCategoryFlags) { return (Flags & (int)spellCategoryFlags) != 0; }
    }

    public sealed class SpellClassOptionsRecord
    {
        public uint Id;
        public uint SpellID;
        public uint ModalNextSpell;
        public byte SpellClassSet;
        public FlagArray128 SpellClassMask;
    }

    public sealed class SpellCooldownsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public uint CategoryRecoveryTime;
        public uint RecoveryTime;
        public uint StartRecoveryTime;
        public uint AuraSpellID;
        public uint SpellID;
    }

    public sealed class SpellDurationRecord
    {
        public uint Id;
        public int Duration;
        public int MaxDuration;
    }

    public sealed class SpellEffectRecord
    {
        public uint Id;
        public short EffectAura;
        public uint DifficultyID;
        public int EffectIndex;
        public uint Effect;
        public float EffectAmplitude;
        public SpellEffectAttributes EffectAttributes;
        public uint EffectAuraPeriod;
        public float EffectBonusCoefficient;
        public float EffectChainAmplitude;
        public int EffectChainTargets;
        public uint EffectItemType;
        public int EffectMechanic;
        public float EffectPointsPerResource;
        public float EffectPosFacing;
        public float EffectRealPointsPerLevel;
        public uint EffectTriggerSpell;
        public float BonusCoefficientFromAP;
        public float PvpMultiplier;
        public float Coefficient;
        public float Variance;
        public float ResourceCoefficient;
        public float GroupSizeBasePointsCoefficient;
        public float EffectBasePoints;
        public int ScalingClass;
        public int[] EffectMiscValue = new int[2];
        public uint[] EffectRadiusIndex = new uint[2];
        public FlagArray128 EffectSpellClassMask;
        public short[] ImplicitTarget = new short[2];
        public uint SpellID;
    }

    public sealed class SpellEmpowerRecord
    {
        public uint Id;
        public int SpellID;
        public int Unused1000;
    }

    public sealed class SpellEmpowerStageRecord
    {
        public uint Id;
        public int Stage;
        public int DurationMs;
        public uint SpellEmpowerID;
    }

    public sealed class SpellEquippedItemsRecord
    {
        public uint Id;
        public uint SpellID;
        public sbyte EquippedItemClass;
        public int EquippedItemInvTypes;
        public int EquippedItemSubclass;
    }

    public sealed class SpellFocusObjectRecord
    {
        public uint Id;
        public string Name;
    }

    public sealed class SpellInterruptsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public short InterruptFlags;
        public int[] AuraInterruptFlags = new int[2];
        public int[] ChannelInterruptFlags = new int[2];
        public uint SpellID;
    }

    public sealed class SpellItemEnchantmentRecord
    {
        public uint Id;
        public string Name;
        public string HordeName;
        public int Duration;
        public uint[] EffectArg = new uint[ItemConst.MaxItemEnchantmentEffects];
        public int Flags;
        public float[] EffectScalingPoints = new float[ItemConst.MaxItemEnchantmentEffects];
        public uint IconFileDataID;
        public int MinItemLevel;
        public int MaxItemLevel;
        public uint TransmogUseConditionID;
        public uint TransmogCost;
        public ushort[] EffectPointsMin = new ushort[ItemConst.MaxItemEnchantmentEffects];
        public ushort ItemVisual;
        public ushort RequiredSkillID;
        public ushort RequiredSkillRank;
        public ushort ItemLevel;
        public byte Charges;
        public ItemEnchantmentType[] Effect = new ItemEnchantmentType[ItemConst.MaxItemEnchantmentEffects];
        public sbyte ScalingClass;
        public sbyte ScalingClassRestricted;
        public byte ConditionID;
        public byte MinLevel;
        public byte MaxLevel;

        public bool HasFlag(SpellItemEnchantmentFlags spellItemEnchantmentFlags) { return (Flags & (ushort)spellItemEnchantmentFlags) != 0; }
    }

    public sealed class SpellItemEnchantmentConditionRecord
    {
        public uint Id;
        public byte[] LtOperandType = new byte[5];
        public uint[] LtOperand = new uint[5];
        public byte[] Operator = new byte[5];
        public byte[] RtOperandType = new byte[5];
        public byte[] RtOperand = new byte[5];
        public byte[] Logic = new byte[5];
    }

    public sealed class SpellKeyboundOverrideRecord
    {
        public uint Id;
        public string Function;
        public sbyte Type;
        public uint Data;
        public int Flags;
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
        public uint SpellID;
        public uint LearnSpellID;
        public uint OverridesSpellID;
    }

    public sealed class SpellLevelsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public ushort MaxLevel;
        public byte MaxPassiveAuraLevel;
        public ushort BaseLevel;
        public ushort SpellLevel;
        public uint SpellID;
    }

    public sealed class SpellMiscRecord
    {
        public uint Id;
        public int[] Attributes = new int[16];
        public byte DifficultyID;
        public ushort CastingTimeIndex;
        public ushort DurationIndex;
        public ushort PvPDurationIndex;
        public ushort RangeIndex;
        public byte SchoolMask;
        public float Speed;
        public float LaunchDelay;
        public float MinDuration;
        public uint SpellIconFileDataID;
        public uint ActiveIconFileDataID;
        public uint ContentTuningID;
        public int ShowFutureSpellPlayerConditionID;
        public int SpellVisualScript;
        public int ActiveSpellVisualScript;
        public uint SpellID;
    }

    public sealed class SpellNameRecord
    {
        public uint Id;                      // SpellID
        public LocalizedString Name;
    }

    public sealed class SpellPowerRecord
    {
        public uint Id;
        public byte OrderIndex;
        public int ManaCost;
        public int ManaCostPerLevel;
        public int ManaPerSecond;
        public uint PowerDisplayID;
        public int AltPowerBarID;
        public float PowerCostPct;
        public float PowerCostMaxPct;
        public float OptionalCostPct;
        public float PowerPctPerSecond;
        public PowerType PowerType;
        public uint RequiredAuraSpellID;
        public uint OptionalCost;                                            // Spell uses [ManaCost, ManaCost+ManaCostAdditional] power - affects tooltip parsing as multiplier on SpellEffectEntry::EffectPointsPerResource
                                                                             //   only SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL, SPELL_EFFECT_WEAPON_PERCENT_DAMAGE, SPELL_EFFECT_WEAPON_DAMAGE, SPELL_EFFECT_NORMALIZED_WEAPON_DMG
        public uint SpellID;
    }

    public sealed class SpellPowerDifficultyRecord
    {
        public uint Id;
        public byte DifficultyID;
        public byte OrderIndex;
    }

    public sealed class SpellProcsPerMinuteRecord
    {
        public uint Id;
        public float BaseProcRate;
        public byte Flags;
    }

    public sealed class SpellProcsPerMinuteModRecord
    {
        public uint Id;
        public SpellProcsPerMinuteModType Type;
        public uint Param;
        public float Coeff;
        public uint SpellProcsPerMinuteID;
    }

    public sealed class SpellRadiusRecord
    {
        public uint Id;
        public float Radius;
        public float RadiusPerLevel;
        public float RadiusMin;
        public float RadiusMax;
    }

    public sealed class SpellRangeRecord
    {
        public uint Id;
        public string DisplayName;
        public string DisplayNameShort;
        public byte Flags;
        public float[] RangeMin = new float[2];
        public float[] RangeMax = new float[2];

        public bool HasFlag(SpellRangeFlag spellRangeFlag) { return (Flags & (byte)spellRangeFlag) != 0; }
    }

    public sealed class SpellReagentsRecord
    {
        public uint Id;
        public uint SpellID;
        public int[] Reagent = new int[SpellConst.MaxReagents];
        public ushort[] ReagentCount = new ushort[SpellConst.MaxReagents];
        public short[] ReagentRecraftCount = new short[SpellConst.MaxReagents];
        public byte[] ReagentSource = new byte[SpellConst.MaxReagents];
    }

    public sealed class SpellReagentsCurrencyRecord
    {
        public uint Id;
        public int SpellID;
        public ushort CurrencyTypesID;
        public ushort CurrencyCount;
    }

    public sealed class SpellScalingRecord
    {
        public uint Id;
        public uint SpellID;
        public uint MinScalingLevel;
        public uint MaxScalingLevel;
        public ushort ScalesFromItemLevel;
    }

    public sealed class SpellShapeshiftRecord
    {
        public uint Id;
        public uint SpellID;
        public sbyte StanceBarOrder;
        public uint[] ShapeshiftExclude = new uint[2];
        public uint[] ShapeshiftMask = new uint[2];
    }

    public sealed class SpellShapeshiftFormRecord
    {
        public uint Id;
        public string Name;
        public uint CreatureDisplayID;
        public byte CreatureType;
        public int Flags;
        public int AttackIconFileID;
        public sbyte BonusActionBar;
        public ushort CombatRoundTime;
        public float DamageVariance;
        public ushort MountTypeID;
        public uint[] PresetSpellID = new uint[SpellConst.MaxShapeshift];

        public bool HasFlag(SpellShapeshiftFormFlags spellShapeshiftFormFlags) { return (Flags & (int)spellShapeshiftFormFlags) != 0; }
    }

    public sealed class SpellTargetRestrictionsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public float ConeDegrees;
        public byte MaxTargets;
        public uint MaxTargetLevel;
        public ushort TargetCreatureType;
        public int Targets;
        public float Width;
        public uint SpellID;
    }

    public sealed class SpellTotemsRecord
    {
        public uint Id;
        public uint SpellID;
        public ushort[] RequiredTotemCategoryID = new ushort[SpellConst.MaxTotems];
        public uint[] Totem = new uint[SpellConst.MaxTotems];
    }

    public sealed class SpellVisualRecord
    {
        public uint Id;
        public float[] MissileCastOffset = new float[3];
        public float[] MissileImpactOffset = new float[3];
        public uint AnimEventSoundID;
        public int Flags;
        public sbyte MissileAttachment;
        public sbyte MissileDestinationAttachment;
        public uint MissileCastPositionerID;
        public uint MissileImpactPositionerID;
        public int MissileTargetingKit;
        public uint HostileSpellVisualID;
        public uint CasterSpellVisualID;
        public ushort SpellVisualMissileSetID;
        public ushort DamageNumberDelay;
        public uint LowViolenceSpellVisualID;
        public uint RaidSpellVisualMissileSetID;
        public int ReducedUnexpectedCameraMovementSpellVisualID;
    }

    public sealed class SpellVisualEffectNameRecord
    {
        public uint Id;
        public int ModelFileDataID;
        public float BaseMissileSpeed;
        public float Scale;
        public float MinAllowedScale;
        public float MaxAllowedScale;
        public float Alpha;
        public uint Flags;
        public int TextureFileDataID;
        public float EffectRadius;
        public uint Type;
        public int GenericID;
        public uint RibbonQualityID;
        public int DissolveEffectID;
        public int ModelPosition;
        public sbyte Unknown901;
        public ushort Unknown1100;
    }

    public sealed class SpellVisualKitRecord
    {
        public uint ID;
        public int ClutterLevel;
        public int FallbackSpellVisualKitId;
        public ushort DelayMin;
        public ushort DelayMax;
        public int[] Flags = new int[2];
    }

    public sealed class SpellVisualMissileRecord
    {
        public float[] CastOffset = new float[3];
        public float[] ImpactOffset = new float[3];
        public uint Id;
        public ushort SpellVisualEffectNameID;
        public uint SoundEntriesID;
        public sbyte Attachment;
        public sbyte DestinationAttachment;
        public ushort CastPositionerID;
        public ushort ImpactPositionerID;
        public int FollowGroundHeight;
        public uint FollowGroundDropSpeed;
        public ushort FollowGroundApproach;
        public uint Flags;
        public ushort SpellMissileMotionID;
        public uint AnimKitID;
        public int ClutterLevel;
        public int DecayTimeAfterImpact;
        public ushort Unused1100;
        public uint SpellVisualMissileSetID;
    }

    public sealed class SpellXSpellVisualRecord
    {
        public uint Id;
        public byte DifficultyID;
        public uint SpellVisualID;
        public float Probability;
        public int Flags;
        public int Priority;
        public int SpellIconFileID;
        public int ActiveIconFileID;
        public ushort ViewerUnitConditionID;
        public uint ViewerPlayerConditionID;
        public ushort CasterUnitConditionID;
        public uint CasterPlayerConditionID;
        public uint SpellID;
    }

    public sealed class SummonPropertiesRecord
    {
        public uint Id;
        public SummonCategory Control;
        public uint Faction;
        public SummonTitle Title;
        public int Slot;
        public uint[] Flags = new uint[2];

        public bool HasFlag(SummonPropertiesFlags summonPropertiesFlags) { return (Flags[0] & (uint)summonPropertiesFlags) != 0; }
    }
}
