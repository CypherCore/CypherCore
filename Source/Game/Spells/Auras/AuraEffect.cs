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
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Spells
{
    public class AuraEffect
    {
        public AuraEffect(Aura abase, uint effindex, int? baseAmount, Unit caster)
        {
            auraBase = abase;
            m_spellInfo = abase.GetSpellInfo();
            _effectInfo = abase.GetSpellEffectInfo(effindex);
            m_baseAmount = baseAmount.HasValue ? baseAmount.Value : _effectInfo.CalcBaseValue(caster, abase.GetAuraType() == AuraObjectType.Unit ? abase.GetOwner().ToUnit() : null, abase.GetCastItemLevel());
            m_donePct = 1.0f;
            m_effIndex = (byte)effindex;
            m_canBeRecalculated = true;
            m_isPeriodic = false;

            CalculatePeriodic(caster, true, false);
            m_amount = CalculateAmount(caster);
            CalculateSpellMod();
        }

        void GetTargetList(out List<Unit> targetList)
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
        void GetApplicationList(out List<AuraApplication> applicationList)
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
            int amount = 0;

            if (!m_spellInfo.HasAttribute(SpellAttr8.MasterySpecialization) || MathFunctions.fuzzyEq(GetSpellEffectInfo().BonusCoefficient, 0.0f))
                amount = GetSpellEffectInfo().CalcValue(caster, m_baseAmount, GetBase().GetOwner().ToUnit(), GetBase().GetCastItemLevel());
            else if (caster != null && caster.IsTypeId(TypeId.Player))
                amount = (int)(caster.GetFloatValue(ActivePlayerFields.Mastery) * GetSpellEffectInfo().BonusCoefficient);

            // check item enchant aura cast
            if (amount == 0 && caster != null)
            {
                ObjectGuid itemGUID = GetBase().GetCastItemGUID();
                if (!itemGUID.IsEmpty())
                {
                    Player playerCaster = caster.ToPlayer();
                    if (playerCaster != null)
                    {
                        Item castItem = playerCaster.GetItemByGuid(itemGUID);
                        if (castItem != null)
                        {
                            if (castItem.GetItemSuffixFactor() != 0)
                            {
                                var item_rand_suffix = CliDB.ItemRandomSuffixStorage.LookupByKey((uint)Math.Abs(castItem.GetItemRandomPropertyId()));
                                if (item_rand_suffix != null)
                                {
                                    for (int k = 0; k < ItemConst.MaxItemRandomProperties; k++)
                                    {
                                        var pEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(item_rand_suffix.Enchantment[k]);
                                        if (pEnchant != null)
                                        {
                                            for (int t = 0; t < ItemConst.MaxItemEnchantmentEffects; t++)
                                            {
                                                if (pEnchant.EffectArg[t] == m_spellInfo.Id)
                                                {
                                                    amount = (int)((item_rand_suffix.AllocationPct[k] * castItem.GetItemSuffixFactor()) / 10000);
                                                    break;
                                                }
                                            }
                                        }

                                        if (amount != 0)
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

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
                    uint mountType = (uint)GetMiscValueB();
                    MountRecord mountEntry = Global.DB2Mgr.GetMount(GetId());
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

            Player modOwner = caster != null ? caster.GetSpellModOwner() : null;

            // Apply casting time mods
            if (m_period != 0)
            {
                // Apply periodic time mod
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetId(), SpellModOp.ActivationTime, ref m_period);

                if (caster != null)
                {
                    // Haste modifies periodic time of channeled spells
                    if (m_spellInfo.IsChanneled())
                        caster.ModSpellDurationTime(m_spellInfo, ref m_period);
                    else if (m_spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                        m_period = (int)(m_period * caster.GetFloatValue(UnitFields.ModCastHaste));
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

            List<AuraApplication> effectApplications = new List<AuraApplication>();
            GetApplicationList(out effectApplications);

            foreach (var appt in effectApplications)
                if (appt.HasEffect(GetEffIndex()))
                    HandleEffect(appt, handleMask, false);

            if (Convert.ToBoolean(handleMask & AuraEffectHandleModes.ChangeAmount))
            {
                if (!mark)
                    m_amount = newAmount;
                else
                    SetAmount(newAmount);
                CalculateSpellMod();
            }

            foreach (var appt in effectApplications)
                if (appt.HasEffect(GetEffIndex()))
                    HandleEffect(appt, handleMask, true);

            if (GetSpellInfo().HasAttribute(SpellAttr8.AuraSendAmount))
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
            bool prevented = false;
            if (apply)
                prevented = GetBase().CallScriptEffectApplyHandlers(this, aurApp, mode);
            else
                prevented = GetBase().CallScriptEffectRemoveHandlers(this, aurApp, mode);

            // check if script events have removed the aura or if default effect prevention was requested
            if ((apply && aurApp.HasRemoveMode()) || prevented)
                return;

            Global.SpellMgr.GetAuraEffectHandler(GetAuraType()).Invoke(this, aurApp, mode, apply);

            // check if script events have removed the aura or if default effect prevention was requested
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
            AuraApplication aurApp = GetBase().GetApplicationOfTarget(target.GetGUID());
            Cypher.Assert(aurApp != null);
            HandleEffect(aurApp, mode, apply);
        }

        void ApplySpellMod(Unit target, bool apply)
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
                        ObjectGuid guid = target.GetGUID();
                        foreach (var iter in target.GetAppliedAuras())
                        {
                            if (iter.Value == null)
                                continue;
                            Aura aura = iter.Value.GetBase();
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

                    List<AuraApplication> effectApplications = new List<AuraApplication>();
                    GetApplicationList(out effectApplications);
                    // tick on targets of effects
                    foreach (var appt in effectApplications)
                        if (appt.HasEffect(GetEffIndex()))
                            PeriodicTick(appt, caster);
                }
            }
        }
        void UpdatePeriodic(Unit caster)
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
                                    AuraEffect aurEff = GetBase().GetEffect(0);
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
                                AuraEffect slow = GetBase().GetEffect(0);
                                if (slow != null)
                                {
                                    int newAmount = slow.GetAmount() + GetAmount();
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

        bool CanPeriodicTickCrit(Unit caster)
        {
            Cypher.Assert(caster);

            return caster.HasAuraTypeWithAffectMask(AuraType.AbilityPeriodicCrit, m_spellInfo);
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

        void SendTickImmune(Unit target, Unit caster)
        {
            if (caster != null)
                caster.SendSpellDamageImmune(target, m_spellInfo.Id, true);
        }

        void PeriodicTick(AuraApplication aurApp, Unit caster)
        {
            bool prevented = GetBase().CallScriptEffectPeriodicHandlers(this, aurApp);
            if (prevented)
                return;

            Unit target = aurApp.GetTarget();

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
                        if (damageInfo == null || damageInfo.GetDamage() == 0)
                            return false;

                        // Spell own damage at apply won't break CC
                        if (spellInfo != null && spellInfo == GetSpellInfo())
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
                    if (spellInfo == null || !Convert.ToBoolean(spellInfo.GetAllEffectsMechanicMask() & (1 << GetMiscValue())))
                        return false;
                    break;
                case AuraType.ModCastingSpeedNotStack:
                    // skip melee hits and instant cast spells
                    if (!eventInfo.GetProcSpell() || eventInfo.GetProcSpell().GetCastTime() == 0)
                        return false;
                    break;
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
                        uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
                        SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId);
                        if (triggeredSpellInfo != null)
                            if (aurApp.GetTarget().m_extraAttacks != 0 && triggeredSpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
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
            bool prevented = GetBase().CallScriptEffectProcHandlers(this, aurApp, eventInfo);
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
                    Player plrTarget = target.ToPlayer();

                    var sp_list = plrTarget.GetSpellMap();
                    foreach (var pair in sp_list)
                    {
                        if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
                            continue;

                        if (pair.Key == spellId || pair.Key == spellId2 || pair.Key == spellId3 || pair.Key == spellId4)
                            continue;

                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key);
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
                    ulong newStance = newAura != null ? (1ul << (newAura.GetMiscValue() - 1)) : 0;

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

        int GetPeriodicTimer() { return m_periodicTimer; }
        public void SetPeriodicTimer(int periodicTimer) { m_periodicTimer = periodicTimer; }

        void RecalculateAmount()
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
        void SetPeriodic(bool isPeriodic) { m_isPeriodic = isPeriodic; }
        bool HasSpellClassMask() { return GetSpellEffectInfo().SpellClassMask; }

        public SpellEffectInfo GetSpellEffectInfo() { return _effectInfo; }

        public bool IsEffect() { return _effectInfo.Effect != 0; }
        public bool IsEffect(SpellEffectName effectName) { return _effectInfo.Effect == effectName; }
        public bool IsAreaAuraEffect()
        {
            if (_effectInfo.Effect == SpellEffectName.ApplyAreaAuraParty ||
                _effectInfo.Effect == SpellEffectName.ApplyAreaAuraRaid ||
                _effectInfo.Effect == SpellEffectName.ApplyAreaAuraFriend ||
                _effectInfo.Effect == SpellEffectName.ApplyAreaAuraEnemy ||
                _effectInfo.Effect == SpellEffectName.ApplyAreaAuraPet ||
                _effectInfo.Effect == SpellEffectName.ApplyAreaAuraOwner)
                return true;
            return false;
        }

        #region Fields
        Aura auraBase;
        SpellInfo m_spellInfo;
        SpellEffectInfo _effectInfo;
        public int m_baseAmount;

        int m_amount;
        int m_damage;
        float m_critChance;
        float m_donePct;

        SpellModifier m_spellmod;

        int m_periodicTimer;
        int m_period;
        uint m_tickNumber;

        byte m_effIndex;
        bool m_canBeRecalculated;
        bool m_isPeriodic;
        #endregion

        #region AuraEffect Handlers
        [AuraEffectHandler(AuraType.None)]
        [AuraEffectHandler(AuraType.Unk46)]
        [AuraEffectHandler(AuraType.Unk48)]
        [AuraEffectHandler(AuraType.PetDamageMulti)]
        [AuraEffectHandler(AuraType.DetectAmore)]
        [AuraEffectHandler(AuraType.ModCriticalThreat)]
        [AuraEffectHandler(AuraType.ModCooldown)]
        [AuraEffectHandler(AuraType.Unk214)]
        [AuraEffectHandler(AuraType.ModDetaunt)]
        [AuraEffectHandler(AuraType.ModSpellDamageFromHealing)]
        [AuraEffectHandler(AuraType.ModTargetResistBySpellClass)]
        [AuraEffectHandler(AuraType.ModDamageDoneForMechanic)]
        [AuraEffectHandler(AuraType.BlockSpellFamily)]
        [AuraEffectHandler(AuraType.Strangulate)]
        [AuraEffectHandler(AuraType.ModCritChanceForCaster)]
        [AuraEffectHandler(AuraType.Unk311)]
        [AuraEffectHandler(AuraType.AnimReplacementSet)]
        [AuraEffectHandler(AuraType.ModSpellPowerPct)]
        [AuraEffectHandler(AuraType.Unk324)]
        [AuraEffectHandler(AuraType.ModBlind)]
        [AuraEffectHandler(AuraType.Unk335)]
        [AuraEffectHandler(AuraType.MountRestrictions)]
        [AuraEffectHandler(AuraType.IncreaseSkillGainChance)]
        [AuraEffectHandler(AuraType.ModResurrectedHealthByGuildMember)]
        [AuraEffectHandler(AuraType.ModAutoattackDamage)]
        [AuraEffectHandler(AuraType.ModSpellCooldownByHaste)]
        [AuraEffectHandler(AuraType.ModGatheringItemsGainedPercent)]
        [AuraEffectHandler(AuraType.Unk351)]
        [AuraEffectHandler(AuraType.Unk352)]
        [AuraEffectHandler(AuraType.ModCamouflage)]
        [AuraEffectHandler(AuraType.Unk354)]
        [AuraEffectHandler(AuraType.Unk356)]
        [AuraEffectHandler(AuraType.EnableBoss1UnitFrame)]
        [AuraEffectHandler(AuraType.WorgenAlteredForm)]
        [AuraEffectHandler(AuraType.Unk359)]
        [AuraEffectHandler(AuraType.ProcTriggerSpellCopy)]
        [AuraEffectHandler(AuraType.OverrideAutoattackWithMeleeSpell)]
        [AuraEffectHandler(AuraType.ModNextSpell)]
        [AuraEffectHandler(AuraType.MaxFarClipPlane)]
        [AuraEffectHandler(AuraType.EnablePowerBarTimer)]
        [AuraEffectHandler(AuraType.SetFairFarClip)]
        void HandleUnused(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply) { }

        /**************************************/
        /***       VISIBILITY & PHASES      ***/
        /**************************************/
        [AuraEffectHandler(AuraType.ModInvisibilityDetect)]
        void HandleModInvisibilityDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            InvisibilityType type = (InvisibilityType)GetMiscValue();

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
        void HandleModInvisibility(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            InvisibilityType type = (InvisibilityType)GetMiscValue();

            if (apply)
            {
                // apply glow vision
                if (target.IsTypeId(TypeId.Player))
                    target.SetByteFlag(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetAuraVision, PlayerFieldByte2Flags.InvisibilityGlow);

                target.m_invisibility.AddFlag(type);
                target.m_invisibility.AddValue(type, GetAmount());
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModInvisibility))
                {
                    // if not have different invisibility auras.
                    // remove glow vision
                    if (target.IsTypeId(TypeId.Player))
                        target.RemoveByteFlag(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetAuraVision, PlayerFieldByte2Flags.InvisibilityGlow);

                    target.m_invisibility.DelFlag(type);
                }
                else
                {
                    bool found = false;
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
        void HandleModStealthDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            StealthType type = (StealthType)GetMiscValue();

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
        void HandleModStealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            StealthType type = (StealthType)GetMiscValue();

            if (apply)
            {
                target.m_stealth.AddFlag(type);
                target.m_stealth.AddValue(type, GetAmount());
                target.SetStandFlags(UnitStandFlags.Creep);
                if (target.IsTypeId(TypeId.Player))
                    target.SetByteFlag(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetAuraVision, PlayerFieldByte2Flags.Stealth);
            }
            else
            {
                target.m_stealth.AddValue(type, -GetAmount());

                if (!target.HasAuraType(AuraType.ModStealth)) // if last SPELL_AURA_MOD_STEALTH
                {
                    target.m_stealth.DelFlag(type);

                    target.RemoveStandFlags(UnitStandFlags.Creep);
                    if (target.IsTypeId(TypeId.Player))
                        target.RemoveByteFlag(ActivePlayerFields.Bytes2, PlayerFieldOffsets.FieldBytes2OffsetAuraVision, PlayerFieldByte2Flags.Stealth);
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
        void HandleModStealthLevel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            StealthType type = (StealthType)GetMiscValue();

            if (apply)
                target.m_stealth.AddValue(type, GetAmount());
            else
                target.m_stealth.AddValue(type, -GetAmount());

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.SpiritOfRedemption)]
        void HandleSpiritOfRedemption(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

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
                target.setDeathState(DeathState.JustDied);
        }

        [AuraEffectHandler(AuraType.Ghost)]
        void HandleAuraGhost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (apply)
            {
                target.SetFlag(PlayerFields.Flags, PlayerFlags.Ghost);
                target.m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
                target.m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
            }
            else
            {
                if (target.HasAuraType(AuraType.Ghost))
                    return;

                target.RemoveFlag(PlayerFields.Flags, PlayerFlags.Ghost);
                target.m_serverSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
                target.m_serverSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
            }
        }

        [AuraEffectHandler(AuraType.Phase)]
        void HandlePhase(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

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
        void HandlePhaseGroup(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

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

        /**********************/
        /***   UNIT MODEL   ***/
        /**********************/
        [AuraEffectHandler(AuraType.ModShapeshift)]
        void HandleAuraModShapeshift(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            SpellShapeshiftFormRecord shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetMiscValue());
            //ASSERT(shapeInfo, "Spell {0} uses unknown ShapeshiftForm (%u).", GetId(), GetMiscValue());

            Unit target = aurApp.GetTarget();
            ShapeShiftForm form = (ShapeShiftForm)GetMiscValue();

            uint modelid = target.GetModelForForm(form);

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
                            target.RemoveMovementImpairingAuras();

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

                ShapeShiftForm prevForm = target.GetShapeshiftForm();
                target.SetShapeshiftForm(form);
                // add the shapeshift aura's boosts
                if (prevForm != form)
                    HandleShapeshiftBoosts(target, true);

                if (modelid > 0)
                {
                    SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransForm());
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
                        target.RemoveMovementImpairingAuras();
                    }
                }

                if (modelid > 0)
                    target.RestoreDisplayId(target.IsMounted());

                switch (form)
                {
                    // Nordrassil Harness - bonus
                    case ShapeShiftForm.BearForm:
                    case ShapeShiftForm.CatForm:
                        AuraEffect dummy = target.GetAuraEffect(37315, 0);
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

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().InitDataForForm();
            else
                target.UpdateDisplayPower();

            if (target.GetClass() == Class.Druid)
            {
                // Dash
                AuraEffect aurEff = target.GetAuraEffect(AuraType.ModIncreaseSpeed, SpellFamilyNames.Druid, new FlagArray128(0, 0, 0x8));
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
        void HandleAuraTransform(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                // update active transform spell only when transform or shapeshift not set or not overwriting negative by positive case
                if (target.GetModelForForm(target.GetShapeshiftForm()) == 0 || !GetSpellInfo().IsPositive())
                {
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
                            uint modelid = ObjectManager.ChooseDisplayId(ci).CreatureDisplayID;
                            if (modelid != 0)
                                model_id = modelid;                     // Will use the default model here

                            target.SetDisplayId(model_id);

                            // Dragonmaw Illusion (set mount model also)
                            if (GetId() == 42016 && target.GetMountID() != 0 && !target.GetAuraEffectsByType(AuraType.ModIncreaseMountedFlightSpeed).Empty())
                                target.SetUInt32Value(UnitFields.MountDisplayId, 16314);
                        }
                    }
                }

                // update active transform spell only when transform or shapeshift not set or not overwriting negative by positive case
                SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransForm());
                if (transformSpellInfo == null || !GetSpellInfo().IsPositive() || transformSpellInfo.IsPositive())
                    target.setTransForm(GetId());

                // polymorph case
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real) && target.IsTypeId(TypeId.Player) && target.IsPolymorphed())
                {
                    // for players, start regeneration after 1s (in polymorph fast regeneration case)
                    // only if caster is Player (after patch 2.4.2)
                    if (GetCasterGUID().IsPlayer())
                        target.ToPlayer().setRegenTimerCount(1 * Time.InMilliseconds);

                    //dismount polymorphed target (after patch 2.4.2)
                    if (target.IsMounted())
                        target.RemoveAurasByType(AuraType.Mounted);
                }
            }
            else
            {
                if (target.GetTransForm() == GetId())
                    target.setTransForm(0);

                target.RestoreDisplayId(target.IsMounted());

                // Dragonmaw Illusion (restore mount model)
                if (GetId() == 42016 && target.GetMountID() == 16314)
                {
                    if (!target.GetAuraEffectsByType(AuraType.Mounted).Empty())
                    {
                        int cr_id = target.GetAuraEffectsByType(AuraType.Mounted)[0].GetMiscValue();
                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)cr_id);
                        if (ci != null)
                        {
                            CreatureModel model = ObjectManager.ChooseDisplayId(ci);
                            Global.ObjectMgr.GetCreatureModelRandomGender(ref model, ci);

                            target.SetUInt32Value(UnitFields.MountDisplayId, model.CreatureDisplayID);
                        }
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModScale)]
        [AuraEffectHandler(AuraType.ModScale2)]
        void HandleAuraModScale(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            float scale = target.GetFloatValue(ObjectFields.ScaleX);
            MathFunctions.ApplyPercentModFloatVar(ref scale, GetAmount(), apply);
            target.SetObjectScale(scale);
        }

        [AuraEffectHandler(AuraType.CloneCaster)]
        void HandleAuraCloneCaster(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                Unit caster = GetCaster();
                if (caster == null || caster == target)
                    return;

                // What must be cloned? at least display and scale
                target.SetDisplayId(caster.GetDisplayId());
                //target.SetObjectScale(caster.GetFloatValue(OBJECT_FIELD_SCALE_X)); // we need retail info about how scaling is handled (aura maybe?)
                target.SetFlag(UnitFields.Flags2, UnitFlags2.MirrorImage);
            }
            else
            {
                target.SetDisplayId(target.GetNativeDisplayId());
                target.RemoveFlag(UnitFields.Flags2, UnitFlags2.MirrorImage);
            }
        }

        /************************/
        /***      FIGHT       ***/
        /************************/
        [AuraEffectHandler(AuraType.FeignDeath)]
        void HandleFeignDeath(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                List<Unit> targets = new List<Unit>();
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
                target.getHostileRefManager().deleteReferences();

                // stop handling the effect if it was removed by linked event
                if (aurApp.HasRemoveMode())
                    return;
                // blizz like 2.0.x
                target.SetFlag(UnitFields.Flags, UnitFlags.Unk29);
                // blizz like 2.0.x
                target.SetFlag(UnitFields.Flags2, UnitFlags2.FeignDeath);
                // blizz like 2.0.x
                target.SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead);

                target.AddUnitState(UnitState.Died);
            }
            else
            {
                // blizz like 2.0.x
                target.RemoveFlag(UnitFields.Flags, UnitFlags.Unk29);
                // blizz like 2.0.x
                target.RemoveFlag(UnitFields.Flags2, UnitFlags2.FeignDeath);
                // blizz like 2.0.x
                target.RemoveFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead);

                target.ClearUnitState(UnitState.Died);
            }
        }

        [AuraEffectHandler(AuraType.ModUnattackable)]
        void HandleModUnattackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
            if (!apply && target.HasAuraType(AuraType.ModUnattackable))
                return;

            target.ApplyModFlag(UnitFields.Flags, UnitFlags.NonAttackable, apply);

            // call functions which may have additional effects after chainging state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                target.CombatStop();
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ImmuneOrLostSelection);
            }
        }

        [AuraEffectHandler(AuraType.ModDisarm)]
        void HandleAuraModDisarm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            AuraType type = GetAuraType();

            //Prevent handling aura twice
            if ((apply) ? target.GetAuraEffectsByType(type).Count > 1 : target.HasAuraType(type))
                return;
            UnitFields field;
            uint flag, slot;
            WeaponAttackType attType;
            switch (type)
            {
                case AuraType.ModDisarm:
                    field = UnitFields.Flags;
                    flag = (uint)UnitFlags.Disarmed;
                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.BaseAttack;
                    break;
                case AuraType.ModDisarmOffhand:
                    field = UnitFields.Flags2;
                    flag = (uint)UnitFlags2.DisarmOffhand;
                    slot = EquipmentSlot.OffHand;
                    attType = WeaponAttackType.OffAttack;
                    break;
                case AuraType.ModDisarmRanged:
                    field = UnitFields.Flags2;
                    flag = (uint)UnitFlags2.DisarmRanged;
                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.RangedAttack;
                    break;
                default:
                    return;
            }

            // if disarm aura is to be removed, remove the flag first to reapply damage/aura mods
            if (!apply)
                target.RemoveFlag(field, flag);

            // Handle damage modification, shapeshifted druids are not affected
            if (target.IsTypeId(TypeId.Player) && !target.IsInFeralForm())
            {
                Player player = target.ToPlayer();

                Item item = player.GetItemByPos(InventorySlots.Bag0, (byte)slot);
                if (item != null)
                {
                    WeaponAttackType attacktype = Player.GetAttackBySlot((byte)slot, item.GetTemplate().GetInventoryType());

                    player.ApplyItemDependentAuras(item, !apply);
                    if (attacktype < WeaponAttackType.Max)
                        player._ApplyWeaponDamage(slot, item, !apply);
                }
            }

            // if disarm effects should be applied, wait to set flag until damage mods are unapplied
            if (apply)
                target.SetFlag(field, flag);

            if (target.IsTypeId(TypeId.Unit) && target.ToCreature().GetCurrentEquipmentId() != 0)
                target.UpdateDamagePhysical(attType);
        }

        [AuraEffectHandler(AuraType.ModSilence)]
        void HandleAuraModSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetFlag(UnitFields.Flags, UnitFlags.Silenced);

                // call functions which may have additional effects after chainging state of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    Spell spell = target.GetCurrentSpell(i);
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

                target.RemoveFlag(UnitFields.Flags, UnitFlags.Silenced);
            }
        }

        [AuraEffectHandler(AuraType.ModPacify)]
        void HandleAuraModPacify(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetFlag(UnitFields.Flags, UnitFlags.Pacified);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModPacify) || target.HasAuraType(AuraType.ModPacifySilence))
                    return;
                target.RemoveFlag(UnitFields.Flags, UnitFlags.Pacified);
            }
        }

        [AuraEffectHandler(AuraType.ModPacifySilence)]
        void HandleAuraModPacifyAndSilence(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // Vengeance of the Blue Flight (@todo REMOVE THIS!)
            // @workaround
            if (m_spellInfo.Id == 45839)
            {
                if (apply)
                    target.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
                else
                    target.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable);
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

        [AuraEffectHandler(AuraType.AllowOnlyAbility)]
        void HandleAuraAllowOnlyAbility(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
            {
                if (apply)
                    target.SetFlag(PlayerFields.Flags, PlayerFlags.AllowOnlyAbility);
                else
                {
                    // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                    if (target.HasAuraType(AuraType.AllowOnlyAbility))
                        return;
                    target.RemoveFlag(PlayerFields.Flags, PlayerFlags.AllowOnlyAbility);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModNoActions)]
        void HandleAuraModNoActions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetFlag(UnitFields.Flags2, UnitFlags2.NoActions);

                // call functions which may have additional effects after chainging state of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    Spell spell = target.GetCurrentSpell(i);
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

                target.RemoveFlag(UnitFields.Flags2, UnitFlags2.NoActions);
            }
        }

        /****************************/
        /***      TRACKING        ***/
        /****************************/
        [AuraEffectHandler(AuraType.TrackCreatures)]
        void HandleAuraTrackCreatures(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (apply)
                target.SetFlag(ActivePlayerFields.TrackCreatures, 1 << (GetMiscValue() - 1));
            else
                target.RemoveFlag(ActivePlayerFields.TrackCreatures, 1 << (GetMiscValue() - 1));
        }

        [AuraEffectHandler(AuraType.TrackResources)]
        void HandleAuraTrackResources(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (apply)
                target.SetFlag(ActivePlayerFields.TrackResources, 1 << (GetMiscValue() - 1));
            else
                target.RemoveFlag(ActivePlayerFields.TrackResources, 1 << (GetMiscValue() - 1));
        }

        [AuraEffectHandler(AuraType.TrackStealthed)]
        void HandleAuraTrackStealthed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (!(apply))
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }
            target.ApplyModFlag(ActivePlayerFields.LocalFlags, PlayerLocalFlags.TrackStealthed, apply);
        }

        [AuraEffectHandler(AuraType.ModStalked)]
        void HandleAuraModStalked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // used by spells: Hunter's Mark, Mind Vision, Syndicate Tracker (MURP) DND
            if (apply)
                target.SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.TrackUnit);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (!target.HasAuraType(GetAuraType()))
                    target.RemoveFlag(ObjectFields.DynamicFlags, UnitDynFlags.TrackUnit);
            }

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.Untrackable)]
        void HandleAuraUntrackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetByteFlag(UnitFields.Bytes1, UnitBytes1Offsets.VisFlag, UnitStandFlags.Untrackable);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveByteFlag(UnitFields.Bytes1, UnitBytes1Offsets.VisFlag, UnitStandFlags.Untrackable);
            }
        }

        /****************************/
        /***  SKILLS & TALENTS    ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModSkill)]
        [AuraEffectHandler(AuraType.ModSkill2)]
        void HandleAuraModSkill(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Skill)))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            SkillType prot = (SkillType)GetMiscValue();
            int points = GetAmount();

            if (prot == SkillType.Defense)
                return;

            target.ModifySkillBonus(prot, (apply ? points : -points), GetAuraType() == AuraType.ModSkillTalent);
        }

        /****************************/
        /***       MOVEMENT       ***/
        /****************************/
        [AuraEffectHandler(AuraType.Mounted)]
        void HandleAuraMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                uint creatureEntry = (uint)GetMiscValue();
                uint displayId = 0;
                uint vehicleId = 0;

                MountRecord mountEntry = Global.DB2Mgr.GetMount(GetId());
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
                            List<MountXDisplayRecord> usableDisplays = mountDisplays.Where(mountDisplay =>
                            {
                                Player playerTarget = target.ToPlayer();
                                if (playerTarget)
                                {
                                    PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountDisplay.PlayerConditionID);
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
                    //if (MountVehicle const* mountVehicle = sObjectMgr->GetMountVehicle(mountEntry->Id))
                    //    creatureEntry = mountVehicle->VehicleCreatureId;
                }

                CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creatureEntry);
                if (creatureInfo != null)
                {
                    vehicleId = creatureInfo.VehicleId;

                    if (displayId == 0)
                    {
                        CreatureModel model = ObjectManager.ChooseDisplayId(creatureInfo);
                        Global.ObjectMgr.GetCreatureModelRandomGender(ref model, creatureInfo);
                        displayId = model.CreatureDisplayID;
                    }

                    //some spell has one aura of mount and one of vehicle
                    foreach (SpellEffectInfo effect in GetBase().GetSpellEffectInfos())
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
        void HandleAuraAllowFlight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    return;
            }

            if (target.SetCanFly(apply))
                if (!apply && !target.IsLevitating())
                    target.GetMotionMaster().MoveFall();
        }

        [AuraEffectHandler(AuraType.WaterWalk)]
        void HandleAuraWaterWalk(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetWaterWalking(apply);
        }

        [AuraEffectHandler(AuraType.FeatherFall)]
        void HandleAuraFeatherFall(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

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
        void HandleAuraHover(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetHover(apply);    //! Sets movementflags
        }

        [AuraEffectHandler(AuraType.WaterBreathing)]
        void HandleWaterBreathing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // update timers in client
            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateMirrorTimers();
        }

        [AuraEffectHandler(AuraType.ForceMoveForward)]
        void HandleForceMoveForward(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetFlag(UnitFields.Flags2, UnitFlags2.ForceMove);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveFlag(UnitFields.Flags2, UnitFlags2.ForceMove);
            }
        }

        [AuraEffectHandler(AuraType.CanTurnWhileFalling)]
        void HandleAuraCanTurnWhileFalling(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetCanTurnWhileFalling(apply);
        }

        /****************************/
        /***        THREAT        ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModThreat)]
        void HandleModThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            for (byte i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    MathFunctions.ApplyPercentModFloatVar(ref target.m_threatModifier[i], GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModTotalThreat)]
        void HandleAuraModTotalThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsAlive() || !target.IsTypeId(TypeId.Player))
                return;

            Unit caster = GetCaster();
            if (caster != null && caster.IsAlive())
                target.getHostileRefManager().addTempThreat(GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModTaunt)]
        void HandleModTaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsAlive() || !target.CanHaveThreatList())
                return;

            Unit caster = GetCaster();
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
        void HandleModConfuse(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Confused);
        }

        [AuraEffectHandler(AuraType.ModFear)]
        void HandleModFear(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Fleeing);
        }

        [AuraEffectHandler(AuraType.ModStun)]
        void HandleAuraModStun(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Stunned);
        }

        [AuraEffectHandler(AuraType.ModRoot)]
        [AuraEffectHandler(AuraType.ModRoot2)]
        void HandleAuraModRoot(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Root);
        }

        [AuraEffectHandler(AuraType.PreventsFleeing)]
        void HandlePreventFleeing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

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
        void HandleModPossess(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

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
        void HandleModPossessPet(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            // Used by spell "Eyes of the Beast"

            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit caster = GetCaster();
            if (caster == null || !caster.IsTypeId(TypeId.Player))
                return;

            Unit target = aurApp.GetTarget();
            if (!target.IsTypeId(TypeId.Unit) || !target.IsPet())
                return;

            Pet pet = target.ToPet();

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
        void HandleModCharm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            if (apply)
                target.SetCharmedBy(caster, CharmType.Charm, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        [AuraEffectHandler(AuraType.AoeCharm)]
        void HandleCharmConvert(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

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
        void HandleAuraControlVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            if (!target.IsVehicle())
                return;

            Unit caster = GetCaster();
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
                        caster.ToCreature().RemoveCorpse();
                }

                if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmount))
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
        void HandleAuraModIncreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
        }

        [AuraEffectHandler(AuraType.ModIncreaseMountedSpeed)]
        [AuraEffectHandler(AuraType.ModMountedSpeedAlways)]
        [AuraEffectHandler(AuraType.ModMountedSpeedNotStack)]
        void HandleAuraModIncreaseMountedSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleAuraModIncreaseSpeed(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModIncreaseVehicleFlightSpeed)]
        [AuraEffectHandler(AuraType.ModIncreaseMountedFlightSpeed)]
        [AuraEffectHandler(AuraType.ModIncreaseFlightSpeed)]
        [AuraEffectHandler(AuraType.ModMountedFlightSpeedAlways)]
        [AuraEffectHandler(AuraType.ModVehicleSpeedAlways)]
        [AuraEffectHandler(AuraType.ModFlightSpeedNotStack)]
        void HandleAuraModIncreaseFlightSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                target.UpdateSpeed(UnitMoveType.Flight);

            //! Update ability to fly
            if (GetAuraType() == AuraType.ModIncreaseMountedFlightSpeed)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask) && (apply || (!target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !target.HasAuraType(AuraType.Fly))))
                {
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
                    if (apply && target.HasAuraEffect(42016, 0) && target.GetMountID() != 0)
                        target.SetUInt32Value(UnitFields.MountDisplayId, 16314);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModIncreaseSwimSpeed)]
        void HandleAuraModIncreaseSwimSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Swim);
        }

        [AuraEffectHandler(AuraType.ModDecreaseSpeed)]
        void HandleAuraModDecreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
            target.UpdateSpeed(UnitMoveType.Swim);
            target.UpdateSpeed(UnitMoveType.Flight);
            target.UpdateSpeed(UnitMoveType.RunBack);
            target.UpdateSpeed(UnitMoveType.SwimBack);
            target.UpdateSpeed(UnitMoveType.FlightBack);
        }

        [AuraEffectHandler(AuraType.UseNormalMovementSpeed)]
        void HandleAuraModUseNormalSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
            target.UpdateSpeed(UnitMoveType.Swim);
            target.UpdateSpeed(UnitMoveType.Flight);
        }

        [AuraEffectHandler(AuraType.ModMinimumSpeedRate)]
        void HandleAuraModMinimumSpeedRate(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
        }

        /*********************************************************/
        /***                     IMMUNITY                      ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.MechanicImmunityMask)]
        void HandleModMechanicImmunityMask(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.MechanicImmunity)]
        void HandleModMechanicImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.EffectImmunity)]
        void HandleAuraModEffectImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            // when removing flag aura, handle flag drop
            Player player = target.ToPlayer();
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
        void HandleAuraModStateImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.SchoolImmunity)]
        void HandleAuraModSchoolImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            if (GetSpellInfo().Mechanic == Mechanics.Banish)
            {
                if (apply)
                    target.AddUnitState(UnitState.Isolated);
                else
                {
                    bool banishFound = false;
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
        void HandleAuraModDmgImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.DispelImmunity)]
        void HandleAuraModDispelImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        /*********************************************************/
        /***                  MODIFY STATS                     ***/
        /*********************************************************/

        /********************************/
        /***        RESISTANCE        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModResistance)]
        void HandleAuraModResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            for (byte x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                if (Convert.ToBoolean(GetMiscValue() & (1 << x)))
                    target.HandleStatModifier(UnitMods.ResistanceStart + x, UnitModifierType.TotalValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModBaseResistancePct)]
        void HandleAuraModBaseResistancePCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
            {
                //pets only have base armor
                if (target.IsPet() && Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                    target.HandleStatModifier(UnitMods.Armor, UnitModifierType.BasePCT, GetAmount(), apply);
            }
            else
            {
                for (byte x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << x)))
                        target.HandleStatModifier(UnitMods.ResistanceStart + x, UnitModifierType.BasePCT, GetAmount(), apply);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModResistancePct)]
        void HandleModResistancePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModResistancePct);
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            for (byte i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
            {
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                {
                    if (spellGroupVal != 0)
                        target.HandleStatModifier(UnitMods.ResistanceStart + i, UnitModifierType.TotalPCT, (float)spellGroupVal, !apply);

                    target.HandleStatModifier(UnitMods.ResistanceStart + i, UnitModifierType.TotalPCT, GetAmount(), apply);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModBaseResistance)]
        void HandleModBaseResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
            {
                //only pets have base stats
                if (target.IsPet() && Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                    target.HandleStatModifier(UnitMods.Armor, UnitModifierType.TotalValue, GetAmount(), apply);
            }
            else
            {
                for (byte i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                        target.HandleStatModifier(UnitMods.ResistanceStart + i, UnitModifierType.TotalValue, GetAmount(), apply);
            }
        }

        [AuraEffectHandler(AuraType.ModTargetResistance)]
        void HandleModTargetResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            // applied to damage as HandleNoImmediateEffect in Unit.CalcAbsorbResist and Unit.CalcArmorReducedDamage

            // show armor penetration
            if (target.IsTypeId(TypeId.Player) && Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                target.ApplyModUInt32Value(ActivePlayerFields.ModTargetPhysicalResistance, GetAmount(), apply);

            // show as spell penetration only full spell penetration bonuses (all resistances except armor and holy
            if (target.IsTypeId(TypeId.Player) && ((SpellSchoolMask)GetMiscValue() & SpellSchoolMask.Spell) == SpellSchoolMask.Spell)
                target.ApplyModUInt32Value(ActivePlayerFields.ModTargetResistance, GetAmount(), apply);
        }

        /********************************/
        /***           STAT           ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModStat)]
        void HandleAuraModStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            if (GetMiscValue() < -2 || GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Spell {0} effect {1} has an unsupported misc value ({2}) for SPELL_AURA_MOD_STAT ", GetId(), GetEffIndex(), GetMiscValue());
                return;
            }

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModStat, true, GetMiscValue());
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            for (var i = Stats.Strength; i < Stats.Max; i++)
            {
                // -1 or -2 is all stats (misc < -2 checked in function beginning)
                if (GetMiscValue() < 0 || GetMiscValue() == (int)i)
                {
                    if (spellGroupVal != 0)
                    {
                        target.HandleStatModifier((UnitMods.StatStart + (int)i), UnitModifierType.TotalValue, (float)spellGroupVal, !apply);
                        if (target.IsTypeId(TypeId.Player) || target.IsPet())
                            target.ApplyStatBuffMod(i, spellGroupVal, !apply);
                    }

                    target.HandleStatModifier(UnitMods.StatStart + (int)i, UnitModifierType.TotalValue, GetAmount(), apply);
                    if (target.IsTypeId(TypeId.Player) || target.IsPet())
                        target.ApplyStatBuffMod(i, GetAmount(), apply);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModPercentStat)]
        void HandleModPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (GetMiscValue() < -1 || GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Misc Value for SPELL_AURA_MOD_PERCENT_STAT not valid");
                return;
            }

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
                return;

            for (int i = (int)Stats.Strength; i < (int)Stats.Max; ++i)
            {
                if (GetMiscValue() == i || GetMiscValue() == -1)
                    target.HandleStatModifier(UnitMods.StatStart + i, UnitModifierType.BasePCT, (float)m_amount, apply);
            }
        }

        [AuraEffectHandler(AuraType.ModSpellDamageOfStatPercent)]
        void HandleModSpellDamagePercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Magic damage modifiers implemented in Unit.SpellDamageBonus
            // This information for client side use only
            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModSpellHealingOfStatPercent)]
        void HandleModSpellHealingPercentFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModSpellDamageOfAttackPower)]
        void HandleModSpellDamagePercentFromAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Magic damage modifiers implemented in Unit.SpellDamageBonus
            // This information for client side use only
            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModSpellHealingOfAttackPower)]
        void HandleModSpellHealingPercentFromAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Recalculate bonus
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModHealingDone)]
        void HandleModHealingDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;
            // implemented in Unit.SpellHealingBonus
            // this information is for client side only
            target.ToPlayer().UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.ModHealingDonePercent)]
        void HandleModHealingDonePct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player)
                player.UpdateHealingDonePercentMod();
        }

        [AuraEffectHandler(AuraType.ModTotalStatPercentage)]
        void HandleModTotalPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModTotalStatPercentage, true, -1);
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
            {
                for (int i = (int)Stats.Strength; i < (int)Stats.Max; i++)
                {
                    if (GetMiscValue() == i || GetMiscValue() == -1) // affect the same stats
                    {
                        target.HandleStatModifier((UnitMods.StatStart + i), UnitModifierType.TotalPCT, spellGroupVal, !apply);
                        if (target.IsTypeId(TypeId.Player) || target.IsPet())
                            target.ApplyStatPercentBuffMod((Stats)i, spellGroupVal, !apply);
                    }
                }
            }

            // save current health state
            float healthPct = target.GetHealthPct();
            bool alive = target.IsAlive();

            for (int i = (int)Stats.Strength; i < (int)Stats.Max; i++)
            {
                if (Convert.ToBoolean(GetMiscValueB() & 1 << i) || GetMiscValueB() == 0) // 0 is also used for all stats
                {
                    int spellGroupVal2 = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModTotalStatPercentage, true, i);
                    if (Math.Abs(spellGroupVal2) >= Math.Abs(GetAmount()))
                        continue;

                    if (spellGroupVal2 != 0)
                    {
                        target.HandleStatModifier(UnitMods.StatStart + i, UnitModifierType.TotalPCT, spellGroupVal2, !apply);
                        if (target.IsTypeId(TypeId.Player) || target.IsPet())
                            target.ApplyStatPercentBuffMod((Stats)i, spellGroupVal2, !apply);
                    }

                    target.HandleStatModifier(UnitMods.StatStart + i, UnitModifierType.TotalPCT, GetAmount(), apply);
                    if (target.IsTypeId(TypeId.Player) || target.IsPet())
                        target.ApplyStatPercentBuffMod((Stats)i, GetAmount(), apply);
                }
            }

            // recalculate current HP/MP after applying aura modifications (only for spells with SPELL_ATTR0_UNK4 0x00000010 flag)
            // this check is total bullshit i think
            if (Convert.ToBoolean(GetMiscValueB() & 1 << (int)Stats.Stamina) && m_spellInfo.HasAttribute(SpellAttr0.Ability))
                target.SetHealth((uint)Math.Max((healthPct * target.GetMaxHealth() * 0.01f), (alive ? 1 : 0)));
        }

        [AuraEffectHandler(AuraType.ModResistanceOfStatPercent)]
        void HandleAuraModResistenceOfStatPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (GetMiscValue() != (int)SpellSchoolMask.Normal)
            {
                // support required adding replace UpdateArmor by loop by UpdateResistence at intellect update
                // and include in UpdateResistence same code as in UpdateArmor for aura mod apply.
                Log.outError(LogFilter.Spells, "Aura SPELL_AURA_MOD_RESISTANCE_OF_STAT_PERCENT(182) does not work for non-armor type resistances!");
                return;
            }

            // Recalculate Armor
            target.UpdateArmor();
        }

        [AuraEffectHandler(AuraType.ModExpertise)]
        void HandleAuraModExpertise(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateExpertise(WeaponAttackType.BaseAttack);
            target.ToPlayer().UpdateExpertise(WeaponAttackType.OffAttack);
        }

        [AuraEffectHandler(AuraType.ModStatBonusPct)]
        void HandleModStatBonusPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (GetMiscValue() < -1 || GetMiscValue() > 4)
            {
                Log.outError(LogFilter.Spells, "WARNING: Misc Value for SPELL_AURA_MOD_STAT_BONUS_PCT not valid");
                return;
            }

            // only players have base stats
            if (!target.IsTypeId(TypeId.Player))
                return;

            for (Stats stat = Stats.Strength; stat < Stats.Max; ++stat)
            {
                if (GetMiscValue() == (int)stat || GetMiscValue() == -1)
                {
                    target.HandleStatModifier(UnitMods.StatStart + (int)stat, UnitModifierType.BasePCTExcludeCreate, m_amount, apply);
                    target.ApplyStatPercentBuffMod(stat, m_amount, apply);
                }
            }
        }

        [AuraEffectHandler(AuraType.OverrideSpellPowerByApPct)]
        void HandleOverrideSpellPowerByAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            target.ApplyModSignedFloatValue(ActivePlayerFields.OverrideSpellPowerByApPct, m_amount, apply);
            target.UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.OverrideAttackPowerBySpPct)]
        void HandleOverrideAttackPowerBySpellPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (!target)
                return;

            target.ApplyModSignedFloatValue(ActivePlayerFields.OverrideApBySpellPowerPercent, m_amount, apply);
            target.UpdateAttackPowerAndDamage();
            target.UpdateAttackPowerAndDamage(true);
        }

        [AuraEffectHandler(AuraType.ModVersatility)]
        void HandleModVersatilityByPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target)
            {
                target.SetStatFloatValue(ActivePlayerFields.VersatilityBonus, target.GetTotalAuraModifier(AuraType.ModVersatility));
                target.UpdateHealingDonePercentMod();
                target.UpdateVersatilityDamageDone();
            }
        }

        [AuraEffectHandler(AuraType.ModMaxPower)]
        void HandleAuraModMaxPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            PowerType power = (PowerType)GetMiscValue();
            UnitMods unitMod = (UnitMods)(UnitMods.PowerStart + (int)power);

            target.HandleStatModifier(unitMod, UnitModifierType.TotalValue, GetAmount(), apply);
        }

        /********************************/
        /***      HEAL & ENERGIZE     ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerRegen)]
        void HandleModPowerRegen(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

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
        void HandleModPowerRegenPCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleModPowerRegen(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModManaRegenFromStat)]
        void HandleModManaRegen(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            //Note: an increase in regen does NOT cause threat.
            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModManaRegenPct)]
        void HandleModManaRegenPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModIncreaseHealth)]
        [AuraEffectHandler(AuraType.ModIncreaseHealth2)]
        [AuraEffectHandler(AuraType.ModMaxHealth)]
        void HandleAuraModIncreaseHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.HandleStatModifier(UnitMods.Health, UnitModifierType.TotalValue, GetAmount(), apply);
                target.ModifyHealth(GetAmount());
            }
            else
            {
                if (target.GetHealth() > 0)
                {
                    int value = (int)Math.Min(target.GetHealth() - 1, (ulong)GetAmount());
                    target.ModifyHealth(-value);
                }

                target.HandleStatModifier(UnitMods.Health, UnitModifierType.TotalValue, GetAmount(), apply);
            }
        }

        void HandleAuraModIncreaseMaxHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            float percent = target.GetHealthPct();

            target.HandleStatModifier(UnitMods.Health, UnitModifierType.TotalValue, GetAmount(), apply);

            // refresh percentage
            if (target.GetHealth() > 0)
            {
                uint newHealth = (uint)Math.Max(target.CountPctFromMaxHealth((int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModIncreaseEnergy)]
        void HandleAuraModIncreaseEnergy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();
            PowerType powerType = (PowerType)GetMiscValue();

            UnitMods unitMod = (UnitMods.PowerStart + (int)powerType);
            target.HandleStatModifier(unitMod, UnitModifierType.TotalValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModIncreaseEnergyPercent)]
        void HandleAuraModIncreaseEnergyPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();
            PowerType powerType = (PowerType)GetMiscValue();

            UnitMods unitMod = UnitMods.PowerStart + (int)powerType;

            // Save old powers for further calculation
            int oldPower = target.GetPower(powerType);
            int oldMaxPower = target.GetMaxPower(powerType);

            // Handle aura effect for max power
            target.HandleStatModifier(unitMod, UnitModifierType.TotalPCT, GetAmount(), apply);

            // Calculate the current power change
            int change = target.GetMaxPower(powerType) - oldMaxPower;
            change = (oldPower + change) - target.GetPower(powerType);
        }

        [AuraEffectHandler(AuraType.ModIncreaseHealthPercent)]
        void HandleAuraModIncreaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            // Unit will keep hp% after MaxHealth being modified if unit is alive.
            float percent = target.GetHealthPct();
            target.HandleStatModifier(UnitMods.Health, UnitModifierType.TotalPCT, GetAmount(), apply);

            if (target.GetHealth() > 0)
            {
                uint newHealth = (uint)Math.Max(target.CountPctFromMaxHealth((int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseHealthPct)]
        void HandleAuraIncreaseBaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            target.HandleStatModifier(UnitMods.Health, UnitModifierType.BasePCT, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModBaseManaPct)]
        void HandleAuraModIncreaseBaseManaPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().HandleStatModifier(UnitMods.Mana, UnitModifierType.BasePCT, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModPowerDisplay)]
        void HandleAuraModPowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
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
        void HandleAuraModOverridePowerDisplay(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            PowerDisplayRecord powerDisplay = CliDB.PowerDisplayStorage.LookupByKey(GetMiscValue());
            if (powerDisplay == null)
                return;

            Unit target = aurApp.GetTarget();
            if (target.GetPowerIndex((PowerType)powerDisplay.ActualType) == (int)PowerType.Max)
                return;

            if (apply)
            {
                target.RemoveAurasByType(GetAuraType(), ObjectGuid.Empty, GetBase());
                target.SetUInt32Value(UnitFields.OverrideDisplayPowerId, powerDisplay.Id);
            }
            else
                target.SetUInt32Value(UnitFields.OverrideDisplayPowerId, 0);
        }

        [AuraEffectHandler(AuraType.ModMaxPowerPct)]
        void HandleAuraModMaxPowerPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();
            if (!target.IsPlayer())
                return;

            PowerType powerType = (PowerType)GetMiscValue();
            UnitMods unitMod = UnitMods.PowerStart + (int)powerType;

            // Save old powers for further calculation
            int oldPower = target.GetPower(powerType);
            int oldMaxPower = target.GetMaxPower(powerType);

            // Handle aura effect for max power
            target.HandleStatModifier(unitMod, UnitModifierType.TotalPCT, (float)GetAmount(), apply);

            // Calculate the current power change
            int change = target.GetMaxPower(powerType) - oldMaxPower;
            change = (oldPower + change) - target.GetPower(powerType);
            target.ModifyPower(powerType, change);
        }

        /********************************/
        /***          FIGHT           ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModParryPercent)]
        void HandleAuraModParryPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateParryPercentage();
        }

        [AuraEffectHandler(AuraType.ModDodgePercent)]
        void HandleAuraModDodgePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateDodgePercentage();
        }

        [AuraEffectHandler(AuraType.ModBlockPercent)]
        void HandleAuraModBlockPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().UpdateBlockPercentage();
        }

        [AuraEffectHandler(AuraType.InterruptRegen)]
        void HandleAuraModRegenInterrupt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleModManaRegen(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModWeaponCritPercent)]
        void HandleAuraModWeaponCritPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (!target)
                return;

            target.HandleBaseModValue(BaseModGroup.CritPercentage, BaseModType.FlatMod, GetAmount(), apply);
            target.HandleBaseModValue(BaseModGroup.OffhandCritPercentage, BaseModType.FlatMod, GetAmount(), apply);
            target.HandleBaseModValue(BaseModGroup.RangedCritPercentage, BaseModType.FlatMod, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModHitChance)]
        void HandleModHitChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
            {
                target.ToPlayer().UpdateMeleeHitChances();
                target.ToPlayer().UpdateRangedHitChances();
            }
            else
            {
                target.m_modMeleeHitChance += (apply) ? GetAmount() : (-GetAmount());
                target.m_modRangedHitChance += (apply) ? GetAmount() : (-GetAmount());
            }
        }

        [AuraEffectHandler(AuraType.ModSpellHitChance)]
        void HandleModSpellHitChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateSpellHitChances();
            else
                target.m_modSpellHitChance += (apply) ? GetAmount() : (-GetAmount());
        }

        [AuraEffectHandler(AuraType.ModSpellCritChance)]
        void HandleModSpellCritChance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateSpellCritChance();
            else
                target.m_baseSpellCritChance += (apply) ? GetAmount() : -GetAmount();
        }

        [AuraEffectHandler(AuraType.ModCritPct)]
        void HandleAuraModCritPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
            {
                target.m_baseSpellCritChance += (apply) ? GetAmount() : -GetAmount();
                return;
            }

            target.ToPlayer().HandleBaseModValue(BaseModGroup.CritPercentage, BaseModType.FlatMod, GetAmount(), apply);
            target.ToPlayer().HandleBaseModValue(BaseModGroup.OffhandCritPercentage, BaseModType.FlatMod, GetAmount(), apply);
            target.ToPlayer().HandleBaseModValue(BaseModGroup.RangedCritPercentage, BaseModType.FlatMod, GetAmount(), apply);

            // included in Player.UpdateSpellCritChance calculation
            target.ToPlayer().UpdateSpellCritChance();
        }

        /********************************/
        /***         ATTACK SPEED     ***/
        /********************************/
        [AuraEffectHandler(AuraType.HasteSpells)]
        [AuraEffectHandler(AuraType.ModCastingSpeedNotStack)]
        void HandleModCastingSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            target.ApplyCastTimePercentMod(GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModMeleeRangedHaste)]
        [AuraEffectHandler(AuraType.ModMeleeRangedHaste2)]
        void HandleModMeleeRangedSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            //! ToDo: Haste auras with the same handler _CAN'T_ stack together
            Unit target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.OffAttack, GetAmount(), apply);
            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.MeleeSlow)]
        [AuraEffectHandler(AuraType.ModSpeedSlowAll)]
        void HandleModCombatSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.MeleeSlow);
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
        void HandleModAttackSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.BaseAttack, GetAmount(), apply);
            target.UpdateDamagePhysical(WeaponAttackType.BaseAttack);
        }

        [AuraEffectHandler(AuraType.ModMeleeHaste)]
        [AuraEffectHandler(AuraType.ModMeleeHaste2)]
        [AuraEffectHandler(AuraType.ModMeleeHaste3)]
        void HandleModMeleeSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            //! ToDo: Haste auras with the same handler _CAN'T_ stack together
            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModMeleeHaste);
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
        void HandleAuraModRangedHaste(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            //! ToDo: Haste auras with the same handler _CAN'T_ stack together
            Unit target = aurApp.GetTarget();

            target.ApplyAttackTimePercentMod(WeaponAttackType.RangedAttack, GetAmount(), apply);
        }

        /********************************/
        /***       COMBAT RATING      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModRating)]
        void HandleModRating(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            for (int rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & (1 << rating)))
                    target.ToPlayer().ApplyRatingMod((CombatRating)rating, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRatingFromStat)]
        void HandleModRatingFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Just recalculate ratings
            for (int rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & (1 << rating)))
                    target.ToPlayer().UpdateRating((CombatRating)rating);
        }

        [AuraEffectHandler(AuraType.ModRatingPct)]
        void HandleModRatingPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // Just recalculate ratings
            for (int rating = 0; rating < (int)CombatRating.Max; ++rating)
                if (Convert.ToBoolean(GetMiscValue() & (1 << rating)))
                    target.ToPlayer().UpdateRating((CombatRating)rating);
        }

        /********************************/
        /***        ATTACK POWER      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModAttackPower)]
        void HandleAuraModAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            target.HandleStatModifier(UnitMods.AttackPower, UnitModifierType.TotalValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPower)]
        void HandleAuraModRangedAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if ((target.getClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            target.HandleStatModifier(UnitMods.AttackPowerRanged, UnitModifierType.TotalValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackPowerPct)]
        void HandleAuraModAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            //UNIT_FIELD_ATTACK_POWER_MULTIPLIER = multiplier - 1
            target.HandleStatModifier(UnitMods.AttackPower, UnitModifierType.TotalPCT, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPowerPct)]
        void HandleAuraModRangedAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if ((target.getClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            //UNIT_FIELD_RANGED_ATTACK_POWER_MULTIPLIER = multiplier - 1
            target.HandleStatModifier(UnitMods.AttackPowerRanged, UnitModifierType.TotalPCT, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackPowerOfArmor)]
        void HandleAuraModAttackPowerOfArmor(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            // Recalculate bonus
            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateAttackPowerAndDamage(false);
        }

        /********************************/
        /***        DAMAGE BONUS      ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModDamageDone)]
        void HandleModDamageDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if ((GetMiscValue() & (int)SpellSchoolMask.Normal) != 0)
            {
                target.HandleStatModifier(UnitMods.DamageMainHand, UnitModifierType.TotalValue, GetAmount(), apply);
                target.HandleStatModifier(UnitMods.DamageOffHand, UnitModifierType.TotalValue, GetAmount(), apply);
                target.HandleStatModifier(UnitMods.DamageRanged, UnitModifierType.TotalValue, GetAmount(), apply);
            }

            // Magic damage modifiers implemented in Unit::SpellBaseDamageBonusDone
            // This information for client side use only
            if (target.IsTypeId(TypeId.Player))
            {
                ActivePlayerFields baseField = GetAmount() >= 0 ? ActivePlayerFields.ModDamageDonePos : ActivePlayerFields.ModDamageDoneNeg;
                for (int i = 0; i < (int)SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                        target.ApplyModInt32Value(baseField + i, GetAmount(), apply);
                }

                Guardian pet = target.ToPlayer().GetGuardianPet();
                if (pet)
                    pet.UpdateAttackPowerAndDamage();
            }
        }

        [AuraEffectHandler(AuraType.ModDamagePercentDone)]
        void HandleModDamagePercentDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();
            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, AuraType.ModDamagePercentDone);
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
            {
                if (spellGroupVal != 0)
                {
                    target.HandleStatModifier(UnitMods.DamageMainHand, UnitModifierType.TotalPCT, spellGroupVal, !apply);
                    target.HandleStatModifier(UnitMods.DamageOffHand, UnitModifierType.TotalPCT, spellGroupVal, !apply);
                    target.HandleStatModifier(UnitMods.DamageRanged, UnitModifierType.TotalPCT, spellGroupVal, !apply);
                }

                target.HandleStatModifier(UnitMods.DamageMainHand, UnitModifierType.TotalPCT, GetAmount(), apply);
                target.HandleStatModifier(UnitMods.DamageOffHand, UnitModifierType.TotalPCT, GetAmount(), apply);
                target.HandleStatModifier(UnitMods.DamageRanged, UnitModifierType.TotalPCT, GetAmount(), apply);
            }

            if (target.IsTypeId(TypeId.Player))
            {
                for (int i = 0; i < (int)SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    {
                        if (spellGroupVal != 0)
                            target.ApplyPercentModFloatValue(ActivePlayerFields.ModDamageDonePct + i, spellGroupVal, !apply);

                        target.ApplyPercentModFloatValue(ActivePlayerFields.ModDamageDonePct + i, GetAmount(), apply);
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModOffhandDamagePct)]
        void HandleModOffhandDamagePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            target.HandleStatModifier(UnitMods.DamageOffHand, UnitModifierType.TotalPCT, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModShieldBlockvaluePct)]
        void HandleShieldBlockValue(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player)
                player.HandleBaseModValue(BaseModGroup.ShieldBlockValue, BaseModType.PCTmod, GetAmount(), apply);
        }

        /********************************/
        /***        POWER COST        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerCostSchoolPct)]
        void HandleModPowerCostPCT(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            float amount = MathFunctions.CalculatePct(1.0f, GetAmount());
            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    target.ApplyModSignedFloatValue(UnitFields.PowerCostMultiplier + i, amount, apply);
        }

        [AuraEffectHandler(AuraType.ModPowerCostSchool)]
        void HandleModPowerCost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    target.ApplyModUInt32Value(UnitFields.PowerCostModifier + i, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ArenaPreparation)]
        void HandleArenaPreparation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetFlag(UnitFields.Flags, UnitFlags.Preparation);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveFlag(UnitFields.Flags, UnitFlags.Preparation);
            }
        }

        [AuraEffectHandler(AuraType.NoReagentUse)]
        void HandleNoReagentUseAura(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            FlagArray128 mask = new FlagArray128();
            var noReagent = target.GetAuraEffectsByType(AuraType.NoReagentUse);
            foreach (var eff in noReagent)
            {
                SpellEffectInfo effect = eff.GetSpellEffectInfo();
                if (effect != null)
                    mask |= effect.SpellClassMask;
            }

            target.SetUInt32Value(ActivePlayerFields.NoReagentCost, mask[0]);
            target.SetUInt32Value(ActivePlayerFields.NoReagentCost + 1, mask[1]);
            target.SetUInt32Value(ActivePlayerFields.NoReagentCost + 2, mask[2]);
        }

        [AuraEffectHandler(AuraType.RetainComboPoints)]
        void HandleAuraRetainComboPoints(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // combo points was added in SPELL_EFFECT_ADD_COMBO_POINTS handler
            // remove only if aura expire by time (in case combo points amount change aura removed without combo points lost)
            if (!apply && aurApp.GetRemoveMode() == AuraRemoveMode.Expire)
                target.ToPlayer().AddComboPoints((sbyte)-GetAmount());
        }

        /*********************************************************/
        /***                    OTHERS                         ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.Dummy)]
        void HandleAuraDummy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Reapply)))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                // pet auras
                PetAura petSpell = Global.SpellMgr.GetPetAura(GetId(), m_effIndex);
                if (petSpell != null)
                {
                    if (apply)
                        target.AddPetAura(petSpell);
                    else
                        target.RemovePetAura(petSpell);
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
                                target.AddThreat(caster, 10.0f);
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
                                Aura owner_aura = target.GetAura(34027, GetCasterGUID());
                                Aura pet_aura = pet.GetAura(58914, GetCasterGUID());
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
                                        BattleField bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(target.GetZoneId());
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
                                            uint spellId = (uint)(target.GetMap().IsHeroic() ? 46163 : 44190);

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
                                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(spellId);

                                        for (uint i = 0; i < spell.StackAmount; ++i)
                                            caster.CastSpell(target, spell.Id, true, null, null, GetCasterGUID());
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
                                        SpellInfo spell = Global.SpellMgr.GetSpellInfo(spellId);
                                        for (uint i = 0; i < spell.StackAmount; ++i)
                                            caster.CastSpell(target, spell.Id, true, null, null, GetCasterGUID());
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
        void HandleChannelDeathItem(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (apply || aurApp.GetRemoveMode() != AuraRemoveMode.Death)
                return;

            Unit caster = GetCaster();

            if (caster == null || !caster.IsTypeId(TypeId.Player))
                return;

            Player plCaster = caster.ToPlayer();
            Unit target = aurApp.GetTarget();

            // Item amount
            if (GetAmount() <= 0)
                return;

            if (GetSpellEffectInfo().ItemType == 0)
                return;

            // Soul Shard
            if (GetSpellEffectInfo().ItemType == 6265)
            {
                // Soul Shard only from units that grant XP or honor
                if (!plCaster.isHonorOrXPTarget(target) ||
                    (target.IsTypeId(TypeId.Unit) && !target.ToCreature().isTappedBy(plCaster)))
                    return;
            }

            //Adding items
            uint noSpaceForCount = 0;
            uint count = (uint)m_amount;

            List<ItemPosCount> dest = new List<ItemPosCount>();
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
        void HandleBindSight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            if (caster == null || !caster.IsTypeId(TypeId.Player))
                return;

            caster.ToPlayer().SetViewpoint(target, apply);
        }

        [AuraEffectHandler(AuraType.ForceReaction)]
        void HandleForceReaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            Player player = target.ToPlayer();
            if (player == null)
                return;

            uint factionId = (uint)GetMiscValue();
            ReputationRank factionRank = (ReputationRank)m_amount;

            player.GetReputationMgr().ApplyForceReaction(factionId, factionRank, apply);
            player.GetReputationMgr().SendForceReactions();

            // stop fighting if at apply forced rank friendly or at remove real rank friendly
            if ((apply && factionRank >= ReputationRank.Friendly) || (!apply && player.GetReputationRank(factionId) >= ReputationRank.Friendly))
                player.StopAttackFaction(factionId);
        }

        [AuraEffectHandler(AuraType.Empathy)]
        void HandleAuraEmpathy(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            if (target.GetCreatureType() == CreatureType.Beast)
                target.ApplyModUInt32Value(ObjectFields.DynamicFlags, (int)UnitDynFlags.SpecialInfo, apply);
        }

        [AuraEffectHandler(AuraType.ModFaction)]
        void HandleAuraModFaction(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetFaction((uint)GetMiscValue());
                if (target.IsTypeId(TypeId.Player))
                    target.RemoveFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
            }
            else
            {
                target.RestoreFaction();
                if (target.IsTypeId(TypeId.Player))
                    target.SetFlag(UnitFields.Flags, UnitFlags.PvpAttackable);
            }
        }

        [AuraEffectHandler(AuraType.ComprehendLanguage)]
        void HandleComprehendLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetFlag(UnitFields.Flags2, UnitFlags2.ComprehendLang);
            else
            {
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveFlag(UnitFields.Flags2, UnitFlags2.ComprehendLang);
            }
        }

        [AuraEffectHandler(AuraType.Linked)]
        void HandleAuraLinked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            Unit target = aurApp.GetTarget();

            uint triggeredSpellId = GetSpellEffectInfo().TriggerSpell;
            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggeredSpellId);
            if (triggeredSpellInfo == null)
                return;

            Unit caster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, target.GetMap().GetDifficultyID()) ? GetCaster() : target;
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
                    ObjectGuid casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, caster.GetMap().GetDifficultyID()) ? GetCasterGUID() : target.GetGUID();
                    target.RemoveAura(triggeredSpellId, casterGUID, 0, aurApp.GetRemoveMode());
                }
            }
            else if (mode.HasAnyFlag(AuraEffectHandleModes.Reapply) && apply)
            {
                ObjectGuid casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, caster.GetMap().GetDifficultyID()) ? GetCasterGUID() : target.GetGUID();
                // change the stack amount to be equal to stack amount of our aura
                Aura triggeredAura = target.GetAura(triggeredSpellId, casterGUID);
                if (triggeredAura != null)
                    triggeredAura.ModStackAmount(GetBase().GetStackAmount() - triggeredAura.GetStackAmount());
            }
        }

        [AuraEffectHandler(AuraType.OpenStable)]
        void HandleAuraOpenStable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player) || !target.IsInWorld)
                return;

            if (apply)
                target.ToPlayer().GetSession().SendStablePet(target.GetGUID());

            // client auto close stable dialog at !apply aura
        }

        [AuraEffectHandler(AuraType.ModFakeInebriate)]
        void HandleAuraModFakeInebriation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.m_invisibilityDetect.AddFlag(InvisibilityType.Drunk);
                target.m_invisibilityDetect.AddValue(InvisibilityType.Drunk, GetAmount());

                if (target.IsTypeId(TypeId.Player))
                {
                    int oldval = target.ToPlayer().GetInt32Value(PlayerFields.FakeInebriation);
                    target.ToPlayer().SetInt32Value(PlayerFields.FakeInebriation, oldval + GetAmount());
                }
            }
            else
            {
                bool removeDetect = !target.HasAuraType(AuraType.ModFakeInebriate);

                target.m_invisibilityDetect.AddValue(InvisibilityType.Drunk, -GetAmount());

                if (target.IsTypeId(TypeId.Player))
                {
                    int oldval = target.ToPlayer().GetInt32Value(PlayerFields.FakeInebriation);
                    target.ToPlayer().SetInt32Value(PlayerFields.FakeInebriation, oldval - GetAmount());

                    if (removeDetect)
                        removeDetect = target.ToPlayer().GetDrunkValue() == 0;
                }

                if (removeDetect)
                    target.m_invisibilityDetect.DelFlag(InvisibilityType.Drunk);
            }

            // call functions which may have additional effects after chainging state of unit
            target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.OverrideSpells)]
        void HandleAuraOverrideSpells(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null || !target.IsInWorld)
                return;

            uint overrideId = (uint)GetMiscValue();

            if (apply)
            {
                target.SetUInt16Value(ActivePlayerFields.Bytes3, PlayerFieldOffsets.FieldBytes3OffsetOverrideSpellsIdUint16Offset, (ushort)overrideId);
                OverrideSpellDataRecord overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);
                if (overrideSpells != null)
                {
                    for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                    {
                        uint spellId = overrideSpells.Spells[i];
                        if (spellId != 0)
                            target.AddTemporarySpell(spellId);
                    }
                }
            }
            else
            {
                target.SetUInt16Value(ActivePlayerFields.Bytes3, PlayerFieldOffsets.FieldBytes3OffsetOverrideSpellsIdUint16Offset, 0);
                OverrideSpellDataRecord overrideSpells = CliDB.OverrideSpellDataStorage.LookupByKey(overrideId);
                if (overrideSpells != null)
                {
                    for (byte i = 0; i < SharedConst.MaxOverrideSpell; ++i)
                    {
                        uint spellId = overrideSpells.Spells[i];
                        if (spellId != 0)
                            target.RemoveTemporarySpell(spellId);
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.SetVehicleId)]
        void HandleAuraSetVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsInWorld)
                return;

            int vehicleId = GetMiscValue();

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
        void HandlePreventResurrection(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (!aurApp.GetTarget().IsTypeId(TypeId.Player))
                return;

            if (apply)
                aurApp.GetTarget().RemoveByteFlag(ActivePlayerFields.LocalFlags, 0, PlayerLocalFlags.ReleaseTimer);
            else if (!aurApp.GetTarget().GetMap().Instanceable())
                aurApp.GetTarget().SetByteFlag(ActivePlayerFields.LocalFlags, 0, PlayerLocalFlags.ReleaseTimer);
        }

        [AuraEffectHandler(AuraType.Mastery)]
        void HandleMastery(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            target.UpdateMastery();
        }

        void HandlePeriodicDummyAuraTick(Unit target, Unit caster)
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
                                Unit veh = target.GetVehicleBase();
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
                                    int mod = (rage < 100) ? rage : 100;
                                    int points = target.CalculateSpellDamage(target, GetSpellInfo(), 1);
                                    int regen = (int)((long)target.GetMaxHealth() * (mod * points / 10) / 1000);
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
                        if (!Convert.ToBoolean(target.GetUInt32Value(UnitFields.Flags) & (uint)(UnitFlags.Stunned | UnitFlags.Fleeing | UnitFlags.Silenced)))
                            target.RemoveAurasDueToSpell(52179);
                        break;
                    }
                    break;
                case SpellFamilyNames.Deathknight:
                    switch (GetId())
                    {
                        case 49016: // Hysteria
                            uint damage = (uint)target.CountPctFromMaxHealth(1);
                            target.DealDamage(target, damage, null, DamageEffectType.NoDamage, SpellSchoolMask.Normal, null, false);
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        void HandlePeriodicTriggerSpellAuraTick(Unit target, Unit caster)
        {
            // generic casting code with custom spells and target/caster customs
            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId);
            SpellInfo auraSpellInfo = GetSpellInfo();
            uint auraId = auraSpellInfo.Id;

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
                                            uint heal = (uint)caster.CountPctFromMaxHealth(10);
                                            HealInfo healInfo = new HealInfo(caster, target, heal, auraSpellInfo, auraSpellInfo.GetSchoolMask());
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
                                            Player player = caster.ToPlayer();
                                            Creature creature = target.ToCreature();
                                            // missing lootid has been reported on startup - just return
                                            if (creature.GetCreatureTemplate().SkinLootId == 0)
                                                return;

                                            player.AutoStoreLoot(creature.GetCreatureTemplate().SkinLootId, LootStorage.Skinning, true);

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

                                        Creature creatureTarget = target.ToCreature();

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
                                        bool all = true;
                                        for (int i = (int)SummonSlot.Totem; i < SharedConst.MaxSummonSlot; ++i)
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
                            Aura permafrostAura = target.GetAura(66193);
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
                                Creature permafrostCasterCreature = permafrostCaster.ToCreature();
                                if (permafrostCasterCreature != null)
                                    permafrostCasterCreature.DespawnOrUnsummon(3000);

                                target.CastSpell(target, 66181, false);
                                target.RemoveAllAuras();
                                Creature targetCreature = target.ToCreature();
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
            triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId);

            if (triggeredSpellInfo != null)
            {
                Unit triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, target.GetMap().GetDifficultyID()) ? caster : target;
                if (triggerCaster != null)
                {
                    triggerCaster.CastSpell(target, triggeredSpellInfo, true, null, this);
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
            {
                Creature c = target.ToCreature();
                if (c == null || caster == null || !Global.ScriptMgr.OnDummyEffect(caster, GetId(), GetEffIndex(), target.ToCreature()) ||
                    !c.GetAI().sOnDummyEffect(caster, GetId(), GetEffIndex()))
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
            }
        }

        void HandlePeriodicTriggerSpellWithValueAuraTick(Unit target, Unit caster)
        {
            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId);
            if (triggeredSpellInfo != null)
            {
                Unit triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo, target.GetMap().GetDifficultyID()) ? caster : target;
                if (triggerCaster != null)
                {
                    int basepoints = GetAmount();
                    triggerCaster.CastCustomSpell(target, triggerSpellId, basepoints, basepoints, basepoints, true, null, this);
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
        }

        void HandlePeriodicDamageAurasTick(Unit target, Unit caster)
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
                            SpellEffectInfo effect = GetSpellInfo().GetEffect(1);
                            if (effect != null)
                            {
                                int percent = effect.CalcValue(caster);
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

            CleanDamage cleanDamage = new CleanDamage(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

            // AOE spells are not affected by the new periodic system.
            bool isAreaAura = GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura);
            // ignore non positive values (can be result apply spellmods to aura damage
            uint damage = (uint)(isAreaAura ? Math.Max(GetAmount(), 0) : m_damage);

            // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
            if (isAreaAura)
                Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);

            if (GetAuraType() == AuraType.PeriodicDamage)
            {
                if (isAreaAura)
                    damage = (uint)(caster.SpellDamageBonusDone(target, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()) * caster.SpellDamagePctDone(target, m_spellInfo, DamageEffectType.DOT));
                damage = target.SpellDamageBonusTaken(caster, GetSpellInfo(), damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount());

                // Calculate armor mitigation
                if (caster.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo(), (sbyte)GetEffIndex()))
                {
                    uint damageReductedArmor = caster.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo());
                    cleanDamage.mitigated_damage += damage - damageReductedArmor;
                    damage = damageReductedArmor;
                }

                // There is a Chance to make a Soul Shard when Drain soul does damage
                if (GetSpellInfo().SpellFamilyName == SpellFamilyNames.Warlock && GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
                {
                    if (caster.IsTypeId(TypeId.Player) && caster.ToPlayer().isHonorOrXPTarget(target))
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
            }
            else
                damage = (uint)target.CountPctFromMaxHealth((int)damage);

            if (!m_spellInfo.HasAttribute(SpellAttr4.FixedDamage))
            {
                if (GetSpellEffectInfo().IsTargetingArea() || isAreaAura)
                {
                    damage = (uint)(damage * target.GetTotalAuraMultiplierByMiscMask(AuraType.ModAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                    if (!caster.IsTypeId(TypeId.Player))
                        damage = (uint)(damage * target.GetTotalAuraMultiplierByMiscMask(AuraType.ModCreatureAoeDamageAvoidance, (uint)m_spellInfo.SchoolMask));
                }
            }

            bool crit = false;
            if (CanPeriodicTickCrit(caster))
                crit = RandomHelper.randChance(isAreaAura ? caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()) : m_critChance);

            if (crit)
                damage = caster.SpellCriticalDamageBonus(m_spellInfo, damage, target);

            uint dmg = damage;
            if (!GetSpellInfo().HasAttribute(SpellAttr4.FixedDamage))
                caster.ApplyResilience(target, ref dmg);
            damage = dmg;

            DamageInfo damageInfo = new DamageInfo(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
            caster.CalcAbsorbResist(damageInfo);
            damage = damageInfo.GetDamage();

            uint absorb = damageInfo.GetAbsorb();
            uint resist = damageInfo.GetResist();
            caster.DealDamageMods(target, ref damage, ref absorb);

            // Set trigger flag
            ProcFlags procAttacker = ProcFlags.DonePeriodic;
            ProcFlags procVictim = ProcFlags.TakenPeriodic;
            ProcFlagsHit hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim |= ProcFlags.TakenDamage;
            }

            int overkill = (int)(damage - target.GetHealth());
            if (overkill < 0)
                overkill = 0;

            SpellPeriodicAuraLogInfo pInfo = new SpellPeriodicAuraLogInfo(this, damage, dmg, (uint)overkill, absorb, resist, 0.0f, crit);

            caster.DealDamage(target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), true);

            caster.ProcSkillsAndAuras(target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.None, hitMask, null, damageInfo, null);
            target.SendPeriodicAuraLog(pInfo);
        }

        void HandlePeriodicHealthLeechAuraTick(Unit target, Unit caster)
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

            CleanDamage cleanDamage = new CleanDamage(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

            bool isAreaAura = GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura);
            // ignore negative values (can be result apply spellmods to aura damage
            uint damage = (uint)(isAreaAura ? Math.Max(GetAmount(), 0) : m_damage);

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
                uint damageReductedArmor = caster.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo());
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

            bool crit = false;
            if (CanPeriodicTickCrit(caster))
                crit = RandomHelper.randChance(isAreaAura ? caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()) : m_critChance);

            if (crit)
                damage = caster.SpellCriticalDamageBonus(m_spellInfo, damage, target);

            uint dmg = damage;
            if (!GetSpellInfo().HasAttribute(SpellAttr4.FixedDamage))
                caster.ApplyResilience(target, ref dmg);
            damage = dmg;

            DamageInfo damageInfo = new DamageInfo(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
            caster.CalcAbsorbResist(damageInfo);

            uint absorb = damageInfo.GetAbsorb();
            uint resist = damageInfo.GetResist();

            // SendSpellNonMeleeDamageLog expects non-absorbed/non-resisted damage
            SpellNonMeleeDamage log = new SpellNonMeleeDamage(caster, target, GetId(), GetBase().GetSpellXSpellVisualId(), GetSpellInfo().GetSchoolMask(), GetBase().GetCastGUID());
            log.damage = damage;
            log.originalDamage = dmg;
            log.absorb = absorb;
            log.resist = resist;
            log.periodicLog = true;
            if (crit)
                log.HitInfo |= HitInfo.CriticalHit;

            // Set trigger flag
            ProcFlags procAttacker = ProcFlags.DonePeriodic;
            ProcFlags procVictim = ProcFlags.TakenPeriodic;
            ProcFlagsHit hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim |= ProcFlags.TakenDamage;
            }

            int new_damage = (int)caster.DealDamage(target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), false);
            if (caster.IsAlive())
            {
                caster.ProcSkillsAndAuras(target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.None, hitMask, null, damageInfo, null);

                float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

                uint heal = (caster.SpellHealingBonusDone(caster, GetSpellInfo(), (uint)(new_damage * gainMultiplier), DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()));
                heal = (caster.SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()));

                HealInfo healInfo = new HealInfo(caster, caster, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
                caster.HealBySpell(healInfo);

                caster.getHostileRefManager().threatAssist(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());
                caster.ProcSkillsAndAuras(caster, ProcFlags.DonePeriodic, ProcFlags.TakenPeriodic, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.None, hitMask, null, null, healInfo);
            }

            caster.SendSpellNonMeleeDamageLog(log);
        }

        void HandlePeriodicHealthFunnelAuraTick(Unit target, Unit caster)
        {
            if (caster == null || !caster.IsAlive() || !target.IsAlive())
                return;

            if (target.HasUnitState(UnitState.Isolated))
            {
                SendTickImmune(target, caster);
                return;
            }

            uint damage = (uint)Math.Max(GetAmount(), 0);
            // do not kill health donator
            if (caster.GetHealth() < damage)
                damage = (uint)caster.GetHealth() - 1;
            if (damage == 0)
                return;

            caster.ModifyHealth(-(int)damage);
            Log.outDebug(LogFilter.Spells, "PeriodicTick: donator {0} target {1} damage {2}.", caster.GetEntry(), target.GetEntry(), damage);

            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            damage = (uint)(damage * gainMultiplier);

            HealInfo healInfo = new HealInfo(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.HealBySpell(healInfo);
            caster.ProcSkillsAndAuras(target, ProcFlags.DonePeriodic, ProcFlags.TakenPeriodic, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.None, ProcFlagsHit.Normal, null, null, healInfo);
        }

        void HandlePeriodicHealAurasTick(Unit target, Unit caster)
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

            bool isAreaAura = GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura);
            // ignore negative values (can be result apply spellmods to aura damage
            int damage = isAreaAura ? Math.Max(GetAmount(), 0) : m_damage;

            if (GetAuraType() == AuraType.ObsModHealth)
            {
                // Taken mods
                float TakenTotalMod = 1.0f;

                // Tenacity increase healing % taken
                AuraEffect Tenacity = target.GetAuraEffect(58549, 0);
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
                // Wild Growth = amount + (6 - 2*doneTicks) * ticks* amount / 100
                if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Druid && m_spellInfo.SpellFamilyFlags & new FlagArray128(0, 0x04000000, 0, 0))
                {
                    int addition = (int)((damage * GetTotalTicks()) * ((6 - (2 * (GetTickNumber() - 1))) / 100));

                    // Item - Druid T10 Restoration 2P Bonus
                    AuraEffect aurEff = caster.GetAuraEffect(70658, 0);
                    if (aurEff != null)
                        // divided by 50 instead of 100 because calculated as for every 2 tick
                        addition += Math.Abs((addition * aurEff.GetAmount()) / 50);

                    damage += addition;
                }
                if (isAreaAura)
                    damage = (int)(caster.SpellHealingBonusDone(target, GetSpellInfo(), (uint)damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount()) * caster.SpellHealingPctDone(target, m_spellInfo));
                damage = (int)target.SpellHealingBonusTaken(caster, GetSpellInfo(), (uint)damage, DamageEffectType.DOT, GetSpellEffectInfo(), GetBase().GetStackAmount());
            }

            bool crit = false;
            if (CanPeriodicTickCrit(caster))
                crit = RandomHelper.randChance(isAreaAura ? caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()) : m_critChance);

            if (crit)
                damage = caster.SpellCriticalHealingBonus(m_spellInfo, damage, target);

            Log.outDebug(LogFilter.Spells, "PeriodicTick: {0} (TypeId: {1}) heal of {2} (TypeId: {3}) for {4} health inflicted by {5}",
                GetCasterGUID().ToString(), GetCaster().GetTypeId(), target.GetGUID().ToString(), target.GetTypeId(), damage, GetId());

            uint heal = (uint)damage;

            HealInfo healInfo = new HealInfo(caster, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.CalcHealAbsorb(healInfo);
            caster.DealHeal(healInfo);

            SpellPeriodicAuraLogInfo pInfo = new SpellPeriodicAuraLogInfo(this, heal, (uint)damage, heal - healInfo.GetEffectiveHeal(), healInfo.GetAbsorb(), 0, 0.0f, crit);
            target.SendPeriodicAuraLog(pInfo);

            target.getHostileRefManager().threatAssist(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());

            // %-based heal - does not proc auras
            if (GetAuraType() == AuraType.ObsModHealth)
                return;

            ProcFlags procAttacker = ProcFlags.DonePeriodic;
            ProcFlags procVictim = ProcFlags.TakenPeriodic;
            ProcFlagsHit hitMask = crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
            // ignore item heals
            if (GetBase().GetCastItemGUID().IsEmpty())
                caster.ProcSkillsAndAuras(target, procAttacker, procVictim, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.None, hitMask, null, null, healInfo);
        }

        void HandlePeriodicManaLeechAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

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
            int drainAmount = Math.Max(m_amount, 0);

            int drainedAmount = -target.ModifyPower(powerType, -drainAmount);
            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            SpellPeriodicAuraLogInfo pInfo = new SpellPeriodicAuraLogInfo(this, (uint)drainedAmount, (uint)drainAmount, 0, 0, 0, gainMultiplier, false);

            int gainAmount = (int)(drainedAmount * gainMultiplier);
            int gainedAmount = 0;
            if (gainAmount != 0)
            {
                gainedAmount = caster.ModifyPower(powerType, gainAmount);
                target.AddThreat(caster, gainedAmount * 0.5f, GetSpellInfo().GetSchoolMask(), GetSpellInfo());
            }

            // Drain Mana
            if (m_spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag<uint>(0x00000010))
            {
                int manaFeedVal = 0;
                AuraEffect aurEff = GetBase().GetEffect(1);
                if (aurEff != null)
                    manaFeedVal = aurEff.GetAmount();
                // Mana Feed - Drain Mana
                if (manaFeedVal > 0)
                {
                    int feedAmount = MathFunctions.CalculatePct(gainedAmount, manaFeedVal);
                    caster.CastCustomSpell(caster, 32554, feedAmount, 0, 0, true, null, this);
                }
            }

            target.SendPeriodicAuraLog(pInfo);
        }

        void HandleObsModPowerAuraTick(Unit target, Unit caster)
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
            int amount = Math.Max(m_amount, 0) * target.GetMaxPower(powerType) / 100;

            SpellPeriodicAuraLogInfo pInfo = new SpellPeriodicAuraLogInfo(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);

            int gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.getHostileRefManager().threatAssist(caster, gain * 0.5f, GetSpellInfo());

            target.SendPeriodicAuraLog(pInfo);
        }

        void HandlePeriodicEnergizeAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();
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
            int amount = Math.Max(m_amount, 0);

            SpellPeriodicAuraLogInfo pInfo = new SpellPeriodicAuraLogInfo(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);
            int gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.getHostileRefManager().threatAssist(caster, gain * 0.5f, GetSpellInfo());

            target.SendPeriodicAuraLog(pInfo);
        }

        void HandlePeriodicPowerBurnAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

            if (caster == null || !target.IsAlive() || target.GetPowerType() != powerType)
                return;

            if (target.HasUnitState(UnitState.Isolated) || target.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // ignore negative values (can be result apply spellmods to aura damage
            int damage = Math.Max(m_amount, 0);

            uint gain = (uint)(-target.ModifyPower(powerType, -damage));

            float dmgMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            SpellInfo spellProto = GetSpellInfo();
            // maybe has to be sent different to client, but not by SMSG_PERIODICAURALOG
            SpellNonMeleeDamage damageInfo = new SpellNonMeleeDamage(caster, target, spellProto.Id, GetBase().GetSpellXSpellVisualId(), spellProto.SchoolMask, GetBase().GetCastGUID());
            // no SpellDamageBonus for burn mana
            caster.CalculateSpellDamageTaken(damageInfo, (int)(gain * dmgMultiplier), spellProto);

            caster.DealDamageMods(damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

            // Set trigger flag
            ProcFlags procAttacker = ProcFlags.DonePeriodic;
            ProcFlags procVictim = ProcFlags.TakenPeriodic;
            ProcFlagsHit hitMask = Unit.createProcHitMask(damageInfo, SpellMissInfo.None);
            ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.NoDmgHeal;
            if (damageInfo.damage != 0)
            {
                procVictim |= ProcFlags.TakenDamage;
                spellTypeMask |= ProcFlagsSpellType.Damage;
            }

            caster.DealSpellDamage(damageInfo, true);

            DamageInfo dotDamageInfo = new DamageInfo(damageInfo, DamageEffectType.DOT, WeaponAttackType.BaseAttack, hitMask);
            caster.ProcSkillsAndAuras(target, procAttacker, procVictim, spellTypeMask, ProcFlagsSpellPhase.None, hitMask, null, dotDamageInfo, null);

            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }

        void HandleBreakableCCAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            int damageLeft = (int)(GetAmount() - eventInfo.GetDamageInfo().GetDamage());

            if (damageLeft <= 0)
                aurApp.GetTarget().RemoveAura(aurApp);
            else
                ChangeAmount(damageLeft);
        }

        void HandleProcTriggerSpellAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit triggerCaster = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();

            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId);
            if (triggeredSpellInfo != null)
            {
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellAuraProc: Triggering spell {0} from aura {1} proc", triggeredSpellInfo.Id, GetId());
                triggerCaster.CastSpell(triggerTarget, triggeredSpellInfo, true, null, this);
            }
            else
                Log.outError(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellAuraProc: Could not trigger spell {0} from aura {1} proc, because the spell does not have an entry in Spell.dbc.", triggerSpellId, GetId());
        }

        void HandleProcTriggerSpellWithValueAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit triggerCaster = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();

            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId);
            if (triggeredSpellInfo != null)
            {
                int basepoints0 = GetAmount();
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Triggering spell {0} with value {1} from aura {2} proc", triggeredSpellInfo.Id, basepoints0, GetId());
                triggerCaster.CastCustomSpell(triggerTarget, triggerSpellId, basepoints0, 0, 0, true, null, this);
            }
            else
                Log.outError(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Could not trigger spell {0} from aura {1} proc, because the spell does not have an entry in Spell.dbc.", triggerSpellId, GetId());
        }

        public void HandleProcTriggerDamageAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit target = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();
            if (triggerTarget.HasUnitState(UnitState.Isolated) || triggerTarget.IsImmunedToDamage(GetSpellInfo()))
            {
                SendTickImmune(triggerTarget, target);
                return;
            }

            SpellNonMeleeDamage damageInfo = new SpellNonMeleeDamage(target, triggerTarget, GetId(), GetBase().GetSpellXSpellVisualId(), GetSpellInfo().SchoolMask, GetBase().GetCastGUID());
            int damage = (int)target.SpellDamageBonusDone(triggerTarget, GetSpellInfo(), (uint)GetAmount(), DamageEffectType.SpellDirect, GetSpellEffectInfo());
            damage = (int)triggerTarget.SpellDamageBonusTaken(target, GetSpellInfo(), (uint)damage, DamageEffectType.SpellDirect, GetSpellEffectInfo());
            target.CalculateSpellDamageTaken(damageInfo, damage, GetSpellInfo());
            target.DealDamageMods(damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);
            target.DealSpellDamage(damageInfo, true);
            target.SendSpellNonMeleeDamageLog(damageInfo);
        }

        [AuraEffectHandler(AuraType.ForceWeather)]
        void HandleAuraForceWeather(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SendPacket(new WeatherPkt((WeatherState)GetMiscValue(), 1.0f));
            else
                target.GetMap().SendZoneWeather(target.GetZoneId(), target);
        }

        [AuraEffectHandler(AuraType.EnableAltPower)]
        void HandleEnableAltPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            int altPowerId = GetMiscValue();
            UnitPowerBarRecord powerEntry = CliDB.UnitPowerBarStorage.LookupByKey(altPowerId);
            if (powerEntry == null)
                return;

            if (apply)
                aurApp.GetTarget().SetMaxPower(PowerType.AlternatePower, (int)powerEntry.MaxPower);
            else
                aurApp.GetTarget().SetMaxPower(PowerType.AlternatePower, 0);
        }

        [AuraEffectHandler(AuraType.ModSpellCategoryCooldown)]
        void HandleModSpellCategoryCooldown(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player)
                player.SendSpellCategoryCooldowns();
        }

        [AuraEffectHandler(AuraType.ShowConfirmationPrompt)]
        [AuraEffectHandler(AuraType.ShowConfirmationPromptWithDifficulty)]
        void HandleShowConfirmationPrompt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (!player)
                return;

            if (apply)
                player.AddTemporarySpell(_effectInfo.TriggerSpell);
            else
                player.RemoveTemporarySpell(_effectInfo.TriggerSpell);
        }

        [AuraEffectHandler(AuraType.OverridePetSpecs)]
        void HandleOverridePetSpecs(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (!player)
                return;

            if (player.GetClass() != Class.Hunter)
                return;

            Pet pet = player.GetPet();
            if (!pet)
                return;

            ChrSpecializationRecord currSpec = CliDB.ChrSpecializationStorage.LookupByKey(pet.GetSpecialization());
            if (currSpec == null)
                return;

            pet.SetSpecialization(Global.DB2Mgr.GetChrSpecializationByIndex(apply ? Class.Max : 0, currSpec.OrderIndex).Id);
        }

        [AuraEffectHandler(AuraType.AllowUsingGameobjectsWhileMounted)]
        void HandleAllowUsingGameobjectsWhileMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (!aurApp.GetTarget().IsTypeId(TypeId.Player))
                return;

            if (apply)
                aurApp.GetTarget().SetFlag(ActivePlayerFields.LocalFlags, PlayerLocalFlags.CanUseObjectsMounted);
            else if (!aurApp.GetTarget().HasAuraType(AuraType.AllowUsingGameobjectsWhileMounted))
                aurApp.GetTarget().RemoveFlag(ActivePlayerFields.LocalFlags, PlayerLocalFlags.CanUseObjectsMounted);
        }

        [AuraEffectHandler(AuraType.PlayScene)]
        void HandlePlayScene(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
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
        void HandleCreateAreaTrigger(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                AreaTrigger.CreateAreaTrigger((uint)GetMiscValue(), GetCaster(), target, GetSpellInfo(), target, GetBase().GetDuration(), GetBase().GetSpellXSpellVisualId(), ObjectGuid.Empty, this);
            }
            else
            {
                Unit caster = GetCaster();
                if (caster)
                    caster.RemoveAreaTrigger(this);
            }
        }

        [AuraEffectHandler(AuraType.PvpTalents)]
        void HandleAuraPvpTalents(AuraApplication auraApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = auraApp.GetTarget().ToPlayer();
            if (target)
            {
                if (apply)
                    target.TogglePvpTalents(true);
                else if (!target.HasAuraType(AuraType.PvpTalents))
                    target.TogglePvpTalents(false);
            }
        }

        [AuraEffectHandler(AuraType.LinkedSummon)]
        void HandleLinkedSummon(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            SpellInfo triggerSpellInfo = Global.SpellMgr.GetSpellInfo(GetSpellEffectInfo().TriggerSpell);
            if (triggerSpellInfo == null)
                return;

            // on apply cast summon spell
            if (apply)
                target.CastSpell(target, triggerSpellInfo, true, null, this);
            // on unapply we need to search for and remove the summoned creature
            else
            {
                List<uint> summonedEntries = new List<uint>();
                foreach (var spellEffect in triggerSpellInfo.GetEffectsForDifficulty(target.GetMap().GetDifficultyID()))
                {
                    if (spellEffect != null && spellEffect.Effect == SpellEffectName.Summon)
                    {
                        uint summonEntry = (uint)spellEffect.MiscValue;
                        if (summonEntry != 0)
                            summonedEntries.Add(summonEntry);
                        
                    }
                }

                // we don't know if there can be multiple summons for the same effect, so consider only 1 summon for each effect
                // most of the spells have multiple effects with the same summon spell id for multiple spawns, so right now it's safe to assume there's only 1 spawn per effect
                foreach (uint summonEntry in summonedEntries)
                {
                    List<Creature> nearbyEntries = new List<Creature>();
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
                            TempSummon tempSummon = creature.ToTempSummon();
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
        #endregion
    }

    class AbsorbAuraOrderPred : Comparer<AuraEffect>
    {
        public override int Compare(AuraEffect aurEffA, AuraEffect aurEffB)
        {
            SpellInfo spellProtoA = aurEffA.GetSpellInfo();
            SpellInfo spellProtoB = aurEffB.GetSpellInfo();

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
