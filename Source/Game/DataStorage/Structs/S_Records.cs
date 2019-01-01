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
using Framework.Dynamic;
using System;

namespace Game.DataStorage
{
    public sealed class ScalingStatDistributionRecord
    {
        public uint Id;
        public ushort PlayerLevelToItemLevelCurveID;
        public int MinLevel;
        public int MaxLevel;
    }

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
        public uint Id;
        public string Description;
        public string Title;
        public ushort ScenarioID;
        public uint CriteriaTreeId;
        public ushort RewardQuestID;
        public int RelatedStep;                                              // Bonus step can only be completed if scenario is in the step specified in this field
        public ushort Supersedes;                                              // Used in conjunction with Proving Grounds scenarios, when sequencing steps (Not using step order?)
        public byte OrderIndex;
        public ScenarioStepFlags Flags;
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
        public uint Id;
        public ushort FirstSceneScriptID;
        public ushort NextSceneScriptID;
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
    }

    public sealed class SceneScriptTextRecord
    {
        public uint Id;
        public string Name;
        public string Script;
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
        public ushort Flags;
        public int SpellBookSpellID;
    }

    public sealed class SkillLineAbilityRecord
    {
        public ulong RaceMask;
        public uint Id;
        public ushort SkillLine;
        public uint Spell;
        public short MinSkillLineRank;
        public int ClassMask;
        public uint SupercedesSpell;
        public AbilityLearnType AcquireMethod;
        public ushort TrivialSkillLineRankHigh;
        public ushort TrivialSkillLineRankLow;
        public sbyte Flags;
        public byte NumSkillUps;
        public short UniqueBit;
        public short TradeSkillCategoryID;
        public ushort SkillupSkillLineID;
    }

    public sealed class SkillRaceClassInfoRecord
    {
        public uint Id;
        public long RaceMask;
        public ushort SkillID;
        public int ClassMask;
        public SkillRaceClassInfoFlags Flags;
        public sbyte Availability;
        public sbyte MinLevel;
        public ushort SkillTierID;
    }

    public sealed class SoundKitRecord
    {
        public uint Id;
        public byte SoundType;
        public float VolumeFloat;
        public ushort Flags;
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

    public sealed class SpellAuraOptionsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public ushort CumulativeAura;
        public uint ProcCategoryRecovery;
        public byte ProcChance;
        public uint ProcCharges;
        public ushort SpellProcsPerMinuteID;
        public int[] ProcTypeMask = new int[2];
        public uint SpellID;
    }

    public sealed class SpellAuraRestrictionsRecord
    {
        public uint Id;
        public byte DifficultyID;
        public byte CasterAuraState;
        public byte TargetAuraState;
        public byte ExcludeCasterAuraState;
        public byte ExcludeTargetAuraState;
        public uint CasterAuraSpell;
        public uint TargetAuraSpell;
        public uint ExcludeCasterAuraSpell;
        public uint ExcludeTargetAuraSpell;
        public uint SpellID;
    }

    public sealed class SpellCastTimesRecord
    {
        public uint Id;
        public int Base;
        public short PerLevel;
        public int Minimum;
    }

    public sealed class SpellCastingRequirementsRecord
    {
        public uint Id;
        public uint SpellID;
        public byte FacingCasterFlags;
        public ushort MinFactionID;
        public sbyte MinReputation;
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
        public SpellCategoryFlags Flags;
        public byte UsesPerWeek;
        public byte MaxCharges;
        public int ChargeRecoveryTime;
        public int TypeMask;
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
        public uint SpellID;
    }

    public sealed class SpellDurationRecord
    {
        public uint Id;
        public int Duration;
        public uint DurationPerLevel;
        public int MaxDuration;
    }

    public sealed class SpellEffectRecord
    {
        public uint Id;
        public uint DifficultyID;
        public uint EffectIndex;
        public uint Effect;
        public float EffectAmplitude;
        public int EffectAttributes;
        public short EffectAura;
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
        public int[] EffectMiscValue = new int[2];
        public uint[] EffectRadiusIndex = new uint[2];
        public FlagArray128 EffectSpellClassMask;
        public short[] ImplicitTarget = new short[2];
        public uint SpellID;
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
        public uint[] AuraInterruptFlags = new uint[2];
        public uint[] ChannelInterruptFlags = new uint[2];
        public uint SpellID;
    }

    public sealed class SpellItemEnchantmentRecord
    {
        public uint Id;
        public string Name;
        public string HordeName;
        public uint[] EffectArg = new uint[ItemConst.MaxItemEnchantmentEffects];
        public float[] EffectScalingPoints = new float[ItemConst.MaxItemEnchantmentEffects];
        public uint TransmogCost;
        public uint IconFileDataID;
        public uint TransmogPlayerConditionID;
        public ushort[] EffectPointsMin = new ushort[ItemConst.MaxItemEnchantmentEffects];
        public ushort ItemVisual;
        public EnchantmentSlotMask Flags;
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
        public ushort BaseLevel;
        public ushort MaxLevel;
        public ushort SpellLevel;
        public byte MaxPassiveAuraLevel;
        public uint SpellID;
    }

    public sealed class SpellMiscRecord
    {
        public uint Id;
        public byte DifficultyID;
        public ushort CastingTimeIndex;
        public ushort DurationIndex;
        public ushort RangeIndex;
        public byte SchoolMask;
        public float Speed;
        public float LaunchDelay;
        public float MinDuration;
        public uint SpellIconFileDataID;
        public uint ActiveIconFileDataID;
        public int[] Attributes = new int[14];
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
        public ushort Param;
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
        public SpellRangeFlag Flags;
        public float[] RangeMin = new float[2];
        public float[] RangeMax = new float[2];
    }

    public sealed class SpellReagentsRecord
    {
        public uint Id;
        public uint SpellID;
        public int[] Reagent = new int[SpellConst.MaxReagents];
        public ushort[] ReagentCount = new ushort[SpellConst.MaxReagents];
    }

    public sealed class SpellScalingRecord
    {
        public uint Id;
        public uint SpellID;
        public int Class;
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
        public sbyte CreatureType;
        public SpellShapeshiftFormFlags Flags;
        public int AttackIconFileID;
        public sbyte BonusActionBar;
        public ushort CombatRoundTime;
        public float DamageVariance;
        public ushort MountTypeID;
        public uint[] CreatureDisplayID = new uint[4];
        public uint[] PresetSpellID = new uint[SpellConst.MaxShapeshift];
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

    public sealed class SpellXSpellVisualRecord
    {
        public uint Id;
        public byte DifficultyID;
        public uint SpellVisualID;
        public float Probability;
        public byte Flags;
        public byte Priority;
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
        public SummonType Title;
        public int Slot;
        public SummonPropFlags Flags;
    }
}
