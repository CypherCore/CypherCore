// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IUnit;

namespace Game.Spells.Auras.EffectHandlers
{
    public partial class AuraEffect
    {
        private readonly Aura _auraBase;
        private readonly SpellInfo _spellInfo;
        private readonly SpellEffectInfo _effectInfo;
        private SpellModifier _spellmod;

        public int BaseAmount { get; set; }
        private int _amount;
        private float? _estimatedAmount; // for periodic Damage and healing Auras this will include Damage done bonuses

        // periodic stuff
        private int _periodicTimer;
        private int _period;     // Time between consecutive ticks
        private uint _ticksDone; // ticks counter

        private bool _canBeRecalculated;
        private bool _isPeriodic;

        public AuraEffect(Aura baseAura, SpellEffectInfo spellEfffectInfo, int? baseAmount, Unit caster)
        {
            _auraBase = baseAura;
            _spellInfo = baseAura.GetSpellInfo();
            _effectInfo = spellEfffectInfo;
            BaseAmount = baseAmount.HasValue ? baseAmount.Value : _effectInfo.CalcBaseValue(caster, baseAura.GetAuraType() == AuraObjectType.Unit ? baseAura.GetOwner().ToUnit() : null, baseAura.GetCastItemId(), baseAura.GetCastItemLevel());
            _canBeRecalculated = true;
            _isPeriodic = false;

            CalculatePeriodic(caster, true, false);
            _amount = CalculateAmount(caster);
            CalculateSpellMod();
        }

        public int CalculateAmount(Unit caster)
        {
            // default amount calculation
            int amount = 0;

            if (!_spellInfo.HasAttribute(SpellAttr8.MasteryAffectPoints) ||
                MathFunctions.fuzzyEq(GetSpellEffectInfo().BonusCoefficient, 0.0f))
                amount = GetSpellEffectInfo().CalcValue(caster, BaseAmount, GetBase().GetOwner().ToUnit(), GetBase().GetCastItemId(), GetBase().GetCastItemLevel());
            else if (caster != null &&
                     caster.IsTypeId(TypeId.Player))
                amount = (int)(caster.ToPlayer().ActivePlayerData.Mastery * GetSpellEffectInfo().BonusCoefficient);

            // custom amount calculations go here
            switch (GetAuraType())
            {
                // crowd control Auras
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModStun:
                case AuraType.ModRoot:
                case AuraType.Transform:
                case AuraType.ModRoot2:
                    _canBeRecalculated = false;

                    if (_spellInfo.ProcFlags == null)
                        break;

                    amount = (int)GetBase().GetUnitOwner().CountPctFromMaxHealth(10);

                    break;
                case AuraType.SchoolAbsorb:
                case AuraType.ManaShield:
                    _canBeRecalculated = false;

                    break;
                case AuraType.Mounted:
                    uint mountType = (uint)GetMiscValueB();
                    MountRecord mountEntry = Global.DB2Mgr.GetMount(GetId());

                    if (mountEntry != null)
                        mountType = mountEntry.MountTypeID;

                    var mountCapability = GetBase().GetUnitOwner().GetMountCapability(mountType);

                    if (mountCapability != null)
                        amount = (int)mountCapability.Id;

                    break;
                case AuraType.ShowConfirmationPromptWithDifficulty:
                    if (caster)
                        amount = (int)caster.GetMap().GetDifficultyID();

                    _canBeRecalculated = false;

                    break;
                default:
                    break;
            }

            if (GetSpellInfo().HasAttribute(SpellAttr10.RollingPeriodic))
            {
                var periodicAuras = GetBase().GetUnitOwner().GetAuraEffectsByType(GetAuraType());

                amount = periodicAuras.Aggregate(0,
                                                 (val, aurEff) =>
                                                 {
                                                     if (aurEff.GetCasterGUID() == GetCasterGUID() &&
                                                         aurEff.GetId() == GetId() &&
                                                         aurEff.GetEffIndex() == GetEffIndex() &&
                                                         aurEff.GetTotalTicks() > 0)
                                                         val += aurEff.GetAmount() * (int)aurEff.GetRemainingTicks() / (int)aurEff.GetTotalTicks();

                                                     return val;
                                                 });
            }

            GetBase().CallScriptEffectCalcAmountHandlers(this, ref amount, ref _canBeRecalculated);

            if (!GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack))
                amount *= GetBase().GetStackAmount();

            if (caster && GetBase().GetAuraType() == AuraObjectType.Unit)
            {
                uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack) ? GetBase().GetStackAmount() : 1u;

                switch (GetAuraType())
                {
                    case AuraType.PeriodicDamage:
                    case AuraType.PeriodicLeech:
                        _estimatedAmount = caster.SpellDamageBonusDone(GetBase().GetUnitOwner(), GetSpellInfo(), (uint)amount, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

                        break;
                    case AuraType.PeriodicHeal:
                        _estimatedAmount = caster.SpellHealingBonusDone(GetBase().GetUnitOwner(), GetSpellInfo(), (uint)amount, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses);

                        break;
                    default:
                        break;
                }
            }

            return amount;
        }

        public uint GetTotalTicks()
        {
            uint totalTicks = 0;

            if (_period != 0 &&
                !GetBase().IsPermanent())
            {
                totalTicks = (uint)(GetBase().GetMaxDuration() / _period);

                if (_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                    ++totalTicks;
            }

            return totalTicks;
        }

        public void CalculatePeriodic(Unit caster, bool resetPeriodicTimer = true, bool load = false)
        {
            _period = (int)GetSpellEffectInfo().ApplyAuraPeriod;

            // prepare periodics
            switch (GetAuraType())
            {
                case AuraType.ObsModPower:
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicHeal:
                case AuraType.ObsModHealth:
                case AuraType.PeriodicTriggerSpell:
                case AuraType.PeriodicTriggerSpellFromClient:
                case AuraType.PeriodicEnergize:
                case AuraType.PeriodicLeech:
                case AuraType.PeriodicHealthFunnel:
                case AuraType.PeriodicManaLeech:
                case AuraType.PeriodicDamagePercent:
                case AuraType.PowerBurn:
                case AuraType.PeriodicDummy:
                case AuraType.PeriodicTriggerSpellWithValue:
                    _isPeriodic = true;

                    break;
                default:
                    break;
            }

            GetBase().CallScriptEffectCalcPeriodicHandlers(this, ref _isPeriodic, ref _period);

            if (!_isPeriodic)
                return;

            Player modOwner = caster?.GetSpellModOwner();

            // Apply casting Time mods
            if (_period != 0)
            {
                // Apply periodic Time mod
                modOwner?.ApplySpellMod(GetSpellInfo(), SpellModOp.Period, ref _period);

                if (caster != null)
                {
                    // Haste modifies periodic Time of channeled spells
                    if (_spellInfo.IsChanneled())
                        caster.ModSpellDurationTime(_spellInfo, ref _period);
                    else if (_spellInfo.HasAttribute(SpellAttr5.SpellHasteAffectsPeriodic))
                        _period = (int)(_period * caster.UnitData.ModCastingSpeed);
                }
            }
            else // prevent infinite loop on Update
            {
                _isPeriodic = false;
            }

            if (load) // aura loaded from db
            {
                if (_period != 0 &&
                    !GetBase().IsPermanent())
                {
                    uint elapsedTime = (uint)(GetBase().GetMaxDuration() - GetBase().GetDuration());
                    _ticksDone = elapsedTime / (uint)_period;
                    _periodicTimer = (int)(elapsedTime % _period);
                }

                if (_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                    ++_ticksDone;
            }
            else // aura just created or reapplied
            {
                // reset periodic timer on aura create or reapply
                // we don't reset periodic timers when aura is triggered by proc
                ResetPeriodic(resetPeriodicTimer);
            }
        }

        public void CalculateSpellMod()
        {
            switch (GetAuraType())
            {
                case AuraType.AddFlatModifier:
                case AuraType.AddPctModifier:
                    if (_spellmod == null)
                    {
                        SpellModifierByClassMask spellmod = new(GetBase());
                        spellmod.op = (SpellModOp)GetMiscValue();

                        spellmod.type = GetAuraType() == AuraType.AddPctModifier ? SpellModType.Pct : SpellModType.Flat;
                        spellmod.spellId = GetId();
                        spellmod.mask = GetSpellEffectInfo().SpellClassMask;
                        _spellmod = spellmod;
                    }

                    (_spellmod as SpellModifierByClassMask).value = GetAmount();

                    break;
                case AuraType.AddFlatModifierBySpellLabel:
                    if (_spellmod == null)
                    {
                        SpellFlatModifierByLabel spellmod = new(GetBase());
                        spellmod.op = (SpellModOp)GetMiscValue();

                        spellmod.type = SpellModType.LabelFlat;
                        spellmod.spellId = GetId();
                        spellmod.value.ModIndex = GetMiscValue();
                        spellmod.value.LabelID = GetMiscValueB();
                        _spellmod = spellmod;
                    }

                    (_spellmod as SpellFlatModifierByLabel).value.ModifierValue = GetAmount();

                    break;
                case AuraType.AddPctModifierBySpellLabel:
                    if (_spellmod == null)
                    {
                        SpellPctModifierByLabel spellmod = new(GetBase());
                        spellmod.op = (SpellModOp)GetMiscValue();

                        spellmod.type = SpellModType.LabelPct;
                        spellmod.spellId = GetId();
                        spellmod.value.ModIndex = GetMiscValue();
                        spellmod.value.LabelID = GetMiscValueB();
                        _spellmod = spellmod;
                    }

                    (_spellmod as SpellPctModifierByLabel).value.ModifierValue = 1.0f + MathFunctions.CalculatePct(1.0f, GetAmount());

                    break;
                default:
                    break;
            }

            GetBase().CallScriptEffectCalcSpellModHandlers(this, ref _spellmod);
        }

        public void ChangeAmount(int newAmount, bool mark = true, bool onStackOrReapply = false, AuraEffect triggeredBy = null)
        {
            // Reapply if amount change
            AuraEffectHandleModes handleMask = 0;

            if (newAmount != GetAmount())
                handleMask |= AuraEffectHandleModes.ChangeAmount;

            if (onStackOrReapply)
                handleMask |= AuraEffectHandleModes.Reapply;

            if (handleMask == 0)
                return;

            GetApplicationList(out List<AuraApplication> effectApplications);

            foreach (var aurApp in effectApplications)
            {
                aurApp.GetTarget()._RegisterAuraEffect(this, false);
                HandleEffect(aurApp, handleMask, false, triggeredBy);
            }

            if (Convert.ToBoolean(handleMask & AuraEffectHandleModes.ChangeAmount))
            {
                if (!mark)
                    _amount = newAmount;
                else
                    SetAmount(newAmount);

                CalculateSpellMod();
            }

            foreach (var aurApp in effectApplications)
            {
                if (aurApp.GetRemoveMode() != AuraRemoveMode.None)
                    continue;

                aurApp.GetTarget()._RegisterAuraEffect(this, true);
                HandleEffect(aurApp, handleMask, true, triggeredBy);
            }

            if (GetSpellInfo().HasAttribute(SpellAttr8.AuraSendAmount) ||
                Aura.EffectTypeNeedsSendingAmount(GetAuraType()))
                GetBase().SetNeedClientUpdateForTargets();
        }

        public void HandleEffect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply, AuraEffect triggeredBy = null)
        {
            // check if call is correct, we really don't want using bitmasks here (with 1 exception)
            Cypher.Assert(mode == AuraEffectHandleModes.Real || mode == AuraEffectHandleModes.SendForClient || mode == AuraEffectHandleModes.ChangeAmount || mode == AuraEffectHandleModes.Stat || mode == AuraEffectHandleModes.Skill || mode == AuraEffectHandleModes.Reapply || mode == (AuraEffectHandleModes.ChangeAmount | AuraEffectHandleModes.Reapply));

            // register/unregister effect in lists in case of real AuraEffect apply/remove
            // registration/unregistration is done always before real effect handling (some effect handlers code is depending on this)
            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                aurApp.GetTarget()._RegisterAuraEffect(this, apply);

            // real aura apply/remove, handle modifier
            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                ApplySpellMod(aurApp.GetTarget(), apply, triggeredBy);

            // call scripts helping/replacing effect handlers
            bool prevented;

            if (apply)
                prevented = GetBase().CallScriptEffectApplyHandlers(this, aurApp, mode);
            else
                prevented = GetBase().CallScriptEffectRemoveHandlers(this, aurApp, mode);

            // check if script events have removed the aura already
            if (apply && aurApp.HasRemoveMode())
                return;

            // call default effect handler if it wasn't prevented
            if (!prevented)
                Global.SpellMgr.GetAuraEffectHandler(GetAuraType()).Invoke(this, aurApp, mode, apply);

            // check if the default handler reemoved the aura
            if (apply && aurApp.HasRemoveMode())
                return;

            // call scripts triggering additional events after apply/remove
            if (apply)
                GetBase().CallScriptAfterEffectApplyHandlers(this, aurApp, mode);
            else
                GetBase().CallScriptAfterEffectRemoveHandlers(this, aurApp, mode);
        }

        public void HandleEffect(Unit target, AuraEffectHandleModes mode, bool apply, AuraEffect triggeredBy = null)
        {
            AuraApplication aurApp = GetBase().GetApplicationOfTarget(target.GetGUID());
            Cypher.Assert(aurApp != null);
            HandleEffect(aurApp, mode, apply, triggeredBy);
        }

        public void Update(uint diff, Unit caster)
        {
            if (!_isPeriodic ||
                GetBase().GetDuration() < 0 && !GetBase().IsPassive() && !GetBase().IsPermanent())
                return;

            uint totalTicks = GetTotalTicks();

            _periodicTimer += (int)diff;

            while (_periodicTimer >= _period)
            {
                _periodicTimer -= _period;

                if (!GetBase().IsPermanent() &&
                    _ticksDone + 1 > totalTicks)
                    break;

                ++_ticksDone;

                GetBase().CallScriptEffectUpdatePeriodicHandlers(this);

                GetApplicationList(out List<AuraApplication> effectApplications);

                // tick on targets of effects
                foreach (var appt in effectApplications)
                    PeriodicTick(appt, caster);
            }
        }

        public float GetCritChanceFor(Unit caster, Unit target)
        {
            return target.SpellCritChanceTaken(caster, null, this, GetSpellInfo().GetSchoolMask(), CalcPeriodicCritChance(caster), GetSpellInfo().GetAttackType());
        }

        public bool IsAffectingSpell(SpellInfo spell)
        {
            if (spell == null)
                return false;

            // Check family Name and EffectClassMask
            if (!spell.IsAffected(_spellInfo.SpellFamilyName, GetSpellEffectInfo().SpellClassMask))
                return false;

            return true;
        }

        public bool CheckEffectProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool result = GetBase().CallScriptCheckEffectProcHandlers(this, aurApp, eventInfo);

            if (!result)
                return false;

            SpellInfo spellInfo = eventInfo.GetSpellInfo();

            switch (GetAuraType())
            {
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModStun:
                case AuraType.ModRoot:
                case AuraType.Transform:
                    {
                        DamageInfo damageInfo = eventInfo.GetDamageInfo();

                        if (damageInfo == null ||
                            damageInfo.GetDamage() == 0)
                            return false;

                        // Spell own Damage at apply won't break CC
                        if (spellInfo != null &&
                            spellInfo == GetSpellInfo())
                        {
                            Aura aura = GetBase();

                            // called from spellcast, should not have ticked yet
                            if (aura.GetDuration() == aura.GetMaxDuration())
                                return false;
                        }

                        break;
                    }
                case AuraType.MechanicImmunity:
                case AuraType.ModMechanicResistance:
                    // compare mechanic
                    if (spellInfo == null ||
                        (spellInfo.GetAllEffectsMechanicMask() & 1ul << GetMiscValue()) == 0)
                        return false;

                    break;
                case AuraType.ModCastingSpeedNotStack:
                    // skip melee hits and instant cast spells
                    if (!eventInfo.GetProcSpell() ||
                        eventInfo.GetProcSpell().GetCastTime() == 0)
                        return false;

                    break;
                case AuraType.ModSchoolMaskDamageFromCaster:
                case AuraType.ModSpellDamageFromCaster:
                    // Compare casters
                    if (GetCasterGUID() != eventInfo.GetActor().GetGUID())
                        return false;

                    break;
                case AuraType.ModPowerCostSchool:
                case AuraType.ModPowerCostSchoolPct:
                    {
                        // Skip melee hits and spells with wrong school or zero cost
                        if (spellInfo == null ||
                            !Convert.ToBoolean((int)spellInfo.GetSchoolMask() & GetMiscValue()) // School Check
                            ||
                            !eventInfo.GetProcSpell())
                            return false;

                        // Costs Check
                        var costs = eventInfo.GetProcSpell().GetPowerCost();
                        var m = costs.Find(cost => cost.Amount > 0);

                        if (m == null)
                            return false;

                        break;
                    }
                case AuraType.ReflectSpellsSchool:
                    // Skip melee hits and spells with wrong school
                    if (spellInfo == null ||
                        !Convert.ToBoolean((int)spellInfo.GetSchoolMask() & GetMiscValue()))
                        return false;

                    break;
                case AuraType.ProcTriggerSpell:
                case AuraType.ProcTriggerSpellWithValue:
                    {
                        // Don't proc extra attacks while already processing extra attack spell
                        uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
                        SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());

                        if (triggeredSpellInfo != null)
                            if (triggeredSpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                            {
                                uint lastExtraAttackSpell = eventInfo.GetActor().GetLastExtraAttackSpell();

                                // Patch 1.12.0(?) extra attack abilities can no longer chain proc themselves
                                if (lastExtraAttackSpell == triggerSpellId)
                                    return false;
                            }

                        break;
                    }
                case AuraType.ModSpellCritChance:
                    // skip spells that can't crit
                    if (spellInfo == null ||
                        !spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
                        return false;

                    break;
                default:
                    break;
            }

            return result;
        }

        public void HandleProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool prevented = GetBase().CallScriptEffectProcHandlers(this, aurApp, eventInfo);

            if (prevented)
                return;

            switch (GetAuraType())
            {
                // CC Auras which use their amount to drop
                // Are there any more Auras which need this?
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModStun:
                case AuraType.ModRoot:
                case AuraType.Transform:
                case AuraType.ModRoot2:
                    HandleBreakableCCAuraProc(aurApp, eventInfo);

                    break;
                case AuraType.Dummy:
                case AuraType.ProcTriggerSpell:
                    HandleProcTriggerSpellAuraProc(aurApp, eventInfo);

                    break;
                case AuraType.ProcTriggerSpellWithValue:
                    HandleProcTriggerSpellWithValueAuraProc(aurApp, eventInfo);

                    break;
                case AuraType.ProcTriggerDamage:
                    HandleProcTriggerDamageAuraProc(aurApp, eventInfo);

                    break;
                default:
                    break;
            }

            GetBase().CallScriptAfterEffectProcHandlers(this, aurApp, eventInfo);
        }

        public void HandleShapeshiftBoosts(Unit target, bool apply)
        {
            uint spellId = 0;
            uint spellId2 = 0;
            uint spellId3 = 0;
            uint spellId4 = 0;

            switch ((ShapeShiftForm)GetMiscValue())
            {
                case ShapeShiftForm.CatForm:
                    spellId = 3025;
                    spellId2 = 48629;
                    spellId3 = 106840;
                    spellId4 = 113636;

                    break;
                case ShapeShiftForm.TreeOfLife:
                    spellId = 5420;
                    spellId2 = 81097;

                    break;
                case ShapeShiftForm.TravelForm:
                    spellId = 5419;

                    break;
                case ShapeShiftForm.AquaticForm:
                    spellId = 5421;

                    break;
                case ShapeShiftForm.BearForm:
                    spellId = 1178;
                    spellId2 = 21178;
                    spellId3 = 106829;
                    spellId4 = 106899;

                    break;
                case ShapeShiftForm.FlightForm:
                    spellId = 33948;
                    spellId2 = 34764;

                    break;
                case ShapeShiftForm.FlightFormEpic:
                    spellId = 40122;
                    spellId2 = 40121;

                    break;
                case ShapeShiftForm.SpiritOfRedemption:
                    spellId = 27792;
                    spellId2 = 27795;
                    spellId3 = 62371;

                    break;
                case ShapeShiftForm.Shadowform:
                    if (target.HasAura(107906)) // Glyph of Shadow
                        spellId = 107904;
                    else if (target.HasAura(126745)) // Glyph of Shadowy Friends
                        spellId = 142024;
                    else
                        spellId = 107903;

                    break;
                case ShapeShiftForm.GhostWolf:
                    if (target.HasAura(58135)) // Glyph of Spectral Wolf
                        spellId = 160942;

                    break;
                default:
                    break;
            }

            if (apply)
            {
                if (spellId != 0)
                    target.CastSpell(target, spellId, new CastSpellExtraArgs(this));

                if (spellId2 != 0)
                    target.CastSpell(target, spellId2, new CastSpellExtraArgs(this));

                if (spellId3 != 0)
                    target.CastSpell(target, spellId3, new CastSpellExtraArgs(this));

                if (spellId4 != 0)
                    target.CastSpell(target, spellId4, new CastSpellExtraArgs(this));

                if (target.IsTypeId(TypeId.Player))
                {
                    Player plrTarget = target.ToPlayer();

                    var sp_list = plrTarget.GetSpellMap();

                    foreach (var pair in sp_list)
                    {
                        if (pair.Value.State == PlayerSpellState.Removed ||
                            pair.Value.Disabled)
                            continue;

                        if (pair.Key == spellId ||
                            pair.Key == spellId2 ||
                            pair.Key == spellId3 ||
                            pair.Key == spellId4)
                            continue;

                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);

                        if (spellInfo == null ||
                            !(spellInfo.IsPassive() || spellInfo.HasAttribute(SpellAttr0.DoNotDisplaySpellbookAuraIconCombatLog)))
                            continue;

                        if (Convert.ToBoolean(spellInfo.Stances & 1ul << GetMiscValue() - 1))
                            target.CastSpell(target, pair.Key, new CastSpellExtraArgs(this));
                    }
                }
            }
            else
            {
                if (spellId != 0)
                    target.RemoveOwnedAura(spellId, target.GetGUID());

                if (spellId2 != 0)
                    target.RemoveOwnedAura(spellId2, target.GetGUID());

                if (spellId3 != 0)
                    target.RemoveOwnedAura(spellId3, target.GetGUID());

                if (spellId4 != 0)
                    target.RemoveOwnedAura(spellId4, target.GetGUID());

                var shapeshifts = target.GetAuraEffectsByType(AuraType.ModShapeshift);
                AuraEffect newAura = null;

                // Iterate through all the shapeshift Auras that the Target has, if there is another aura with SPELL_AURA_MOD_SHAPESHIFT, then this aura is being removed due to that one being applied
                foreach (var eff in shapeshifts)
                    if (eff != this)
                    {
                        newAura = eff;

                        break;
                    }

                foreach (var app in target.GetAppliedAuras())
                {
                    if (app.Value == null)
                        continue;

                    // Use the new aura to see on what stance the Target will be
                    ulong newStance = newAura != null ? 1ul << newAura.GetMiscValue() - 1 : 0;

                    // If the stances are not compatible with the spell, remove it
                    if (app.Value.GetBase().IsRemovedOnShapeLost(target) &&
                        !Convert.ToBoolean(app.Value.GetBase().GetSpellInfo().Stances & newStance))
                        target.RemoveAura(app);
                }
            }
        }

        public Unit GetCaster()
        {
            return _auraBase.GetCaster();
        }

        public ObjectGuid GetCasterGUID()
        {
            return _auraBase.GetCasterGUID();
        }

        public Aura GetBase()
        {
            return _auraBase;
        }

        public SpellInfo GetSpellInfo()
        {
            return _spellInfo;
        }

        public uint GetId()
        {
            return _spellInfo.Id;
        }

        public uint GetEffIndex()
        {
            return _effectInfo.EffectIndex;
        }

        public int GetBaseAmount()
        {
            return BaseAmount;
        }

        public int GetPeriod()
        {
            return _period;
        }

        public int GetMiscValueB()
        {
            return GetSpellEffectInfo().MiscValueB;
        }

        public int GetMiscValue()
        {
            return GetSpellEffectInfo().MiscValue;
        }

        public AuraType GetAuraType()
        {
            return GetSpellEffectInfo().ApplyAuraName;
        }

        public int GetAmount()
        {
            return _amount;
        }

        public bool HasAmount()
        {
            return _amount != 0;
        }

        public void SetAmount(int amount)
        {
            _amount = amount;
            _canBeRecalculated = false;
        }

        public float? GetEstimatedAmount()
        {
            return _estimatedAmount;
        }

        public int GetPeriodicTimer()
        {
            return _periodicTimer;
        }

        public void SetPeriodicTimer(int periodicTimer)
        {
            _periodicTimer = periodicTimer;
        }

        public void RecalculateAmount(AuraEffect triggeredBy = null)
        {
            if (!CanBeRecalculated())
                return;

            ChangeAmount(CalculateAmount(GetCaster()), false, false, triggeredBy);
        }

        public void RecalculateAmount(Unit caster, AuraEffect triggeredBy = null)
        {
            if (!CanBeRecalculated())
                return;

            ChangeAmount(CalculateAmount(caster), false, false, triggeredBy);
        }

        public bool CanBeRecalculated()
        {
            return _canBeRecalculated;
        }

        public void SetCanBeRecalculated(bool val)
        {
            _canBeRecalculated = val;
        }

        public void ResetTicks()
        {
            _ticksDone = 0;
        }

        public uint GetTickNumber()
        {
            return _ticksDone;
        }

        public uint GetRemainingTicks()
        {
            return GetTotalTicks() - _ticksDone;
        }

        public bool IsPeriodic()
        {
            return _isPeriodic;
        }

        public void SetPeriodic(bool isPeriodic)
        {
            _isPeriodic = isPeriodic;
        }

        public SpellEffectInfo GetSpellEffectInfo()
        {
            return _effectInfo;
        }

        public bool IsEffect()
        {
            return _effectInfo.Effect != 0;
        }

        public bool IsEffect(SpellEffectName effectName)
        {
            return _effectInfo.Effect == effectName;
        }

        public bool IsAreaAuraEffect()
        {
            return _effectInfo.IsAreaAuraEffect();
        }

        private void GetTargetList(out List<Unit> targetList)
        {
            targetList = new List<Unit>();
            var targetMap = GetBase().GetApplicationMap();

            // remove all targets which were not added to new list - they no longer deserve area aura
            foreach (var app in targetMap.Values)
                if (app.HasEffect(GetEffIndex()))
                    targetList.Add(app.GetTarget());
        }

        private void GetApplicationList(out List<AuraApplication> applicationList)
        {
            applicationList = new List<AuraApplication>();
            var targetMap = GetBase().GetApplicationMap();

            foreach (var app in targetMap.Values)
                if (app.HasEffect(GetEffIndex()))
                    applicationList.Add(app);
        }

        private void ResetPeriodic(bool resetPeriodicTimer = false)
        {
            _ticksDone = 0;

            if (resetPeriodicTimer)
            {
                _periodicTimer = 0;

                // Start periodic on next tick or at aura apply
                if (_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                    _periodicTimer = _period;
            }
        }

        private void ApplySpellMod(Unit target, bool apply, AuraEffect triggeredBy = null)
        {
            if (_spellmod == null ||
                !target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().AddSpellMod(_spellmod, apply);

            // Auras with charges do not mod amount of passive Auras
            if (GetBase().IsUsingCharges())
                return;

            // reapply some passive spells after add/remove related spellmods
            // Warning: it is a dead loop if 2 Auras each other amount-shouldn't happen
            BitSet recalculateEffectMask = new(SpellConst.MaxEffects);

            switch ((SpellModOp)GetMiscValue())
            {
                case SpellModOp.Points:
                    recalculateEffectMask.SetAll(true);

                    break;
                case SpellModOp.PointsIndex0:
                    recalculateEffectMask.Set(0, true);

                    break;
                case SpellModOp.PointsIndex1:
                    recalculateEffectMask.Set(1, true);

                    break;
                case SpellModOp.PointsIndex2:
                    recalculateEffectMask.Set(2, true);

                    break;
                case SpellModOp.PointsIndex3:
                    recalculateEffectMask.Set(3, true);

                    break;
                case SpellModOp.PointsIndex4:
                    recalculateEffectMask.Set(4, true);

                    break;
                default:
                    break;
            }

            if (recalculateEffectMask.Any())
            {
                if (triggeredBy == null)
                    triggeredBy = this;

                ObjectGuid guid = target.GetGUID();
                var auras = target.GetAppliedAuras();

                foreach (var iter in auras)
                {
                    Aura aura = iter.Value.GetBase();

                    // only passive and permament Auras-active Auras should have amount set on spellcast and not be affected
                    // if aura is cast by others, it will not be affected
                    if ((aura.IsPassive() || aura.IsPermanent()) &&
                        aura.GetCasterGUID() == guid &&
                        aura.GetSpellInfo().IsAffectedBySpellMod(_spellmod))
                        for (uint i = 0; i < recalculateEffectMask.Count; ++i)
                            if (recalculateEffectMask[(int)i])
                            {
                                AuraEffect aurEff = aura.GetEffect(i);

                                if (aurEff != null)
                                    if (aurEff != triggeredBy)
                                        aurEff.RecalculateAmount(triggeredBy);
                            }
                }
            }
        }

        private void SendTickImmune(Unit target, Unit caster)
        {
            caster?.SendSpellDamageImmune(target, _spellInfo.Id, true);
        }

        private void PeriodicTick(AuraApplication aurApp, Unit caster)
        {
            bool prevented = GetBase().CallScriptEffectPeriodicHandlers(this, aurApp);

            if (prevented)
                return;

            Unit target = aurApp.GetTarget();

            // Update serverside orientation of tracking channeled Auras on periodic update ticks
            // exclude players because can turn during channeling and shouldn't desync orientation client/server
            if (caster != null &&
                !caster.IsPlayer() &&
                _spellInfo.IsChanneled() &&
                _spellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel) &&
                caster.UnitData.ChannelObjects.Size() != 0)
            {
                ObjectGuid channelGuid = caster.UnitData.ChannelObjects[0];

                if (channelGuid != caster.GetGUID())
                {
                    WorldObject objectTarget = Global.ObjAccessor.GetWorldObject(caster, channelGuid);

                    if (objectTarget != null)
                        caster.SetInFront(objectTarget);
                }
            }

            switch (GetAuraType())
            {
                case AuraType.PeriodicDummy:
                    // handled via scripts
                    break;
                case AuraType.PeriodicTriggerSpell:
                    HandlePeriodicTriggerSpellAuraTick(target, caster);

                    break;
                case AuraType.PeriodicTriggerSpellFromClient:
                    // Don't actually do anything - client will trigger casts of these spells by itself
                    break;
                case AuraType.PeriodicTriggerSpellWithValue:
                    HandlePeriodicTriggerSpellWithValueAuraTick(target, caster);

                    break;
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicWeaponPercentDamage:
                case AuraType.PeriodicDamagePercent:
                    HandlePeriodicDamageAurasTick(target, caster);

                    break;
                case AuraType.PeriodicLeech:
                    HandlePeriodicHealthLeechAuraTick(target, caster);

                    break;
                case AuraType.PeriodicHealthFunnel:
                    HandlePeriodicHealthFunnelAuraTick(target, caster);

                    break;
                case AuraType.PeriodicHeal:
                case AuraType.ObsModHealth:
                    HandlePeriodicHealAurasTick(target, caster);

                    break;
                case AuraType.PeriodicManaLeech:
                    HandlePeriodicManaLeechAuraTick(target, caster);

                    break;
                case AuraType.ObsModPower:
                    HandleObsModPowerAuraTick(target, caster);

                    break;
                case AuraType.PeriodicEnergize:
                    HandlePeriodicEnergizeAuraTick(target, caster);

                    break;
                case AuraType.PowerBurn:
                    HandlePeriodicPowerBurnAuraTick(target, caster);

                    break;
                default:
                    break;
            }
        }

        private bool HasSpellClassMask()
        {
            return GetSpellEffectInfo().SpellClassMask;
        }

    }
}