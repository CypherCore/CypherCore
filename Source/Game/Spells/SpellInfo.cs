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
using Game.BattleGrounds;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Spells
{
    public class SpellInfo
    {
        public SpellInfo(SpellInfoLoadHelper data, Dictionary<uint, SpellEffectRecord[]> effectsMap, MultiMap<uint, SpellXSpellVisualRecord> visuals)
        {
            Id = data.Entry.Id;

            _effects = new Dictionary<uint, SpellEffectInfo[]>();

            if (effectsMap != null)
            {
                foreach (var pair in effectsMap)
                {
                    if (!_effects.ContainsKey(pair.Key))
                        _effects[pair.Key] = new SpellEffectInfo[pair.Value.Length];

                    for (int i = 0; i < pair.Value.Length; ++i)
                    {
                        SpellEffectRecord effect = pair.Value[i];
                        if (effect == null)
                            continue;

                        _effects[pair.Key][effect.EffectIndex] = new SpellEffectInfo(this, effect.EffectIndex, effect);
                    }
                }
            }

            SpellName = data.Entry.Name;

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
                CastTimeEntry = CliDB.SpellCastTimesStorage.LookupByKey(_misc.CastingTimeIndex);
                DurationEntry = CliDB.SpellDurationStorage.LookupByKey(_misc.DurationIndex);
                RangeIndex = _misc.RangeIndex;
                RangeEntry = CliDB.SpellRangeStorage.LookupByKey(_misc.RangeIndex);
                Speed = _misc.Speed;
                SchoolMask = (SpellSchoolMask)_misc.SchoolMask;
                AttributesCu = 0;

                IconFileDataId = _misc.SpellIconFileDataID;
                ActiveIconFileDataId = _misc.ActiveIconFileDataID;
            }

            if (visuals != null)
                _visuals = visuals;

            // sort all visuals so that the ones without a condition requirement are last on the list
            foreach (var key in _visuals.Keys.ToList())
                _visuals[key] = _visuals[key].OrderByDescending(x => x.CasterPlayerConditionID).ToList();

            // SpellScalingEntry
            SpellScalingRecord _scaling = data.Scaling;
            if (_scaling != null)
            {
                Scaling._Class = _scaling.Class;
                Scaling.MinScalingLevel = _scaling.MinScalingLevel;
                Scaling.MaxScalingLevel = _scaling.MaxScalingLevel;
                Scaling.ScalesFromItemLevel = _scaling.ScalesFromItemLevel;
            }

            // SpellAuraOptionsEntry
            SpellAuraOptionsRecord _options = data.AuraOptions;
            if (_options != null)
            {
                SpellProcsPerMinuteRecord _ppm = CliDB.SpellProcsPerMinuteStorage.LookupByKey(_options.SpellProcsPerMinuteID);
                ProcFlags = (ProcFlags)_options.ProcTypeMask[0];
                ProcChance = _options.ProcChance;
                ProcCharges = _options.ProcCharges;
                ProcCooldown = _options.ProcCategoryRecovery;
                ProcBasePPM = _ppm != null ? _ppm.BaseProcRate : 0.0f;
                ProcPPMMods = Global.DB2Mgr.GetSpellProcsPerMinuteMods(_options.SpellProcsPerMinuteID);
                StackAmount = _options.CumulativeAura;
            }

            // SpellAuraRestrictionsEntry
            SpellAuraRestrictionsRecord _aura = data.AuraRestrictions;
            if (_aura != null)
            {
                CasterAuraState = (AuraStateType)_aura.CasterAuraState;
                TargetAuraState = (AuraStateType)_aura.TargetAuraState;
                CasterAuraStateNot = (AuraStateType)_aura.ExcludeCasterAuraState;
                TargetAuraStateNot = (AuraStateType)_aura.ExcludeTargetAuraState;
                CasterAuraSpell = _aura.CasterAuraSpell;
                TargetAuraSpell = _aura.TargetAuraSpell;
                ExcludeCasterAuraSpell = _aura.ExcludeCasterAuraSpell;
                ExcludeTargetAuraSpell = _aura.ExcludeTargetAuraSpell;
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
            }

            EquippedItemClass = ItemClass.None;
            EquippedItemSubClassMask = -1;
            EquippedItemInventoryTypeMask = -1;
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
                // TODO: 6.x these flags have 2 parts
                AuraInterruptFlags = _interrupt.AuraInterruptFlags;
                ChannelInterruptFlags = _interrupt.ChannelInterruptFlags;
            }

            // SpellLevelsEntry
            SpellLevelsRecord _levels = data.Levels;
            if (_levels != null)
            {
                MaxLevel = _levels.MaxLevel;
                BaseLevel = _levels.BaseLevel;
                SpellLevel = _levels.SpellLevel;
            }

            // SpellPowerEntry
            PowerCosts = Global.DB2Mgr.GetSpellPowers(Id, Difficulty.None, out _hasPowerDifficultyData);

            // SpellReagentsEntry
            SpellReagentsRecord _reagents = data.Reagents;
            for (var i = 0; i < SpellConst.MaxReagents; ++i)
            {
                Reagent[i] = _reagents != null ? _reagents.Reagent[i] : 0;
                ReagentCount[i] = _reagents != null ? _reagents.ReagentCount[i] : 0u;
            }

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
                targets = (SpellCastTargetFlags)_target.Targets;
                ConeAngle = _target.ConeDegrees;
                Width = _target.Width;
                TargetCreatureType = _target.TargetCreatureType;
                MaxAffectedTargets = _target.MaxTargets;
                MaxTargetLevel = _target.MaxTargetLevel;
            }

            // SpellTotemsEntry
            SpellTotemsRecord _totem = data.Totems;
            for (var i = 0; i < 2; ++i)
            {
                TotemCategory[i] = _totem != null ? _totem.RequiredTotemCategoryID[i] : 0u;
                Totem[i] = _totem != null ? _totem.Totem[i] : 0;
            }
            ChainEntry = null;
            ExplicitTargetMask = 0;

            _spellSpecific = SpellSpecificType.Normal;
            _auraState = AuraStateType.None;
        }

        public bool HasEffect(Difficulty difficulty, SpellEffectName effect)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (var eff in effects)
            {
                if (eff != null && eff.IsEffect(effect))
                    return true;
            }

            return false;
        }

        public bool HasEffect(SpellEffectName effect)
        {
            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo eff in pair.Value)
                {
                    if (eff != null && eff.IsEffect(effect))
                        return true;
                }

            }
            return false;
        }

        public bool HasAura(Difficulty difficulty, AuraType aura)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect != null && effect.IsAura(aura))
                    return true;
            }
            return false;
        }

        public bool HasAreaAuraEffect(Difficulty difficulty)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect != null && effect.IsAreaAuraEffect())
                    return true;
            }

            return false;
        }

        public bool HasAreaAuraEffect()
        {
            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect != null && effect.IsAreaAuraEffect())
                        return true;
                }
            }
            return false;
        }

        public bool HasOnlyDamageEffects()
        {
            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect == null)
                        continue;

                    switch (effect.Effect)
                    {
                        case SpellEffectName.WeaponDamage:
                        case SpellEffectName.WeaponDamageNoschool:
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
            }

            return true;
        }

        public bool IsExplicitDiscovery()
        {
            SpellEffectInfo effect0 = GetEffect(Difficulty.None, 0);
            SpellEffectInfo effect1 = GetEffect(Difficulty.None, 1);

            return ((effect0 != null && (effect0.Effect == SpellEffectName.CreateRandomItem || effect0.Effect == SpellEffectName.CreateLoot))
                && effect1 != null && effect1.Effect == SpellEffectName.ScriptEffect)
                || Id == 64323;
        }

        public bool IsLootCrafting()
        {
            return HasEffect(SpellEffectName.CreateRandomItem) || HasEffect(SpellEffectName.CreateLoot);
        }

        public bool IsQuestTame()
        {
            SpellEffectInfo effect0 = GetEffect(Difficulty.None, 0);
            SpellEffectInfo effect1 = GetEffect(Difficulty.None, 1);
            return effect0 != null && effect1 != null && effect0.Effect == SpellEffectName.Threat && effect1.Effect == SpellEffectName.ApplyAura
                && effect1.ApplyAuraName == AuraType.Dummy;
        }

        public bool IsProfession(Difficulty difficulty = Difficulty.None)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect != null && effect.Effect == SpellEffectName.Skill)
                {
                    uint skill = (uint)effect.MiscValue;

                    if (Global.SpellMgr.IsProfessionSkill(skill))
                        return true;
                }
            }
            return false;
        }

        public bool IsPrimaryProfession(Difficulty difficulty)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect != null && effect.Effect == SpellEffectName.Skill)
                {
                    uint skill = (uint)effect.MiscValue;

                    if (Global.SpellMgr.IsPrimaryProfessionSkill(skill))
                        return true;
                }
            }
            return false;
        }

        public bool IsPrimaryProfessionFirstRank(Difficulty difficulty = Difficulty.None)
        {
            return IsPrimaryProfession(difficulty) && GetRank() == 1;
        }

        public bool IsAbilityOfSkillType(SkillType skillType)
        {
            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(Id);

            foreach (var spell_idx in bounds)
                if (spell_idx.SkillLine == (uint)skillType)
                    return true;

            return false;
        }

        public bool IsAffectingArea(Difficulty difficulty)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect != null && effect.IsEffect() && (effect.IsTargetingArea() || effect.IsEffect(SpellEffectName.PersistentAreaAura) || effect.IsAreaAuraEffect()))
                    return true;
            }
            return false;
        }

        // checks if spell targets are selected from area, doesn't include spell effects in check (like area wide auras for example)
        public bool IsTargetingArea(Difficulty difficulty)
        {
            var effects = GetEffectsForDifficulty(difficulty);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect != null && effect.IsEffect() && effect.IsTargetingArea())
                    return true;
            }
            return false;
        }

        public bool NeedsExplicitUnitTarget()
        {
            return Convert.ToBoolean(GetExplicitTargetMask() & SpellCastTargetFlags.UnitMask);
        }

        public bool NeedsToBeTriggeredByCaster(SpellInfo triggeringSpell, Difficulty difficulty)
        {
            if (NeedsExplicitUnitTarget())
                return true;

            if (triggeringSpell.IsChanneled())
            {
                SpellCastTargetFlags mask = 0;
                var effects = GetEffectsForDifficulty(difficulty);
                foreach (SpellEffectInfo effect in effects)
                {
                    if (effect != null && (effect.TargetA.GetTarget() != Targets.UnitCaster && effect.TargetA.GetTarget() != Targets.DestCaster
                        && effect.TargetB.GetTarget() != Targets.UnitCaster && effect.TargetB.GetTarget() != Targets.DestCaster))
                    {
                        mask |= effect.GetProvidedTargetMask();
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
            if (HasAttribute(SpellAttr1.UnautocastableByPet))
                return false;
            return true;
        }

        public bool IsStackableWithRanks()
        {
            if (IsPassive())
                return false;

            // All stance spells. if any better way, change it.
            var effects = GetEffectsForDifficulty(Difficulty.None);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect == null)
                    continue;

                switch (SpellFamilyName)
                {
                    case SpellFamilyNames.Paladin:
                        // Paladin aura Spell
                        if (effect.Effect == SpellEffectName.ApplyAreaAuraRaid)
                            return false;
                        break;
                    case SpellFamilyNames.Druid:
                        // Druid form Spell
                        if (effect.Effect == SpellEffectName.ApplyAura &&
                            effect.ApplyAuraName == AuraType.ModShapeshift)
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
            return StackAmount > 1 && !IsChanneled() && !HasAttribute(SpellAttr3.StackForDiffCasters);
        }

        public bool IsCooldownStartedOnEvent()
        {
            if (HasAttribute(SpellAttr0.DisabledWhileActive))
                return true;

            SpellCategoryRecord category = CliDB.SpellCategoryStorage.LookupByKey(CategoryId);
            return category != null && category.Flags.HasAnyFlag(SpellCategoryFlags.CooldownStartsOnEvent);
        }

        public bool IsDeathPersistent()
        {
            return HasAttribute(SpellAttr3.DeathPersistent);
        }

        public bool IsRequiringDeadTarget()
        {
            return HasAttribute(SpellAttr3.OnlyTargetGhosts);
        }

        public bool IsAllowingDeadTarget()
        {
            return HasAttribute(SpellAttr2.CanTargetDead)
                || Convert.ToBoolean(targets & (SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitDead));
        }

        public bool IsGroupBuff()
        {
            var effects = GetEffectsForDifficulty(Difficulty.None);
            foreach (SpellEffectInfo effect in effects)
            {
                if (effect == null)
                    continue;

                switch (effect.TargetA.GetCheckType())
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

        public bool CanBeUsedInCombat()
        {
            return !HasAttribute(SpellAttr0.CantUsedInCombat);
        }

        public bool IsPositive()
        {
            return !HasAttribute(SpellCustomAttributes.Negative);
        }

        public bool IsPositiveEffect(uint effIndex)
        {
            switch (effIndex)
            {
                default:
                case 0:
                    return !HasAttribute(SpellCustomAttributes.NegativeEff0);
                case 1:
                    return !HasAttribute(SpellCustomAttributes.NegativeEff1);
                case 2:
                    return !HasAttribute(SpellCustomAttributes.NegativeEff2);
            }
        }

        public bool IsChanneled()
        {
            return HasAttribute(SpellAttr1.Channeled1 | SpellAttr1.Channeled2);
        }

        public bool IsMoveAllowedChannel()
        {
            return IsChanneled() && HasAttribute(SpellAttr5.CanChannelWhenMoving);
        }

        public bool NeedsComboPoints()
        {
            return HasAttribute(SpellAttr1.ReqComboPoints1 | SpellAttr1.ReqComboPoints2);
        }

        public bool IsNextMeleeSwingSpell()
        {
            return HasAttribute(SpellAttr0.OnNextSwing | SpellAttr0.OnNextSwing2);
        }

        public bool IsBreakingStealth()
        {
            return !HasAttribute(SpellAttr1.NotBreakStealth);
        }

        public bool IsRangedWeaponSpell()
        {
            return (SpellFamilyName == SpellFamilyNames.Hunter && !SpellFamilyFlags[1].HasAnyFlag(0x10000000u)) // for 53352, cannot find better way
                || Convert.ToBoolean(EquippedItemSubClassMask & (int)ItemSubClassWeapon.MaskRanged);
        }

        public bool IsAutoRepeatRangedSpell()
        {
            return HasAttribute(SpellAttr2.AutorepeatFlag);
        }

        public bool HasInitialAggro()
        {
            return !(HasAttribute(SpellAttr1.NoThreat) || HasAttribute(SpellAttr3.NoInitialAggro));
        }

        public WeaponAttackType GetAttackType()
        {
            WeaponAttackType result;
            switch (DmgClass)
            {
                case SpellDmgClass.Melee:
                    if (HasAttribute(SpellAttr3.ReqOffhand))
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
            if (item && item.IsFitToSpellRequirements(this))
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

        bool IsAffectedBySpellMods()
        {
            return !HasAttribute(SpellAttr3.NoDoneBonus);
        }

        public bool IsAffectedBySpellMod(SpellModifier mod)
        {
            if (!IsAffectedBySpellMods())
                return false;

            SpellInfo affectSpell = Global.SpellMgr.GetSpellInfo(mod.spellId);
            if (affectSpell == null)
                return false;

            return IsAffected(affectSpell.SpellFamilyName, mod.mask);
        }

        public bool CanPierceImmuneAura(SpellInfo auraSpellInfo)
        {
            // aura can't be pierced
            if (auraSpellInfo == null || auraSpellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return false;

            // these spells pierce all avalible spells (Resurrection Sickness for example)
            if (HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return true;

            // these spells (Cyclone for example) can pierce all...
            if (HasAttribute(SpellAttr1.UnaffectedBySchoolImmune) || HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune))
            {
                // ...but not these (Divine shield, Ice block, Cyclone and Banish for example)
                if (auraSpellInfo == null ||
                    (auraSpellInfo.Mechanic != Mechanics.ImmuneShield &&
                        auraSpellInfo.Mechanic != Mechanics.Invulnerability &&
                        (auraSpellInfo.Mechanic != Mechanics.Banish || (IsRankOf(auraSpellInfo) && auraSpellInfo.Dispel != DispelType.None)))) // Banish shouldn't be immune to itself, but Cyclone should
                    return true;
            }

            // Dispels other auras on immunity, check if this spell makes the unit immune to aura
            if (HasAttribute(SpellAttr1.DispelAurasOnImmunity) && CanSpellProvideImmunityAgainstAura(auraSpellInfo))
                return true;

            return false;
        }

        public bool CanDispelAura(SpellInfo auraSpellInfo)
        {
            // These auras (like Divine Shield) can't be dispelled
            if (auraSpellInfo.HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return false;

            // These spells (like Mass Dispel) can dispel all auras
            if (HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return true;

            // These auras (Cyclone for example) are not dispelable
            if (auraSpellInfo.HasAttribute(SpellAttr1.UnaffectedBySchoolImmune) || auraSpellInfo.HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune))
                return false;

            return true;
        }

        public bool IsSingleTarget()
        {
            // all other single target spells have if it has AttributesEx5
            if (HasAttribute(SpellAttr5.SingleTargetSpell))
                return true;

            switch (GetSpellSpecific())
            {
                case SpellSpecificType.Judgement:
                    return true;
                default:
                    break;
            }

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
                case SpellSpecificType.Judgement:
                case SpellSpecificType.WarlockCorruption:
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
            if (CliDB.GetTalentSpellCost(Id) > 0 &&
            (Effects[0].Effect == SpellEffects.LearnSpell || Effects[1].Effect == SpellEffects.LearnSpell || Effects[2].Effect == SpellEffects.LearnSpell))
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
                actAsShifted = !shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.IsNotAShapeshift);
            }

            if (actAsShifted)
            {
                if (HasAttribute(SpellAttr0.NotShapeshift) || (shapeInfo != null && shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.PreventUsingOwnSkills))) // not while shapeshifted
                    return SpellCastResult.NotShapeshift;
                else if (Stances != 0)   // needs other shapeshift
                    return SpellCastResult.OnlyShapeshift;
            }
            else
            {
                // needs shapeshift
                if (!HasAttribute(SpellAttr2.NotNeedShapeshift) && Stances != 0)
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
                    if (areaId == zone_id || areaId == area_id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return SpellCastResult.IncorrectArea;
            }

            // continent limitation (virtual continent)
            if (HasAttribute(SpellAttr4.CastOnlyInOutland))
            {
                uint mountFlags = 0;
                if (player && player.HasAuraType(AuraType.MountRestrictions))
                {
                    foreach (AuraEffect auraEffect in player.GetAuraEffectsByType(AuraType.MountRestrictions))
                        mountFlags |= (uint)auraEffect.GetMiscValue();
                }
                else
                {
                    AreaTableRecord areaTable = CliDB.AreaTableStorage.LookupByKey(area_id);
                    if (areaTable != null)
                        mountFlags = areaTable.MountFlags;
                }
                if (!Convert.ToBoolean(mountFlags & (uint)AreaMountFlags.FlyingAllowed))
                    return SpellCastResult.IncorrectArea;

                if (player)
                {
                    uint mapToCheck = map_id;
                    MapRecord mapEntry1 = CliDB.MapStorage.LookupByKey(map_id);
                    if (mapEntry1 != null)
                        mapToCheck = (uint)mapEntry1.CosmeticParentMapID;
                    if ((mapToCheck == 1116 || mapToCheck == 1464) && !player.HasSpell(191645)) // Draenor Pathfinder
                        return SpellCastResult.IncorrectArea;
                    else if (mapToCheck == 1220 && !player.HasSpell(233368)) // Broken Isles Pathfinder
                        return SpellCastResult.IncorrectArea;
                    else if ((mapToCheck == 1642 || mapToCheck == 1643) && !player.HasSpell(278833)) // Battle for Azeroth Pathfinder
                        return SpellCastResult.IncorrectArea;
                }
            }

            var mapEntry = CliDB.MapStorage.LookupByKey(map_id);

            // raid instance limitation
            if (HasAttribute(SpellAttr6.NotInRaidInstance))
            {
                if (mapEntry == null || mapEntry.IsRaid())
                    return SpellCastResult.NotInRaidInstance;
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
                case 34976:         // Netherstorm Flag
                    return map_id == 566 && player != null && player.InBattleground() ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                case 2584:          // Waiting to Resurrect
                case 22011:         // Spirit Heal Channel
                case 22012:         // Spirit Heal
                case 24171:         // Resurrection Impact Visual
                case 42792:         // Recently Dropped Flag
                case 43681:         // Inactive
                case 44535:         // Spirit Heal (mana)
                    if (mapEntry == null)
                        return SpellCastResult.IncorrectArea;

                    return zone_id == 4197 || (mapEntry.IsBattleground() && player != null && player.InBattleground()) ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                case 44521:         // Preparation
                    {
                        if (player == null)
                            return SpellCastResult.RequiresArea;

                        if (mapEntry == null)
                            return SpellCastResult.IncorrectArea;

                        if (!mapEntry.IsBattleground())
                            return SpellCastResult.RequiresArea;

                        Battleground bg = player.GetBattleground();
                        return bg && bg.GetStatus() == BattlegroundStatus.WaitJoin ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
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
                        return bg && bg.GetStatus() == BattlegroundStatus.WaitJoin ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
                    }
            }

            // aura limitations
            if (player)
            {
                foreach (SpellEffectInfo effect in GetEffectsForDifficulty(player.GetMap().GetDifficultyID()))
                {
                    if (effect == null || !effect.IsAura())
                        continue;

                    switch (effect.ApplyAuraName)
                    {
                        case AuraType.ModShapeshift:
                            {
                                SpellShapeshiftFormRecord spellShapeshiftForm = CliDB.SpellShapeshiftFormStorage.LookupByKey(effect.MiscValue);
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
                                uint mountType = (uint)effect.MiscValueB;
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

        public SpellCastResult CheckTarget(Unit caster, WorldObject target, bool Implicit = true)
        {
            if (HasAttribute(SpellAttr1.CantTargetSelf) && caster == target)
                return SpellCastResult.BadTargets;

            // check visibility - ignore stealth for implicit (area) targets
            if (!HasAttribute(SpellAttr6.CanTargetInvisible) && !caster.CanSeeOrDetect(target, Implicit))
                return SpellCastResult.BadTargets;

            Unit unitTarget = target.ToUnit();

            // creature/player specific target checks
            if (unitTarget != null)
            {
                if (HasAttribute(SpellAttr1.CantTargetInCombat))
                {
                    if (unitTarget.IsInCombat())
                        return SpellCastResult.TargetAffectingCombat;
                    // player with active pet counts as a player in combat
                    else if (unitTarget.IsTypeId(TypeId.Player))
                    {
                        Pet pet = unitTarget.ToPlayer().GetPet();
                        if (pet)
                            if (pet.GetVictim() && !pet.HasUnitState(UnitState.Controlled))
                                return SpellCastResult.TargetAffectingCombat;
                    }
                }

                // only spells with SPELL_ATTR3_ONLY_TARGET_GHOSTS can target ghosts
                if (HasAttribute(SpellAttr3.OnlyTargetGhosts) != unitTarget.HasAuraType(AuraType.Ghost))
                {
                    if (HasAttribute(SpellAttr3.OnlyTargetGhosts))
                        return SpellCastResult.TargetNotGhost;
                    else
                        return SpellCastResult.BadTargets;
                }

                if (caster != unitTarget)
                {
                    if (caster.IsTypeId(TypeId.Player))
                    {
                        // Do not allow these spells to target creatures not tapped by us (Banish, Polymorph, many quest spells)
                        if (HasAttribute(SpellAttr2.CantTargetTapped))
                        {
                            Creature targetCreature = unitTarget.ToCreature();
                            if (targetCreature != null)
                                if (targetCreature.hasLootRecipient() && !targetCreature.isTappedBy(caster.ToPlayer()))
                                    return SpellCastResult.CantCastOnTapped;
                        }

                        if (HasAttribute(SpellCustomAttributes.PickPocket))
                        {
                            if (unitTarget.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;
                            else if ((unitTarget.GetCreatureTypeMask() & (uint)CreatureType.MaskHumanoidOrUndead) == 0)
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
            if (HasAttribute(SpellAttr3.OnlyTargetPlayers) && !unitTarget.IsTypeId(TypeId.Player))
                return SpellCastResult.TargetNotPlayer;

            if (!IsAllowingDeadTarget() && !unitTarget.IsAlive())
                return SpellCastResult.TargetsDead;

            // check this flag only for implicit targets (chain and area), allow to explicitly target units for spells like Shield of Righteousness
            if (Implicit && HasAttribute(SpellAttr6.CantTargetCrowdControlled) && !unitTarget.CanFreeMove())
                return SpellCastResult.BadTargets;

            if (!CheckTargetCreatureType(unitTarget))
            {
                if (target.IsTypeId(TypeId.Player))
                    return SpellCastResult.TargetIsPlayer;
                else
                    return SpellCastResult.BadTargets;
            }

            // check GM mode and GM invisibility - only for player casts (npc casts are controlled by AI) and negative spells
            if (unitTarget != caster && (caster.IsControlledByPlayer() || !IsPositive()) && unitTarget.IsTypeId(TypeId.Player))
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
            if (!caster.IsVehicle() && caster.GetCharmerOrOwner() != target)
            {
                if (TargetAuraState != 0 && !unitTarget.HasAuraState(TargetAuraState, this, caster))
                    return SpellCastResult.TargetAurastate;

                if (TargetAuraStateNot != 0 && unitTarget.HasAuraState(TargetAuraStateNot, this, caster))
                    return SpellCastResult.TargetAurastate;
            }

            if (TargetAuraSpell != 0 && !unitTarget.HasAura(TargetAuraSpell))
                return SpellCastResult.TargetAurastate;

            if (ExcludeTargetAuraSpell != 0 && unitTarget.HasAura(ExcludeTargetAuraSpell))
                return SpellCastResult.TargetAurastate;

            if (unitTarget.HasAuraType(AuraType.PreventResurrection))
                if (HasEffect(caster.GetMap().GetDifficultyID(), SpellEffectName.SelfResurrect) || HasEffect(caster.GetMap().GetDifficultyID(), SpellEffectName.Resurrect))
                    return SpellCastResult.TargetCannotBeResurrected;

            if (HasAttribute(SpellAttr8.BattleResurrection))
            {
                Map map = caster.GetMap();
                if (map)
                {
                    InstanceMap iMap = map.ToInstanceMap();
                    if (iMap)
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

        public SpellCastResult CheckExplicitTarget(Unit caster, WorldObject target, Item itemTarget = null)
        {
            SpellCastTargetFlags neededTargets = GetExplicitTargetMask();
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
                if (Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.UnitAlly | SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitMinipet | SpellCastTargetFlags.UnitPassenger)))
                {
                    if (Convert.ToBoolean(neededTargets & SpellCastTargetFlags.UnitEnemy))
                        if (caster._IsValidAttackTarget(unitTarget, this))
                            return SpellCastResult.SpellCastOk;
                    if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitAlly)
                        || (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitParty) && caster.IsInPartyWith(unitTarget))
                        || (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitRaid) && caster.IsInRaidWith(unitTarget)))
                        if (caster._IsValidAssistTarget(unitTarget, this))
                            return SpellCastResult.SpellCastOk;
                    if (Convert.ToBoolean(neededTargets & SpellCastTargetFlags.UnitMinipet))
                        if (unitTarget.GetGUID() == caster.GetCritterGUID())
                            return SpellCastResult.SpellCastOk;
                    if (Convert.ToBoolean(neededTargets & SpellCastTargetFlags.UnitPassenger))
                        if (unitTarget.IsOnVehicle(caster))
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
            if (vehicle)
            {
                VehicleSeatFlags checkMask = 0;
                foreach (SpellEffectInfo effect in GetEffectsForDifficulty(caster.GetMap().GetDifficultyID()))
                {
                    if (effect != null && effect.ApplyAuraName == AuraType.ModShapeshift)
                    {
                        var shapeShiftFromEntry = CliDB.SpellShapeshiftFormStorage.LookupByKey((uint)effect.MiscValue);
                        if (shapeShiftFromEntry != null && !shapeShiftFromEntry.Flags.HasAnyFlag(SpellShapeshiftFormFlags.IsNotAShapeshift))
                            checkMask |= VehicleSeatFlags.Uncontrolled;
                        break;
                    }
                }

                if (HasAura(caster.GetMap().GetDifficultyID(), AuraType.Mounted))
                    checkMask |= VehicleSeatFlags.CanCastMountSpell;

                if (checkMask == 0)
                    checkMask = VehicleSeatFlags.CanAttack;

                var vehicleSeat = vehicle.GetSeatForPassenger(caster);
                if (!HasAttribute(SpellAttr6.CastableWhileOnVehicle) && !HasAttribute(SpellAttr0.CastableWhileMounted)
                    && (vehicleSeat.Flags & checkMask) != checkMask)
                    return SpellCastResult.CantDoThatRightNow;

                // Can only summon uncontrolled minions/guardians when on controlled vehicle
                if (vehicleSeat.Flags.HasAnyFlag((VehicleSeatFlags.CanControl | VehicleSeatFlags.Unk2)))
                {
                    foreach (SpellEffectInfo effect in GetEffectsForDifficulty(caster.GetMap().GetDifficultyID()))
                    {
                        if (effect == null || effect.Effect != SpellEffectName.Summon)
                            continue;

                        var props = CliDB.SummonPropertiesStorage.LookupByKey(effect.MiscValueB);
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
            //if (target.IsMagnet())
            //return true;

            uint creatureType = target.GetCreatureTypeMask();
            return TargetCreatureType == 0 || creatureType == 0 || Convert.ToBoolean(creatureType & TargetCreatureType);
        }

        public SpellSchoolMask GetSchoolMask()
        {
            return SchoolMask;
        }

        public uint GetAllEffectsMechanicMask()
        {
            uint mask = 0;
            if (Mechanic != 0)
                mask |= (uint)(1 << (int)Mechanic);

            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect != null && effect.IsEffect() && effect.Mechanic != 0)
                        mask |= (uint)(1 << (int)effect.Mechanic);
                }
            }
            return mask;
        }

        public uint GetEffectMechanicMask(byte effIndex)
        {
            uint mask = 0;
            if (Mechanic != 0)
                mask |= (uint)(1 << (int)Mechanic);

            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect != null && effect.EffectIndex == effIndex && effect.IsEffect() && effect.Mechanic != 0)
                        mask |= (uint)(1 << (int)effect.Mechanic);
                }
            }
            return mask;
        }

        public uint GetSpellMechanicMaskByEffectMask(uint effectMask)
        {
            uint mask = 0;
            if (Mechanic != 0)
                mask |= (uint)(1 << (int)Mechanic);

            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect != null && Convert.ToBoolean(effectMask & (1 << (int)effect.EffectIndex)) && effect.Mechanic != 0)
                        mask |= (uint)(1 << (int)effect.Mechanic);
                }
            }
            return mask;
        }

        public Mechanics GetEffectMechanic(uint effIndex, Difficulty difficulty)
        {
            SpellEffectInfo effect = GetEffect(difficulty, effIndex);
            if (effect != null && effect.IsEffect() && effect.Mechanic != 0)
                return effect.Mechanic;
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

            // Seals
            if (GetSpellSpecific() == SpellSpecificType.Seal)
                _auraState = AuraStateType.Judgement;

            // Conflagrate aura state on Immolate and Shadowflame
            if (SpellFamilyName == SpellFamilyNames.Warlock &&
                // Immolate
                (SpellFamilyFlags[0].HasAnyFlag(4u) ||
                // Shadowflame
                SpellFamilyFlags[2].HasAnyFlag(2u)))
                _auraState = AuraStateType.Conflagrate;

            // Faerie Fire (druid versions)
            if (SpellFamilyName == SpellFamilyNames.Druid && SpellFamilyFlags[0].HasAnyFlag(0x400u))
                _auraState = AuraStateType.FaerieFire;

            // Sting (hunter's pet ability)
            if (GetCategory() == 1133)
                _auraState = AuraStateType.FaerieFire;

            // Victorious
            if (SpellFamilyName == SpellFamilyNames.Warrior && SpellFamilyFlags[1].HasAnyFlag(0x40000u))
                _auraState = AuraStateType.WarriorVictoryRush;

            // Swiftmend state on Regrowth & Rejuvenation
            if (SpellFamilyName == SpellFamilyNames.Druid && SpellFamilyFlags[0].HasAnyFlag(0x50u))
                _auraState = AuraStateType.Swiftmend;

            // Deadly poison aura state
            if (SpellFamilyName == SpellFamilyNames.Rogue && SpellFamilyFlags[0].HasAnyFlag(0x10000u))
                _auraState = AuraStateType.DeadlyPoison;

            // Enrage aura state
            if (Dispel == DispelType.Enrage)
                _auraState = AuraStateType.Enrage;

            // Bleeding aura state
            if (Convert.ToBoolean(GetAllEffectsMechanicMask() & 1 << (int)Mechanics.Bleed))
                _auraState = AuraStateType.Bleeding;

            if (Convert.ToBoolean(GetSchoolMask() & SpellSchoolMask.Frost))
            {
                foreach (var pair in _effects)
                    foreach (SpellEffectInfo effect in pair.Value)
                        if (effect != null && (effect.IsAura(AuraType.ModStun) || effect.IsAura(AuraType.ModRoot)))
                            _auraState = AuraStateType.Frozen;
            }

            switch (Id)
            {
                case 71465: // Divine Surge
                case 50241: // Evasive Charges
                    _auraState = AuraStateType.Unk22;
                    break;
                default:
                    break;
            }
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
                        if (HasAuraInterruptFlag(SpellAuraInterruptFlags.NotSeated))
                        {
                            bool food = false;
                            bool drink = false;
                            foreach (var pair in _effects)
                            {
                                foreach (SpellEffectInfo effect in pair.Value)
                                {
                                    if (effect == null || !effect.IsAura())
                                        continue;
                                    switch (effect.ApplyAuraName)
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
                        SpellEffectInfo effect = GetEffect(Difficulty.None, 0);
                        if (effect != null && SpellFamilyFlags[0].HasAnyFlag(0x1000000u) && effect.IsAura(AuraType.ModConfuse))
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

                        //seed of corruption and corruption
                        if (SpellFamilyFlags[1].HasAnyFlag(0x10u) || SpellFamilyFlags[0].HasAnyFlag(0x2u))
                            _spellSpecific = SpellSpecificType.WarlockCorruption;
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

                        // Judgement
                        if (Id == 20271)
                            _spellSpecific = SpellSpecificType.Judgement;

                        // only paladin auras have this (for palaldin class family)
                        if (SpellFamilyFlags[2].HasAnyFlag(0x00000020u))
                            _spellSpecific = SpellSpecificType.Aura;

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

            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect != null && effect.Effect == SpellEffectName.ApplyAura)
                    {
                        switch (effect.ApplyAuraName)
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
        }

        public void _LoadSpellDiminishInfo()
        {
            SpellDiminishInfo diminishInfo = new SpellDiminishInfo();
            diminishInfo.DiminishGroup = diminishingGroupCompute();
            diminishInfo.DiminishReturnType = diminishingTypeCompute(diminishInfo.DiminishGroup);
            diminishInfo.DiminishMaxLevel = diminishingMaxLevelCompute(diminishInfo.DiminishGroup);
            diminishInfo.DiminishDurationLimit = diminishingLimitDurationCompute();

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

        DiminishingGroup diminishingGroupCompute()
        {
            if (IsPositive())
                return DiminishingGroup.None;

            if (HasAura(Difficulty.None, AuraType.ModTaunt))
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

        DiminishingReturnsType diminishingTypeCompute(DiminishingGroup group)
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

        DiminishingLevels diminishingMaxLevelCompute(DiminishingGroup group)
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

        int diminishingLimitDurationCompute()
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
            var loadImmunityInfoFn = new Action<SpellEffectInfo>(effectInfo =>
            {
                uint schoolImmunityMask = 0;
                uint applyHarmfulAuraImmunityMask = 0;
                uint mechanicImmunityMask = 0;
                uint dispelImmunity = 0;
                uint damageImmunityMask = 0;

                int miscVal = effectInfo.MiscValue;
                int amount = effectInfo.CalcValue();

                ImmunityInfo immuneInfo = effectInfo.GetImmunityInfo();

                switch (effectInfo.ApplyAuraName)
                {
                    case AuraType.MechanicImmunityMask:
                        {
                            switch (miscVal)
                            {
                                case 96:   // Free Friend, Uncontrollable Frenzy, Warlord's Presence
                                    {
                                        mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                        break;
                                    }
                                case 1615: // Incite Rage, Wolf Spirit, Overload, Lightning Tendrils
                                    {
                                        switch (Id)
                                        {
                                            case 43292: // Incite Rage
                                            case 49172: // Wolf Spirit
                                                mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                                immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                                immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                                immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                                goto case 61869;
                                            // no break intended
                                            case 61869: // Overload
                                            case 63481:
                                            case 61887: // Lightning Tendrils
                                            case 63486:
                                                mechanicImmunityMask |= (1 << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence);

                                                immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBack);
                                                immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBackDest);
                                                break;
                                            default:
                                                break;
                                        }
                                        break;
                                    }
                                case 679:  // Mind Control, Avenging Fury
                                    {
                                        if (Id == 57742) // Avenging Fury
                                        {
                                            mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                        }
                                        break;
                                    }
                                case 1557: // Startling Roar, Warlord Roar, Break Bonds, Stormshield
                                    {
                                        if (Id == 64187) // Stormshield
                                        {
                                            mechanicImmunityMask |= (1 << (int)Mechanics.Stun);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                        }
                                        else
                                        {
                                            mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                        }
                                        break;
                                    }
                                case 1614: // Fixate
                                case 1694: // Fixated, Lightning Tendrils
                                    {
                                        immuneInfo.SpellEffectImmune.Add(SpellEffectName.AttackMe);
                                        immuneInfo.AuraTypeImmune.Add(AuraType.ModTaunt);
                                        break;
                                    }
                                case 1630: // Fervor, Berserk
                                    {
                                        if (Id == 64112) // Berserk
                                        {
                                            immuneInfo.SpellEffectImmune.Add(SpellEffectName.AttackMe);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModTaunt);
                                        }
                                        else
                                        {
                                            mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                        }
                                        break;
                                    }
                                case 477:  // Bladestorm
                                case 1733: // Bladestorm, Killing Spree
                                    {
                                        if (amount == 0)
                                        {
                                            mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                            immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBack);
                                            immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBackDest);

                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                        }
                                        break;
                                    }
                                case 878: // Whirlwind, Fog of Corruption, Determination
                                    {
                                        if (Id == 66092) // Determination
                                        {
                                            mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Stun)
                                                | (1 << (int)Mechanics.Disoriented) | (1 << (int)Mechanics.Freeze);

                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                        }
                                        break;
                                    }
                                default:
                                    break;
                            }

                            if (immuneInfo.AuraTypeImmune.Empty())
                            {
                                if (miscVal.HasAnyFlag(1 << 10))
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                if (miscVal.HasAnyFlag(1 << 1))
                                    immuneInfo.AuraTypeImmune.Add(AuraType.Transform);

                                // These flag can be recognized wrong:
                                if (miscVal.HasAnyFlag(1 << 6))
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                if (miscVal.HasAnyFlag(1 << 0))
                                {
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                }
                                if (miscVal.HasAnyFlag(1 << 2))
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                if (miscVal.HasAnyFlag(1 << 9))
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                if (miscVal.HasAnyFlag(1 << 7))
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModDisarm);
                            }
                            break;
                        }
                    case AuraType.MechanicImmunity:
                        {
                            switch (Id)
                            {
                                case 34471: // The Beast Within
                                case 19574: // Bestial Wrath
                                case 42292: // PvP trinket
                                case 46227: // Medallion of Immunity
                                case 59752: // Every Man for Himself
                                case 53490: // Bullheaded
                                case 65547: // PvP Trinket
                                case 134946: // Supremacy of the Alliance
                                case 134956: // Supremacy of the Horde
                                case 195710: // Honorable Medallion
                                case 208683: // Gladiator's Medallion
                                    mechanicImmunityMask |= (uint)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;
                                    break;
                                case 54508: // Demonic Empowerment
                                    mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Stun);
                                    break;
                                default:
                                    if (miscVal < 1)
                                        return;

                                    mechanicImmunityMask |= 1u << miscVal;
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
                            dispelImmunity = (uint)miscVal;
                            break;
                        }
                    default:
                        break;
                }

                immuneInfo.SchoolImmuneMask = schoolImmunityMask;
                immuneInfo.ApplyHarmfulAuraImmuneMask = applyHarmfulAuraImmunityMask;
                immuneInfo.MechanicImmuneMask = mechanicImmunityMask;
                immuneInfo.DispelImmune = dispelImmunity;
                immuneInfo.DamageSchoolMask = damageImmunityMask;
            });

            foreach (var effects in _effects)
            {
                foreach (SpellEffectInfo effect in effects.Value)
                {
                    if (effect == null)
                        continue;

                    loadImmunityInfoFn(effect);
                }
            }
        }

        public void ApplyAllSpellImmunitiesTo(Unit target, SpellEffectInfo effect, bool apply)
        {
            ImmunityInfo immuneInfo = effect.GetImmunityInfo();

            uint schoolImmunity = immuneInfo.SchoolImmuneMask;
            if (schoolImmunity != 0)
            {
                target.ApplySpellImmune(Id, SpellImmunity.School, schoolImmunity, apply);

                if (apply && HasAttribute(SpellAttr1.DispelAurasOnImmunity))
                {
                    target.RemoveAppliedAuras(aurApp =>
                    {
                        SpellInfo auraSpellInfo = aurApp.GetBase().GetSpellInfo();
                        return (((uint)auraSpellInfo.GetSchoolMask() & schoolImmunity) != 0 && // Check for school mask
                            CanDispelAura(auraSpellInfo) &&
                            (IsPositive() != aurApp.IsPositive()) &&                     // Check spell vs aura possitivity
                            !auraSpellInfo.IsPassive() &&                                // Don't remove passive auras
                            auraSpellInfo.Id != Id);                                     // Don't remove self
                    });
                }
            }

            uint mechanicImmunity = immuneInfo.MechanicImmuneMask;
            if (mechanicImmunity != 0)
            {
                for (uint i = 0; i < (int)Mechanics.Max; ++i)
                    if (Convert.ToBoolean(mechanicImmunity & (1 << (int)i)))
                        target.ApplySpellImmune(Id, SpellImmunity.Mechanic, i, apply);

                if (apply && HasAttribute(SpellAttr1.DispelAurasOnImmunity))
                    target.RemoveAurasWithMechanic(mechanicImmunity, AuraRemoveMode.Default, Id);
            }

            uint dispelImmunity = immuneInfo.DispelImmune;
            if (dispelImmunity != 0)
            {
                target.ApplySpellImmune(Id, SpellImmunity.Dispel, dispelImmunity, apply);

                if (apply && HasAttribute(SpellAttr1.DispelAurasOnImmunity))
                {
                    target.RemoveAppliedAuras(aurApp =>
                    {
                        SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                        if ((uint)spellInfo.Dispel == dispelImmunity)
                            return true;

                        return false;
                    });
                }
            }

            uint damageImmunity = immuneInfo.DamageSchoolMask;
            if (damageImmunity != 0)
                target.ApplySpellImmune(Id, SpellImmunity.Damage, damageImmunity, apply);

            foreach (AuraType auraType in immuneInfo.AuraTypeImmune)
            {
                target.ApplySpellImmune(Id, SpellImmunity.State, auraType, apply);
                if (apply && HasAttribute(SpellAttr1.DispelAurasOnImmunity))
                    target.RemoveAurasByType(auraType);
            }

            foreach (SpellEffectName effectType in immuneInfo.SpellEffectImmune)
                target.ApplySpellImmune(Id, SpellImmunity.Effect, effectType, apply);
        }

        bool CanSpellProvideImmunityAgainstAura(SpellInfo auraSpellInfo)
        {
            if (auraSpellInfo == null)
                return false;

            foreach (SpellEffectInfo effectInfo in GetEffectsForDifficulty(Difficulty.None))
            {
                if (effectInfo == null)
                    continue;

                ImmunityInfo immuneInfo = effectInfo.GetImmunityInfo();

                if (!auraSpellInfo.HasAttribute(SpellAttr1.UnaffectedBySchoolImmune) && !auraSpellInfo.HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune))
                {
                    uint schoolImmunity = immuneInfo.SchoolImmuneMask;
                    if (schoolImmunity != 0)
                        if (((uint)auraSpellInfo.SchoolMask & schoolImmunity) != 0)
                            return true;
                }

                uint mechanicImmunity = immuneInfo.MechanicImmuneMask;
                if (mechanicImmunity != 0)
                    if ((mechanicImmunity & (1 << (int)auraSpellInfo.Mechanic)) != 0)
                        return true;

                uint dispelImmunity = immuneInfo.DispelImmune;
                if (dispelImmunity != 0)
                    if ((uint)auraSpellInfo.Dispel == dispelImmunity)
                        return true;

                bool immuneToAllEffects = true;
                foreach (SpellEffectInfo auraSpellEffectInfo in auraSpellInfo.GetEffectsForDifficulty(Difficulty.None))
                {
                    if (auraSpellEffectInfo == null)
                        continue;

                    SpellEffectName effectName = auraSpellEffectInfo.Effect;
                    if (effectName == 0)
                        continue;

                    if (!immuneInfo.SpellEffectImmune.Contains(effectName))
                    {
                        immuneToAllEffects = false;
                        break;
                    }

                    uint mechanic = (uint)auraSpellEffectInfo.Mechanic;
                    if (mechanic != 0)
                    {
                        if (!Convert.ToBoolean(immuneInfo.MechanicImmuneMask & (1 << (int)mechanic)))
                        {
                            immuneToAllEffects = false;
                            break;
                        }
                    }

                    if (!auraSpellInfo.HasAttribute(SpellAttr3.IgnoreHitResult))
                    {
                        AuraType auraName = auraSpellEffectInfo.ApplyAuraName;
                        if (auraName != 0)
                        {
                            bool isImmuneToAuraEffectApply = false;
                            if (!immuneInfo.AuraTypeImmune.Contains(auraName))
                                isImmuneToAuraEffectApply = true;

                            if (!isImmuneToAuraEffectApply && !auraSpellInfo.IsPositiveEffect(auraSpellEffectInfo.EffectIndex) && !auraSpellInfo.HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune))
                            {
                                uint applyHarmfulAuraImmunityMask = immuneInfo.ApplyHarmfulAuraImmuneMask;
                                if (applyHarmfulAuraImmunityMask != 0)
                                    if (((uint)auraSpellInfo.GetSchoolMask() & applyHarmfulAuraImmunityMask) != 0)
                                        isImmuneToAuraEffectApply = true;
                            }

                            if (!isImmuneToAuraEffectApply)
                            {
                                immuneToAllEffects = false;
                                break;
                            }
                        }
                    }
                }

                if (immuneToAllEffects)
                    return true;
            }

            return false;
        }

        public bool SpellCancelsAuraEffect(AuraEffect aurEff)
        {
            if (!HasAttribute(SpellAttr1.DispelAurasOnImmunity))
                return false;

            if (aurEff.GetSpellInfo().HasAttribute(SpellAttr0.UnaffectedByInvulnerability))
                return false;

            foreach (SpellEffectInfo effectInfo in GetEffectsForDifficulty(Difficulty.None))
            {
                if (effectInfo == null)
                    continue;

                if (effectInfo.Effect != SpellEffectName.ApplyAura)
                    continue;

                uint miscValue = (uint)effectInfo.MiscValue;
                switch (effectInfo.ApplyAuraName)
                {
                    case AuraType.StateImmunity:
                        if (miscValue != (uint)aurEff.GetSpellEffectInfo().ApplyAuraName)
                            continue;
                        break;
                    case AuraType.SchoolImmunity:
                    case AuraType.ModImmuneAuraApplySchool:
                        if (aurEff.GetSpellInfo().HasAttribute(SpellAttr2.UnaffectedByAuraSchoolImmune) || !Convert.ToBoolean((uint)aurEff.GetSpellInfo().SchoolMask & miscValue))
                            continue;
                        break;
                    case AuraType.DispelImmunity:
                        if (miscValue != (uint)aurEff.GetSpellInfo().Dispel)
                            continue;
                        break;
                    case AuraType.MechanicImmunity:
                        if (miscValue != (uint)aurEff.GetSpellInfo().Mechanic)
                        {
                            if (miscValue != (uint)aurEff.GetSpellEffectInfo().Mechanic)
                                continue;
                        }
                        break;
                    default:
                        continue;
                }

                return true;
            }

            return false;
        }

        public float GetMinRange(bool positive = false)
        {
            if (RangeEntry == null)
                return 0.0f;

            return RangeEntry.RangeMin[positive ? 1 : 0];
        }

        public float GetMaxRange(bool positive = false, Unit caster = null, Spell spell = null)
        {
            if (RangeEntry == null)
                return 0.0f;

            float range = RangeEntry.RangeMax[positive ? 1 : 0];
            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(Id, SpellModOp.Range, ref range, spell);
            }
            return range;
        }

        public int CalcDuration(Unit caster = null)
        {
            int duration = GetDuration();

            if (caster)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner)
                    modOwner.ApplySpellMod(Id, SpellModOp.Duration, ref duration);
            }

            return duration;
        }

        public int GetDuration()
        {
            if (DurationEntry == null)
                return 0;
            return (DurationEntry.Duration == -1) ? -1 : Math.Abs(DurationEntry.Duration);
        }

        public int GetMaxDuration()
        {
            if (DurationEntry == null)
                return 0;
            return (DurationEntry.MaxDuration == -1) ? -1 : Math.Abs(DurationEntry.MaxDuration);
        }

        public int CalcCastTime(uint level = 0, Spell spell = null)
        {
            int castTime = 0;
            if (CastTimeEntry != null)
            {
                int calcLevel = spell != null ? (int)spell.GetCaster().getLevel() : 0;
                if (MaxLevel != 0 && calcLevel > MaxLevel)
                    calcLevel = (int)MaxLevel;

                if (HasAttribute(SpellAttr13.Unk17))
                    calcLevel *= 5;

                if (MaxLevel != 0 && calcLevel > MaxLevel)
                    calcLevel = (int)MaxLevel;

                if (BaseLevel != 0)
                    calcLevel -= (int)BaseLevel;

                if (calcLevel < 0)
                    calcLevel = 0;

                castTime = (int)(CastTimeEntry.Base + CastTimeEntry.PerLevel * level);
                if (castTime < CastTimeEntry.Minimum)
                    castTime = CastTimeEntry.Minimum;
            }

            if (castTime <= 0)
                return 0;

            if (spell != null)
                spell.GetCaster().ModSpellCastTime(this, ref castTime, spell);

            if (HasAttribute(SpellAttr0.ReqAmmo) && (!IsAutoRepeatRangedSpell()) && !HasAttribute(SpellAttr9.AimedShot))
                castTime += 500;

            return (castTime > 0) ? castTime : 0;
        }

        public uint GetMaxTicks(Difficulty difficulty)
        {
            int DotDuration = GetDuration();
            if (DotDuration == 0)
                return 1;

            foreach (SpellEffectInfo effect in GetEffectsForDifficulty(difficulty))
            {
                if (effect != null && effect.Effect == SpellEffectName.ApplyAura)
                {
                    switch (effect.ApplyAuraName)
                    {
                        case AuraType.PeriodicDamage:
                        case AuraType.PeriodicDamagePercent:
                        case AuraType.PeriodicHeal:
                        case AuraType.ObsModHealth:
                        case AuraType.ObsModPower:
                        case AuraType.Unk48:
                        case AuraType.PowerBurn:
                        case AuraType.PeriodicLeech:
                        case AuraType.PeriodicManaLeech:
                        case AuraType.PeriodicEnergize:
                        case AuraType.PeriodicDummy:
                        case AuraType.PeriodicTriggerSpell:
                        case AuraType.PeriodicTriggerSpellWithValue:
                        case AuraType.PeriodicHealthFunnel:
                            if (effect.ApplyAuraPeriod != 0)
                                return (uint)(DotDuration / effect.ApplyAuraPeriod);
                            break;
                    }
                }
            }

            return 6;
        }

        public uint GetRecoveryTime()
        {
            return RecoveryTime > CategoryRecoveryTime ? RecoveryTime : CategoryRecoveryTime;
        }

        public List<SpellPowerCost> CalcPowerCost(Unit caster, SpellSchoolMask schoolMask)
        {
            List<SpellPowerCost> costs = new List<SpellPowerCost>();

            var collector = new Action<List<SpellPowerRecord>>(powers =>
            {
                int healthCost = 0;

                foreach (SpellPowerRecord power in powers)
                {
                    if (power.RequiredAuraSpellID != 0 && !caster.HasAura(power.RequiredAuraSpellID))
                        continue;

                    // Spell drain all exist power on cast (Only paladin lay of Hands)
                    if (HasAttribute(SpellAttr1.DrainAllPower))
                    {
                        // If power type - health drain all
                        if (power.PowerType == PowerType.Health)
                        {
                            healthCost = (int)caster.GetHealth();
                            continue;
                        }
                        // Else drain all power
                        if (power.PowerType < PowerType.Max)
                        {
                            SpellPowerCost cost = new SpellPowerCost();
                            cost.Power = power.PowerType;
                            cost.Amount = caster.GetPower(cost.Power);
                            costs.Add(cost);
                            continue;
                        }

                        Log.outError(LogFilter.Spells, "SpellInfo.GetCostDataList: Unknown power type '{0}' in spell {1}", power.PowerType, Id);
                        continue;
                    }

                    // Base powerCost
                    int powerCost = power.ManaCost;
                    // PCT cost from total amount
                    if (power.PowerCostPct != 0)
                    {
                        switch (power.PowerType)
                        {
                            // health as power used
                            case PowerType.Health:
                                powerCost += (int)MathFunctions.CalculatePct(caster.GetMaxHealth(), power.PowerCostPct);
                                break;
                            case PowerType.Mana:
                                powerCost += (int)MathFunctions.CalculatePct(caster.GetCreateMana(), power.PowerCostPct);
                                break;
                            case PowerType.Rage:
                            case PowerType.Focus:
                            case PowerType.Energy:
                                powerCost += MathFunctions.CalculatePct(caster.GetMaxPower(power.PowerType), power.PowerCostPct);
                                break;
                            case PowerType.Runes:
                            case PowerType.RunicPower:
                                Log.outDebug(LogFilter.Spells, "GetCostDataList: Not implemented yet!");
                                break;
                            default:
                                Log.outError(LogFilter.Spells, "GetCostDataList: Unknown power type '{0}' in spell {1}", power.PowerType, Id);
                                continue;
                        }
                    }

                    if (power.PowerCostMaxPct != 0)
                        healthCost += (int)MathFunctions.CalculatePct(caster.GetMaxHealth(), power.PowerCostMaxPct);

                    if (power.PowerType != PowerType.Health)
                    {
                        // Flat mod from caster auras by spell school and power type
                        var auras = caster.GetAuraEffectsByType(AuraType.ModPowerCostSchool);
                        foreach (var eff in auras)
                        {
                            if (!Convert.ToBoolean(eff.GetMiscValue() & (int)schoolMask))
                                continue;

                            if (!Convert.ToBoolean(eff.GetMiscValueB() & (1 << (int)power.PowerType)))
                                continue;

                            powerCost += eff.GetAmount();
                        }
                    }

                    // Shiv - costs 20 + weaponSpeed*10 energy (apply only to non-triggered spell with energy cost)
                    if (HasAttribute(SpellAttr4.SpellVsExtendCost))
                    {
                        uint speed = 0;
                        SpellShapeshiftFormRecord ss = CliDB.SpellShapeshiftFormStorage.LookupByKey(caster.GetShapeshiftForm());
                        if (ss != null)
                            speed = ss.CombatRoundTime;
                        else
                        {
                            WeaponAttackType slot = WeaponAttackType.BaseAttack;
                            if (HasAttribute(SpellAttr3.ReqOffhand))
                                slot = WeaponAttackType.OffAttack;

                            speed = caster.GetBaseAttackTime(slot);
                        }

                        powerCost += (int)(speed / 100);
                    }

                    // Apply cost mod by spell
                    Player modOwner = caster.GetSpellModOwner();
                    if (modOwner)
                    {
                        if (power.OrderIndex == 0)
                            modOwner.ApplySpellMod(Id, SpellModOp.Cost, ref powerCost);
                        else if (power.OrderIndex == 1)
                            modOwner.ApplySpellMod(Id, SpellModOp.SpellCost2, ref powerCost);
                    }

                    if (!caster.IsControlledByPlayer() && MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f) && SpellLevel != 0)
                    {
                        if (HasAttribute(SpellAttr0.LevelDamageCalculation))
                        {
                            GtNpcManaCostScalerRecord spellScaler = CliDB.NpcManaCostScalerGameTable.GetRow(SpellLevel);
                            GtNpcManaCostScalerRecord casterScaler = CliDB.NpcManaCostScalerGameTable.GetRow(caster.getLevel());
                            if (spellScaler != null && casterScaler != null)
                                powerCost *= (int)(casterScaler.Scaler / spellScaler.Scaler);
                        }
                    }

                    // PCT mod from user auras by spell school and power type
                    var aurasPct = caster.GetAuraEffectsByType(AuraType.ModPowerCostSchoolPct);
                    foreach (var eff in aurasPct)
                    {
                        if (!Convert.ToBoolean(eff.GetMiscValue() & (int)schoolMask))
                            continue;

                        if (!Convert.ToBoolean(eff.GetMiscValueB() & (1 << (int)power.PowerType)))
                            continue;

                        powerCost += MathFunctions.CalculatePct(powerCost, eff.GetAmount());
                    }

                    if (power.PowerType == PowerType.Health)
                    {
                        healthCost += powerCost;
                        continue;
                    }

                    bool found = false;
                    for (var i = 0; i < costs.Count; ++i)
                    {
                        var cost = costs[i];
                        if (cost.Power == power.PowerType)
                        {
                            cost.Amount += powerCost;
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        SpellPowerCost cost = new SpellPowerCost();
                        cost.Power = power.PowerType;
                        cost.Amount = powerCost;
                        costs.Add(cost);
                    }
                }

                if (healthCost > 0)
                {
                    SpellPowerCost cost = new SpellPowerCost();
                    cost.Power = PowerType.Health;
                    cost.Amount = healthCost;
                    costs.Add(cost);
                }
            });

            if (!_hasPowerDifficultyData) // optimization - use static data for 99.5% cases (4753 of 4772 in build 6.1.0.19702)
                collector(PowerCosts);
            else
                collector(Global.DB2Mgr.GetSpellPowers(Id, caster.GetMap().GetDifficultyID()));

            return costs;
        }

        float CalcPPMHasteMod(SpellProcsPerMinuteModRecord mod, Unit caster)
        {
            float haste = caster.GetFloatValue(UnitFields.ModHaste);
            float rangedHaste = caster.GetFloatValue(UnitFields.ModRangedHaste);
            float spellHaste = caster.GetFloatValue(UnitFields.ModCastHaste);
            float regenHaste = caster.GetFloatValue(UnitFields.ModHasteRegen);

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
            if (!caster.IsTypeId(TypeId.Player))
                return 0.0f;

            float crit = caster.GetFloatValue(ActivePlayerFields.CritPercentage);
            float rangedCrit = caster.GetFloatValue(ActivePlayerFields.RangedCritPercentage);
            float spellCrit = caster.GetFloatValue(ActivePlayerFields.SpellCritPercentage1);

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

            float itemLevelPoints = ItemEnchantment.GetRandomPropertyPoints((uint)itemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
            float basePoints = ItemEnchantment.GetRandomPropertyPoints(mod.Param, ItemQuality.Rare, InventoryType.Chest, 0);
            if (itemLevelPoints == basePoints)
                return 0.0f;

            return ((itemLevelPoints / basePoints) - 1.0f) * mod.Coeff;
        }

        public float CalcProcPPM(Unit caster, int itemLevel)
        {
            float ppm = ProcBasePPM;
            if (!caster)
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
                            if (caster.getClassMask().HasAnyFlag(mod.Param))
                                ppm *= 1.0f + mod.Coeff;
                            break;
                        }
                    case SpellProcsPerMinuteModType.Spec:
                        {
                            Player plrCaster = caster.ToPlayer();
                            if (plrCaster)
                                if (plrCaster.GetUInt32Value(PlayerFields.CurrentSpecId) == mod.Param)
                                    ppm *= 1.0f + mod.Coeff;
                            break;
                        }
                    case SpellProcsPerMinuteModType.Race:
                        {
                            if (caster.getRaceMask().HasAnyFlag(mod.Param))
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
            if (HasAttribute(SpellAttr0.Negative1) || HasAttribute(SpellAttr2.Unk3))
                return this;

            bool needRankSelection = false;
            foreach (SpellEffectInfo effect in GetEffectsForDifficulty(Difficulty.None))
            {
                if (effect != null && IsPositiveEffect(effect.EffectIndex) &&
                    (effect.Effect == SpellEffectName.ApplyAura ||
                    effect.Effect == SpellEffectName.ApplyAreaAuraParty ||
                    effect.Effect == SpellEffectName.ApplyAreaAuraRaid) &&
                    effect.Scaling.Coefficient != 0)
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

        public uint GetSpellXSpellVisualId(Unit caster = null)
        {
            if (caster)
            {
                Difficulty difficulty = caster.GetMap().GetDifficultyID();
                DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
                while (difficultyEntry != null)
                {
                    var visualList = _visuals.LookupByKey(difficulty);
                    if (visualList != null)
                    {
                        foreach (SpellXSpellVisualRecord visual in visualList)
                        {
                            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(visual.CasterPlayerConditionID);
                            if (playerCondition == null || (caster.IsTypeId(TypeId.Player) && ConditionManager.IsPlayerMeetingCondition(caster.ToPlayer(), playerCondition)))
                                return visual.Id;
                        }
                    }

                    difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
                }
            }

            var defaultList = _visuals.LookupByKey(Difficulty.None);
            if (defaultList != null)
            {
                foreach (var visual in defaultList)
                {
                    PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(visual.CasterPlayerConditionID);
                    if (playerCondition == null || (caster && caster.IsTypeId(TypeId.Player) && ConditionManager.IsPlayerMeetingCondition(caster.ToPlayer(), playerCondition)))
                        return visual.Id;
                }
            }

            return 0;
        }

        public uint GetSpellVisual(Unit caster = null)
        {
            var visual = CliDB.SpellXSpellVisualStorage.LookupByKey(GetSpellXSpellVisualId(caster));
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
            SpellCastTargetFlags targetMask = targets;
            // prepare target mask using effect target entries
            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect == null || !effect.IsEffect())
                        continue;

                    targetMask |= effect.TargetA.GetExplicitTargetMask(srcSet, dstSet);
                    targetMask |= effect.TargetB.GetExplicitTargetMask(srcSet, dstSet);

                    // add explicit target flags based on spell effects which have SpellEffectImplicitTargetTypes.Explicit and no valid target provided
                    if (effect.GetImplicitTargetType() != SpellEffectImplicitTargetTypes.Explicit)
                        continue;

                    // extend explicit target mask only if valid targets for effect could not be provided by target types
                    SpellCastTargetFlags effectTargetMask = effect.GetMissingTargetMask(srcSet, dstSet, targetMask);

                    // don't add explicit object/dest flags when spell has no max range
                    if (GetMaxRange(true) == 0.0f && GetMaxRange(false) == 0.0f)
                        effectTargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.DestLocation);
                    targetMask |= effectTargetMask;
                }
            }

            ExplicitTargetMask = (uint)targetMask;
        }

        public bool _IsPositiveEffect(uint effIndex, bool deep)
        {
            // not found a single positive spell with this attribute
            if (HasAttribute(SpellAttr0.Negative1))
                return false;

            switch (SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    switch (Id)
                    {
                        case 29214: // Wrath of the Plaguebringer
                        case 34700: // Allergic Reaction
                        case 54836: // Wrath of the Plaguebringer
                            return false;
                        case 30877: // Tag Murloc
                        case 61716: // Rabbit Costume
                        case 61734: // Noblegarden Bunny
                        case 62344: // Fists of Stone
                        case 61819: // Manabonked! (item)
                        case 61834: // Manabonked! (minigob)
                        case 73523: // Rigor Mortis
                            return true;
                    }
                    break;
                case SpellFamilyNames.Mage:
                    // Arcane Missiles
                    if (SpellFamilyFlags[0] == 0x00000800)
                        return false;
                    break;
                case SpellFamilyNames.Priest:
                    switch (Id)
                    {
                        case 64844: // Divine Hymn
                        case 64904: // Hymn of Hope
                        case 47585: // Dispersion
                            return true;
                    }
                    break;
                case SpellFamilyNames.Rogue:
                    switch (Id)
                    {
                        // Envenom must be considered as a positive effect even though it deals damage
                        case 32645:     // Envenom
                            return true;
                    }
                    break;
            }

            if (Mechanic == Mechanics.ImmuneShield)
                return true;

            // Special case: effects which determine positivity of whole spell
            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect != null && effect.IsAura() && effect.ApplyAuraName == AuraType.ModStealth)
                        return true;
                }
            }

            foreach (var pair in _effects)
            {
                foreach (SpellEffectInfo effect in pair.Value)
                {
                    if (effect == null || effect.EffectIndex != effIndex)
                        continue;

                    switch (effect.Effect)
                    {
                        case SpellEffectName.Dummy:
                            // some explicitly required dummy effect sets
                            switch (Id)
                            {
                                case 28441:
                                    return false; // AB Effect 000
                            }
                            break;
                        // always positive effects (check before target checks that provided non-positive result in some case for positive effects)
                        case SpellEffectName.Heal:
                        case SpellEffectName.LearnSpell:
                        case SpellEffectName.SkillStep:
                        case SpellEffectName.HealPct:
                        case SpellEffectName.EnergizePct:
                            return true;
                        case SpellEffectName.ApplyAreaAuraEnemy:
                            return false;

                        // non-positive aura use
                        case SpellEffectName.ApplyAura:
                        case SpellEffectName.ApplyAreaAuraFriend:
                            {
                                switch (effect.ApplyAuraName)
                                {
                                    case AuraType.ModDamageDone:            // dependent from bas point sign (negative . negative)
                                    case AuraType.ModStat:
                                    case AuraType.ModSkill:
                                    case AuraType.ModSkill2:
                                    case AuraType.ModDodgePercent:
                                    case AuraType.ModHealingPct:
                                    case AuraType.ModHealingDone:
                                    case AuraType.ModHealingDonePercent:
                                        if (effect.CalcValue() < 0)
                                            return false;
                                        break;
                                    case AuraType.ModDamageTaken:           // dependent from bas point sign (positive . negative)
                                        if (effect.CalcValue() > 0)
                                            return false;
                                        break;
                                    case AuraType.ModCritPct:
                                    case AuraType.ModSpellCritChance:
                                        if (effect.CalcValue() > 0)
                                            return true;        // some expected positive spells have SPELL_ATTR1_NEGATIVE
                                        break;
                                    case AuraType.AddTargetTrigger:
                                        return true;
                                    case AuraType.PeriodicTriggerSpellWithValue:
                                    case AuraType.PeriodicTriggerSpell:
                                        if (!deep)
                                        {
                                            var spellTriggeredProto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell);
                                            if (spellTriggeredProto != null)
                                            {
                                                // negative targets of main spell return early
                                                foreach (var pair2 in spellTriggeredProto._effects)
                                                {
                                                    foreach (SpellEffectInfo eff in pair2.Value)
                                                    {
                                                        if (eff == null || eff.Effect == 0)
                                                            continue;
                                                        // if non-positive trigger cast targeted to positive target this main cast is non-positive
                                                        // this will place this spell auras as debuffs
                                                        if (_IsPositiveTarget(eff.TargetA.getTarget(), eff.TargetB.getTarget()) && !spellTriggeredProto._IsPositiveEffect(eff.EffectIndex, true))
                                                            return false;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case AuraType.ProcTriggerSpell:
                                        // many positive auras have negative triggered spells at damage for example and this not make it negative (it can be canceled for example)
                                        break;
                                    case AuraType.ModStun:   //have positive and negative spells, we can't sort its correctly at this moment.
                                        bool more = false;
                                        foreach (var pair2 in _effects)
                                        {
                                            foreach (SpellEffectInfo eff in pair2.Value)
                                            {
                                                if (eff != null && eff.EffectIndex != 0)
                                                {
                                                    more = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (effIndex == 0 && !more)
                                            return false;       // but all single stun aura spells is negative
                                        break;
                                    case AuraType.ModPacifySilence:
                                        if (Id == 24740)             // Wisp Costume
                                            return true;
                                        return false;
                                    case AuraType.ModRoot:
                                    case AuraType.ModRoot2:
                                    case AuraType.ModSilence:
                                    case AuraType.Ghost:
                                    case AuraType.PeriodicLeech:
                                    case AuraType.ModStalked:
                                    case AuraType.PeriodicDamagePercent:
                                    case AuraType.PreventResurrection:
                                        return false;
                                    case AuraType.PeriodicDamage:            // used in positive spells also.
                                                                             // part of negative spell if casted at self (prevent cancel)
                                        if (effect.TargetA.GetTarget() == Targets.UnitCaster)
                                            return false;
                                        break;
                                    case AuraType.ModDecreaseSpeed:         // used in positive spells also
                                                                            // part of positive spell if casted at self
                                        if (effect.TargetA.GetTarget() != Targets.UnitCaster)
                                            return false;
                                        // but not this if this first effect (didn't find better check)
                                        if (HasAttribute(SpellAttr0.Negative1) && effIndex == 0)
                                            return false;
                                        break;
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
                                            }
                                            break;
                                        }
                                    case AuraType.AddFlatModifier:          // mods
                                    case AuraType.AddPctModifier:
                                        {
                                            // non-positive mods
                                            switch ((SpellModOp)effect.MiscValue)
                                            {
                                                case SpellModOp.Cost: // dependent from bas point sign (negative . positive)
                                                    if (effect.CalcValue() > 0)
                                                    {
                                                        if (!deep)
                                                        {
                                                            bool negative = true;
                                                            for (uint i = 0; i < SpellConst.MaxEffects; ++i)
                                                            {
                                                                if (i != effIndex)
                                                                {
                                                                    if (_IsPositiveEffect(i, true))
                                                                    {
                                                                        negative = false;
                                                                        break;
                                                                    }
                                                                }
                                                            }
                                                            if (negative)
                                                                return false;
                                                        }
                                                    }
                                                    break;
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                    }



                    // non-positive targets
                    if (!_IsPositiveTarget(effect.TargetA.getTarget(), effect.TargetB.getTarget()))
                        return false;

                    // negative spell if triggered spell is negative
                    if (!deep && effect.ApplyAuraName == 0 && effect.TriggerSpell != 0)
                    {
                        SpellInfo spellTriggeredProto = Global.SpellMgr.GetSpellInfo(effect.TriggerSpell);
                        if (spellTriggeredProto != null)
                            if (!spellTriggeredProto._IsPositiveSpell())
                                return false;
                    }
                }
            }
            // ok, positive
            return true;
        }

        public bool _IsPositiveSpell()
        {
            // spells with at least one negative effect are considered negative
            // some self-applied spells have negative effects but in self casting case negative check ignored.
            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                if (!_IsPositiveEffect(i, true))
                    return false;
            return true;
        }

        bool _IsPositiveTarget(uint targetA, uint targetB)
        {
            // non-positive targets
            switch ((Targets)targetA)
            {
                case Targets.UnitNearbyEnemy:
                case Targets.UnitEnemy:
                case Targets.UnitSrcAreaEnemy:
                case Targets.UnitDestAreaEnemy:
                case Targets.UnitConeEnemy24:
                case Targets.UnitConeEnemy104:
                case Targets.DestDynobjEnemy:
                case Targets.DestEnemy:
                    return false;
                default:
                    break;
            }
            if (targetB != 0)
                return _IsPositiveTarget(targetB, 0);
            return true;
        }

        public void _UnloadImplicitTargetConditionLists()
        {
            // find the same instances of ConditionList and delete them.
            foreach (var pair in _effects)
            {
                for (uint i = 0; i < pair.Value.Length; ++i)
                {
                    SpellEffectInfo effect = pair.Value[i];
                    if (effect == null)
                        continue;

                    var cur = effect.ImplicitTargetConditions;
                    if (cur == null)
                        continue;

                    for (var j = i; j < pair.Value.Length; ++j)
                    {
                        SpellEffectInfo eff = pair.Value[j];
                        if (eff != null && eff.ImplicitTargetConditions == cur)
                            eff.ImplicitTargetConditions = null;
                    }
                }
            }
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

        public SpellEffectInfo[] GetEffectsForDifficulty(Difficulty difficulty)
        {
            SpellEffectInfo[] effList = new SpellEffectInfo[SpellConst.MaxEffects];

            // downscale difficulty if original was not found
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            while (difficultyEntry != null)
            {
                var effectArray = _effects.LookupByKey(difficulty);
                if (effectArray != null)
                {
                    foreach (SpellEffectInfo effect in effectArray)
                    {
                        if (effect == null)
                            continue;

                        if (effList[effect.EffectIndex] == null)
                            effList[effect.EffectIndex] = effect;
                    }
                }

                difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
            }

            // DIFFICULTY_NONE effects are the default effects, always active if current difficulty's effects don't overwrite
            var effects = _effects.LookupByKey(Difficulty.None);
            if (effects != null)
            {
                foreach (SpellEffectInfo effect in effects)
                {
                    if (effect == null)
                        continue;

                    if (effList[effect.EffectIndex] == null)
                        effList[effect.EffectIndex] = effect;
                }
            }

            return effList;
        }

        public SpellEffectInfo GetEffect(uint index) { return GetEffect(Difficulty.None, index); }
        public SpellEffectInfo GetEffect(Difficulty difficulty, uint index)
        {
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            while (difficultyEntry != null)
            {
                var effectArray = _effects.LookupByKey(difficulty);
                if (effectArray != null)
                    if (effectArray.Length > index && effectArray[index] != null)
                        return effectArray[index];

                difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficultyEntry.FallbackDifficultyID);
            }

            var spellEffectInfos = _effects.LookupByKey(Difficulty.None);
            if (spellEffectInfos != null)
                if (spellEffectInfos.Length > index)
                    return spellEffectInfos[index];

            return null;
        }

        public bool HasAttribute(SpellAttr0 attribute) { return Convert.ToBoolean(Attributes & attribute); }
        public bool HasAttribute(SpellAttr1 attribute) { return Convert.ToBoolean(AttributesEx & attribute); }
        public bool HasAttribute(SpellAttr2 attribute) { return Convert.ToBoolean(AttributesEx2 & attribute); }
        public bool HasAttribute(SpellAttr3 attribute) { return Convert.ToBoolean(AttributesEx3 & attribute); }
        public bool HasAttribute(SpellAttr4 attribute) { return Convert.ToBoolean(AttributesEx4 & attribute); }
        public bool HasAttribute(SpellAttr5 attribute) { return Convert.ToBoolean(AttributesEx5 & attribute); }
        public bool HasAttribute(SpellAttr6 attribute) { return Convert.ToBoolean(AttributesEx6 & attribute); }
        public bool HasAttribute(SpellAttr7 attribute) { return Convert.ToBoolean(AttributesEx7 & attribute); }
        public bool HasAttribute(SpellAttr8 attribute) { return Convert.ToBoolean(AttributesEx8 & attribute); }
        public bool HasAttribute(SpellAttr9 attribute) { return Convert.ToBoolean(AttributesEx9 & attribute); }
        public bool HasAttribute(SpellAttr10 attribute) { return Convert.ToBoolean(AttributesEx10 & attribute); }
        public bool HasAttribute(SpellAttr11 attribute) { return Convert.ToBoolean(AttributesEx11 & attribute); }
        public bool HasAttribute(SpellAttr12 attribute) { return Convert.ToBoolean(AttributesEx12 & attribute); }
        public bool HasAttribute(SpellAttr13 attribute) { return Convert.ToBoolean(AttributesEx13 & attribute); }
        public bool HasAttribute(SpellCustomAttributes attribute) { return Convert.ToBoolean(AttributesCu & attribute); }

        public bool HasAnyAuraInterruptFlag() { return AuraInterruptFlags.Any(flag => flag != 0); }
        public bool HasAuraInterruptFlag(SpellAuraInterruptFlags flag) { return (AuraInterruptFlags[0] & (uint)flag) != 0; }
        public bool HasAuraInterruptFlag(SpellAuraInterruptFlags2 flag) { return (AuraInterruptFlags[1] & (uint)flag) != 0; }

        public bool HasChannelInterruptFlag(SpellChannelInterruptFlags flag) { return (ChannelInterruptFlags[0] & (uint)flag) != 0; }


        #region Fields
        public uint Id;
        uint CategoryId;
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
        public SpellCustomAttributes AttributesCu { get; set; }
        public ulong Stances { get; set; }
        public ulong StancesNot { get; set; }
        public SpellCastTargetFlags targets { get; set; }
        public uint TargetCreatureType { get; set; }
        public uint RequiresSpellFocus { get; set; }
        public uint FacingCasterFlags { get; set; }
        public AuraStateType CasterAuraState { get; set; }
        public AuraStateType TargetAuraState { get; set; }
        public AuraStateType CasterAuraStateNot { get; set; }
        public AuraStateType TargetAuraStateNot { get; set; }
        public uint CasterAuraSpell { get; set; }
        public uint TargetAuraSpell { get; set; }
        public uint ExcludeCasterAuraSpell { get; set; }
        public uint ExcludeTargetAuraSpell { get; set; }
        public SpellCastTimesRecord CastTimeEntry { get; set; }
        public uint RecoveryTime { get; set; }
        public uint CategoryRecoveryTime { get; set; }
        public uint StartRecoveryCategory { get; set; }
        public uint StartRecoveryTime { get; set; }
        public SpellInterruptFlags InterruptFlags { get; set; }
        public uint[] AuraInterruptFlags { get; set; } = new uint[2];
        public uint[] ChannelInterruptFlags { get; set; } = new uint[2];
        public ProcFlags ProcFlags { get; set; }
        public uint ProcChance { get; set; }
        public uint ProcCharges { get; set; }
        public uint ProcCooldown { get; set; }
        public float ProcBasePPM { get; set; }
        List<SpellProcsPerMinuteModRecord> ProcPPMMods = new List<SpellProcsPerMinuteModRecord>();
        public uint MaxLevel { get; set; }
        public uint BaseLevel { get; set; }
        public uint SpellLevel { get; set; }
        public SpellDurationRecord DurationEntry { get; set; }
        public List<SpellPowerRecord> PowerCosts = new List<SpellPowerRecord>();
        public uint RangeIndex { get; set; }
        public SpellRangeRecord RangeEntry { get; set; }
        public float Speed { get; set; }
        public uint StackAmount { get; set; }
        public uint[] Totem = new uint[SpellConst.MaxTotems];
        public int[] Reagent = new int[SpellConst.MaxReagents];
        public uint[] ReagentCount = new uint[SpellConst.MaxReagents];
        public ItemClass EquippedItemClass { get; set; }
        public int EquippedItemSubClassMask { get; set; }
        public int EquippedItemInventoryTypeMask { get; set; }
        public uint[] TotemCategory = new uint[SpellConst.MaxTotems];
        public uint IconFileDataId { get; set; }
        public uint ActiveIconFileDataId { get; set; }
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
        // SpellScalingEntry
        public ScalingInfo Scaling;
        public uint ExplicitTargetMask { get; set; }
        public SpellChainNode ChainEntry { get; set; }

        internal Dictionary<uint, SpellEffectInfo[]> _effects;
        MultiMap<uint, SpellXSpellVisualRecord> _visuals = new MultiMap<uint, SpellXSpellVisualRecord>();
        bool _hasPowerDifficultyData;
        SpellSpecificType _spellSpecific;
        AuraStateType _auraState;

        SpellDiminishInfo _diminishInfo;
        #endregion

        public struct ScalingInfo
        {
            public int _Class { get; set; }
            public uint MinScalingLevel;
            public uint MaxScalingLevel;
            public uint ScalesFromItemLevel;
        }
    }

    public class SpellEffectInfo
    {
        public SpellEffectInfo(SpellInfo spellInfo, uint effIndex, SpellEffectRecord _effect)
        {
            _spellInfo = spellInfo;
            EffectIndex = effIndex;

            TargetA = new SpellImplicitTargetInfo();
            TargetB = new SpellImplicitTargetInfo();
            SpellClassMask = new FlagArray128();

            if (_effect != null)
            {
                EffectIndex = _effect.EffectIndex;
                Effect = (SpellEffectName)_effect.Effect;
                ApplyAuraName = (AuraType)_effect.EffectAura;
                ApplyAuraPeriod = _effect.EffectAuraPeriod;
                RealPointsPerLevel = _effect.EffectRealPointsPerLevel;
                BasePoints = (int)_effect.EffectBasePoints;
                PointsPerResource = _effect.EffectPointsPerResource;
                Amplitude = _effect.EffectAmplitude;
                ChainAmplitude = _effect.EffectChainAmplitude;
                BonusCoefficient = _effect.EffectBonusCoefficient;
                MiscValue = _effect.EffectMiscValue[0];
                MiscValueB = _effect.EffectMiscValue[1];
                Mechanic = (Mechanics)_effect.EffectMechanic;
                PositionFacing = _effect.EffectPosFacing;
                TargetA = new SpellImplicitTargetInfo((Targets)_effect.ImplicitTarget[0]);
                TargetB = new SpellImplicitTargetInfo((Targets)_effect.ImplicitTarget[1]);
                RadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(_effect.EffectRadiusIndex[0]);
                MaxRadiusEntry = CliDB.SpellRadiusStorage.LookupByKey(_effect.EffectRadiusIndex[1]);
                ChainTargets = _effect.EffectChainTargets;
                ItemType = _effect.EffectItemType;
                TriggerSpell = _effect.EffectTriggerSpell;
                SpellClassMask = _effect.EffectSpellClassMask;
                BonusCoefficientFromAP = _effect.BonusCoefficientFromAP;
                Scaling.Coefficient = _effect.Coefficient;
                Scaling.Variance = _effect.Variance;
                Scaling.ResourceCoefficient = _effect.ResourceCoefficient;
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
                Effect == SpellEffectName.ApplyAreaAuraOwner)
                return true;
            return false;
        }

        public bool IsFarUnitTargetEffect()
        {
            return (Effect == SpellEffectName.SummonPlayer)
                || (Effect == SpellEffectName.SummonRafFriend)
                || (Effect == SpellEffectName.Resurrect)
                || (Effect == SpellEffectName.SkinPlayerCorpse);
        }

        bool IsFarDestTargetEffect()
        {
            return Effect == SpellEffectName.TeleportUnitsOld;
        }

        public bool IsUnitOwnedAuraEffect()
        {
            return IsAreaAuraEffect() || Effect == SpellEffectName.ApplyAura;
        }

        public int CalcValue(Unit caster = null, int? bp = null, Unit target = null, int itemLevel = -1)
        {
            float throwAway;
            return CalcValue(out throwAway, caster, bp, target, itemLevel);
        }

        public int CalcValue(out float variance, Unit caster = null, int? bp = null, Unit target = null, int itemLevel = -1)
        {
            variance = 0.0f;
            float basePointsPerLevel = RealPointsPerLevel;
            // TODO: this needs to be a float, not rounded
            int basePoints = CalcBaseValue(caster, target, itemLevel);
            float value = bp.HasValue ? bp.Value : BasePoints;
            float comboDamage = PointsPerResource;

            if (Scaling.Variance != 0)
            {
                float delta = Math.Abs(Scaling.Variance * 0.5f);
                float valueVariance = RandomHelper.FRand(-delta, delta);
                value += basePoints * valueVariance;
                variance = valueVariance;
            }

            // base amount modification based on spell lvl vs caster lvl
            if (Scaling.Coefficient != 0.0f)
            {
                if (Scaling.ResourceCoefficient != 0)
                    comboDamage = Scaling.ResourceCoefficient * value;
            }
            else
            {
                if (GetScalingExpectedStat() == ExpectedStatType.None)
                {
                    int level = caster ? (int)caster.getLevel() : 0;
                    if (level > (int)_spellInfo.MaxLevel && _spellInfo.MaxLevel > 0)
                        level = (int)_spellInfo.MaxLevel;
                    level -= (int)_spellInfo.BaseLevel;
                    if (level < 0)
                        level = 0;
                    value += level * basePointsPerLevel;
                }
            }
            // random damage
            if (caster)
            {
                // bonus amount from combo points
                if (caster.m_playerMovingMe && comboDamage != 0)
                {
                    uint comboPoints = caster.m_playerMovingMe.GetComboPoints();
                    if (comboPoints != 0)
                        value += comboDamage * comboPoints;
                }

                value = caster.ApplyEffectModifiers(_spellInfo, EffectIndex, value);
            }

            return (int)Math.Round(value);
        }

        public int CalcBaseValue(Unit caster, Unit target, int itemLevel)
        {
            if (Scaling.Coefficient != 0.0f)
            {
                uint level = _spellInfo.SpellLevel;
                if (target && _spellInfo.IsPositiveEffect(EffectIndex) && (Effect == SpellEffectName.ApplyAura))
                    level = target.getLevel();
                else if (caster)
                    level = caster.getLevel();

                if (_spellInfo.BaseLevel != 0 && !_spellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel) && _spellInfo.HasAttribute(SpellAttr10.UseSpellBaseLevelForScaling))
                    level = _spellInfo.BaseLevel;

                if (_spellInfo.Scaling.MinScalingLevel != 0 && _spellInfo.Scaling.MinScalingLevel > level)
                    level = _spellInfo.Scaling.MinScalingLevel;

                if (_spellInfo.Scaling.MaxScalingLevel != 0 && _spellInfo.Scaling.MaxScalingLevel < level)
                    level = _spellInfo.Scaling.MaxScalingLevel;

                float tempValue = 0.0f;
                if (level > 0)
                {
                    if (_spellInfo.Scaling._Class == 0)
                        return 0;

                    uint effectiveItemLevel = itemLevel != -1 ? (uint)itemLevel : 1u;
                    if (_spellInfo.Scaling.ScalesFromItemLevel != 0 || _spellInfo.HasAttribute(SpellAttr11.ScalesWithItemLevel))
                    {
                        if (_spellInfo.Scaling.ScalesFromItemLevel != 0)
                            effectiveItemLevel = _spellInfo.Scaling.ScalesFromItemLevel;

                        if (_spellInfo.Scaling._Class == -8)
                        {
                            RandPropPointsRecord randPropPoints = CliDB.RandPropPointsStorage.LookupByKey(effectiveItemLevel);
                            if (randPropPoints == null)
                                randPropPoints = CliDB.RandPropPointsStorage.LookupByKey(CliDB.RandPropPointsStorage.Count - 1);

                            tempValue = randPropPoints.DamageReplaceStat;
                        }
                        else
                            tempValue = ItemEnchantment.GetRandomPropertyPoints(effectiveItemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
                    }
                    else
                        tempValue = CliDB.GetSpellScalingColumnForClass(CliDB.SpellScalingGameTable.GetRow(level), _spellInfo.Scaling._Class);

                    if (_spellInfo.Scaling._Class == -7)
                    {
                        // todo: get inventorytype here
                        GtCombatRatingsMultByILvlRecord ratingMult = CliDB.CombatRatingsMultByILvlGameTable.GetRow(effectiveItemLevel);
                        if (ratingMult != null)
                            tempValue *= ratingMult.ArmorMultiplier;
                    }
                }

                tempValue *= Scaling.Coefficient;
                if (tempValue != 0.0f && tempValue < 1.0f)
                    tempValue = 1.0f;

                return (int)Math.Round(tempValue);
            }
            else
            {
                float tempValue = BasePoints;
                ExpectedStatType stat = GetScalingExpectedStat();
                if (stat != ExpectedStatType.None)
                {
                    if (_spellInfo.HasAttribute(SpellAttr0.LevelDamageCalculation))
                        stat = ExpectedStatType.CreatureAutoAttackDps;

                    // TODO - add expansion and content tuning id args?
                    uint level = caster ? caster.getLevel() : 1;
                    tempValue = Global.DB2Mgr.EvaluateExpectedStat(stat, level, -2, 0, Class.None) * BasePoints / 100.0f;
                }

                return (int)Math.Round(tempValue);
            }
        }

        public float CalcValueMultiplier(Unit caster, Spell spell = null)
        {
            float multiplier = Amplitude;
            Player modOwner = (caster != null ? caster.GetSpellModOwner() : null);
            if (modOwner != null)
                modOwner.ApplySpellMod(_spellInfo.Id, SpellModOp.ValueMultiplier, ref multiplier, spell);
            return multiplier;
        }

        public float CalcDamageMultiplier(Unit caster, Spell spell = null)
        {
            float multiplierPercent = ChainAmplitude * 100.0f;
            Player modOwner = (caster != null ? caster.GetSpellModOwner() : null);
            if (modOwner != null)
                modOwner.ApplySpellMod(_spellInfo.Id, SpellModOp.DamageMultiplier, ref multiplierPercent, spell);
            return multiplierPercent / 100.0f;
        }

        public bool HasRadius()
        {
            return RadiusEntry != null;
        }

        public bool HasMaxRadius()
        {
            return MaxRadiusEntry != null;
        }

        public float CalcRadius(Unit caster = null, Spell spell = null)
        {
            SpellRadiusRecord entry = RadiusEntry;
            if (!HasRadius() && HasMaxRadius())
                entry = MaxRadiusEntry;

            if (entry == null)
                return 0.0f;

            float radius = entry.RadiusMin;

            // Client uses max if min is 0
            if (radius == 0.0f)
                radius = entry.RadiusMax;

            if (caster != null)
            {
                radius += entry.RadiusPerLevel * caster.getLevel();
                radius = Math.Min(radius, entry.RadiusMax);
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(_spellInfo.Id, SpellModOp.Radius, ref radius, spell);
            }

            return radius;
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
                case SpellEffectName.WeaponDamageNoschool:
                case SpellEffectName.WeaponDamage:
                    return ExpectedStatType.CreatureSpellDamage;
                case SpellEffectName.Heal:
                case SpellEffectName.HealMechanical:
                    return ExpectedStatType.PlayerHealth;
                case SpellEffectName.Energize:
                case SpellEffectName.PowerBurn:
                    if (MiscValue == 0)
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
                case SpellEffectName.ApllyAuraOnPet:
                case SpellEffectName.Unk202:
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
                            if (MiscValue == 0)
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
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.UnitAndDest), // 5 SPELL_EFFECT_TELEPORT_UNITS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 6 SPELL_EFFECT_APPLY_AURA
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 7 SPELL_EFFECT_ENVIRONMENTAL_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 8 SPELL_EFFECT_POWER_DRAIN
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 9 SPELL_EFFECT_HEALTH_LEECH
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 10 SPELL_EFFECT_HEAL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 11 SPELL_EFFECT_BIND
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 12 SPELL_EFFECT_PORTAL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 13 SPELL_EFFECT_RITUAL_BASE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 14 SPELL_EFFECT_RITUAL_SPECIALIZE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 15 SPELL_EFFECT_RITUAL_ACTIVATE_PORTAL
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
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 52 SPELL_EFFECT_GUARANTEE_HIT
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
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 70 SPELL_EFFECT_PULL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 71 SPELL_EFFECT_PICKPOCKET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 72 SPELL_EFFECT_ADD_FARSIGHT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 73 SPELL_EFFECT_UNTRAIN_TALENTS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 74 SPELL_EFFECT_APPLY_GLYPH
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 75 SPELL_EFFECT_HEAL_MECHANICAL
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 76 SPELL_EFFECT_SUMMON_OBJECT_WILD
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 77 SPELL_EFFECT_SCRIPT_EFFECT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 78 SPELL_EFFECT_ATTACK
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 79 SPELL_EFFECT_SANCTUARY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 80 SPELL_EFFECT_ADD_COMBO_POINTS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 81 SPELL_EFFECT_CREATE_HOUSE
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
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 105 SPELL_EFFECT_SUMMON_OBJECT_SLOT2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 106 SPELL_EFFECT_CHANGE_RAID_MARKER
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 107 SPELL_EFFECT_SUMMON_OBJECT_SLOT4
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 108 SPELL_EFFECT_DISPEL_MECHANIC
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Dest), // 109 SPELL_EFFECT_SUMMON_DEAD_PET
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Unit), // 110 SPELL_EFFECT_DESTROY_ALL_TOTEMS
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 111 SPELL_EFFECT_DURABILITY_DAMAGE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 112 SPELL_EFFECT_112
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.CorpseAlly), // 113 SPELL_EFFECT_RESURRECT_NEW
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
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 146 SPELL_EFFECT_ACTIVATE_RUNE
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
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 157 SPELL_EFFECT_CREATE_ITEM_2
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 158 SPELL_EFFECT_MILLING
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 159 SPELL_EFFECT_ALLOW_RENAME_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 160 SPELL_EFFECT_160
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 161 SPELL_EFFECT_TALENT_SPEC_COUNT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 162 SPELL_EFFECT_TALENT_SPEC_SELECT
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 163 SPELL_EFFECT_163
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 164 SPELL_EFFECT_REMOVE_AURA
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 165 SPELL_EFFECT_165
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 166 SPELL_EFFECT_GIVE_CURRENCY
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 167 SPELL_EFFECT_167
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 168 SPELL_EFFECT_168
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 169 SPELL_EFFECT_DESTROY_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 170 SPELL_EFFECT_170
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 171 SPELL_EFFECT_171
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 172 SPELL_EFFECT_172
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 173 SPELL_EFFECT_UNLOCK_GUILD_VAULT_TAB
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 174 SPELL_EFFECT_174
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 175 SPELL_EFFECT_175
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 176 SPELL_EFFECT_176
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 177 SPELL_EFFECT_177
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 178 SPELL_EFFECT_178
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 179 SPELL_EFFECT_CREATE_AREATRIGGER
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 180 SPELL_EFFECT_UPDATE_AREATRIGGER
            new StaticData(SpellEffectImplicitTargetTypes.Caster,   SpellTargetObjectTypes.Unit), // 181 SPELL_EFFECT_REMOVE_TALENT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 182 SPELL_EFFECT_182
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 183 SPELL_EFFECT_183
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 184 SPELL_EFFECT_REPUTATION_2
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 185 SPELL_EFFECT_185
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 186 SPELL_EFFECT_186
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 187 SPELL_EFFECT_RANDOMIZE_ARCHAEOLOGY_DIGSITES
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 188 SPELL_EFFECT_188
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 189 SPELL_EFFECT_LOOT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 190 SPELL_EFFECT_190
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 191 SPELL_EFFECT_TELEPORT_TO_DIGSITE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 192 SPELL_EFFECT_192
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 193 SPELL_EFFECT_193
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 194 SPELL_EFFECT_194
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 195 SPELL_EFFECT_195
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 196 SPELL_EFFECT_196
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 197 SPELL_EFFECT_197
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.Dest), // 198 SPELL_EFFECT_PLAY_SCENE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 199 SPELL_EFFECT_199
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 200 SPELL_EFFECT_HEAL_BATTLEPET_PCT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 201 SPELL_EFFECT_ENABLE_BATTLE_PETS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 202 SPELL_EFFECT_202
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 203 SPELL_EFFECT_203
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 204 SPELL_EFFECT_CHANGE_BATTLEPET_QUALITY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 205 SPELL_EFFECT_LAUNCH_QUEST_CHOICE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 206 SPELL_EFFECT_ALTER_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 207 SPELL_EFFECT_LAUNCH_QUEST_TASK
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 208 SPELL_EFFECT_208
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 209 SPELL_EFFECT_209
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 210 SPELL_EFFECT_LEARN_GARRISON_BUILDING
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 211 SPELL_EFFECT_LEARN_GARRISON_SPECIALIZATION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 212 SPELL_EFFECT_212
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 213 SPELL_EFFECT_213
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 214 SPELL_EFFECT_CREATE_GARRISON
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 215 SPELL_EFFECT_UPGRADE_CHARACTER_SPELLS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 216 SPELL_EFFECT_CREATE_SHIPMENT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 217 SPELL_EFFECT_UPGRADE_GARRISON
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 218 SPELL_EFFECT_218
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 219 SPELL_EFFECT_CREATE_CONVERSATION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 220 SPELL_EFFECT_ADD_GARRISON_FOLLOWER
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 221 SPELL_EFFECT_221
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 222 SPELL_EFFECT_CREATE_HEIRLOOM_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 223 SPELL_EFFECT_CHANGE_ITEM_BONUSES
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 224 SPELL_EFFECT_ACTIVATE_GARRISON_BUILDING
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 225 SPELL_EFFECT_GRANT_BATTLEPET_LEVEL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 226 SPELL_EFFECT_226
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 227 SPELL_EFFECT_227
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 228 SPELL_EFFECT_228
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 229 SPELL_EFFECT_SET_FOLLOWER_QUALITY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 230 SPELL_EFFECT_INCREASE_FOLLOWER_ITEM_LEVEL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 231 SPELL_EFFECT_INCREASE_FOLLOWER_EXPERIENCE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 232 SPELL_EFFECT_REMOVE_PHASE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 233 SPELL_EFFECT_RANDOMIZE_FOLLOWER_ABILITIES
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 234 SPELL_EFFECT_234
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 235 SPELL_EFFECT_235
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 236 SPELL_EFFECT_GIVE_EXPERIENCE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 237 SPELL_EFFECT_GIVE_RESTED_EXPERIENCE_BONUS
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 238 SPELL_EFFECT_INCREASE_SKILL
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 239 SPELL_EFFECT_END_GARRISON_BUILDING_CONSTRUCTION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 240 SPELL_EFFECT_240
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 241 SPELL_EFFECT_241
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 242 SPELL_EFFECT_242
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 243 SPELL_EFFECT_APPLY_ENCHANT_ILLUSION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 244 SPELL_EFFECT_LEARN_FOLLOWER_ABILITY
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 245 SPELL_EFFECT_UPGRADE_HEIRLOOM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 246 SPELL_EFFECT_FINISH_GARRISON_MISSION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 247 SPELL_EFFECT_ADD_GARRISON_MISSION
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 248 SPELL_EFFECT_FINISH_SHIPMENT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 249 SPELL_EFFECT_249
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 250 SPELL_EFFECT_TAKE_SCREENSHOT
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 251 SPELL_EFFECT_SET_GARRISON_CACHE_SIZE
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 252 SPELL_EFFECT_252
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 253 SPELL_EFFECT_GIVE_HONOR
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 254 SPELL_EFFECT_254
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Unit), // 255 SPELL_EFFECT_LEARN_TRANSMOG_SET
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 256 SPELL_EFFECT_256
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 257 SPELL_EFFECT_257
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 258 SPELL_EFFECT_MODIFY_KEYSTONE
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 259 SPELL_EFFECT_RESPEC_AZERITE_EMPOWERED_ITEM
            new StaticData(SpellEffectImplicitTargetTypes.None,     SpellTargetObjectTypes.None), // 260 SPELL_EFFECT_SUMMON_STABLED_PET
            new StaticData(SpellEffectImplicitTargetTypes.Explicit, SpellTargetObjectTypes.Item), // 261 SPELL_EFFECT_SCRAP_ITEM
        };

        #region Fields
        SpellInfo _spellInfo;
        public uint EffectIndex;

        public SpellEffectName Effect;
        public AuraType ApplyAuraName;
        public uint ApplyAuraPeriod;
        public float RealPointsPerLevel;
        public int BasePoints;
        public float PointsPerResource;
        public float Amplitude;
        public float ChainAmplitude;
        public float BonusCoefficient;
        public int MiscValue;
        public int MiscValueB;
        public Mechanics Mechanic;
        public float PositionFacing;
        public SpellImplicitTargetInfo TargetA;
        public SpellImplicitTargetInfo TargetB;
        public SpellRadiusRecord RadiusEntry;
        public SpellRadiusRecord MaxRadiusEntry;
        public int ChainTargets;
        public uint ItemType;
        public uint TriggerSpell;
        public FlagArray128 SpellClassMask;
        public float BonusCoefficientFromAP;
        public List<Condition> ImplicitTargetConditions;
        public ScalingInfo Scaling;

        ImmunityInfo _immunityInfo;
        #endregion

        public struct ScalingInfo
        {
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
                    return (float)RandomHelper.NextDouble() * (2 * pi);
                default:
                    return 0.0f;
            }
        }

        public Targets GetTarget()
        {
            return _target;
        }
        public uint getTarget()
        {
            return (uint)_target;
        }

        public SpellCastTargetFlags GetExplicitTargetMask(bool srcSet, bool dstSet)
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
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 0
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 1 TARGET_UNIT_CASTER
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 2 TARGET_UNIT_NEARBY_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 3 TARGET_UNIT_NEARBY_PARTY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 4 TARGET_UNIT_NEARBY_ALLY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 5 TARGET_UNIT_PET
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 6 TARGET_UNIT_TARGET_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 7 TARGET_UNIT_SRC_AREA_ENTRY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 8 TARGET_UNIT_DEST_AREA_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 9 TARGET_DEST_HOME
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 10
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 11 TARGET_UNIT_SRC_AREA_UNK_11
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 12
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 13
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 14
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 15 TARGET_UNIT_SRC_AREA_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 16 TARGET_UNIT_DEST_AREA_ENEMY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 17 TARGET_DEST_DB
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 18 TARGET_DEST_CASTER
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 19
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 20 TARGET_UNIT_CASTER_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 21 TARGET_UNIT_TARGET_ALLY
            new StaticData(SpellTargetObjectTypes.Src,  SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 22 TARGET_SRC_CASTER
            new StaticData(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 23 TARGET_GAMEOBJECT_TARGET
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 24 TARGET_UNIT_CONE_ENEMY_24
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 25 TARGET_UNIT_TARGET_ANY
            new StaticData(SpellTargetObjectTypes.GobjItem, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default, SpellTargetDirectionTypes.None),    // 26 TARGET_GAMEOBJECT_ITEM_TARGET
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 27 TARGET_UNIT_MASTER
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 28 TARGET_DEST_DYNOBJ_ENEMY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 29 TARGET_DEST_DYNOBJ_ALLY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 30 TARGET_UNIT_SRC_AREA_ALLY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 31 TARGET_UNIT_DEST_AREA_ALLY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),  // 32 TARGET_DEST_CASTER_SUMMON
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 33 TARGET_UNIT_SRC_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 34 TARGET_UNIT_DEST_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 35 TARGET_UNIT_TARGET_PARTY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 36 TARGET_DEST_CASTER_UNK_36
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Last,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Party,    SpellTargetDirectionTypes.None),        // 37 TARGET_UNIT_LASTTARGET_AREA_PARTY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 38 TARGET_UNIT_NEARBY_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 39 TARGET_DEST_CASTER_FISHING
            new StaticData(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 40 TARGET_GAMEOBJECT_NEARBY_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontRight), // 41 TARGET_DEST_CASTER_FRONT_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackRight),  // 42 TARGET_DEST_CASTER_BACK_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackLeft),   // 43 TARGET_DEST_CASTER_BACK_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),  // 44 TARGET_DEST_CASTER_FRONT_LEFT
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.None),        // 45 TARGET_UNIT_TARGET_CHAINHEAL_ALLY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.None),        // 46 TARGET_DEST_NEARBY_ENTRY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 47 TARGET_DEST_CASTER_FRONT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Back),        // 48 TARGET_DEST_CASTER_BACK
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Right),       // 49 TARGET_DEST_CASTER_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Left),        // 50 TARGET_DEST_CASTER_LEFT
            new StaticData(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Src,    SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 51 TARGET_GAMEOBJECT_SRC_AREA
            new StaticData(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 52 TARGET_GAMEOBJECT_DEST_AREA
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),        // 53 TARGET_DEST_TARGET_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 54 TARGET_UNIT_CONE_ENEMY_54
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 55 TARGET_DEST_CASTER_FRONT_LEAP
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 56 TARGET_UNIT_CASTER_AREA_RAID
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 57 TARGET_UNIT_TARGET_RAID
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Nearby,  SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 58 TARGET_UNIT_NEARBY_RAID
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Ally,     SpellTargetDirectionTypes.Front),       // 59 TARGET_UNIT_CONE_ALLY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Entry,    SpellTargetDirectionTypes.Front),       // 60 TARGET_UNIT_CONE_ENTRY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.RaidClass, SpellTargetDirectionTypes.None),      // 61 TARGET_UNIT_TARGET_AREA_RAID_CLASS
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 62 TARGET_UNK_62
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 63 TARGET_DEST_TARGET_ANY
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 64 TARGET_DEST_TARGET_FRONT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Back),        // 65 TARGET_DEST_TARGET_BACK
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Right),       // 66 TARGET_DEST_TARGET_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Left),        // 67 TARGET_DEST_TARGET_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontRight), // 68 TARGET_DEST_TARGET_FRONT_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackRight),  // 69 TARGET_DEST_TARGET_BACK_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackLeft),   // 70 TARGET_DEST_TARGET_BACK_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),  // 71 TARGET_DEST_TARGET_FRONT_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 72 TARGET_DEST_CASTER_RANDOM
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 73 TARGET_DEST_CASTER_RADIUS
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 74 TARGET_DEST_TARGET_RANDOM
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 75 TARGET_DEST_TARGET_RADIUS
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 76 TARGET_DEST_CHANNEL_TARGET
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 77 TARGET_UNIT_CHANNEL_TARGET
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 78 TARGET_DEST_DEST_FRONT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Back),        // 79 TARGET_DEST_DEST_BACK
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Right),       // 80 TARGET_DEST_DEST_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Left),        // 81 TARGET_DEST_DEST_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontRight), // 82 TARGET_DEST_DEST_FRONT_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackRight),  // 83 TARGET_DEST_DEST_BACK_RIGHT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.BackLeft),   // 84 TARGET_DEST_DEST_BACK_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.FrontLeft),  // 85 TARGET_DEST_DEST_FRONT_LEFT
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 86 TARGET_DEST_DEST_RANDOM
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 87 TARGET_DEST_DEST
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 88 TARGET_DEST_DYNOBJ_NONE
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 89 TARGET_DEST_TRAJ
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 90 TARGET_UNIT_TARGET_MINIPET
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 91 TARGET_DEST_DEST_RADIUS
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 92 TARGET_UNIT_SUMMONER
            new StaticData(SpellTargetObjectTypes.Corpse,SpellTargetReferenceTypes.Src,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.None),       // 93 TARGET_CORPSE_SRC_AREA_ENEMY
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 94 TARGET_UNIT_VEHICLE
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Passenger,SpellTargetDirectionTypes.None),       // 95 TARGET_UNIT_TARGET_PASSENGER
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 96 TARGET_UNIT_PASSENGER_0
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 97 TARGET_UNIT_PASSENGER_1
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 98 TARGET_UNIT_PASSENGER_2
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 99 TARGET_UNIT_PASSENGER_3
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 100 TARGET_UNIT_PASSENGER_4
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 101 TARGET_UNIT_PASSENGER_5
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 102 TARGET_UNIT_PASSENGER_6
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 103 TARGET_UNIT_PASSENGER_7
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Enemy,    SpellTargetDirectionTypes.Front),       // 104 TARGET_UNIT_CONE_ENEMY_104
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 105 TARGET_UNIT_UNK_105
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Channel, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 106 TARGET_DEST_CHANNEL_CASTER
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.Dest,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 107 TARGET_UNK_DEST_AREA_UNK_107
            new StaticData(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),       // 108 TARGET_GAMEOBJECT_CONE_108
            new StaticData(SpellTargetObjectTypes.Gobj, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Front),        // 109 TARGET_GAMEOBJECT_CONE_109
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Cone,    SpellTargetCheckTypes.Entry  ,  SpellTargetDirectionTypes.Front),       // 110 TARGET_DEST_UNK_110
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 111
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 112
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 113
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 114
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 115
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 116
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 117
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 118
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Raid,     SpellTargetDirectionTypes.None),        // 119
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Area,    SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 120
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Target, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 121
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 122
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 123
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 124
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 125
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 126
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 127
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 128
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 129
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 130
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 131
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 132
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 133
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 134
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 135
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 136
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 137
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 138
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 139
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 140
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 141
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 142
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 143
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 144
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 145
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 146
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 147
            new StaticData(SpellTargetObjectTypes.None, SpellTargetReferenceTypes.None,   SpellTargetSelectionCategories.Nyi,     SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 148
            new StaticData(SpellTargetObjectTypes.Dest, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.Random),      // 149
            new StaticData(SpellTargetObjectTypes.Unit, SpellTargetReferenceTypes.Caster, SpellTargetSelectionCategories.Default, SpellTargetCheckTypes.Default,  SpellTargetDirectionTypes.None),        // 150
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
        public uint MechanicImmuneMask;
        public uint DispelImmune;
        public uint DamageSchoolMask;

        public List<AuraType> AuraTypeImmune = new List<AuraType>();
        public List<SpellEffectName> SpellEffectImmune = new List<SpellEffectName>();
    }
}

