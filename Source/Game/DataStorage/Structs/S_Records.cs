/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
    public sealed class SandboxScalingRecord
    {
        public uint Id;
        public uint MinLevel;
        public uint MaxLevel;
        public uint Flags;
    }

    public sealed class ScalingStatDistributionRecord
    {
        public uint Id;
        public ushort PlayerLevelToItemLevelCurveID;
        public byte MinLevel;
        public uint MaxLevel;
    }

    public sealed class ScenarioRecord
    {
        public uint Id;
        public string Name;
        public ushort AreaTableID;
        public byte Flags;
        public byte Type;
    }

    public sealed class ScenarioStepRecord
    {
        public uint Id;
        public string Description;
        public string Title;
        public ushort ScenarioID;
        public ushort Supersedes;                                          // Used in conjunction with Proving Grounds scenarios, when sequencing steps (Not using step order?)
        public ushort RewardQuestID;
        public byte OrderIndex;
        public ScenarioStepFlags Flags;
        public ushort CriteriaTreeId;
        public byte RelatedStep;                                     // Bonus step can only be completed if scenario is in the step specified in this field

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
        public uint Id;
        public LocalizedString DisplayName;
        public string Description;
        public string AlternateVerb;
        public ushort Flags;
        public SkillCategory CategoryID;
        public byte CanLink;
        public uint SpellIconFileID;
        public byte ParentSkillLineID;
    }

    public sealed class SkillLineAbilityRecord
    {
        public long RaceMask;
        public uint Id;
        public uint Spell;
        public uint SupercedesSpell;
        public ushort SkillLine;
        public ushort TrivialSkillLineRankHigh;
        public ushort TrivialSkillLineRankLow;
        public ushort UniqueBit;
        public ushort TradeSkillCategoryID;
        public byte NumSkillUps;
        public int ClassMask;
        public ushort MinSkillLineRank;
        public AbilytyLearnType AcquireMethod;
        public byte Flags;
    }

    public sealed class SkillRaceClassInfoRecord
    {
        public uint Id;
        public long RaceMask;
        public ushort SkillID;
        public SkillRaceClassInfoFlags Flags;
        public ushort SkillTierID;
        public byte Availability;
        public byte MinLevel;
        public int ClassMask;
    }

    public sealed class SoundKitRecord
    {
        public uint Id;
        public float VolumeFloat;
        public float MinDistance;
        public float DistanceCutoff;
        public ushort Flags;
        public ushort SoundEntriesAdvancedID;
        public byte SoundType;
        public byte DialogType;
        public byte EAXDef;
        public float VolumeVariationPlus;
        public float VolumeVariationMinus;
        public float PitchVariationPlus;
        public float PitchVariationMinus;
        public float PitchAdjust;
        public ushort BusOverwriteID;
        public byte MaxInstances;
    }

    public sealed class SpecializationSpellsRecord
    {
        public string Description;
        public uint SpellID;
        public uint OverridesSpellID;
        public ushort SpecID;
        public byte DisplayOrder;
        public uint Id;
    }

    public sealed class SpellRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string NameSubtext;
        public string Description;
        public string AuraDescription;
    }

    public sealed class SpellAuraOptionsRecord
    {
        public uint Id;
        public uint ProcCharges;
        public uint ProcTypeMask;
        public uint ProcCategoryRecovery;
        public ushort CumulativeAura;
        public ushort SpellProcsPerMinuteID;
        public byte DifficultyID;
        public byte ProcChance;
        public uint SpellID;
    }

    public sealed class SpellAuraRestrictionsRecord
    {
        public uint Id;
        public uint CasterAuraSpell;
        public uint TargetAuraSpell;
        public uint ExcludeCasterAuraSpell;
        public uint ExcludeTargetAuraSpell;
        public byte DifficultyID;
        public byte CasterAuraState;
        public byte TargetAuraState;
        public byte ExcludeCasterAuraState;
        public byte ExcludeTargetAuraState;
        public uint SpellID;
    }

    public sealed class SpellCastTimesRecord
    {
        public uint Id;
        public int Base;
        public int Minimum;
        public short PerLevel;
    }

    public sealed class SpellCastingRequirementsRecord
    {
        public uint Id;
        public uint SpellID;
        public ushort MinFactionID;
        public ushort RequiredAreasID;
        public ushort RequiresSpellFocus;
        public byte FacingCasterFlags;
        public byte MinReputation;
        public byte RequiredAuraVision;
    }

    public sealed class SpellCategoriesRecord
    {
        public uint Id;
        public ushort Category;
        public ushort StartRecoveryCategory;
        public ushort ChargeCategory;
        public byte DifficultyID;
        public byte DefenseType;
        public byte DispelType;
        public byte Mechanic;
        public byte PreventionType;
        public uint SpellID;
    }

    public sealed class SpellCategoryRecord
    {
        public uint Id;
        public string Name;
        public int ChargeRecoveryTime;
        public SpellCategoryFlags Flags;
        public byte UsesPerWeek;
        public byte MaxCharges;
        public byte TypeMask;
    }

    public sealed class SpellClassOptionsRecord
    {
        public uint Id;
        public uint SpellID;
        public FlagArray128 SpellClassMask;
        public byte SpellClassSet;
        public ushort ModalNextSpell;
    }

    public sealed class SpellCooldownsRecord
    {
        public uint Id;
        public uint CategoryRecoveryTime;
        public uint RecoveryTime;
        public uint StartRecoveryTime;
        public byte DifficultyID;
        public uint SpellID;
    }

    public sealed class SpellDurationRecord
    {
        public uint Id;
        public int Duration;
        public int MaxDuration;
        public int DurationPerLevel;
    }

    public sealed class SpellEffectRecord
    {
        public uint Id;
        public uint Effect;
        public int EffectBasePoints;
        public byte EffectIndex;
        public uint EffectAura;
        public uint DifficultyID;
        public float EffectAmplitude;
        public uint EffectAuraPeriod;
        public float EffectBonusCoefficient;
        public float EffectChainAmplitude;
        public int EffectChainTargets;
        public int EffectDieSides;
        public uint EffectItemType;
        public uint EffectMechanic;
        public float EffectPointsPerResource;
        public float EffectRealPointsPerLevel;
        public uint EffectTriggerSpell;
        public float EffectPosFacing;
        public uint EffectAttributes;
        public float BonusCoefficientFromAP;
        public float PvpMultiplier;
        public float Coefficient;
        public float Variance;
        public float ResourceCoefficient;
        public float GroupSizeBasePointsCoefficient;
        public FlagArray128 EffectSpellClassMask;
        public int[] EffectMiscValue = new int[2];
        public uint[] EffectRadiusIndex = new uint[2];
        public uint[] ImplicitTarget = new uint[2];
        public uint SpellID;
    }

    public sealed class SpellEquippedItemsRecord
    {
        public uint Id;
        public uint SpellID;
        public int EquippedItemInvTypes;
        public int EquippedItemSubclass;
        public sbyte EquippedItemClass;
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
        public ushort InterruptFlags;
        public uint[] AuraInterruptFlags = new uint[2];
        public uint[] ChannelInterruptFlags = new uint[2];
        public uint SpellID;
    }

    public sealed class SpellItemEnchantmentRecord
    {
        public uint Id;
        public string Name;
        public uint[] EffectArg = new uint[ItemConst.MaxItemEnchantmentEffects];
        public float[] EffectScalingPoints = new float[ItemConst.MaxItemEnchantmentEffects];
        public uint TransmogCost;
        public uint IconFileDataID;
        public ushort[] EffectPointsMin = new ushort[ItemConst.MaxItemEnchantmentEffects];
        public ushort ItemVisual;
        public EnchantmentSlotMask Flags;
        public ushort RequiredSkillID;
        public ushort RequiredSkillRank;
        public ushort ItemLevel;
        public byte Charges;
        public ItemEnchantmentType[] Effect = new ItemEnchantmentType[ItemConst.MaxItemEnchantmentEffects];
        public byte ConditionID;
        public byte MinLevel;
        public byte MaxLevel;
        public sbyte ScalingClass;
        public sbyte ScalingClassRestricted;
        public ushort TransmogPlayerConditionID;
    }

    public sealed class SpellItemEnchantmentConditionRecord
    {
        public uint Id;
        public uint[] LtOperand = new uint[5];
        public byte[] LtOperandType = new byte[5];
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
        public ushort BaseLevel;
        public ushort MaxLevel;
        public ushort SpellLevel;
        public byte DifficultyID;
        public byte MaxPassiveAuraLevel;
        public uint SpellID;
    }

    public sealed class SpellMiscRecord
    {
        public uint Id;
        public ushort CastingTimeIndex;
        public ushort DurationIndex;
        public ushort RangeIndex;
        public byte SchoolMask;
        public uint SpellIconFileDataID;
        public float Speed;
        public uint ActiveIconFileDataID;
        public float LaunchDelay;
        public byte DifficultyID;
        public uint[] Attributes = new uint[14];
        public uint SpellID;
    }

    public sealed class SpellPowerRecord
    {
        public int ManaCost;
        public float PowerCostPct;
        public float PowerPctPerSecond;
        public uint RequiredAuraSpellID;
        public float PowerCostMaxPct;
        public byte OrderIndex;
        public PowerType PowerType;
        public uint Id;
        public byte ManaCostPerLevel;
        public ushort ManaPerSecond;
        public int OptionalCost;                                      // Spell uses [ManaCost, ManaCost+ManaCostAdditional] power - affects tooltip parsing as multiplier on SpellEffectEntry::EffectPointsPerResource
                                                                        //   only SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL, SPELL_EFFECT_WEAPON_PERCENT_DAMAGE, SPELL_EFFECT_WEAPON_DAMAGE, SPELL_EFFECT_NORMALIZED_WEAPON_DMG
        public uint PowerDisplayID;
        public uint AltPowerBarID;
        public uint SpellID;
    }

    public sealed class SpellPowerDifficultyRecord
    {
        public byte DifficultyID;
        public byte OrderIndex;
        public uint Id;
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
        public float Coeff;
        public ushort Param;
        public SpellProcsPerMinuteModType Type;
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
        public float[] RangeMin = new float[2];
        public float[] RangeMax = new float[2];
        public SpellRangeFlag Flags;
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
        public ushort ScalesFromItemLevel;
        public sbyte Class;
        public byte MinScalingLevel;
        public uint MaxScalingLevel;
    }

    public sealed class SpellShapeshiftRecord
    {
        public uint Id;
        public uint SpellID;
        public uint[] ShapeshiftExclude = new uint[2];
        public uint[] ShapeshiftMask = new uint[2];
        public byte StanceBarOrder;
    }

    public sealed class SpellShapeshiftFormRecord
    {
        public uint Id;
        public string Name;
        public float DamageVariance;
        public SpellShapeshiftFormFlags Flags;
        public ushort CombatRoundTime;
        public ushort MountTypeID;
        public sbyte CreatureType;
        public byte BonusActionBar;
        public uint AttackIconFileID;
        public ushort[] CreatureDisplayID = new ushort[4];
        public ushort[] PresetSpellID = new ushort[SpellConst.MaxShapeshift];
    }

    public sealed class SpellTargetRestrictionsRecord
    {
        public uint Id;
        public float ConeDegrees;
        public float Width;
        public uint Targets;
        public ushort TargetCreatureType;
        public byte DifficultyID;
        public byte MaxTargets;
        public uint MaxTargetLevel;
        public uint SpellID;
    }

    public sealed class SpellTotemsRecord
    {
        public uint Id;
        public uint SpellID;
        public uint[] Totem = new uint[SpellConst.MaxTotems];
        public ushort[] RequiredTotemCategoryID = new ushort[SpellConst.MaxTotems];
    }

    public sealed class SpellXSpellVisualRecord
    {
        public uint SpellVisualID;
        public uint Id;
        public float Probability;
        public ushort CasterPlayerConditionID;
        public ushort CasterUnitConditionID;
        public ushort ViewerPlayerConditionID;
        public ushort ViewerUnitConditionID;
        public uint SpellIconFileID;
        public uint ActiveIconFileID;
        public byte Flags;
        public byte DifficultyID;
        public byte Priority;
        public uint SpellID;
    }

    public sealed class SummonPropertiesRecord
    {
        public uint Id;
        public SummonPropFlags Flags;
        public SummonCategory Control;
        public ushort Faction;
        public SummonType Title;
        public sbyte Slot;
    }
}
