// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using System.Numerics;

namespace Game.Spells
{
    public class SpellInfo
    {
        public SpellInfo(SpellNameRecord spellName, Difficulty difficulty, SpellInfoLoadHelper data)
        {
            Id = spellName.Id;
            Difficulty = difficulty;

            foreach (SpellEffectRecord spellEffect in data.Effects)
            {
                if (spellEffect == null)
                    continue;

                _effects.EnsureWritableListIndex((uint)spellEffect.EffectIndex, new SpellEffectInfo(this));
                _effects[spellEffect.EffectIndex] = new SpellEffectInfo(this, spellEffect);
            }

            // Correct EffectIndex for blank effects
            for (int i = 0; i < _effects.Count; ++i)
                _effects[i].EffectIndex = (uint)i;

            SpellName = spellName.Name;

            SpellMiscRecord _misc = data.Misc;
            if (_misc != null)
            {
                Attributes = (SpellAttr0)_misc.Attributes[0];
                AttributesEx = (SpellAttr1)_misc.Attributes[1];
                AttributesEx2 = (SpellAttr2)_misc.Attributes[2];
                AttributesEx3 = (SpellAttr3)_misc.Attributes[3];
                AttributesEx4 = (SpellAttr4)_misc.Attributes[4];
                AttributesEx5 = (SpellAttr5)_misc.Attributes[5];
                AttributesEx6 = (SpellAttr6)_misc.Attributes[6];
                AttributesEx7 = (SpellAttr7)_misc.Attributes[7];
                AttributesEx8 = (SpellAttr8)_misc.Attributes[8];
                AttributesEx9 = (SpellAttr9)_misc.Attributes[9];
                AttributesEx10 = (SpellAttr10)_misc.Attributes[10];
                AttributesEx11 = (SpellAttr11)_misc.Attributes[11];
                AttributesEx12 = (SpellAttr12)_misc.Attributes[12];
                AttributesEx13 = (SpellAttr13)_misc.Attributes[13];
                AttributesEx14 = (SpellAttr14)_misc.Attributes[14];
                AttributesEx15 = (SpellAttr15)_misc.Attributes[15];
                CastTimeEntry = CliDB.SpellCastTimesStorage.LookupByKey(_misc.CastingTimeIndex);
                DurationEntry = CliDB.SpellDurationStorage.LookupByKey(_misc.DurationIndex);
                RangeEntry = CliDB.SpellRangeStorage.LookupByKey(_misc.RangeIndex);
                Speed = _misc.Speed;
                LaunchDelay = _misc.LaunchDelay;
                SchoolMask = (SpellSchoolMask)_misc.SchoolMask;
                IconFileDataId = _misc.SpellIconFileDataID;
                ActiveIconFileDataId = _misc.ActiveIconFileDataID;
                ContentTuningId = _misc.ContentTuningID;
                ShowFutureSpellPlayerConditionID = (uint)_misc.ShowFutureSpellPlayerConditionID;
            }

            // SpellScalingEntry
            SpellScalingRecord _scaling = data.Scaling;
            if (_scaling != null)
            {
                Scaling.MinScalingLevel = _scaling.MinScalingLevel;
                Scaling.MaxScalingLevel = _scaling.MaxScalingLevel;
                Scaling.ScalesFromItemLevel = _scaling.ScalesFromItemLevel;
            }

            // SpellAuraOptionsEntry
            SpellAuraOptionsRecord _options = data.AuraOptions;
            if (_options != null)
            {
                ProcFlags = new ProcFlagsInit(_options.ProcTypeMask);
                ProcChance = _options.ProcChance;
                ProcCharges = (uint)_options.ProcCharges;
                ProcCooldown = _options.ProcCategoryRecovery;
                StackAmount = _options.CumulativeAura;

                SpellProcsPerMinuteRecord _ppm = CliDB.SpellProcsPerMinuteStorage.LookupByKey(_options.SpellProcsPerMinuteID);
                if (_ppm != null)
                {
                    ProcBasePPM = _ppm.BaseProcRate;
                    ProcPPMMods = Global.DB2Mgr.GetSpellProcsPerMinuteMods(_ppm.Id);
                }
            }

            // SpellAuraRestrictionsEntry
            SpellAuraRestrictionsRecord _aura = data.AuraRestrictions;
            if (_aura != null)
            {
                CasterAuraState = (AuraStateType)_aura.CasterAuraState;
                TargetAuraState = (AuraStateType)_aura.TargetAuraState;
                ExcludeCasterAuraState = (AuraStateType)_aura.ExcludeCasterAuraState;
                ExcludeTargetAuraState = (AuraStateType)_aura.ExcludeTargetAuraState;
                CasterAuraSpell = _aura.CasterAuraSpell;
                TargetAuraSpell = _aura.TargetAuraSpell;
                ExcludeCasterAuraSpell = _aura.ExcludeCasterAuraSpell;
                ExcludeTargetAuraSpell = _aura.ExcludeTargetAuraSpell;
                CasterAuraType = (AuraType)_aura.CasterAuraType;
                TargetAuraType = (AuraType)_aura.TargetAuraType;
                ExcludeCasterAuraType = (AuraType)_aura.ExcludeCasterAuraType;
                ExcludeTargetAuraType = (AuraType)_aura.ExcludeTargetAuraType;
            }

            RequiredAreasID = -1;
            // SpellCastingRequirementsEntry
            SpellCastingRequirementsRecord _castreq = data.CastingRequirements;
            if (_castreq != null)
            {
                RequiresSpellFocus = _castreq.RequiresSpellFocus;
                FacingCasterFlags = _castreq.FacingCasterFlags;
                RequiredAreasID = _castreq.RequiredAreasID;
            }

            // SpellCategoriesEntry
            SpellCategoriesRecord _categorie = data.Categories;
            if (_categorie != null)
            {
                CategoryId = _categorie.Category;
                Dispel = (DispelType)_categorie.DispelType;
                Mechanic = (Mechanics)_categorie.Mechanic;
                StartRecoveryCategory = _categorie.StartRecoveryCategory;
                DmgClass = (SpellDmgClass)_categorie.DefenseType;
                PreventionType = (SpellPreventionType)_categorie.PreventionType;
                ChargeCategoryId = _categorie.ChargeCategory;
            }

            // SpellClassOptionsEntry
            SpellFamilyFlags = new FlagArray128();
            SpellClassOptionsRecord _class = data.ClassOptions;
            if (_class != null)
            {
                SpellFamilyName = (SpellFamilyNames)_class.SpellClassSet;
                SpellFamilyFlags = _class.SpellClassMask;
            }

            // SpellCooldownsEntry
            SpellCooldownsRecord _cooldowns = data.Cooldowns;
            if (_cooldowns != null)
            {
                RecoveryTime = _cooldowns.RecoveryTime;
                CategoryRecoveryTime = _cooldowns.CategoryRecoveryTime;
                StartRecoveryTime = _cooldowns.StartRecoveryTime;
                CooldownAuraSpellId = _cooldowns.AuraSpellID;
            }

            EquippedItemClass = ItemClass.None;
            EquippedItemSubClassMask = 0;
            EquippedItemInventoryTypeMask = 0;

            // SpellEmpowerStageEntry
            foreach (var stage in data.EmpowerStages)
                EmpowerStageThresholds.Add(TimeSpan.FromMilliseconds(stage.DurationMs));

            // SpellEquippedItemsEntry
            SpellEquippedItemsRecord _equipped = data.EquippedItems;
            if (_equipped != null)
            {
                EquippedItemClass = (ItemClass)_equipped.EquippedItemClass;
                EquippedItemSubClassMask = _equipped.EquippedItemSubclass;
                EquippedItemInventoryTypeMask = _equipped.EquippedItemInvTypes;
            }

            // SpellInterruptsEntry
            SpellInterruptsRecord _interrupt = data.Interrupts;
            if (_interrupt != null)
            {
                InterruptFlags = (SpellInterruptFlags)_interrupt.InterruptFlags;
                AuraInterruptFlags = (SpellAuraInterruptFlags)_interrupt.AuraInterruptFlags[0];
                AuraInterruptFlags2 = (SpellAuraInterruptFlags2)_interrupt.AuraInterruptFlags[1];
                ChannelInterruptFlags = (SpellAuraInterruptFlags)_interrupt.ChannelInterruptFlags[0];
                ChannelInterruptFlags2 = (SpellAuraInterruptFlags2)_interrupt.ChannelInterruptFlags[1];
            }

            foreach (var label in data.Labels)
                Labels.Add(label.LabelID);

            // SpellLevelsEntry
            SpellLevelsRecord _levels = data.Levels;
            if (_levels != null)
            {
                MaxLevel = _levels.MaxLevel;
                BaseLevel = _levels.BaseLevel;
                SpellLevel = _levels.SpellLevel;
            }

            // SpellPowerEntry
            PowerCosts = data.Powers;

            // SpellReagentsEntry
            SpellReagentsRecord _reagents = data.Reagents;
            if (_reagents != null)
            {
                for (var i = 0; i < SpellConst.MaxReagents; ++i)
                {
                    Reagent[i] = _reagents.Reagent[i];
                    ReagentCount[i] = _reagents.ReagentCount[i];
                }
            }

            ReagentsCurrency = data.ReagentsCurrency;

            // SpellShapeshiftEntry
            SpellShapeshiftRecord _shapeshift = data.Shapeshift;
            if (_shapeshift != null)
            {
                Stances = MathFunctions.MakePair64(_shapeshift.ShapeshiftMask[0], _shapeshift.ShapeshiftMask[1]);
                StancesNot = MathFunctions.MakePair64(_shapeshift.ShapeshiftExclude[0], _shapeshift.ShapeshiftExclude[1]);
            }

            // SpellTargetRestrictionsEntry
            SpellTargetRestrictionsRecord _target = data.TargetRestrictions;
            if (_target != null)
            {
                Targets = (SpellCastTargetFlags)_target.Targets;
                ConeAngle = _target.ConeDegrees;
                Width = _target.Width;
                TargetCreatureType = _target.TargetCreatureType;
                MaxAffectedTargets = _target.MaxTargets;
                MaxTargetLevel = _target.MaxTargetLevel;
            }

            // SpellTotemsEntry
            SpellTotemsRecord _totem = data.Totems;
            if (_totem != null)
            {
                for (var i = 0; i < 2; ++i)
                {
                    TotemCategory[i] = _totem.RequiredTotemCategoryID[i];
                    Totem[i] = _totem.Totem[i];
                }
            }

            _visuals = data.Visuals;

            _spellSpecific = SpellSpecificType.Normal;
            _auraState = AuraStateType.None;
        }

        public SpellInfo(SpellNameRecord spellName, Difficulty difficulty, List<SpellEffectRecord> effects)
        {
            Id = spellName.Id;
            Difficulty = difficulty;
            SpellName = spellName.Name;

            foreach (SpellEffectRecord spellEffect in effects)
            {
                _effects.EnsureWritableListIndex((uint)spellEffect.EffectIndex, new SpellEffectInfo(this));
                _effects[(int)spellEffect.EffectIndex] = new SpellEffectInfo(this, spellEffect);
            }

            // Correct EffectIndex for blank effects
            for (int i = 0; i < _effects.Count; ++i)
                _effects[i].EffectIndex = (uint)i;
        }

        public bool HasEffect(SpellEffectName effect)
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.IsEffect(effect))
                    return true;

            return false;
        }

        public bool HasAura(AuraType aura)
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.IsAura(aura))
                    return true;

            return false;
        }

        public bool HasAreaAuraEffect()
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.IsAreaAuraEffect())
                    return true;

            return false;
        }

        public bool HasOnlyDamageEffects()
        {
            foreach (var effectInfo in _effects)
            {
                switch (effectInfo.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                    case SpellEffectName.NormalizedWeaponDmg:
                    case SpellEffectName.WeaponPercentDamage:
                    case SpellEffectName.SchoolDamage:
                    case SpellEffectName.EnvironmentalDamage:
                    case SpellEffectName.HealthLeech:
                    case SpellEffectName.DamageFromMaxHealthPCT:
                        continue;
                    default:
                        return false;
                }
            }

            return true;
        }

        public bool IsExplicitDiscovery()
        {
            if (GetEffects().Count < 2)
                return false;

            return ((GetEffect(0).Effect == SpellEffectName.CreateRandomItem
                || GetEffect(0).Effect == SpellEffectName.CreateLoot)
                && GetEffect(1).Effect == SpellEffectName.ScriptEffect)
                || Id == 64323;
        }

        public bool IsLootCrafting()
        {
            return HasEffect(SpellEffectName.CreateRandomItem) || HasEffect(SpellEffectName.CreateLoot);
        }

        public bool IsProfession()
        {
            foreach (var effectInfo in _effects)
            {
                if (effectInfo.IsEffect(SpellEffectName.Skill))
                {
                    uint skill = (uint)effectInfo.MiscValue;

                    if (Global.SpellMgr.IsProfessionSkill(skill))
                        return true;
                }
            }
            return false;
        }

        public bool IsPrimaryProfession()
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.IsEffect(SpellEffectName.Skill) && Global.SpellMgr.IsPrimaryProfessionSkill((uint)effectInfo.MiscValue))
                    return true;

            return false;
        }

        public bool IsPrimaryProfessionFirstRank()
        {
            return IsPrimaryProfession() && GetRank() == 1;
        }

        public bool IsAbilityOfSkillType(SkillType skillType)
        {
            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(Id);

            foreach (var spell_idx in bounds)
                if (spell_idx.SkillLine == (uint)skillType)
                    return true;

            return false;
        }

        public bool IsAffectingArea()
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.IsEffect() && (effectInfo.IsTargetingArea() || effectInfo.IsEffect(SpellEffectName.PersistentAreaAura) || effectInfo.IsAreaAuraEffect()))
                    return true;

            return false;
        }

        // checks if spell targets are selected from area, doesn't include spell effects in check (like area wide auras for example)
        public bool IsTargetingArea()
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.IsEffect() && effectInfo.IsTargetingArea())
                    return true;

            return false;
        }

        public bool NeedsExplicitUnitTarget()
        {
            return Convert.ToBoolean(GetExplicitTargetMask() & SpellCastTargetFlags.UnitMask);
        }

        public bool NeedsToBeTriggeredByCaster(SpellInfo triggeringSpell)
        {
            if (NeedsExplicitUnitTarget())
                return true;

            if (triggeringSpell.IsChanneled())
            {
                SpellCastTargetFlags mask = 0;
                foreach (var effectInfo in _effects)
                {
                    if (effectInfo.TargetA.GetTarget() != Framework.Constants.Targets.UnitCaster && effectInfo.TargetA.GetTarget() != Framework.Constants.Targets.DestCaster
                        && effectInfo.TargetB.GetTarget() != Framework.Constants.Targets.UnitCaster && effectInfo.TargetB.GetTarget() != Framework.Constants.Targets.DestCaster)
                    {
                        mask |= effectInfo.GetProvidedTargetMask();
                    }
                }

                if (mask.HasAnyFlag(SpellCastTargetFlags.UnitMask))
                    return true;
            }

            return false;
        }

        public bool IsPassive()
        {
            return HasAttribute(SpellAttr0.Passive);
        }

        public bool IsAutocastable()
        {
            if (IsPassive())
                return false;
            if (HasAttribute(SpellAttr1.NoAutocastAi))
                return false;
            return true;
        }

        public bool IsAutocastEnabledByDefault()
        {
            return !HasAttribute(SpellAttr9.AutocastOffByDefault);
        }

        public bool IsStackableWithRanks()
        {
            if (IsPassive())
                return false;

            // All stance spells. if any better way, change it.
            foreach (var effectInfo in _effects)
            {
                switch (SpellFamilyName)
                {
                    case SpellFamilyNames.Paladin:
                        // Paladin aura Spell
                        if (effectInfo.Effect == SpellEffectName.ApplyAreaAuraRaid)
                            return false;
                        break;
                    case SpellFamilyNames.Druid:
                        // Druid form Spell
                        if (effectInfo.Effect == SpellEffectName.ApplyAura &&
                            effectInfo.ApplyAuraName == AuraType.ModShapeshift)
                            return false;
                        break;
                }
            }
            return true;
        }

        public bool IsPassiveStackableWithRanks()
        {
            return IsPassive() && !HasEffect(SpellEffectName.ApplyAura);
        }

        public bool IsMultiSlotAura()
        {
            return IsPassive() || Id == 55849 || Id == 40075 || Id == 44413; // Power Spark, Fel Flak Fire, Incanter's Absorption
        }

        public bool IsStackableOnOneSlotWithDifferentCasters()
        {
            // TODO: Re-verify meaning of SPELL_ATTR3_STACK_FOR_DIFF_CASTERS and update conditions here
            return StackAmount > 1 && !IsChanneled() && !HasAttribute(SpellAttr3.DotStackingRule);
        }

        public bool IsCooldownStartedOnEvent()
        {
            if (HasAttribute(SpellAttr0.CooldownOnEvent))
                return true;

            SpellCategoryRecord category = CliDB.SpellCategoryStorage.LookupByKey(CategoryId);
            return category != null && category.HasFlag(SpellCategoryFlags.CooldownStartsOnEvent);
        }

        public bool IsDeathPersistent()
        {
            return HasAttribute(SpellAttr3.AllowAuraWhileDead);
        }

        public bool IsRequiringDeadTarget()
        {
            return HasAttribute(SpellAttr3.OnlyOnGhosts);
        }

        public bool IsAllowingDeadTarget()
        {
            if (HasAttribute(SpellAttr2.AllowDeadTarget) || Targets.HasAnyFlag(SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitDead))
                return true;

            foreach (var effectInfo in _effects)
                if (effectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.Corpse || effectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.Corpse)
                    return true;

            return false;
        }

        public bool IsGroupBuff()
        {
            foreach (var effectInfo in _effects)
            {
                switch (effectInfo.TargetA.GetCheckType())
                {
                    case SpellTargetCheckTypes.Party:
                    case SpellTargetCheckTypes.Raid:
                    case SpellTargetCheckTypes.RaidClass:
                        return true;
                    default:
                        break;
                }
            }

            return false;
        }

        public bool CanBeUsedInCombat(Unit caster)
        {
            return !HasAttribute(SpellAttr0.NotInCombatOnlyPeaceful)
                || (caster.HasAuraType(AuraType.AllowMountInCombat) && HasAura(AuraType.Mounted));
        }

        public bool IsPositive()
        {
            for (var index = 0; index < NegativeEffects.Length; ++index)
                if (NegativeEffects.Get(index))
                    return false;

            return true;
        }

        public bool IsPositiveEffect(uint effIndex)
        {
            return !NegativeEffects.Get((int)effIndex);
        }

        public bool IsChanneled()
        {
            return HasAttribute(SpellAttr1.IsChannelled | SpellAttr1.IsSelfChannelled);
        }

        public bool IsMoveAllowedChannel()
        {
            return IsChanneled() && !ChannelInterruptFlags.HasFlag(SpellAuraInterruptFlags.Moving | SpellAuraInterruptFlags.Turning);
        }

        public bool IsNextMeleeSwingSpell()
        {
            return HasAttribute(SpellAttr0.OnNextSwingNoDamage | SpellAttr0.OnNextSwing);
        }

        public bool IsRangedWeaponSpell()
        {
            return (SpellFamilyName == SpellFamilyNames.Hunter && !SpellFamilyFlags[1].HasAnyFlag(0x10000000u)) // for 53352, cannot find better way
                || Convert.ToBoolean(EquippedItemSubClassMask & (int)ItemSubClassWeapon.MaskRanged)
                || Attributes.HasAnyFlag(SpellAttr0.UsesRangedSlot);
        }

        public bool IsAutoRepeatRangedSpell()
        {
            return HasAttribute(SpellAttr2.AutoRepeat);
        }

        public bool IsEmpowerSpell()
        {
            return !EmpowerStageThresholds.Empty();
        }

        public bool HasInitialAggro()
        {
            return !(HasAttribute(SpellAttr1.NoThreat) || HasAttribute(SpellAttr2.NoInitialThreat) || HasAttribute(SpellAttr4.NoHarmfulThreat));
        }

        public bool HasHitDelay()
        {
            return Speed > 0.0f || LaunchDelay > 0.0f;
        }

        public WeaponAttackType GetAttackType()
        {
            WeaponAttackType result;
            switch (DmgClass)
            {
                case SpellDmgClass.Melee:
                    if (HasAttribute(SpellAttr3.RequiresOffHandWeapon))
                        result = WeaponAttackType.OffAttack;
                    else
                        result = WeaponAttackType.BaseAttack;
                    break;
                case SpellDmgClass.Ranged:
                    result = IsRangedWeaponSpell() ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack;
                    break;
                default:
                    // Wands
                    if (IsAutoRepeatRangedSpell())
                        result = WeaponAttackType.RangedAttack;
                    else
                        result = WeaponAttackType.BaseAttack;
                    break;
            }

            return result;
        }

        public bool IsItemFitToSpellRequirements(Item item)
        {
            // item neutral spell
            if (EquippedItemClass == ItemClass.None)
                return true;

            // item dependent spell
            if (item != null && item.IsFitToSpellRequirements(this))
                return true;

            return false;
        }

        public bool IsAffected(SpellFamilyNames familyName, FlagArray128 familyFlags)
        {
            if (familyName == 0)
                return true;

            if (familyName != SpellFamilyName)
                return false;

            if (familyFlags && !(familyFlags & SpellFamilyFlags))
                return false;

            return true;
        }

        public bool IsAffectedBySpellMods()
        {
            return !HasAttribute(SpellAttr3.IgnoreCasterModifiers);
        }

        public int IsAffectedBySpellMod(SpellModifier mod)
        {
            SpellInfo affectSpell = Global.SpellMgr.GetSpellInfo(mod.spellId, Difficulty);
            if (affectSpell == null)
                return 0;

            switch (mod.type)
            {
                case SpellModType.Flat:
                case SpellModType.Pct:
                {
                    // TEMP: dont use IsAffected - !familyName and !familyFlags are not valid options for spell mods
                    // TODO: investigate if the !familyName and !familyFlags conditions are even valid for all other (nonmod) uses of SpellInfo::IsAffected
                    if (affectSpell.SpellFamilyName != SpellFamilyName)
                        return 0;

                    // spell modifiers should apply as many times as number of matched SpellFamilyFlags bits (verified with spell 1218116 with modifier 384451 in patch 11.1.0 and client tooltip code since at least 3.3.5)
                    // unknown if this is a bug or strange design choice...
                    var matched = (mod as SpellModifierByClassMask).mask & SpellFamilyFlags;
                    return BitOperations.PopCount(matched[0]) + BitOperations.PopCount(matched[1]) + BitOperations.PopCount(matched[2]) + BitOperations.PopCount(matched[3]);
                }
                case SpellModType.LabelFlat:
                    return HasLabel((uint)(mod as SpellFlatModifierByLabel).value.LabelID) ? 1 : 0;
                case SpellModType.LabelPct:
                    return HasLabel((uint)(mod as SpellPctModifierByLabel).value.LabelID) ? 1 : 0;
                default:
                    break;
            }

            return 0;
        }

        public bool IsUpdatingTemporaryAuraValuesBySpellMod()
        {
            switch (Id)
            {
                case 384669:    // Overflowing Maelstrom
                    return true;
                default:
                    break;
            }

            return false;
        }

        public bool CanPierceImmuneAura(SpellInfo auraSpellInfo)
        {
            // Dispels other auras on immunity, check if this spell makes the unit immune to aura
            if (HasAttribute(SpellAttr1.ImmunityPurgesEffect) && CanSpellProvideImmunityAgainstAura(auraSpellInfo))
                return true;

            return false;
        }

        public bool CanDispelAura(SpellInfo auraSpellInfo)
        {
            // These auras (like Divine Shield) can't be dispelled
            if (auraSpellInfo.HasAttribute(SpellAttr0.NoImmunities))
                return false;

            return true;
        }

        public bool IsSingleTarget()
        {
            // all other single target spells have if it has AttributesEx5
            if (HasAttribute(SpellAttr5.LimitN))
                return true;

            return false;
        }

        public bool IsAuraExclusiveBySpecificWith(SpellInfo spellInfo)
        {
            SpellSpecificType spellSpec1 = GetSpellSpecific();
            SpellSpecificType spellSpec2 = spellInfo.GetSpellSpecific();
            switch (spellSpec1)
            {
                case SpellSpecificType.WarlockArmor:
                case SpellSpecificType.MageArmor:
                case SpellSpecificType.ElementalShield:
                case SpellSpecificType.MagePolymorph:
                case SpellSpecificType.Presence:
                case SpellSpecificType.Charm:
                case SpellSpecificType.Scroll:
                case SpellSpecificType.WarriorEnrage:
                case SpellSpecificType.MageArcaneBrillance:
                case SpellSpecificType.PriestDivineSpirit:
                    return spellSpec1 == spellSpec2;
                case SpellSpecificType.Food:
                    return spellSpec2 == SpellSpecificType.Food
                        || spellSpec2 == SpellSpecificType.FoodAndDrink;
                case SpellSpecificType.Drink:
                    return spellSpec2 == SpellSpecificType.Drink
                        || spellSpec2 == SpellSpecificType.FoodAndDrink;
                case SpellSpecificType.FoodAndDrink:
                    return spellSpec2 == SpellSpecificType.Food
                        || spellSpec2 == SpellSpecificType.Drink
                        || spellSpec2 == SpellSpecificType.FoodAndDrink;
                default:
                    return false;
            }
        }

        public bool IsAuraExclusiveBySpecificPerCasterWith(SpellInfo spellInfo)
        {
            SpellSpecificType spellSpec = GetSpellSpecific();
            switch (spellSpec)
            {
                case SpellSpecificType.Seal:
                case SpellSpecificType.Hand:
                case SpellSpecificType.Aura:
                case SpellSpecificType.Sting:
                case SpellSpecificType.Curse:
                case SpellSpecificType.Bane:
                case SpellSpecificType.Aspect:
                    return spellSpec == spellInfo.GetSpellSpecific();
                default:
                    return false;
            }
        }

        public SpellCastResult CheckShapeshift(ShapeShiftForm form)
        {
            // talents that learn spells can have stance requirements that need ignore
            // (this requirement only for client-side stance show in talent description)
            /* TODO: 6.x fix this in proper way (probably spell flags/attributes?)
            if (CliDB.GetTalentSpellCost(Id) > 0 && HasEffect(SpellEffects.LearnSpell))
            return SpellCastResult.SpellCastOk;
            */

            //if (HasAttribute(SPELL_ATTR13_ACTIVATES_REQUIRED_SHAPESHIFT))
            //    return SPELL_CAST_OK;

            ulong stanceMask = (form != 0 ? 1ul << ((int)form - 1) : 0);

            if (Convert.ToBoolean(stanceMask & StancesNot)) // can explicitly not be casted in this stance
                return SpellCastResult.NotShapeshift;

            if (Convert.ToBoolean(stanceMask & Stances))    // can explicitly be casted in this stance
                return SpellCastResult.SpellCastOk;

            bool actAsShifted = false;
            SpellShapeshiftFormRecord shapeInfo = null;
            if (form > 0)
            {
                shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(form);
                if (shapeInfo == null)
                {
                    Log.outError(LogFilter.Spells, "GetErrorAtShapeshiftedCast: unknown shapeshift {0}", form);
                    return SpellCastResult.SpellCastOk;
                }
                actAsShifted = !shapeInfo.HasFlag(SpellShapeshiftFormFlags.Stance);
            }

            if (actAsShifted)
            {
                if (HasAttribute(SpellAttr0.NotShapeshifted) || (shapeInfo != null && shapeInfo.HasFlag(SpellShapeshiftFormFlags.CanOnlyCastShapeshiftSpells))) // not while shapeshifted
                    return SpellCastResult.NotShapeshift;
                else if (Stances != 0)   // needs other shapeshift
                    return SpellCastResult.OnlyShapeshift;
            }
            else
            {
                // needs shapeshift
                if (!HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm) && Stances != 0)
                    return SpellCastResult.OnlyShapeshift;
            }

            return SpellCastResult.SpellCastOk;
        }

        public SpellCastResult CheckLocation(uint map_id, uint zone_id, uint area_id, Player player)
        {
            // normal case
            if (RequiredAreasID > 0)
            {
                bool found = false;
                List<uint> areaGroupMembers = Global.DB2Mgr.GetAreasForGroup((uint)RequiredAreasID);
                foreach (uint areaId in areaGroupMembers)
                {
                    if (Global.DB2Mgr.IsInArea(area_id, areaId))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return SpellCastResult.IncorrectArea;
            }

            // continent limitation (virtual continent)
            if (HasAttribute(SpellAttr4.OnlyFlyingAreas))
            {
                AreaMountFlags mountFlags = 0;
                if (player != null && player.HasAuraType(AuraType.MountRestrictions))
                {
                    foreach (AuraEffect auraEffect in player.GetAuraEffectsByType(AuraType.MountRestrictions))
                        mountFlags |= (AreaMountFlags)auraEffect.GetMiscValue();
                }
                else
                {
                    AreaTableRecord areaTable = CliDB.AreaTableStorage.LookupByKey(area_id);
                    if (areaTable != null)
                        mountFlags = (AreaMountFlags)areaTable.MountFlags;
                }
                if (!mountFlags.HasFlag(AreaMountFlags.AllowFlyingMounts))
                    return SpellCastResult.IncorrectArea;

                if (player != null && !ConditionManager.IsPlayerMeetingCondition(player, 72968)) // Hardcoded PlayerCondition id for attribute check in client
                    return SpellCastResult.IncorrectArea;
            }

            var mapEntry = CliDB.MapStorage.LookupByKey(map_id);

            // raid instance limitation
            if (HasAttribute(SpellAttr6.NotInRaidInstances))
            {
                if (mapEntry == null || mapEntry.IsRaid())
                    return SpellCastResult.NotInRaidInstance;
            }

            if (HasAttribute(SpellAttr8.RemoveOutsideDungeonsAndRaids))
            {
                if (mapEntry == null || !mapEntry.IsDungeon())
                    return SpellCastResult.TargetNotInInstance;
            }

            if (HasAttribute(SpellAttr8.NotInBattleground))
            {
                if (mapEntry == null || mapEntry.IsBattleground())
                    return SpellCastResult.NotInBattleground;
            }

            // DB base check (if non empty then must fit at least single for allow)
            var saBounds = Global.SpellMgr.GetSpellAreaMapBounds(Id);
            if (!saBounds.Empty())
            {
                foreach (var bound in saBounds)
                {
                    if (bound.IsFitToRequirements(player, zone_id, area_id))
                        return SpellCastResult.SpellCastOk;
                }
                return SpellCastResult.IncorrectArea;
            }

            // bg spell checks
            switch (Id)
            {
                case 23333:         // Warsong Flag
                case 23335:         // Silverwing Flag
                    return map_id == 489 && player != null && player.InBattleground() ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                case 2584:          // Waiting to Resurrect
                case 42792:         // Recently Dropped Flag
                case 43681:         // Inactive
                case 44535:         // Spirit Heal (mana)
                    if (mapEntry == null)
                        return SpellCastResult.IncorrectArea;

                    return zone_id == (uint)AreaId.Wintergrasp || (mapEntry.IsBattleground() && player != null && player.InBattleground()) ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                case 44521:         // Preparation
                {
                    if (player == null)
                        return SpellCastResult.RequiresArea;

                    if (mapEntry == null)
                        return SpellCastResult.IncorrectArea;

                    if (!mapEntry.IsBattleground())
                        return SpellCastResult.RequiresArea;

                    Battleground bg = player.GetBattleground();
                    return bg != null && bg.GetStatus() == BattlegroundStatus.WaitJoin ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                }
                case 32724:         // Gold Team (Alliance)
                case 32725:         // Green Team (Alliance)
                case 35774:         // Gold Team (Horde)
                case 35775:         // Green Team (Horde)
                    if (mapEntry == null)
                        return SpellCastResult.IncorrectArea;

                    return mapEntry.IsBattleArena() && player != null && player.InBattleground() ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                case 32727:        // Arena Preparation
                {
                    if (player == null)
                        return SpellCastResult.RequiresArea;

                    if (mapEntry == null)
                        return SpellCastResult.IncorrectArea;

                    if (!mapEntry.IsBattleArena())
                        return SpellCastResult.RequiresArea;

                    Battleground bg = player.GetBattleground();
                    return bg != null && bg.GetStatus() == BattlegroundStatus.WaitJoin ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                }
            }

            // aura limitations
            if (player != null)
            {
                foreach (var effectInfo in _effects)
                {
                    if (!effectInfo.IsAura())
                        continue;

                    switch (effectInfo.ApplyAuraName)
                    {
                        case AuraType.ModShapeshift:
                        {
                            SpellShapeshiftFormRecord spellShapeshiftForm = CliDB.SpellShapeshiftFormStorage.LookupByKey(effectInfo.MiscValue);
                            if (spellShapeshiftForm != null)
                            {
                                uint mountType = spellShapeshiftForm.MountTypeID;
                                if (mountType != 0)
                                    if (player.GetMountCapability(mountType) == null)
                                        return SpellCastResult.NotHere;
                            }
                            break;
                        }
                        case AuraType.Mounted:
                        {
                            uint mountType = (uint)effectInfo.MiscValueB;
                            MountRecord mountEntry = Global.DB2Mgr.GetMount(Id);
                            if (mountEntry != null)
                                mountType = mountEntry.MountTypeID;
                            if (mountType != 0 && player.GetMountCapability(mountType) == null)
                                return SpellCastResult.NotHere;
                            break;
                        }
                    }
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        public SpellCastResult CheckTarget(WorldObject caster, WorldObject target, bool Implicit = true)
        {
            if (HasAttribute(SpellAttr1.ExcludeCaster) && caster == target)
                return SpellCastResult.BadTargets;

            // check visibility - ignore invisibility/stealth for implicit (area) targets
            if (!HasAttribute(SpellAttr6.IgnorePhaseShift) && !caster.CanSeeOrDetect(target, Implicit))
                return SpellCastResult.BadTargets;

            Unit unitTarget = target.ToUnit();

            if (HasAttribute(SpellAttr8.OnlyTargetIfSameCreator))
            {
                var getCreatorOrSelf = (WorldObject obj) =>
                {
                    ObjectGuid creator = obj.GetCreatorGUID();
                    if (creator.IsEmpty())
                        creator = obj.GetGUID();

                    return creator;
                };

                if (getCreatorOrSelf(caster) != getCreatorOrSelf(target))
                    return SpellCastResult.BadTargets;
            }

            // creature/player specific target checks
            if (unitTarget != null)
            {
                // spells cannot be cast if target has a pet in combat either
                if (HasAttribute(SpellAttr1.OnlyPeacefulTargets) && (unitTarget.IsInCombat() || unitTarget.HasUnitFlag(UnitFlags.PetInCombat)))
                    return SpellCastResult.TargetAffectingCombat;

                // only spells with SPELL_ATTR3_ONLY_TARGET_GHOSTS can target ghosts
                if (HasAttribute(SpellAttr3.OnlyOnGhosts) != unitTarget.HasAuraType(AuraType.Ghost))
                {
                    if (HasAttribute(SpellAttr3.OnlyOnGhosts))
                        return SpellCastResult.TargetNotGhost;
                    else
                        return SpellCastResult.BadTargets;
                }

                if (caster != unitTarget)
                {
                    if (caster.IsTypeId(TypeId.Player))
                    {
                        // Do not allow these spells to target creatures not tapped by us (Banish, Polymorph, many quest spells)
                        if (HasAttribute(SpellAttr2.CannotCastOnTapped))
                        {
                            Creature targetCreature = unitTarget.ToCreature();
                            if (targetCreature != null)
                                if (targetCreature.HasLootRecipient() && !targetCreature.IsTappedBy(caster.ToPlayer()))
                                    return SpellCastResult.CantCastOnTapped;
                        }

                        if (HasAttribute(SpellCustomAttributes.PickPocket))
                        {
                            Creature targetCreature = unitTarget.ToCreature();
                            if (targetCreature == null)
                                return SpellCastResult.BadTargets;

                            if (!targetCreature.CanHaveLoot() || !Loots.LootStorage.Pickpocketing.HaveLootFor(targetCreature.GetCreatureDifficulty().PickPocketLootID))
                                return SpellCastResult.TargetNoPockets;
                        }

                        // Not allow disarm unarmed player
                        if (Mechanic == Mechanics.Disarm)
                        {
                            if (unitTarget.IsTypeId(TypeId.Player))
                            {
                                Player player = unitTarget.ToPlayer();
                                if (player.GetWeaponForAttack(WeaponAttackType.BaseAttack) == null || !player.IsUseEquipedWeapon(true))
                                    return SpellCastResult.TargetNoWeapons;
                            }
                            else if (unitTarget.GetVirtualItemId(0) == 0)
                                return SpellCastResult.TargetNoWeapons;
                        }
                    }
                }

                if (HasAttribute(SpellAttr8.OnlyTargetOwnSummons))
                    if (!unitTarget.IsSummon() || unitTarget.ToTempSummon().GetSummonerGUID() != caster.GetGUID())
                        return SpellCastResult.BadTargets;

                if (HasAttribute(SpellAttr3.NotOnAoeImmune))
                    if (unitTarget.GetSpellOtherImmunityMask().HasFlag(SpellOtherImmunity.AoETarget))
                        return SpellCastResult.BadTargets;

                if (HasAttribute(SpellAttr9.TargetMustBeGrounded) &&
                    (unitTarget.HasUnitMovementFlag(MovementFlag.Falling | MovementFlag.Swimming | MovementFlag.Flying | MovementFlag.Hover) ||
                    unitTarget.HasExtraUnitMovementFlag2(MovementFlags3.AdvFlying)))
                    return SpellCastResult.TargetNotGrounded;
            }
            // corpse specific target checks
            else if (target.IsTypeId(TypeId.Corpse))
            {
                Corpse corpseTarget = target.ToCorpse();
                // cannot target bare bones
                if (corpseTarget.GetCorpseType() == CorpseType.Bones)
                    return SpellCastResult.BadTargets;
                // we have to use owner for some checks (aura preventing resurrection for example)
                Player owner = Global.ObjAccessor.FindPlayer(corpseTarget.GetOwnerGUID());
                if (owner != null)
                    unitTarget = owner;
                // we're not interested in corpses without owner
                else
                    return SpellCastResult.BadTargets;
            }
            // other types of objects - always valid
            else
                return SpellCastResult.SpellCastOk;

            // corpseOwner and unit specific target checks
            if (!unitTarget.IsPlayer())
            {
                if (HasAttribute(SpellAttr3.OnlyOnPlayer))
                    return SpellCastResult.TargetNotPlayer;

                if (HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && unitTarget.IsControlledByPlayer())
                    return SpellCastResult.TargetIsPlayerControlled;
            }
            else if (HasAttribute(SpellAttr5.NotOnPlayer))
                return SpellCastResult.TargetIsPlayer;

            if (!IsAllowingDeadTarget() && !unitTarget.IsAlive())
                return SpellCastResult.TargetsDead;

            // check this flag only for implicit targets (chain and area), allow to explicitly target units for spells like Shield of Righteousness
            if (Implicit && HasAttribute(SpellAttr6.DoNotChainToCrowdControlledTargets) && !unitTarget.CanFreeMove())
                return SpellCastResult.BadTargets;

            if (!CheckTargetCreatureType(unitTarget))
            {
                if (target.IsTypeId(TypeId.Player))
                    return SpellCastResult.TargetIsPlayer;
                else
                    return SpellCastResult.BadTargets;
            }

            // check GM mode and GM invisibility - only for player casts (npc casts are controlled by AI) and negative spells
            if (unitTarget != caster && (caster.GetAffectingPlayer() != null || !IsPositive()) && unitTarget.IsTypeId(TypeId.Player))
            {
                if (!unitTarget.ToPlayer().IsVisible())
                    return SpellCastResult.BmOrInvisgod;

                if (unitTarget.ToPlayer().IsGameMaster())
                    return SpellCastResult.BmOrInvisgod;
            }

            // not allow casting on flying player
            if (unitTarget.HasUnitState(UnitState.InFlight) && !HasAttribute(SpellCustomAttributes.AllowInflightTarget))
                return SpellCastResult.BadTargets;

            /* TARGET_UNIT_MASTER gets blocked here for passengers, because the whole idea of this check is to
            not allow passengers to be implicitly hit by spells, however this target type should be an exception,
            if this is left it kills spells that award kill credit from vehicle to master (few spells),
            the use of these 2 covers passenger target check, logically, if vehicle cast this to master it should always hit
            him, because it would be it's passenger, there's no such case where this gets to fail legitimacy, this problem
            cannot be solved from within the check in other way since target type cannot be called for the spell currently
            Spell examples: [ID - 52864 Devour Water, ID - 52862 Devour Wind, ID - 49370 Wyrmrest Defender: Destabilize Azure Dragonshrine Effect] */
            Unit unitCaster = caster.ToUnit();
            if (unitCaster != null)
            {
                if (!unitCaster.IsVehicle() && unitCaster.GetCharmerOrOwner() != target)
                {
                    if (TargetAuraState != 0 && !unitTarget.HasAuraState(TargetAuraState, this, unitCaster))
                        return SpellCastResult.TargetAurastate;

                    if (ExcludeTargetAuraState != 0 && unitTarget.HasAuraState(ExcludeTargetAuraState, this, unitCaster))
                        return SpellCastResult.TargetAurastate;
                }
            }

            if (TargetAuraSpell != 0 && !unitTarget.HasAura(TargetAuraSpell))
                return SpellCastResult.TargetAurastate;

            if (ExcludeTargetAuraSpell != 0 && unitTarget.HasAura(ExcludeTargetAuraSpell))
                return SpellCastResult.TargetAurastate;

            if (TargetAuraType != 0 && !unitTarget.HasAuraType(TargetAuraType))
                return SpellCastResult.TargetAurastate;

            if (ExcludeTargetAuraType != 0 && unitTarget.HasAuraType(ExcludeTargetAuraType))
                return SpellCastResult.TargetAurastate;

            if (unitTarget.HasAuraType(AuraType.PreventResurrection) && !HasAttribute(SpellAttr7.BypassNoResurrectAura))
                if (HasEffect(SpellEffectName.SelfResurrect) || HasEffect(SpellEffectName.Resurrect))
                    return SpellCastResult.TargetCannotBeResurrected;

            if (HasAttribute(SpellAttr8.EnforceInCombatRessurectionLimit))
            {
                Map map = caster.GetMap();
                if (map != null)
                {
                    InstanceMap iMap = map.ToInstanceMap();
                    if (iMap != null)
                    {
                        InstanceScript instance = iMap.GetInstanceScript();
                        if (instance != null)
                            if (instance.GetCombatResurrectionCharges() == 0 && instance.IsEncounterInProgress())
                                return SpellCastResult.TargetCannotBeResurrected;
                    }
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        public SpellCastResult CheckExplicitTarget(WorldObject caster, WorldObject target, Item itemTarget = null)
        {
            SpellCastTargetFlags neededTargets = (SpellCastTargetFlags)RequiredExplicitTargetMask;
            if (target == null)
            {
                if (Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.GameobjectMask | SpellCastTargetFlags.CorpseMask)))
                    if (!Convert.ToBoolean(neededTargets & SpellCastTargetFlags.GameobjectItem) || itemTarget == null)
                        return SpellCastResult.BadTargets;
                return SpellCastResult.SpellCastOk;
            }
            Unit unitTarget = target.ToUnit();
            if (unitTarget != null)
            {
                if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.UnitAlly | SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitMinipet | SpellCastTargetFlags.UnitPassenger))
                {
                    Unit unitCaster = caster.ToUnit();
                    if (neededTargets.HasFlag(SpellCastTargetFlags.UnitEnemy))
                        if (caster.IsValidAttackTarget(unitTarget, this))
                            return SpellCastResult.SpellCastOk;

                    if (neededTargets.HasFlag(SpellCastTargetFlags.UnitAlly)
                        || (neededTargets.HasFlag(SpellCastTargetFlags.UnitParty) && unitCaster != null && unitCaster.IsInPartyWith(unitTarget))
                        || (neededTargets.HasFlag(SpellCastTargetFlags.UnitRaid) && unitCaster != null && unitCaster.IsInRaidWith(unitTarget)))
                        if (caster.IsValidAssistTarget(unitTarget, this))
                            return SpellCastResult.SpellCastOk;

                    if (neededTargets.HasFlag(SpellCastTargetFlags.UnitMinipet) && unitCaster != null)
                        if (unitTarget.GetGUID() == unitCaster.GetCritterGUID())
                            return SpellCastResult.SpellCastOk;

                    if (neededTargets.HasFlag(SpellCastTargetFlags.UnitPassenger) && unitCaster != null)
                        if (unitTarget.IsOnVehicle(unitCaster))
                            return SpellCastResult.SpellCastOk;

                    return SpellCastResult.BadTargets;
                }
            }
            return SpellCastResult.SpellCastOk;
        }

        public SpellCastResult CheckVehicle(Unit caster)
        {
            // All creatures should be able to cast as passengers freely, restriction and attribute are only for players
            if (!caster.IsTypeId(TypeId.Player))
                return SpellCastResult.SpellCastOk;

            Vehicle vehicle = caster.GetVehicle();
            if (vehicle != null)
            {
                VehicleSeatFlags checkMask = 0;
                foreach (var effectInfo in _effects)
                {
                    if (effectInfo.IsAura(AuraType.ModShapeshift))
                    {
                        var shapeShiftFromEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)effectInfo.MiscValue);
                        if (shapeShiftFromEntry != null && !shapeShiftFromEntry.HasFlag(SpellShapeshiftFormFlags.Stance))
                            checkMask |= VehicleSeatFlags.Uncontrolled;
                        break;
                    }
                }

                if (HasAura(AuraType.Mounted))
                    checkMask |= VehicleSeatFlags.CanCastMountSpell;

                if (checkMask == 0)
                    checkMask = VehicleSeatFlags.CanAttack;

                var vehicleSeat = vehicle.GetSeatForPassenger(caster);
                if (!HasAttribute(SpellAttr6.AllowWhileRidingVehicle) && !HasAttribute(SpellAttr0.AllowWhileMounted)
                    && (vehicleSeat.Flags & (int)checkMask) != (int)checkMask)
                    return SpellCastResult.CantDoThatRightNow;

                // Can only summon uncontrolled minions/guardians when on controlled vehicle
                if (vehicleSeat.HasFlag(VehicleSeatFlags.CanControl | VehicleSeatFlags.Unk2))
                {
                    foreach (var effectInfo in _effects)
                    {
                        if (!effectInfo.IsEffect(SpellEffectName.Summon))
                            continue;

                        var props = CliDB.SummonPropertiesStorage.LookupByKey(effectInfo.MiscValueB);
                        if (props != null && props.Control != SummonCategory.Wild)
                            return SpellCastResult.CantDoThatRightNow;
                    }
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        public bool CheckTargetCreatureType(Unit target)
        {
            // Curse of Doom & Exorcism: not find another way to fix spell target check :/
            if (SpellFamilyName == SpellFamilyNames.Warlock && GetCategory() == 1179)
            {
                // not allow cast at player
                if (target.IsTypeId(TypeId.Player))
                    return false;
                else
                    return true;
            }

            // if target is magnet (i.e Grounding Totem) the check is skipped
            if (target.IsMagnet())
                return true;


            uint creatureType = target.GetCreatureTypeMask();
            return TargetCreatureType == 0 || creatureType == 0 || (creatureType & TargetCreatureType) != 0 || target.HasAuraType(AuraType.IgnoreSpellCreatureTypeRequirements);
        }

        public SpellSchoolMask GetSchoolMask()
        {
            return SchoolMask;
        }

        public ulong GetAllEffectsMechanicMask()
        {
            ulong mask = 0;
            if (Mechanic != 0)
                mask |= 1ul << (int)Mechanic;

            foreach (var effectInfo in _effects)
                if (effectInfo.IsEffect() && effectInfo.Mechanic != 0)
                    mask |= 1ul << (int)effectInfo.Mechanic;

            return mask;
        }

        public ulong GetEffectMechanicMask(uint effIndex)
        {
            ulong mask = 0;
            if (Mechanic != 0)
                mask |= 1ul << (int)Mechanic;

            if (GetEffect(effIndex).IsEffect() && GetEffect(effIndex).Mechanic != 0)
                mask |= 1ul << (int)GetEffect(effIndex).Mechanic;

            return mask;
        }

        public ulong GetSpellMechanicMaskByEffectMask(uint effectMask)
        {
            ulong mask = 0;
            if (Mechanic != 0)
                mask |= 1ul << (int)Mechanic;

            foreach (var effectInfo in _effects)
                if ((effectMask & (1 << (int)effectInfo.EffectIndex)) != 0 && effectInfo.Mechanic != 0)
                    mask |= 1ul << (int)effectInfo.Mechanic;

            return mask;
        }

        public Mechanics GetEffectMechanic(uint effIndex)
        {
            if (GetEffect(effIndex).IsEffect() && GetEffect(effIndex).Mechanic != 0)
                return GetEffect(effIndex).Mechanic;

            if (Mechanic != 0)
                return Mechanic;

            return Mechanics.None;
        }

        public uint GetDispelMask()
        {
            return GetDispelMask(Dispel);
        }

        public static uint GetDispelMask(DispelType type)
        {
            // If dispel all
            if (type == DispelType.ALL)
                return (uint)DispelType.AllMask;
            else
                return (uint)(1 << (int)type);
        }

        public SpellCastTargetFlags GetExplicitTargetMask()
        {
            return (SpellCastTargetFlags)ExplicitTargetMask;
        }

        public AuraStateType GetAuraState()
        {
            return _auraState;
        }

        public void _LoadAuraState()
        {
            _auraState = AuraStateType.None;

            // Faerie Fire
            if (GetCategory() == 1133)
                _auraState = AuraStateType.FaerieFire;

            // Swiftmend state on Regrowth, Rejuvenation, Wild Growth
            if (SpellFamilyName == SpellFamilyNames.Druid && (SpellFamilyFlags[0].HasAnyFlag(0x50u) || SpellFamilyFlags[1].HasAnyFlag(0x4000000u)))
                _auraState = AuraStateType.DruidPeriodicHeal;

            // Deadly poison aura state
            if (SpellFamilyName == SpellFamilyNames.Rogue && SpellFamilyFlags[0].HasAnyFlag(0x10000u))
                _auraState = AuraStateType.RoguePoisoned;

            // Enrage aura state
            if (Dispel == DispelType.Enrage)
                _auraState = AuraStateType.Enraged;

            // Bleeding aura state
            if (Convert.ToBoolean(GetAllEffectsMechanicMask() & (1 << (int)Mechanics.Bleed)))
                _auraState = AuraStateType.Bleed;

            if (Convert.ToBoolean(GetSchoolMask() & SpellSchoolMask.Frost))
            {
                foreach (var effectInfo in _effects)
                    if (effectInfo.IsAura(AuraType.ModStun) || effectInfo.IsAura(AuraType.ModRoot) || effectInfo.IsAura(AuraType.ModRoot2))
                        _auraState = AuraStateType.Frozen;
            }

            switch (Id)
            {
                case 1064: // Dazed
                    _auraState = AuraStateType.Dazed;
                    break;
                case 32216: // Victorious
                    _auraState = AuraStateType.Victorious;
                    break;
                case 71465: // Divine Surge
                case 50241: // Evasive Charges
                case 81262: // Efflorescence
                    _auraState = AuraStateType.RaidEncounter;
                    break;
                case 6950:   // Faerie Fire
                case 9806:   // Phantom Strike
                case 9991:   // Touch of Zanzil
                case 13424:  // Faerie Fire
                case 13752:  // Faerie Fire
                case 16432:  // Plague Mist
                case 20656:  // Faerie Fire
                case 25602:  // Faerie Fire
                case 32129:  // Faerie Fire
                case 35325:  // Glowing Blood
                case 35328:  // Lambent Blood
                case 35329:  // Vibrant Blood
                case 35331:  // Black Blood
                case 49163:  // Perpetual Instability
                case 65863:  // Faerie Fire
                case 79559:  // Luxscale Light
                case 82855:  // Dazzling
                case 102953: // In the Rumpus
                case 127907: // Phosphorescence
                case 127913: // Phosphorescence
                case 129007: // Zijin Sting
                case 130159: // Fae Touch
                case 142537: // Spotter Smoke
                case 168455: // Spotted!
                case 176905: // Super Sticky Glitter Bomb
                case 189502: // Marked
                case 201785: // Intruder Alert!
                case 201786: // Intruder Alert!
                case 201935: // Spotted!
                case 239233: // Smoke Bomb
                case 319400: // Glitter Burst
                case 321470: // Dimensional Shifter Mishap
                case 331134: // Spotted
                    _auraState = AuraStateType.FaerieFire;
                    break;
                default:
                    break;
            }

            if (Mechanic == Mechanics.Banish)
                _auraState = AuraStateType.Banished;
        }

        public SpellSpecificType GetSpellSpecific()
        {
            return _spellSpecific;
        }

        public void _LoadSpellSpecific()
        {
            _spellSpecific = SpellSpecificType.Normal;

            switch (SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                {
                    // Food / Drinks (mostly)
                    if (HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
                    {
                        bool food = false;
                        bool drink = false;
                        foreach (var effectInfo in _effects)
                        {
                            if (!effectInfo.IsAura())
                                continue;

                            switch (effectInfo.ApplyAuraName)
                            {
                                // Food
                                case AuraType.ModRegen:
                                case AuraType.ObsModHealth:
                                    food = true;
                                    break;
                                // Drink
                                case AuraType.ModPowerRegen:
                                case AuraType.ObsModPower:
                                    drink = true;
                                    break;
                                default:
                                    break;
                            }
                        }

                        if (food && drink)
                            _spellSpecific = SpellSpecificType.FoodAndDrink;
                        else if (food)
                            _spellSpecific = SpellSpecificType.Food;
                        else if (drink)
                            _spellSpecific = SpellSpecificType.Drink;
                    }
                    // scrolls effects
                    else
                    {
                        SpellInfo firstRankSpellInfo = GetFirstRankSpell();
                        switch (firstRankSpellInfo.Id)
                        {
                            case 8118: // Strength
                            case 8099: // Stamina
                            case 8112: // Spirit
                            case 8096: // Intellect
                            case 8115: // Agility
                            case 8091: // Armor
                                _spellSpecific = SpellSpecificType.Scroll;
                                break;
                        }
                    }
                    break;
                }
                case SpellFamilyNames.Mage:
                {
                    // family flags 18(Molten), 25(Frost/Ice), 28(Mage)
                    if (SpellFamilyFlags[0].HasAnyFlag(0x12040000u))
                        _spellSpecific = SpellSpecificType.MageArmor;

                    // Arcane brillance and Arcane intelect (normal check fails because of flags difference)
                    if (SpellFamilyFlags[0].HasAnyFlag(0x400u))
                        _spellSpecific = SpellSpecificType.MageArcaneBrillance;

                    if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u) && GetEffect(0).IsAura(AuraType.ModConfuse))
                        _spellSpecific = SpellSpecificType.MagePolymorph;

                    break;
                }
                case SpellFamilyNames.Warrior:
                {
                    if (Id == 12292) // Death Wish
                        _spellSpecific = SpellSpecificType.WarriorEnrage;

                    break;
                }
                case SpellFamilyNames.Warlock:
                {
                    // Warlock (Bane of Doom | Bane of Agony | Bane of Havoc)
                    if (Id == 603 || Id == 980 || Id == 80240)
                        _spellSpecific = SpellSpecificType.Bane;

                    // only warlock curses have this
                    if (Dispel == DispelType.Curse)
                        _spellSpecific = SpellSpecificType.Curse;

                    // Warlock (Demon Armor | Demon Skin | Fel Armor)
                    if (SpellFamilyFlags[1].HasAnyFlag(0x20000020u) || SpellFamilyFlags[2].HasAnyFlag(0x00000010u))
                        _spellSpecific = SpellSpecificType.WarlockArmor;
                    break;
                }
                case SpellFamilyNames.Priest:
                {
                    // Divine Spirit and Prayer of Spirit
                    if (SpellFamilyFlags[0].HasAnyFlag(0x20u))
                        _spellSpecific = SpellSpecificType.PriestDivineSpirit;

                    break;
                }
                case SpellFamilyNames.Hunter:
                {
                    // only hunter stings have this
                    if (Dispel == DispelType.Poison)
                        _spellSpecific = SpellSpecificType.Sting;

                    // only hunter aspects have this (but not all aspects in hunter family)
                    if (SpellFamilyFlags & new FlagArray128(0x00200000, 0x00000000, 0x00001010, 0x00000000))
                        _spellSpecific = SpellSpecificType.Aspect;

                    break;
                }
                case SpellFamilyNames.Paladin:
                {
                    // Collection of all the seal family flags. No other paladin spell has any of those.
                    if (SpellFamilyFlags[1].HasAnyFlag(0xA2000800))
                        _spellSpecific = SpellSpecificType.Seal;

                    if (SpellFamilyFlags[0].HasAnyFlag(0x00002190u))
                        _spellSpecific = SpellSpecificType.Hand;

                    // only paladin auras have this (for palaldin class family)
                    switch (Id)
                    {
                        case 465:    // Devotion Aura
                        case 32223:  // Crusader Aura
                        case 183435: // Retribution Aura
                        case 317920: // Concentration Aura
                            _spellSpecific = SpellSpecificType.Aura;
                            break;
                        default:
                            break;
                    }

                    break;
                }
                case SpellFamilyNames.Shaman:
                {
                    // family flags 10 (Lightning), 42 (Earth), 37 (Water), proc shield from T2 8 pieces bonus
                    if (SpellFamilyFlags[1].HasAnyFlag(0x420u) || SpellFamilyFlags[0].HasAnyFlag(0x00000400u) || Id == 23552)
                        _spellSpecific = SpellSpecificType.ElementalShield;

                    break;
                }
                case SpellFamilyNames.Deathknight:
                    if (Id == 48266 || Id == 48263 || Id == 48265)
                        _spellSpecific = SpellSpecificType.Presence;
                    break;
            }

            foreach (var effectInfo in _effects)
            {
                if (effectInfo.IsEffect(SpellEffectName.ApplyAura))
                {
                    switch (effectInfo.ApplyAuraName)
                    {
                        case AuraType.ModCharm:
                        case AuraType.ModPossessPet:
                        case AuraType.ModPossess:
                        case AuraType.AoeCharm:
                            _spellSpecific = SpellSpecificType.Charm;
                            break;
                        case AuraType.TrackCreatures:
                            // @workaround For non-stacking tracking spells (We need generic solution)
                            if (Id == 30645) // Gas Cloud Tracking
                                _spellSpecific = SpellSpecificType.Normal;
                            break;
                        case AuraType.TrackResources:
                        case AuraType.TrackStealthed:
                            _spellSpecific = SpellSpecificType.Tracker;
                            break;
                    }
                }
            }
        }

        public void _LoadSpellDiminishInfo()
        {
            SpellDiminishInfo diminishInfo = new();
            diminishInfo.DiminishGroup = DiminishingGroupCompute();
            diminishInfo.DiminishReturnType = DiminishingTypeCompute(diminishInfo.DiminishGroup);
            diminishInfo.DiminishMaxLevel = DiminishingMaxLevelCompute(diminishInfo.DiminishGroup);
            diminishInfo.DiminishDurationLimit = DiminishingLimitDurationCompute();

            _diminishInfo = diminishInfo;
        }

        public DiminishingGroup GetDiminishingReturnsGroupForSpell()
        {
            return _diminishInfo.DiminishGroup;
        }

        public DiminishingReturnsType GetDiminishingReturnsGroupType()
        {
            return _diminishInfo.DiminishReturnType;
        }

        public DiminishingLevels GetDiminishingReturnsMaxLevel()
        {
            return _diminishInfo.DiminishMaxLevel;
        }

        public int GetDiminishingReturnsLimitDuration()
        {
            return _diminishInfo.DiminishDurationLimit;
        }

        DiminishingGroup DiminishingGroupCompute()
        {
            if (IsPositive())
                return DiminishingGroup.None;

            if (HasAura(AuraType.ModTaunt))
                return DiminishingGroup.Taunt;

            switch (Id)
            {
                case 20549:     // War Stomp (Racial - Tauren)
                case 24394:     // Intimidation
                case 118345:    // Pulverize (Primal Earth Elemental)
                case 118905:    // Static Charge (Capacitor Totem)
                    return DiminishingGroup.Stun;
                case 107079:    // Quaking Palm
                    return DiminishingGroup.Incapacitate;
                case 155145:    // Arcane Torrent (Racial - Blood Elf)
                    return DiminishingGroup.Silence;
                case 108199:    // Gorefiend's Grasp
                case 191244:    // Sticky Bomb
                    return DiminishingGroup.AOEKnockback;
                default:
                    break;
            }

            // Explicit Diminishing Groups
            switch (SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    // Frost Tomb
                    if (Id == 48400)
                        return DiminishingGroup.None;
                    // Gnaw
                    else if (Id == 47481)
                        return DiminishingGroup.Stun;
                    // ToC Icehowl Arctic Breath
                    else if (Id == 66689)
                        return DiminishingGroup.None;
                    // Black Plague
                    else if (Id == 64155)
                        return DiminishingGroup.None;
                    // Screams of the Dead (King Ymiron)
                    else if (Id == 51750)
                        return DiminishingGroup.None;
                    // Crystallize (Keristrasza heroic)
                    else if (Id == 48179)
                        return DiminishingGroup.None;
                    break;
                case SpellFamilyNames.Mage:
                {
                    // Frost Nova -- 122
                    if (SpellFamilyFlags[0].HasAnyFlag(0x40u))
                        return DiminishingGroup.Root;
                    // Freeze (Water Elemental) -- 33395
                    if (SpellFamilyFlags[2].HasAnyFlag(0x200u))
                        return DiminishingGroup.Root;

                    // Dragon's Breath -- 31661
                    if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
                        return DiminishingGroup.Incapacitate;
                    // Polymorph -- 118
                    if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u))
                        return DiminishingGroup.Incapacitate;
                    // Ring of Frost -- 82691
                    if (SpellFamilyFlags[2].HasAnyFlag(0x40u))
                        return DiminishingGroup.Incapacitate;
                    // Ice Nova -- 157997
                    if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
                        return DiminishingGroup.Incapacitate;
                    break;
                }
                case SpellFamilyNames.Warrior:
                {
                    // Shockwave -- 132168
                    if (SpellFamilyFlags[1].HasAnyFlag(0x8000u))
                        return DiminishingGroup.Stun;
                    // Storm Bolt -- 132169
                    if (SpellFamilyFlags[2].HasAnyFlag(0x1000u))
                        return DiminishingGroup.Stun;

                    // Intimidating Shout -- 5246
                    if (SpellFamilyFlags[0].HasAnyFlag(0x40000u))
                        return DiminishingGroup.Disorient;
                    break;
                }
                case SpellFamilyNames.Warlock:
                {
                    // Mortal Coil -- 6789
                    if (SpellFamilyFlags[0].HasAnyFlag(0x80000u))
                        return DiminishingGroup.Incapacitate;
                    // Banish -- 710
                    if (SpellFamilyFlags[1].HasAnyFlag(0x8000000u))
                        return DiminishingGroup.Incapacitate;

                    // Fear -- 118699
                    if (SpellFamilyFlags[1].HasAnyFlag(0x400u))
                        return DiminishingGroup.Disorient;
                    // Howl of Terror -- 5484
                    if (SpellFamilyFlags[1].HasAnyFlag(0x8u))
                        return DiminishingGroup.Disorient;

                    // Shadowfury -- 30283
                    if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
                        return DiminishingGroup.Stun;
                    // Summon Infernal -- 22703
                    if (SpellFamilyFlags[0].HasAnyFlag(0x1000u))
                        return DiminishingGroup.Stun;

                    // 170995 -- Cripple
                    if (Id == 170995)
                        return DiminishingGroup.LimitOnly;
                    break;
                }
                case SpellFamilyNames.WarlockPet:
                {
                    // Fellash -- 115770
                    // Whiplash -- 6360
                    if (SpellFamilyFlags[0].HasAnyFlag(0x8000000u))
                        return DiminishingGroup.AOEKnockback;

                    // Mesmerize (Shivarra pet) -- 115268
                    // Seduction (Succubus pet) -- 6358
                    if (SpellFamilyFlags[0].HasAnyFlag(0x2000000u))
                        return DiminishingGroup.Disorient;

                    // Axe Toss (Felguard pet) -- 89766
                    if (SpellFamilyFlags[1].HasAnyFlag(0x4u))
                        return DiminishingGroup.Stun;
                    break;
                }
                case SpellFamilyNames.Druid:
                {
                    // Maim -- 22570
                    if (SpellFamilyFlags[1].HasAnyFlag(0x80u))
                        return DiminishingGroup.Stun;
                    // Mighty Bash -- 5211
                    if (SpellFamilyFlags[0].HasAnyFlag(0x2000u))
                        return DiminishingGroup.Stun;
                    // Rake -- 163505 -- no flags on the stun
                    if (Id == 163505)
                        return DiminishingGroup.Stun;

                    // Incapacitating Roar -- 99, no flags on the stun, 14
                    if (SpellFamilyFlags[1].HasAnyFlag(0x1u))
                        return DiminishingGroup.Incapacitate;

                    // Cyclone -- 33786
                    if (SpellFamilyFlags[1].HasAnyFlag(0x20u))
                        return DiminishingGroup.Disorient;

                    // Solar Beam -- 81261
                    if (Id == 81261)
                        return DiminishingGroup.Silence;

                    // Typhoon -- 61391
                    if (SpellFamilyFlags[1].HasAnyFlag(0x1000000u))
                        return DiminishingGroup.AOEKnockback;
                    // Ursol's Vortex -- 118283, no family flags
                    if (Id == 118283)
                        return DiminishingGroup.AOEKnockback;

                    // Entangling Roots -- 339
                    if (SpellFamilyFlags[0].HasAnyFlag(0x200u))
                        return DiminishingGroup.Root;
                    // Mass Entanglement -- 102359
                    if (SpellFamilyFlags[2].HasAnyFlag(0x4u))
                        return DiminishingGroup.Root;
                    break;
                }
                case SpellFamilyNames.Rogue:
                {
                    // Between the Eyes -- 199804
                    if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
                        return DiminishingGroup.Stun;
                    // Cheap Shot -- 1833
                    if (SpellFamilyFlags[0].HasAnyFlag(0x400u))
                        return DiminishingGroup.Stun;
                    // Kidney Shot -- 408
                    if (SpellFamilyFlags[0].HasAnyFlag(0x200000u))
                        return DiminishingGroup.Stun;

                    // Gouge -- 1776
                    if (SpellFamilyFlags[0].HasAnyFlag(0x8u))
                        return DiminishingGroup.Incapacitate;
                    // Sap -- 6770
                    if (SpellFamilyFlags[0].HasAnyFlag(0x80u))
                        return DiminishingGroup.Incapacitate;

                    // Blind -- 2094
                    if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u))
                        return DiminishingGroup.Disorient;

                    // Garrote -- 1330
                    if (SpellFamilyFlags[1].HasAnyFlag(0x20000000u))
                        return DiminishingGroup.Silence;
                    break;
                }
                case SpellFamilyNames.Hunter:
                {
                    // Charge (Tenacity pet) -- 53148, no flags
                    if (Id == 53148)
                        return DiminishingGroup.Root;
                    // Ranger's Net -- 200108
                    // Tracker's Net -- 212638
                    if (Id == 200108 || Id == 212638)
                        return DiminishingGroup.Root;

                    // Binding Shot -- 117526, no flags
                    if (Id == 117526)
                        return DiminishingGroup.Stun;

                    // Freezing Trap -- 3355
                    if (SpellFamilyFlags[0].HasAnyFlag(0x8u))
                        return DiminishingGroup.Incapacitate;
                    // Wyvern Sting -- 19386
                    if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
                        return DiminishingGroup.Incapacitate;

                    // Bursting Shot -- 224729
                    if (SpellFamilyFlags[2].HasAnyFlag(0x40u))
                        return DiminishingGroup.Disorient;
                    // Scatter Shot -- 213691
                    if (SpellFamilyFlags[2].HasAnyFlag(0x8000u))
                        return DiminishingGroup.Disorient;

                    // Spider Sting -- 202933
                    if (Id == 202933)
                        return DiminishingGroup.Silence;
                    break;
                }
                case SpellFamilyNames.Paladin:
                {
                    // Repentance -- 20066
                    if (SpellFamilyFlags[0].HasAnyFlag(0x4u))
                        return DiminishingGroup.Incapacitate;

                    // Blinding Light -- 105421
                    if (Id == 105421)
                        return DiminishingGroup.Disorient;

                    // Avenger's Shield -- 31935
                    if (SpellFamilyFlags[0].HasAnyFlag(0x4000u))
                        return DiminishingGroup.Silence;

                    // Hammer of Justice -- 853
                    if (SpellFamilyFlags[0].HasAnyFlag(0x800u))
                        return DiminishingGroup.Stun;
                    break;
                }
                case SpellFamilyNames.Shaman:
                {
                    // Hex -- 51514
                    // Hex -- 196942 (Voodoo Totem)
                    if (SpellFamilyFlags[1].HasAnyFlag(0x8000u))
                        return DiminishingGroup.Incapacitate;

                    // Thunderstorm -- 51490
                    if (SpellFamilyFlags[1].HasAnyFlag(0x2000u))
                        return DiminishingGroup.AOEKnockback;
                    // Earthgrab Totem -- 64695
                    if (SpellFamilyFlags[2].HasAnyFlag(0x4000u))
                        return DiminishingGroup.Root;

                    // Lightning Lasso -- 204437
                    if (SpellFamilyFlags[3].HasAnyFlag(0x2000000u))
                        return DiminishingGroup.Stun;
                    break;
                }
                case SpellFamilyNames.Deathknight:
                {
                    // Chains of Ice -- 96294
                    if (Id == 96294)
                        return DiminishingGroup.Root;

                    // Blinding Sleet -- 207167
                    if (Id == 207167)
                        return DiminishingGroup.Disorient;

                    // Strangulate -- 47476
                    if (SpellFamilyFlags[0].HasAnyFlag(0x200u))
                        return DiminishingGroup.Silence;

                    // Asphyxiate -- 108194
                    if (SpellFamilyFlags[2].HasAnyFlag(0x100000u))
                        return DiminishingGroup.Stun;
                    // Gnaw (Ghoul) -- 91800, no flags
                    if (Id == 91800)
                        return DiminishingGroup.Stun;
                    // Monstrous Blow (Ghoul w/ Dark Transformation active) -- 91797
                    if (Id == 91797)
                        return DiminishingGroup.Stun;
                    // Winter is Coming -- 207171
                    if (Id == 207171)
                        return DiminishingGroup.Stun;
                    break;
                }
                case SpellFamilyNames.Priest:
                {
                    // Holy Word: Chastise -- 200200
                    if (SpellFamilyFlags[2].HasAnyFlag(0x20u) && GetSpellVisual() == 52021)
                        return DiminishingGroup.Stun;
                    // Mind Bomb -- 226943
                    if (Id == 226943)
                        return DiminishingGroup.Stun;

                    // Mind Control -- 605
                    if (SpellFamilyFlags[0].HasAnyFlag(0x20000u) && GetSpellVisual() == 39068)
                        return DiminishingGroup.Incapacitate;
                    // Holy Word: Chastise -- 200196
                    if (SpellFamilyFlags[2].HasAnyFlag(0x20u) && GetSpellVisual() == 52019)
                        return DiminishingGroup.Incapacitate;

                    // Psychic Scream -- 8122
                    if (SpellFamilyFlags[0].HasAnyFlag(0x10000u))
                        return DiminishingGroup.Disorient;

                    // Silence -- 15487
                    if (SpellFamilyFlags[1].HasAnyFlag(0x200000u) && GetSpellVisual() == 39025)
                        return DiminishingGroup.Silence;

                    // Shining Force -- 204263
                    if (Id == 204263)
                        return DiminishingGroup.AOEKnockback;
                    break;
                }
                case SpellFamilyNames.Monk:
                {
                    // Disable -- 116706, no flags
                    if (Id == 116706)
                        return DiminishingGroup.Root;

                    // Fists of Fury -- 120086
                    if (SpellFamilyFlags[1].HasAnyFlag(0x800000u) && !SpellFamilyFlags[2].HasAnyFlag(0x8u))
                        return DiminishingGroup.Stun;
                    // Leg Sweep -- 119381
                    if (SpellFamilyFlags[1].HasAnyFlag(0x200u))
                        return DiminishingGroup.Stun;

                    // Incendiary Breath (honor talent) -- 202274, no flags
                    if (Id == 202274)
                        return DiminishingGroup.Incapacitate;
                    // Paralysis -- 115078
                    if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
                        return DiminishingGroup.Incapacitate;

                    // Song of Chi-Ji -- 198909
                    if (Id == 198909)
                        return DiminishingGroup.Disorient;
                    break;
                }
                case SpellFamilyNames.DemonHunter:
                    switch (Id)
                    {
                        case 179057: // Chaos Nova
                        case 211881: // Fel Eruption
                        case 200166: // Metamorphosis
                        case 205630: // Illidan's Grasp
                            return DiminishingGroup.Stun;
                        case 217832: // Imprison
                        case 221527: // Imprison
                            return DiminishingGroup.Incapacitate;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            return DiminishingGroup.None;
        }

        DiminishingReturnsType DiminishingTypeCompute(DiminishingGroup group)
        {
            switch (group)
            {
                case DiminishingGroup.Taunt:
                case DiminishingGroup.Stun:
                    return DiminishingReturnsType.All;
                case DiminishingGroup.LimitOnly:
                case DiminishingGroup.None:
                    return DiminishingReturnsType.None;
                default:
                    return DiminishingReturnsType.Player;
            }
        }

        DiminishingLevels DiminishingMaxLevelCompute(DiminishingGroup group)
        {
            switch (group)
            {
                case DiminishingGroup.Taunt:
                    return DiminishingLevels.TauntImmune;
                case DiminishingGroup.AOEKnockback:
                    return DiminishingLevels.Level2;
                default:
                    return DiminishingLevels.Immune;
            }
        }

        int DiminishingLimitDurationCompute()
        {
            // Explicit diminishing duration
            switch (SpellFamilyName)
            {
                case SpellFamilyNames.Mage:
                    // Dragon's Breath - 3 seconds in PvP
                    if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
                        return 3 * Time.InMilliseconds;
                    break;
                case SpellFamilyNames.Warlock:
                    // Cripple - 4 seconds in PvP
                    if (Id == 170995)
                        return 4 * Time.InMilliseconds;
                    break;
                case SpellFamilyNames.Hunter:
                    // Binding Shot - 3 seconds in PvP
                    if (Id == 117526)
                        return 3 * Time.InMilliseconds;

                    // Wyvern Sting - 6 seconds in PvP
                    if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
                        return 6 * Time.InMilliseconds;
                    break;
                case SpellFamilyNames.Monk:
                    // Paralysis - 4 seconds in PvP regardless of if they are facing you
                    if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
                        return 4 * Time.InMilliseconds;
                    break;
                case SpellFamilyNames.DemonHunter:
                    switch (Id)
                    {
                        case 217832: // Imprison
                        case 221527: // Imprison
                            return 4 * Time.InMilliseconds;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }

            return 8 * Time.InMilliseconds;
        }

        public void _LoadImmunityInfo()
        {
            foreach (SpellEffectInfo effect in _effects)
            {
                uint schoolImmunityMask = 0;
                uint applyHarmfulAuraImmunityMask = 0;
                ulong mechanicImmunityMask = 0;
                uint dispelImmunityMask = 0;
                uint damageImmunityMask = 0;
                byte otherImmunityMask = 0;

                int miscVal = effect.MiscValue;

                ImmunityInfo immuneInfo = effect.GetImmunityInfo();

                switch (effect.ApplyAuraName)
                {
                    case AuraType.MechanicImmunityMask:
                    {
                        CreatureImmunities creatureImmunities = Global.SpellMgr.GetCreatureImmunities(miscVal);
                        if (creatureImmunities != null)
                        {
                            schoolImmunityMask |= creatureImmunities.School.ToUInt();
                            dispelImmunityMask |= creatureImmunities.DispelType.ToUInt();
                            mechanicImmunityMask |= creatureImmunities.Mechanic.ToUInt();
                            otherImmunityMask |= (byte)creatureImmunities.Other;
                            foreach (SpellEffectName effectType in creatureImmunities.Effect)
                                immuneInfo.SpellEffectImmune.Add(effectType);
                            foreach (AuraType aura in creatureImmunities.Aura)
                                immuneInfo.AuraTypeImmune.Add(aura);
                        }
                        break;
                    }
                    case AuraType.MechanicImmunity:
                    {
                        switch (Id)
                        {
                            case 42292: // PvP trinket
                            case 59752: // Every Man for Himself
                                mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;
                                immuneInfo.AuraTypeImmune.Add(AuraType.UseNormalMovementSpeed);
                                break;
                            case 34471: // The Beast Within
                            case 19574: // Bestial Wrath
                            case 46227: // Medallion of Immunity
                            case 53490: // Bullheaded
                            case 65547: // PvP Trinket
                            case 134946: // Supremacy of the Alliance
                            case 134956: // Supremacy of the Horde
                            case 195710: // Honorable Medallion
                            case 208683: // Gladiator's Medallion
                                mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;
                                break;
                            case 54508: // Demonic Empowerment
                                mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Stun);
                                break;
                            default:
                                if (miscVal < 1)
                                    break;

                                mechanicImmunityMask |= 1ul << miscVal;
                                break;
                        }
                        break;
                    }
                    case AuraType.EffectImmunity:
                    {
                        immuneInfo.SpellEffectImmune.Add((SpellEffectName)miscVal);
                        break;
                    }
                    case AuraType.StateImmunity:
                    {
                        immuneInfo.AuraTypeImmune.Add((AuraType)miscVal);
                        break;
                    }
                    case AuraType.SchoolImmunity:
                    {
                        schoolImmunityMask |= (uint)miscVal;
                        break;
                    }
                    case AuraType.ModImmuneAuraApplySchool:
                    {
                        applyHarmfulAuraImmunityMask |= (uint)miscVal;
                        break;
                    }
                    case AuraType.DamageImmunity:
                    {
                        damageImmunityMask |= (uint)miscVal;
                        break;
                    }
                    case AuraType.DispelImmunity:
                    {
                        dispelImmunityMask = 1u << miscVal;
                        break;
                    }
                    default:
                        break;
                }

                immuneInfo.SchoolImmuneMask = schoolImmunityMask;
                immuneInfo.ApplyHarmfulAuraImmuneMask = applyHarmfulAuraImmunityMask;
                immuneInfo.MechanicImmuneMask = mechanicImmunityMask;
                immuneInfo.DispelImmuneMask = dispelImmunityMask;
                immuneInfo.DamageSchoolMask = damageImmunityMask;
                immuneInfo.OtherImmuneMask = otherImmunityMask;

                _allowedMechanicMask |= immuneInfo.MechanicImmuneMask;
            }

            if (HasAttribute(SpellAttr5.AllowWhileStunned))
            {
                switch (Id)
                {
                    case 22812: // Barkskin
                    case 47585: // Dispersion
                        _allowedMechanicMask |=
                            (1 << (int)Mechanics.Stun) |
                            (1 << (int)Mechanics.Freeze) |
                            (1 << (int)Mechanics.Knockout) |
                            (1 << (int)Mechanics.Sleep);
                        break;
                    case 49039: // Lichborne, don't allow normal stuns
                        break;
                    default:
                        _allowedMechanicMask |= (1 << (int)Mechanics.Stun);
                        break;
                }
            }

            if (HasAttribute(SpellAttr5.AllowWhileConfused))
                _allowedMechanicMask |= (1 << (int)Mechanics.Disoriented);

            if (HasAttribute(SpellAttr5.AllowWhileFleeing))
            {
                switch (Id)
                {
                    case 22812: // Barkskin
                    case 47585: // Dispersion
                        _allowedMechanicMask |= (1 << (int)Mechanics.Fear) | (1 << (int)Mechanics.Horror);
                        break;
                    default:
                        _allowedMechanicMask |= (1 << (int)Mechanics.Fear);
                        break;
                }
            }
        }

        public void _LoadSqrtTargetLimit(int maxTargets, int numNonDiminishedTargets, uint? maxTargetsValueHolderSpell, uint? maxTargetsValueHolderEffect,
            uint? numNonDiminishedTargetsValueHolderSpell, uint? numNonDiminishedTargetsValueHolderEffect)
        {
            SqrtDamageAndHealingDiminishing.MaxTargets = maxTargets;
            SqrtDamageAndHealingDiminishing.NumNonDiminishedTargets = numNonDiminishedTargets;

            if (maxTargetsValueHolderEffect.HasValue)
            {
                SpellInfo maxTargetValueHolder = this;
                if (maxTargetsValueHolderSpell.HasValue)
                    maxTargetValueHolder = Global.SpellMgr.GetSpellInfo(maxTargetsValueHolderSpell.Value, Difficulty);

                if (maxTargetValueHolder == null)
                    Log.outError(LogFilter.Spells, $"SpellInfo::_LoadSqrtTargetLimit(maxTargets): Spell {maxTargetsValueHolderSpell} does not exist");
                else if (maxTargetsValueHolderEffect >= maxTargetValueHolder.GetEffects().Count)
                    Log.outError(LogFilter.Spells, $"SpellInfo::_LoadSqrtTargetLimit(maxTargets): Spell {maxTargetValueHolder.Id} does not have effect {maxTargetsValueHolderEffect.Value}");
                else
                {
                    SpellEffectInfo valueHolder = maxTargetValueHolder.GetEffect(maxTargetsValueHolderEffect.Value);
                    int expectedValue = valueHolder.CalcBaseValue(null, null, 0, -1);
                    if (maxTargets != expectedValue)
                        Log.outError(LogFilter.Spells, $"SpellInfo::_LoadSqrtTargetLimit(maxTargets): Spell {maxTargetValueHolder.Id} has different value in effect {maxTargetsValueHolderEffect.Value} than expected, recheck target caps (expected {maxTargets}, got {expectedValue})");
                }
            }

            if (numNonDiminishedTargetsValueHolderEffect.HasValue)
            {
                SpellInfo numNonDiminishedTargetsValueHolder = this;
                if (numNonDiminishedTargetsValueHolderSpell.HasValue)
                    numNonDiminishedTargetsValueHolder = Global.SpellMgr.GetSpellInfo(numNonDiminishedTargetsValueHolderSpell.Value, Difficulty);

                if (numNonDiminishedTargetsValueHolder == null)
                    Log.outError(LogFilter.Spells, $"SpellInfo::_LoadSqrtTargetLimit(numNonDiminishedTargets): Spell {maxTargetsValueHolderSpell} does not exist");
                else if (numNonDiminishedTargetsValueHolderEffect >= numNonDiminishedTargetsValueHolder.GetEffects().Count)
                    Log.outError(LogFilter.Spells, $"SpellInfo::_LoadSqrtTargetLimit(numNonDiminishedTargets): Spell {numNonDiminishedTargetsValueHolder.Id} does not have effect {maxTargetsValueHolderEffect.Value}");
                else
                {
                    SpellEffectInfo valueHolder = numNonDiminishedTargetsValueHolder.GetEffect(numNonDiminishedTargetsValueHolderEffect.Value);
                    int expectedValue = valueHolder.CalcBaseValue(null, null, 0, -1);
                    if (numNonDiminishedTargets != expectedValue)
                        Log.outError(LogFilter.Spells, $"SpellInfo::_LoadSqrtTargetLimit(numNonDiminishedTargets): Spell {numNonDiminishedTargetsValueHolder.Id} has different value in effect {numNonDiminishedTargetsValueHolderEffect.Value} than expected, recheck target caps (expected {numNonDiminishedTargets}, got {expectedValue})");
                }
            }
        }

        public void ApplyAllSpellImmunitiesTo(Unit target, SpellEffectInfo spellEffectInfo, bool apply)
        {
            ImmunityInfo immuneInfo = spellEffectInfo.GetImmunityInfo();

            uint schoolImmunity = immuneInfo.SchoolImmuneMask;
            if (schoolImmunity != 0)
            {
                target.ApplySpellImmune(Id, SpellImmunity.School, schoolImmunity, apply);

                if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                {
                    target.RemoveAppliedAuras(aurApp =>
                    {
                        SpellInfo auraSpellInfo = aurApp.GetBase().GetSpellInfo();
                        if (auraSpellInfo.Id == Id)                                      // Don't remove self
                            return false;
                        if (auraSpellInfo.IsPassive())                                   // Don't remove passive auras
                            return false;
                        if (((uint)auraSpellInfo.GetSchoolMask() & schoolImmunity) == 0)           // Check for school mask
                            return false;
                        if (!CanDispelAura(auraSpellInfo))
                            return false;
                        if (!HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects))
                        {
                            WorldObject existingAuraCaster = aurApp.GetBase().GetWorldObjectCaster();
                            if (existingAuraCaster != null && existingAuraCaster.IsFriendlyTo(target)) // Check spell vs aura possitivity
                                return false;
                        }
                        return true;
                    });
                }

                if (apply && (schoolImmunity & (uint)SpellSchoolMask.Normal) != 0)
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.InvulnerabilityBuff);
            }

            ulong mechanicImmunity = immuneInfo.MechanicImmuneMask;
            if (mechanicImmunity != 0)
            {
                for (uint i = 0; i < (int)Mechanics.Max; ++i)
                    if (Convert.ToBoolean(mechanicImmunity & (1ul << (int)i)))
                        target.ApplySpellImmune(Id, SpellImmunity.Mechanic, i, apply);

                if (HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                {
                    // exception for purely snare mechanic (eg. hands of freedom)!
                    if (apply)
                        target.RemoveAurasWithMechanic(mechanicImmunity, AuraRemoveMode.Default, Id);
                    else
                    {
                        List<Aura> aurasToUpdateTargets = new();
                        target.RemoveAppliedAuras(aurApp =>
                        {
                            Aura aura = aurApp.GetBase();
                            if ((aura.GetSpellInfo().GetAllEffectsMechanicMask() & mechanicImmunity) != 0)
                                aurasToUpdateTargets.Add(aura);

                            // only update targets, don't remove anything
                            return false;
                        });

                        foreach (Aura aura in aurasToUpdateTargets)
                            aura.UpdateTargetMap(aura.GetCaster());
                    }
                }
            }

            uint dispelImmunity = immuneInfo.DispelImmuneMask;
            if (dispelImmunity != 0)
            {
                for (int i = 0; i < (int)DispelType.Max; ++i)
                    if ((dispelImmunity & (1u << i)) != 0)
                        target.ApplySpellImmune(Id, SpellImmunity.Dispel, (uint)i, apply);

                if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                {
                    target.RemoveAppliedAuras(aurApp =>
                    {
                        SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                        uint dispelMask = spellInfo.GetDispelMask();
                        if ((dispelMask & dispelImmunity) == dispelMask)
                            return true;

                        return false;
                    });
                }
            }

            uint damageImmunity = immuneInfo.DamageSchoolMask;
            if (damageImmunity != 0)
            {
                target.ApplySpellImmune(Id, SpellImmunity.Damage, damageImmunity, apply);

                if (apply && (damageImmunity & (uint)SpellSchoolMask.Normal) != 0)
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.InvulnerabilityBuff);
            }

            foreach (AuraType auraType in immuneInfo.AuraTypeImmune)
            {
                target.ApplySpellImmune(Id, SpellImmunity.State, auraType, apply);
                if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                    target.RemoveAurasByType(auraType, aurApp => CanDispelAura(aurApp.GetBase().GetSpellInfo()));
            }

            foreach (SpellEffectName effectType in immuneInfo.SpellEffectImmune)
                target.ApplySpellImmune(Id, SpellImmunity.Effect, effectType, apply);

            byte otherImmuneMask = immuneInfo.OtherImmuneMask;
            if (otherImmuneMask != 0)
                target.ApplySpellImmune(Id, SpellImmunity.Other, otherImmuneMask, apply);
        }

        bool CanSpellProvideImmunityAgainstAura(SpellInfo auraSpellInfo)
        {
            if (auraSpellInfo == null)
                return false;

            foreach (var effectInfo in _effects)
            {
                if (!effectInfo.IsEffect())
                    continue;

                ImmunityInfo immuneInfo = effectInfo.GetImmunityInfo();

                if (!auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
                {
                    uint schoolImmunity = immuneInfo.SchoolImmuneMask;
                    if (schoolImmunity != 0)
                        if (((uint)auraSpellInfo.SchoolMask & schoolImmunity) != 0)
                            return true;
                }

                ulong mechanicImmunity = immuneInfo.MechanicImmuneMask;
                if (mechanicImmunity != 0)
                    if ((mechanicImmunity & (1ul << (int)auraSpellInfo.Mechanic)) != 0)
                        return true;

                uint dispelImmunity = immuneInfo.DispelImmuneMask;
                if (dispelImmunity != 0)
                    if ((uint)auraSpellInfo.Dispel == dispelImmunity)
                        return true;

                bool immuneToAllEffects = true;
                foreach (var auraSpellEffectInfo in auraSpellInfo.GetEffects())
                {
                    if (!auraSpellEffectInfo.IsAura())
                        continue;

                    if (mechanicImmunity != 0)
                        if ((mechanicImmunity & (1ul << (int)auraSpellEffectInfo.Mechanic)) != 0)
                            continue;

                    AuraType auraName = auraSpellEffectInfo.ApplyAuraName;
                    if (auraName != 0)
                    {
                        if (immuneInfo.AuraTypeImmune.Contains(auraName))
                            continue;

                        if (!auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities) && !auraSpellInfo.IsPositiveEffect(auraSpellEffectInfo.EffectIndex))
                        {
                            uint applyHarmfulAuraImmunityMask = immuneInfo.ApplyHarmfulAuraImmuneMask;
                            if (applyHarmfulAuraImmunityMask != 0)
                                if (((uint)auraSpellInfo.GetSchoolMask() & applyHarmfulAuraImmunityMask) != 0)
                                    continue;
                        }
                    }

                    immuneToAllEffects = false;
                }

                if (immuneToAllEffects)
                    return true;
            }

            return false;
        }

        bool CanSpellEffectProvideImmunityAgainstAuraEffect(SpellEffectInfo immunityEffectInfo, SpellInfo auraSpellInfo, SpellEffectInfo auraEffectInfo)
        {
            ImmunityInfo immuneInfo = immunityEffectInfo.GetImmunityInfo();
            if (immuneInfo == null)
                return false;

            if (!auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
            {
                uint schoolImmunity = immuneInfo.SchoolImmuneMask;
                if (schoolImmunity != 0)
                    if (((uint)auraSpellInfo.SchoolMask & schoolImmunity) != 0)
                        return true;

                uint applyHarmfulAuraImmunityMask = immuneInfo.ApplyHarmfulAuraImmuneMask;
                if (applyHarmfulAuraImmunityMask != 0)
                    if (((uint)auraSpellInfo.GetSchoolMask() & applyHarmfulAuraImmunityMask) != 0)
                        return true;
            }

            ulong mechanicImmunity = immuneInfo.MechanicImmuneMask;
            if (mechanicImmunity != 0)
            {
                if ((mechanicImmunity & (1ul << (int)auraSpellInfo.Mechanic)) != 0)
                    return true;
                if ((mechanicImmunity & (1ul << (int)auraEffectInfo.Mechanic)) != 0)
                    return true;
            }

            uint dispelImmunity = immuneInfo.DispelImmuneMask;
            if (dispelImmunity != 0 && (uint)auraSpellInfo.Dispel == dispelImmunity)
                return true;

            if (immuneInfo.AuraTypeImmune.Contains(auraEffectInfo.ApplyAuraName))
                return true;

            return false;
        }

        public bool SpellCancelsAuraEffect(AuraEffect aurEff)
        {
            if (!HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                return false;

            if (aurEff.GetSpellInfo().HasAttribute(SpellAttr0.NoImmunities))
                return false;

            if (aurEff.GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoImmunity))
                return false;

            foreach (SpellEffectInfo effect in GetEffects())
                if (CanSpellEffectProvideImmunityAgainstAuraEffect(effect, aurEff.GetSpellInfo(), aurEff.GetSpellEffectInfo()))
                    return true;

            return false;
        }

        public ulong GetAllowedMechanicMask()
        {
            return _allowedMechanicMask;
        }

        public ulong GetMechanicImmunityMask(Unit caster)
        {
            ulong casterMechanicImmunityMask = caster.GetMechanicImmunityMask();
            ulong mechanicImmunityMask = 0;

            if (CanBeInterrupted(null, caster, true))
            {
                if ((casterMechanicImmunityMask & (1 << (int)Mechanics.Silence)) != 0)
                    mechanicImmunityMask |= (1 << (int)Mechanics.Silence);

                if ((casterMechanicImmunityMask & (1 << (int)Mechanics.Interrupt)) != 0)
                    mechanicImmunityMask |= (1 << (int)Mechanics.Interrupt);
            }

            return mechanicImmunityMask;
        }

        public float GetMinRange(bool positive = false)
        {
            if (RangeEntry == null)
                return 0.0f;

            return RangeEntry.RangeMin[positive ? 1 : 0];
        }

        public float GetMaxRange(bool positive = false, WorldObject caster = null, Spell spell = null)
        {
            if (RangeEntry == null)
                return 0.0f;

            float range = RangeEntry.RangeMax[positive ? 1 : 0];
            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(this, SpellModOp.Range, ref range, spell);
            }
            return range;
        }

        public int CalcDuration(WorldObject caster = null)
        {
            int duration = GetDuration();

            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(this, SpellModOp.Duration, ref duration);
            }

            return duration;
        }

        public int GetDuration()
        {
            if (DurationEntry == null)
                return IsPassive() ? -1 : 0;

            return (DurationEntry.Duration == -1) ? -1 : Math.Abs(DurationEntry.Duration);
        }

        public int GetMaxDuration()
        {
            if (DurationEntry == null)
                return IsPassive() ? -1 : 0;

            return (DurationEntry.MaxDuration == -1) ? -1 : Math.Abs(DurationEntry.MaxDuration);
        }

        public int CalcCastTime(Spell spell = null)
        {
            int castTime = 0;
            if (CastTimeEntry != null)
                castTime = Math.Max(CastTimeEntry.Base, CastTimeEntry.Minimum);

            if (castTime <= 0)
                return 0;

            if (spell != null)
                spell.GetCaster().ModSpellCastTime(this, ref castTime, spell);

            if (HasAttribute(SpellAttr0.UsesRangedSlot) && (!IsAutoRepeatRangedSpell()) && !HasAttribute(SpellAttr9.CooldownIgnoresRangedWeapon))
                castTime += 500;

            return (castTime > 0) ? castTime : 0;
        }

        public uint GetMaxTicks()
        {
            uint totalTicks = 0;
            int DotDuration = GetDuration();

            foreach (var effectInfo in GetEffects())
            {
                if (!effectInfo.IsEffect(SpellEffectName.ApplyAura))
                    continue;

                switch (effectInfo.ApplyAuraName)
                {
                    case AuraType.PeriodicDamage:
                    case AuraType.PeriodicDamagePercent:
                    case AuraType.PeriodicHeal:
                    case AuraType.ObsModHealth:
                    case AuraType.ObsModPower:
                    case AuraType.PeriodicTriggerSpellFromClient:
                    case AuraType.PowerBurn:
                    case AuraType.PeriodicLeech:
                    case AuraType.PeriodicManaLeech:
                    case AuraType.PeriodicEnergize:
                    case AuraType.PeriodicDummy:
                    case AuraType.PeriodicTriggerSpell:
                    case AuraType.PeriodicTriggerSpellWithValue:
                    case AuraType.PeriodicHealthFunnel:
                        // skip infinite periodics
                        if (effectInfo.ApplyAuraPeriod > 0 && DotDuration > 0)
                        {
                            totalTicks = (uint)DotDuration / effectInfo.ApplyAuraPeriod;
                            if (HasAttribute(SpellAttr5.ExtraInitialPeriod))
                                ++totalTicks;
                        }
                        break;
                }
            }

            return totalTicks;
        }

        public uint GetRecoveryTime()
        {
            return RecoveryTime > CategoryRecoveryTime ? RecoveryTime : CategoryRecoveryTime;
        }

        public SpellPowerCost CalcPowerCost(PowerType powerType, bool optionalCost, WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
        {
            // gameobject casts don't use power
            Unit unitCaster = caster.ToUnit();
            if (unitCaster == null)
                return null;

            var spellPowerRecord = PowerCosts.FirstOrDefault(spellPowerEntry => spellPowerEntry?.PowerType == powerType);
            if (spellPowerRecord == null)
                return null;

            return CalcPowerCost(spellPowerRecord, optionalCost, caster, schoolMask, spell);
        }

        public SpellPowerCost CalcPowerCost(SpellPowerRecord power, bool optionalCost, WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
        {
            // gameobject casts don't use power
            Unit unitCaster = caster.ToUnit();
            if (unitCaster == null)
                return null;

            if (power.RequiredAuraSpellID != 0 && !unitCaster.HasAura(power.RequiredAuraSpellID))
                return null;

            // Spell drain all exist power on cast (Only paladin lay of Hands)
            if (HasAttribute(SpellAttr1.UseAllMana))
            {
                if (optionalCost)
                    return null;

                // If power type - health drain all
                if (power.PowerType == PowerType.Health)
                    return new SpellPowerCost() { Power = PowerType.Health, Amount = (int)unitCaster.GetHealth() };

                // Else drain all power
                if (power.PowerType < PowerType.Max)
                    return new SpellPowerCost() { Power = power.PowerType, Amount = unitCaster.GetPower(power.PowerType) };

                Log.outError(LogFilter.Spells, $"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");
                return default;
            }

            // Base powerCost
            int powerCost = 0;
            if (!optionalCost)
            {
                powerCost = power.ManaCost;
                // PCT cost from total amount
                if (power.PowerCostPct != 0)
                {
                    switch (power.PowerType)
                    {
                        // health as power used
                        case PowerType.Health:
                            if (MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f))
                                powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetMaxHealth(), power.PowerCostMaxPct);
                            else
                                powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetMaxHealth(), power.PowerCostPct);
                            break;
                        case PowerType.Mana:
                            powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetCreateMana(), power.PowerCostPct);
                            break;
                        case PowerType.AlternatePower:
                            Log.outError(LogFilter.Spells, $"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");
                            return null;
                        default:
                        {
                            PowerTypeRecord powerTypeEntry = Global.DB2Mgr.GetPowerTypeEntry(power.PowerType);
                            if (powerTypeEntry != null)
                            {
                                powerCost += MathFunctions.CalculatePct(powerTypeEntry.MaxBasePower, power.PowerCostPct);
                                break;
                            }

                            Log.outError(LogFilter.Spells, $"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");
                            return null;
                        }
                    }
                }
            }
            else
            {
                powerCost = (int)power.OptionalCost;

                if (power.OptionalCostPct != 0)
                {
                    switch (power.PowerType)
                    {
                        // health as power used
                        case PowerType.Health:
                            powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetMaxHealth(), power.OptionalCostPct);
                            break;
                        case PowerType.Mana:
                            powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetCreateMana(), power.OptionalCostPct);
                            break;
                        case PowerType.AlternatePower:
                            Log.outError(LogFilter.Spells, $"SpellInfo::CalcPowerCost: Unsupported power type POWER_ALTERNATE_POWER in spell {Id} for optional cost percent");
                            return null;
                        default:
                        {
                            var powerTypeEntry = Global.DB2Mgr.GetPowerTypeEntry(power.PowerType);
                            if (powerTypeEntry != null)
                            {
                                powerCost += (int)MathFunctions.CalculatePct(powerTypeEntry.MaxBasePower, power.OptionalCostPct);
                                break;
                            }

                            Log.outError(LogFilter.Spells, $"SpellInfo::CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id} for optional cost percent");
                            return null;
                        }
                    }
                }

                powerCost += unitCaster.GetTotalAuraModifier(AuraType.ModAdditionalPowerCost, aurEff =>
                {
                    return aurEff.GetMiscValue() == (int)power.PowerType && aurEff.IsAffectingSpell(this);
                });
            }

            bool initiallyNegative = powerCost < 0;

            // Shiv - costs 20 + weaponSpeed*10 energy (apply only to non-triggered spell with energy cost)
            if (HasAttribute(SpellAttr4.WeaponSpeedCostScaling))
            {
                uint speed = 0;
                SpellShapeshiftFormRecord ss = CliDB.SpellShapeshiftFormStorage.LookupByKey(unitCaster.GetShapeshiftForm());
                if (ss != null)
                    speed = ss.CombatRoundTime;
                else
                {
                    WeaponAttackType slot = WeaponAttackType.BaseAttack;
                    if (!HasAttribute(SpellAttr3.RequiresMainHandWeapon) && HasAttribute(SpellAttr3.RequiresOffHandWeapon))
                        slot = WeaponAttackType.OffAttack;

                    speed = unitCaster.GetBaseAttackTime(slot);
                }

                powerCost += (int)speed / 100;
            }

            if (power.PowerType != PowerType.Health)
            {
                if (!optionalCost)
                {
                    // Flat mod from caster auras by spell school and power type
                    foreach (AuraEffect aura in unitCaster.GetAuraEffectsByType(AuraType.ModPowerCostSchool))
                    {
                        if ((aura.GetMiscValue() & (int)schoolMask) == 0)
                            continue;

                        if ((aura.GetMiscValueB() & (1 << (int)power.PowerType)) == 0)
                            continue;

                        powerCost += aura.GetAmount();
                    }
                }

                // PCT mod from user auras by spell school and power type
                foreach (var schoolCostPct in unitCaster.GetAuraEffectsByType(AuraType.ModPowerCostSchoolPct))
                {
                    if ((schoolCostPct.GetMiscValue() & (int)schoolMask) == 0)
                        continue;

                    if ((schoolCostPct.GetMiscValueB() & (1 << (int)power.PowerType)) == 0)
                        continue;

                    powerCost += MathFunctions.CalculatePct(powerCost, schoolCostPct.GetAmount());
                }
            }

            // Apply cost mod by spell
            Player modOwner = unitCaster.GetSpellModOwner();
            if (modOwner != null)
            {
                SpellModOp mod = SpellModOp.Max;
                switch (power.OrderIndex)
                {
                    case 0:
                        mod = SpellModOp.PowerCost0;
                        break;
                    case 1:
                        mod = SpellModOp.PowerCost1;
                        break;
                    case 2:
                        mod = SpellModOp.PowerCost2;
                        break;
                    default:
                        break;
                }

                if (mod != SpellModOp.Max)
                {
                    if (!optionalCost)
                        modOwner.ApplySpellMod(this, mod, ref powerCost, spell);
                    else
                    {
                        // optional cost ignores flat modifiers
                        int flatMod = 0;
                        float pctMod = 1.0f;
                        modOwner.GetSpellModValues(this, mod, spell, powerCost, ref flatMod, ref pctMod);
                        powerCost = (int)(powerCost * pctMod);
                    }
                }
            }

            if (!unitCaster.IsControlledByPlayer() && MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f) && SpellLevel != 0 && power.PowerType == PowerType.Mana)
            {
                if (HasAttribute(SpellAttr0.ScalesWithCreatureLevel))
                {
                    GtNpcManaCostScalerRecord spellScaler = CliDB.NpcManaCostScalerGameTable.GetRow(SpellLevel);
                    GtNpcManaCostScalerRecord casterScaler = CliDB.NpcManaCostScalerGameTable.GetRow(unitCaster.GetLevel());
                    if (spellScaler != null && casterScaler != null)
                        powerCost *= (int)(casterScaler.Scaler / spellScaler.Scaler);
                }
            }

            if (power.PowerType == PowerType.Mana)
                powerCost = (int)((float)powerCost * (1.0f + unitCaster.m_unitData.ManaCostMultiplier));

            // power cost cannot become negative if initially positive
            if (initiallyNegative != (powerCost < 0))
                powerCost = 0;

            return new SpellPowerCost() { Power = power.PowerType, Amount = powerCost };
        }

        public List<SpellPowerCost> CalcPowerCost(WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
        {
            List<SpellPowerCost> costs = new();
            if (caster.IsUnit())
            {
                SpellPowerCost getOrCreatePowerCost(PowerType powerType)
                {
                    var itr = costs.Find(cost => cost.Power == powerType);
                    if (itr != null)
                        return itr;

                    costs.Add(new SpellPowerCost() { Power = powerType, Amount = 0 });
                    return costs.Last();
                }

                foreach (SpellPowerRecord power in PowerCosts)
                {
                    if (power == null)
                        continue;

                    SpellPowerCost cost = CalcPowerCost(power, false, caster, schoolMask, spell);
                    if (cost != null)
                        getOrCreatePowerCost(cost.Power).Amount += cost.Amount;

                    SpellPowerCost optionalCost = CalcPowerCost(power, true, caster, schoolMask, spell);
                    if (optionalCost != null)
                    {
                        SpellPowerCost cost1 = getOrCreatePowerCost(optionalCost.Power);
                        int remainingPower = caster.ToUnit().GetPower(optionalCost.Power) - cost1.Amount;
                        if (remainingPower > 0)
                            cost1.Amount += Math.Min(optionalCost.Amount, remainingPower);
                    }
                }
            }

            return costs;
        }

        float CalcPPMHasteMod(SpellProcsPerMinuteModRecord mod, Unit caster)
        {
            float haste = caster.m_unitData.ModHaste;
            float rangedHaste = caster.m_unitData.ModRangedHaste;
            float spellHaste = caster.m_unitData.ModSpellHaste;
            float regenHaste = caster.m_unitData.ModHasteRegen;

            switch (mod.Param)
            {
                case 1:
                    return (1.0f / haste - 1.0f) * mod.Coeff;
                case 2:
                    return (1.0f / rangedHaste - 1.0f) * mod.Coeff;
                case 3:
                    return (1.0f / spellHaste - 1.0f) * mod.Coeff;
                case 4:
                    return (1.0f / regenHaste - 1.0f) * mod.Coeff;
                case 5:
                    return (1.0f / Math.Min(Math.Min(Math.Min(haste, rangedHaste), spellHaste), regenHaste) - 1.0f) * mod.Coeff;
                default:
                    break;
            }

            return 0.0f;
        }

        float CalcPPMCritMod(SpellProcsPerMinuteModRecord mod, Unit caster)
        {
            Player player = caster.ToPlayer();
            if (player == null)
                return 0.0f;

            float crit = player.m_activePlayerData.CritPercentage;
            float rangedCrit = player.m_activePlayerData.RangedCritPercentage;
            float spellCrit = player.m_activePlayerData.SpellCritPercentage;

            switch (mod.Param)
            {
                case 1:
                    return crit * mod.Coeff * 0.01f;
                case 2:
                    return rangedCrit * mod.Coeff * 0.01f;
                case 3:
                    return spellCrit * mod.Coeff * 0.01f;
                case 4:
                    return Math.Min(Math.Min(crit, rangedCrit), spellCrit) * mod.Coeff * 0.01f;
                default:
                    break;
            }

            return 0.0f;
        }

        float CalcPPMItemLevelMod(SpellProcsPerMinuteModRecord mod, int itemLevel)
        {
            if (itemLevel == mod.Param)
                return 0.0f;

            float itemLevelPoints = ItemEnchantmentManager.GetRandomPropertyPoints((uint)itemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
            float basePoints = ItemEnchantmentManager.GetRandomPropertyPoints(mod.Param, ItemQuality.Rare, InventoryType.Chest, 0);
            if (itemLevelPoints == basePoints)
                return 0.0f;

            return ((itemLevelPoints / basePoints) - 1.0f) * mod.Coeff;
        }

        public float CalcProcPPM(Unit caster, int itemLevel)
        {
            float ppm = ProcBasePPM;
            if (caster == null)
                return ppm;

            foreach (SpellProcsPerMinuteModRecord mod in ProcPPMMods)
            {
                switch (mod.Type)
                {
                    case SpellProcsPerMinuteModType.Haste:
                    {
                        ppm *= 1.0f + CalcPPMHasteMod(mod, caster);
                        break;
                    }
                    case SpellProcsPerMinuteModType.Crit:
                    {
                        ppm *= 1.0f + CalcPPMCritMod(mod, caster);
                        break;
                    }
                    case SpellProcsPerMinuteModType.Class:
                    {
                        if (caster.GetClassMask().HasAnyFlag(mod.Param))
                            ppm *= 1.0f + mod.Coeff;
                        break;
                    }
                    case SpellProcsPerMinuteModType.Spec:
                    {
                        Player plrCaster = caster.ToPlayer();
                        if (plrCaster != null)
                            if (plrCaster.GetPrimarySpecialization() == (ChrSpecialization)mod.Param)
                                ppm *= 1.0f + mod.Coeff;
                        break;
                    }
                    case SpellProcsPerMinuteModType.Race:
                    {
                        if ((caster.GetRaceMask() & mod.Param) != 0)
                            ppm *= 1.0f + mod.Coeff;
                        break;
                    }
                    case SpellProcsPerMinuteModType.ItemLevel:
                    {
                        ppm *= 1.0f + CalcPPMItemLevelMod(mod, itemLevel);
                        break;
                    }
                    case SpellProcsPerMinuteModType.Battleground:
                    {
                        if (caster.GetMap().IsBattlegroundOrArena())
                            ppm *= 1.0f + mod.Coeff;
                        break;
                    }
                    default:
                        break;
                }
            }

            return ppm;
        }

        public bool IsRanked()
        {
            return ChainEntry != null;
        }

        public byte GetRank()
        {
            if (ChainEntry == null)
                return 1;
            return ChainEntry.rank;
        }

        public SpellInfo GetFirstRankSpell()
        {
            if (ChainEntry == null)
                return this;
            return ChainEntry.first;
        }
        SpellInfo GetLastRankSpell()
        {
            if (ChainEntry == null)
                return null;
            return ChainEntry.last;
        }
        public SpellInfo GetNextRankSpell()
        {
            if (ChainEntry == null)
                return null;
            return ChainEntry.next;
        }
        SpellInfo GetPrevRankSpell()
        {
            if (ChainEntry == null)
                return null;
            return ChainEntry.prev;
        }

        public SpellInfo GetAuraRankForLevel(uint level)
        {
            // ignore passive spells
            if (IsPassive())
                return this;

            // Client ignores spell with these attributes (sub_53D9D0)
            if (HasAttribute(SpellAttr0.AuraIsDebuff) || HasAttribute(SpellAttr2.AllowLowLevelBuff) || HasAttribute(SpellAttr3.OnlyProcOnCaster))
                return this;

            bool needRankSelection = false;
            foreach (var effectInfo in GetEffects())
            {
                if (IsPositiveEffect(effectInfo.EffectIndex) &&
                    (effectInfo.IsEffect(SpellEffectName.ApplyAura) ||
                    effectInfo.IsEffect(SpellEffectName.ApplyAreaAuraParty) ||
                    effectInfo.IsEffect(SpellEffectName.ApplyAreaAuraRaid)) &&
                    effectInfo.Scaling.Coefficient != 0)
                {
                    needRankSelection = true;
                    break;
                }
            }

            // not required
            if (!needRankSelection)
                return this;

            for (SpellInfo nextSpellInfo = this; nextSpellInfo != null; nextSpellInfo = nextSpellInfo.GetPrevRankSpell())
            {
                // if found appropriate level
                if ((level + 10) >= nextSpellInfo.SpellLevel)
                    return nextSpellInfo;

                // one rank less then
            }

            // not found
            return null;
        }

        public bool IsRankOf(SpellInfo spellInfo)
        {
            return GetFirstRankSpell() == spellInfo.GetFirstRankSpell();
        }

        public bool IsDifferentRankOf(SpellInfo spellInfo)
        {
            if (Id == spellInfo.Id)
                return false;
            return IsRankOf(spellInfo);
        }

        public bool IsHighRankOf(SpellInfo spellInfo)
        {
            if (ChainEntry != null && spellInfo.ChainEntry != null)
            {
                if (ChainEntry.first == spellInfo.ChainEntry.first)
                    if (ChainEntry.rank > spellInfo.ChainEntry.rank)
                        return true;
            }
            return false;
        }

        public uint GetSpellXSpellVisualId(WorldObject caster = null, WorldObject viewer = null)
        {
            foreach (SpellXSpellVisualRecord visual in _visuals)
            {
                if (visual.CasterPlayerConditionID != 0)
                    if (caster == null || !caster.IsPlayer() || !ConditionManager.IsPlayerMeetingCondition(caster.ToPlayer(), visual.CasterPlayerConditionID))
                        continue;

                var unitCondition = CliDB.UnitConditionStorage.LookupByKey(visual.CasterUnitConditionID);
                if (unitCondition != null)
                    if (caster == null || !caster.IsUnit() || !ConditionManager.IsUnitMeetingCondition(caster.ToUnit(), viewer?.ToUnit(), unitCondition))
                        continue;

                return visual.Id;
            }

            return 0;
        }

        public uint GetSpellVisual(WorldObject caster = null, WorldObject viewer = null)
        {
            var visual = CliDB.SpellXSpellVisualStorage.LookupByKey(GetSpellXSpellVisualId(caster, viewer));
            if (visual != null)
            {
                //if (visual.LowViolenceSpellVisualID && forPlayer.GetViolenceLevel() operator 2)
                //    return visual.LowViolenceSpellVisualID;

                return visual.SpellVisualID;
            }

            return 0;
        }

        public void _InitializeExplicitTargetMask()
        {
            bool srcSet = false;
            bool dstSet = false;

            // prepare target mask using effect target entries
            foreach (var effectInfo in GetEffects())
            {
                if (!effectInfo.IsEffect())
                    continue;

                SpellCastTargetFlags targetMask = 0;
                targetMask |= effectInfo.TargetA.GetExplicitTargetMask(ref srcSet, ref dstSet);
                targetMask |= effectInfo.TargetB.GetExplicitTargetMask(ref srcSet, ref dstSet);

                // add explicit target flags based on spell effects which have SpellEffectImplicitTargetTypes.Explicit and no valid target provided
                if (effectInfo.GetImplicitTargetType() == SpellEffectImplicitTargetTypes.Explicit)
                {

                    // extend explicit target mask only if valid targets for effect could not be provided by target types
                    SpellCastTargetFlags effectTargetMask = effectInfo.GetMissingTargetMask(srcSet, dstSet, targetMask);

                    // don't add explicit object/dest flags when spell has no max range
                    if (GetMaxRange(true) == 0.0f && GetMaxRange(false) == 0.0f)
                        effectTargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.DestLocation);

                    targetMask |= effectTargetMask;
                }

                ExplicitTargetMask |= (uint)targetMask;
                if (!effectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.DontFailSpellOnTargetingFailure))
                    RequiredExplicitTargetMask |= (uint)targetMask;
            }

            ExplicitTargetMask |= (uint)Targets;
            if (!HasAttribute(SpellAttr13.DoNotFailIfNoTarget))
                RequiredExplicitTargetMask |= (uint)Targets;
        }

        public bool _isPositiveTarget(SpellEffectInfo effect)
        {
            if (!effect.IsEffect())
                return true;

            return effect.TargetA.GetCheckType() != SpellTargetCheckTypes.Enemy &&
                effect.TargetB.GetCheckType() != SpellTargetCheckTypes.Enemy;
        }

        bool _isPositiveEffectImpl(SpellInfo spellInfo, SpellEffectInfo effect, List<Tuple<SpellInfo, uint>> visited)
        {
            if (!effect.IsEffect())
                return true;

            // attribute may be already set in DB
            if (!spellInfo.IsPositiveEffect(effect.EffectIndex))
                return false;

            // passive auras like talents are all positive
            if (spellInfo.IsPassive())
                return true;

            // not found a single positive spell with this attribute
            if (spellInfo.HasAttribute(SpellAttr0.AuraIsDebuff))
                return false;

            if (spellInfo.HasAttribute(SpellAttr4.AuraIsBuff))
                return true;

            if (effect.EffectAttributes.HasFlag(SpellEffectAttributes.IsHarmful))
                return false;

            visited.Add(Tuple.Create(spellInfo, effect.EffectIndex));

            //We need scaling level info for some auras that compute bp 0 or positive but should be debuffs
            float bpScalePerLevel = effect.RealPointsPerLevel;
            int bp = effect.CalcValue();
            switch (spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    switch (spellInfo.Id)
                    {
                        case 40268: // Spiritual Vengeance, Teron Gorefiend, Black Temple
                        case 61987: // Avenging Wrath Marker
                        case 61988: // Divine Shield exclude aura
                        case 64412: // Phase Punch, Algalon the Observer, Ulduar
                        case 72410: // Rune of Blood, Saurfang, Icecrown Citadel
                        case 71204: // Touch of Insignificance, Lady Deathwhisper, Icecrown Citadel
                            return false;
                        case 24732: // Bat Costume
                        case 30877: // Tag Murloc
                        case 61716: // Rabbit Costume
                        case 61734: // Noblegarden Bunny
                        case 62344: // Fists of Stone
                        case 50344: // Dream Funnel
                        case 61819: // Manabonked! (item)
                        case 61834: // Manabonked! (minigob)
                        case 73523: // Rigor Mortis
                            return true;
                        default:
                            break;
                    }
                    break;
                case SpellFamilyNames.Rogue:
                    switch (spellInfo.Id)
                    {
                        // Envenom must be considered as a positive effect even though it deals damage
                        case 32645: // Envenom
                            return true;
                        case 40251: // Shadow of Death, Teron Gorefiend, Black Temple
                            return false;
                        default:
                            break;
                    }
                    break;
                case SpellFamilyNames.Warrior:
                    // Slam, Execute
                    if ((spellInfo.SpellFamilyFlags[0] & 0x20200000) != 0)
                        return false;
                    break;
                default:
                    break;
            }

            switch (spellInfo.Mechanic)
            {
                case Mechanics.ImmuneShield:
                    return true;
                default:
                    break;
            }

            // Special case: effects which determine positivity of whole spell
            if (spellInfo.HasAttribute(SpellAttr1.AuraUnique))
            {
                // check for targets, there seems to be an assortment of dummy triggering spells that should be negative
                foreach (var otherEffect in spellInfo.GetEffects())
                    if (!_isPositiveTarget(otherEffect))
                        return false;
            }

            foreach (var otherEffect in spellInfo.GetEffects())
            {
                switch (otherEffect.Effect)
                {
                    case SpellEffectName.Heal:
                    case SpellEffectName.LearnSpell:
                    case SpellEffectName.SkillStep:
                    case SpellEffectName.HealPct:
                        return true;
                    case SpellEffectName.Instakill:
                        if (otherEffect.EffectIndex != effect.EffectIndex && // for spells like 38044: instakill effect is negative but auras on target must count as buff
                            otherEffect.TargetA.GetTarget() == effect.TargetA.GetTarget() &&
                            otherEffect.TargetB.GetTarget() == effect.TargetB.GetTarget())
                            return false;
                        break;
                    default:
                        break;
                }

                if (otherEffect.IsAura())
                {
                    switch (otherEffect.ApplyAuraName)
                    {
                        case AuraType.ModStealth:
                        case AuraType.ModUnattackable:
                            return true;
                        case AuraType.SchoolHealAbsorb:
                        case AuraType.Empathy:
                        case AuraType.ModSpellDamageFromCaster:
                        case AuraType.PreventsFleeing:
                            return false;
                        default:
                            break;
                    }
                }
            }

            switch (effect.Effect)
            {
                case SpellEffectName.WeaponDamage:
                case SpellEffectName.WeaponDamageNoSchool:
                case SpellEffectName.NormalizedWeaponDmg:
                case SpellEffectName.WeaponPercentDamage:
                case SpellEffectName.SchoolDamage:
                case SpellEffectName.EnvironmentalDamage:
                case SpellEffectName.HealthLeech:
                case SpellEffectName.Instakill:
                case SpellEffectName.PowerDrain:
                case SpellEffectName.StealBeneficialBuff:
                case SpellEffectName.InterruptCast:
                case SpellEffectName.Pickpocket:
                case SpellEffectName.GameObjectDamage:
                case SpellEffectName.DurabilityDamage:
                case SpellEffectName.DurabilityDamagePct:
                case SpellEffectName.ApplyAreaAuraEnemy:
                case SpellEffectName.Tamecreature:
                case SpellEffectName.Distract:
                    return false;
                case SpellEffectName.Energize:
                case SpellEffectName.EnergizePct:
                case SpellEffectName.HealPct:
                case SpellEffectName.HealMaxHealth:
                case SpellEffectName.HealMechanical:
                    return true;
                case SpellEffectName.KnockBack:
                case SpellEffectName.Charge:
                case SpellEffectName.PersistentAreaAura:
                case SpellEffectName.AttackMe:
                case SpellEffectName.PowerBurn:
                    // check targets
                    if (!_isPositiveTarget(effect))
                        return false;
                    break;
                case SpellEffectName.Dispel:
                    // non-positive dispel
                    switch ((DispelType)effect.MiscValue)
                    {
                        case DispelType.Stealth:
                        case DispelType.Invisibility:
                        case DispelType.Enrage:
                            return false;
                        default:
                            break;
                    }

                    // also check targets
                    if (!_isPositiveTarget(effect))
                        return false;
                    break;
                case SpellEffectName.DispelMechanic:
                    if (!_isPositiveTarget(effect))
                    {
                        // non-positive mechanic dispel on negative target
                        switch ((Mechanics)effect.MiscValue)
                        {
                            case Mechanics.Bandage:
                            case Mechanics.Shield:
                            case Mechanics.Mount:
                            case Mechanics.Invulnerability:
                                return false;
                            default:
                                break;
                        }
                    }
                    break;
                case SpellEffectName.Threat:
                case SpellEffectName.ModifyThreatPercent:
                    // check targets AND basepoints
                    if (!_isPositiveTarget(effect) && bp > 0)
                        return false;
                    break;
                default:
                    break;
            }

            if (effect.IsAura())
            {
                // non-positive aura use
                switch (effect.ApplyAuraName)
                {
                    case AuraType.ModStat:                    // dependent from basepoint sign (negative -> negative)
                    case AuraType.ModSkill:
                    case AuraType.ModSkill2:
                    case AuraType.ModDodgePercent:
                    case AuraType.ModHealingDone:
                    case AuraType.ModDamageDoneCreature:
                    case AuraType.ObsModHealth:
                    case AuraType.ObsModPower:
                    case AuraType.ModCritPct:
                    case AuraType.ModHitChance:
                    case AuraType.ModSpellHitChance:
                    case AuraType.ModSpellCritChance:
                    case AuraType.ModRangedHaste:
                    case AuraType.ModMeleeRangedHaste:
                    case AuraType.ModCastingSpeedNotStack:
                    case AuraType.HasteSpells:
                    case AuraType.ModRecoveryRateBySpellLabel:
                    case AuraType.ModDetectRange:
                    case AuraType.ModIncreaseHealthPercent:
                    case AuraType.ModTotalStatPercentage:
                    case AuraType.ModIncreaseSwimSpeed:
                    case AuraType.ModPercentStat:
                    case AuraType.ModIncreaseHealth:
                    case AuraType.ModSpeedAlways:
                        if (bp < 0 || bpScalePerLevel < 0) //TODO: What if both are 0? Should it be a buff or debuff?
                            return false;
                        break;
                    case AuraType.ModAttackspeed:            // some buffs have negative bp, check both target and bp
                    case AuraType.ModMeleeHaste:
                    case AuraType.ModDamageDone:
                    case AuraType.ModResistance:
                    case AuraType.ModResistancePct:
                    case AuraType.ModRating:
                    case AuraType.ModAttackPower:
                    case AuraType.ModRangedAttackPower:
                    case AuraType.ModDamagePercentDone:
                    case AuraType.ModSpeedSlowAll:
                    case AuraType.MeleeSlow:
                    case AuraType.ModAttackPowerPct:
                    case AuraType.ModHealingDonePercent:
                    case AuraType.ModHealingPct:
                        if (!_isPositiveTarget(effect) || bp < 0)
                            return false;
                        break;
                    case AuraType.ModDamageTaken:           // dependent from basepoint sign (positive . negative)
                    case AuraType.ModMeleeDamageTaken:
                    case AuraType.ModMeleeDamageTakenPct:
                    case AuraType.ModPowerCostSchool:
                    case AuraType.ModPowerCostSchoolPct:
                    case AuraType.ModMechanicDamageTakenPercent:
                        if (bp > 0)
                            return false;
                        break;
                    case AuraType.ModDamagePercentTaken:   // check targets and basepoints (ex Recklessness)
                        if (!_isPositiveTarget(effect) && bp > 0)
                            return false;
                        break;
                    case AuraType.ModHealthRegenPercent:   // check targets and basepoints (target enemy and negative bp -> negative)
                        if (!_isPositiveTarget(effect) && bp < 0)
                            return false;
                        break;
                    case AuraType.AddTargetTrigger:
                        return true;
                    case AuraType.PeriodicTriggerSpellWithValue:
                    case AuraType.PeriodicTriggerSpellFromClient:
                        SpellInfo spellTriggeredProto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell, spellInfo.Difficulty);
                        if (spellTriggeredProto != null)
                        {
                            // negative targets of main spell return early
                            foreach (var spellTriggeredEffect in spellTriggeredProto.GetEffects())
                            {
                                // already seen this
                                if (visited.Contains(Tuple.Create(spellTriggeredProto, spellTriggeredEffect.EffectIndex)))
                                    continue;

                                if (!spellTriggeredEffect.IsEffect())
                                    continue;

                                // if non-positive trigger cast targeted to positive target this main cast is non-positive
                                // this will place this spell auras as debuffs
                                if (_isPositiveTarget(spellTriggeredEffect) && !_isPositiveEffectImpl(spellTriggeredProto, spellTriggeredEffect, visited))
                                    return false;
                            }
                        }
                        break;
                    case AuraType.PeriodicTriggerSpell:
                    case AuraType.ModStun:
                    case AuraType.Transform:
                    case AuraType.ModDecreaseSpeed:
                    case AuraType.ModFear:
                    case AuraType.ModTaunt:
                    // special auras: they may have non negative target but still need to be marked as debuff
                    // checked again after all effects (SpellInfo::_InitializeSpellPositivity)
                    case AuraType.ModPacify:
                    case AuraType.ModPacifySilence:
                    case AuraType.ModDisarm:
                    case AuraType.ModDisarmOffhand:
                    case AuraType.ModDisarmRanged:
                    case AuraType.ModCharm:
                    case AuraType.AoeCharm:
                    case AuraType.ModPossess:
                    case AuraType.ModLanguage:
                    case AuraType.DamageShield:
                    case AuraType.ProcTriggerSpell:
                    case AuraType.ModAttackerMeleeHitChance:
                    case AuraType.ModAttackerRangedHitChance:
                    case AuraType.ModAttackerSpellHitChance:
                    case AuraType.ModAttackerMeleeCritChance:
                    case AuraType.ModAttackerSpellAndWeaponCritChance:
                    case AuraType.Dummy:
                    case AuraType.PeriodicDummy:
                    case AuraType.ModHealing:
                    case AuraType.ModWeaponCritPercent:
                    case AuraType.PowerBurn:
                    case AuraType.ModCooldown:
                    case AuraType.ModChargeRecoveryByTypeMask:
                    case AuraType.ModIncreaseSpeed:
                    case AuraType.ModParryPercent:
                    case AuraType.SetVehicleId:
                    case AuraType.PeriodicEnergize:
                    case AuraType.EffectImmunity:
                    case AuraType.OverrideClassScripts:
                    case AuraType.ModShapeshift:
                    case AuraType.ModThreat:
                    case AuraType.ProcTriggerSpellWithValue:
                        // check target for positive and negative spells
                        if (!_isPositiveTarget(effect))
                            return false;
                        break;
                    case AuraType.ModConfuse:
                    case AuraType.ChannelDeathItem:
                    case AuraType.ModRoot:
                    case AuraType.ModRoot2:
                    case AuraType.ModSilence:
                    case AuraType.ModDetaunt:
                    case AuraType.Ghost:
                    case AuraType.ModLeech:
                    case AuraType.PeriodicManaLeech:
                    case AuraType.ModStalked:
                    case AuraType.PreventResurrection:
                    case AuraType.PeriodicDamage:
                    case AuraType.PeriodicWeaponPercentDamage:
                    case AuraType.PeriodicDamagePercent:
                    case AuraType.MeleeAttackPowerAttackerBonus:
                    case AuraType.RangedAttackPowerAttackerBonus:
                        return false;
                    case AuraType.MechanicImmunity:
                    {
                        // non-positive immunities
                        switch ((Mechanics)effect.MiscValue)
                        {
                            case Mechanics.Bandage:
                            case Mechanics.Shield:
                            case Mechanics.Mount:
                            case Mechanics.Invulnerability:
                                return false;
                            default:
                                break;
                        }
                        break;
                    }
                    case AuraType.AddFlatModifier:          // mods
                    case AuraType.AddPctModifier:
                    case AuraType.AddFlatModifierBySpellLabel:
                    case AuraType.AddPctModifierBySpellLabel:
                    {
                        switch ((SpellModOp)effect.MiscValue)
                        {
                            case SpellModOp.ChangeCastTime:        // dependent from basepoint sign (positive . negative)
                            case SpellModOp.Period:
                            case SpellModOp.PowerCostOnMiss:
                            case SpellModOp.StartCooldown:
                                if (bp > 0)
                                    return false;
                                break;
                            case SpellModOp.Cooldown:
                            case SpellModOp.PowerCost0:
                            case SpellModOp.PowerCost1:
                            case SpellModOp.PowerCost2:
                                if (!spellInfo.IsPositive() && bp > 0) // dependent on prev effects too (ex Arcane Power)
                                    return false;
                                break;
                            case SpellModOp.PointsIndex0:          // always positive
                            case SpellModOp.PointsIndex1:
                            case SpellModOp.PointsIndex2:
                            case SpellModOp.PointsIndex3:
                            case SpellModOp.PointsIndex4:
                            case SpellModOp.Points:
                            case SpellModOp.Hate:
                            case SpellModOp.ChainAmplitude:
                            case SpellModOp.Amplitude:
                                return true;
                            case SpellModOp.Duration:
                            case SpellModOp.CritChance:
                            case SpellModOp.HealingAndDamage:
                            case SpellModOp.ChainTargets:
                                if (!spellInfo.IsPositive() && bp < 0) // dependent on prev effects too
                                    return false;
                                break;
                            default:                                // dependent from basepoint sign (negative . negative)
                                if (bp < 0)
                                    return false;
                                break;
                        }
                        break;
                    }
                    default:
                        break;
                }
            }

            // negative spell if triggered spell is negative
            if (effect.ApplyAuraName == 0 && effect.TriggerSpell != 0)
            {
                SpellInfo spellTriggeredProto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell, spellInfo.Difficulty);
                if (spellTriggeredProto != null)
                {
                    // spells with at least one negative effect are considered negative
                    // some self-applied spells have negative effects but in self casting case negative check ignored.
                    foreach (var spellTriggeredEffect in spellTriggeredProto.GetEffects())
                    {
                        // already seen this
                        if (visited.Contains(Tuple.Create(spellTriggeredProto, spellTriggeredEffect.EffectIndex)))
                            continue;

                        if (!spellTriggeredEffect.IsEffect())
                            continue;

                        if (!_isPositiveEffectImpl(spellTriggeredProto, spellTriggeredEffect, visited))
                            return false;
                    }
                }
            }

            // ok, positive
            return true;
        }

        public void InitializeSpellPositivity()
        {
            List<Tuple<SpellInfo, uint>> visited = new();

            foreach (SpellEffectInfo effect in GetEffects())
                if (!_isPositiveEffectImpl(this, effect, visited))
                    NegativeEffects[(int)effect.EffectIndex] = true;


            // additional checks after effects marked
            foreach (var spellEffectInfo in GetEffects())
            {
                if (!spellEffectInfo.IsEffect() || !IsPositiveEffect(spellEffectInfo.EffectIndex))
                    continue;

                switch (spellEffectInfo.ApplyAuraName)
                {
                    // has other non positive effect?
                    // then it should be marked negative if has same target as negative effect (ex 8510, 8511, 8893, 10267)
                    case AuraType.Dummy:
                    case AuraType.ModStun:
                    case AuraType.ModFear:
                    case AuraType.ModTaunt:
                    case AuraType.Transform:
                    case AuraType.ModAttackspeed:
                    case AuraType.ModDecreaseSpeed:
                    {
                        for (uint j = spellEffectInfo.EffectIndex + 1; j < GetEffects().Count; ++j)
                            if (!IsPositiveEffect(j)
                                && spellEffectInfo.TargetA.GetTarget() == GetEffect(j).TargetA.GetTarget()
                                && spellEffectInfo.TargetB.GetTarget() == GetEffect(j).TargetB.GetTarget())
                                NegativeEffects[(int)spellEffectInfo.EffectIndex] = true;
                        break;
                    }
                    default:
                        break;
                }
            }
        }

        public void _UnloadImplicitTargetConditionLists()
        {
            // find the same instances of ConditionList and delete them.
            foreach (SpellEffectInfo effect in _effects)
                effect.ImplicitTargetConditions = null;
        }

        public bool MeetsFutureSpellPlayerCondition(Player player)
        {
            if (ShowFutureSpellPlayerConditionID == 0)
                return false;

            return ConditionManager.IsPlayerMeetingCondition(player, ShowFutureSpellPlayerConditionID);
        }

        public bool HasLabel(uint labelId)
        {
            return Labels.Contains(labelId);
        }

        public static SpellCastTargetFlags GetTargetFlagMask(SpellTargetObjectTypes objType)
        {
            switch (objType)
            {
                case SpellTargetObjectTypes.Dest:
                    return SpellCastTargetFlags.DestLocation;
                case SpellTargetObjectTypes.UnitAndDest:
                    return SpellCastTargetFlags.DestLocation | SpellCastTargetFlags.Unit;
                case SpellTargetObjectTypes.CorpseAlly:
                    return SpellCastTargetFlags.CorpseAlly;
                case SpellTargetObjectTypes.CorpseEnemy:
                    return SpellCastTargetFlags.CorpseEnemy;
                case SpellTargetObjectTypes.Corpse:
                    return SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy;
                case SpellTargetObjectTypes.Unit:
                    return SpellCastTargetFlags.Unit;
                case SpellTargetObjectTypes.Gobj:
                    return SpellCastTargetFlags.Gameobject;
                case SpellTargetObjectTypes.GobjItem:
                    return SpellCastTargetFlags.GameobjectItem;
                case SpellTargetObjectTypes.Item:
                    return SpellCastTargetFlags.Item;
                case SpellTargetObjectTypes.Src:
                    return SpellCastTargetFlags.SourceLocation;
                default:
                    return SpellCastTargetFlags.None;
            }
        }

        public uint GetCategory()
        {
            return CategoryId;
        }

        public List<SpellEffectInfo> GetEffects() { return _effects; }

        public SpellEffectInfo GetEffect(uint index) { return _effects[(int)index]; }

        public bool HasTargetType(Targets target)
        {
            foreach (var effectInfo in _effects)
                if (effectInfo.TargetA.GetTarget() == target || effectInfo.TargetB.GetTarget() == target)
                    return true;

            return false;
        }

        public List<SpellXSpellVisualRecord> GetSpellVisuals()
        {
            return _visuals;
        }

        public bool HasAttribute(SpellAttr0 attribute) { return (Attributes & attribute) != 0; }
        public bool HasAttribute(SpellAttr1 attribute) { return (AttributesEx & attribute) != 0; }
        public bool HasAttribute(SpellAttr2 attribute) { return (AttributesEx2 & attribute) != 0; }
        public bool HasAttribute(SpellAttr3 attribute) { return (AttributesEx3 & attribute) != 0; }
        public bool HasAttribute(SpellAttr4 attribute) { return (AttributesEx4 & attribute) != 0; }
        public bool HasAttribute(SpellAttr5 attribute) { return (AttributesEx5 & attribute) != 0; }
        public bool HasAttribute(SpellAttr6 attribute) { return (AttributesEx6 & attribute) != 0; }
        public bool HasAttribute(SpellAttr7 attribute) { return (AttributesEx7 & attribute) != 0; }
        public bool HasAttribute(SpellAttr8 attribute) { return (AttributesEx8 & attribute) != 0; }
        public bool HasAttribute(SpellAttr9 attribute) { return (AttributesEx9 & attribute) != 0; }
        public bool HasAttribute(SpellAttr10 attribute) { return (AttributesEx10 & attribute) != 0; }
        public bool HasAttribute(SpellAttr11 attribute) { return (AttributesEx11 & attribute) != 0; }
        public bool HasAttribute(SpellAttr12 attribute) { return (AttributesEx12 & attribute) != 0; }
        public bool HasAttribute(SpellAttr13 attribute) { return (AttributesEx13 & attribute) != 0; }
        public bool HasAttribute(SpellAttr14 attribute) { return (AttributesEx14 & attribute) != 0; }
        public bool HasAttribute(SpellAttr15 attribute) { return (AttributesEx15 & attribute) != 0; }
        public bool HasAttribute(SpellCustomAttributes attribute) { return (AttributesCu & attribute) != 0; }

        public bool CanBeInterrupted(WorldObject interruptCaster, Unit interruptTarget, bool ignoreImmunity = false)
        {
            return HasAttribute(SpellAttr7.NoUiNotInterruptible)
                || HasChannelInterruptFlag(SpellAuraInterruptFlags.Damage | SpellAuraInterruptFlags.EnteringCombat)
                || (interruptTarget.IsPlayer() && InterruptFlags.HasFlag(SpellInterruptFlags.DamageCancelsPlayerOnly))
                || InterruptFlags.HasFlag(SpellInterruptFlags.DamageCancels)
                || (interruptCaster != null && interruptCaster.IsUnit() && interruptCaster.ToUnit().HasAuraTypeWithMiscvalue(AuraType.AllowInterruptSpell, (int)Id))
                || (((interruptTarget.GetMechanicImmunityMask() & (1 << (int)Mechanics.Interrupt)) == 0 || ignoreImmunity)
                    && !interruptTarget.HasAuraTypeWithAffectMask(AuraType.PreventInterrupt, this)
                    && PreventionType.HasAnyFlag(SpellPreventionType.Silence));
        }

        public bool HasAnyAuraInterruptFlag() { return AuraInterruptFlags != SpellAuraInterruptFlags.None || AuraInterruptFlags2 != SpellAuraInterruptFlags2.None; }
        public bool HasAuraInterruptFlag(SpellAuraInterruptFlags flag) { return AuraInterruptFlags.HasAnyFlag(flag); }
        public bool HasAuraInterruptFlag(SpellAuraInterruptFlags2 flag) { return AuraInterruptFlags2.HasAnyFlag(flag); }

        public bool HasChannelInterruptFlag(SpellAuraInterruptFlags flag) { return ChannelInterruptFlags.HasAnyFlag(flag); }
        public bool HasChannelInterruptFlag(SpellAuraInterruptFlags2 flag) { return ChannelInterruptFlags2.HasAnyFlag(flag); }

        #region Fields
        public uint Id { get; set; }
        public Difficulty Difficulty { get; set; }
        public uint CategoryId { get; set; }
        public DispelType Dispel { get; set; }
        public Mechanics Mechanic { get; set; }
        public SpellAttr0 Attributes { get; set; }
        public SpellAttr1 AttributesEx { get; set; }
        public SpellAttr2 AttributesEx2 { get; set; }
        public SpellAttr3 AttributesEx3 { get; set; }
        public SpellAttr4 AttributesEx4 { get; set; }
        public SpellAttr5 AttributesEx5 { get; set; }
        public SpellAttr6 AttributesEx6 { get; set; }
        public SpellAttr7 AttributesEx7 { get; set; }
        public SpellAttr8 AttributesEx8 { get; set; }
        public SpellAttr9 AttributesEx9 { get; set; }
        public SpellAttr10 AttributesEx10 { get; set; }
        public SpellAttr11 AttributesEx11 { get; set; }
        public SpellAttr12 AttributesEx12 { get; set; }
        public SpellAttr13 AttributesEx13 { get; set; }
        public SpellAttr14 AttributesEx14 { get; set; }
        public SpellAttr15 AttributesEx15 { get; set; }
        public SpellCustomAttributes AttributesCu { get; set; }
        public BitSet NegativeEffects { get; set; } = new BitSet(SpellConst.MaxEffects);
        public ulong Stances { get; set; }
        public ulong StancesNot { get; set; }
        public SpellCastTargetFlags Targets { get; set; }
        public uint TargetCreatureType { get; set; }
        public uint RequiresSpellFocus { get; set; }
        public uint FacingCasterFlags { get; set; }
        public AuraStateType CasterAuraState { get; set; }
        public AuraStateType TargetAuraState { get; set; }
        public AuraStateType ExcludeCasterAuraState { get; set; }
        public AuraStateType ExcludeTargetAuraState { get; set; }
        public uint CasterAuraSpell { get; set; }
        public uint TargetAuraSpell { get; set; }
        public uint ExcludeCasterAuraSpell { get; set; }
        public uint ExcludeTargetAuraSpell { get; set; }
        public AuraType CasterAuraType { get; set; }
        public AuraType TargetAuraType { get; set; }
        public AuraType ExcludeCasterAuraType { get; set; }
        public AuraType ExcludeTargetAuraType { get; set; }
        public SpellCastTimesRecord CastTimeEntry { get; set; }
        public uint RecoveryTime { get; set; }
        public uint CategoryRecoveryTime { get; set; }
        public uint StartRecoveryCategory { get; set; }
        public uint StartRecoveryTime { get; set; }
        public uint CooldownAuraSpellId { get; set; }
        public SpellInterruptFlags InterruptFlags { get; set; }
        public SpellAuraInterruptFlags AuraInterruptFlags { get; set; }
        public SpellAuraInterruptFlags2 AuraInterruptFlags2 { get; set; }
        public SpellAuraInterruptFlags ChannelInterruptFlags { get; set; }
        public SpellAuraInterruptFlags2 ChannelInterruptFlags2 { get; set; }
        public ProcFlagsInit ProcFlags { get; set; }
        public uint ProcChance { get; set; }
        public uint ProcCharges { get; set; }
        public uint ProcCooldown { get; set; }
        public float ProcBasePPM { get; set; }
        List<SpellProcsPerMinuteModRecord> ProcPPMMods = new();
        public uint MaxLevel { get; set; }
        public uint BaseLevel { get; set; }
        public uint SpellLevel { get; set; }
        public SpellDurationRecord DurationEntry { get; set; }
        public SpellPowerRecord[] PowerCosts = new SpellPowerRecord[SpellConst.MaxPowersPerSpell];
        public SpellRangeRecord RangeEntry { get; set; }
        public float Speed { get; set; }
        public float LaunchDelay { get; set; }
        public uint StackAmount { get; set; }
        public uint[] Totem = new uint[SpellConst.MaxTotems];
        public uint[] TotemCategory = new uint[SpellConst.MaxTotems];
        public int[] Reagent = new int[SpellConst.MaxReagents];
        public uint[] ReagentCount = new uint[SpellConst.MaxReagents];
        public List<SpellReagentsCurrencyRecord> ReagentsCurrency = new();
        public ItemClass EquippedItemClass { get; set; }
        public int EquippedItemSubClassMask { get; set; }
        public int EquippedItemInventoryTypeMask { get; set; }
        public uint IconFileDataId { get; set; }
        public uint ActiveIconFileDataId { get; set; }
        public uint ContentTuningId { get; set; }
        public uint ShowFutureSpellPlayerConditionID { get; set; }
        public LocalizedString SpellName { get; set; }
        public float ConeAngle { get; set; }
        public float Width { get; set; }
        public uint MaxTargetLevel { get; set; }
        public uint MaxAffectedTargets { get; set; }
        public SpellFamilyNames SpellFamilyName { get; set; }
        public FlagArray128 SpellFamilyFlags { get; set; }
        public SpellDmgClass DmgClass { get; set; }
        public SpellPreventionType PreventionType { get; set; }
        public int RequiredAreasID { get; set; }
        public SpellSchoolMask SchoolMask { get; set; }
        public uint ChargeCategoryId;
        public List<uint> Labels = new();
        public List<TimeSpan> EmpowerStageThresholds = new();

        // SpellScalingEntry
        public ScalingInfo Scaling;
        public uint ExplicitTargetMask { get; set; }
        public uint RequiredExplicitTargetMask { get; set; }
        public SpellChainNode ChainEntry { get; set; }

        public SqrtDamageAndHealingDiminishingStruct SqrtDamageAndHealingDiminishing;

        List<SpellEffectInfo> _effects = new();
        List<SpellXSpellVisualRecord> _visuals = new();
        SpellSpecificType _spellSpecific;
        AuraStateType _auraState;

        SpellDiminishInfo _diminishInfo;
        ulong _allowedMechanicMask;
        #endregion

        public struct ScalingInfo
        {
            public uint MinScalingLevel;
            public uint MaxScalingLevel;
            public uint ScalesFromItemLevel;
        }

        public struct SqrtDamageAndHealingDiminishingStruct
        {
            public int MaxTargets;               // The amount of targets after the damage decreases by the Square Root AOE formula
            public int NumNonDiminishedTargets;  // The amount of targets that still take the full amount before the damage decreases by the Square Root AOE formula
        }
    }

    public class SpellEffectInfo
    {
        public SpellEffectInfo(SpellInfo spellInfo, SpellEffectRecord effect = null)
        {
            _spellInfo = spellInfo;
            if (effect != null)
            {
                EffectIndex = (uint)effect.EffectIndex;
                Effect = (SpellEffectName)effect.Effect;
                ApplyAuraName = (AuraType)effect.EffectAura;
                ApplyAuraPeriod = effect.EffectAuraPeriod;
                BasePoints = (int)effect.EffectBasePoints;
                RealPointsPerLevel = effect.EffectRealPointsPerLevel;
                PointsPerResource = effect.EffectPointsPerResource;
                Amplitude = effect.EffectAmplitude;
                ChainAmplitude = effect.EffectChainAmplitude;
                BonusCoefficient = effect.EffectBonusCoefficient;
                MiscValue = effect.EffectMiscValue[0];
                MiscValueB = effect.EffectMiscValue[1];
                Mechanic = (Mechanics)effect.EffectMechanic;
                PositionFacing = effect.EffectPosFacing;
                TargetA = new SpellImplicitTargetInfo((Targets)effect.ImplicitTarget[0]);
                TargetB = new SpellImplicitTargetInfo((Targets)effect.ImplicitTarget[1]);
                TargetARadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(effect.EffectRadiusIndex[0]);
                TargetBRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(effect.EffectRadiusIndex[1]);
                ChainTargets = effect.EffectChainTargets;
                ItemType = effect.EffectItemType;
                TriggerSpell = effect.EffectTriggerSpell;
                SpellClassMask = effect.EffectSpellClassMask;
                BonusCoefficientFromAP = effect.BonusCoefficientFromAP;
                Scaling.Class = effect.ScalingClass;
                Scaling.Coefficient = effect.Coefficient;
                Scaling.Variance = effect.Variance;
                Scaling.ResourceCoefficient = effect.ResourceCoefficient;
                EffectAttributes = effect.EffectAttributes;
            }

            ImplicitTargetConditions = null;

            _immunityInfo = new ImmunityInfo();
        }

        public bool IsEffect()
        {
            return Effect != 0;
        }

        public bool IsEffect(SpellEffectName effectName)
        {
            return Effect == effectName;
        }

        public bool IsAura()
        {
            return (IsUnitOwnedAuraEffect() || Effect == SpellEffectName.PersistentAreaAura) && ApplyAuraName != 0;
        }

        public bool IsAura(AuraType aura)
        {
            return IsAura() && ApplyAuraName == aura;
        }

        public bool IsTargetingArea()
        {
            return TargetA.IsArea() || TargetB.IsArea();
        }

        public bool IsAreaAuraEffect()
        {
            if (Effect == SpellEffectName.ApplyAreaAuraParty ||
                Effect == SpellEffectName.ApplyAreaAuraRaid ||
                Effect == SpellEffectName.ApplyAreaAuraFriend ||
                Effect == SpellEffectName.ApplyAreaAuraEnemy ||
                Effect == SpellEffectName.ApplyAreaAuraPet ||
                Effect == SpellEffectName.ApplyAreaAuraOwner ||
                Effect == SpellEffectName.ApplyAreaAuraSummons ||
                Effect == SpellEffectName.ApplyAreaAuraPartyNonrandom)
                return true;
            return false;
        }

        public bool IsUnitOwnedAuraEffect()
        {
            return IsAreaAuraEffect() || Effect == SpellEffectName.ApplyAura || Effect == SpellEffectName.ApplyAuraOnPet;
        }

        public int CalcValue(WorldObject caster = null, int? bp = null, Unit target = null, uint castItemId = 0, int itemLevel = -1)
        {
            return CalcValue(out _, caster, bp, target, castItemId, itemLevel);
        }

        public int CalcValue(out float variance, WorldObject caster = null, int? bp = null, Unit target = null, uint castItemId = 0, int itemLevel = -1)
        {
            variance = 0.0f;
            double basePointsPerLevel = RealPointsPerLevel;
            // TODO: this needs to be a float, not rounded
            int basePoints = CalcBaseValue(caster, target, castItemId, itemLevel);
            double value = bp.HasValue ? bp.Value : basePoints;
            double comboDamage = PointsPerResource;

            Unit casterUnit = null;
            if (caster != null)
                casterUnit = caster.ToUnit();

            if (Scaling.Variance != 0)
            {
                float delta = Math.Abs(Scaling.Variance * 0.5f);
                double valueVariance = RandomHelper.FRand(-delta, delta);
                value += (double)basePoints * valueVariance;
                variance = (float)valueVariance;
            }

            // base amount modification based on spell lvl vs caster lvl
            if (Scaling.Coefficient != 0.0f)
            {
                if (Scaling.ResourceCoefficient != 0)
                    comboDamage = Scaling.ResourceCoefficient * value;
            }
            else if (GetScalingExpectedStat() == ExpectedStatType.None)
            {
                if (casterUnit != null && basePointsPerLevel != 0.0f)
                {
                    int level = (int)casterUnit.GetLevel();
                    if (level > (int)_spellInfo.MaxLevel && _spellInfo.MaxLevel > 0)
                        level = (int)_spellInfo.MaxLevel;

                    // if base level is greater than spell level, reduce by base level (eg. pilgrims foods)
                    level -= (int)Math.Max(_spellInfo.BaseLevel, _spellInfo.SpellLevel);
                    if (level < 0)
                        level = 0;
                    value += level * basePointsPerLevel;
                }
            }

            // random damage
            if (casterUnit != null)
            {
                // bonus amount from combo points
                if (comboDamage != 0)
                {
                    int comboPoints = casterUnit.GetPower(PowerType.ComboPoints);
                    if (comboPoints != 0)
                        value += comboDamage * comboPoints;
                }
            }

            if (_spellInfo.HasAttribute(SpellAttr8.MasteryAffectsPoints))
            {
                Player playerCaster = caster?.ToPlayer();
                if (playerCaster != null)
                    value += playerCaster.m_activePlayerData.Mastery * BonusCoefficient;
            }

            if (caster != null)
                value = caster.ApplyEffectModifiers(_spellInfo, EffectIndex, value);


            return (int)Math.Round(value);
        }

        public int CalcBaseValue(WorldObject caster, Unit target, uint itemId, int itemLevel)
        {
            if (Scaling.Coefficient != 0.0f)
            {
                uint level = _spellInfo.SpellLevel;
                if (target != null && _spellInfo.HasAttribute(SpellAttr8.UseTargetsLevelForSpellScaling))
                    level = target.GetLevel();
                else if (caster != null && caster.IsUnit())
                    level = caster.ToUnit().GetLevel();

                if (_spellInfo.BaseLevel != 0 && !_spellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel) && _spellInfo.HasAttribute(SpellAttr10.UseSpellBaseLevelForScaling))
                    level = _spellInfo.BaseLevel;

                if (_spellInfo.Scaling.MinScalingLevel != 0 && _spellInfo.Scaling.MinScalingLevel > level)
                    level = _spellInfo.Scaling.MinScalingLevel;

                if (_spellInfo.Scaling.MaxScalingLevel != 0 && _spellInfo.Scaling.MaxScalingLevel < level)
                    level = _spellInfo.Scaling.MaxScalingLevel;

                float tempValue = 0.0f;
                if (level > 0)
                {
                    if (Scaling.Class == 0)
                        return 0;

                    uint effectiveItemLevel = itemLevel != -1 ? (uint)itemLevel : 1u;
                    if (_spellInfo.Scaling.ScalesFromItemLevel != 0 || _spellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel))
                    {
                        if (_spellInfo.Scaling.ScalesFromItemLevel != 0)
                            effectiveItemLevel = _spellInfo.Scaling.ScalesFromItemLevel;

                        if (Scaling.Class == -8 || Scaling.Class == -9)
                        {
                            RandPropPointsRecord randPropPoints = CliDB.RandPropPointsStorage.LookupByKey(effectiveItemLevel);
                            if (randPropPoints == null)
                                randPropPoints = CliDB.RandPropPointsStorage.LookupByKey(CliDB.RandPropPointsStorage.GetNumRows() - 1);

                            tempValue = Scaling.Class == -8 ? randPropPoints.DamageReplaceStatF : randPropPoints.DamageSecondaryF;
                        }
                        else
                            tempValue = ItemEnchantmentManager.GetRandomPropertyPoints(effectiveItemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
                    }
                    else
                        tempValue = CliDB.GetSpellScalingColumnForClass(CliDB.SpellScalingGameTable.GetRow(level), Scaling.Class);

                    if (Scaling.Class == -7)
                    {
                        GtGenericMultByILvlRecord ratingMult = CliDB.CombatRatingsMultByILvlGameTable.GetRow(effectiveItemLevel);
                        if (ratingMult != null)
                        {
                            ItemSparseRecord itemSparse = CliDB.ItemSparseStorage.LookupByKey(itemId);
                            if (itemSparse != null)
                                tempValue *= CliDB.GetIlvlStatMultiplier(ratingMult, itemSparse.inventoryType);
                        }
                    }

                    if (Scaling.Class == -6)
                    {
                        GtGenericMultByILvlRecord staminaMult = CliDB.StaminaMultByILvlGameTable.GetRow(effectiveItemLevel);
                        if (staminaMult != null)
                        {
                            ItemSparseRecord itemSparse = CliDB.ItemSparseStorage.LookupByKey(itemId);
                            if (itemSparse != null)
                                tempValue *= CliDB.GetIlvlStatMultiplier(staminaMult, itemSparse.inventoryType);
                        }
                    }
                }

                tempValue *= Scaling.Coefficient;
                if (tempValue > 0.0f && tempValue < 1.0f)
                    tempValue = 1.0f;

                return (int)Math.Round(tempValue);
            }
            else
            {
                float tempValue = BasePoints;
                ExpectedStatType stat = GetScalingExpectedStat();
                if (stat != ExpectedStatType.None)
                {
                    if (_spellInfo.HasAttribute(SpellAttr0.ScalesWithCreatureLevel))
                        stat = ExpectedStatType.CreatureAutoAttackDps;

                    // TODO - add expansion and content tuning id args?
                    uint contentTuningId = _spellInfo.ContentTuningId; // content tuning should be passed as arg, the one stored in SpellInfo is fallback
                    int expansion = -2;
                    ContentTuningRecord contentTuning = CliDB.ContentTuningStorage.LookupByKey(contentTuningId);
                    if (contentTuning != null)
                        expansion = contentTuning.ExpansionID;

                    uint level = 1;
                    if (target != null && _spellInfo.HasAttribute(SpellAttr8.UseTargetsLevelForSpellScaling))
                        level = target.GetLevel();
                    else if (caster != null && caster.IsUnit())
                        level = caster.ToUnit().GetLevel();

                    tempValue = Global.DB2Mgr.EvaluateExpectedStat(stat, level, expansion, 0, Class.None, 0) * BasePoints / 100.0f;
                }

                return (int)Math.Round(tempValue);
            }
        }

        public float CalcValueMultiplier(WorldObject caster, Spell spell = null)
        {
            float multiplier = Amplitude;
            Player modOwner = (caster != null ? caster.GetSpellModOwner() : null);
            if (modOwner != null)
                modOwner.ApplySpellMod(_spellInfo, SpellModOp.Amplitude, ref multiplier, spell);
            return multiplier;
        }

        public float CalcDamageMultiplier(WorldObject caster, Spell spell = null)
        {
            float multiplierPercent = ChainAmplitude * 100.0f;
            Player modOwner = (caster != null ? caster.GetSpellModOwner() : null);
            if (modOwner != null)
                modOwner.ApplySpellMod(_spellInfo, SpellModOp.ChainAmplitude, ref multiplierPercent, spell);
            return multiplierPercent / 100.0f;
        }

        public bool HasRadius(SpellTargetIndex targetIndex)
        {
            switch (targetIndex)
            {
                case SpellTargetIndex.TargetA:
                    return TargetARadiusEntry != null;
                case SpellTargetIndex.TargetB:
                    return TargetBRadiusEntry != null;
                default:
                    return false;
            }
        }

        public float CalcRadius(WorldObject caster = null, SpellTargetIndex targetIndex = SpellTargetIndex.TargetA, Spell spell = null)
        {
            // TargetA -> TargetARadiusEntry
            // TargetB -> TargetBRadiusEntry
            // Aura effects have TargetARadiusEntry == TargetBRadiusEntry (mostly)
            SpellImplicitTargetInfo target = TargetA;
            var entry = TargetARadiusEntry;
            if (targetIndex == SpellTargetIndex.TargetB && HasRadius(targetIndex))
            {
                target = TargetB;
                entry = TargetBRadiusEntry;
            }

            if (entry == null)
                return 0.0f;

            float radius = entry.RadiusMin;

            // Random targets use random value between RadiusMin and RadiusMax
            // For other cases, client uses RadiusMax if RadiusMin is 0
            if (target.GetTarget() == Targets.DestCasterRandom ||
                target.GetTarget() == Targets.DestTargetRandom ||
                target.GetTarget() == Targets.DestDestRandom)
                radius += (entry.RadiusMax - radius) * RandomHelper.NextSingle();
            else if (radius == 0.0f)
                radius = entry.RadiusMax;

            if (caster != null)
            {
                Unit casterUnit = caster.ToUnit();
                if (casterUnit != null)
                    radius += entry.RadiusPerLevel * casterUnit.GetLevel();

                radius = Math.Min(radius, entry.RadiusMax);
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(_spellInfo, SpellModOp.Radius, ref radius, spell);

                if (!_spellInfo.HasAttribute(SpellAttr9.NoMovementRadiusBonus))
                    if (casterUnit != null && Spell.CanIncreaseRangeByMovement(casterUnit))
                        radius += 2.0f;
            }

            return radius;
        }

        public (float, float) CalcRadiusBounds(WorldObject caster, SpellTargetIndex targetIndex, Spell spell)
        {
            // TargetA -> TargetARadiusEntry
            // TargetB -> TargetBRadiusEntry
            // Aura effects have TargetARadiusEntry == TargetBRadiusEntry (mostly)
            SpellRadiusRecord entry = TargetARadiusEntry;
            if (targetIndex == SpellTargetIndex.TargetB && HasRadius(targetIndex))
                entry = TargetBRadiusEntry;

            (float, float) bounds = default;
            if (entry == null)
                return bounds;

            bounds = (entry.RadiusMin, entry.RadiusMax);

            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(_spellInfo, SpellModOp.Radius, ref bounds.Item2, spell);

                if (!_spellInfo.HasAttribute(SpellAttr9.NoMovementRadiusBonus))
                {
                    Unit casterUnit = caster.ToUnit(); ;
                    if (casterUnit != null && Spell.CanIncreaseRangeByMovement(casterUnit))
                    {
                        bounds.Item1 = Math.Max(bounds.Item1 - 2.0f, 0.0f);
                        bounds.Item2 += 2.0f;
                    }
                }
            }

            return bounds;
        }

        public SpellCastTargetFlags GetProvidedTargetMask()
        {
            return SpellInfo.GetTargetFlagMask(TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(TargetB.GetObjectType());
        }

        public SpellCastTargetFlags GetMissingTargetMask(bool srcSet = false, bool dstSet = false, SpellCastTargetFlags mask = 0)
        {
            var effImplicitTargetMask = SpellInfo.GetTargetFlagMask(GetUsedTargetObjectType());
            SpellCastTargetFlags providedTargetMask = GetProvidedTargetMask() | mask;

            // remove all flags covered by effect target mask
            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.UnitMask))
                effImplicitTargetMask &= ~SpellCastTargetFlags.UnitMask;
            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.CorpseMask))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask);
            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.GameobjectItem))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.GameobjectItem | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.Item);
            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.Gameobject))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem);
            if (Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.Item))
                effImplicitTargetMask &= ~(SpellCastTargetFlags.Item | SpellCastTargetFlags.GameobjectItem);
            if (dstSet || Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.DestLocation))
                effImplicitTargetMask &= ~SpellCastTargetFlags.DestLocation;
            if (srcSet || Convert.ToBoolean(providedTargetMask & SpellCastTargetFlags.SourceLocation))
                effImplicitTargetMask &= ~SpellCastTargetFlags.SourceLocation;

            return effImplicitTargetMask;
        }

        public SpellEffectImplicitTargetTypes GetImplicitTargetType()
        {
            return _data[(int)Effect].ImplicitTargetType;
        }

        public SpellTargetObjectTypes GetUsedTargetObjectType()
        {
            return _data[(int)Effect].UsedTargetObjectType;
        }

        ExpectedStatType GetScalingExpectedStat()
        {
            switch (Effect)
            {
                case SpellEffectName.SchoolDamage:
                case SpellEffectName.EnvironmentalDamage:
                case SpellEffectName.HealthLeech:
                case SpellEffectName.WeaponDamageNoSchool:
                case SpellEffectName.WeaponDamage:
                    return ExpectedStatType.CreatureSpellDamage;
                case SpellEffectName.Heal:
                case SpellEffectName.HealMechanical:
                    return ExpectedStatType.PlayerHealth;
                case SpellEffectName.Energize:
                case SpellEffectName.PowerBurn:
                    if (MiscValue == (int)PowerType.Mana)
                        return ExpectedStatType.PlayerMana;
                    return ExpectedStatType.None;
                case SpellEffectName.PowerDrain:
                    return ExpectedStatType.PlayerMana;
                case SpellEffectName.ApplyAura:
                case SpellEffectName.PersistentAreaAura:
                case SpellEffectName.ApplyAreaAuraParty:
                case SpellEffectName.ApplyAreaAuraRaid:
                case SpellEffectName.ApplyAreaAuraPet:
                case SpellEffectName.ApplyAreaAuraFriend:
                case SpellEffectName.ApplyAreaAuraEnemy:
                case SpellEffectName.ApplyAreaAuraOwner:
                case SpellEffectName.ApplyAuraOnPet:
                case SpellEffectName.ApplyAreaAuraSummons:
                case SpellEffectName.ApplyAreaAuraPartyNonrandom:
                    switch (ApplyAuraName)
                    {
                        case AuraType.PeriodicDamage:
                        case AuraType.ModDamageDone:
                        case AuraType.DamageShield:
                        case AuraType.ProcTriggerDamage:
                        case AuraType.PeriodicLeech:
                        case AuraType.ModDamageDoneCreature:
                        case AuraType.PeriodicHealthFunnel:
                        case AuraType.ModMeleeAttackPowerVersus:
                        case AuraType.ModRangedAttackPowerVersus:
                        case AuraType.ModFlatSpellDamageVersus:
                            return ExpectedStatType.CreatureSpellDamage;
                        case AuraType.PeriodicHeal:
                        case AuraType.ModDamageTaken:
                        case AuraType.ModIncreaseHealth:
                        case AuraType.SchoolAbsorb:
                        case AuraType.ModRegen:
                        case AuraType.ManaShield:
                        case AuraType.ModHealing:
                        case AuraType.ModHealingDone:
                        case AuraType.ModHealthRegenInCombat:
                        case AuraType.ModMaxHealth:
                        case AuraType.ModIncreaseHealth2:
                        case AuraType.SchoolHealAbsorb:
                            return ExpectedStatType.PlayerHealth;
                        case AuraType.PeriodicManaLeech:
                            return ExpectedStatType.PlayerMana;
                        case AuraType.ModStat:
                        case AuraType.ModAttackPower:
                        case AuraType.ModRangedAttackPower:
                            return ExpectedStatType.PlayerPrimaryStat;
                        case AuraType.ModRating:
                            return ExpectedStatType.PlayerSecondaryStat;
                        case AuraType.ModResistance:
                        case AuraType.ModBaseResistance:
                        case AuraType.ModTargetResistance:
                        case AuraType.ModBonusArmor:
                            return ExpectedStatType.ArmorConstant;
                        case AuraType.PeriodicEnergize:
                        case AuraType.ModIncreaseEnergy:
                        case AuraType.ModPowerCostSchool:
                        case AuraType.ModPowerRegen:
                        case AuraType.PowerBurn:
                        case AuraType.ModMaxPower:
                            if (MiscValue == (int)PowerType.Mana)
                                return ExpectedStatType.PlayerMana;
                            return ExpectedStatType.None;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            return ExpectedStatType.None;
        }

        public ImmunityInfo GetImmunityInfo() { return _immunityInfo; }

        public class StaticData
        {
            public StaticData(SpellEffectImplicitTargetTypes implicittarget, SpellTargetObjectTypes usedtarget)
            {
                ImplicitTargetType = implicittarget;
                UsedTargetObjectType = usedtarget;
            }

            public SpellEffectImplicitTargetTypes ImplicitTargetType; // defines what target can be added to effect target list if there's no valid target type provided for effect
            public SpellTargetObjectTypes UsedTargetObjectType; // defines valid target object type for spell effect
        }

        static StaticData[] _data = new StaticData[(int)SpellEffectName.TotalSpellEffects]
        {
            // implicit target type           used target object type
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 0
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 1 SPELL_EFFECT_INSTAKILL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 2 SPELL_EFFECT_SCHOOL_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 3 SPELL_EFFECT_DUMMY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 4 SPELL_EFFECT_PORTAL_TELEPORT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 5 SPELL_EFFECT_5
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 6 SPELL_EFFECT_APPLY_AURA
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 7 SPELL_EFFECT_ENVIRONMENTAL_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 8 SPELL_EFFECT_POWER_DRAIN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 9 SPELL_EFFECT_HEALTH_LEECH
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 10 SPELL_EFFECT_HEAL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 11 SPELL_EFFECT_BIND
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 12 SPELL_EFFECT_PORTAL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 13 SPELL_EFFECT_TELEPORT_TO_RETURN_POINT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 14 SPELL_EFFECT_INCREASE_CURRENCY_CAP
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 15 SPELL_EFFECT_TELEPORT_WITH_SPELL_VISUAL_KIT_LOADING_SCREEN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 16 SPELL_EFFECT_QUEST_COMPLETE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 17 SPELL_EFFECT_WEAPON_DAMAGE_NOSCHOOL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseAlly), // 18 SPELL_EFFECT_RESURRECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 19 SPELL_EFFECT_ADD_EXTRA_ATTACKS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 20 SPELL_EFFECT_DODGE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 21 SPELL_EFFECT_EVADE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 22 SPELL_EFFECT_PARRY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 23 SPELL_EFFECT_BLOCK
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 24 SPELL_EFFECT_CREATE_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 25 SPELL_EFFECT_WEAPON
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 26 SPELL_EFFECT_DEFENSE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 27 SPELL_EFFECT_PERSISTENT_AREA_AURA
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 28 SPELL_EFFECT_SUMMON
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 29 SPELL_EFFECT_LEAP
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 30 SPELL_EFFECT_ENERGIZE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 31 SPELL_EFFECT_WEAPON_PERCENT_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 32 SPELL_EFFECT_TRIGGER_MISSILE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.GobjItem), // 33 SPELL_EFFECT_OPEN_LOCK
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 34 SPELL_EFFECT_SUMMON_CHANGE_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 35 SPELL_EFFECT_APPLY_AREA_AURA_PARTY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 36 SPELL_EFFECT_LEARN_SPELL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 37 SPELL_EFFECT_SPELL_DEFENSE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 38 SPELL_EFFECT_DISPEL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 39 SPELL_EFFECT_LANGUAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 40 SPELL_EFFECT_DUAL_WIELD
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 41 SPELL_EFFECT_JUMP
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 42 SPELL_EFFECT_JUMP_DEST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 43 SPELL_EFFECT_TELEPORT_UNITS_FACE_CASTER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 44 SPELL_EFFECT_SKILL_STEP
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 45 SPELL_EFFECT_ADD_HONOR
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 46 SPELL_EFFECT_SPAWN
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 47 SPELL_EFFECT_TRADE_SKILL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 48 SPELL_EFFECT_STEALTH
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 49 SPELL_EFFECT_DETECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 50 SPELL_EFFECT_TRANS_DOOR
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 51 SPELL_EFFECT_FORCE_CRITICAL_HIT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 52 SPELL_EFFECT_SET_MAX_BATTLE_PET_COUNT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 53 SPELL_EFFECT_ENCHANT_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 54 SPELL_EFFECT_ENCHANT_ITEM_TEMPORARY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 55 SPELL_EFFECT_TAMECREATURE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 56 SPELL_EFFECT_SUMMON_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 57 SPELL_EFFECT_LEARN_PET_SPELL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 58 SPELL_EFFECT_WEAPON_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 59 SPELL_EFFECT_CREATE_RANDOM_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 60 SPELL_EFFECT_PROFICIENCY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 61 SPELL_EFFECT_SEND_EVENT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 62 SPELL_EFFECT_POWER_BURN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 63 SPELL_EFFECT_THREAT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 64 SPELL_EFFECT_TRIGGER_SPELL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 65 SPELL_EFFECT_APPLY_AREA_AURA_RAID
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 66 SPELL_EFFECT_RECHARGE_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 67 SPELL_EFFECT_HEAL_MAX_HEALTH
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 68 SPELL_EFFECT_INTERRUPT_CAST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 69 SPELL_EFFECT_DISTRACT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 70 SPELL_EFFECT_COMPLETE_AND_REWARD_WORLD_QUEST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 71 SPELL_EFFECT_PICKPOCKET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 72 SPELL_EFFECT_ADD_FARSIGHT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 73 SPELL_EFFECT_UNTRAIN_TALENTS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 74 SPELL_EFFECT_APPLY_GLYPH
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 75 SPELL_EFFECT_HEAL_MECHANICAL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 76 SPELL_EFFECT_SUMMON_OBJECT_WILD
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 77 SPELL_EFFECT_SCRIPT_EFFECT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 78 SPELL_EFFECT_ATTACK
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 79 SPELL_EFFECT_SANCTUARY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 80 SPELL_EFFECT_MODIFY_FOLLOWER_ITEM_LEVEL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 81 SPELL_EFFECT_PUSH_ABILITY_TO_ACTION_BAR
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 82 SPELL_EFFECT_BIND_SIGHT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 83 SPELL_EFFECT_DUEL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 84 SPELL_EFFECT_STUCK
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 85 SPELL_EFFECT_SUMMON_PLAYER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj), // 86 SPELL_EFFECT_ACTIVATE_OBJECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj), // 87 SPELL_EFFECT_GAMEOBJECT_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj), // 88 SPELL_EFFECT_GAMEOBJECT_REPAIR
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Gobj), // 89 SPELL_EFFECT_GAMEOBJECT_SET_DESTRUCTION_STATE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 90 SPELL_EFFECT_KILL_CREDIT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 91 SPELL_EFFECT_THREAT_ALL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 92 SPELL_EFFECT_ENCHANT_HELD_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 93 SPELL_EFFECT_FORCE_DESELECT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 94 SPELL_EFFECT_SELF_RESURRECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 95 SPELL_EFFECT_SKINNING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 96 SPELL_EFFECT_CHARGE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 97 SPELL_EFFECT_CAST_BUTTON
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 98 SPELL_EFFECT_KNOCK_BACK
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 99 SPELL_EFFECT_DISENCHANT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 100 SPELL_EFFECT_INEBRIATE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 101 SPELL_EFFECT_FEED_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 102 SPELL_EFFECT_DISMISS_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 103 SPELL_EFFECT_REPUTATION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 104 SPELL_EFFECT_SUMMON_OBJECT_SLOT1
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 105 SPELL_EFFECT_SURVEY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 106 SPELL_EFFECT_CHANGE_RAID_MARKER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 107 SPELL_EFFECT_SHOW_CORPSE_LOOT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 108 SPELL_EFFECT_DISPEL_MECHANIC
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 109 SPELL_EFFECT_RESURRECT_PET
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 110 SPELL_EFFECT_DESTROY_ALL_TOTEMS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 111 SPELL_EFFECT_DURABILITY_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 112 SPELL_EFFECT_112
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 113 SPELL_EFFECT_CANCEL_CONVERSATION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 114 SPELL_EFFECT_ATTACK_ME
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 115 SPELL_EFFECT_DURABILITY_DAMAGE_PCT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseEnemy), // 116 SPELL_EFFECT_SKIN_PLAYER_CORPSE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 117 SPELL_EFFECT_SPIRIT_HEAL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 118 SPELL_EFFECT_SKILL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 119 SPELL_EFFECT_APPLY_AREA_AURA_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 120 SPELL_EFFECT_TELEPORT_GRAVEYARD
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 121 SPELL_EFFECT_NORMALIZED_WEAPON_DMG
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 122 SPELL_EFFECT_122
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 123 SPELL_EFFECT_SEND_TAXI
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 124 SPELL_EFFECT_PULL_TOWARDS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 125 SPELL_EFFECT_MODIFY_THREAT_PERCENT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 126 SPELL_EFFECT_STEAL_BENEFICIAL_BUFF
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 127 SPELL_EFFECT_PROSPECTING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 128 SPELL_EFFECT_APPLY_AREA_AURA_FRIEND
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 129 SPELL_EFFECT_APPLY_AREA_AURA_ENEMY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 130 SPELL_EFFECT_REDIRECT_THREAT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 131 SPELL_EFFECT_PLAY_SOUND
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 132 SPELL_EFFECT_PLAY_MUSIC
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 133 SPELL_EFFECT_UNLEARN_SPECIALIZATION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 134 SPELL_EFFECT_KILL_CREDIT2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 135 SPELL_EFFECT_CALL_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 136 SPELL_EFFECT_HEAL_PCT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 137 SPELL_EFFECT_ENERGIZE_PCT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 138 SPELL_EFFECT_LEAP_BACK
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 139 SPELL_EFFECT_CLEAR_QUEST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 140 SPELL_EFFECT_FORCE_CAST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 141 SPELL_EFFECT_FORCE_CAST_WITH_VALUE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 142 SPELL_EFFECT_TRIGGER_SPELL_WITH_VALUE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 143 SPELL_EFFECT_APPLY_AREA_AURA_OWNER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 144 SPELL_EFFECT_KNOCK_BACK_DEST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 145 SPELL_EFFECT_PULL_TOWARDS_DEST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 146 SPELL_EFFECT_RESTORE_GARRISON_TROOP_VITALITY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 147 SPELL_EFFECT_QUEST_FAIL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 148 SPELL_EFFECT_TRIGGER_MISSILE_SPELL_WITH_VALUE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 149 SPELL_EFFECT_CHARGE_DEST
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 150 SPELL_EFFECT_QUEST_START
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 151 SPELL_EFFECT_TRIGGER_SPELL_2
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 152 SPELL_EFFECT_SUMMON_RAF_FRIEND
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 153 SPELL_EFFECT_CREATE_TAMED_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 154 SPELL_EFFECT_DISCOVER_TAXI
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 155 SPELL_EFFECT_TITAN_GRIP
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 156 SPELL_EFFECT_ENCHANT_ITEM_PRISMATIC
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 157 SPELL_EFFECT_CREATE_LOOT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 158 SPELL_EFFECT_MILLING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 159 SPELL_EFFECT_ALLOW_RENAME_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 160 SPELL_EFFECT_160
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 161 SPELL_EFFECT_TALENT_SPEC_COUNT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 162 SPELL_EFFECT_TALENT_SPEC_SELECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 163 SPELL_EFFECT_OBLITERATE_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 164 SPELL_EFFECT_REMOVE_AURA
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 165 SPELL_EFFECT_DAMAGE_FROM_MAX_HEALTH_PCT
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 166 SPELL_EFFECT_GIVE_CURRENCY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 167 SPELL_EFFECT_UPDATE_PLAYER_PHASE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 168 SPELL_EFFECT_ALLOW_CONTROL_PET
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 169 SPELL_EFFECT_DESTROY_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 170 SPELL_EFFECT_UPDATE_ZONE_AURAS_AND_PHASES
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 171 SPELL_EFFECT_SUMMON_PERSONAL_GAMEOBJECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseAlly), // 172 SPELL_EFFECT_RESURRECT_WITH_AURA
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 173 SPELL_EFFECT_UNLOCK_GUILD_VAULT_TAB
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 174 SPELL_EFFECT_APPLY_AURA_ON_PET
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 175 SPELL_EFFECT_175
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 176 SPELL_EFFECT_SANCTUARY_2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 177 SPELL_EFFECT_DESPAWN_PERSISTENT_AREA_AURA
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 178 SPELL_EFFECT_178
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 179 SPELL_EFFECT_CREATE_AREATRIGGER
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 180 SPELL_EFFECT_UPDATE_AREATRIGGER
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 181 SPELL_EFFECT_REMOVE_TALENT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 182 SPELL_EFFECT_DESPAWN_AREATRIGGER
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 183 SPELL_EFFECT_183
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 184 SPELL_EFFECT_REPUTATION_2
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 185 SPELL_EFFECT_185
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 186 SPELL_EFFECT_186
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 187 SPELL_EFFECT_RANDOMIZE_ARCHAEOLOGY_DIGSITES
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 188 SPELL_EFFECT_SUMMON_STABLED_PET_AS_GUARDIAN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 189 SPELL_EFFECT_LOOT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 190 SPELL_EFFECT_CHANGE_PARTY_MEMBERS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 191 SPELL_EFFECT_TELEPORT_TO_DIGSITE
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 192 SPELL_EFFECT_UNCAGE_BATTLEPET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 193 SPELL_EFFECT_START_PET_BATTLE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 194 SPELL_EFFECT_194
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 195 SPELL_EFFECT_PLAY_SCENE_SCRIPT_PACKAGE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 196 SPELL_EFFECT_CREATE_SCENE_OBJECT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 197 SPELL_EFFECT_CREATE_PERSONAL_SCENE_OBJECT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 198 SPELL_EFFECT_PLAY_SCENE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 199 SPELL_EFFECT_DESPAWN_SUMMON
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 200 SPELL_EFFECT_HEAL_BATTLEPET_PCT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 201 SPELL_EFFECT_ENABLE_BATTLE_PETS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 202 SPELL_EFFECT_APPLY_AREA_AURA_SUMMONS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 203 SPELL_EFFECT_REMOVE_AURA_2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 204 SPELL_EFFECT_CHANGE_BATTLEPET_QUALITY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 205 SPELL_EFFECT_LAUNCH_QUEST_CHOICE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 206 SPELL_EFFECT_ALTER_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 207 SPELL_EFFECT_LAUNCH_QUEST_TASK
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 208 SPELL_EFFECT_SET_REPUTATION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 209 SPELL_EFFECT_209
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 210 SPELL_EFFECT_LEARN_GARRISON_BUILDING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 211 SPELL_EFFECT_LEARN_GARRISON_SPECIALIZATION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 212 SPELL_EFFECT_REMOVE_AURA_BY_SPELL_LABEL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 213 SPELL_EFFECT_JUMP_DEST_2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 214 SPELL_EFFECT_CREATE_GARRISON
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 215 SPELL_EFFECT_UPGRADE_CHARACTER_SPELLS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 216 SPELL_EFFECT_CREATE_SHIPMENT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 217 SPELL_EFFECT_UPGRADE_GARRISON
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 218 SPELL_EFFECT_218
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 219 SPELL_EFFECT_CREATE_CONVERSATION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 220 SPELL_EFFECT_ADD_GARRISON_FOLLOWER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 221 SPELL_EFFECT_ADD_GARRISON_MISSION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 222 SPELL_EFFECT_CREATE_HEIRLOOM_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 223 SPELL_EFFECT_CHANGE_ITEM_BONUSES
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 224 SPELL_EFFECT_ACTIVATE_GARRISON_BUILDING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 225 SPELL_EFFECT_GRANT_BATTLEPET_LEVEL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 226 SPELL_EFFECT_TRIGGER_ACTION_SET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 227 SPELL_EFFECT_TELEPORT_TO_LFG_DUNGEON
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 228 SPELL_EFFECT_228
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 229 SPELL_EFFECT_SET_FOLLOWER_QUALITY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 230 SPELL_EFFECT_230
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 231 SPELL_EFFECT_INCREASE_FOLLOWER_EXPERIENCE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 232 SPELL_EFFECT_REMOVE_PHASE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 233 SPELL_EFFECT_RANDOMIZE_FOLLOWER_ABILITIES
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 234 SPELL_EFFECT_234
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 235 SPELL_EFFECT_235
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 236 SPELL_EFFECT_GIVE_EXPERIENCE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 237 SPELL_EFFECT_GIVE_RESTED_EXPERIENCE_BONUS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 238 SPELL_EFFECT_INCREASE_SKILL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 239 SPELL_EFFECT_END_GARRISON_BUILDING_CONSTRUCTION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 240 SPELL_EFFECT_GIVE_ARTIFACT_POWER
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 241 SPELL_EFFECT_241
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 242 SPELL_EFFECT_GIVE_ARTIFACT_POWER_NO_BONUS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 243 SPELL_EFFECT_APPLY_ENCHANT_ILLUSION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 244 SPELL_EFFECT_LEARN_FOLLOWER_ABILITY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 245 SPELL_EFFECT_UPGRADE_HEIRLOOM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 246 SPELL_EFFECT_FINISH_GARRISON_MISSION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 247 SPELL_EFFECT_ADD_GARRISON_MISSION_SET
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 248 SPELL_EFFECT_FINISH_SHIPMENT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 249 SPELL_EFFECT_FORCE_EQUIP_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 250 SPELL_EFFECT_TAKE_SCREENSHOT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 251 SPELL_EFFECT_SET_GARRISON_CACHE_SIZE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 252 SPELL_EFFECT_TELEPORT_UNITS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 253 SPELL_EFFECT_GIVE_HONOR
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 254 SPELL_EFFECT_JUMP_CHARGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 255 SPELL_EFFECT_LEARN_TRANSMOG_SET
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 256 SPELL_EFFECT_256
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 257 SPELL_EFFECT_257
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 258 SPELL_EFFECT_MODIFY_KEYSTONE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 259 SPELL_EFFECT_RESPEC_AZERITE_EMPOWERED_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 260 SPELL_EFFECT_SUMMON_STABLED_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 261 SPELL_EFFECT_SCRAP_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 262 SPELL_EFFECT_262
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 263 SPELL_EFFECT_REPAIR_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 264 SPELL_EFFECT_REMOVE_GEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 265 SPELL_EFFECT_LEARN_AZERITE_ESSENCE_POWER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 266 SPELL_EFFECT_SET_ITEM_BONUS_LIST_GROUP_ENTRY
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 267 SPELL_EFFECT_CREATE_PRIVATE_CONVERSATION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 268 SPELL_EFFECT_APPLY_MOUNT_EQUIPMENT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 269 SPELL_EFFECT_INCREASE_ITEM_BONUS_LIST_GROUP_STEP
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 270 SPELL_EFFECT_270
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 271 SPELL_EFFECT_APPLY_AREA_AURA_PARTY_NONRANDOM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 272 SPELL_EFFECT_SET_COVENANT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 273 SPELL_EFFECT_CRAFT_RUNEFORGE_LEGENDARY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 274 SPELL_EFFECT_274
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 275 SPELL_EFFECT_275
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 276 SPELL_EFFECT_LEARN_TRANSMOG_ILLUSION
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 277 SPELL_EFFECT_SET_CHROMIE_TIME
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 278 SPELL_EFFECT_278
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 279 SPELL_EFFECT_LEARN_GARR_TALENT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 280 SPELL_EFFECT_280
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 281 SPELL_EFFECT_LEARN_SOULBIND_CONDUIT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 282 SPELL_EFFECT_CONVERT_ITEMS_TO_CURRENCY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 283 SPELL_EFFECT_COMPLETE_CAMPAIGN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 284 SPELL_EFFECT_SEND_CHAT_MESSAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 285 SPELL_EFFECT_MODIFY_KEYSTONE_2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 286 SPELL_EFFECT_GRANT_BATTLEPET_EXPERIENCE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 287 SPELL_EFFECT_SET_GARRISON_FOLLOWER_LEVEL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 288 SPELL_EFFECT_CRAFT_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 289 SPELL_EFFECT_MODIFY_AURA_STACKS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 290 SPELL_EFFECT_MODIFY_COOLDOWN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 291 SPELL_EFFECT_MODIFY_COOLDOWNS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 292 SPELL_EFFECT_MODIFY_COOLDOWNS_BY_CATEGORY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 293 SPELL_EFFECT_MODIFY_CHARGES
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 294 SPELL_EFFECT_CRAFT_LOOT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 295 SPELL_EFFECT_SALVAGE_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 296 SPELL_EFFECT_CRAFT_SALVAGE_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 297 SPELL_EFFECT_RECRAFT_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 298 SPELL_EFFECT_CANCEL_ALL_PRIVATE_CONVERSATIONS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 299 SPELL_EFFECT_299
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 300 SPELL_EFFECT_300
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 301 SPELL_EFFECT_CRAFT_ENCHANT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.None), // 302 SPELL_EFFECT_GATHERING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 303 SPELL_EFFECT_CREATE_TRAIT_TREE_CONFIG
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 304 SPELL_EFFECT_CHANGE_ACTIVE_COMBAT_TRAIT_CONFIG
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 305 SPELL_EFFECT_305
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 306 SPELL_EFFECT_UPDATE_INTERACTIONS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 307 SPELL_EFFECT_307
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 308 SPELL_EFFECT_CANCEL_PRELOAD_WORLD
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 309 SPELL_EFFECT_PRELOAD_WORLD
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 310 SPELL_EFFECT_310
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 311 SPELL_EFFECT_ENSURE_WORLD_LOADED
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 312 SPELL_EFFECT_312
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 313 SPELL_EFFECT_CHANGE_ITEM_BONUSES_2
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 314 SPELL_EFFECT_ADD_SOCKET_BONUS
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 315 SPELL_EFFECT_LEARN_TRANSMOG_APPEARANCE_FROM_ITEM_MOD_APPEARANCE_GROUP
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 316 SPELL_EFFECT_KILL_CREDIT_LABEL_1
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 317 SPELL_EFFECT_KILL_CREDIT_LABEL_2
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 318 SPELL_EFFECT_318
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 319 SPELL_EFFECT_319
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 320 SPELL_EFFECT_320
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 321 SPELL_EFFECT_321
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 322 SPELL_EFFECT_322
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 323 SPELL_EFFECT_323
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 324 SPELL_EFFECT_324
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 325 SPELL_EFFECT_325
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 326 SPELL_EFFECT_326
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 327 SPELL_EFFECT_327
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 328 SPELL_EFFECT_328
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 329 SPELL_EFFECT_329
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 330 SPELL_EFFECT_330
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 331 SPELL_EFFECT_331
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 332 SPELL_EFFECT_332
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 333 SPELL_EFFECT_333
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 334 SPELL_EFFECT_334
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 335 SPELL_EFFECT_335
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 336 SPELL_EFFECT_336
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 337 SPELL_EFFECT_337
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 338 SPELL_EFFECT_338
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 339 SPELL_EFFECT_UI_ACTION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 340 SPELL_EFFECT_340
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 341 SPELL_EFFECT_LEARN_WARBAND_SCENE
        };

        #region Fields
        SpellInfo _spellInfo;
        public uint EffectIndex;

        public SpellEffectName Effect;
        public AuraType ApplyAuraName;
        public uint ApplyAuraPeriod;
        public float BasePoints;
        public float RealPointsPerLevel;
        public float PointsPerResource;
        public float Amplitude;
        public float ChainAmplitude;
        public float BonusCoefficient;
        public int MiscValue;
        public int MiscValueB;
        public Mechanics Mechanic;
        public float PositionFacing;
        public SpellImplicitTargetInfo TargetA = new();
        public SpellImplicitTargetInfo TargetB = new();
        public SpellRadiusRecord TargetARadiusEntry;
        public SpellRadiusRecord TargetBRadiusEntry;
        public int ChainTargets;
        public uint ItemType;
        public uint TriggerSpell;
        public FlagArray128 SpellClassMask;
        public float BonusCoefficientFromAP;
        public List<Condition> ImplicitTargetConditions;
        public SpellEffectAttributes EffectAttributes;
        public ScalingInfo Scaling;

        ImmunityInfo _immunityInfo;
        #endregion

        public struct ScalingInfo
        {
            public int Class;
            public float Coefficient;
            public float Variance;
            public float ResourceCoefficient;
        }
    }

    public class SpellImplicitTargetInfo
    {
        public SpellImplicitTargetInfo(Targets target = 0)
        {
            _target = target;
        }

        public bool IsArea()
        {
            return GetSelectionCategory() == SpellTargetSelectionCategories.Area || GetSelectionCategory() == SpellTargetSelectionCategories.Cone;
        }

        public SpellTargetSelectionCategories GetSelectionCategory()
        {
            return _data[(int)_target].SelectionCategory;
        }

        public SpellTargetReferenceTypes GetReferenceType()
        {
            return _data[(int)_target].ReferenceType;
        }

        public SpellTargetObjectTypes GetObjectType()
        {
            return _data[(int)_target].ObjectType;
        }

        public SpellTargetCheckTypes GetCheckType()
        {
            return _data[(int)_target].SelectionCheckType;
        }

        SpellTargetDirectionTypes GetDirectionType()
        {
            return _data[(int)_target].DirectionType;
        }

        public float CalcDirectionAngle()
        {
            float pi = MathFunctions.PI;
            switch (GetDirectionType())
            {
                case SpellTargetDirectionTypes.Front:
                    return 0.0f;
                case SpellTargetDirectionTypes.Back:
                    return pi;
                case SpellTargetDirectionTypes.Right:
                    return -pi / 2;
                case SpellTargetDirectionTypes.Left:
                    return pi / 2;
                case SpellTargetDirectionTypes.FrontRight:
                    return -pi / 4;
                case SpellTargetDirectionTypes.BackRight:
                    return -3 * pi / 4;
                case SpellTargetDirectionTypes.BackLeft:
                    return 3 * pi / 4;
                case SpellTargetDirectionTypes.FrontLeft:
                    return pi / 4;
                case SpellTargetDirectionTypes.Random:
                    return RandomHelper.NextSingle() * (2 * pi);
                default:
                    return 0.0f;
            }
        }

        public Targets GetTarget()
        {
            return _target;
        }

        public SpellCastTargetFlags GetExplicitTargetMask(ref bool srcSet, ref bool dstSet)
        {
            SpellCastTargetFlags targetMask = 0;
            if (GetTarget() == Targets.DestTraj)
            {
                if (!srcSet)
                    targetMask = SpellCastTargetFlags.SourceLocation;
                if (!dstSet)
                    targetMask |= SpellCastTargetFlags.DestLocation;
            }
            else
            {
                switch (GetReferenceType())
                {
                    case SpellTargetReferenceTypes.Src:
                        if (srcSet)
                            break;
                        targetMask = SpellCastTargetFlags.SourceLocation;
                        break;
                    case SpellTargetReferenceTypes.Dest:
                        if (dstSet)
                            break;
                        targetMask = SpellCastTargetFlags.DestLocation;
                        break;
                    case SpellTargetReferenceTypes.Target:
                        switch (GetObjectType())
                        {
                            case SpellTargetObjectTypes.Gobj:
                                targetMask = SpellCastTargetFlags.Gameobject;
                                break;
                            case SpellTargetObjectTypes.GobjItem:
                                targetMask = SpellCastTargetFlags.GameobjectItem;
                                break;
                            case SpellTargetObjectTypes.UnitAndDest:
                            case SpellTargetObjectTypes.Unit:
                            case SpellTargetObjectTypes.Dest:
                                switch (GetCheckType())
                                {
                                    case SpellTargetCheckTypes.Enemy:
                                        targetMask = SpellCastTargetFlags.UnitEnemy;
                                        break;
                                    case SpellTargetCheckTypes.Ally:
                                        targetMask = SpellCastTargetFlags.UnitAlly;
                                        break;
                                    case SpellTargetCheckTypes.Party:
                                        targetMask = SpellCastTargetFlags.UnitParty;
                                        break;
                                    case SpellTargetCheckTypes.Raid:
                                        targetMask = SpellCastTargetFlags.UnitRaid;
                                        break;
                                    case SpellTargetCheckTypes.Passenger:
                                        targetMask = SpellCastTargetFlags.UnitPassenger;
                                        break;
                                    case SpellTargetCheckTypes.RaidClass:
                                    default:
                                        targetMask = SpellCastTargetFlags.Unit;
                                        break;
                                }
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }

            switch (GetObjectType())
            {
                case SpellTargetObjectTypes.Src:
                    srcSet = true;
                    break;
                case SpellTargetObjectTypes.Dest:
                case SpellTargetObjectTypes.UnitAndDest:
                    dstSet = true;
                    break;
                default:
                    break;
            }
            return targetMask;
        }

        Targets _target;

        public struct StaticData
        {
            public StaticData(SpellTargetObjectTypes obj, SpellTargetReferenceTypes reference,
                SpellTargetSelectionCategories selection, SpellTargetCheckTypes selectionCheck, SpellTargetDirectionTypes direction)
            {
                ObjectType = obj;
                ReferenceType = reference;
                SelectionCategory = selection;
                SelectionCheckType = selectionCheck;
                DirectionType = direction;
            }
            public SpellTargetObjectTypes ObjectType;    // type of object returned by target type
            public SpellTargetReferenceTypes ReferenceType; // defines which object is used as a reference when selecting target
            public SpellTargetSelectionCategories SelectionCategory;
            public SpellTargetCheckTypes SelectionCheckType; // defines selection criteria
            public SpellTargetDirectionTypes DirectionType; // direction for cone and dest targets
        }

        static StaticData[] _data = new StaticData[(int)Targets.TotalSpellTargets]
        {
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 0
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 1 TARGET_UNIT_CASTER
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 2 TARGET_UNIT_NEARBY_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 3 TARGET_UNIT_NEARBY_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 4 TARGET_UNIT_NEARBY_PARTY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 5 TARGET_UNIT_PET
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 6 TARGET_UNIT_TARGET_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 7 TARGET_UNIT_SRC_AREA_ENTRY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 8 TARGET_UNIT_DEST_AREA_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 9 TARGET_DEST_HOME
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 10
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 11 TARGET_UNIT_SRC_AREA_UNK_11
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 12
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 13
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 14
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 15 TARGET_UNIT_SRC_AREA_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 16 TARGET_UNIT_DEST_AREA_ENEMY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 17 TARGET_DEST_DB
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 18 TARGET_DEST_CASTER
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 19
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 20 TARGET_UNIT_CASTER_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 21 TARGET_UNIT_TARGET_ALLY
            new StaticData(SpellTargetObjectTypes.Src,          SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 22 TARGET_SRC_CASTER
            new StaticData(SpellTargetObjectTypes.Gobj,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 23 TARGET_GAMEOBJECT_TARGET
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 24 TARGET_UNIT_CONE_ENEMY_24
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 25 TARGET_UNIT_TARGET_ANY
            new StaticData(SpellTargetObjectTypes.GobjItem,     SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 26 TARGET_GAMEOBJECT_ITEM_TARGET
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 27 TARGET_UNIT_MASTER
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 28 TARGET_DEST_DYNOBJ_ENEMY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 29 TARGET_DEST_DYNOBJ_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 30 TARGET_UNIT_SRC_AREA_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 31 TARGET_UNIT_DEST_AREA_ALLY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),   // 32 TARGET_DEST_CASTER_SUMMON
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 33 TARGET_UNIT_SRC_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 34 TARGET_UNIT_DEST_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 35 TARGET_UNIT_TARGET_PARTY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 36 TARGET_DEST_CASTER_UNK_36
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Last,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 37 TARGET_UNIT_LASTTARGET_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 38 TARGET_UNIT_NEARBY_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 39 TARGET_DEST_CASTER_FISHING
            new StaticData(SpellTargetObjectTypes.Gobj,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 40 TARGET_GAMEOBJECT_NEARBY_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontRight),  // 41 TARGET_DEST_CASTER_FRONT_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackRight),   // 42 TARGET_DEST_CASTER_BACK_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackLeft),    // 43 TARGET_DEST_CASTER_BACK_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),   // 44 TARGET_DEST_CASTER_FRONT_LEFT
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 45 TARGET_UNIT_TARGET_CHAINHEAL_ALLY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 46 TARGET_DEST_NEARBY_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 47 TARGET_DEST_CASTER_FRONT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Back),        // 48 TARGET_DEST_CASTER_BACK
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Right),       // 49 TARGET_DEST_CASTER_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Left),        // 50 TARGET_DEST_CASTER_LEFT
            new StaticData(SpellTargetObjectTypes.Gobj,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 51 TARGET_GAMEOBJECT_SRC_AREA
            new StaticData(SpellTargetObjectTypes.Gobj,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 52 TARGET_GAMEOBJECT_DEST_AREA
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 53 TARGET_DEST_TARGET_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 54 TARGET_UNIT_CONE_180_DEG_ENEMY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 55 TARGET_DEST_CASTER_FRONT_LEAP
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 56 TARGET_UNIT_CASTER_AREA_RAID
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 57 TARGET_UNIT_TARGET_RAID
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 58 TARGET_UNIT_NEARBY_RAID
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.Front),       // 59 TARGET_UNIT_CONE_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.Front),       // 60 TARGET_UNIT_CONE_ENTRY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.RaidClass,SpellTargetDirectionTypes.None),        // 61 TARGET_UNIT_TARGET_AREA_RAID_CLASS
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 62 TARGET_DEST_CASTER_GROUND
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 63 TARGET_DEST_TARGET_ANY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 64 TARGET_DEST_TARGET_FRONT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Back),        // 65 TARGET_DEST_TARGET_BACK
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Right),       // 66 TARGET_DEST_TARGET_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Left),        // 67 TARGET_DEST_TARGET_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontRight),  // 68 TARGET_DEST_TARGET_FRONT_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackRight),   // 69 TARGET_DEST_TARGET_BACK_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackLeft),    // 70 TARGET_DEST_TARGET_BACK_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),   // 71 TARGET_DEST_TARGET_FRONT_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 72 TARGET_DEST_CASTER_RANDOM
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 73 TARGET_DEST_CASTER_RADIUS
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 74 TARGET_DEST_TARGET_RANDOM
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 75 TARGET_DEST_TARGET_RADIUS
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 76 TARGET_DEST_CHANNEL_TARGET
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 77 TARGET_UNIT_CHANNEL_TARGET
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 78 TARGET_DEST_DEST_FRONT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Back),        // 79 TARGET_DEST_DEST_BACK
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Right),       // 80 TARGET_DEST_DEST_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Left),        // 81 TARGET_DEST_DEST_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontRight),  // 82 TARGET_DEST_DEST_FRONT_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackRight),   // 83 TARGET_DEST_DEST_BACK_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackLeft),    // 84 TARGET_DEST_DEST_BACK_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),   // 85 TARGET_DEST_DEST_FRONT_LEFT
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 86 TARGET_DEST_DEST_RANDOM
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 87 TARGET_DEST_DEST
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 88 TARGET_DEST_DYNOBJ_NONE
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Traj   , SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 89 TARGET_DEST_TRAJ
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 90 TARGET_UNIT_TARGET_MINIPET
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 91 TARGET_DEST_DEST_RADIUS
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 92 TARGET_UNIT_SUMMONER
            new StaticData(SpellTargetObjectTypes.Corpse,       SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 93 TARGET_CORPSE_SRC_AREA_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 94 TARGET_UNIT_VEHICLE
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Passenger,SpellTargetDirectionTypes.None),        // 95 TARGET_UNIT_TARGET_PASSENGER
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 96 TARGET_UNIT_PASSENGER_0
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 97 TARGET_UNIT_PASSENGER_1
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 98 TARGET_UNIT_PASSENGER_2
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 99 TARGET_UNIT_PASSENGER_3
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 100 TARGET_UNIT_PASSENGER_4
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 101 TARGET_UNIT_PASSENGER_5
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 102 TARGET_UNIT_PASSENGER_6
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 103 TARGET_UNIT_PASSENGER_7
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 104 TARGET_UNIT_CONE_CASTER_TO_DEST_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 105 TARGET_UNIT_CASTER_AND_PASSENGERS
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 106 TARGET_DEST_NEARBY_DB
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 107 TARGET_DEST_NEARBY_ENTRY_2
            new StaticData(SpellTargetObjectTypes.Gobj,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 108 TARGET_GAMEOBJECT_CONE_CASTER_TO_DEST_ENEMY
            new StaticData(SpellTargetObjectTypes.Gobj,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.Front),       // 109 TARGET_GAMEOBJECT_CONE_CASTER_TO_DEST_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Entry  ,  SpellTargetDirectionTypes.Front),       // 110 TARGET_UNIT_CONE_CASTER_TO_DEST_ENTRY
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 111
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 112
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 113
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 114
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 115 TARGET_UNIT_SRC_AREA_FURTHEST_ENEMY
            new StaticData(SpellTargetObjectTypes.UnitAndDest,  SpellTargetReferenceTypes.Last,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 116 TARGET_UNIT_AND_DEST_LAST_ENEMY
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 117
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 118 TARGET_UNIT_TARGET_ALLY_OR_RAID
            new StaticData(SpellTargetObjectTypes.Corpse,       SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 119 TARGET_CORPSE_SRC_AREA_RAID
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Summoned, SpellTargetDirectionTypes.None),        // 120 TARGET_UNIT_SELF_AND_SUMMONS
            new StaticData(SpellTargetObjectTypes.Corpse,       SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 121 TARGET_CORPSE_TARGET_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 122 TARGET_UNIT_AREA_THREAT_LIST
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 123 TARGET_UNIT_AREA_TAP_LIST
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 124 TARGET_UNIT_TARGET_TAP_LIST
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 125 TARGET_DEST_CASTER_GROUND_2
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 126 TARGET_UNIT_CASTER_AREA_ENEMY_CLUMP
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 127 TARGET_DEST_CASTER_ENEMY_CLUMP_CENTROID
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.Front),       // 128 TARGET_UNIT_RECT_CASTER_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 129 TARGET_UNIT_RECT_CASTER_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 130 TARGET_UNIT_RECT_CASTER
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 131 TARGET_DEST_SUMMONER
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 132 TARGET_DEST_TARGET_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Line,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 133 TARGET_UNIT_LINE_CASTER_TO_DEST_ALLY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Line,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 134 TARGET_UNIT_LINE_CASTER_TO_DEST_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Line,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 135 TARGET_UNIT_LINE_CASTER_TO_DEST
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.Front),       // 136 TARGET_UNIT_CONE_CASTER_TO_DEST_ALLY
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 137 TARGET_DEST_CASTER_MOVEMENT_DIRECTION
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 138 TARGET_DEST_DEST_GROUND
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 139
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 140 TARGET_DEST_CASTER_CLUMP_CENTROID
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 141
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.FrontRight),  // 142 TARGET_DEST_NEARBY_ENTRY_OR_DB
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 143
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 144
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 145
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 146
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 147
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 148 TARGET_DEST_DEST_TARGET_TOWARDS_CASTER
            new StaticData(SpellTargetObjectTypes.Dest,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 149
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 150 TARGET_UNIT_OWN_CRITTER
            new StaticData(SpellTargetObjectTypes.Unit,         SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 151
            new StaticData(SpellTargetObjectTypes.None,         SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 152
        };
    }

    public class SpellPowerCost
    {
        public PowerType Power;
        public int Amount;
    }

    class SpellDiminishInfo
    {
        public DiminishingGroup DiminishGroup = DiminishingGroup.None;
        public DiminishingReturnsType DiminishReturnType = DiminishingReturnsType.None;
        public DiminishingLevels DiminishMaxLevel = DiminishingLevels.Immune;
        public int DiminishDurationLimit;
    }

    public class ImmunityInfo
    {
        public uint SchoolImmuneMask;
        public uint ApplyHarmfulAuraImmuneMask;
        public ulong MechanicImmuneMask;
        public uint DispelImmuneMask;
        public uint DamageSchoolMask;
        public byte OtherImmuneMask;

        public List<AuraType> AuraTypeImmune = new();
        public List<SpellEffectName> SpellEffectImmune = new();
    }
}

