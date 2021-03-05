﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Spells
{
    public class AuraEffect
    {
        public AuraEffect(Aura baseAura, uint effIndex, int? baseAmount, Unit caster)
        {
            auraBase = baseAura;
            m_spellInfo = baseAura.GetSpellInfo();
            _effectInfo = m_spellInfo.GetEffect(effIndex);
            m_baseAmount = baseAmount.HasValue ? baseAmount.Value : _effectInfo.CalcBaseValue(caster, baseAura.GetAuraType() == AuraObjectType.Unit ? baseAura.GetOwner().ToUnit() : null, baseAura.GetCastItemId(), baseAura.GetCastItemLevel());
            m_donePct = 1.0f;
            m_effIndex = (byte)effIndex;
            m_canBeRecalculated = true;
            m_isPeriodic = false;

            CalculatePeriodic(caster, true, false);
            m_amount = CalculateAmount(caster);
            CalculateSpellMod();
        }

        private void GetTargetList(out List<Unit> targetList)
        {
            targetList = new List<Unit>();
            var targetMap = GetBase().GetApplicationMap();
            // remove all targets which were not added to new list - they no longer deserve area aura
            foreach (var app in targetMap.Values)
            {
                if (app.HasEffect(GetEffIndex()))
                    targetList.Add(app.GetTarget());
            }
        }

        private void GetApplicationList(out List<AuraApplication> applicationList)
        {
            applicationList = new List<AuraApplication>();
            var targetMap = GetBase().GetApplicationMap();
            foreach (var app in targetMap.Values)
            {
                if (app.HasEffect(GetEffIndex()))
                    applicationList.Add(app);
            }
        }

        public int CalculateAmount(Unit caster)
        {
            // default amount calculation
            var amount = 0;

            if (!m_spellInfo.HasAttribute(SpellAttr8.MasterySpecialization) || MathFunctions.fuzzyEq(GetSpellEffectInfo().BonusCoefficient, 0.0f))
                amount = GetSpellEffectInfo().CalcValue(caster, m_baseAmount, GetBase().GetOwner().ToUnit(), GetBase().GetCastItemId(), GetBase().GetCastItemLevel());
            else if (caster != null && caster.IsTypeId(TypeId.Player))
                amount = (int)(caster.ToPlayer().m_activePlayerData.Mastery * GetSpellEffectInfo().BonusCoefficient);

            // custom amount calculations go here
            switch (GetAuraType())
            {
                // crowd control auras
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModStun:
                case AuraType.ModRoot:
                case AuraType.Transform:
                case AuraType.ModRoot2:
                    m_canBeRecalculated = false;
                    if (m_spellInfo.ProcFlags == 0)
                        break;
                    amount = (int)(GetBase().GetUnitOwner().CountPctFromMaxHealth(10));
                    break;
                case AuraType.SchoolAbsorb:
                case AuraType.ManaShield:
                    m_canBeRecalculated = false;
                    break;
                case AuraType.Mounted:
                    var mountType = (uint)GetMiscValueB();
                    var mountEntry = Global.DB2Mgr.GetMount(GetId());
                    if (mountEntry != null)
                        mountType = mountEntry.MountTypeID;

                    var mountCapability = GetBase().GetUnitOwner().GetMountCapability(mountType);
                    if (mountCapability != null)
                    {
                        amount = (int)mountCapability.Id;
                        m_canBeRecalculated = false;
                    }
                    break;
                case AuraType.ShowConfirmationPromptWithDifficulty:
                    if (caster)
                        amount = (int)caster.GetMap().GetDifficultyID();
                    m_canBeRecalculated = false;
                    break;
                default:
                    break;
            }

            GetBase().CallScriptEffectCalcAmountHandlers(this, ref amount, ref m_canBeRecalculated);
            if (!GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.NoScaleWithStack))
                amount *= GetBase().GetStackAmount();
            return amount;
        }
        public void CalculatePeriodic(Unit caster, bool resetPeriodicTimer = true, bool load = false)
        {
            m_period = (int)GetSpellEffectInfo().ApplyAuraPeriod;

            // prepare periodics
            switch (GetAuraType())
            {
                case AuraType.ObsModPower:
                    // 3 spells have no amplitude set
                    if (m_period == 0)
                        m_period = 1 * Time.InMilliseconds;
                    break;
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicHeal:
                case AuraType.ObsModHealth:
                case AuraType.PeriodicTriggerSpell:
                case AuraType.PeriodicEnergize:
                case AuraType.PeriodicLeech:
                case AuraType.PeriodicHealthFunnel:
                case AuraType.PeriodicManaLeech:
                case AuraType.PeriodicDamagePercent:
                case AuraType.PowerBurn:
                case AuraType.PeriodicDummy:
                case AuraType.PeriodicTriggerSpellWithValue:
                    m_isPeriodic = true;
                    break;
                default:
                    break;
            }

            GetBase().CallScriptEffectCalcPeriodicHandlers(this, ref m_isPeriodic, ref m_period);

            if (!m_isPeriodic)
                return;

            var modOwner = caster != null ? caster.GetSpellModOwner() : null;

            // Apply casting time mods
            if (m_period != 0)
            {
                // Apply periodic time mod
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.ActivationTime, ref m_period);

                if (caster != null)
                {
                    // Haste modifies periodic time of channeled spells
                    if (m_spellInfo.IsChanneled())
                        caster.ModSpellDurationTime(m_spellInfo, ref m_period);
                    else if (m_spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                        m_period = (int)(m_period * caster.m_unitData.ModCastingSpeed);
                }
            }

            if (load) // aura loaded from db
            {
                m_tickNumber = (uint)(m_period != 0 ? GetBase().GetDuration() / m_period : 0);
                m_periodicTimer = m_period != 0 ? GetBase().GetDuration() % m_period : 0;
                if (m_spellInfo.HasAttribute(SpellAttr5.StartPeriodicAtApply))
                    ++m_tickNumber;
            }
            else // aura just created or reapplied
            {
                m_tickNumber = 0;

                // reset periodic timer on aura create or reapply
                // we don't reset periodic timers when aura is triggered by proc
                if (resetPeriodicTimer)
                {
                    m_periodicTimer = 0;
                    // Start periodic on next tick or at aura apply
                    if (m_period != 0 && !m_spellInfo.HasAttribute(SpellAttr5.StartPeriodicAtApply))
                        m_periodicTimer += m_period;
                }
            }
        }
        public void CalculateSpellMod()
        {
            switch (GetAuraType())
            {
                case AuraType.AddFlatModifier:
                case AuraType.AddPctModifier:
                    if (m_spellmod == null)
                    {
                        m_spellmod = new SpellModifier(GetBase());
                        m_spellmod.op = (SpellModOp)GetMiscValue();

                        m_spellmod.type = GetAuraType() == AuraType.AddPctModifier ? SpellModType.Pct : SpellModType.Flat;
                        m_spellmod.spellId = GetId();
                        m_spellmod.mask = GetSpellEffectInfo().SpellClassMask;
                    }
                    m_spellmod.value = GetAmount();
                    break;
                default:
                    break;
            }
            GetBase().CallScriptEffectCalcSpellModHandlers(this, ref m_spellmod);
        }
        public void ChangeAmount(int newAmount, bool mark = true, bool onStackOrReapply = false)
        {
            // Reapply if amount change
            AuraEffectHandleModes handleMask = 0;
            if (newAmount != GetAmount())
                handleMask |= AuraEffectHandleModes.ChangeAmount;
            if (onStackOrReapply)
                handleMask |= AuraEffectHandleModes.Reapply;

            if (handleMask == 0)
                return;

            GetApplicationList(out var effectApplications);

            foreach (var aurApp in effectApplications)
            {
                aurApp.GetTarget()._RegisterAuraEffect(this, false);
                HandleEffect(aurApp, handleMask, false);
            }

            if (Convert.ToBoolean(handleMask & AuraEffectHandleModes.ChangeAmount))
            {
                if (!mark)
                    m_amount = newAmount;
                else
                    SetAmount(newAmount);
                CalculateSpellMod();
            }

            foreach (var aurApp in effectApplications)
            {
                if (aurApp.GetRemoveMode() != AuraRemoveMode.None)
                    continue;

                aurApp.GetTarget()._RegisterAuraEffect(this, true);
                HandleEffect(aurApp, handleMask, true);
            }

            if (GetSpellInfo().HasAttribute(SpellAttr8.AuraSendAmount) || Aura.EffectTypeNeedsSendingAmount(GetAuraType()))
                GetBase().SetNeedClientUpdateForTargets();
        }

        public void HandleEffect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            // check if call is correct, we really don't want using bitmasks here (with 1 exception)
            Cypher.Assert(mode == AuraEffectHandleModes.Real || mode == AuraEffectHandleModes.SendForClient
                || mode == AuraEffectHandleModes.ChangeAmount || mode == AuraEffectHandleModes.Stat
                || mode == AuraEffectHandleModes.Skill || mode == AuraEffectHandleModes.Reapply
                || mode == (AuraEffectHandleModes.ChangeAmount | AuraEffectHandleModes.Reapply));

            // register/unregister effect in lists in case of real AuraEffect apply/remove
            // registration/unregistration is done always before real effect handling (some effect handlers code is depending on this)
            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                aurApp.GetTarget()._RegisterAuraEffect(this, apply);

            // real aura apply/remove, handle modifier
            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                ApplySpellMod(aurApp.GetTarget(), apply);

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
        public void HandleEffect(Unit target, AuraEffectHandleModes mode, bool apply)
        {
            var aurApp = GetBase().GetApplicationOfTarget(target.GetGUID());
            Cypher.Assert(aurApp != null);
            HandleEffect(aurApp, mode, apply);
        }

        private void ApplySpellMod(Unit target, bool apply)
        {
            if (m_spellmod == null || !target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().AddSpellMod(m_spellmod, apply);

            // Auras with charges do not mod amount of passive auras
            if (GetBase().IsUsingCharges())
                return;
            // reapply some passive spells after add/remove related spellmods
            // Warning: it is a dead loop if 2 auras each other amount-shouldn't happen
            switch ((SpellModOp)GetMiscValue())
            {
                case SpellModOp.AllEffects:
                case SpellModOp.Effect1:
                case SpellModOp.Effect2:
                case SpellModOp.Effect3:
                case SpellModOp.Effect4:
                case SpellModOp.Effect5:
                    {
                        var guid = target.GetGUID();
                        foreach (var iter in target.GetAppliedAuras())
                        {
                            if (iter.Value == null)
                                continue;
                            var aura = iter.Value.GetBase();
                            // only passive and permament auras-active auras should have amount set on spellcast and not be affected
                            // if aura is casted by others, it will not be affected
                            if ((aura.IsPassive() || aura.IsPermanent()) && aura.GetCasterGUID() == guid && aura.GetSpellInfo().IsAffectedBySpellMod(m_spellmod))
                            {
                                AuraEffect aurEff;
                                if ((SpellModOp)GetMiscValue() == SpellModOp.AllEffects)
                                {
                                    for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                                    {
                                        if ((aurEff = aura.GetEffect(i)) != null)
                                            aurEff.RecalculateAmount();
                                    }
                                }
                                else if ((SpellModOp)GetMiscValue() == SpellModOp.Effect1)
                                {
                                    aurEff = aura.GetEffect(0);
                                    if (aurEff != null)
                                        aurEff.RecalculateAmount();
                                }
                                else if ((SpellModOp)GetMiscValue() == SpellModOp.Effect2)
                                {
                                    aurEff = aura.GetEffect(1);
                                    if (aurEff != null)
                                        aurEff.RecalculateAmount();
                                }
                                else if ((SpellModOp)GetMiscValue() == SpellModOp.Effect3)
                                {
                                    aurEff = aura.GetEffect(2);
                                    if (aurEff != null)
                                        aurEff.RecalculateAmount();
                                }
                                else if ((SpellModOp)GetMiscValue() == SpellModOp.Effect4)
                                {
                                    aurEff = aura.GetEffect(3);
                                    if (aurEff != null)
                                        aurEff.RecalculateAmount();
                                }
                                else if ((SpellModOp)GetMiscValue() == SpellModOp.Effect5)
                                {
                                    aurEff = aura.GetEffect(4);
                                    if (aurEff != null)
                                        aurEff.RecalculateAmount();
                                }
                            }
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void Update(uint diff, Unit caster)
        {
            if (m_isPeriodic && (GetBase().GetDuration() >= 0 || GetBase().IsPassive() || GetBase().IsPermanent()))
            {
                if (m_periodicTimer > diff)
                    m_periodicTimer -= (int)diff;
                else // tick also at m_periodicTimer == 0 to prevent lost last tick in case max m_duration == (max m_periodicTimer)*N
                {
                    ++m_tickNumber;
                    // update before tick (aura can be removed in TriggerSpell or PeriodicTick calls)
                    m_periodicTimer += m_period - (int)diff;
                    UpdatePeriodic(caster);

                    GetApplicationList(out var effectApplications);
                    // tick on targets of effects
                    foreach (var appt in effectApplications)
                        if (appt.HasEffect(GetEffIndex()))
                            PeriodicTick(appt, caster);
                }
            }
        }

        private void UpdatePeriodic(Unit caster)
        {
            switch (GetAuraType())
            {
                case AuraType.PeriodicDummy:
                    switch (GetSpellInfo().SpellFamilyName)
                    {
                        case SpellFamilyNames.Generic:
                            switch (GetId())
                            {
                                // Drink
                                case 430:
                                case 431:
                                case 432:
                                case 1133:
                                case 1135:
                                case 1137:
                                case 10250:
                                case 22734:
                                case 27089:
                                case 34291:
                                case 43182:
                                case 43183:
                                case 46755:
                                case 49472: // Drink Coffee
                                case 57073:
                                case 61830:
                                case 69176:
                                case 72623:
                                case 80166:
                                case 80167:
                                case 87958:
                                case 87959:
                                case 92736:
                                case 92797:
                                case 92800:
                                case 92803:
                                    if (caster == null || !caster.IsTypeId(TypeId.Player))
                                        return;
                                    // Get SPELL_AURA_MOD_POWER_REGEN aura from spell
                                    var aurEff = GetBase().GetEffect(0);
                                    if (aurEff != null)
                                    {
                                        if (aurEff.GetAuraType() != AuraType.ModPowerRegen)
                                        {
                                            m_isPeriodic = false;
                                            Log.outError(LogFilter.Spells, "Aura {0} structure has been changed - first aura is no longer SPELL_AURA_MOD_POWER_REGEN", GetId());
                                        }
                                        else
                                        {
                                            // default case - not in arena
                                            if (!caster.ToPlayer().InArena())
                                            {
                                                aurEff.ChangeAmount(GetAmount());
                                                m_isPeriodic = false;
                                            }
                                            else
                                            {
                                                // **********************************************
                                                // This feature uses only in arenas
                                                // **********************************************
                                                // Here need increase mana regen per tick (6 second rule)
                                                // on 0 tick -   0  (handled in 2 second)
                                                // on 1 tick - 166% (handled in 4 second)
                                                // on 2 tick - 133% (handled in 6 second)

                                                // Apply bonus for 1 - 4 tick
                                                switch (m_tickNumber)
                                                {
                                                    case 1:   // 0%
                                                        aurEff.ChangeAmount(0);
                                                        break;
                                                    case 2:   // 166%
                                                        aurEff.ChangeAmount(GetAmount() * 5 / 3);
                                                        break;
                                                    case 3:   // 133%
                                                        aurEff.ChangeAmount(GetAmount() * 4 / 3);
                                                        break;
                                                    default:  // 100% - normal regen
                                                        aurEff.ChangeAmount(GetAmount());
                                                        // No need to update after 4th tick
                                                        m_isPeriodic = false;
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case 58549: // Tenacity
                                case 59911: // Tenacity (vehicle)
                                    GetBase().RefreshDuration();
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case SpellFamilyNames.Mage:
                            if (GetId() == 55342)// Mirror Image
                                m_isPeriodic = false;
                            break;
                        case SpellFamilyNames.Deathknight:
                            // Chains of Ice
                            if (GetSpellInfo().SpellFamilyFlags[1].HasAnyFlag(0x00004000u))
                            {
                                // Get 0 effect aura
                                var slow = GetBase().GetEffect(0);
                                if (slow != null)
                                {
                                    var newAmount = slow.GetAmount() + GetAmount();
                                    if (newAmount > 0)
                                        newAmount = 0;
                                    slow.ChangeAmount(newAmount);
                                }
                                return;
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            GetBase().CallScriptEffectUpdatePeriodicHandlers(this);
        }

        public bool IsAffectingSpell(SpellInfo spell)
        {
            if (spell == null)
                return false;

            // Check family name and EffectClassMask
            if (!spell.IsAffected(m_spellInfo.SpellFamilyName, GetSpellEffectInfo().SpellClassMask))
                return false;

            return true;
        }

        private void SendTickImmune(Unit target, Unit caster)
        {
            if (caster != null)
                caster.SendSpellDamageImmune(target, m_spellInfo.Id, true);
        }

        private void PeriodicTick(AuraApplication aurApp, Unit caster)
        {
            var prevented = GetBase().CallScriptEffectPeriodicHandlers(this, aurApp);
            if (prevented)
                return;

            var target = aurApp.GetTarget();

            switch (GetAuraType())
            {
                case AuraType.PeriodicDummy:
                    HandlePeriodicDummyAuraTick(target, caster);
                    break;
                case AuraType.PeriodicTriggerSpell:
                    HandlePeriodicTriggerSpellAuraTick(target, caster);
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

        public bool CheckEffectProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            var result = GetBase().CallScriptCheckEffectProcHandlers(this, aurApp, eventInfo);
            if (!result)
                return false;

            var spellInfo = eventInfo.GetSpellInfo();
            switch (GetAuraType())
            {
                case AuraType.ModConfuse:
                case AuraType.ModFear:
                case AuraType.ModStun:
                case AuraType.ModRoot:
                case AuraType.Transform:
                    {
                        var damageInfo = eventInfo.GetDamageInfo();
                        if (damageInfo == null || damageInfo.GetDamage() == 0)
                            return false;

                        // Spell own damage at apply won't break CC
                        if (spellInfo != null && spellInfo == GetSpellInfo())
                        {
                            var aura = GetBase();
                            // called from spellcast, should not have ticked yet
                            if (aura.GetDuration() == aura.GetMaxDuration())
                                return false;
                        }
                        break;
                    }
                case AuraType.MechanicImmunity:
                case AuraType.ModMechanicResistance:
                    // compare mechanic
                    if (spellInfo == null || !Convert.ToBoolean(spellInfo.GetAllEffectsMechanicMask() & (1 << GetMiscValue())))
                        return false;
                    break;
                case AuraType.ModCastingSpeedNotStack:
                    // skip melee hits and instant cast spells
                    if (!eventInfo.GetProcSpell() || eventInfo.GetProcSpell().GetCastTime() == 0)
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
                        if (spellInfo == null || !Convert.ToBoolean((int)spellInfo.GetSchoolMask() & GetMiscValue()) // School Check
                            || !eventInfo.GetProcSpell())
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
                    if (spellInfo == null || !Convert.ToBoolean((int)spellInfo.GetSchoolMask() & GetMiscValue()))
                        return false;
                    break;
                case AuraType.ProcTriggerSpell:
                case AuraType.ProcTriggerSpellWithValue:
                    {
                        // Don't proc extra attacks while already processing extra attack spell
                        var triggerSpellId = GetSpellEffectInfo().TriggerSpell;
                        var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
                        if (triggeredSpellInfo != null)
                            if (aurApp.GetTarget().ExtraAttacks != 0 && triggeredSpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                                return false;
                        break;
                    }
                default:
                    break;
            }

            return result;
        }

        public void HandleProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            var prevented = GetBase().CallScriptEffectProcHandlers(this, aurApp, eventInfo);
            if (prevented)
                return;

            switch (GetAuraType())
            {
                // CC Auras which use their amount to drop
                // Are there any more auras which need this?
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
                    target.CastSpell(target, spellId, true, null, this);

                if (spellId2 != 0)
                    target.CastSpell(target, spellId2, true, null, this);

                if (spellId3 != 0)
                    target.CastSpell(target, spellId3, true, null, this);

                if (spellId4 != 0)
                    target.CastSpell(target, spellId4, true, null, this);

                if (target.IsTypeId(TypeId.Player))
                {
                    var plrTarget = target.ToPlayer();

                    var sp_list = plrTarget.GetSpellMap();
                    foreach (var pair in sp_list)
                    {
                        if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
                            continue;

                        if (pair.Key == spellId || pair.Key == spellId2 || pair.Key == spellId3 || pair.Key == spellId4)
                            continue;

                        var spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);
                        if (spellInfo == null || !(spellInfo.IsPassive() || spellInfo.HasAttribute(SpellAttr0.HiddenClientside)))
                            continue;

                        // always valid?
                        if (spellInfo.HasAttribute(SpellAttr8.MasterySpecialization) && !plrTarget.IsCurrentSpecMasterySpell(spellInfo))
                            continue;

                        if (Convert.ToBoolean(spellInfo.Stances & (1ul << (GetMiscValue() - 1))))
                            target.CastSpell(target, pair.Key, true, null, this);
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
                // Iterate through all the shapeshift auras that the target has, if there is another aura with SPELL_AURA_MOD_SHAPESHIFT, then this aura is being removed due to that one being applied
                foreach (var eff in shapeshifts)
                {
                    if (eff != this)
                    {
                        newAura = eff;
                        break;
                    }
                }

                foreach (var app in target.GetAppliedAuras())
                {
                    if (app.Value == null)
                        continue;

                    // Use the new aura to see on what stance the target will be
                    var newStance = newAura != null ? (1ul << (newAura.GetMiscValue() - 1)) : 0;

                    // If the stances are not compatible with the spell, remove it
                    if (app.Value.GetBase().IsRemovedOnShapeLost(target) && !Convert.ToBoolean(app.Value.GetBase().GetSpellInfo().Stances & newStance))
                        target.RemoveAura(app);
                }
            }
        }

        public Unit GetCaster() { return auraBase.GetCaster(); }
        public ObjectGuid GetCasterGUID() { return auraBase.GetCasterGUID(); }
        public Aura GetBase() { return auraBase; }

        public SpellInfo GetSpellInfo() { return m_spellInfo; }
        public uint GetId() { return m_spellInfo.Id; }
        public byte GetEffIndex() { return m_effIndex; }
        public int GetBaseAmount() { return m_baseAmount; }
        public int GetPeriod() { return m_period; }

        public int GetMiscValueB() { return GetSpellEffectInfo().MiscValueB; }
        public int GetMiscValue() { return GetSpellEffectInfo().MiscValue; }
        public AuraType GetAuraType() { return GetSpellEffectInfo().ApplyAuraName; }
        public int GetAmount() { return m_amount; }
        public bool HasAmount() { return m_amount != 0; }
        public void SetAmount(int _amount) { m_amount = _amount; m_canBeRecalculated = false; }

        public int GetPeriodicTimer() { return m_periodicTimer; }
        public void SetPeriodicTimer(int periodicTimer) { m_periodicTimer = periodicTimer; }

        private void RecalculateAmount()
        {
            if (!CanBeRecalculated())
                return;
            ChangeAmount(CalculateAmount(GetCaster()), false);
        }
        public void RecalculateAmount(Unit caster)
        {
            if (!CanBeRecalculated())
                return;
            ChangeAmount(CalculateAmount(caster), false);
        }

        public bool CanBeRecalculated() { return m_canBeRecalculated; }
        public void SetCanBeRecalculated(bool val) { m_canBeRecalculated = val; }

        public void SetDamage(int val) { m_damage = val; }
        public int GetDamage() { return m_damage; }
        public void SetCritChance(float val) { m_critChance = val; }
        public float GetCritChance() { return m_critChance; }
        public void SetDonePct(float val) { m_donePct = val; }
        public float GetDonePct() { return m_donePct; }

        public uint GetTickNumber() { return m_tickNumber; }
        public int GetTotalTicks()
        {
            return m_period != 0 ? (GetBase().GetMaxDuration() / m_period) : 1;
        }
        public void ResetPeriodic(bool resetPeriodicTimer = false)
        {
            if (resetPeriodicTimer)
                m_periodicTimer = m_period;
            m_tickNumber = 0;
        }

        public bool IsPeriodic() { return m_isPeriodic; }
        private void SetPeriodic(bool isPeriodic) { m_isPeriodic = isPeriodic; }
        private bool HasSpellClassMask() { return GetSpellEffectInfo().SpellClassMask; }

        public SpellEffectInfo GetSpellEffectInfo() { return _effectInfo; }

        public bool IsEffect() { return _effectInfo.Effect != 0; }
        public bool IsEffect(SpellEffectName effectName) { return _effectInfo.Effect == effectName; }
        public bool IsAreaAuraEffect()
        {
            return _effectInfo.IsAreaAuraEffect();
        }

        #region Fields

        private Aura auraBase;
        private SpellInfo m_spellInfo;
        private SpellEffectInfo _effectInfo;
        public int m_baseAmount;

        private int m_amount;
        private int m_damage;
        private float m_critChance;
        private float m_donePct;

        private SpellModifier m_spellmod;

        private int m_periodicTimer;
        private int m_period;
        private uint m_tickNumber;

        private byte m_effIndex;
        private bool m_canBeRecalculated;
        private bool m_isPeriodic;
        #endregion

        #region AuraEffect Handlers
        /**************************************/
        /***       VISIBILITY & PHASES      ***/
        /**************************************/
        [AuraEffectHandler(AuraType.None)]
        private void HandleUnused(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
        }
        
        [AuraEffectHandler(AuraType.ModInvisibilityDetect)]
        private void HandleModInvisibilityDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();
            var type = (InvisibilityType)GetMiscValue();

            if (apply)
            {
                target.m_invisibilityDetect.AddFlag(type);
                target.m_invisibilityDetect.AddValue(type, GetAmount());
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModInvisibilityDetect))
                    target.m_invisibilityDetect.DelFlag(type);

                target.m_invisibilityDetect.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModInvisibility)]
        private void HandleModInvisibility(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            var target = aurApp.GetTarget();
            var type = (InvisibilityType)GetMiscValue();

            if (apply)
            {
                // apply glow vision
                var playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    playerTarget.AddAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                target.m_invisibility.AddFlag(type);
                target.m_invisibility.AddValue(type, GetAmount());
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModInvisibility))
                {
                    // if not have different invisibility auras.
                    // remove glow vision
                    var playerTarget = target.ToPlayer();
                    if (playerTarget != null)
                        playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                    target.m_invisibility.DelFlag(type);
                }
                else
                {
                    var found = false;
                    var invisAuras = target.GetAuraEffectsByType(AuraType.ModInvisibility);
                    foreach (var eff in invisAuras)
                    {
                        if (GetMiscValue() == eff.GetMiscValue())
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        target.m_invisibility.DelFlag(type);
                }

                target.m_invisibility.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after chainging state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                // drop flag at invisibiliy in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
            }
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModStealthDetect)]
        private void HandleModStealthDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();
            var type = (StealthType)GetMiscValue();

            if (apply)
            {
                target.m_stealthDetect.AddFlag(type);
                target.m_stealthDetect.AddValue(type, GetAmount());
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModStealthDetect))
                    target.m_stealthDetect.DelFlag(type);

                target.m_stealthDetect.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModStealth)]
        private void HandleModStealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            var target = aurApp.GetTarget();
            var type = (StealthType)GetMiscValue();

            if (apply)
            {
                target.m_stealth.AddFlag(type);
                target.m_stealth.AddValue(type, GetAmount());
                target.AddVisFlags(UnitVisFlags.Creep);
                var playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    playerTarget.AddAuraVision(PlayerFieldByte2Flags.Stealth);
            }
            else
            {
                target.m_stealth.AddValue(type, -GetAmount());

                if (!target.HasAuraType(AuraType.ModStealth)) // if last SPELL_AURA_MOD_STEALTH
                {
                    target.m_stealth.DelFlag(type);

                    target.RemoveVisFlags(UnitVisFlags.Creep);
                    var playerTarget = target.ToPlayer();
                    if (playerTarget != null)
                        playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.Stealth);
                }
            }

            // call functions which may have additional effects after chainging state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                // drop flag at stealth in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
            }
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModStealthLevel)]
        private void HandleModStealthLevel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();
            var type = (StealthType)GetMiscValue();

            if (apply)
                target.m_stealth.AddValue(type, GetAmount());
            else
                target.m_stealth.AddValue(type, -GetAmount());

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.DetectAmore)]
        private void HandleDetectAmore(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                return;

            if (apply)
            {
                var playerTarget = target.ToPlayer();
                if (playerTarget != null)
                {
                    playerTarget.AddAuraVision((PlayerFieldByte2Flags)(1 << (GetMiscValue() - 1)));
                }
            }
            else
            {
                if (target.HasAuraType(AuraType.DetectAmore))
                {
                    var amoreAuras = target.GetAuraEffectsByType(AuraType.DetectAmore);
                    foreach (var auraEffect in amoreAuras)
                    {
                        if (GetMiscValue() == auraEffect.GetMiscValue())
                            return;
                    }
                }

                var playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    playerTarget.RemoveAuraVision((PlayerFieldByte2Flags)(1 << (GetMiscValue() - 1)));
            }
        }

        [AuraEffectHandler(AuraType.SpiritOfRedemption)]
        private void HandleSpiritOfRedemption(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // prepare spirit state
            if (apply)
            {
                if (target.IsTypeId(TypeId.Player))
                {
                    // disable breath/etc timers
                    target.ToPlayer().StopMirrorTimers();

                    // set stand state (expected in this form)
                    if (!target.IsStandState())
                        target.SetStandState(UnitStandStateType.Stand);
                }
            }
            // die at aura end
            else if (target.IsAlive())
                // call functions which may have additional effects after chainging state of unit
                target.SetDeathState(DeathState.JustDied);
        }

        [AuraEffectHandler(AuraType.Ghost)]
        private void HandleAuraGhost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
            {
                target.AddPlayerFlag(PlayerFlags.Ghost);
                target.m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
                target.m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
            }
            else
            {
                if (target.HasAuraType(AuraType.Ghost))
                    return;

                target.RemovePlayerFlag(PlayerFlags.Ghost);
                target.m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
                target.m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
            }
        }

        [AuraEffectHandler(AuraType.Phase)]
        private void HandlePhase(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                PhasingHandler.AddPhase(target, (uint)GetMiscValueB(), true);

                // call functions which may have additional effects after chainging state of unit
                // phase auras normally not expected at BG but anyway better check
                // drop flag at invisibiliy in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
            }
            else
                PhasingHandler.RemovePhase(target, (uint)GetMiscValueB(), true);
        }

        [AuraEffectHandler(AuraType.PhaseGroup)]
        private void HandlePhaseGroup(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                PhasingHandler.AddPhaseGroup(target, (uint)GetMiscValueB(), true);

                // call functions which may have additional effects after chainging state of unit
                // phase auras normally not expected at BG but anyway better check
                // drop flag at invisibiliy in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
            }
            else
                PhasingHandler.RemovePhaseGroup(target, (uint)GetMiscValueB(), true);
        }

        [AuraEffectHandler(AuraType.PhaseAlwaysVisible)]
        private void HandlePhaseAlwaysVisible(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (!apply)
                PhasingHandler.SetAlwaysVisible(target, true, true);
            else
            {
                if (target.HasAuraType(AuraType.PhaseAlwaysVisible) || (target.IsPlayer() && target.ToPlayer().IsGameMaster()))
                    return;

                PhasingHandler.SetAlwaysVisible(target, false, true);
            }
        }
        
        /**********************/
        /***   UNIT MODEL   ***/
        /**********************/
        [AuraEffectHandler(AuraType.ModShapeshift)]
        private void HandleAuraModShapeshift(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetMiscValue());
            //ASSERT(shapeInfo, "Spell {0} uses unknown ShapeshiftForm (%u).", GetId(), GetMiscValue());

            var target = aurApp.GetTarget();
            var form = (ShapeShiftForm)GetMiscValue();
            var modelid = target.GetModelForForm(form, GetId());

            if (apply)
            {
                // remove polymorph before changing display id to keep new display id
                switch (form)
                {
                    case ShapeShiftForm.CatForm:
                    case ShapeShiftForm.TreeOfLife:
                    case ShapeShiftForm.TravelForm:
                    case ShapeShiftForm.AquaticForm:
                    case ShapeShiftForm.BearForm:
                    case ShapeShiftForm.FlightFormEpic:
                    case ShapeShiftForm.FlightForm:
                    case ShapeShiftForm.MoonkinForm:
                        {
                            // remove movement affects
                            target.RemoveAurasByShapeShift();

                            // and polymorphic affects
                            if (target.IsPolymorphed())
                                target.RemoveAurasDueToSpell(target.GetTransForm());
                            break;
                        }
                    default:
                        break;
                }

                // remove other shapeshift before applying a new one
                target.RemoveAurasByType(AuraType.ModShapeshift, ObjectGuid.Empty, GetBase());

                // stop handling the effect if it was removed by linked event
                if (aurApp.HasRemoveMode())
                    return;

                var prevForm = target.GetShapeshiftForm();
                target.SetShapeshiftForm(form);
                // add the shapeshift aura's boosts
                if (prevForm != form)
                    HandleShapeshiftBoosts(target, true);

                if (modelid > 0)
                {
                    var transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransForm(), GetBase().GetCastDifficulty());
                    if (transformSpellInfo == null || !GetSpellInfo().IsPositive())
                        target.SetDisplayId(modelid);
                }
            }
            else
            {
                // reset model id if no other auras present
                // may happen when aura is applied on linked event on aura removal
                if (!target.HasAuraType(AuraType.ModShapeshift))
                {
                    target.SetShapeshiftForm(ShapeShiftForm.None);
                    if (target.GetClass() == Class.Druid)
                    {
                        // Remove movement impairing effects also when shifting out
                        target.RemoveAurasByShapeShift();
                    }
                }

                if (modelid > 0)
                    target.RestoreDisplayId(target.IsMounted());

                switch (form)
                {
                    // Nordrassil Harness - bonus
                    case ShapeShiftForm.BearForm:
                    case ShapeShiftForm.CatForm:
                        var dummy = target.GetAuraEffect(37315, 0);
                        if (dummy != null)
                            target.CastSpell(target, 37316, true, null, dummy);
                        break;
                    // Nordrassil Regalia - bonus
                    case ShapeShiftForm.MoonkinForm:
                        dummy = target.GetAuraEffect(37324, 0);
                        if (dummy != null)
                            target.CastSpell(target, 37325, true, null, dummy);
                        break;
                    default:
                        break;
                }

                // remove the shapeshift aura's boosts
                HandleShapeshiftBoosts(target, apply);
            }

            var playerTarget = target.ToPlayer();
            if (playerTarget != null)
            {
                playerTarget.SendMovementSetCollisionHeight(playerTarget.GetCollisionHeight(false), UpdateCollisionHeightReason.Force);
                playerTarget.InitDataForForm();
            }
            else
                target.UpdateDisplayPower();

            if (target.GetClass() == Class.Druid)
            {
                // Dash
                var aurEff = target.GetAuraEffect(AuraType.ModIncreaseSpeed, SpellFamilyNames.Druid, new FlagArray128(0, 0, 0x8));
                if (aurEff != null)
                    aurEff.RecalculateAmount();

                // Disarm handling
                // If druid shifts while being disarmed we need to deal with that since forms aren't affected by disarm
                // and also HandleAuraModDisarm is not triggered
                if (!target.CanUseAttackType(WeaponAttackType.BaseAttack))
                {
                    Item pItem = target.ToPlayer().GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                    if (pItem != null)
                        target.ToPlayer()._ApplyWeaponDamage(EquipmentSlot.MainHand, pItem, apply);
                }
            }

            // stop handling the effect if it was removed by linked event
            if (apply && aurApp.HasRemoveMode())
                return;

            if (target.IsTypeId(TypeId.Player))
            {
                // Learn spells for shapeshift form - no need to send action bars or add spells to spellbook
                for (byte i = 0; i < SpellConst.MaxShapeshift; ++i)
                {
                    if (shapeInfo.PresetSpellID[i] == 0)
                        continue;
                    if (apply)
                        target.ToPlayer().AddTemporarySpell(shapeInfo.PresetSpellID[i]);
                    else
                        target.ToPlayer().RemoveTemporarySpell(shapeInfo.PresetSpellID[i]);
                }
            }
        }

        [AuraEffectHandler(AuraType.Transform)]
        private void HandleAuraTransform(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                // update active transform spell only when transform not set or not overwriting negative by positive case
                var transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransForm(), GetBase().GetCastDifficulty());
                if (transformSpellInfo == null || !GetSpellInfo().IsPositive() || transformSpellInfo.IsPositive())
                {
                    target.SetTransForm(GetId());
                    // special case (spell specific functionality)
                    if (GetMiscValue() == 0)
                    {
                        switch (GetId())
                        {
                            // Orb of Deception
                            case 16739:
                                {
                                    if (!target.IsTypeId(TypeId.Player))
                                        return;

                                    switch (target.GetRace())
                                    {
                                        // Blood Elf
                                        case Race.BloodElf:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 17830 : 17829u);
                                            break;
                                        // Orc
                                        case Race.Orc:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10140 : 10139u);
                                            break;
                                        // Troll
                                        case Race.Troll:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10134 : 10135u);
                                            break;
                                        // Tauren
                                        case Race.Tauren:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10147 : 10136u);
                                            break;
                                        // Undead
                                        case Race.Undead:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10145 : 10146u);
                                            break;
                                        // Draenei
                                        case Race.Draenei:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 17828 : 17827u);
                                            break;
                                        // Dwarf
                                        case Race.Dwarf:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10142 : 10141u);
                                            break;
                                        // Gnome
                                        case Race.Gnome:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10149 : 10148u);
                                            break;
                                        // Human
                                        case Race.Human:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10138 : 10137u);
                                            break;
                                        // Night Elf
                                        case Race.NightElf:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 10144 : 10143u);
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                }
                            // Murloc costume
                            case 42365:
                                target.SetDisplayId(21723);
                                break;
                            // Dread Corsair
                            case 50517:
                            // Corsair Costume
                            case 51926:
                                {
                                    if (!target.IsTypeId(TypeId.Player))
                                        return;

                                    switch (target.GetRace())
                                    {
                                        // Blood Elf
                                        case Race.BloodElf:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25043 : 25032u);
                                            break;
                                        // Orc
                                        case Race.Orc:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25050 : 25039u);
                                            break;
                                        // Troll
                                        case Race.Troll:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25052 : 25041u);
                                            break;
                                        // Tauren
                                        case Race.Tauren:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25051 : 25040u);
                                            break;
                                        // Undead
                                        case Race.Undead:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25053 : 25042u);
                                            break;
                                        // Draenei
                                        case Race.Draenei:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25044 : 25033u);
                                            break;
                                        // Dwarf
                                        case Race.Dwarf:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25045 : 25034u);
                                            break;
                                        // Gnome
                                        case Race.Gnome:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25035 : 25046u);
                                            break;
                                        // Human
                                        case Race.Human:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25037 : 25048u);
                                            break;
                                        // Night Elf
                                        case Race.NightElf:
                                            target.SetDisplayId(target.GetGender() == Gender.Female ? 25038 : 25049u);
                                            break;
                                        default:
                                            break;
                                    }
                                    break;
                                }
                            // Pygmy Oil
                            case 53806:
                                target.SetDisplayId(22512);
                                break;
                            // Honor the Dead
                            case 65386:
                            case 65495:
                                target.SetDisplayId(target.GetGender() == Gender.Male ? 29203 : 29204u);
                                break;
                            // Darkspear Pride
                            case 75532:
                                target.SetDisplayId(target.GetGender() == Gender.Male ? 31737 : 31738u);
                                break;
                            // Gnomeregan Pride
                            case 75531:
                                target.SetDisplayId(target.GetGender() == Gender.Male ? 31654 : 31655u);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)GetMiscValue());
                        if (ci == null)
                        {
                            target.SetDisplayId(16358);              // pig pink ^_^
                            Log.outError(LogFilter.Spells, "Auras: unknown creature id = {0} (only need its modelid) From Spell Aura Transform in Spell ID = {1}", GetMiscValue(), GetId());
                        }
                        else
                        {
                            uint model_id = 0;
                            var modelid = ObjectManager.ChooseDisplayId(ci).CreatureDisplayID;
                            if (modelid != 0)
                                model_id = modelid;                     // Will use the default model here

                            target.SetDisplayId(model_id);

                            // Dragonmaw Illusion (set mount model also)
                            if (GetId() == 42016 && target.GetMountDisplayId() != 0 && !target.GetAuraEffectsByType(AuraType.ModIncreaseMountedFlightSpeed).Empty())
                                target.SetMountDisplayId(16314);
                        }
                    }
                }

                // polymorph case
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real) && target.IsTypeId(TypeId.Player) && target.IsPolymorphed())
                {
                    // for players, start regeneration after 1s (in polymorph fast regeneration case)
                    // only if caster is Player (after patch 2.4.2)
                    if (GetCasterGUID().IsPlayer())
                        target.ToPlayer().SetRegenTimerCount(1 * Time.InMilliseconds);

                    //dismount polymorphed target (after patch 2.4.2)
                    if (target.IsMounted())
                        target.RemoveAurasByType(AuraType.Mounted);
                }
            }
            else
            {
                if (target.GetTransForm() == GetId())
                    target.SetTransForm(0);

                target.RestoreDisplayId(target.IsMounted());

                // Dragonmaw Illusion (restore mount model)
                if (GetId() == 42016 && target.GetMountDisplayId() == 16314)
                {
                    if (!target.GetAuraEffectsByType(AuraType.Mounted).Empty())
                    {
                        var cr_id = target.GetAuraEffectsByType(AuraType.Mounted)[0].GetMiscValue();
                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)cr_id);
                        if (ci != null)
                        {
                            var model = ObjectManager.ChooseDisplayId(ci);
                            Global.ObjectMgr.GetCreatureModelRandomGender(ref model, ci);

                            target.SetMountDisplayId(model.CreatureDisplayID);
                        }
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModScale)]
        [AuraEffectHandler(AuraType.ModScale2)]
        public void HandleAuraModScale(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            var target = aurApp.GetTarget();

            var scale = target.GetObjectScale();
            scale += MathFunctions.CalculatePct(1.0f, apply ? GetAmount() : -GetAmount());
            target.SetObjectScale(scale);
        }

        [AuraEffectHandler(AuraType.CloneCaster)]
        private void HandleAuraCloneCaster(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                var caster = GetCaster();
                if (caster == null || caster == target)
                    return;

                // What must be cloned? at least display and scale
                target.SetDisplayId(caster.GetDisplayId());
                //target.SetObjectScale(caster.GetFloatValue(OBJECT_FIELD_SCALE_X)); // we need retail info about how scaling is handled (aura maybe?)
                target.AddUnitFlag2(UnitFlags2.MirrorImage);
            }
            else
            {
                target.SetDisplayId(target.GetNativeDisplayId());
                target.RemoveUnitFlag2(UnitFlags2.MirrorImage);
            }
        }

        /************************/
        /***      FIGHT       ***/
        /************************/
        [AuraEffectHandler(AuraType.FeignDeath)]
        private void HandleFeignDeath(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                var targets = new List<Unit>();
                var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(target, target, target.GetMap().GetVisibilityRange());
                var searcher = new UnitListSearcher(target, targets, u_check);

                Cell.VisitAllObjects(target, searcher, target.GetMap().GetVisibilityRange());
                foreach (var unit in targets)
                {
                    if (!unit.HasUnitState(UnitState.Casting))
                        continue;

                    for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
                    {
                        if (unit.GetCurrentSpell(i) != null
                        && unit.GetCurrentSpell(i).m_targets.GetUnitTargetGUID() == target.GetGUID())
                        {
                            unit.InterruptSpell(i, false);
                        }
                    }
                }
                target.CombatStop();
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);

                // prevent interrupt message
                if (GetCasterGUID() == target.GetGUID() && target.GetCurrentSpell(CurrentSpellTypes.Generic) != null)
                    target.FinishSpell(CurrentSpellTypes.Generic, false);
                target.InterruptNonMeleeSpells(true);
                target.GetHostileRefManager().DeleteReferences();

                // stop handling the effect if it was removed by linked event
                if (aurApp.HasRemoveMode())
                    return;

                target.AddUnitFlag(UnitFlags.Unk29);
                target.AddUnitFlag2(UnitFlags2.FeignDeath);
                target.AddDynamicFlag(UnitDynFlags.Dead);
                target.AddUnitState(UnitState.Died);
            }
            else
            {
                target.RemoveUnitFlag(UnitFlags.Unk29);
                target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
                target.RemoveDynamicFlag(UnitDynFlags.Dead);
                target.ClearUnitState(UnitState.Died);
            }
        }

        [AuraEffectHandler(AuraType.ModUnattackable)]
        private void HandleModUnattackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (!apply && target.HasAuraType(AuraType.ModUnattackable))
                return;

            if (apply)
                target.AddUnitFlag(UnitFlags.NonAttackable);
            else
                target.RemoveUnitFlag(UnitFlags.NonAttackable);

            // call functions which may have additional effects after chainging state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                target.CombatStop();
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
            }
        }

        [AuraEffectHandler(AuraType.ModDisarm)]
        private void HandleAuraModDisarm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            var type = GetAuraType();

            //Prevent handling aura twice
            if (apply ? target.GetAuraEffectsByType(type).Count > 1 : target.HasAuraType(type))
                return;

            Action<Unit> flagAddFn = null;
            Action<Unit> flagRemoveFn = null;
            uint slot;
            WeaponAttackType attType;
            switch (type)
            {
                case AuraType.ModDisarm:
                    if (apply)
                        flagAddFn = unit => { unit.AddUnitFlag(UnitFlags.Disarmed); };
                    else
                        flagRemoveFn = unit => { unit.RemoveUnitFlag(UnitFlags.Disarmed); };
                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.BaseAttack;
                    break;
                case AuraType.ModDisarmOffhand:
                    if (apply)
                        flagAddFn = unit => { unit.AddUnitFlag2(UnitFlags2.DisarmOffhand); };
                    else
                        flagRemoveFn = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmOffhand); };
                    slot = EquipmentSlot.OffHand;
                    attType = WeaponAttackType.OffAttack;
                    break;
                case AuraType.ModDisarmRanged:
                    if (apply)
                        flagAddFn = unit => { unit.AddUnitFlag2(UnitFlags2.DisarmRanged); };
                    else
                        flagRemoveFn = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmRanged); };
                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.RangedAttack;
                    break;
                default:
                    return;
            }

            // if disarm aura is to be removed, remove the flag first to reapply damage/aura mods
            flagRemoveFn?.Invoke(target);

            // Handle damage modification, shapeshifted druids are not affected
            if (target.IsTypeId(TypeId.Player) && !target.IsInFeralForm())
            {
                var player = target.ToPlayer();

                Item item = player.GetItemByPos(InventorySlots.Bag0, (byte)slot);
                if (item != null)
                {
                    var attackType = Player.GetAttackBySlot((byte)slot, item.GetTemplate().GetInventoryType());

                    player.ApplyItemDependentAuras(item, !apply);
                    if (attackType < WeaponAttackType.Max)
                    {
                        player._ApplyWeaponDamage(slot, item, !apply);
                        if (!apply) // apply case already handled on item dependent aura removal (if any)
                            player.UpdateWeaponDependentAuras(attackType);
                    }
                }
            }

            // if disarm effects should be applied, wait to set flag until damage mods are unapplied
            flagAddFn?.Invoke(target);

            if (target.IsTypeId(TypeId.Unit) && target.ToCreature().GetCurrentEquipmentId() != 0)
                target.UpdateDamagePhysical(attType);
        }

        [AuraEffectHandler(AuraType.ModSilence)]
        private void HandleAuraModSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                target.AddUnitFlag(UnitFlags.Silenced);

                // call functions which may have additional effects after chainging state of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    var spell = target.GetCurrentSpell(i);
                    if (spell != null)
                        if (spell.m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence))
                            // Stop spells on prepare or casting state
                            target.InterruptSpell(i, false);
                }
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModSilence) || target.HasAuraType(AuraType.ModPacifySilence))
                    return;

                target.RemoveUnitFlag(UnitFlags.Silenced);
            }
        }

        [AuraEffectHandler(AuraType.ModPacify)]
        private void HandleAuraModPacify(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
                target.AddUnitFlag(UnitFlags.Pacified);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModPacify) || target.HasAuraType(AuraType.ModPacifySilence))
                    return;
                target.RemoveUnitFlag(UnitFlags.Pacified);
            }
        }

        [AuraEffectHandler(AuraType.ModPacifySilence)]
        private void HandleAuraModPacifyAndSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            // Vengeance of the Blue Flight (@todo REMOVE THIS!)
            // @workaround
            if (m_spellInfo.Id == 45839)
            {
                if (apply)
                    target.AddUnitFlag(UnitFlags.NonAttackable);
                else
                    target.RemoveUnitFlag(UnitFlags.NonAttackable);
            }
            if (!(apply))
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModPacifySilence))
                    return;
            }
            HandleAuraModPacify(aurApp, mode, apply);
            HandleAuraModSilence(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.DisableAttackingExceptAbilities)]
        private void HandleAuraDisableAttackingExceptAbilities(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            if (apply)
                aurApp.GetTarget().AttackStop();
        }
        
        [AuraEffectHandler(AuraType.ModNoActions)]
        private void HandleAuraModNoActions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                target.AddUnitFlag2(UnitFlags2.NoActions);

                // call functions which may have additional effects after chainging state of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    var spell = target.GetCurrentSpell(i);
                    if (spell)
                        if (spell.m_spellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions))
                            // Stop spells on prepare or casting state
                            target.InterruptSpell(i, false);
                }
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModNoActions))
                    return;

                target.RemoveUnitFlag2(UnitFlags2.NoActions);
            }
        }

        /****************************/
        /***      TRACKING        ***/
        /****************************/
        [AuraEffectHandler(AuraType.TrackCreatures)]
        private void HandleAuraTrackCreatures(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.AddTrackCreatureFlag(1u << (GetMiscValue() - 1));
            else
                target.RemoveTrackCreatureFlag(1u << (GetMiscValue() - 1));
        }

        [AuraEffectHandler(AuraType.TrackResources)]
        private void HandleAuraTrackResources(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            var bitIndex = (uint)GetMiscValue() - 1;
            var index = bitIndex / 32;
            var flag = 1u << ((int)bitIndex % 32);
            if (apply)
                target.AddTrackResourceFlag(index, flag);
            else
                target.RemoveTrackResourceFlag(index, flag);
        }

        [AuraEffectHandler(AuraType.TrackStealthed)]
        private void HandleAuraTrackStealthed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (!(apply))
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }
            if (apply)
                target.AddPlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
            else
                target.RemovePlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
        }

        [AuraEffectHandler(AuraType.ModStalked)]
        private void HandleAuraModStalked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            // used by spells: Hunter's Mark, Mind Vision, Syndicate Tracker (MURP) DND
            if (apply)
                target.AddDynamicFlag(UnitDynFlags.TrackUnit);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (!target.HasAuraType(GetAuraType()))
                    target.RemoveDynamicFlag(UnitDynFlags.TrackUnit);
            }

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.Untrackable)]
        private void HandleAuraUntrackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.AddVisFlags(UnitVisFlags.Untrackable);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveVisFlags(UnitVisFlags.Untrackable);
            }
        }

        /****************************/
        /***  SKILLS & TALENTS    ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModSkill)]
        [AuraEffectHandler(AuraType.ModSkill2)]
        private void HandleAuraModSkill(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Skill)))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            var prot = (SkillType)GetMiscValue();
            var points = GetAmount();

            if (prot == SkillType.Defense)
                return;

            target.ModifySkillBonus(prot, (apply ? points : -points), GetAuraType() == AuraType.ModSkillTalent);
        }

        /****************************/
        /***       MOVEMENT       ***/
        /****************************/
        [AuraEffectHandler(AuraType.Mounted)]
        private void HandleAuraMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                var creatureEntry = (uint)GetMiscValue();
                uint displayId = 0;
                uint vehicleId = 0;

                var mountEntry = Global.DB2Mgr.GetMount(GetId());
                if (mountEntry != null)
                {
                    var mountDisplays = Global.DB2Mgr.GetMountDisplays(mountEntry.Id);
                    if (mountDisplays != null)
                    {
                        if (mountEntry.IsSelfMount())
                        {
                            displayId = 73200; //DisplayId: HiddenMount 
                        }
                        else
                        {
                            var usableDisplays = mountDisplays.Where(mountDisplay =>
                            {
                                var playerTarget = target.ToPlayer();
                                if (playerTarget)
                                {
                                    var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountDisplay.PlayerConditionID);
                                    if (playerCondition != null)
                                        return ConditionManager.IsPlayerMeetingCondition(playerTarget, playerCondition);
                                }

                                return true;
                            }).ToList();

                            if (!usableDisplays.Empty())
                                displayId = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                        }
                    }
                    // TODO: CREATE TABLE mount_vehicle (mountId, vehicleCreatureId) for future mounts that are vehicles (new mounts no longer have proper data in MiscValue)
                    //if (MountVehicle const* mountVehicle = sObjectMgr.GetMountVehicle(mountEntry.Id))
                    //    creatureEntry = mountVehicle.VehicleCreatureId;
                }

                CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creatureEntry);
                if (creatureInfo != null)
                {
                    vehicleId = creatureInfo.VehicleId;

                    if (displayId == 0)
                    {
                        var model = ObjectManager.ChooseDisplayId(creatureInfo);
                        Global.ObjectMgr.GetCreatureModelRandomGender(ref model, creatureInfo);
                        displayId = model.CreatureDisplayID;
                    }

                    //some spell has one aura of mount and one of vehicle
                    foreach (var effect in GetSpellInfo().GetEffects())
                        if (effect != null && GetSpellEffectInfo().Effect == SpellEffectName.Summon && effect.MiscValue == GetMiscValue())
                            displayId = 0;
                }

                target.Mount(displayId, vehicleId, creatureEntry);

                // cast speed aura
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                {
                    var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(GetAmount());
                    if (mountCapability != null)
                        target.CastSpell(target, mountCapability.ModSpellAuraID, true);
                }
            }
            else
            {
                target.Dismount();
                //some mounts like Headless Horseman's Mount or broom stick are skill based spell
                // need to remove ALL arura related to mounts, this will stop client crash with broom stick
                // and never endless flying after using Headless Horseman's Mount
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                {
                    target.RemoveAurasByType(AuraType.Mounted);

                    // remove speed aura
                    var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(GetAmount());
                    if (mountCapability != null)
                        target.RemoveAurasDueToSpell(mountCapability.ModSpellAuraID, target.GetGUID());
                }
            }
        }

        [AuraEffectHandler(AuraType.Fly)]
        private void HandleAuraAllowFlight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    return;
            }

            target.SetCanTransitionBetweenSwimAndFly(apply);

            if (target.SetCanFly(apply))
                if (!apply && !target.IsLevitating())
                    target.GetMotionMaster().MoveFall();
        }

        [AuraEffectHandler(AuraType.WaterWalk)]
        private void HandleAuraWaterWalk(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetWaterWalking(apply);
        }

        [AuraEffectHandler(AuraType.FeatherFall)]
        private void HandleAuraFeatherFall(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetFeatherFall(apply);

            // start fall from current height
            if (!apply && target.IsTypeId(TypeId.Player))
                target.ToPlayer().SetFallInformation(0, target.GetPositionZ());
        }

        [AuraEffectHandler(AuraType.Hover)]
        private void HandleAuraHover(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetHover(apply);    //! Sets movementflags
        }

        [AuraEffectHandler(AuraType.WaterBreathing)]
        private void HandleWaterBreathing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            // update timers in client
            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateMirrorTimers();
        }

        [AuraEffectHandler(AuraType.ForceMoveForward)]
        private void HandleForceMoveForward(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
                target.AddUnitFlag2(UnitFlags2.ForceMove);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveUnitFlag2(UnitFlags2.ForceMove);
            }
        }

        [AuraEffectHandler(AuraType.CanTurnWhileFalling)]
        private void HandleAuraCanTurnWhileFalling(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetCanTurnWhileFalling(apply);
        }

        [AuraEffectHandler(AuraType.IgnoreMovementForces)]
        private void HandleIgnoreMovementForces(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();
            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetIgnoreMovementForces(apply);
        }

        /****************************/
        /***        THREAT        ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModThreat)]
        private void HandleModThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();
            for (byte i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                {
                    if (apply)
                        MathFunctions.AddPct(ref target.m_threatModifier[i], GetAmount());
                    else
                    {
                        var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModThreat, 1u << i);
                        target.m_threatModifier[i] = amount;
                    }
                }
        }

        [AuraEffectHandler(AuraType.ModTotalThreat)]
        private void HandleAuraModTotalThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsAlive() || !target.IsTypeId(TypeId.Player))
                return;

            var caster = GetCaster();
            if (caster != null && caster.IsAlive())
                target.GetHostileRefManager().AddTempThreat(GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModTaunt)]
        private void HandleModTaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsAlive() || !target.CanHaveThreatList())
                return;

            var caster = GetCaster();
            if (caster == null || !caster.IsAlive())
                return;

            if (apply)
                target.TauntApply(caster);
            else
            {
                // When taunt aura fades out, mob will switch to previous target if current has less than 1.1 * secondthreat
                target.TauntFadeOut(caster);
            }
        }

        /*****************************/
        /***        CONTROL        ***/
        /*****************************/
        [AuraEffectHandler(AuraType.ModConfuse)]
        private void HandleModConfuse(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Confused);
        }

        [AuraEffectHandler(AuraType.ModFear)]
        private void HandleModFear(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Fleeing);
        }

        [AuraEffectHandler(AuraType.ModStun)]
        private void HandleAuraModStun(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Stunned);
        }

        [AuraEffectHandler(AuraType.ModRoot)]
        [AuraEffectHandler(AuraType.ModRoot2)]
        private void HandleAuraModRoot(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Root);
        }

        [AuraEffectHandler(AuraType.PreventsFleeing)]
        private void HandlePreventFleeing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            // Since patch 3.0.2 this mechanic no longer affects fear effects. It will ONLY prevent humanoids from fleeing due to low health.
            if (!apply || target.HasAuraType(AuraType.ModFear))
                return;
            // TODO: find a way to cancel fleeing for assistance.
            // Currently this will only stop creatures fleeing due to low health that could not find nearby allies to flee towards.
            target.SetControlled(false, UnitState.Fleeing);
        }

        /***************************/
        /***        CHARM        ***/
        /***************************/
        [AuraEffectHandler(AuraType.ModPossess)]
        private void HandleModPossess(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            var caster = GetCaster();

            // no support for posession AI yet
            if (caster != null && caster.IsTypeId(TypeId.Unit))
            {
                HandleModCharm(aurApp, mode, apply);
                return;
            }

            if (apply)
                target.SetCharmedBy(caster, CharmType.Possess, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        [AuraEffectHandler(AuraType.ModPossessPet)]
        private void HandleModPossessPet(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            // Used by spell "Eyes of the Beast"

            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var caster = GetCaster();
            if (caster == null || !caster.IsTypeId(TypeId.Player))
                return;

            var target = aurApp.GetTarget();
            if (!target.IsTypeId(TypeId.Unit) || !target.IsPet())
                return;

            var pet = target.ToPet();

            if (apply)
            {
                if (caster.ToPlayer().GetPet() != pet)
                    return;

                // Must clear current motion or pet leashes back to owner after a few yards
                //  when under spell 'Eyes of the Beast'
                pet.GetMotionMaster().Clear();
                pet.SetCharmedBy(caster, CharmType.Possess, aurApp);
            }
            else
            {
                pet.RemoveCharmedBy(caster);

                if (!pet.IsWithinDistInMap(caster, pet.GetMap().GetVisibilityRange()))
                    pet.Remove(PetSaveMode.NotInSlot, true);
                else
                {
                    // Reinitialize the pet bar or it will appear greyed out
                    caster.ToPlayer().PetSpellInitialize();

                    // Follow owner only if not fighting or owner didn't click "stay" at new location
                    // This may be confusing because pet bar shows "stay" when under the spell but it retains
                    //  the "follow" flag. Player MUST click "stay" while under the spell.
                    if (pet.GetVictim() == null && !pet.GetCharmInfo().HasCommandState(CommandStates.Stay))
                    {
                        pet.GetMotionMaster().MoveFollow(caster, SharedConst.PetFollowDist, pet.GetFollowAngle());
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModCharm)]
        private void HandleModCharm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            var caster = GetCaster();

            if (apply)
                target.SetCharmedBy(caster, CharmType.Charm, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        [AuraEffectHandler(AuraType.AoeCharm)]
        private void HandleCharmConvert(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            var caster = GetCaster();

            if (apply)
                target.SetCharmedBy(caster, CharmType.Convert, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        /**
         * Such auras are applied from a caster(=player) to a vehicle.
         * This has been verified using spell #49256
         */
        [AuraEffectHandler(AuraType.ControlVehicle)]
        private void HandleAuraControlVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();
            if (!target.IsVehicle())
                return;

            var caster = GetCaster();
            if (caster == null || caster == target)
                return;

            if (apply)
            {
                // Currently spells that have base points  0 and DieSides 0 = "0/0" exception are pushed to -1,
                // however the idea of 0/0 is to ingore flag VEHICLE_SEAT_FLAG_CAN_ENTER_OR_EXIT and -1 checks for it,
                // so this break such spells or most of them.
                // Current formula about m_amount: effect base points + dieside - 1
                // TO DO: Reasearch more about 0/0 and fix it.
                caster._EnterVehicle(target.GetVehicleKit(), (sbyte)(m_amount - 1), aurApp);
            }
            else
            {
                // Remove pending passengers before exiting vehicle - might cause an Uninstall
                target.GetVehicleKit().RemovePendingEventsForPassenger(caster);

                if (GetId() == 53111) // Devour Humanoid
                {
                    target.Kill(caster);
                    if (caster.IsTypeId(TypeId.Unit))
                        caster.ToCreature().DespawnOrUnsummon();
                }

                var seatChange = mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmount)                             // Seat change on the same direct vehicle
                                 || target.HasAuraTypeWithCaster(AuraType.ControlVehicle, caster.GetGUID());                                 // Seat change to a proxy vehicle (for example turret mounted on a siege engine)

                if (!seatChange)
                    caster._ExitVehicle();
                else
                    target.GetVehicleKit().RemovePassenger(caster);  // Only remove passenger from vehicle without launching exit movement or despawning the vehicle

                // some SPELL_AURA_CONTROL_VEHICLE auras have a dummy effect on the player - remove them
                caster.RemoveAurasDueToSpell(GetId());
            }
        }

        /*********************************************************/
        /***                  MODIFY SPEED                     ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.ModIncreaseSpeed)]
        [AuraEffectHandler(AuraType.ModSpeedAlways)]
        [AuraEffectHandler(AuraType.ModSpeedNotStack)]
        [AuraEffectHandler(AuraType.ModMinimumSpeed)]
        private void HandleAuraModIncreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
        }

        [AuraEffectHandler(AuraType.ModIncreaseMountedSpeed)]
        [AuraEffectHandler(AuraType.ModMountedSpeedAlways)]
        [AuraEffectHandler(AuraType.ModMountedSpeedNotStack)]
        private void HandleAuraModIncreaseMountedSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleAuraModIncreaseSpeed(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModIncreaseVehicleFlightSpeed)]
        [AuraEffectHandler(AuraType.ModIncreaseMountedFlightSpeed)]
        [AuraEffectHandler(AuraType.ModIncreaseFlightSpeed)]
        [AuraEffectHandler(AuraType.ModMountedFlightSpeedAlways)]
        [AuraEffectHandler(AuraType.ModVehicleSpeedAlways)]
        [AuraEffectHandler(AuraType.ModFlightSpeedNotStack)]
        private void HandleAuraModIncreaseFlightSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            var target = aurApp.GetTarget();
            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                target.UpdateSpeed(UnitMoveType.Flight);

            //! Update ability to fly
            if (GetAuraType() == AuraType.ModIncreaseMountedFlightSpeed)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask) && (apply || (!target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !target.HasAuraType(AuraType.Fly))))
                {
                    target.SetCanTransitionBetweenSwimAndFly(apply);

                    if (target.SetCanFly(apply))
                        if (!apply && !target.IsLevitating())
                            target.GetMotionMaster().MoveFall();
                }

                //! Someone should clean up these hacks and remove it from this function. It doesn't even belong here.
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                {
                    //Players on flying mounts must be immune to polymorph
                    if (target.IsTypeId(TypeId.Player))
                        target.ApplySpellImmune(GetId(), SpellImmunity.Mechanic, (uint)Mechanics.Polymorph, apply);

                    // Dragonmaw Illusion (overwrite mount model, mounted aura already applied)
                    if (apply && target.HasAuraEffect(42016, 0) && target.GetMountDisplayId() != 0)
                        target.SetMountDisplayId(16314);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModIncreaseSwimSpeed)]
        private void HandleAuraModIncreaseSwimSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Swim);
        }

        [AuraEffectHandler(AuraType.ModDecreaseSpeed)]
        private void HandleAuraModDecreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
            target.UpdateSpeed(UnitMoveType.Swim);
            target.UpdateSpeed(UnitMoveType.Flight);
            target.UpdateSpeed(UnitMoveType.RunBack);
            target.UpdateSpeed(UnitMoveType.SwimBack);
            target.UpdateSpeed(UnitMoveType.FlightBack);
        }

        [AuraEffectHandler(AuraType.UseNormalMovementSpeed)]
        private void HandleAuraModUseNormalSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
            target.UpdateSpeed(UnitMoveType.Swim);
            target.UpdateSpeed(UnitMoveType.Flight);
        }

        [AuraEffectHandler(AuraType.ModMinimumSpeedRate)]
        private void HandleAuraModMinimumSpeedRate(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
        }

        [AuraEffectHandler(AuraType.ModMovementForceMagnitude)]
        private void HandleModMovementForceMagnitude(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            aurApp.GetTarget().UpdateMovementForcesModMagnitude();
        }

        /*********************************************************/
        /***                     IMMUNITY                      ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.MechanicImmunityMask)]
        private void HandleModMechanicImmunityMask(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.MechanicImmunity)]
        private void HandleModMechanicImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.EffectImmunity)]
        private void HandleAuraModEffectImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            // when removing flag aura, handle flag drop
            var player = target.ToPlayer();
            if (!apply && player != null && GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.ImmuneOrLostSelection))
            {
                if (player.InBattleground())
                {
                    Battleground bg = player.GetBattleground();
                    if (bg)
                        bg.EventPlayerDroppedFlag(player);
                }
                else
                    Global.OutdoorPvPMgr.HandleDropFlag(player, GetSpellInfo().Id);
            }
        }

        [AuraEffectHandler(AuraType.StateImmunity)]
        private void HandleAuraModStateImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.SchoolImmunity)]
        private void HandleAuraModSchoolImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            if (GetSpellInfo().Mechanic == Mechanics.Banish)
            {
                if (apply)
                    target.AddUnitState(UnitState.Isolated);
                else
                {
                    var banishFound = false;
                    var banishAuras = target.GetAuraEffectsByType(GetAuraType());
                    foreach (var aurEff in banishAuras)
                    {
                        if (aurEff.GetSpellInfo().Mechanic == Mechanics.Banish)
                        {
                            banishFound = true;
                            break;
                        }
                    }

                    if (!banishFound)
                        target.ClearUnitState(UnitState.Isolated);
                }
            }

            if (apply && GetMiscValue() == (int)SpellSchoolMask.Normal)
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);

            // remove all flag auras (they are positive, but they must be removed when you are immune)
            if (GetSpellInfo().HasAttribute(SpellAttr1.DispelAurasOnImmunity)
                && GetSpellInfo().HasAttribute(SpellAttr2.DamageReducedShield))
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
        }

        [AuraEffectHandler(AuraType.DamageImmunity)]
        private void HandleAuraModDmgImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.DispelImmunity)]
        private void HandleAuraModDispelImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        /*********************************************************/
        /***                  MODIFY STATS                     ***/
        /*********************************************************/

        /********************************/
        /***        RESISTANCE        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModResistance)]
        private void HandleAuraModResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            for (var x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                if (Convert.ToBoolean(GetMiscValue() & (1 << x)))
                    target.HandleStatFlatModifier(UnitMods.ResistanceStart + x, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModBaseResistancePct)]
        private void HandleAuraModBaseResistancePCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
            {
                //pets only have base armor
                if (target.IsPet() && Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                {
                    if (apply)
                        target.ApplyStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, GetAmount());
                    else
                    {
                        var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, (uint)SpellSchoolMask.Normal);
                        target.SetStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, amount);
                    }
                }
            }
            else
            {
                for (var x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << x)))
                    {
                        if (apply)
                            target.ApplyStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, GetAmount());
                        else
                        {
                            var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, 1u << x);
                            target.SetStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, amount);
                        }
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModResistancePct)]
        private void HandleModResistancePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            for (var i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
            {
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                {
                    var amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModResistancePct, 1u << i);
                    if (target.GetPctModifierValue(UnitMods.ResistanceStart + i, UnitModifierPctType.Total) == amount)
                        continue;

                    target.SetStatPctModifier(UnitMods.ResistanceStart + i, UnitModifierPctType.Total, amount);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModBaseResistance)]
        private void HandleModBaseResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
            {
                //only pets have base stats
                if (target.IsPet() && Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                    target.HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, GetAmount(), apply);
            }
            else
            {
                for (var i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                        target.HandleStatFlatModifier(UnitMods.ResistanceStart + i, UnitModifierFlatType.Total, GetAmount(), apply);
            }
        }

        [AuraEffectHandler(AuraType.ModTargetResistance)]
        private void HandleModTargetResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            // applied to damage as HandleNoImmediateEffect in Unit.CalcAbsorbResist and Unit.CalcArmorReducedDamage

            // show armor penetration
            if (target.IsTypeId(TypeId.Player) && Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                target.ApplyModTargetPhysicalResistance(GetAmount(), apply);

            // show as spell penetration only full spell penetration bonuses (all resistances except armor and holy
            if (target.IsTypeId(TypeId.Player) && ((SpellSchoolMask)GetMiscValue() & SpellSchoolMask.Spell) == SpellSchoolMask.Spell)
                target.ApplyModTargetResistance(GetAmount(), apply);
        }

        /********************************/
        /***           STAT           ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModStat)]
        private void HandleAuraModStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            if (GetMiscValue() < -2 || GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Spell {0} effect {1} has an unsupported misc value ({2}) for SPELL_AURA_MOD_STAT ", GetId(), GetEffIndex(), GetMiscValue());
                return;
            }

            var target = aurApp.GetTarget();
            var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModStat, true, GetMiscValue());
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            for (var i = Stats.Strength; i < Stats.Max; i++)
            {
                // -1 or -2 is all stats (misc < -2 checked in function beginning)
                if (GetMiscValue() < 0 || GetMiscValue() == (int)i)
                {
                    if (spellGroupVal != 0)
                    {
                        target.HandleStatFlatModifier((UnitMods.StatStart + (int)i), UnitModifierFlatType.Total, (float)spellGroupVal, !apply);
                        if (target.IsTypeId(TypeId.Player) || target.IsPet())
                            target.UpdateStatBuffMod(i);
                    }

                    target.HandleStatFlatModifier(UnitMods.StatStart + (int)i, UnitModifierFlatType.Total, GetAmount(), apply);
                    if (target.IsTypeId(TypeId.Player) || target.IsPet())
                        target.UpdateStatBuffMod(i);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModPercentStat)]
        private void HandleModPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (GetMiscValue() < -1 || GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Misc Value for SPELL_AURA_MOD_PERCENT_STAT not valid");
                return;
            }

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
                return;

            for (var i = (int)Stats.Strength; i < (int)Stats.Max; ++i)
            {
                if (GetMiscValue() == i || GetMiscValue() == -1)
                {
                    if (apply)
                        target.ApplyStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, m_amount);
                    else
                    {
                        var amount = target.GetTotalAuraMultiplier(AuraType.ModPercentStat, aurEff =>
                        {
                            if (aurEff.GetMiscValue() == i || aurEff.GetMiscValue() == -1)
                                return true;
                            return false;
                        });
                        target.SetStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, amount);
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModSpellDamageOfStatPercent)]
        private void HandleModSpellDamagePercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Magic damage modifiers implemented in Unit.SpellDamageBonus
            // This information for client side use only
            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModSpellHealingOfStatPercent)]
        private void HandleModSpellHealingPercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModHealingDone)]
        private void HandleModHealingDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;
            // implemented in Unit.SpellHealingBonus
            // this information is for client side only
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModHealingDonePercent)]
        private void HandleModHealingDonePct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (player)
                player.UpdateHealingDonePercentMod();
        }

        [AuraEffectHandler(AuraType.ModTotalStatPercentage)]
        private void HandleModTotalPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            // save current health state
            float healthPct = target.GetHealthPct();
            var zeroHealth = !target.IsAlive();

            // players in corpse state may mean two different states:
            /// 1. player just died but did not release (in this case health == 0)
            /// 2. player is corpse running (ie ghost) (in this case health == 1)
            if (target.GetDeathState() == DeathState.Corpse)
                zeroHealth = target.GetHealth() == 0;

            for (var i = (int)Stats.Strength; i < (int)Stats.Max; i++)
            {
                if (Convert.ToBoolean(GetMiscValueB() & 1 << i) || GetMiscValueB() == 0) // 0 is also used for all stats
                {
                    var amount = target.GetTotalAuraMultiplier(AuraType.ModTotalStatPercentage, aurEff =>
                    {
                        if ((aurEff.GetMiscValueB() & 1 << i) != 0 || aurEff.GetMiscValueB() == 0)
                            return true;
                        return false;
                    });

                    if (target.GetPctModifierValue(UnitMods.StatStart + i, UnitModifierPctType.Total) == amount)
                        continue;

                    target.SetStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Total, amount);
                    if (target.IsTypeId(TypeId.Player) || target.IsPet())
                        target.UpdateStatBuffMod((Stats)i);
                }
            }

            // recalculate current HP/MP after applying aura modifications (only for spells with SPELL_ATTR0_ABILITY 0x00000010 flag)
            // this check is total bullshit i think
            if ((Convert.ToBoolean(GetMiscValueB() & 1 << (int)Stats.Stamina) || GetMiscValueB() == 0) && m_spellInfo.HasAttribute(SpellAttr0.Ability))
                target.SetHealth(Math.Max(MathFunctions.CalculatePct(target.GetMaxHealth(), healthPct), (zeroHealth ? 0 : 1ul)));
        }

        [AuraEffectHandler(AuraType.ModExpertise)]
        private void HandleAuraModExpertise(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateExpertise(WeaponAttackType.BaseAttack);
            target.ToPlayer().UpdateExpertise(WeaponAttackType.OffAttack);
        }

        [AuraEffectHandler(AuraType.ModStatBonusPct)]
        private void HandleModStatBonusPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            if (GetMiscValue() < -1 || GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Misc Value for SPELL_AURA_MOD_STAT_BONUS_PCT not valid");
                return;
            }

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
                return;

            for (var stat = Stats.Strength; stat < Stats.Max; ++stat)
            {
                if (GetMiscValue() == (int)stat || GetMiscValue() == -1)
                {
                    target.HandleStatFlatModifier(UnitMods.StatStart + (int)stat, UnitModifierFlatType.BasePCTExcludeCreate, m_amount, apply);
                    target.UpdateStatBuffMod(stat);
                }
            }
        }

        [AuraEffectHandler(AuraType.OverrideSpellPowerByApPct)]
        private void HandleOverrideSpellPowerByAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            target.ApplyModOverrideSpellPowerByAPPercent(m_amount, apply);
            target.UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.OverrideAttackPowerBySpPct)]
        private void HandleOverrideAttackPowerBySpellPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            target.ApplyModOverrideAPBySpellPowerPercent(m_amount, apply);
            target.UpdateAttackPowerAndDamage();
            target.UpdateAttackPowerAndDamage(true);
        }

        [AuraEffectHandler(AuraType.ModVersatility)]
        private void HandleModVersatilityByPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target)
            {
                target.SetVersatilityBonus(target.GetTotalAuraModifier(AuraType.ModVersatility));
                target.UpdateHealingDonePercentMod();
                target.UpdateVersatilityDamageDone();
            }
        }

        [AuraEffectHandler(AuraType.ModMaxPower)]
        private void HandleAuraModMaxPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            var power = (PowerType)GetMiscValue();
            var unitMod = (UnitMods)(UnitMods.PowerStart + (int)power);

            target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        /********************************/
        /***      HEAL & ENERGIZE     ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerRegen)]
        private void HandleModPowerRegen(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Update manaregen value
            if (GetMiscValue() == (int)PowerType.Mana)
                target.ToPlayer().UpdateManaRegen();
            else if (GetMiscValue() == (int)PowerType.Runes)
                target.ToPlayer().UpdateAllRunesRegen();
            // other powers are not immediate effects - implemented in Player.Regenerate, Creature.Regenerate
        }

        [AuraEffectHandler(AuraType.ModPowerRegenPercent)]
        private void HandleModPowerRegenPCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleModPowerRegen(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModManaRegenPct)]
        private void HandleModManaRegenPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModIncreaseHealth)]
        [AuraEffectHandler(AuraType.ModIncreaseHealth2)]
        [AuraEffectHandler(AuraType.ModMaxHealth)]
        private void HandleAuraModIncreaseHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);
                target.ModifyHealth(GetAmount());
            }
            else
            {
                if (target.GetHealth() > 0)
                {
                    var value = (int)Math.Min(target.GetHealth() - 1, (ulong)GetAmount());
                    target.ModifyHealth(-value);
                }

                target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);
            }
        }

        private void HandleAuraModIncreaseMaxHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            float percent = target.GetHealthPct();

            target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);

            // refresh percentage
            if (target.GetHealth() > 0)
            {
                var newHealth = (uint)Math.Max(target.CountPctFromMaxHealth((int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModIncreaseEnergy)]
        private void HandleAuraModIncreaseEnergy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();
            var powerType = (PowerType)GetMiscValue();

            var unitMod = (UnitMods.PowerStart + (int)powerType);
            target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModIncreaseEnergyPercent)]
        private void HandleAuraModIncreaseEnergyPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();
            var powerType = (PowerType)GetMiscValue();

            var unitMod = UnitMods.PowerStart + (int)powerType;

            // Save old powers for further calculation
            int oldPower = target.GetPower(powerType);
            int oldMaxPower = target.GetMaxPower(powerType);

            // Handle aura effect for max power
            if (apply)
                target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent, aurEff =>
                    {
                        if (aurEff.GetMiscValue() == (int)powerType)
                            return true;
                        return false;
                    });

                amount *= target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct, aurEff =>
                    {
                        if (aurEff.GetMiscValue() == (int)powerType)
                            return true;
                        return false;
                    });

                target.SetStatPctModifier(unitMod, UnitModifierPctType.Total, amount);
            }

            // Calculate the current power change
            var change = target.GetMaxPower(powerType) - oldMaxPower;
            change = (oldPower + change) - target.GetPower(powerType);
        }

        [AuraEffectHandler(AuraType.ModIncreaseHealthPercent)]
        private void HandleAuraModIncreaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            // Unit will keep hp% after MaxHealth being modified if unit is alive.
            float percent = target.GetHealthPct();
            if (apply)
                target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent) * target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent2);
                target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, amount);
            }

            if (target.GetHealth() > 0)
            {
                var newHealth = (uint)Math.Max(MathFunctions.CalculatePct(target.GetMaxHealth(), (int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseHealthPct)]
        private void HandleAuraIncreaseBaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (apply)
                target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModBaseHealthPct);
                target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseManaPct)]
        private void HandleAuraModIncreaseBaseManaPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            if (apply)
                target.ApplyStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModBaseManaPct);
                target.SetStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModManaCostPct)]
        private void HandleModManaCostPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().ApplyModManaCostMultiplier(GetAmount() / 100.0f, apply);
        }
        
        [AuraEffectHandler(AuraType.ModPowerDisplay)]
        private void HandleAuraModPowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.RealOrReapplyMask))
                return;

            if (GetMiscValue() >= (int)PowerType.Max)
                return;

            if (apply)
                aurApp.GetTarget().RemoveAurasByType(GetAuraType(), ObjectGuid.Empty, GetBase());

            aurApp.GetTarget().UpdateDisplayPower();
        }

        [AuraEffectHandler(AuraType.ModOverridePowerDisplay)]
        private void HandleAuraModOverridePowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var powerDisplay = CliDB.PowerDisplayStorage.LookupByKey(GetMiscValue());
            if (powerDisplay == null)
                return;

            var target = aurApp.GetTarget();
            if (target.GetPowerIndex((PowerType)powerDisplay.ActualType) == (int)PowerType.Max)
                return;

            if (apply)
            {
                target.RemoveAurasByType(GetAuraType(), ObjectGuid.Empty, GetBase());
                target.SetOverrideDisplayPowerId(powerDisplay.Id);
            }
            else
                target.SetOverrideDisplayPowerId(0);
        }

        [AuraEffectHandler(AuraType.ModMaxPowerPct)]
        private void HandleAuraModMaxPowerPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();
            if (!target.IsPlayer())
                return;

            var powerType = (PowerType)GetMiscValue();
            var unitMod = UnitMods.PowerStart + (int)powerType;

            // Save old powers for further calculation
            int oldPower = target.GetPower(powerType);
            int oldMaxPower = target.GetMaxPower(powerType);

            // Handle aura effect for max power
            if (apply)
                target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct, aurEff =>
                {
                    if (aurEff.GetMiscValue() == (int)powerType)
                        return true;
                    return false;
                });

                amount *= target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent, aurEff =>
                {
                    if (aurEff.GetMiscValue() == (int)powerType)
                        return true;
                    return false;
                });

                target.SetStatPctModifier(unitMod, UnitModifierPctType.Total, amount);
            }

            // Calculate the current power change
            var change = target.GetMaxPower(powerType) - oldMaxPower;
            change = (oldPower + change) - target.GetPower(powerType);
            target.ModifyPower(powerType, change);
        }

        /********************************/
        /***          FIGHT           ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModParryPercent)]
        private void HandleAuraModParryPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateParryPercentage();
        }

        [AuraEffectHandler(AuraType.ModDodgePercent)]
        private void HandleAuraModDodgePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateDodgePercentage();
        }

        [AuraEffectHandler(AuraType.ModBlockPercent)]
        private void HandleAuraModBlockPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateBlockPercentage();
        }

        [AuraEffectHandler(AuraType.InterruptRegen)]
        private void HandleAuraModRegenInterrupt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModWeaponCritPercent)]
        private void HandleAuraModWeaponCritPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            target.UpdateAllWeaponDependentCritAuras();
        }

        [AuraEffectHandler(AuraType.ModHitChance)]
        private void HandleModHitChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
            {
                target.ToPlayer().UpdateMeleeHitChances();
                target.ToPlayer().UpdateRangedHitChances();
            }
            else
            {
                target.ModMeleeHitChance += (apply) ? GetAmount() : (-GetAmount());
                target.ModRangedHitChance += (apply) ? GetAmount() : (-GetAmount());
            }
        }

        [AuraEffectHandler(AuraType.ModSpellHitChance)]
        private void HandleModSpellHitChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateSpellHitChances();
            else
                target.ModSpellHitChance += (apply) ? GetAmount() : (-GetAmount());
        }

        [AuraEffectHandler(AuraType.ModSpellCritChance)]
        private void HandleModSpellCritChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateSpellCritChance();
            else
                target.BaseSpellCritChance += (apply) ? GetAmount() : -GetAmount();
        }

        [AuraEffectHandler(AuraType.ModCritPct)]
        private void HandleAuraModCritPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
            {
                target.BaseSpellCritChance += (apply) ? GetAmount() : -GetAmount();
                return;
            }

            target.ToPlayer().UpdateAllWeaponDependentCritAuras();

            // included in Player.UpdateSpellCritChance calculation
            target.ToPlayer().UpdateSpellCritChance();
        }

        /********************************/
        /***         ATTACK SPEED     ***/
        /********************************/
        [AuraEffectHandler(AuraType.HasteSpells)]
        [AuraEffectHandler(AuraType.ModCastingSpeedNotStack)]
        private void HandleModCastingSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            // Do not apply such auras in normal way
            if (GetAmount() >= 1000)
            {
                if (apply)
                    target.SetInstantCast(true);
                else
                {
                    // only SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK can have this high amount
                    // it's some rare case that you have 2 auras like that, but just in case ;)

                    var remove = true;
                    var castingSpeedNotStack = target.GetAuraEffectsByType(AuraType.ModCastingSpeedNotStack);
                    foreach (var aurEff in castingSpeedNotStack)
                    {
                        if (aurEff != this && aurEff.GetAmount() >= 1000)
                        {
                            remove = false;
                            break;
                        }
                    }

                    if (remove)
                        target.SetInstantCast(false);
                }

                return;
            }

            target.ApplyCastTimePercentMod(GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModMeleeRangedHaste)]
        [AuraEffectHandler(AuraType.ModMeleeRangedHaste2)]
        private void HandleModMeleeRangedSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            //! ToDo: Haste auras with the same handler _CAN'T_ stack together
            var target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.MeleeSlow)]
        [AuraEffectHandler(AuraType.ModSpeedSlowAll)]
        private void HandleModCombatSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();
            var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.MeleeSlow);
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
            {
                target.ApplyCastTimePercentMod(spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, spellGroupVal, !apply);
            }

            target.ApplyCastTimePercentMod(m_amount, apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackspeed)]
        private void HandleModAttackSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.UpdateDamagePhysical(WeaponAttackType.BaseAttack);
        }

        [AuraEffectHandler(AuraType.ModMeleeHaste)]
        [AuraEffectHandler(AuraType.ModMeleeHaste2)]
        [AuraEffectHandler(AuraType.ModMeleeHaste3)]
        private void HandleModMeleeSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            //! ToDo: Haste auras with the same handler _CAN'T_ stack together
            var target = aurApp.GetTarget();
            var spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModMeleeHaste);
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
            {
                target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, spellGroupVal, !apply);
                target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, spellGroupVal, !apply);
            }
            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedHaste)]
        [AuraEffectHandler(AuraType.ModRangedHaste2)]
        private void HandleAuraModRangedHaste(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            //! ToDo: Haste auras with the same handler _CAN'T_ stack together
            var target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        /********************************/
        /***       COMBAT RATING      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModRating)]
        private void HandleModRating(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            for (var rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & (1 << rating)))
                    target.ToPlayer().ApplyRatingMod((CombatRating)rating, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRatingPct)]
        private void HandleModRatingPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Just recalculate ratings
            for (var rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & (1 << rating)))
                    target.ToPlayer().UpdateRating((CombatRating)rating);
        }

        /********************************/
        /***        ATTACK POWER      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModAttackPower)]
        private void HandleAuraModAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            target.HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPower)]
        private void HandleAuraModRangedAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if ((target.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            target.HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackPowerPct)]
        private void HandleAuraModAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            //UNIT_FIELD_ATTACK_POWER_MULTIPLIER = multiplier - 1
            if (apply)
                target.ApplyStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModAttackPowerPct);
                target.SetStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPowerPct)]
        private void HandleAuraModRangedAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if ((target.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            //UNIT_FIELD_RANGED_ATTACK_POWER_MULTIPLIER = multiplier - 1
            if (apply)
                target.ApplyStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModRangedAttackPowerPct);
                target.SetStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, amount);
            }
        }

        /********************************/
        /***        DAMAGE BONUS      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModDamageDone)]
        private void HandleModDamageDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            if ((GetMiscValue() & (int)SpellSchoolMask.Normal) != 0)
                target.UpdateAllDamageDoneMods();

            // Magic damage modifiers implemented in Unit::SpellBaseDamageBonusDone
            // This information for client side use only
            var playerTarget = target.ToPlayer();
            if (playerTarget != null)
            {
                for (var i = 0; i < (int)SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    {
                        if (GetAmount() >= 0)
                            playerTarget.ApplyModDamageDonePos((SpellSchools)i, GetAmount(), apply);
                        else
                            playerTarget.ApplyModDamageDoneNeg((SpellSchools)i, GetAmount(), apply);
                    }
                }

                var pet = playerTarget.GetGuardianPet();
                if (pet)
                    pet.UpdateAttackPowerAndDamage();
            }
        }

        [AuraEffectHandler(AuraType.ModDamagePercentDone)]
        private void HandleModDamagePercentDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            // also handles spell group stacks
            if (Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                target.UpdateAllDamagePctDoneMods();

            var thisPlayer = target.ToPlayer();
            if (thisPlayer != null)
            {
                for (var i = SpellSchools.Normal; i < SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << (int)i)))
                    {
                        // only aura type modifying PLAYER_FIELD_MOD_DAMAGE_DONE_PCT
                        var amount = thisPlayer.GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, 1u << (int)i);
                        thisPlayer.SetModDamageDonePercent(i, amount);
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModOffhandDamagePct)]
        private void HandleModOffhandDamagePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget();

            // also handles spell group stacks
            target.UpdateDamagePctDoneMods(WeaponAttackType.OffAttack);
        }

        private void HandleShieldBlockValue(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (player != null)
                player.HandleBaseModFlatValue(BaseModGroup.ShieldBlockValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModShieldBlockvaluePct)]
        private void HandleShieldBlockValuePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            if (apply)
                target.ApplyBaseModPctValue(BaseModGroup.ShieldBlockValue, GetAmount());
            else
            {
                var amount = target.GetTotalAuraMultiplier(AuraType.ModShieldBlockvaluePct);
                target.SetBaseModPctValue(BaseModGroup.ShieldBlockValue, amount);
            }
        }

        /********************************/
        /***        POWER COST        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerCostSchool)]
        private void HandleModPowerCost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            // handled in SpellInfo::CalcPowerCost, this is only for client UI
            if ((GetMiscValueB() & (1 << (int)PowerType.Mana)) == 0)
                return;

            var target = aurApp.GetTarget();

            for (var i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    target.ApplyModManaCostModifier((SpellSchools)i, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ArenaPreparation)]
        private void HandleArenaPreparation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
                target.AddUnitFlag(UnitFlags.Preparation);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveUnitFlag(UnitFlags.Preparation);
            }
        }

        [AuraEffectHandler(AuraType.NoReagentUse)]
        private void HandleNoReagentUseAura(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            var mask = new FlagArray128();
            var noReagent = target.GetAuraEffectsByType(AuraType.NoReagentUse);
            foreach (var eff in noReagent)
            {
                var effect = eff.GetSpellEffectInfo();
                if (effect != null)
                    mask |= effect.SpellClassMask;
            }

            target.ToPlayer().SetNoRegentCostMask(mask);
        }

        /*********************************************************/
        /***                    OTHERS                         ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.Dummy)]
        private void HandleAuraDummy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Reapply)))
                return;

            var target = aurApp.GetTarget();

            var caster = GetCaster();

            // pet auras
            if (target.GetTypeId() == TypeId.Player && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                var petSpell = Global.SpellMgr.GetPetAura(GetId(), m_effIndex);
                if (petSpell != null)
                {
                    if (apply)
                        target.ToPlayer().AddPetAura(petSpell);
                    else
                        target.ToPlayer().RemovePetAura(petSpell);
                }
            }

            if (mode.HasAnyFlag(AuraEffectHandleModes.Real | AuraEffectHandleModes.Reapply))
            {
                // AT APPLY
                if (apply)
                {
                    switch (GetId())
                    {
                        case 1515:                                      // Tame beast
                            // FIX_ME: this is 2.0.12 threat effect replaced in 2.1.x by dummy aura, must be checked for correctness
                            if (caster != null && target.CanHaveThreatList())
                                target.GetThreatManager().AddThreat(caster, 10.0f);
                            break;
                        case 13139:                                     // net-o-matic
                            // root to self part of (root_target.charge.root_self sequence
                            if (caster != null)
                                caster.CastSpell(caster, 13138, true, null, this);
                            break;
                        case 34026:   // kill command
                            {
                                Unit pet = target.GetGuardianPet();
                                if (pet == null)
                                    break;

                                target.CastSpell(target, 34027, true, null, this);

                                // set 3 stacks and 3 charges (to make all auras not disappear at once)
                                var owner_aura = target.GetAura(34027, GetCasterGUID());
                                var pet_aura = pet.GetAura(58914, GetCasterGUID());
                                if (owner_aura != null)
                                {
                                    owner_aura.SetStackAmount((byte)owner_aura.GetSpellInfo().StackAmount);
                                    if (pet_aura != null)
                                    {
                                        pet_aura.SetCharges(0);
                                        pet_aura.SetStackAmount((byte)owner_aura.GetSpellInfo().StackAmount);
                                    }
                                }
                                break;
                            }
                        case 37096:                                     // Blood Elf Illusion
                            {
                                if (caster != null)
                                {
                                    if (caster.GetGender() == Gender.Female)
                                        caster.CastSpell(target, 37095, true, null, this); // Blood Elf Disguise
                                    else
                                        caster.CastSpell(target, 37093, true, null, this);
                                }
                                break;
                            }
                        case 39850:                                     // Rocket Blast
                            if (RandomHelper.randChance(20))                       // backfire stun
                                target.CastSpell(target, 51581, true, null, this);
                            break;
                        case 43873:                                     // Headless Horseman Laugh
                            target.PlayDistanceSound(11965);
                            break;
                        case 46354:                                     // Blood Elf Illusion
                            if (caster != null)
                            {
                                if (caster.GetGender() == Gender.Female)
                                    caster.CastSpell(target, 46356, true, null, this);
                                else
                                    caster.CastSpell(target, 46355, true, null, this);
                            }
                            break;
                        case 46361:                                     // Reinforced Net
                            if (caster != null)
                                target.GetMotionMaster().MoveFall();
                            break;
                    }
                }
                // AT REMOVE
                else
                {
                    if ((GetSpellInfo().IsQuestTame()) && caster != null && caster.IsAlive() && target.IsAlive())
                    {
                        uint finalSpelId = 0;
                        switch (GetId())
                        {
                            case 19548:
                                finalSpelId = 19597;
                                break;
                            case 19674:
                                finalSpelId = 19677;
                                break;
                            case 19687:
                                finalSpelId = 19676;
                                break;
                            case 19688:
                                finalSpelId = 19678;
                                break;
                            case 19689:
                                finalSpelId = 19679;
                                break;
                            case 19692:
                                finalSpelId = 19680;
                                break;
                            case 19693:
                                finalSpelId = 19684;
                                break;
                            case 19694:
                                finalSpelId = 19681;
                                break;
                            case 19696:
                                finalSpelId = 19682;
                                break;
                            case 19697:
                                finalSpelId = 19683;
                                break;
                            case 19699:
                                finalSpelId = 19685;
                                break;
                            case 19700:
                                finalSpelId = 19686;
                                break;
                            case 30646:
                                finalSpelId = 30647;
                                break;
                            case 30653:
                                finalSpelId = 30648;
                                break;
                            case 30654:
                                finalSpelId = 30652;
                                break;
                            case 30099:
                                finalSpelId = 30100;
                                break;
                            case 30102:
                                finalSpelId = 30103;
                                break;
                            case 30105:
                                finalSpelId = 30104;
                                break;
                        }

                        if (finalSpelId != 0)
                            caster.CastSpell(target, finalSpelId, true, null, this);
                    }

                    switch (m_spellInfo.SpellFamilyName)
                    {
                        case SpellFamilyNames.Generic:
                            switch (GetId())
                            {
                                case 2584: // Waiting to Resurrect
                                    // Waiting to resurrect spell cancel, we must remove player from resurrect queue
                                    if (target.IsTypeId(TypeId.Player))
                                    {
                                        Battleground bg = target.ToPlayer().GetBattleground();
                                        if (bg)
                                            bg.RemovePlayerFromResurrectQueue(target.GetGUID());
                                        var bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(target.GetZoneId());
                                        if (bf != null)
                                            bf.RemovePlayerFromResurrectQueue(target.GetGUID());
                                    }
                                    break;
                                case 36730:                                     // Flame Strike
                                    {
                                        target.CastSpell(target, 36731, true, null, this);
                                        break;
                                    }
                                case 44191:                                     // Flame Strike
                                    {
                                        if (target.GetMap().IsDungeon())
                                        {
                                            var spellId = (uint)(target.GetMap().IsHeroic() ? 46163 : 44190);

                                            target.CastSpell(target, spellId, true, null, this);
                                        }
                                        break;
                                    }
                                case 43681: // Inactive
                                    {
                                        if (!target.IsTypeId(TypeId.Player) || aurApp.GetRemoveMode() != AuraRemoveMode.Expire)
                                            return;

                                        if (target.GetMap().IsBattleground())
                                            target.ToPlayer().LeaveBattleground();
                                        break;
                                    }
                                case 42783: // Wrath of the Astromancer
                                    target.CastSpell(target, (uint)GetAmount(), true, null, this);
                                    break;
                                case 46308: // Burning Winds casted only at creatures at spawn
                                    target.CastSpell(target, 47287, true, null, this);
                                    break;
                                case 52172:  // Coyote Spirit Despawn Aura
                                case 60244:  // Blood Parrot Despawn Aura
                                    target.CastSpell((Unit)null, (uint)GetAmount(), true, null, this);
                                    break;
                                case 91604: // Restricted Flight Area
                                    if (aurApp.GetRemoveMode() == AuraRemoveMode.Expire)
                                        target.CastSpell(target, 58601, true);
                                    break;
                            }
                            break;
                        case SpellFamilyNames.Deathknight:
                            // Summon Gargoyle (Dismiss Gargoyle at remove)
                            if (GetId() == 61777)
                                target.CastSpell(target, (uint)GetAmount(), true);
                            break;
                        default:
                            break;
                    }
                }
            }

            // AT APPLY & REMOVE

            switch (m_spellInfo.SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    {
                        if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                            break;
                        switch (GetId())
                        {
                            // Recently Bandaged
                            case 11196:
                                target.ApplySpellImmune(GetId(), SpellImmunity.Mechanic, (uint)GetMiscValue(), apply);
                                break;
                            // Unstable Power
                            case 24658:
                                {
                                    uint spellId = 24659;
                                    if (apply && caster != null)
                                    {
                                        var spell = Global.SpellMgr.GetSpellInfo(spellId, GetBase().GetCastDifficulty());

                                        for (uint i = 0; i < spell.StackAmount; ++i)
                                            caster.CastSpell(target, spell, true, null, null, GetCasterGUID());
                                        break;
                                    }
                                    target.RemoveAurasDueToSpell(spellId);
                                    break;
                                }
                            // Restless Strength
                            case 24661:
                                {
                                    uint spellId = 24662;
                                    if (apply && caster != null)
                                    {
                                        var spell = Global.SpellMgr.GetSpellInfo(spellId, GetBase().GetCastDifficulty());
                                        for (uint i = 0; i < spell.StackAmount; ++i)
                                            caster.CastSpell(target, spell, true, null, null, GetCasterGUID());
                                        break;
                                    }
                                    target.RemoveAurasDueToSpell(spellId);
                                    break;
                                }
                            // Tag Murloc
                            case 30877:
                                {
                                    // Tag/untag Blacksilt Scout
                                    target.SetEntry((uint)(apply ? 17654 : 17326));
                                    break;
                                }
                            case 57819: // Argent Champion
                            case 57820: // Ebon Champion
                            case 57821: // Champion of the Kirin Tor
                            case 57822: // Wyrmrest Champion
                                {
                                    if (!caster || !caster.IsTypeId(TypeId.Player))
                                        break;

                                    uint FactionID = 0;

                                    if (apply)
                                    {
                                        switch (m_spellInfo.Id)
                                        {
                                            case 57819:
                                                FactionID = 1106; // Argent Crusade
                                                break;
                                            case 57820:
                                                FactionID = 1098;// Knights of the Ebon Blade
                                                break;
                                            case 57821:
                                                FactionID = 1090; // Kirin Tor
                                                break;
                                            case 57822:
                                                FactionID = 1091; // The Wyrmrest Accord
                                                break;
                                        }
                                    }
                                    caster.ToPlayer().SetChampioningFaction(FactionID);
                                    break;
                                }
                            // LK Intro VO (1)
                            case 58204:
                                if (target.IsTypeId(TypeId.Player))
                                {
                                    // Play part 1
                                    if (apply)
                                        target.PlayDirectSound(14970, target.ToPlayer());
                                    // continue in 58205
                                    else
                                        target.CastSpell(target, 58205, true);
                                }
                                break;
                            // LK Intro VO (2)
                            case 58205:
                                if (target.IsTypeId(TypeId.Player))
                                {
                                    // Play part 2
                                    if (apply)
                                        target.PlayDirectSound(14971, target.ToPlayer());
                                    // Play part 3
                                    else
                                        target.PlayDirectSound(14972, target.ToPlayer());
                                }
                                break;
                        }
                        break;
                    }
            }
        }

        [AuraEffectHandler(AuraType.ChannelDeathItem)]
        private void HandleChannelDeathItem(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (apply || aurApp.GetRemoveMode() != AuraRemoveMode.Death)
                return;

            var caster = GetCaster();

            if (caster == null || !caster.IsTypeId(TypeId.Player))
                return;

            var plCaster = caster.ToPlayer();
            var target = aurApp.GetTarget();

            // Item amount
            if (GetAmount() <= 0)
                return;

            if (GetSpellEffectInfo().ItemType == 0)
                return;

            // Soul Shard
            if (GetSpellEffectInfo().ItemType == 6265)
            {
                // Soul Shard only from units that grant XP or honor
                if (!plCaster.IsHonorOrXPTarget(target) ||
                    (target.IsTypeId(TypeId.Unit) && !target.ToCreature().IsTappedBy(plCaster)))
                    return;
            }

            //Adding items
            uint noSpaceForCount;
            var count = (uint)m_amount;

            var dest = new List<ItemPosCount>();
            InventoryResult msg = plCaster.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, GetSpellEffectInfo().ItemType, count, out noSpaceForCount);
            if (msg != InventoryResult.Ok)
            {
                count -= noSpaceForCount;
                plCaster.SendEquipError(msg, null, null, GetSpellEffectInfo().ItemType);
                if (count == 0)
                    return;
            }

            Item newitem = plCaster.StoreNewItem(dest, GetSpellEffectInfo().ItemType, true);
            if (newitem == null)
            {
                plCaster.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }
            plCaster.SendNewItem(newitem, count, true, true);
        }

        [AuraEffectHandler(AuraType.BindSight)]
        private void HandleBindSight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            var caster = GetCaster();

            if (caster == null || !caster.IsTypeId(TypeId.Player))
                return;

            caster.ToPlayer().SetViewpoint(target, apply);
        }

        [AuraEffectHandler(AuraType.ForceReaction)]
        private void HandleForceReaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();

            var player = target.ToPlayer();
            if (player == null)
                return;

            var factionId = (uint)GetMiscValue();
            var factionRank = (ReputationRank)m_amount;

            player.GetReputationMgr().ApplyForceReaction(factionId, factionRank, apply);
            player.GetReputationMgr().SendForceReactions();

            // stop fighting if at apply forced rank friendly or at remove real rank friendly
            if ((apply && factionRank >= ReputationRank.Friendly) || (!apply && player.GetReputationRank(factionId) >= ReputationRank.Friendly))
                player.StopAttackFaction(factionId);
        }

        [AuraEffectHandler(AuraType.Empathy)]
        private void HandleAuraEmpathy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            if (target.GetCreatureType() == CreatureType.Beast)
            {
                if (apply)
                    target.AddDynamicFlag(UnitDynFlags.SpecialInfo);
                else
                    target.RemoveDynamicFlag(UnitDynFlags.SpecialInfo);
            }
        }

        [AuraEffectHandler(AuraType.ModFaction)]
        private void HandleAuraModFaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                target.SetFaction((uint)GetMiscValue());
                if (target.IsTypeId(TypeId.Player))
                    target.RemoveUnitFlag(UnitFlags.PvpAttackable);
            }
            else
            {
                target.RestoreFaction();
                if (target.IsTypeId(TypeId.Player))
                    target.AddUnitFlag(UnitFlags.PvpAttackable);
            }
        }

        [AuraEffectHandler(AuraType.LearnSpell)]
        private void HandleLearnSpell(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (player == null)
                return;

            if (apply)
                player.LearnSpell((uint)GetMiscValue(), true, 0, true);
            else
                player.RemoveSpell((uint)GetMiscValue(), false, false, true);
        }
        
        [AuraEffectHandler(AuraType.ComprehendLanguage)]
        private void HandleComprehendLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
                target.AddUnitFlag2(UnitFlags2.ComprehendLang);
            else
            {
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag2(UnitFlags2.ComprehendLang);
            }
        }

        [AuraEffectHandler(AuraType.Linked)]
        private void HandleAuraLinked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            var target = aurApp.GetTarget();

            var triggeredSpellId = GetSpellEffectInfo().TriggerSpell;
            var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggeredSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo == null)
                return;

            var caster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? GetCaster() : target;
            if (!caster)
                return;

            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                if (apply)
                {
                    // If amount avalible cast with basepoints (Crypt Fever for example)
                    if (GetAmount() != 0)
                        caster.CastCustomSpell(target, triggeredSpellId, m_amount, 0, 0, true, null, this);
                    else
                        caster.CastSpell(target, triggeredSpellId, true, null, this);
                }
                else
                {
                    var casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? GetCasterGUID() : target.GetGUID();
                    target.RemoveAura(triggeredSpellId, casterGUID, 0, aurApp.GetRemoveMode());
                }
            }
            else if (mode.HasAnyFlag(AuraEffectHandleModes.Reapply) && apply)
            {
                var casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? GetCasterGUID() : target.GetGUID();
                // change the stack amount to be equal to stack amount of our aura
                var triggeredAura = target.GetAura(triggeredSpellId, casterGUID);
                if (triggeredAura != null)
                    triggeredAura.ModStackAmount(GetBase().GetStackAmount() - triggeredAura.GetStackAmount());
            }
        }

        [AuraEffectHandler(AuraType.OpenStable)]
        private void HandleAuraOpenStable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player) || !target.IsInWorld)
                return;

            if (apply)
                target.ToPlayer().GetSession().SendStablePet(target.GetGUID());

            // client auto close stable dialog at !apply aura
        }

        [AuraEffectHandler(AuraType.ModFakeInebriate)]
        private void HandleAuraModFakeInebriation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                target.m_invisibilityDetect.AddFlag(InvisibilityType.Drunk);
                target.m_invisibilityDetect.AddValue(InvisibilityType.Drunk, GetAmount());

                var playerTarget = target.ToPlayer();
                if (playerTarget)
                    playerTarget.ApplyModFakeInebriation(GetAmount(), true);
            }
            else
            {
                var removeDetect = !target.HasAuraType(AuraType.ModFakeInebriate);

                target.m_invisibilityDetect.AddValue(InvisibilityType.Drunk, -GetAmount());

                var playerTarget = target.ToPlayer();
                if (playerTarget != null)
                {
                    playerTarget.ApplyModFakeInebriation(GetAmount(), false);

                    if (removeDetect)
                        removeDetect = playerTarget.GetDrunkValue() == 0;
                }

                if (removeDetect)
                    target.m_invisibilityDetect.DelFlag(InvisibilityType.Drunk);
            }

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.OverrideSpells)]
        private void HandleAuraOverrideSpells(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();

            if (target == null || !target.IsInWorld)
                return;

            var overrideId = (uint)GetMiscValue();

            if (apply)
            {
                target.SetOverrideSpellsId(overrideId);
                var overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);
                if (overrideSpells != null)
                {
                    for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                    {
                        var spellId = overrideSpells.Spells[i];
                        if (spellId != 0)
                            target.AddTemporarySpell(spellId);
                    }
                }
            }
            else
            {
                target.SetOverrideSpellsId(0);
                var overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);
                if (overrideSpells != null)
                {
                    for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                    {
                        var spellId = overrideSpells.Spells[i];
                        if (spellId != 0)
                            target.RemoveTemporarySpell(spellId);
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.SetVehicleId)]
        private void HandleAuraSetVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (!target.IsInWorld)
                return;

            var vehicleId = GetMiscValue();

            if (apply)
            {
                if (!target.CreateVehicleKit((uint)vehicleId, 0))
                    return;
            }
            else if (target.GetVehicleKit() != null)
                target.RemoveVehicleKit();

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (apply)
                target.ToPlayer().SendOnCancelExpectedVehicleRideAura();
        }

        [AuraEffectHandler(AuraType.PreventResurrection)]
        private void HandlePreventResurrection(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
            else if (!target.GetMap().Instanceable())
                target.AddPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
        }

        [AuraEffectHandler(AuraType.Mastery)]
        private void HandleMastery(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            target.UpdateMastery();
        }

        private void HandlePeriodicDummyAuraTick(Unit target, Unit caster)
        {
            switch (GetSpellInfo().SpellFamilyName)
            {
                case SpellFamilyNames.Generic:
                    switch (GetId())
                    {
                        case 66149: // Bullet Controller Periodic - 10 Man
                        case 68396: // Bullet Controller Periodic - 25 Man
                            {
                                if (caster == null)
                                    break;

                                caster.CastCustomSpell(66152, SpellValueMod.MaxTargets, RandomHelper.IRand(1, 6), target, true);
                                caster.CastCustomSpell(66153, SpellValueMod.MaxTargets, RandomHelper.IRand(1, 6), target, true);
                                break;
                            }
                        case 62292: // Blaze (Pool of Tar)
                            // should we use custom damage?
                            target.CastSpell((Unit)null, GetSpellEffectInfo().TriggerSpell, true);
                            break;
                        case 62399: // Overload Circuit
                            if (target.GetMap().IsDungeon() && target.GetAppliedAuras().Count(p => p.Key == 62399) >= (target.GetMap().IsHeroic() ? 4 : 2))
                            {
                                target.CastSpell(target, 62475, true); // System Shutdown
                                var veh = target.GetVehicleBase();
                                if (veh != null)
                                    veh.CastSpell(target, 62475, true);
                            }
                            break;
                        case 64821: // Fuse Armor (Razorscale)
                            if (GetBase().GetStackAmount() == GetSpellInfo().StackAmount)
                            {
                                target.CastSpell(target, 64774, true, null, null, GetCasterGUID());
                                target.RemoveAura(64821);
                            }
                            break;
                    }
                    break;
                case SpellFamilyNames.Mage:
                    {
                        // Mirror Image
                        if (GetId() == 55342)
                            // Set name of summons to name of caster
                            target.CastSpell((Unit)null, GetSpellEffectInfo().TriggerSpell, true);
                        break;
                    }
                case SpellFamilyNames.Druid:
                    {
                        switch (GetSpellInfo().Id)
                        {
                            // Frenzied Regeneration
                            case 22842:
                                {
                                    // Converts up to 10 rage per second into health for $d.  Each point of rage is converted into ${$m2/10}.1% of max health.
                                    // Should be manauser
                                    if (target.GetPowerType() != PowerType.Rage)
                                        break;
                                    int rage = target.GetPower(PowerType.Rage);
                                    // Nothing todo
                                    if (rage == 0)
                                        break;
                                    var mod = (rage < 100) ? rage : 100;
                                    var points = target.CalculateSpellDamage(target, GetSpellInfo(), 1);
                                    var regen = (int)((long)target.GetMaxHealth() * (mod * points / 10) / 1000);
                                    target.CastCustomSpell(target, 22845, regen, 0, 0, true, null, this);
                                    target.SetPower(PowerType.Rage, (rage - mod));
                                    break;
                                }
                        }
                        break;
                    }
                case SpellFamilyNames.Rogue:
                    {
                        switch (GetSpellInfo().Id)
                        {
                            // Master of Subtlety
                            case 31666:
                                if (!target.HasAuraType(AuraType.ModStealth))
                                    target.RemoveAurasDueToSpell(31665);
                                break;
                            // Overkill
                            case 58428:
                                if (!target.HasAuraType(AuraType.ModStealth))
                                    target.RemoveAurasDueToSpell(58427);
                                break;
                        }
                        break;
                    }
                case SpellFamilyNames.Hunter:
                    {
                        // Explosive Shot
                        if (GetSpellInfo().SpellFamilyFlags[1].HasAnyFlag(0x80000000))
                        {
                            if (caster != null)
                                caster.CastCustomSpell(53352, SpellValueMod.BasePoint0, m_amount, target, true, null, this);
                            break;
                        }
                        switch (GetSpellInfo().Id)
                        {
                            // Feeding Frenzy Rank 1
                            case 53511:
                                if (target.GetVictim() != null && target.GetVictim().HealthBelowPct(35))
                                    target.CastSpell(target, 60096, true, null, this);
                                return;
                            // Feeding Frenzy Rank 2
                            case 53512:
                                if (target.GetVictim() != null && target.GetVictim().HealthBelowPct(35))
                                    target.CastSpell(target, 60097, true, null, this);
                                return;
                            default:
                                break;
                        }
                        break;
                    }
                case SpellFamilyNames.Shaman:
                    if (GetId() == 52179) // Astral Shift
                    {
                        // Periodic need for remove visual on stun/fear/silence lost
                        if (!target.HasUnitFlag(UnitFlags.Stunned | UnitFlags.Fleeing | UnitFlags.Silenced))
                            target.RemoveAurasDueToSpell(52179);
                        break;
                    }
                    break;
                case SpellFamilyNames.Deathknight:
                    switch (GetId())
                    {
                        case 49016: // Hysteria
                            var damage = (uint)target.CountPctFromMaxHealth(1);
                            target.DealDamage(target, damage, null, DamageEffectType.NoDamage, SpellSchoolMask.Normal, null, false);
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private void HandlePeriodicTriggerSpellAuraTick(Unit target, Unit caster)
        {
            // generic casting code with custom spells and target/caster customs
            var triggerSpellId = GetSpellEffectInfo().TriggerSpell;

            var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            var auraSpellInfo = GetSpellInfo();
            var auraId = auraSpellInfo.Id;

            // specific code for cases with no trigger spell provided in field
            if (triggeredSpellInfo == null)
            {
                switch (auraSpellInfo.SpellFamilyName)
                {
                    case SpellFamilyNames.Generic:
                        {
                            switch (auraId)
                            {
                                // Brood Affliction: Bronze
                                case 23170:
                                    triggerSpellId = 23171;
                                    break;
                                // Restoration
                                case 24379:
                                case 23493:
                                    {
                                        if (caster != null)
                                        {
                                            var heal = (uint)caster.CountPctFromMaxHealth(10);
                                            var healInfo = new HealInfo(caster, target, heal, auraSpellInfo, auraSpellInfo.GetSchoolMask());
                                            caster.HealBySpell(healInfo);

                                            // @todo: should proc other auras?
                                            int mana = caster.GetMaxPower(PowerType.Mana);
                                            if (mana != 0)
                                            {
                                                mana /= 10;
                                                caster.EnergizeBySpell(caster, 23493, mana, PowerType.Mana);
                                            }
                                        }
                                        return;
                                    }
                                // Nitrous Boost
                                case 27746:
                                    if (caster != null && target.GetPower(PowerType.Mana) >= 10)
                                    {
                                        target.ModifyPower(PowerType.Mana, -10);
                                        target.SendEnergizeSpellLog(caster, 27746, 10, 0, PowerType.Mana);
                                    }
                                    else
                                        target.RemoveAurasDueToSpell(27746);
                                    return;
                                // Frost Blast
                                case 27808:
                                    if (caster != null)
                                        caster.CastCustomSpell(29879, SpellValueMod.BasePoint0, (int)target.CountPctFromMaxHealth(21), target, true, null, this);
                                    return;
                                // Inoculate Nestlewood Owlkin
                                case 29528:
                                    if (!target.IsTypeId(TypeId.Unit)) // prevent error reports in case ignored player target
                                        return;
                                    break;
                                // Feed Captured Animal
                                case 29917:
                                    triggerSpellId = 29916;
                                    break;
                                // Extract Gas
                                case 30427:
                                    {
                                        // move loot to player inventory and despawn target
                                        if (caster != null && caster.IsTypeId(TypeId.Player) &&
                                                target.IsTypeId(TypeId.Unit) &&
                                                target.ToCreature().GetCreatureTemplate().CreatureType == CreatureType.GasCloud)
                                        {
                                            var player = caster.ToPlayer();
                                            var creature = target.ToCreature();
                                            // missing lootid has been reported on startup - just return
                                            if (creature.GetCreatureTemplate().SkinLootId == 0)
                                                return;

                                            player.AutoStoreLoot(creature.GetCreatureTemplate().SkinLootId, LootStorage.Skinning, ItemContext.None, true);

                                            creature.DespawnOrUnsummon();
                                        }
                                        return;
                                    }
                                // Quake
                                case 30576:
                                    triggerSpellId = 30571;
                                    break;
                                // Doom
                                // @todo effect trigger spell may be independant on spell targets, and executed in spell finish phase
                                // so instakill will be naturally done before trigger spell
                                case 31347:
                                    {
                                        target.CastSpell(target, 31350, true, null, this);
                                        target.KillSelf();
                                        return;
                                    }
                                // Spellcloth
                                case 31373:
                                    {
                                        // Summon Elemental after create item
                                        target.SummonCreature(17870, 0, 0, 0, target.GetOrientation(), TempSummonType.DeadDespawn, 0);
                                        return;
                                    }
                                // Flame Quills
                                case 34229:
                                    {
                                        // cast 24 spells 34269-34289, 34314-34316
                                        for (uint spell_id = 34269; spell_id != 34290; ++spell_id)
                                            target.CastSpell(target, spell_id, true, null, this);
                                        for (uint spell_id = 34314; spell_id != 34317; ++spell_id)
                                            target.CastSpell(target, spell_id, true, null, this);
                                        return;
                                    }
                                // Remote Toy
                                case 37027:
                                    triggerSpellId = 37029;
                                    break;
                                // Eye of Grillok
                                case 38495:
                                    triggerSpellId = 38530;
                                    break;
                                // Absorb Eye of Grillok (Zezzak's Shard)
                                case 38554:
                                    {
                                        if (caster == null || !target.IsTypeId(TypeId.Unit))
                                            return;

                                        caster.CastSpell(caster, 38495, true, null, this);

                                        var creatureTarget = target.ToCreature();

                                        creatureTarget.DespawnOrUnsummon();
                                        return;
                                    }
                                // Tear of Azzinoth Summon Channel - it's not really supposed to do anything, and this only prevents the console spam
                                case 39857:
                                    triggerSpellId = 39856;
                                    break;
                                // Personalized Weather
                                case 46736:
                                    triggerSpellId = 46737;
                                    break;
                            }
                            break;
                        }
                    case SpellFamilyNames.Shaman:
                        {
                            switch (auraId)
                            {
                                // Lightning Shield (The Earthshatterer set trigger after cast Lighting Shield)
                                case 28820:
                                    {
                                        // Need remove self if Lightning Shield not active
                                        if (target.GetAuraEffect(AuraType.ProcTriggerSpell, SpellFamilyNames.Shaman, new FlagArray128(0x400, 0, 0)) == null)
                                            target.RemoveAurasDueToSpell(28820);
                                        return;
                                    }
                                // Totemic Mastery (Skyshatter Regalia (Shaman Tier 6) - bonus)
                                case 38443:
                                    {
                                        var all = true;
                                        for (var i = (int)SummonSlot.Totem; i < SharedConst.MaxSummonSlot; ++i)
                                        {
                                            if (target.m_SummonSlot[i].IsEmpty())
                                            {
                                                all = false;
                                                break;
                                            }
                                        }

                                        if (all)
                                            target.CastSpell(target, 38437, true, null, this);
                                        else
                                            target.RemoveAurasDueToSpell(38437);
                                        return;
                                    }
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            else
            {
                // Spell exist but require custom code
                switch (auraId)
                {
                    // Pursuing Spikes (Anub'arak)
                    case 65920:
                    case 65922:
                    case 65923:
                        {
                            Unit permafrostCaster = null;
                            var permafrostAura = target.GetAura(66193);
                            if (permafrostAura == null)
                                permafrostAura = target.GetAura(67855);
                            if (permafrostAura == null)
                                permafrostAura = target.GetAura(67856);
                            if (permafrostAura == null)
                                permafrostAura = target.GetAura(67857);

                            if (permafrostAura != null)
                                permafrostCaster = permafrostAura.GetCaster();

                            if (permafrostCaster != null)
                            {
                                var permafrostCasterCreature = permafrostCaster.ToCreature();
                                if (permafrostCasterCreature != null)
                                    permafrostCasterCreature.DespawnOrUnsummon(3000);

                                target.CastSpell(target, 66181, false);
                                target.RemoveAllAuras();
                                var targetCreature = target.ToCreature();
                                if (targetCreature != null)
                                    targetCreature.DisappearAndDie();
                            }
                            break;
                        }
                    // Mana Tide
                    case 16191:
                        target.CastCustomSpell(target, triggerSpellId, m_amount, 0, 0, true, null, this);
                        return;
                    // Negative Energy Periodic
                    case 46284:
                        target.CastCustomSpell(triggerSpellId, SpellValueMod.MaxTargets, (int)(m_tickNumber / 10 + 1), null, true, null, this);
                        return;
                    // Poison (Grobbulus)
                    case 28158:
                    case 54362:
                    // Slime Pool (Dreadscale & Acidmaw)
                    case 66882:
                        target.CastCustomSpell(triggerSpellId, SpellValueMod.RadiusMod, (int)((((float)m_tickNumber / 60) * 0.9f + 0.1f) * 10000 * 2 / 3), null, true, null, this);
                        return;
                    // Slime Spray - temporary here until preventing default effect works again
                    // added on 9.10.2010
                    case 69508:
                        {
                            if (caster != null)
                                caster.CastSpell(target, triggerSpellId, true, null, null, caster.GetGUID());
                            return;
                        }
                    case 24745: // Summon Templar, Trigger
                    case 24747: // Summon Templar Fire, Trigger
                    case 24757: // Summon Templar Air, Trigger
                    case 24759: // Summon Templar Earth, Trigger
                    case 24761: // Summon Templar Water, Trigger
                    case 24762: // Summon Duke, Trigger
                    case 24766: // Summon Duke Fire, Trigger
                    case 24769: // Summon Duke Air, Trigger
                    case 24771: // Summon Duke Earth, Trigger
                    case 24773: // Summon Duke Water, Trigger
                    case 24785: // Summon Royal, Trigger
                    case 24787: // Summon Royal Fire, Trigger
                    case 24791: // Summon Royal Air, Trigger
                    case 24792: // Summon Royal Earth, Trigger
                    case 24793: // Summon Royal Water, Trigger
                        {
                            // All this spells trigger a spell that requires reagents; if the
                            // triggered spell is cast as "triggered", reagents are not consumed
                            if (caster != null)
                                caster.CastSpell(target, triggerSpellId, false);
                            return;
                        }
                }
            }

            // Reget trigger spell proto
            triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());

            if (triggeredSpellInfo != null)
            {
                var triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? caster : target;
                if (triggerCaster != null)
                {
                    triggerCaster.CastSpell(target, triggeredSpellInfo, true, null, this);
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
        }

        private void HandlePeriodicTriggerSpellWithValueAuraTick(Unit target, Unit caster)
        {
            var triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                var triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? caster : target;
                if (triggerCaster != null)
                {
                    var basepoints = GetAmount();
                    triggerCaster.CastCustomSpell(target, triggerSpellId, basepoints, basepoints, basepoints, true, null, this);
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
        }

        private void HandlePeriodicDamageAurasTick(Unit target, Unit caster)
        {
            if (!caster || !target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // Consecrate ticks can miss and will not show up in the combat log
            if (GetSpellEffectInfo().Effect == SpellEffectName.PersistentAreaAura &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            // some auras remove at specific health level or more
            if (GetAuraType() == AuraType.PeriodicDamage)
            {
                switch (GetSpellInfo().Id)
                {
                    case 43093:
                    case 31956:
                    case 38801:  // Grievous Wound
                    case 35321:
                    case 38363:
                    case 39215:  // Gushing Wound
                        if (target.IsFullHealth())
                        {
                            target.RemoveAurasDueToSpell(GetSpellInfo().Id);
                            return;
                        }
                        break;
                    case 38772: // Grievous Wound
                        {
                            var effect = GetSpellInfo().GetEffect(1);
                            if (effect != null)
                            {
                                var percent = effect.CalcValue(caster);
                                if (!target.HealthBelowPct(percent))
                                {
                                    target.RemoveAurasDueToSpell(GetSpellInfo().Id);
                                    return;
                                }
                            }
                            break;
                        }
                }
            }

            var cleanDamage = new CleanDamage(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

            // AOE spells are not affected by the new periodic system.
            var isAreaAura = GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura);
            // ignore non positive values (can be result apply spellmods to aura damage
            var damage = (uint)(isAreaAura ? Math.Max(GetAmount(), 0) : m_damage);

            // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
            if (isAreaAura)
                Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);

            switch (GetAuraType())
            {
                case AuraType.PeriodicDamage:
                    {
                        if (isAreaAura)
                            damage = (uint)(caster.SpellDamageBonusDone(target, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()) * caster.SpellDamagePctDone(target, m_spellInfo, DamageEffectType.DOT));
                        damage = target.SpellDamageBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount());

                    // Calculate armor mitigation
                    if (caster.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo(), (sbyte)GetEffIndex()))
                    {
                            var damageReductedArmor = caster.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo());
                            cleanDamage.mitigated_damage += damage - damageReductedArmor;
                            damage = damageReductedArmor;
                        }
                        // There is a Chance to make a Soul Shard when Drain soul does damage
                        if (GetSpellInfo().SpellFamilyName == SpellFamilyNames.Warlock && GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
                        {
                            if (caster.IsTypeId(TypeId.Player) && caster.ToPlayer().IsHonorOrXPTarget(target))
                                caster.CastSpell(caster, 95810, true, null, this);
                        }

                        if (GetSpellInfo().SpellFamilyName == SpellFamilyNames.Generic)
                        {
                            switch (GetId())
                            {
                                case 70911: // Unbound Plague
                                case 72854: // Unbound Plague
                                case 72855: // Unbound Plague
                                case 72856: // Unbound Plague
                                    damage *= (uint)Math.Pow(1.25f, m_tickNumber);
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    }
                case AuraType.PeriodicWeaponPercentDamage:
                    {
                        var attackType = GetSpellInfo().GetAttackType();

                        var weaponDamage = MathFunctions.CalculatePct(caster.CalculateDamage(attackType, false, true), GetAmount());

                        // Add melee damage bonuses (also check for negative)
                        var damageBonusDone = caster.MeleeDamageBonusDone(target, Math.Max(weaponDamage, 0), attackType, GetSpellInfo());

                        damage = target.MeleeDamageBonusTaken(caster, damageBonusDone, attackType, DamageEffectType.DOT, GetSpellInfo());
                        break;
                    }
                case AuraType.PeriodicDamagePercent:
                    // ceil obtained value, it may happen that 10 ticks for 10% damage may not kill owner
                    damage = (uint)Math.Ceiling(MathFunctions.CalculatePct((float)target.GetMaxHealth(), (float)damage));
                    break;
                default:
                    break;
            }

            if (!m_spellInfo.HasAttribute(SpellAttr4.FixedDamage))
            {
                if (GetSpellEffectInfo().IsTargetingArea() || isAreaAura)
                {
                    damage = (uint)(damage * target.GetTotalAuraMultiplierByMiscMask(AuraType.ModAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                    if (!caster.IsTypeId(TypeId.Player))
                        damage = (uint)(damage * target.GetTotalAuraMultiplierByMiscMask(AuraType.ModCreatureAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                }
            }

            var crit = RandomHelper.randChance(isAreaAura ? caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()) : m_critChance);
            if (crit)
                damage = caster.SpellCriticalDamageBonus(m_spellInfo, damage, target);

            var dmg = damage;
            if (!GetSpellInfo().HasAttribute(SpellAttr4.FixedDamage))
                caster.ApplyResilience(target, ref dmg);
            damage = dmg;

            var damageInfo = new DamageInfo(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
            caster.CalcAbsorbResist(damageInfo);
            damage = damageInfo.GetDamage();

            var absorb = damageInfo.GetAbsorb();
            var resist = damageInfo.GetResist();
            caster.DealDamageMods(target, ref damage, ref absorb);

            // Set trigger flag
            var procAttacker = ProcFlags.DonePeriodic;
            var procVictim = ProcFlags.TakenPeriodic;
            var hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim |= ProcFlags.TakenDamage;
            }

            var overkill = (int)(damage - target.GetHealth());
            if (overkill < 0)
                overkill = 0;

            var pInfo = new SpellPeriodicAuraLogInfo(this, damage, dmg, (uint)overkill, absorb, resist, 0.0f, crit);

            caster.DealDamage(target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), true);

            caster.ProcSkillsAndAuras(target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.None, hitMask, null, damageInfo, null);
            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandlePeriodicHealthLeechAuraTick(Unit target, Unit caster)
        {
            if (!caster || !target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            if (GetSpellEffectInfo().Effect == SpellEffectName.PersistentAreaAura &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            var cleanDamage = new CleanDamage(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

            var isAreaAura = GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura);
            // ignore negative values (can be result apply spellmods to aura damage
            var damage = (uint)(isAreaAura ? Math.Max(GetAmount(), 0) : m_damage);

            if (isAreaAura)
            {
                // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
                Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);
                damage = (uint)(caster.SpellDamageBonusDone(target, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()) * caster.SpellDamagePctDone(target, m_spellInfo, DamageEffectType.DOT));
            }
            damage = target.SpellDamageBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount());

            // Calculate armor mitigation
            if (caster.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo(), (sbyte)GetEffIndex()))
            {
                var damageReductedArmor = caster.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo());
                cleanDamage.mitigated_damage += damage - damageReductedArmor;
                damage = damageReductedArmor;
            }

            if (!m_spellInfo.HasAttribute(SpellAttr4.FixedDamage))
            {
                if (GetSpellEffectInfo().IsTargetingArea() || isAreaAura)
                {
                    damage = (uint)(damage * target.GetTotalAuraMultiplierByMiscMask(AuraType.ModAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                    if (!caster.IsTypeId(TypeId.Player))
                        damage = (uint)(damage * target.GetTotalAuraMultiplierByMiscMask(AuraType.ModCreatureAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                }
            }

            var crit = RandomHelper.randChance(isAreaAura ? caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()) : m_critChance);
            if (crit)
                damage = caster.SpellCriticalDamageBonus(m_spellInfo, damage, target);

            var dmg = damage;
            if (!GetSpellInfo().HasAttribute(SpellAttr4.FixedDamage))
                caster.ApplyResilience(target, ref dmg);
            damage = dmg;

            var damageInfo = new DamageInfo(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
            caster.CalcAbsorbResist(damageInfo);

            var absorb = damageInfo.GetAbsorb();
            var resist = damageInfo.GetResist();

            // SendSpellNonMeleeDamageLog expects non-absorbed/non-resisted damage
            var log = new SpellNonMeleeDamage(caster, target, GetSpellInfo(), GetBase().GetSpellVisual(), GetSpellInfo().GetSchoolMask(), GetBase().GetCastGUID());
            log.damage = damage;
            log.originalDamage = dmg;
            log.absorb = absorb;
            log.resist = resist;
            log.periodicLog = true;
            if (crit)
                log.HitInfo |= HitInfo.CriticalHit;

            // Set trigger flag
            var procAttacker = ProcFlags.DonePeriodic;
            var procVictim = ProcFlags.TakenPeriodic;
            var hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim |= ProcFlags.TakenDamage;
            }

            var new_damage = (int)caster.DealDamage(target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), false);
            if (caster.IsAlive())
            {
                caster.ProcSkillsAndAuras(target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.None, hitMask, null, damageInfo, null);

                var gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

                var heal = (caster.SpellHealingBonusDone(caster, GetSpellInfo(), (uint)(new_damage * gainMultiplier), DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()));
                heal = (caster.SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()));

                var healInfo = new HealInfo(caster, caster, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
                caster.HealBySpell(healInfo);

                caster.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());
                caster.ProcSkillsAndAuras(caster, ProcFlags.DonePeriodic, ProcFlags.TakenPeriodic, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.None, hitMask, null, null, healInfo);
            }

            caster.SendSpellNonMeleeDamageLog(log);
        }

        private void HandlePeriodicHealthFunnelAuraTick(Unit target, Unit caster)
        {
            if (caster == null || !caster.IsAlive() || !target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);
                return;
            }

            var damage = (uint)Math.Max(GetAmount(), 0);
            // do not kill health donator
            if (caster.GetHealth() < damage)
                damage = (uint)caster.GetHealth() - 1;
            if (damage == 0)
                return;

            caster.ModifyHealth(-(int)damage);
            Log.outDebug(LogFilter.Spells, "PeriodicTick: donator {0} target {1} damage {2}.", caster.GetEntry(), target.GetEntry(), damage);

            var gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            damage = (uint)(damage * gainMultiplier);

            var healInfo = new HealInfo(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.HealBySpell(healInfo);
            caster.ProcSkillsAndAuras(target, ProcFlags.DonePeriodic, ProcFlags.TakenPeriodic, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.None, ProcFlagsHit.Normal, null, null, healInfo);
        }

        private void HandlePeriodicHealAurasTick(Unit target, Unit caster)
        {
            if (!caster || !target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);
                return;
            }

            // heal for caster damage (must be alive)
            if (target != caster && GetSpellInfo().HasAttribute(SpellAttr2.HealthFunnel) && !caster.IsAlive())
                return;

            // don't regen when permanent aura target has full power
            if (GetBase().IsPermanent() && target.IsFullHealth())
                return;

            var isAreaAura = GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura);
            // ignore negative values (can be result apply spellmods to aura damage
            var damage = isAreaAura ? Math.Max(GetAmount(), 0) : m_damage;

            if (GetAuraType() == AuraType.ObsModHealth)
            {
                // Taken mods
                var TakenTotalMod = 1.0f;

                // Tenacity increase healing % taken
                var Tenacity = target.GetAuraEffect(58549, 0);
                if (Tenacity != null)
                    MathFunctions.AddPct(ref TakenTotalMod, Tenacity.GetAmount());

                // Healing taken percent
                float minval = target.GetMaxNegativeAuraModifier(AuraType.ModHealingPct);
                if (minval != 0)
                    MathFunctions.AddPct(ref TakenTotalMod, minval);

                float maxval = target.GetMaxPositiveAuraModifier(AuraType.ModHealingPct);
                if (maxval != 0)
                    MathFunctions.AddPct(ref TakenTotalMod, maxval);

                TakenTotalMod = Math.Max(TakenTotalMod, 0.0f);

                damage = (int)target.CountPctFromMaxHealth(damage);
                damage = (int)(damage * TakenTotalMod);
            }
            else
            {
                if (isAreaAura)
                    damage = (int)(caster.SpellHealingBonusDone(target, GetSpellInfo(), (uint)damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()) * caster.SpellHealingPctDone(target, m_spellInfo));
                damage = (int)target.SpellHealingBonusTaken(caster, GetSpellInfo(), (uint)damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount());
            }

            var crit = RandomHelper.randChance(isAreaAura ? caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()) : m_critChance);
            if (crit)
                damage = caster.SpellCriticalHealingBonus(m_spellInfo, damage, target);

            Log.outDebug(LogFilter.Spells, "PeriodicTick: {0} (TypeId: {1}) heal of {2} (TypeId: {3}) for {4} health inflicted by {5}",
                GetCasterGUID().ToString(), GetCaster().GetTypeId(), target.GetGUID().ToString(), target.GetTypeId(), damage, GetId());

            var heal = (uint)damage;

            var healInfo = new HealInfo(caster, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.CalcHealAbsorb(healInfo);
            caster.DealHeal(healInfo);

            var pInfo = new SpellPeriodicAuraLogInfo(this, heal, (uint)damage, heal - healInfo.GetEffectiveHeal(), healInfo.GetAbsorb(), 0, 0.0f, crit);
            target.SendPeriodicAuraLog(pInfo);

            target.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());

            // %-based heal - does not proc auras
            if (GetAuraType() == AuraType.ObsModHealth)
                return;

            var procAttacker = ProcFlags.DonePeriodic;
            var procVictim = ProcFlags.TakenPeriodic;
            var hitMask = crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
            // ignore item heals
            if (GetBase().GetCastItemGUID().IsEmpty())
                caster.ProcSkillsAndAuras(target, procAttacker, procVictim, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.None, hitMask, null, null, healInfo);
        }

        private void HandlePeriodicManaLeechAuraTick(Unit target, Unit caster)
        {
            var powerType = (PowerType)GetMiscValue();

            if (caster == null || !caster.IsAlive() || !target.IsAlive() || target.GetPowerType() != powerType)
                return;

            if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            if (GetSpellEffectInfo().Effect == SpellEffectName.PersistentAreaAura &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            // ignore negative values (can be result apply spellmods to aura damage
            var drainAmount = Math.Max(m_amount, 0);

            var drainedAmount = -target.ModifyPower(powerType, -drainAmount);
            var gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            var pInfo = new SpellPeriodicAuraLogInfo(this, (uint)drainedAmount, (uint)drainAmount, 0, 0, 0, gainMultiplier, false);

            var gainAmount = (int)(drainedAmount * gainMultiplier);
            var gainedAmount = 0;
            if (gainAmount != 0)
            {
                gainedAmount = caster.ModifyPower(powerType, gainAmount);
                // energize is not modified by threat modifiers
                target.GetThreatManager().AddThreat(caster, gainedAmount * 0.5f, GetSpellInfo(), true);
            }

            // Drain Mana
            if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag<uint>(0x00000010))
            {
                var manaFeedVal = 0;
                var aurEff = GetBase().GetEffect(1);
                if (aurEff != null)
                    manaFeedVal = aurEff.GetAmount();
                // Mana Feed - Drain Mana
                if (manaFeedVal > 0)
                {
                    var feedAmount = MathFunctions.CalculatePct(gainedAmount, manaFeedVal);
                    caster.CastCustomSpell(caster, 32554, feedAmount, 0, 0, true, null, this);
                }
            }

            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandleObsModPowerAuraTick(Unit target, Unit caster)
        {
            PowerType powerType;
            if (GetMiscValue() == (int)PowerType.All)
                powerType = target.GetPowerType();
            else
                powerType = (PowerType)GetMiscValue();

            if (!target.IsAlive() || target.GetMaxPower(powerType) == 0)
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);
                return;
            }

            // don't regen when permanent aura target has full power
            if (GetBase().IsPermanent() && target.GetPower(powerType) == target.GetMaxPower(powerType))
                return;

            // ignore negative values (can be result apply spellmods to aura damage
            var amount = Math.Max(m_amount, 0) * target.GetMaxPower(powerType) / 100;

            var pInfo = new SpellPeriodicAuraLogInfo(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);

            var gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, GetSpellInfo(), true);

            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandlePeriodicEnergizeAuraTick(Unit target, Unit caster)
        {
            var powerType = (PowerType)GetMiscValue();
            if (!target.IsAlive() || target.GetMaxPower(powerType) == 0)
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);
                return;
            }

            // don't regen when permanent aura target has full power
            if (GetBase().IsPermanent() && target.GetPower(powerType) == target.GetMaxPower(powerType))
                return;

            // ignore negative values (can be result apply spellmods to aura damage
            var amount = Math.Max(m_amount, 0);

            var pInfo = new SpellPeriodicAuraLogInfo(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);
            var gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, GetSpellInfo(), true);

            target.SendPeriodicAuraLog(pInfo);
        }

        private void HandlePeriodicPowerBurnAuraTick(Unit target, Unit caster)
        {
            var powerType = (PowerType)GetMiscValue();

            if (caster == null || !target.IsAlive() || target.GetPowerType() != powerType)
                return;

            if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // ignore negative values (can be result apply spellmods to aura damage
            var damage = Math.Max(m_amount, 0);

            var gain = (uint)(-target.ModifyPower(powerType, -damage));

            var dmgMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            var spellProto = GetSpellInfo();
            // maybe has to be sent different to client, but not by SMSG_PERIODICAURALOG
            var damageInfo = new SpellNonMeleeDamage(caster, target, spellProto, GetBase().GetSpellVisual(), spellProto.SchoolMask, GetBase().GetCastGUID());
            // no SpellDamageBonus for burn mana
            caster.CalculateSpellDamageTaken(damageInfo, (int)(gain * dmgMultiplier), spellProto);

            caster.DealDamageMods(damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

            // Set trigger flag
            var procAttacker = ProcFlags.DonePeriodic;
            var procVictim = ProcFlags.TakenPeriodic;
            var hitMask = Unit.CreateProcHitMask(damageInfo, SpellMissInfo.None);
            var spellTypeMask = ProcFlagsSpellType.NoDmgHeal;
            if (damageInfo.damage != 0)
            {
                procVictim |= ProcFlags.TakenDamage;
                spellTypeMask |= ProcFlagsSpellType.Damage;
            }

            caster.DealSpellDamage(damageInfo, true);

            var dotDamageInfo = new DamageInfo(damageInfo, DamageEffectType.DOT, WeaponAttackType.BaseAttack, hitMask);
            caster.ProcSkillsAndAuras(target, procAttacker, procVictim, spellTypeMask, ProcFlagsSpellPhase.None, hitMask, null, dotDamageInfo, null);

            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }

        private void HandleBreakableCCAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            var damageLeft = (int)(GetAmount() - eventInfo.GetDamageInfo().GetDamage());

            if (damageLeft <= 0)
                aurApp.GetTarget().RemoveAura(aurApp);
            else
                ChangeAmount(damageLeft);
        }

        private void HandleProcTriggerSpellAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            var triggerCaster = aurApp.GetTarget();
            var triggerTarget = eventInfo.GetProcTarget();

            var triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                Log.outDebug(LogFilter.Spells, $"AuraEffect.HandleProcTriggerSpellAuraProc: Triggering spell {triggeredSpellInfo.Id} from aura {GetId()} proc");
                triggerCaster.CastSpell(triggerTarget, triggeredSpellInfo, true, null, this);
            }
            else if (triggerSpellId != 0 && GetAuraType() != AuraType.Dummy)
                Log.outError(LogFilter.Spells, $"AuraEffect.HandleProcTriggerSpellAuraProc: Could not trigger spell {triggerSpellId} from aura {GetId()} proc, because the spell does not have an entry in Spell.dbc.");
        }

        private void HandleProcTriggerSpellWithValueAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            var triggerCaster = aurApp.GetTarget();
            var triggerTarget = eventInfo.GetProcTarget();

            var triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            var triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                var basepoints0 = GetAmount();
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Triggering spell {0} with value {1} from aura {2} proc", triggeredSpellInfo.Id, basepoints0, GetId());
                triggerCaster.CastCustomSpell(triggerTarget, triggerSpellId, basepoints0, 0, 0, true, null, this);
            }
            else
                Log.outError(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Could not trigger spell {0} from aura {1} proc, because the spell does not have an entry in Spell.dbc.", triggerSpellId, GetId());
        }

        public void HandleProcTriggerDamageAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            var target = aurApp.GetTarget();
            var triggerTarget = eventInfo.GetProcTarget();
            if (triggerTarget.HasUnitState(UnitState.Isolated) || triggerTarget.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(triggerTarget, target);
                return;
            }

            var damageInfo = new SpellNonMeleeDamage(target, triggerTarget, GetSpellInfo(), GetBase().GetSpellVisual(), GetSpellInfo().SchoolMask, GetBase().GetCastGUID());
            var damage = (int)target.SpellDamageBonusDone(triggerTarget, GetSpellInfo(), (uint)GetAmount(), DamageEffectType.SpellDirect, GetSpellEffectInfo());
            damage = (int)triggerTarget.SpellDamageBonusTaken(target, GetSpellInfo(), (uint)damage, DamageEffectType.SpellDirect, GetSpellEffectInfo());
            target.CalculateSpellDamageTaken(damageInfo, damage, GetSpellInfo());
            target.DealDamageMods(damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);
            target.DealSpellDamage(damageInfo, true);
            target.SendSpellNonMeleeDamageLog(damageInfo);
        }

        [AuraEffectHandler(AuraType.ForceWeather)]
        private void HandleAuraForceWeather(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SendPacket(new WeatherPkt((WeatherState)GetMiscValue(), 1.0f));
            else
                target.GetMap().SendZoneWeather(target.GetZoneId(), target);
        }

        [AuraEffectHandler(AuraType.EnableAltPower)]
        private void HandleEnableAltPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var altPowerId = GetMiscValue();
            var powerEntry = CliDB.UnitPowerBarStorage.LookupByKey(altPowerId);
            if (powerEntry == null)
                return;

            if (apply)
                aurApp.GetTarget().SetMaxPower(PowerType.AlternatePower, (int)powerEntry.MaxPower);
            else
                aurApp.GetTarget().SetMaxPower(PowerType.AlternatePower, 0);
        }

        [AuraEffectHandler(AuraType.ModSpellCategoryCooldown)]
        private void HandleModSpellCategoryCooldown(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (player)
                player.SendSpellCategoryCooldowns();
        }

        [AuraEffectHandler(AuraType.ShowConfirmationPrompt)]
        [AuraEffectHandler(AuraType.ShowConfirmationPromptWithDifficulty)]
        private void HandleShowConfirmationPrompt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (!player)
                return;

            if (apply)
                player.AddTemporarySpell(_effectInfo.TriggerSpell);
            else
                player.RemoveTemporarySpell(_effectInfo.TriggerSpell);
        }

        [AuraEffectHandler(AuraType.OverridePetSpecs)]
        private void HandleOverridePetSpecs(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (!player)
                return;

            if (player.GetClass() != Class.Hunter)
                return;

            var pet = player.GetPet();
            if (!pet)
                return;

            var currSpec = CliDB.ChrSpecializationStorage.LookupByKey(pet.GetSpecialization());
            if (currSpec == null)
                return;

            pet.SetSpecialization(Global.DB2Mgr.GetChrSpecializationByIndex(apply ? Class.Max : 0, currSpec.OrderIndex).Id);
        }

        [AuraEffectHandler(AuraType.AllowUsingGameobjectsWhileMounted)]
        private void HandleAllowUsingGameobjectsWhileMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.AddPlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
            else if (!target.HasAuraType(AuraType.AllowUsingGameobjectsWhileMounted))
                target.RemovePlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
        }

        [AuraEffectHandler(AuraType.PlayScene)]
        private void HandlePlayScene(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var player = aurApp.GetTarget().ToPlayer();
            if (!player)
                return;

            SceneTemplate sceneTemplate = Global.ObjectMgr.GetSceneTemplate((uint)GetMiscValue());
            if (sceneTemplate == null)
                return;

            if (apply)
                player.GetSceneMgr().PlaySceneByTemplate(sceneTemplate);
            else
                player.GetSceneMgr().CancelSceneByPackageId(sceneTemplate.ScenePackageId);
        }

        [AuraEffectHandler(AuraType.AreaTrigger)]
        private void HandleCreateAreaTrigger(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();

            if (apply)
            {
                AreaTrigger.CreateAreaTrigger((uint)GetMiscValue(), GetCaster(), target, GetSpellInfo(), target, GetBase().GetDuration(), GetBase().GetSpellVisual(), ObjectGuid.Empty, this);
            }
            else
            {
                var caster = GetCaster();
                if (caster)
                    caster.RemoveAreaTrigger(this);
            }
        }

        [AuraEffectHandler(AuraType.PvpTalents)]
        private void HandleAuraPvpTalents(AuraApplication auraApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = auraApp.GetTarget().ToPlayer();
            if (target)
            {
                if (apply)
                    target.TogglePvpTalents(true);
                else if (!target.HasAuraType(AuraType.PvpTalents))
                    target.TogglePvpTalents(false);
            }
        }

        [AuraEffectHandler(AuraType.LinkedSummon)]
        private void HandleLinkedSummon(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget();
            var triggerSpellInfo = Global.SpellMgr.GetSpellInfo(GetSpellEffectInfo().TriggerSpell, GetBase().GetCastDifficulty());
            if (triggerSpellInfo == null)
                return;

            // on apply cast summon spell
            if (apply)
                target.CastSpell(target, triggerSpellInfo, true, null, this);
            // on unapply we need to search for and remove the summoned creature
            else
            {
                var summonedEntries = new List<uint>();
                foreach (var spellEffect in triggerSpellInfo.GetEffects())
                {
                    if (spellEffect != null && spellEffect.Effect == SpellEffectName.Summon)
                    {
                        var summonEntry = (uint)spellEffect.MiscValue;
                        if (summonEntry != 0)
                            summonedEntries.Add(summonEntry);

                    }
                }

                // we don't know if there can be multiple summons for the same effect, so consider only 1 summon for each effect
                // most of the spells have multiple effects with the same summon spell id for multiple spawns, so right now it's safe to assume there's only 1 spawn per effect
                foreach (var summonEntry in summonedEntries)
                {
                    var nearbyEntries = new List<Creature>();
                    target.GetCreatureListWithEntryInGrid(nearbyEntries, summonEntry);
                    foreach (var creature in nearbyEntries)
                    {
                        if (creature.GetOwner() == target)
                        {
                            creature.DespawnOrUnsummon();
                            break;
                        }
                        else
                        {
                            var tempSummon = creature.ToTempSummon();
                            if (tempSummon)
                            {
                                if (tempSummon.GetSummoner() == target)
                                {
                                    tempSummon.DespawnOrUnsummon();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.SetFFAPvp)]
        private void HandleSetFFAPvP(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            target.UpdatePvPState(true);
        }
        
        [AuraEffectHandler(AuraType.ModOverrideZonePvpType)]
        private void HandleModOverrideZonePVPType(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.SetOverrideZonePVPType((ZonePVPTypeOverride)GetMiscValue());
            else if (target.HasAuraType(AuraType.ModOverrideZonePvpType))
                target.SetOverrideZonePVPType((ZonePVPTypeOverride)target.GetAuraEffectsByType(AuraType.ModOverrideZonePvpType).Last().GetMiscValue());
            else
                target.SetOverrideZonePVPType(ZonePVPTypeOverride.None);

            target.UpdateHostileAreaState(CliDB.AreaTableStorage.LookupByKey(target.GetZoneId()));
            target.UpdatePvPState();
        }

        [AuraEffectHandler(AuraType.BattleGroundPlayerPositionFactional)]
        [AuraEffectHandler(AuraType.BattleGroundPlayerPosition)]
        private void HandleBattlegroundPlayerPosition(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            var target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            BattlegroundMap battlegroundMap = target.GetMap().ToBattlegroundMap();
            if (battlegroundMap == null)
                return;

            Battleground bg = battlegroundMap.GetBG();
            if (bg == null)
                return;

            if (apply)
            {
                var playerPosition = new BattlegroundPlayerPosition();
                playerPosition.Guid = target.GetGUID();
                playerPosition.ArenaSlot = (sbyte)GetMiscValue();
                playerPosition.Pos = target.GetPosition();

                if (GetAuraType() == AuraType.BattleGroundPlayerPositionFactional)
                    playerPosition.IconID = target.GetTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconHordeFlag : BattlegroundConst.PlayerPositionIconAllianceFlag;
                else if (GetAuraType() == AuraType.BattleGroundPlayerPosition)
                    playerPosition.IconID = target.GetTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconAllianceFlag : BattlegroundConst.PlayerPositionIconHordeFlag;
                else
                    Log.outWarn(LogFilter.Spells, $"Unknown aura effect {GetAuraType()} handled by HandleBattlegroundPlayerPosition.");

                bg.AddPlayerPosition(playerPosition);
            }
            else
                bg.RemovePlayerPosition(target.GetGUID());
        }
        #endregion
    }

    internal class AbsorbAuraOrderPred : Comparer<AuraEffect>
    {
        public override int Compare(AuraEffect aurEffA, AuraEffect aurEffB)
        {
            var spellProtoA = aurEffA.GetSpellInfo();
            var spellProtoB = aurEffB.GetSpellInfo();

            // Fel Blossom
            if (spellProtoA.Id == 28527)
                return 1;
            if (spellProtoB.Id == 28527)
                return 0;

            // Ice Barrier
            if (spellProtoA.GetCategory() == 471)
                return 1;
            if (spellProtoB.GetCategory() == 471)
                return 0;

            // Sacrifice
            if (spellProtoA.Id == 7812)
                return 1;
            if (spellProtoB.Id == 7812)
                return 0;

            // Cauterize (must be last)
            if (spellProtoA.Id == 86949)
                return 0;
            if (spellProtoB.Id == 86949)
                return 1;

            // Spirit of Redemption (must be last)
            if (spellProtoA.Id == 20711)
                return 0;
            if (spellProtoB.Id == 20711)
                return 1;

            return 0;
        }
    }
}
