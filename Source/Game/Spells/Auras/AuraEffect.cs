// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Entities.GameObjectType;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Spells
{
    public class AuraEffect
    {
        public AuraEffect(Aura baseAura, SpellEffectInfo spellEfffectInfo, int? baseAmount, Unit caster)
        {
            auraBase = baseAura;
            m_spellInfo = baseAura.GetSpellInfo();
            _effectInfo = spellEfffectInfo;
            m_baseAmount = baseAmount.HasValue ? baseAmount.Value : _effectInfo.CalcBaseValue(caster, baseAura.GetAuraType() == AuraObjectType.Unit ? baseAura.GetOwner().ToUnit() : null, baseAura.GetCastItemId(), baseAura.GetCastItemLevel());
            m_canBeRecalculated = true;
            m_isPeriodic = false;

            CalculatePeriodic(caster, true, false);
            _amount = CalculateAmount(caster);
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

            if (!m_spellInfo.HasAttribute(SpellAttr8.MasteryAffectsPoints) || MathFunctions.fuzzyEq(GetSpellEffectInfo().BonusCoefficient, 0.0f))
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
                    if (m_spellInfo.ProcFlags == null)
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
                        amount = (int)mountCapability.Id;
                    break;
                case AuraType.ShowConfirmationPromptWithDifficulty:
                    if (caster != null)
                        amount = (int)caster.GetMap().GetDifficultyID();
                    m_canBeRecalculated = false;
                    break;
                default:
                    break;
            }

            if (GetSpellInfo().HasAttribute(SpellAttr10.RollingPeriodic))
            {
                var periodicAuras = GetBase().GetUnitOwner().GetAuraEffectsByType(GetAuraType());
                uint totalTicks = GetTotalTicks();
                if (totalTicks != 0)
                {
                    amount = periodicAuras.Aggregate(amount, (val, aurEff) =>
                    {
                        if (aurEff.GetCasterGUID() == GetCasterGUID() && aurEff.GetId() == GetId() && aurEff.GetEffIndex() == GetEffIndex())
                            val += (int)(aurEff.GetEstimatedAmount().GetValueOrDefault(aurEff.GetAmount()) * (float)aurEff.GetRemainingTicks() / (float)aurEff.GetTotalTicks());
                        return val;
                    });
                }
            }

            GetBase().CallScriptEffectCalcAmountHandlers(this, ref amount, ref m_canBeRecalculated);
            if (!GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.SuppressPointsStacking))
                amount *= GetBase().GetStackAmount();

            _estimatedAmount = CalculateEstimatedAmount(caster, amount);

            return amount;
        }

        public static float? CalculateEstimatedAmount(Unit caster, Unit target, SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, int amount, byte stack, AuraEffect aurEff)
        {
            uint stackAmountForBonuses = !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.SuppressPointsStacking) ? stack : 1u;

            switch (spellEffectInfo.ApplyAuraName)
            {
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicLeech:
                    return caster.SpellDamageBonusDone(target, spellInfo, amount, DamageEffectType.DOT, spellEffectInfo, stackAmountForBonuses, null, aurEff);
                case AuraType.PeriodicHeal:
                    return caster.SpellHealingBonusDone(target, spellInfo, amount, DamageEffectType.DOT, spellEffectInfo, stackAmountForBonuses, null, aurEff);
                default:
                    break;
            }

            return null;
        }

        public float? CalculateEstimatedAmount(Unit caster, int amount)
        {
            if (caster == null || GetBase().GetAuraType() != AuraObjectType.Unit)
                return null;

            return CalculateEstimatedAmount(caster, GetBase().GetUnitOwner(), GetSpellInfo(), GetSpellEffectInfo(), amount, GetBase().GetStackAmount(), this);
        }

        public static float CalculateEstimatedfTotalPeriodicAmount(Unit caster, Unit target, SpellInfo spellInfo, SpellEffectInfo spellEffectInfo, float amount, byte stack)
        {
            int maxDuration = Aura.CalcMaxDuration(spellInfo, caster, null);
            if (maxDuration <= 0)
                return 0.0f;

            int period = (int)spellEffectInfo.ApplyAuraPeriod;
            if (period == 0)
                return 0.0f;

            Player modOwner = caster.GetSpellModOwner();
            if (modOwner != null)
                modOwner.ApplySpellMod(spellInfo, SpellModOp.Period, ref period);

            // Haste modifies periodic time of channeled spells
            if (spellInfo.IsChanneled())
                caster.ModSpellDurationTime(spellInfo, ref period);
            else if (spellInfo.HasAttribute(SpellAttr5.SpellHasteAffectsPeriodic))
                period = (int)(period * caster.m_unitData.ModCastingSpeed);
            else if (spellInfo.HasAttribute(SpellAttr8.MeleeHasteAffectsPeriodic))
                period = (int)(period * caster.m_unitData.ModHaste);

            if (period == 0)
                return 0.0f;

            float totalTicks = (float)maxDuration / period;
            if (spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                totalTicks += 1.0f;

            return totalTicks * CalculateEstimatedAmount(caster, target, spellInfo, spellEffectInfo, (int)amount, stack, null).GetValueOrDefault(amount);
        }

        public uint GetTotalTicks()
        {
            uint totalTicks = 0;
            if (_period != 0 && !GetBase().IsPermanent())
            {
                totalTicks = (uint)(GetBase().GetMaxDuration() / _period);
                if (m_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                    ++totalTicks;
            }

            return totalTicks;
        }

        void ResetPeriodic(bool resetPeriodicTimer = false)
        {
            _ticksDone = 0;
            if (resetPeriodicTimer)
            {
                _periodicTimer = 0;
                // Start periodic on next tick or at aura apply
                if (m_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
                    _periodicTimer = _period;
            }
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
                    m_isPeriodic = true;
                    break;
                default:
                    break;
            }

            GetBase().CallScriptEffectCalcPeriodicHandlers(this, ref m_isPeriodic, ref _period);

            if (!m_isPeriodic)
                return;

            Player modOwner = caster != null ? caster.GetSpellModOwner() : null;

            // Apply casting time mods
            if (_period != 0)
            {
                // Apply periodic time mod
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.Period, ref _period);

                if (caster != null)
                {
                    // Haste modifies periodic time of channeled spells
                    if (m_spellInfo.IsChanneled())
                        caster.ModSpellDurationTime(m_spellInfo, ref _period);
                    else if (m_spellInfo.HasAttribute(SpellAttr5.SpellHasteAffectsPeriodic))
                        _period = (int)(_period * caster.m_unitData.ModCastingSpeed);
                    else if (m_spellInfo.HasAttribute(SpellAttr8.MeleeHasteAffectsPeriodic))
                        _period = (int)(_period * caster.m_unitData.ModHaste);
                }
            }
            else // prevent infinite loop on Update
                m_isPeriodic = false;

            if (load) // aura loaded from db
            {
                if (_period != 0 && !GetBase().IsPermanent())
                {
                    uint elapsedTime = (uint)(GetBase().GetMaxDuration() - GetBase().GetDuration());
                    _ticksDone = elapsedTime / (uint)_period;
                    _periodicTimer = (int)(elapsedTime % _period);
                }

                if (m_spellInfo.HasAttribute(SpellAttr5.ExtraInitialPeriod))
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
                    if (m_spellmod == null)
                    {
                        SpellModifierByClassMask spellmod = new(GetBase());
                        spellmod.op = (SpellModOp)GetMiscValue();

                        spellmod.type = GetAuraType() == AuraType.AddPctModifier ? SpellModType.Pct : SpellModType.Flat;
                        spellmod.spellId = GetId();
                        spellmod.mask = GetSpellEffectInfo().SpellClassMask;
                        m_spellmod = spellmod;
                    }
                    (m_spellmod as SpellModifierByClassMask).value = GetAmount();
                    break;
                case AuraType.AddFlatModifierBySpellLabel:
                    if (m_spellmod == null)
                    {
                        SpellFlatModifierByLabel spellmod = new(GetBase());
                        spellmod.op = (SpellModOp)GetMiscValue();

                        spellmod.type = SpellModType.LabelFlat;
                        spellmod.spellId = GetId();
                        spellmod.value.ModIndex = GetMiscValue();
                        spellmod.value.LabelID = GetMiscValueB();
                        m_spellmod = spellmod;
                    }
                    (m_spellmod as SpellFlatModifierByLabel).value.ModifierValue = GetAmount();
                    break;
                case AuraType.AddPctModifierBySpellLabel:
                    if (m_spellmod == null)
                    {
                        SpellPctModifierByLabel spellmod = new(GetBase());
                        spellmod.op = (SpellModOp)GetMiscValue();

                        spellmod.type = SpellModType.LabelPct;
                        spellmod.spellId = GetId();
                        spellmod.value.ModIndex = GetMiscValue();
                        spellmod.value.LabelID = GetMiscValueB();
                        m_spellmod = spellmod;
                    }
                    (m_spellmod as SpellPctModifierByLabel).value.ModifierValue = 1.0f + MathFunctions.CalculatePct(1.0f, GetAmount());
                    break;
                default:
                    break;
            }
            GetBase().CallScriptEffectCalcSpellModHandlers(this, ref m_spellmod);

            // validate modifier
            if (m_spellmod != null)
            {
                bool isValid = true;
                bool logErrors = GetBase().GetLoadedScripts().Any(script => script.DoEffectCalcSpellMod.Count > 0);
                if (m_spellmod.op >= SpellModOp.Max)
                {
                    isValid = false;
                    if (logErrors)
                        Log.outError(LogFilter.Spells, $"Aura script for spell id {GetId()} created invalid spell modifier op {m_spellmod.op}");
                }

                if (m_spellmod.type >= SpellModType.End)
                {
                    isValid = false;
                    if (logErrors)
                        Log.outError(LogFilter.Spells, $"Aura script for spell id {GetId()} created invalid spell modifier type {m_spellmod.type}");
                }

                if (!isValid)
                    m_spellmod = null;
            }
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

            if (GetSpellInfo().HasAttribute(SpellAttr8.AuraPointsOnClient) || Aura.EffectTypeNeedsSendingAmount(GetAuraType()))
                GetBase().SetNeedClientUpdateForTargets();
        }

        public void HandleEffect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply, AuraEffect triggeredBy = null)
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

        void ApplySpellMod(Unit target, bool apply, AuraEffect triggeredBy = null)
        {
            if (m_spellmod == null || !target.IsTypeId(TypeId.Player))
                return;

            target.ToPlayer().AddSpellMod(m_spellmod, apply);

            // Auras with charges do not mod amount of passive auras
            if (GetBase().IsUsingCharges())
                return;
            // reapply some passive spells after add/remove related spellmods
            // Warning: it is a dead loop if 2 auras each other amount-shouldn't happen
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
                    // only passive and permament auras-active auras should have amount set on spellcast and not be affected
                    // if aura is cast by others, it will not be affected
                    if ((aura.IsPassive() || aura.IsPermanent()) && aura.GetCasterGUID() == guid && aura.GetSpellInfo().IsAffectedBySpellMod(m_spellmod))
                    {
                        for (uint i = 0; i < recalculateEffectMask.Count; ++i)
                        {
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
            }
        }

        public void Update(uint diff, Unit caster)
        {
            if (!m_isPeriodic || (GetBase().GetDuration() < 0 && !GetBase().IsPassive() && !GetBase().IsPermanent()))
                return;

            uint totalTicks = GetTotalTicks();

            _periodicTimer += (int)diff;
            while (_periodicTimer >= _period)
            {
                _periodicTimer -= _period;

                if (!GetBase().IsPermanent() && (_ticksDone + 1) > totalTicks)
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

            // Update serverside orientation of tracking channeled auras on periodic update ticks
            // exclude players because can turn during channeling and shouldn't desync orientation client/server
            if (caster != null && !caster.IsPlayer() && m_spellInfo.IsChanneled() && m_spellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel) && caster.m_unitData.ChannelObjects.Size() != 0)
            {
                ObjectGuid channelGuid = caster.m_unitData.ChannelObjects[0];
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
                    if (spellInfo == null || (spellInfo.GetAllEffectsMechanicMask() & (1ul << GetMiscValue())) == 0)
                        return false;
                    break;
                case AuraType.ModCastingSpeedNotStack:
                    // skip melee hits and instant cast spells
                    if (eventInfo.GetProcSpell() == null || eventInfo.GetProcSpell().GetCastTime() == 0)
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
                        || eventInfo.GetProcSpell() == null)
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
                    SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
                    if (triggeredSpellInfo != null)
                    {
                        if (triggeredSpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                        {
                            uint lastExtraAttackSpell = eventInfo.GetActor().GetLastExtraAttackSpell();

                            // Patch 1.12.0(?) extra attack abilities can no longer chain proc themselves
                            if (lastExtraAttackSpell == triggerSpellId)
                                return false;
                        }
                    }
                    break;
                }
                case AuraType.ModSpellCritChance:
                    // skip spells that can't crit
                    if (spellInfo == null || !spellInfo.HasAttribute(SpellCustomAttributes.CanCrit))
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
                        if (pair.Value.State == PlayerSpellState.Removed || pair.Value.Disabled)
                            continue;

                        if (pair.Key == spellId || pair.Key == spellId2 || pair.Key == spellId3 || pair.Key == spellId4)
                            continue;

                        SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(pair.Key, Difficulty.None);
                        if (spellInfo == null || !(spellInfo.IsPassive() || spellInfo.HasAttribute(SpellAttr0.DoNotDisplaySpellbookAuraIconCombatLog)))
                            continue;

                        if (Convert.ToBoolean(spellInfo.Stances & (1ul << (GetMiscValue() - 1))))
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
        public uint GetEffIndex() { return _effectInfo.EffectIndex; }
        public int GetBaseAmount() { return m_baseAmount; }
        public int GetPeriod() { return _period; }

        public int GetMiscValueB() { return GetSpellEffectInfo().MiscValueB; }
        public int GetMiscValue() { return GetSpellEffectInfo().MiscValue; }
        public AuraType GetAuraType() { return GetSpellEffectInfo().ApplyAuraName; }
        public int GetAmount() { return _amount; }
        public bool HasAmount() { return _amount != 0; }
        public void SetAmount(int amount) { _amount = amount; m_canBeRecalculated = false; }

        public float? GetEstimatedAmount() { return _estimatedAmount; }

        public int GetPeriodicTimer() { return _periodicTimer; }
        public void SetPeriodicTimer(int periodicTimer) { _periodicTimer = periodicTimer; }

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

        public bool CanBeRecalculated() { return m_canBeRecalculated; }
        public void SetCanBeRecalculated(bool val) { m_canBeRecalculated = val; }

        public void ResetTicks() { _ticksDone = 0; }
        public uint GetTickNumber() { return _ticksDone; }
        public uint GetRemainingTicks() { return GetTotalTicks() - _ticksDone; }

        public bool IsPeriodic() { return m_isPeriodic; }
        public void SetPeriodic(bool isPeriodic) { m_isPeriodic = isPeriodic; }
        bool HasSpellClassMask() { return GetSpellEffectInfo().SpellClassMask; }

        public SpellEffectInfo GetSpellEffectInfo() { return _effectInfo; }

        public bool IsEffect() { return _effectInfo.Effect != 0; }
        public bool IsEffect(SpellEffectName effectName) { return _effectInfo.Effect == effectName; }
        public bool IsAreaAuraEffect()
        {
            return _effectInfo.IsAreaAuraEffect();
        }

        #region Fields
        Aura auraBase;
        SpellInfo m_spellInfo;
        SpellEffectInfo _effectInfo;
        SpellModifier m_spellmod;

        public int m_baseAmount;
        int _amount;
        float? _estimatedAmount;   // for periodic damage and healing auras this will include damage done bonuses

        // periodic stuff
        int _periodicTimer;
        int _period; // time between consecutive ticks
        uint _ticksDone; // ticks counter

        bool m_canBeRecalculated;
        bool m_isPeriodic;
        #endregion

        #region AuraEffect Handlers
        /**************************************/
        /***       VISIBILITY & PHASES      ***/
        /**************************************/
        [AuraEffectHandler(AuraType.None)]
        void HandleUnused(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
        }

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

            // call functions which may have additional effects after changing state of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModInvisibility)]
        void HandleModInvisibility(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            Player playerTarget = target.ToPlayer();
            InvisibilityType type = (InvisibilityType)GetMiscValue();

            if (apply)
            {
                // apply glow vision
                if (playerTarget != null && type == InvisibilityType.General)
                    playerTarget.AddAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                target.m_invisibility.AddFlag(type);
                target.m_invisibility.AddValue(type, GetAmount());

                target.SetVisFlag(UnitVisFlags.Invisible);
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModInvisibility))
                {
                    // if not have different invisibility auras.
                    // always remove glow vision
                    if (playerTarget != null)
                        playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

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
                    {
                        // if not have invisibility auras of type INVISIBILITY_GENERAL
                        // remove glow vision
                        if (playerTarget != null && type == InvisibilityType.General)
                            playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                        target.m_invisibility.DelFlag(type);

                        target.RemoveVisFlag(UnitVisFlags.Invisible);
                    }
                }

                target.m_invisibility.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after changing state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                // drop flag at invisibiliy in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);
            }

            if (target.IsInWorld)
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

            // call functions which may have additional effects after changing state of unit
            if (target.IsInWorld)
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
                target.SetVisFlag(UnitVisFlags.Stealthed);
                Player playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    playerTarget.AddAuraVision(PlayerFieldByte2Flags.Stealth);
            }
            else
            {
                target.m_stealth.AddValue(type, -GetAmount());

                if (!target.HasAuraType(AuraType.ModStealth)) // if last SPELL_AURA_MOD_STEALTH
                {
                    target.m_stealth.DelFlag(type);

                    target.RemoveVisFlag(UnitVisFlags.Stealthed);
                    Player playerTarget = target.ToPlayer();
                    if (playerTarget != null)
                        playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.Stealth);
                }
            }

            // call functions which may have additional effects after changing state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                // drop flag at stealth in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);
            }

            if (target.IsInWorld)
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

            // call functions which may have additional effects after changing state of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.DetectAmore)]
        void HandleDetectAmore(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                return;

            if (apply)
            {
                Player playerTarget = target.ToPlayer();
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

                Player playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    playerTarget.RemoveAuraVision((PlayerFieldByte2Flags)(1 << (GetMiscValue() - 1)));
            }
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
                    // set stand state (expected in this form)
                    if (!target.IsStandState())
                        target.SetStandState(UnitStandStateType.Stand);
                }
            }
            // die at aura end
            else if (target.IsAlive())
                // call functions which may have additional effects after changing state of unit
                target.SetDeathState(DeathState.JustDied);
        }

        [AuraEffectHandler(AuraType.Ghost)]
        void HandleAuraGhost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
            {
                target.SetPlayerFlag(PlayerFlags.Ghost);
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
        void HandlePhase(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                PhasingHandler.AddPhase(target, (uint)GetMiscValueB(), true);
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
                PhasingHandler.AddPhaseGroup(target, (uint)GetMiscValueB(), true);
            else
                PhasingHandler.RemovePhaseGroup(target, (uint)GetMiscValueB(), true);
        }

        [AuraEffectHandler(AuraType.PhaseAlwaysVisible)]
        void HandlePhaseAlwaysVisible(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

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
        void HandleAuraModShapeshift(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.RealOrReapplyMask))
                return;

            SpellShapeshiftFormRecord shapeInfo = CliDB.SpellShapeshiftFormStorage.LookupByKey(GetMiscValue());
            //ASSERT(shapeInfo, "Spell {0} uses unknown ShapeshiftForm (%u).", GetId(), GetMiscValue());

            Unit target = aurApp.GetTarget();
            ShapeShiftForm form = (ShapeShiftForm)GetMiscValue();
            uint modelid = target.GetModelForForm(form, GetId());

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
                            target.RemoveAurasDueToSpell(target.GetTransformSpell());
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
                    SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransformSpell(), GetBase().GetCastDifficulty());
                    if (transformSpellInfo == null || !GetSpellInfo().IsPositive())
                        target.SetDisplayId(modelid);
                }

                if (!shapeInfo.HasFlag(SpellShapeshiftFormFlags.Stance))
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Shapeshifting, GetSpellInfo());
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
                        AuraEffect dummy = target.GetAuraEffect(37315, 0);
                        if (dummy != null)
                            target.CastSpell(target, 37316, new CastSpellExtraArgs(dummy));
                        break;
                    // Nordrassil Regalia - bonus
                    case ShapeShiftForm.MoonkinForm:
                        dummy = target.GetAuraEffect(37324, 0);
                        if (dummy != null)
                            target.CastSpell(target, 37325, new CastSpellExtraArgs(dummy));
                        break;
                    default:
                        break;
                }

                // remove the shapeshift aura's boosts
                HandleShapeshiftBoosts(target, apply);
            }

            Player playerTarget = target.ToPlayer();
            if (playerTarget != null)
            {
                playerTarget.SendMovementSetCollisionHeight(playerTarget.GetCollisionHeight(), UpdateCollisionHeightReason.Force);
                playerTarget.InitDataForForm();
            }
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
                // update active transform spell only when transform not set or not overwriting negative by positive case
                SpellInfo transformSpellInfo = Global.SpellMgr.GetSpellInfo(target.GetTransformSpell(), GetBase().GetCastDifficulty());
                if (transformSpellInfo == null || !GetSpellInfo().IsPositive() || transformSpellInfo.IsPositive())
                {
                    target.SetTransformSpell(GetId());
                    // special case (spell specific functionality)
                    if (GetMiscValue() == 0)
                    {
                        bool isFemale = target.GetNativeGender() == Gender.Female;
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
                                        target.SetDisplayId(isFemale ? 17830 : 17829u);
                                        break;
                                    // Orc
                                    case Race.Orc:
                                        target.SetDisplayId(isFemale ? 10140 : 10139u);
                                        break;
                                    // Troll
                                    case Race.Troll:
                                        target.SetDisplayId(isFemale ? 10134 : 10135u);
                                        break;
                                    // Tauren
                                    case Race.Tauren:
                                        target.SetDisplayId(isFemale ? 10147 : 10136u);
                                        break;
                                    // Undead
                                    case Race.Undead:
                                        target.SetDisplayId(isFemale ? 10145 : 10146u);
                                        break;
                                    // Draenei
                                    case Race.Draenei:
                                        target.SetDisplayId(isFemale ? 17828 : 17827u);
                                        break;
                                    // Dwarf
                                    case Race.Dwarf:
                                        target.SetDisplayId(isFemale ? 10142 : 10141u);
                                        break;
                                    // Gnome
                                    case Race.Gnome:
                                        target.SetDisplayId(isFemale ? 10149 : 10148u);
                                        break;
                                    // Human
                                    case Race.Human:
                                        target.SetDisplayId(isFemale ? 10138 : 10137u);
                                        break;
                                    // Night Elf
                                    case Race.NightElf:
                                        target.SetDisplayId(isFemale ? 10144 : 10143u);
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
                                        target.SetDisplayId(isFemale ? 25043 : 25032u);
                                        break;
                                    // Orc
                                    case Race.Orc:
                                        target.SetDisplayId(isFemale ? 25050 : 25039u);
                                        break;
                                    // Troll
                                    case Race.Troll:
                                        target.SetDisplayId(isFemale ? 25052 : 25041u);
                                        break;
                                    // Tauren
                                    case Race.Tauren:
                                        target.SetDisplayId(isFemale ? 25051 : 25040u);
                                        break;
                                    // Undead
                                    case Race.Undead:
                                        target.SetDisplayId(isFemale ? 25053 : 25042u);
                                        break;
                                    // Draenei
                                    case Race.Draenei:
                                        target.SetDisplayId(isFemale ? 25044 : 25033u);
                                        break;
                                    // Dwarf
                                    case Race.Dwarf:
                                        target.SetDisplayId(isFemale ? 25045 : 25034u);
                                        break;
                                    // Gnome
                                    case Race.Gnome:
                                        target.SetDisplayId(isFemale ? 25035 : 25046u);
                                        break;
                                    // Human
                                    case Race.Human:
                                        target.SetDisplayId(isFemale ? 25037 : 25048u);
                                        break;
                                    // Night Elf
                                    case Race.NightElf:
                                        target.SetDisplayId(isFemale ? 25038 : 25049u);
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
                                target.SetDisplayId(isFemale ? 29204 : 29203u);
                                break;
                            // Darkspear Pride
                            case 75532:
                                target.SetDisplayId(isFemale ? 31738 : 31737u);
                                break;
                            // Gnomeregan Pride
                            case 75531:
                                target.SetDisplayId(isFemale ? 31655 : 31654u);
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
                if (target.GetTransformSpell() == GetId())
                    target.SetTransformSpell(0);

                target.RestoreDisplayId(target.IsMounted());

                // Dragonmaw Illusion (restore mount model)
                if (GetId() == 42016 && target.GetMountDisplayId() == 16314)
                {
                    if (!target.GetAuraEffectsByType(AuraType.Mounted).Empty())
                    {
                        int cr_id = target.GetAuraEffectsByType(AuraType.Mounted)[0].GetMiscValue();
                        CreatureTemplate ci = Global.ObjectMgr.GetCreatureTemplate((uint)cr_id);
                        if (ci != null)
                        {
                            CreatureModel model = ObjectManager.ChooseDisplayId(ci);
                            Global.ObjectMgr.GetCreatureModelRandomGender(ref model, ci);

                            target.SetMountDisplayId(model.CreatureDisplayID);
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

            aurApp.GetTarget().RecalculateObjectScale();
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
                target.SetUnitFlag2(UnitFlags2.MirrorImage);
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
        void HandleFeignDeath(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                var isAffectedByFeignDeath = bool (Unit attacker) =>
                {
                    Creature attackerCreature = attacker.ToCreature();
                    return attackerCreature == null || !attackerCreature.IsIgnoringFeignDeath();
                };

                List<Unit> targets = new();
                var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(target, target, target.GetMap().GetVisibilityRange());
                var searcher = new UnitListSearcher(target, targets, u_check);

                Cell.VisitAllObjects(target, searcher, target.GetMap().GetVisibilityRange());
                foreach (var unit in targets)
                {
                    if (!unit.HasUnitState(UnitState.Casting))
                        continue;

                    if (!isAffectedByFeignDeath(unit))
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

                foreach (var (_, refe) in target.GetThreatManager().GetThreatenedByMeList())
                    if (isAffectedByFeignDeath(refe.GetOwner()))
                        refe.ScaleThreat(0.0f);

                if (target.GetMap().IsDungeon()) // feign death does not remove combat in dungeons
                {
                    target.AttackStop();
                    Player targetPlayer = target.ToPlayer();
                    if (targetPlayer != null)
                        targetPlayer.SendAttackSwingCancelAttack();
                }
                else
                    target.CombatStop(false, false, isAffectedByFeignDeath);

                // prevent interrupt message
                if (GetCasterGUID() == target.GetGUID() && target.GetCurrentSpell(CurrentSpellTypes.Generic) != null)
                    target.FinishSpell(CurrentSpellTypes.Generic, SpellCastResult.Interrupted);
                target.InterruptNonMeleeSpells(true);

                // stop handling the effect if it was removed by linked event
                if (aurApp.HasRemoveMode())
                    return;

                target.SetUnitFlag(UnitFlags.PreventEmotesFromChatText);
                target.SetUnitFlag2(UnitFlags2.FeignDeath);
                target.SetUnitFlag3(UnitFlags3.FakeDead);
                target.AddUnitState(UnitState.Died);

                Creature creature = target.ToCreature();
                if (creature != null)
                    creature.SetReactState(ReactStates.Passive);
            }
            else
            {
                target.RemoveUnitFlag(UnitFlags.PreventEmotesFromChatText);
                target.RemoveUnitFlag2(UnitFlags2.FeignDeath);
                target.RemoveUnitFlag3(UnitFlags3.FakeDead);
                target.ClearUnitState(UnitState.Died);

                Creature creature = target.ToCreature();
                if (creature != null)
                    creature.InitializeReactState();
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

            if (apply)
                target.SetUnitFlag(UnitFlags.NonAttackable2);
            else
                target.RemoveUnitFlag(UnitFlags.NonAttackable2);

            // call functions which may have additional effects after changing state of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                if (target.GetMap().IsDungeon())
                {
                    target.AttackStop();
                    Player targetPlayer = target.ToPlayer();
                    if (targetPlayer != null)
                        targetPlayer.SendAttackSwingCancelAttack();
                }
                else
                    target.CombatStop();
            }
        }

        [AuraEffectHandler(AuraType.ModDisarm)]
        void HandleAuraModDisarm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            //Prevent handling aura twice
            AuraType type = GetAuraType();

            if (apply ? target.GetAuraEffectsByType(type).Count > 1 : target.HasAuraType(type))
                return;

            Action<Unit> flagChangeFunc = null;
            byte slot;
            WeaponAttackType attType;
            switch (type)
            {
                case AuraType.ModDisarm:
                    if (apply)
                        flagChangeFunc = unit => { unit.SetUnitFlag(UnitFlags.Disarmed); };
                    else
                        flagChangeFunc = unit => { unit.RemoveUnitFlag(UnitFlags.Disarmed); };
                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.BaseAttack;
                    break;
                case AuraType.ModDisarmOffhand:
                    if (apply)
                        flagChangeFunc = unit => { unit.SetUnitFlag2(UnitFlags2.DisarmOffhand); };
                    else
                        flagChangeFunc = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmOffhand); };
                    slot = EquipmentSlot.OffHand;
                    attType = WeaponAttackType.OffAttack;
                    break;
                case AuraType.ModDisarmRanged:
                    if (apply)
                        flagChangeFunc = unit => { unit.SetUnitFlag2(UnitFlags2.DisarmRanged); };
                    else
                        flagChangeFunc = unit => { unit.RemoveUnitFlag2(UnitFlags2.DisarmRanged); };
                    slot = EquipmentSlot.MainHand;
                    attType = WeaponAttackType.RangedAttack;
                    break;
                default:
                    return;
            }

            // set/remove flag before weapon bonuses so it's properly reflected in CanUseAttackType
            flagChangeFunc?.Invoke(target);

            // Handle damage modification, shapeshifted druids are not affected
            if (target.IsTypeId(TypeId.Player) && !target.IsInFeralForm())
            {
                Player player = target.ToPlayer();

                Item item = player.GetItemByPos(InventorySlots.Bag0, slot);
                if (item != null)
                {
                    WeaponAttackType attackType = Player.GetAttackBySlot(slot, item.GetTemplate().GetInventoryType());

                    player.ApplyItemDependentAuras(item, !apply);
                    if (attackType < WeaponAttackType.Max)
                    {
                        player._ApplyWeaponDamage(slot, item, !apply);
                        if (!apply) // apply case already handled on item dependent aura removal (if any)
                            player.UpdateWeaponDependentAuras(attackType);
                    }
                }
            }

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
                target.SetSilencedSchoolMask((SpellSchoolMask)GetMiscValue());

                // call functions which may have additional effects after changing state of unit
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
                int silencedSchoolMask = 0;
                foreach (AuraEffect auraEffect in target.GetAuraEffectsByType(AuraType.ModSilence))
                    silencedSchoolMask |= auraEffect.GetMiscValue();

                foreach (AuraEffect auraEffect in target.GetAuraEffectsByType(AuraType.ModPacifySilence))
                    silencedSchoolMask |= auraEffect.GetMiscValue();

                target.ReplaceAllSilencedSchoolMask((SpellSchoolMask)silencedSchoolMask);
            }
        }

        [AuraEffectHandler(AuraType.ModPacify)]
        void HandleAuraModPacify(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetUnitFlag(UnitFlags.Pacified);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(AuraType.ModPacify) || target.HasAuraType(AuraType.ModPacifySilence))
                    return;
                target.RemoveUnitFlag(UnitFlags.Pacified);
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
                    target.SetUnitFlag(UnitFlags.NonAttackable);
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

        [AuraEffectHandler(AuraType.ModNoActions)]
        void HandleAuraModNoActions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag2(UnitFlags2.NoActions);

                // call functions which may have additional effects after changing state of unit
                // Stop cast only spells vs PreventionType & SPELL_PREVENTION_TYPE_SILENCE
                for (var i = CurrentSpellTypes.Melee; i < CurrentSpellTypes.Max; ++i)
                {
                    Spell spell = target.GetCurrentSpell(i);
                    if (spell != null)
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
        void HandleAuraTrackCreatures(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.SetTrackCreatureFlag(1u << (GetMiscValue() - 1));
            else
                target.RemoveTrackCreatureFlag(1u << (GetMiscValue() - 1));
        }

        [AuraEffectHandler(AuraType.TrackStealthed)]
        void HandleAuraTrackStealthed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (!(apply))
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }
            if (apply)
                target.SetPlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
            else
                target.RemovePlayerLocalFlag(PlayerLocalFlags.TrackStealthed);
        }

        [AuraEffectHandler(AuraType.ModStalked)]
        void HandleAuraModStalked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // used by spells: Hunter's Mark, Mind Vision, Syndicate Tracker (MURP) DND
            if (apply)
                target.SetDynamicFlag(UnitDynFlags.TrackUnit);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (!target.HasAuraType(GetAuraType()))
                    target.RemoveDynamicFlag(UnitDynFlags.TrackUnit);
            }

            // call functions which may have additional effects after changing state of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.Untrackable)]
        void HandleAuraUntrackable(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.SetVisFlag(UnitVisFlags.Untrackable);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveVisFlag(UnitVisFlags.Untrackable);
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

        [AuraEffectHandler(AuraType.AllowTalentSwapping)]
        void HandleAuraAllowTalentSwapping(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.SetUnitFlag2(UnitFlags2.AllowChangingTalents);
            else if (!target.HasAuraType(GetAuraType()))
                target.RemoveUnitFlag2(UnitFlags2.AllowChangingTalents);
        }

        /****************************/
        /***       MOVEMENT       ***/
        /****************************/
        [AuraEffectHandler(AuraType.Mounted)]
        void HandleAuraMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                {
                    uint creatureEntry = (uint)GetMiscValue();
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
                                displayId = SharedConst.DisplayIdHiddenMount;
                            }
                            else
                            {
                                var usableDisplays = mountDisplays.Where(mountDisplay =>
                                {
                                    Player playerTarget = target.ToPlayer();
                                    if (playerTarget != null)
                                        return ConditionManager.IsPlayerMeetingCondition(playerTarget, mountDisplay.PlayerConditionID);

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
                        foreach (SpellEffectInfo effect in GetSpellInfo().GetEffects())
                            if (effect.IsEffect(SpellEffectName.Summon) && effect.MiscValue == GetMiscValue())
                                displayId = 0;
                    }

                    target.Mount(displayId, vehicleId, creatureEntry);
                }

                // cast speed aura
                if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                {
                    var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(GetAmount());
                    if (mountCapability != null)
                    {
                        target.SetFlightCapabilityID(mountCapability.FlightCapabilityID, true);
                        target.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(this));
                    }
                }
            }
            else
            {
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                    target.Dismount();

                //some mounts like Headless Horseman's Mount or broom stick are skill based spell
                // need to remove ALL arura related to mounts, this will stop client crash with broom stick
                // and never endless flying after using Headless Horseman's Mount
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                    target.RemoveAurasByType(AuraType.Mounted);

                if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                {
                    // remove speed aura
                    var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(GetAmount());
                    if (mountCapability != null)
                        target.RemoveAurasDueToSpell(mountCapability.ModSpellAuraID, target.GetGUID());

                    target.SetFlightCapabilityID(0, true);
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

            target.SetCanTransitionBetweenSwimAndFly(apply);

            if (target.SetCanFly(apply))
                if (!apply && !target.IsGravityDisabled())
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
                target.SetUnitFlag2(UnitFlags2.ForceMovement);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveUnitFlag2(UnitFlags2.ForceMovement);
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

        [AuraEffectHandler(AuraType.AdvFlying)]
        void HandleModAdvFlying(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            target.SetCanDoubleJump(apply || target.HasAura(196055));
            target.SetCanFly(apply);
            target.SetCanAdvFly(apply);
        }

        [AuraEffectHandler(AuraType.IgnoreMovementForces)]
        void HandleIgnoreMovementForces(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
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

            target.SetIgnoreMovementForces(apply);
        }

        [AuraEffectHandler(AuraType.DisableInertia)]
        void HandleDisableInertia(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
            }

            target.SetDisableInertia(apply);
        }

        void HandleSetCantSwim(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
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

            target.SetMoveCantSwim(apply);
        }

        /****************************/
        /***        THREAT        ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModThreat)]
        void HandleModThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            aurApp.GetTarget().GetThreatManager().UpdateMySpellSchoolModifiers();
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
                caster.GetThreatManager().UpdateMyTempModifiers();
        }

        [AuraEffectHandler(AuraType.ModTaunt)]
        void HandleModTaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsAlive() || !target.CanHaveThreatList())
                return;

            target.GetThreatManager().TauntUpdate();
        }

        [AuraEffectHandler(AuraType.ModDetaunt)]
        void HandleModDetaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit caster = GetCaster();
            Unit target = aurApp.GetTarget();

            if (caster == null || !caster.IsAlive() || !target.IsAlive() || !caster.CanHaveThreatList())
                return;

            caster.GetThreatManager().TauntUpdate();
        }

        [AuraEffectHandler(AuraType.ModFixate)]
        void HandleAuraModFixate(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit caster = GetCaster();
            Unit target = aurApp.GetTarget();

            if (caster == null || !caster.IsAlive() || !target.IsAlive() || !caster.CanHaveThreatList())
                return;

            if (apply)
                caster.GetThreatManager().FixateTarget(target);
            else
                caster.GetThreatManager().ClearFixate();
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

            if (apply)
                target.GetThreatManager().EvaluateSuppressed();
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

            if (apply)
                target.GetThreatManager().EvaluateSuppressed();
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

        [AuraEffectHandler(AuraType.ModRootDisableGravity)]
        void HandleAuraModRootAndDisableGravity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Root);

            // Do not remove DisableGravity if there are more than this auraEffect of that kind on the unit or if it's a creature with DisableGravity on its movement template.
            if (!apply && (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.ModRootDisableGravity) || (target.IsCreature() && target.ToCreature().IsFloating())))
                return;

            if (target.SetDisableGravity(apply))
                if (!apply && !target.IsFlying())
                    target.GetMotionMaster().MoveFall();
        }

        [AuraEffectHandler(AuraType.ModStunDisableGravity)]
        void HandleAuraModStunAndDisableGravity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Stunned);

            if (apply)
                target.GetThreatManager().EvaluateSuppressed();

            // Do not remove DisableGravity if there are more than this auraEffect of that kind on the unit or if it's a creature with DisableGravity on its movement template.
            if (!apply && (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.ModStunDisableGravity) || (target.IsCreature() && target.ToCreature().IsFloating())))
                return;

            if (target.SetDisableGravity(apply))
                if (!apply && !target.IsFlying())
                    target.GetMotionMaster().MoveFall();
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

                    // TODO: remove this
                    if (pet.GetVictim() == null && !pet.GetCharmInfo().HasCommandState(CommandStates.Stay))
                        pet.GetMotionMaster().MoveFollow(caster, SharedConst.PetFollowDist, pet.GetFollowAngle());
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
                caster._EnterVehicle(target.GetVehicleKit(), (sbyte)(GetAmount() - 1), aurApp);
            }
            else
            {
                // Remove pending passengers before exiting vehicle - might cause an Uninstall
                target.GetVehicleKit().RemovePendingEventsForPassenger(caster);

                if (GetId() == 53111) // Devour Humanoid
                {
                    Unit.Kill(target, caster);
                    if (caster.IsTypeId(TypeId.Unit))
                        caster.ToCreature().DespawnOrUnsummon();
                }

                bool seatChange = mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmount)                             // Seat change on the same direct vehicle
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
                    target.SetCanTransitionBetweenSwimAndFly(apply);

                    if (target.SetCanFly(apply))
                        if (!apply && !target.IsGravityDisabled())
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

        [AuraEffectHandler(AuraType.ModMovementForceMagnitude)]
        void HandleModMovementForceMagnitude(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            aurApp.GetTarget().UpdateMovementForcesModMagnitude();
        }

        [AuraEffectHandler(AuraType.ModAdvFlyingAirFriction)]
        [AuraEffectHandler(AuraType.ModAdvFlyingMaxVel)]
        [AuraEffectHandler(AuraType.ModAdvFlyingLiftCoef)]
        [AuraEffectHandler(AuraType.ModAdvFlyingAddImpulseMaxSpeed)]
        [AuraEffectHandler(AuraType.ModAdvFlyingBankingRate)]
        [AuraEffectHandler(AuraType.ModAdvFlyingPitchingRateDown)]
        [AuraEffectHandler(AuraType.ModAdvFlyingPitchingRateUp)]
        [AuraEffectHandler(AuraType.ModAdvFlyingOverMaxDeceleration)]
        void HandleAuraModAdvFlyingSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            switch (GetAuraType())
            {
                case AuraType.ModAdvFlyingAirFriction:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeSingle.AirFriction, true);
                    break;
                case AuraType.ModAdvFlyingMaxVel:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeSingle.MaxVel, true);
                    break;
                case AuraType.ModAdvFlyingLiftCoef:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeSingle.LiftCoefficient, true);
                    break;
                case AuraType.ModAdvFlyingAddImpulseMaxSpeed:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeSingle.AddImpulseMaxSpeed, true);
                    break;
                case AuraType.ModAdvFlyingBankingRate:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeRange.BankingRate, true);
                    break;
                case AuraType.ModAdvFlyingPitchingRateDown:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeRange.PitchingRateDown, true);
                    break;
                case AuraType.ModAdvFlyingPitchingRateUp:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeRange.PitchingRateUp, true);
                    break;
                case AuraType.ModAdvFlyingOverMaxDeceleration:
                    target.UpdateAdvFlyingSpeed(AdvFlyingRateTypeSingle.OverMaxDeceleration, true);
                    break;
            }
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
            // TODO: this should be handled in aura script for flag spells using AfterEffectRemove hook
            Player player = target.ToPlayer();
            if (!apply && player != null && GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.StealthOrInvis))
            {
                if (!player.InBattleground())
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

            // TODO: should be changed to a proc script on flag spell (they have "Taken positive" proc flags in db2)
            {
                if (apply && GetMiscValue() == (int)SpellSchoolMask.Normal)
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

                // remove all flag auras (they are positive, but they must be removed when you are immune)
                if (GetSpellInfo().HasAttribute(SpellAttr1.ImmunityPurgesEffect)
                    && GetSpellInfo().HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);
            }

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Immune);
                target.GetThreatManager().EvaluateSuppressed();
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit
                if (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.DamageImmunity))
                    return;

                target.RemoveUnitFlag(UnitFlags.Immune);
            }
        }

        [AuraEffectHandler(AuraType.DamageImmunity)]
        void HandleAuraModDmgImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            m_spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Immune);
                target.GetThreatManager().EvaluateSuppressed();
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit
                if (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.SchoolImmunity))
                    return;
                target.RemoveUnitFlag(UnitFlags.Immune);
            }
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
                    target.HandleStatFlatModifier(UnitMods.ResistanceStart + x, UnitModifierFlatType.Total, GetAmount(), apply);
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
                {
                    if (apply)
                        target.ApplyStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, GetAmount());
                    else
                    {
                        float amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, (uint)SpellSchoolMask.Normal);
                        target.SetStatPctModifier(UnitMods.Armor, UnitModifierPctType.Base, amount);
                    }
                }
            }
            else
            {
                for (byte x = (byte)SpellSchools.Normal; x < (byte)SpellSchools.Max; x++)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << x)))
                    {
                        if (apply)
                            target.ApplyStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, GetAmount());
                        else
                        {
                            float amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModBaseResistancePct, 1u << x);
                            target.SetStatPctModifier(UnitMods.ResistanceStart + x, UnitModifierPctType.Base, amount);
                        }
                    }
                }
            }
        }

        [AuraEffectHandler(AuraType.ModResistancePct)]
        void HandleModResistancePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            for (byte i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
            {
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                {
                    float amount = target.GetTotalAuraMultiplierByMiscMask(AuraType.ModResistancePct, 1u << i);
                    if (target.GetPctModifierValue(UnitMods.ResistanceStart + i, UnitModifierPctType.Total) == amount)
                        continue;

                    target.SetStatPctModifier(UnitMods.ResistanceStart + i, UnitModifierPctType.Total, amount);
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
                    target.HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Total, GetAmount(), apply);
            }
            else
            {
                for (byte i = (byte)SpellSchools.Normal; i < (byte)SpellSchools.Max; i++)
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                        target.HandleStatFlatModifier(UnitMods.ResistanceStart + i, UnitModifierFlatType.Total, GetAmount(), apply);
            }
        }

        [AuraEffectHandler(AuraType.ModTargetResistance)]
        void HandleModTargetResistance(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
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
                {
                    if (apply)
                        target.ApplyStatPctModifier(UnitMods.StatStart + i, UnitModifierPctType.Base, GetAmount());
                    else
                    {
                        float amount = target.GetTotalAuraMultiplier(AuraType.ModPercentStat, aurEff =>
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
            if (player != null)
                player.UpdateHealingDonePercentMod();
        }

        [AuraEffectHandler(AuraType.ModTotalStatPercentage)]
        void HandleModTotalPercentStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            // save current health state
            float healthPct = target.GetHealthPct();
            bool zeroHealth = !target.IsAlive();

            // players in corpse state may mean two different states:
            /// 1. player just died but did not release (in this case health == 0)
            /// 2. player is corpse running (ie ghost) (in this case health == 1)
            if (target.GetDeathState() == DeathState.Corpse)
                zeroHealth = target.GetHealth() == 0;

            for (int i = (int)Stats.Strength; i < (int)Stats.Max; i++)
            {
                if (Convert.ToBoolean(GetMiscValueB() & 1 << i) || GetMiscValueB() == 0) // 0 is also used for all stats
                {
                    float amount = target.GetTotalAuraMultiplier(AuraType.ModTotalStatPercentage, aurEff =>
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
            if ((Convert.ToBoolean(GetMiscValueB() & 1 << (int)Stats.Stamina) || GetMiscValueB() == 0) && m_spellInfo.HasAttribute(SpellAttr0.IsAbility))
                target.SetHealth(Math.Max(MathFunctions.CalculatePct(target.GetMaxHealth(), healthPct), (zeroHealth ? 0 : 1ul)));
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

        // Increase armor by <AuraEffect.BasePoints> % of your <primary stat>
        [AuraEffectHandler(AuraType.ModArmorPctFromStat)]
        void HandleModArmorPctFromStat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            // only players have primary stats
            Player player = aurApp.GetTarget().ToPlayer();
            if (player == null)
                return;

            player.UpdateArmor();
        }

        [AuraEffectHandler(AuraType.ModBonusArmor)]
        void HandleModBonusArmor(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().HandleStatFlatModifier(UnitMods.Armor, UnitModifierFlatType.Base, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModBonusArmorPct)]
        void HandleModBonusArmorPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().UpdateArmor();
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
                    target.HandleStatFlatModifier(UnitMods.StatStart + (int)stat, UnitModifierFlatType.BasePCTExcludeCreate, GetAmount(), apply);
                    target.UpdateStatBuffMod(stat);
                }
            }
        }

        [AuraEffectHandler(AuraType.OverrideSpellPowerByApPct)]
        void HandleOverrideSpellPowerByAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            target.ApplyModOverrideSpellPowerByAPPercent(GetAmount(), apply);
            target.UpdateSpellDamageAndHealingBonus();
        }

        [AuraEffectHandler(AuraType.OverrideAttackPowerBySpPct)]
        void HandleOverrideAttackPowerBySpellPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            target.ApplyModOverrideAPBySpellPowerPercent(GetAmount(), apply);
            target.UpdateAttackPowerAndDamage();
            target.UpdateAttackPowerAndDamage(true);
        }

        [AuraEffectHandler(AuraType.ModVersatility)]
        void HandleModVersatilityByPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target != null)
            {
                target.SetVersatilityBonus(target.GetTotalAuraModifier(AuraType.ModVersatility));
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

            target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, GetAmount(), apply);
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

            int amt = apply ? GetAmount() : -GetAmount();
            if (amt < 0)
                target.ModifyHealth(Math.Max((int)(1 - target.GetHealth()), amt));

            target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);

            if (amt > 0)
                target.ModifyHealth(amt);
        }

        void HandleAuraModIncreaseMaxHealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            float percent = target.GetHealthPct();

            target.HandleStatFlatModifier(UnitMods.Health, UnitModifierFlatType.Total, GetAmount(), apply);

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
            target.HandleStatFlatModifier(unitMod, UnitModifierFlatType.Total, GetAmount(), apply);
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
            if (apply)
                target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseEnergyPercent, aurEff =>
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
            if (apply)
                target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent) * target.GetTotalAuraMultiplier(AuraType.ModIncreaseHealthPercent2);
                target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Total, amount);
            }

            if (target.GetHealth() > 0)
            {
                uint newHealth = (uint)Math.Max(MathFunctions.CalculatePct(target.GetMaxHealth(), (int)percent), 1);
                target.SetHealth(newHealth);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseHealthPct)]
        void HandleAuraIncreaseBaseHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.ApplyStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModBaseHealthPct);
                target.SetStatPctModifier(UnitMods.Health, UnitModifierPctType.Base, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModBaseManaPct)]
        void HandleAuraModIncreaseBaseManaPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.ApplyStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModBaseManaPct);
                target.SetStatPctModifier(UnitMods.Mana, UnitModifierPctType.Base, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModManaCostPct)]
        void HandleModManaCostPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            aurApp.GetTarget().ApplyModManaCostMultiplier(GetAmount() / 100.0f, apply);
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
                target.SetOverrideDisplayPowerId(powerDisplay.Id);
            }
            else
                target.SetOverrideDisplayPowerId(0);
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
            if (apply)
                target.ApplyStatPctModifier(unitMod, UnitModifierPctType.Total, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModMaxPowerPct, aurEff =>
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
            int change = target.GetMaxPower(powerType) - oldMaxPower;
            change = (oldPower + change) - target.GetPower(powerType);
            target.ModifyPower(powerType, change);
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnHealthPct)]
        void HandleTriggerSpellOnHealthPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasFlag(AuraEffectHandleModes.Real) || !apply)
                return;

            Unit target = aurApp.GetTarget();
            int thresholdPct = GetAmount();
            uint triggerSpell = GetSpellEffectInfo().TriggerSpell;

            switch ((AuraTriggerOnHealthChangeDirection)GetMiscValue())
            {
                case AuraTriggerOnHealthChangeDirection.Above:
                    if (!target.HealthAbovePct(thresholdPct))
                        return;
                    break;
                case AuraTriggerOnHealthChangeDirection.Below:
                    if (!target.HealthBelowPct(thresholdPct))
                        return;
                    break;
                default:
                    break;
            }

            target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
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
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsPlayer())
                return;

            target.ToPlayer().UpdateManaRegen();
        }

        [AuraEffectHandler(AuraType.ModWeaponCritPercent)]
        void HandleAuraModWeaponCritPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            target.UpdateAllWeaponDependentCritAuras();
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
                target.ModSpellHitChance += apply ? GetAmount() : (-GetAmount());
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
                target.BaseSpellCritChance += apply ? GetAmount() : -GetAmount();
        }

        [AuraEffectHandler(AuraType.ModCritPct)]
        void HandleAuraModCritPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
            {
                target.BaseSpellCritChance += apply ? GetAmount() : -GetAmount();
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
        void HandleModCastingSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            // Do not apply such auras in normal way
            if (GetAmount() >= 1000)
            {
                if (apply)
                    target.SetInstantCast(true);
                else
                {
                    // only SPELL_AURA_MOD_CASTING_SPEED_NOT_STACK can have this high amount
                    // it's some rare case that you have 2 auras like that, but just in case ;)

                    bool remove = true;
                    var castingSpeedNotStack = target.GetAuraEffectsByType(AuraType.ModCastingSpeedNotStack);
                    foreach (AuraEffect aurEff in castingSpeedNotStack)
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

            int spellGroupVal = target.GetHighestExclusiveSameEffectSpellGroupValue(this, GetAuraType());
            if (Math.Abs(spellGroupVal) >= Math.Abs(GetAmount()))
                return;

            if (spellGroupVal != 0)
                target.ApplyCastTimePercentMod(spellGroupVal, !apply);

            target.ApplyCastTimePercentMod(GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModMeleeRangedHaste)]
        [AuraEffectHandler(AuraType.ModMeleeRangedHaste2)]
        void HandleModMeleeRangedSpeedPct(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
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

            target.ApplyCastTimePercentMod(GetAmount(), apply);
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

            target.HandleStatFlatModifier(UnitMods.AttackPower, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPower)]
        void HandleAuraModRangedAttackPower(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if ((target.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            target.HandleStatFlatModifier(UnitMods.AttackPowerRanged, UnitModifierFlatType.Total, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModAttackPowerPct)]
        void HandleAuraModAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            //UNIT_FIELD_ATTACK_POWER_MULTIPLIER = multiplier - 1
            if (apply)
                target.ApplyStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModAttackPowerPct);
                target.SetStatPctModifier(UnitMods.AttackPower, UnitModifierPctType.Total, amount);
            }
        }

        [AuraEffectHandler(AuraType.ModRangedAttackPowerPct)]
        void HandleAuraModRangedAttackPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            if ((target.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                return;

            //UNIT_FIELD_RANGED_ATTACK_POWER_MULTIPLIER = multiplier - 1
            if (apply)
                target.ApplyStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModRangedAttackPowerPct);
                target.SetStatPctModifier(UnitMods.AttackPowerRanged, UnitModifierPctType.Total, amount);
            }
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
                target.UpdateAllDamageDoneMods();

            // Magic damage modifiers implemented in Unit::SpellBaseDamageBonusDone
            // This information for client side use only
            Player playerTarget = target.ToPlayer();
            if (playerTarget != null)
            {
                for (int i = 0; i < (int)SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    {
                        if (GetAmount() >= 0)
                            playerTarget.ApplyModDamageDonePos((SpellSchools)i, GetAmount(), apply);
                        else
                            playerTarget.ApplyModDamageDoneNeg((SpellSchools)i, GetAmount(), apply);
                    }
                }

                Guardian pet = playerTarget.GetGuardianPet();
                if (pet != null)
                    pet.UpdateAttackPowerAndDamage();
            }
        }

        [AuraEffectHandler(AuraType.ModDamagePercentDone)]
        void HandleModDamagePercentDone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Unit target = aurApp.GetTarget();

            // also handles spell group stacks
            if (Convert.ToBoolean(GetMiscValue() & (int)SpellSchoolMask.Normal))
                target.UpdateAllDamagePctDoneMods();

            Player thisPlayer = target.ToPlayer();
            if (thisPlayer != null)
            {
                for (var i = SpellSchools.Normal; i < SpellSchools.Max; ++i)
                {
                    if (Convert.ToBoolean(GetMiscValue() & (1 << (int)i)))
                    {
                        // only aura type modifying PLAYER_FIELD_MOD_DAMAGE_DONE_PCT
                        float amount = thisPlayer.GetTotalAuraMultiplierByMiscMask(AuraType.ModDamagePercentDone, 1u << (int)i);
                        thisPlayer.SetModDamageDonePercent(i, amount);
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

            // also handles spell group stacks
            target.UpdateDamagePctDoneMods(WeaponAttackType.OffAttack);
        }

        void HandleShieldBlockValue(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player != null)
                player.HandleBaseModFlatValue(BaseModGroup.ShieldBlockValue, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ModShieldBlockvaluePct)]
        void HandleShieldBlockValuePercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag((AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Stat)))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.ApplyBaseModPctValue(BaseModGroup.ShieldBlockValue, GetAmount());
            else
            {
                float amount = target.GetTotalAuraMultiplier(AuraType.ModShieldBlockvaluePct);
                target.SetBaseModPctValue(BaseModGroup.ShieldBlockValue, amount);
            }
        }

        /********************************/
        /***        POWER COST        ***/
        /********************************/
        [AuraEffectHandler(AuraType.ModPowerCostSchool)]
        void HandleModPowerCost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            // handled in SpellInfo::CalcPowerCost, this is only for client UI
            if ((GetMiscValueB() & (1 << (int)PowerType.Mana)) == 0)
                return;

            Unit target = aurApp.GetTarget();

            for (int i = 0; i < (int)SpellSchools.Max; ++i)
                if (Convert.ToBoolean(GetMiscValue() & (1 << i)))
                    target.ApplyModManaCostModifier((SpellSchools)i, GetAmount(), apply);
        }

        [AuraEffectHandler(AuraType.ArenaPreparation)]
        void HandleArenaPreparation(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetUnitFlag(UnitFlags.Preparation);
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;
                target.RemoveUnitFlag(UnitFlags.Preparation);
            }

            target.ModifyAuraState(AuraStateType.ArenaPreparation, apply);
        }

        [AuraEffectHandler(AuraType.NoReagentUse)]
        void HandleNoReagentUseAura(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            FlagArray128 mask = new();
            var noReagent = target.GetAuraEffectsByType(AuraType.NoReagentUse);
            foreach (var eff in noReagent)
            {
                SpellEffectInfo effect = eff.GetSpellEffectInfo();
                if (effect != null)
                    mask |= effect.SpellClassMask;
            }

            target.ToPlayer().SetNoRegentCostMask(mask);
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

            // pet auras
            if (target.GetTypeId() == TypeId.Player && mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                PetAura petSpell = Global.SpellMgr.GetPetAura(GetId(), (byte)GetEffIndex());
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
                                caster.CastSpell(caster, 13138, new CastSpellExtraArgs(this));
                            break;
                        case 34026:   // kill command
                        {
                            Unit pet = target.GetGuardianPet();
                            if (pet == null)
                                break;

                            target.CastSpell(target, 34027, new CastSpellExtraArgs(this));

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
                                    caster.CastSpell(target, 37095, new CastSpellExtraArgs(this)); // Blood Elf Disguise
                                else
                                    caster.CastSpell(target, 37093, new CastSpellExtraArgs(this));
                            }
                            break;
                        }
                        case 39850:                                     // Rocket Blast
                            if (RandomHelper.randChance(20))                       // backfire stun
                                target.CastSpell(target, 51581, new CastSpellExtraArgs(this));
                            break;
                        case 43873:                                     // Headless Horseman Laugh
                            target.PlayDistanceSound(11965);
                            break;
                        case 46354:                                     // Blood Elf Illusion
                            if (caster != null)
                            {
                                if (caster.GetGender() == Gender.Female)
                                    caster.CastSpell(target, 46356, new CastSpellExtraArgs(this));
                                else
                                    caster.CastSpell(target, 46355, new CastSpellExtraArgs(this));
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
                    switch (m_spellInfo.SpellFamilyName)
                    {
                        case SpellFamilyNames.Generic:
                            switch (GetId())
                            {
                                case 36730:                                     // Flame Strike
                                    target.CastSpell(target, 36731, new CastSpellExtraArgs(this));
                                    break;
                                case 43681: // Inactive
                                {
                                    if (!target.IsTypeId(TypeId.Player) || aurApp.GetRemoveMode() != AuraRemoveMode.Expire)
                                        return;

                                    if (target.GetMap().IsBattleground())
                                        target.ToPlayer().LeaveBattleground();
                                    break;
                                }
                                case 42783: // Wrath of the Astromancer
                                    target.CastSpell(target, (uint)GetAmount(), new CastSpellExtraArgs(this));
                                    break;
                                case 46308: // Burning Winds casted only at creatures at spawn
                                    target.CastSpell(target, 47287, new CastSpellExtraArgs(this));
                                    break;
                                case 52172:  // Coyote Spirit Despawn Aura
                                case 60244:  // Blood Parrot Despawn Aura
                                    target.CastSpell((Unit)null, (uint)GetAmount(), new CastSpellExtraArgs(this));
                                    break;
                                case 91604: // Restricted Flight Area
                                    if (aurApp.GetRemoveMode() == AuraRemoveMode.Expire)
                                        target.CastSpell(target, 58601, new CastSpellExtraArgs(this));
                                    break;
                            }
                            break;
                        case SpellFamilyNames.Deathknight:
                            // Summon Gargoyle (Dismiss Gargoyle at remove)
                            if (GetId() == 61777)
                                target.CastSpell(target, (uint)GetAmount(), new CastSpellExtraArgs(this));
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
                                SpellInfo spell = Global.SpellMgr.GetSpellInfo(spellId, GetBase().GetCastDifficulty());
                                CastSpellExtraArgs args = new();
                                args.TriggerFlags = TriggerCastFlags.FullMask;
                                args.OriginalCaster = GetCasterGUID();
                                args.OriginalCastId = GetBase().GetCastId();
                                args.CastDifficulty = GetBase().GetCastDifficulty();

                                for (uint i = 0; i < spell.StackAmount; ++i)
                                    caster.CastSpell(target, spell.Id, args);
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
                                SpellInfo spell = Global.SpellMgr.GetSpellInfo(spellId, GetBase().GetCastDifficulty());
                                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                args.OriginalCaster = GetCasterGUID();
                                args.OriginalCastId = GetBase().GetCastId();
                                args.CastDifficulty = GetBase().GetCastDifficulty();

                                for (uint i = 0; i < spell.StackAmount; ++i)
                                    caster.CastSpell(target, spell.Id, args);
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
                            if (caster == null || !caster.IsTypeId(TypeId.Player))
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
                                    target.CastSpell(target, 58205, new CastSpellExtraArgs(this));
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
                if (!plCaster.IsHonorOrXPTarget(target) ||
                    (target.IsTypeId(TypeId.Unit) && !target.ToCreature().IsTappedBy(plCaster)))
                    return;
            }

            //Adding items
            uint noSpaceForCount;
            uint count = (uint)GetAmount();

            List<ItemPosCount> dest = new();
            InventoryResult msg = plCaster.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, GetSpellEffectInfo().ItemType, count, out noSpaceForCount);
            if (msg != InventoryResult.Ok)
            {
                count -= noSpaceForCount;
                plCaster.SendEquipError(msg, null, null, GetSpellEffectInfo().ItemType);
                if (count == 0)
                    return;
            }

            Item newitem = plCaster.StoreNewItem(dest, GetSpellEffectInfo().ItemType, true);
            if (newitem != null)
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
            ReputationRank factionRank = (ReputationRank)GetAmount();

            player.GetReputationMgr().ApplyForceReaction(factionId, factionRank, apply);

            // stop fighting at apply (if forced rank friendly) or at remove (if real rank friendly)
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
            {
                if (apply)
                    target.SetDynamicFlag(UnitDynFlags.SpecialInfo);
                else
                    target.RemoveDynamicFlag(UnitDynFlags.SpecialInfo);
            }
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
                    target.RemoveUnitFlag(UnitFlags.PlayerControlled);
            }
            else
            {
                target.RestoreFaction();
                if (target.IsTypeId(TypeId.Player))
                    target.SetUnitFlag(UnitFlags.PlayerControlled);
            }
        }

        [AuraEffectHandler(AuraType.LearnSpell)]
        void HandleLearnSpell(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player == null)
                return;

            if (apply)
                player.LearnSpell((uint)GetMiscValue(), true, 0, true);
            else
                player.RemoveSpell((uint)GetMiscValue(), false, false, true);
        }

        [AuraEffectHandler(AuraType.ComprehendLanguage)]
        void HandleComprehendLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetUnitFlag2(UnitFlags2.ComprehendLang);
            else
            {
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag2(UnitFlags2.ComprehendLang);
            }
        }

        [AuraEffectHandler(AuraType.ModAlternativeDefaultLanguage)]
        void HandleModAlternativeDefaultLanguage(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                target.SetUnitFlag3(UnitFlags3.AlternativeDefaultLanguage);
            else
            {
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag3(UnitFlags3.AlternativeDefaultLanguage);
            }
        }

        [AuraEffectHandler(AuraType.Linked)]
        void HandleAuraLinked(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            Unit target = aurApp.GetTarget();

            uint triggeredSpellId = GetSpellEffectInfo().TriggerSpell;
            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggeredSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo == null)
                return;

            Unit caster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? GetCaster() : target;
            if (caster == null)
                return;

            if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
            {
                if (apply)
                {
                    CastSpellExtraArgs args = new(this);
                    if (GetAmount() != 0) // If amount avalible cast with basepoints (Crypt Fever for example)
                        args.AddSpellMod(SpellValueMod.BasePoint0, GetAmount());

                    caster.CastSpell(target, triggeredSpellId, args);
                }
                else
                {
                    ObjectGuid casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? GetCasterGUID() : target.GetGUID();
                    target.RemoveAura(triggeredSpellId, casterGUID);
                }
            }
            else if (mode.HasAnyFlag(AuraEffectHandleModes.Reapply) && apply)
            {
                ObjectGuid casterGUID = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? GetCasterGUID() : target.GetGUID();
                // change the stack amount to be equal to stack amount of our aura
                Aura triggeredAura = target.GetAura(triggeredSpellId, casterGUID);
                if (triggeredAura != null)
                    triggeredAura.ModStackAmount(GetBase().GetStackAmount() - triggeredAura.GetStackAmount());
            }
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnPowerPct)]
        void HandleTriggerSpellOnPowerPercent(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real) || !apply)
                return;

            Unit target = aurApp.GetTarget();

            int effectAmount = GetAmount();
            uint triggerSpell = GetSpellEffectInfo().TriggerSpell;
            float powerAmountPct = MathFunctions.GetPctOf(target.GetPower((PowerType)GetMiscValue()), target.GetMaxPower((PowerType)GetMiscValue()));

            switch ((AuraTriggerOnPowerChangeDirection)GetMiscValueB())
            {
                case AuraTriggerOnPowerChangeDirection.Gain:
                    if (powerAmountPct < effectAmount)
                        return;
                    break;
                case AuraTriggerOnPowerChangeDirection.Loss:
                    if (powerAmountPct > effectAmount)
                        return;
                    break;
                default:
                    break;
            }

            target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnPowerAmount)]
        void HandleTriggerSpellOnPowerAmount(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real) || !apply)
                return;

            Unit target = aurApp.GetTarget();

            int effectAmount = GetAmount();
            uint triggerSpell = GetSpellEffectInfo().TriggerSpell;
            float powerAmount = target.GetPower((PowerType)GetMiscValue());

            switch ((AuraTriggerOnPowerChangeDirection)GetMiscValueB())
            {
                case AuraTriggerOnPowerChangeDirection.Gain:
                    if (powerAmount < effectAmount)
                        return;
                    break;
                case AuraTriggerOnPowerChangeDirection.Loss:
                    if (powerAmount > effectAmount)
                        return;
                    break;
                default:
                    break;
            }

            target.CastSpell(target, triggerSpell, new CastSpellExtraArgs(this));
        }

        [AuraEffectHandler(AuraType.TriggerSpellOnExpire)]
        void HandleTriggerSpellOnExpire(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasFlag(AuraEffectHandleModes.Real) || apply || aurApp.GetRemoveMode() != AuraRemoveMode.Expire)
                return;

            Unit caster = aurApp.GetTarget();

            // MiscValue (Caster):
            // 0 - Aura target
            // 1 - Aura caster
            // 2 - ? Aura target is always TARGET_UNIT_CASTER so we consider the same behavior as MiscValue 1
            uint casterType = (uint)GetMiscValue();
            if (casterType > 0)
                caster = GetCaster();

            caster?.CastSpell(aurApp.GetTarget(), GetSpellEffectInfo().TriggerSpell, new CastSpellExtraArgs(this));
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
                target.ToPlayer().SetStableMaster(target.GetGUID());

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

                Player playerTarget = target.ToPlayer();
                if (playerTarget != null)
                    playerTarget.ApplyModFakeInebriation(GetAmount(), true);
            }
            else
            {
                bool removeDetect = !target.HasAuraType(AuraType.ModFakeInebriate);

                target.m_invisibilityDetect.AddValue(InvisibilityType.Drunk, -GetAmount());

                Player playerTarget = target.ToPlayer();
                if (playerTarget != null)
                {
                    playerTarget.ApplyModFakeInebriation(GetAmount(), false);

                    if (removeDetect)
                        removeDetect = playerTarget.GetDrunkValue() == 0;
                }

                if (removeDetect)
                    target.m_invisibilityDetect.DelFlag(InvisibilityType.Drunk);
            }

            // call functions which may have additional effects after changing state of unit
            if (target.IsInWorld)
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
                target.SetOverrideSpellsId(overrideId);
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
                target.SetOverrideSpellsId(0);
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

            target.RemoveVehicleKit();

            if (apply)
            {
                if (!target.CreateVehicleKit((uint)vehicleId, 0))
                    return;
            }
            else
            {
                Creature creature = target.ToCreature();
                if (creature != null)
                {
                    uint originalVehicleId = creature.GetCreatureTemplate().VehicleId;
                    if (originalVehicleId != 0)
                        creature.CreateVehicleKit(originalVehicleId, creature.GetEntry());
                }
            }

            if (!target.IsTypeId(TypeId.Player))
                return;

            if (apply)
                target.ToPlayer().SendOnCancelExpectedVehicleRideAura();
        }

        [AuraEffectHandler(AuraType.SetVignette)]
        void HandleSetVignette(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            aurApp.GetTarget().SetVignette((uint)(apply ? GetMiscValue() : 0));
        }

        [AuraEffectHandler(AuraType.PreventResurrection)]
        void HandlePreventResurrection(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.RemovePlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
            else if (!target.GetMap().Instanceable())
                target.SetPlayerLocalFlag(PlayerLocalFlags.ReleaseTimer);
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

        void HandlePeriodicTriggerSpellAuraTick(Unit target, Unit caster)
        {
            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandlePeriodicTriggerSpellAuraTick: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");
                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                Unit triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? caster : target;
                if (triggerCaster != null)
                {
                    triggerCaster.CastSpell(target, triggerSpellId, new CastSpellExtraArgs(this));
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
                Log.outError(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
        }

        void HandlePeriodicTriggerSpellWithValueAuraTick(Unit target, Unit caster)
        {
            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandlePeriodicTriggerSpellWithValueAuraTick: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");
                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                Unit triggerCaster = triggeredSpellInfo.NeedsToBeTriggeredByCaster(m_spellInfo) ? caster : target;
                if (triggerCaster != null)
                {
                    CastSpellExtraArgs args = new(this);
                    for (int i = 0; i < SpellConst.MaxEffects; ++i)
                        args.AddSpellMod(SpellValueMod.BasePoint0 + i, GetAmount());

                    triggerCaster.CastSpell(target, triggerSpellId, args);
                    Log.outDebug(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} Trigger {1}", GetId(), triggeredSpellInfo.Id);
                }
            }
            else
                Log.outError(LogFilter.Spells, "AuraEffect.HandlePeriodicTriggerSpellWithValueAuraTick: Spell {0} has non-existent spell {1} in EffectTriggered[{2}] and is therefor not triggered.", GetId(), triggerSpellId, GetEffIndex());
        }

        void HandlePeriodicDamageAurasTick(Unit target, Unit caster)
        {
            if (!target.IsAlive())
                return;

            if (target.IsImmunedToDamage(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // Consecrate ticks can miss and will not show up in the combat log
            // dynobj auras must always have a caster
            if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            CleanDamage cleanDamage = new(0, 0, WeaponAttackType.BaseAttack, MeleeHitOutcome.Normal);

            uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.SuppressPointsStacking) ? GetBase().GetStackAmount() : 1u;

            // ignore non positive values (can be result apply spellmods to aura damage
            uint damage = (uint)Math.Max(GetAmount(), 0);

            // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
            Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);

            switch (GetAuraType())
            {
                case AuraType.PeriodicDamage:
                {
                    if (caster != null)
                        damage = (uint)caster.SpellDamageBonusDone(target, GetSpellInfo(), (int)damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses, null, this);

                    damage = (uint)target.SpellDamageBonusTaken(caster, GetSpellInfo(), (int)damage, DamageEffectType.DOT);

                    if (GetSpellInfo().SpellFamilyName == SpellFamilyNames.Generic)
                    {
                        switch (GetId())
                        {
                            case 70911: // Unbound Plague
                            case 72854: // Unbound Plague
                            case 72855: // Unbound Plague
                            case 72856: // Unbound Plague
                                damage *= (uint)Math.Pow(1.25f, _ticksDone);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                }
                case AuraType.PeriodicWeaponPercentDamage:
                {
                    WeaponAttackType attackType = GetSpellInfo().GetAttackType();

                    damage = MathFunctions.CalculatePct(caster.CalculateDamage(attackType, false, true), GetAmount());

                    // Add melee damage bonuses (also check for negative)
                    if (caster != null)
                        damage = (uint)caster.MeleeDamageBonusDone(target, (int)damage, attackType, DamageEffectType.DOT, GetSpellInfo(), GetSpellEffectInfo().Mechanic, GetSpellInfo().GetSchoolMask(), null, this);

                    damage = (uint)target.MeleeDamageBonusTaken(caster, (int)damage, attackType, DamageEffectType.DOT, GetSpellInfo());
                    break;
                }
                case AuraType.PeriodicDamagePercent:
                    // ceil obtained value, it may happen that 10 ticks for 10% damage may not kill owner
                    damage = (uint)Math.Ceiling(MathFunctions.CalculatePct((float)target.GetMaxHealth(), (float)damage));
                    damage = (uint)target.SpellDamageBonusTaken(caster, GetSpellInfo(), (int)damage, DamageEffectType.DOT);
                    break;
                default:
                    break;
            }

            bool crit = RandomHelper.randChance(GetCritChanceFor(caster, target));
            if (crit)
                damage = Unit.SpellCriticalDamageBonus(caster, m_spellInfo, damage, target);

            // Calculate armor mitigation
            if (Unit.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo()))
            {
                uint damageReducedArmor = Unit.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetAttackType(), GetBase().GetCasterLevel());
                cleanDamage.mitigated_damage += damage - damageReducedArmor;
                damage = damageReducedArmor;
            }

            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
            {
                if (GetSpellEffectInfo().IsTargetingArea() || GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) || GetSpellInfo().HasAttribute(SpellAttr5.TreatAsAreaEffect) || GetSpellInfo().HasAttribute(SpellAttr7.TreatAsNpcAoe))
                    damage = (uint)target.CalculateAOEAvoidance((int)damage, (uint)m_spellInfo.SchoolMask, (caster != null && !caster.IsControlledByPlayer()) || GetSpellInfo().HasAttribute(SpellAttr7.TreatAsNpcAoe));
            }

            int dmg = (int)damage;
            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers) && caster != null && caster.CanApplyResilience())
                Unit.ApplyResilience(target, ref dmg);
            damage = (uint)dmg;

            DamageInfo damageInfo = new(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, WeaponAttackType.BaseAttack);
            Unit.CalcAbsorbResist(damageInfo);
            damage = damageInfo.GetDamage();

            uint absorb = damageInfo.GetAbsorb();
            uint resist = damageInfo.GetResist();
            Unit.DealDamageMods(caster, target, ref damage, ref absorb);

            // Set trigger flag
            ProcFlagsInit procAttacker = new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic);
            ProcFlagsInit procVictim = new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic);
            ProcFlagsHit hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim.Or(ProcFlags.TakeAnyDamage);
            }

            int overkill = (int)(damage - target.GetHealth());
            if (overkill < 0)
                overkill = 0;

            SpellPeriodicAuraLogInfo pInfo = new(this, damage, (uint)dmg, (uint)overkill, absorb, resist, 0.0f, crit);

            Unit.DealDamage(caster, target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), true);

            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, null, damageInfo, null);
            target.SendPeriodicAuraLog(pInfo);
        }

        void HandlePeriodicHealthLeechAuraTick(Unit target, Unit caster)
        {
            if (!target.IsAlive())
                return;

            if (target.IsImmunedToDamage(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // dynobj auras must always have a caster
            if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            CleanDamage cleanDamage = new(0, 0, GetSpellInfo().GetAttackType(), MeleeHitOutcome.Normal);

            uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.SuppressPointsStacking) ? GetBase().GetStackAmount() : 1u;

            // ignore negative values (can be result apply spellmods to aura damage
            uint damage = (uint)Math.Max(GetAmount(), 0);

            if (caster != null)
                damage = (uint)caster.SpellDamageBonusDone(target, GetSpellInfo(), (int)damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses, null, this);

            damage = (uint)target.SpellDamageBonusTaken(caster, GetSpellInfo(), (int)damage, DamageEffectType.DOT);

            bool crit = RandomHelper.randChance(GetCritChanceFor(caster, target));
            if (crit)
                damage = Unit.SpellCriticalDamageBonus(caster, m_spellInfo, damage, target);

            // Calculate armor mitigation
            if (Unit.IsDamageReducedByArmor(GetSpellInfo().GetSchoolMask(), GetSpellInfo()))
            {
                uint damageReducedArmor = Unit.CalcArmorReducedDamage(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetAttackType(), GetBase().GetCasterLevel());
                cleanDamage.mitigated_damage += damage - damageReducedArmor;
                damage = damageReducedArmor;
            }

            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers))
            {
                if (GetSpellEffectInfo().IsTargetingArea() || GetSpellEffectInfo().IsAreaAuraEffect() || GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) || GetSpellInfo().HasAttribute(SpellAttr5.TreatAsAreaEffect) || GetSpellInfo().HasAttribute(SpellAttr7.TreatAsNpcAoe))
                    damage = (uint)target.CalculateAOEAvoidance((int)damage, (uint)m_spellInfo.SchoolMask, (caster != null && !caster.IsControlledByPlayer()) || GetSpellInfo().HasAttribute(SpellAttr7.TreatAsNpcAoe));
            }

            int dmg = (int)damage;
            if (!GetSpellInfo().HasAttribute(SpellAttr4.IgnoreDamageTakenModifiers) && caster != null && caster.CanApplyResilience())
                Unit.ApplyResilience(target, ref dmg);

            damage = (uint)dmg;

            DamageInfo damageInfo = new(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask(), DamageEffectType.DOT, GetSpellInfo().GetAttackType());
            Unit.CalcAbsorbResist(damageInfo);

            uint absorb = damageInfo.GetAbsorb();
            uint resist = damageInfo.GetResist();

            // SendSpellNonMeleeDamageLog expects non-absorbed/non-resisted damage
            SpellNonMeleeDamage log = new(caster, target, GetSpellInfo(), GetBase().GetSpellVisual(), GetSpellInfo().GetSchoolMask(), GetBase().GetCastId());
            log.damage = damage;
            log.originalDamage = (uint)dmg;
            log.absorb = absorb;
            log.resist = resist;
            log.periodicLog = true;
            if (crit)
                log.HitInfo |= HitInfo.CriticalHit;

            // Set trigger flag
            ProcFlagsInit procAttacker = new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic);
            ProcFlagsInit procVictim = new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic);
            ProcFlagsHit hitMask = damageInfo.GetHitMask();

            if (damage != 0)
            {
                hitMask |= crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
                procVictim.Or(ProcFlags.TakeAnyDamage);
            }

            int new_damage = (int)Unit.DealDamage(caster, target, damage, cleanDamage, DamageEffectType.DOT, GetSpellInfo().GetSchoolMask(), GetSpellInfo(), false);
            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Damage, ProcFlagsSpellPhase.Hit, hitMask, null, damageInfo, null);

            // process caster heal from now on (must be in world)
            if (caster == null || !caster.IsAlive())
                return;

            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            uint heal = (uint)caster.SpellHealingBonusDone(caster, GetSpellInfo(), (int)(new_damage * gainMultiplier), DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses, null, this);
            heal = (uint)caster.SpellHealingBonusTaken(caster, GetSpellInfo(), (int)heal, DamageEffectType.DOT);

            HealInfo healInfo = new(caster, caster, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.HealBySpell(healInfo);

            caster.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());
            Unit.ProcSkillsAndAuras(caster, caster, new ProcFlagsInit(ProcFlags.DealHelpfulPeriodic), new ProcFlagsInit(ProcFlags.TakeHelpfulPeriodic), ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, null, null, healInfo);

            caster.SendSpellNonMeleeDamageLog(log);
        }

        void HandlePeriodicHealthFunnelAuraTick(Unit target, Unit caster)
        {
            if (caster == null || !caster.IsAlive() || !target.IsAlive())
                return;

            if (target.IsImmunedToAuraPeriodicTick(caster, GetSpellInfo(), GetSpellEffectInfo()))
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

            HealInfo healInfo = new(caster, target, damage, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            caster.HealBySpell(healInfo);
            Unit.ProcSkillsAndAuras(caster, target, new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic), new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic), ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, ProcFlagsHit.Normal, null, null, healInfo);
        }

        void HandlePeriodicHealAurasTick(Unit target, Unit caster)
        {
            if (!target.IsAlive())
                return;

            if (target.IsImmunedToAuraPeriodicTick(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // don't regen when permanent aura target has full power
            if (GetBase().IsPermanent() && target.IsFullHealth())
                return;

            uint stackAmountForBonuses = !GetSpellEffectInfo().EffectAttributes.HasFlag(SpellEffectAttributes.SuppressPointsStacking) ? GetBase().GetStackAmount() : 1u;

            // ignore negative values (can be result apply spellmods to aura damage
            uint damage = (uint)Math.Max(GetAmount(), 0);

            if (GetAuraType() == AuraType.ObsModHealth)
                damage = (uint)target.CountPctFromMaxHealth((int)damage);
            else if (caster != null)
                damage = (uint)caster.SpellHealingBonusDone(target, GetSpellInfo(), (int)damage, DamageEffectType.DOT, GetSpellEffectInfo(), stackAmountForBonuses, null, this);

            damage = (uint)target.SpellHealingBonusTaken(caster, GetSpellInfo(), (int)damage, DamageEffectType.DOT);

            bool crit = RandomHelper.randChance(GetCritChanceFor(caster, target));
            if (crit)
                damage = (uint)Unit.SpellCriticalHealingBonus(caster, m_spellInfo, (int)damage, target);

            Log.outDebug(LogFilter.Spells, "PeriodicTick: {0} (TypeId: {1}) heal of {2} (TypeId: {3}) for {4} health inflicted by {5}",
                GetCasterGUID().ToString(), GetCaster().GetTypeId(), target.GetGUID().ToString(), target.GetTypeId(), damage, GetId());

            uint heal = (uint)damage;

            HealInfo healInfo = new(caster, target, heal, GetSpellInfo(), GetSpellInfo().GetSchoolMask());
            Unit.CalcHealAbsorb(healInfo);
            Unit.DealHeal(healInfo);

            SpellPeriodicAuraLogInfo pInfo = new(this, heal, (uint)damage, heal - healInfo.GetEffectiveHeal(), healInfo.GetAbsorb(), 0, 0.0f, crit);
            target.SendPeriodicAuraLog(pInfo);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, healInfo.GetEffectiveHeal() * 0.5f, GetSpellInfo());

            // %-based heal - does not proc auras
            if (GetAuraType() == AuraType.ObsModHealth)
                return;

            ProcFlagsInit procAttacker = new ProcFlagsInit(ProcFlags.DealHelpfulPeriodic);
            ProcFlagsInit procVictim = new ProcFlagsInit(ProcFlags.TakeHelpfulPeriodic);
            ProcFlagsHit hitMask = crit ? ProcFlagsHit.Critical : ProcFlagsHit.Normal;
            // ignore item heals
            if (GetBase().GetCastItemGUID().IsEmpty())
                Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, ProcFlagsSpellType.Heal, ProcFlagsSpellPhase.Hit, hitMask, null, null, healInfo);
        }

        void HandlePeriodicManaLeechAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

            if (caster == null || !caster.IsAlive() || !target.IsAlive() || target.GetPowerType() != powerType)
                return;

            if (target.IsImmunedToAuraPeriodicTick(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            if (GetSpellEffectInfo().IsEffect(SpellEffectName.PersistentAreaAura) &&
                caster.SpellHitResult(target, GetSpellInfo(), false) != SpellMissInfo.None)
                return;

            // ignore negative values (can be result apply spellmods to aura damage
            int drainAmount = Math.Max(GetAmount(), 0);

            int drainedAmount = -target.ModifyPower(powerType, -drainAmount);
            float gainMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            SpellPeriodicAuraLogInfo pInfo = new(this, (uint)drainedAmount, (uint)drainAmount, 0, 0, 0, gainMultiplier, false);

            int gainAmount = (int)(drainedAmount * gainMultiplier);
            int gainedAmount = 0;
            if (gainAmount != 0)
            {
                gainedAmount = caster.ModifyPower(powerType, gainAmount);
                // energize is not modified by threat modifiers
                if (!GetSpellInfo().HasAttribute(SpellAttr4.NoHelpfulThreat))
                    target.GetThreatManager().AddThreat(caster, gainedAmount * 0.5f, GetSpellInfo(), true);
            }

            // Drain Mana
            if (caster.GetGuardianPet() != null && m_spellInfo.SpellFamilyName == SpellFamilyNames.Warlock && m_spellInfo.SpellFamilyFlags[0].HasAnyFlag<uint>(0x00000010))
            {
                int manaFeedVal = 0;
                AuraEffect aurEff = GetBase().GetEffect(1);
                if (aurEff != null)
                    manaFeedVal = aurEff.GetAmount();

                if (manaFeedVal > 0)
                {
                    int feedAmount = MathFunctions.CalculatePct(gainedAmount, manaFeedVal);

                    CastSpellExtraArgs args = new(this);
                    args.AddSpellMod(SpellValueMod.BasePoint0, feedAmount);
                    caster.CastSpell(caster, 32554, args);
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

            if (target.IsImmunedToAuraPeriodicTick(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // don't regen when permanent aura target has full power
            if (GetBase().IsPermanent() && target.GetPower(powerType) == target.GetMaxPower(powerType))
                return;

            // ignore negative values (can be result apply spellmods to aura damage
            int amount = Math.Max(GetAmount(), 0) * target.GetMaxPower(powerType) / 100;

            SpellPeriodicAuraLogInfo pInfo = new(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);

            int gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, GetSpellInfo(), true);

            target.SendPeriodicAuraLog(pInfo);
        }

        void HandlePeriodicEnergizeAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();
            if (!target.IsAlive() || target.GetMaxPower(powerType) == 0)
                return;

            if (target.IsImmunedToAuraPeriodicTick(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // don't regen when permanent aura target has full power
            if (GetBase().IsPermanent() && target.GetPower(powerType) == target.GetMaxPower(powerType))
                return;

            // ignore negative values (can be result apply spellmods to aura damage
            int amount = Math.Max(GetAmount(), 0);

            SpellPeriodicAuraLogInfo pInfo = new(this, (uint)amount, (uint)amount, 0, 0, 0, 0.0f, false);
            int gain = target.ModifyPower(powerType, amount);

            if (caster != null)
                target.GetThreatManager().ForwardThreatForAssistingMe(caster, gain * 0.5f, GetSpellInfo(), true);

            target.SendPeriodicAuraLog(pInfo);
        }

        void HandlePeriodicPowerBurnAuraTick(Unit target, Unit caster)
        {
            PowerType powerType = (PowerType)GetMiscValue();

            if (caster == null || !target.IsAlive() || target.GetPowerType() != powerType)
                return;

            if (target.IsImmunedToDamage(caster, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(target, caster);
                return;
            }

            // ignore negative values (can be result apply spellmods to aura damage
            int damage = Math.Max(GetAmount(), 0);

            uint gain = (uint)(-target.ModifyPower(powerType, -damage));

            float dmgMultiplier = GetSpellEffectInfo().CalcValueMultiplier(caster);

            SpellInfo spellProto = GetSpellInfo();
            // maybe has to be sent different to client, but not by SMSG_PERIODICAURALOG
            SpellNonMeleeDamage damageInfo = new(caster, target, spellProto, GetBase().GetSpellVisual(), spellProto.SchoolMask, GetBase().GetCastId());
            damageInfo.periodicLog = true;
            // no SpellDamageBonus for burn mana
            caster.CalculateSpellDamageTaken(damageInfo, (int)(gain * dmgMultiplier), spellProto);

            Unit.DealDamageMods(damageInfo.attacker, damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);

            // Set trigger flag
            ProcFlagsInit procAttacker = new ProcFlagsInit(ProcFlags.DealHarmfulPeriodic);
            ProcFlagsInit procVictim = new ProcFlagsInit(ProcFlags.TakeHarmfulPeriodic);
            ProcFlagsHit hitMask = Unit.CreateProcHitMask(damageInfo, SpellMissInfo.None);
            ProcFlagsSpellType spellTypeMask = ProcFlagsSpellType.NoDmgHeal;
            if (damageInfo.damage != 0)
            {
                procVictim.Or(ProcFlags.TakeAnyDamage);
                spellTypeMask |= ProcFlagsSpellType.Damage;
            }

            caster.DealSpellDamage(damageInfo, true);

            DamageInfo dotDamageInfo = new(damageInfo, DamageEffectType.DOT, WeaponAttackType.BaseAttack, hitMask);
            Unit.ProcSkillsAndAuras(caster, target, procAttacker, procVictim, spellTypeMask, ProcFlagsSpellPhase.Hit, hitMask, null, dotDamageInfo, null);

            caster.SendSpellNonMeleeDamageLog(damageInfo);
        }

        bool CanPeriodicTickCrit()
        {
            if (GetSpellInfo().HasAttribute(SpellAttr2.CantCrit))
                return false;

            if (GetSpellInfo().HasAttribute(SpellAttr8.PeriodicCanCrit))
                return true;

            return false;
        }

        float CalcPeriodicCritChance(Unit caster)
        {
            if (caster == null || !CanPeriodicTickCrit())
                return 0.0f;

            Player modOwner = caster.GetSpellModOwner();
            if (modOwner == null)
                return 0.0f;

            float critChance = modOwner.SpellCritChanceDone(null, this, GetSpellInfo().GetSchoolMask(), GetSpellInfo().GetAttackType());
            return Math.Max(0.0f, critChance);
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
            if (GetSpellInfo().HasAttribute(SpellAttr8.TargetProcsOnCaster) && eventInfo.GetTypeMask().HasFlag(ProcFlags.TakenHitMask))
                triggerTarget = eventInfo.GetActor();

            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandleProcTriggerSpellAuraProc: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");
                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                Log.outDebug(LogFilter.Spells, $"AuraEffect.HandleProcTriggerSpellAuraProc: Triggering spell {triggeredSpellInfo.Id} from aura {GetId()} proc");
                triggerCaster.CastSpell(triggerTarget, triggeredSpellInfo.Id, new CastSpellExtraArgs(this).SetTriggeringSpell(eventInfo.GetProcSpell()));
            }
            else if (triggerSpellId != 0 && GetAuraType() != AuraType.Dummy)
                Log.outError(LogFilter.Spells, $"AuraEffect.HandleProcTriggerSpellAuraProc: Spell {GetId()} has non-existent spell {triggerSpellId} in EffectTriggered[{GetEffIndex()}] and is therefore not triggered.");
        }

        void HandleProcTriggerSpellWithValueAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit triggerCaster = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();
            if (GetSpellInfo().HasAttribute(SpellAttr8.TargetProcsOnCaster) && eventInfo.GetTypeMask().HasFlag(ProcFlags.TakenHitMask))
                triggerTarget = eventInfo.GetActor();

            uint triggerSpellId = GetSpellEffectInfo().TriggerSpell;
            if (triggerSpellId == 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HandleProcTriggerSpellAuraProc: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have triggered spell.");
                return;
            }

            SpellInfo triggeredSpellInfo = Global.SpellMgr.GetSpellInfo(triggerSpellId, GetBase().GetCastDifficulty());
            if (triggeredSpellInfo != null)
            {
                CastSpellExtraArgs args = new(this);
                args.SetTriggeringSpell(eventInfo.GetProcSpell());
                args.AddSpellMod(SpellValueMod.BasePoint0, GetAmount());
                triggerCaster.CastSpell(triggerTarget, triggerSpellId, args);
                Log.outDebug(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Triggering spell {0} with value {1} from aura {2} proc", triggeredSpellInfo.Id, GetAmount(), GetId());
            }
            else
                Log.outError(LogFilter.Spells, "AuraEffect.HandleProcTriggerSpellWithValueAuraProc: Spell {GetId()} has non-existent spell {triggerSpellId} in EffectTriggered[{GetEffIndex()}] and is therefore not triggered.");
        }

        void HandleProcTriggerDamageAuraProc(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            Unit target = aurApp.GetTarget();
            Unit triggerTarget = eventInfo.GetProcTarget();
            if (triggerTarget.IsImmunedToDamage(target, GetSpellInfo(), GetSpellEffectInfo()))
            {
                SendTickImmune(triggerTarget, target);
                return;
            }

            SpellNonMeleeDamage damageInfo = new(target, triggerTarget, GetSpellInfo(), GetBase().GetSpellVisual(), GetSpellInfo().SchoolMask, GetBase().GetCastId());
            int damage = target.SpellDamageBonusDone(triggerTarget, GetSpellInfo(), GetAmount(), DamageEffectType.SpellDirect, GetSpellEffectInfo(), 1, null, this);
            damage = triggerTarget.SpellDamageBonusTaken(target, GetSpellInfo(), damage, DamageEffectType.SpellDirect);
            target.CalculateSpellDamageTaken(damageInfo, damage, GetSpellInfo());
            Unit.DealDamageMods(damageInfo.attacker, damageInfo.target, ref damageInfo.damage, ref damageInfo.absorb);
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

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.AddSpellCategoryCooldownMod(GetMiscValue(), GetAmount());
            else
                target.RemoveSpellCategoryCooldownMod(GetMiscValue(), GetAmount());
        }

        [AuraEffectHandler(AuraType.ShowConfirmationPrompt)]
        [AuraEffectHandler(AuraType.ShowConfirmationPromptWithDifficulty)]
        void HandleShowConfirmationPrompt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player == null)
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
            if (player == null)
                return;

            if (player.GetClass() != Class.Hunter)
                return;

            Pet pet = player.GetPet();
            if (pet == null)
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

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (apply)
                target.SetPlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
            else if (!target.HasAuraType(AuraType.AllowUsingGameobjectsWhileMounted))
                target.RemovePlayerLocalFlag(PlayerLocalFlags.CanUseObjectsMounted);
        }

        [AuraEffectHandler(AuraType.PlayScene)]
        void HandlePlayScene(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player player = aurApp.GetTarget().ToPlayer();
            if (player == null)
                return;

            if (apply)
                player.GetSceneMgr().PlayScene((uint)GetMiscValue());
            else
                player.GetSceneMgr().CancelSceneBySceneId((uint)GetMiscValue());
        }

        [AuraEffectHandler(AuraType.AreaTrigger)]
        void HandleCreateAreaTrigger(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                AreaTriggerId createPropertiesId = new((uint)GetMiscValue(), false);
                AreaTrigger.CreateAreaTrigger(createPropertiesId, target, GetBase().GetDuration(), GetCaster(), target, GetBase().GetSpellVisual(), GetSpellInfo(), null, this);
            }
            else
            {
                Unit caster = GetCaster();
                if (caster != null)
                    caster.RemoveAreaTrigger(this);
            }
        }

        [AuraEffectHandler(AuraType.PvpTalents)]
        void HandleAuraPvpTalents(AuraApplication auraApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = auraApp.GetTarget().ToPlayer();
            if (target != null)
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
            SpellInfo triggerSpellInfo = Global.SpellMgr.GetSpellInfo(GetSpellEffectInfo().TriggerSpell, GetBase().GetCastDifficulty());
            if (triggerSpellInfo == null)
                return;

            // on apply cast summon spell
            if (apply)
            {
                CastSpellExtraArgs args = new(this);
                args.CastDifficulty = triggerSpellInfo.Difficulty;
                target.CastSpell(target, triggerSpellInfo.Id, args);
            }
            // on unapply we need to search for and remove the summoned creature
            else
            {
                List<uint> summonedEntries = new();
                foreach (var spellEffectInfo in triggerSpellInfo.GetEffects())
                {
                    if (spellEffectInfo.IsEffect(SpellEffectName.Summon))
                    {
                        uint summonEntry = (uint)spellEffectInfo.MiscValue;
                        if (summonEntry != 0)
                            summonedEntries.Add(summonEntry);

                    }
                }

                // we don't know if there can be multiple summons for the same effect, so consider only 1 summon for each effect
                // most of the spells have multiple effects with the same summon spell id for multiple spawns, so right now it's safe to assume there's only 1 spawn per effect
                foreach (uint summonEntry in summonedEntries)
                {
                    List<Creature> nearbyEntries = target.GetCreatureListWithEntryInGrid(summonEntry);
                    foreach (var creature in nearbyEntries)
                    {
                        if (creature.GetOwnerGUID() == target.GetGUID())
                        {
                            creature.DespawnOrUnsummon();
                            break;
                        }
                        else
                        {
                            TempSummon tempSummon = creature.ToTempSummon();
                            if (tempSummon != null)
                            {
                                if (tempSummon.GetSummonerGUID() == target.GetGUID())
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
        void HandleSetFFAPvP(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            target.UpdatePvPState(true);
        }

        [AuraEffectHandler(AuraType.ModOverrideZonePvpType)]
        void HandleModOverrideZonePVPType(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
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
        void HandleBattlegroundPlayerPosition(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();
            if (target == null)
                return;

            if (!apply && aurApp.GetRemoveMode() != AuraRemoveMode.Default)
            {
                GameObject gameObjectCaster = target.GetMap().GetGameObject(GetCasterGUID());
                if (gameObjectCaster != null)
                {
                    if (gameObjectCaster.GetGoType() == GameObjectTypes.NewFlag)
                    {
                        gameObjectCaster.HandleCustomTypeCommand(new SetNewFlagState(FlagState.Dropped, target));
                        GameObject droppedFlag = gameObjectCaster.SummonGameObject(gameObjectCaster.GetGoInfo().NewFlag.FlagDrop, target.GetPosition(), Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(target.GetOrientation(), 0.0f, 0.0f)), TimeSpan.FromSeconds(gameObjectCaster.GetGoInfo().NewFlag.ExpireDuration / 1000), GameObjectSummonType.TimedDespawn);
                        if (droppedFlag != null)
                            droppedFlag.SetOwnerGUID(gameObjectCaster.GetGUID());
                    }
                }
            }

            BattlegroundMap battlegroundMap = target.GetMap().ToBattlegroundMap();
            if (battlegroundMap == null)
                return;

            Battleground bg = battlegroundMap.GetBG();
            if (bg == null)
                return;

            if (apply)
            {
                BattlegroundPlayerPosition playerPosition = new();
                playerPosition.Guid = target.GetGUID();
                playerPosition.ArenaSlot = (sbyte)GetMiscValue();
                playerPosition.Pos = target.GetPosition();

                if (GetAuraType() == AuraType.BattleGroundPlayerPositionFactional)
                    playerPosition.IconID = target.GetEffectiveTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconHordeFlag : BattlegroundConst.PlayerPositionIconAllianceFlag;
                else if (GetAuraType() == AuraType.BattleGroundPlayerPosition)
                    playerPosition.IconID = target.GetEffectiveTeam() == Team.Alliance ? BattlegroundConst.PlayerPositionIconAllianceFlag : BattlegroundConst.PlayerPositionIconHordeFlag;
                else
                    Log.outWarn(LogFilter.Spells, $"Unknown aura effect {GetAuraType()} handled by HandleBattlegroundPlayerPosition.");

                bg.AddPlayerPosition(playerPosition);
            }
            else
                bg.RemovePlayerPosition(target.GetGUID());
        }

        [AuraEffectHandler(AuraType.StoreTeleportReturnPoint)]
        void HandleStoreTeleportReturnPoint(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player playerTarget = aurApp.GetTarget().ToPlayer();
            if (playerTarget == null)
                return;

            if (apply)
                playerTarget.AddStoredAuraTeleportLocation(GetSpellInfo().Id);
            else if (!playerTarget.GetSession().IsLogingOut())
                playerTarget.RemoveStoredAuraTeleportLocation(GetSpellInfo().Id);
        }

        [AuraEffectHandler(AuraType.MountRestrictions)]
        void HandleMountRestrictions(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            aurApp.GetTarget().UpdateMountCapability();
        }

        [AuraEffectHandler(AuraType.CosmeticMounted)]
        void HandleCosmeticMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            if (apply)
                aurApp.GetTarget().SetCosmeticMountDisplayId((uint)GetMiscValue());
            else
                aurApp.GetTarget().SetCosmeticMountDisplayId(0); // set cosmetic mount to 0, even if multiple auras are active; tested with zandalari racial + divine steed

            Player playerTarget = aurApp.GetTarget().ToPlayer();
            if (playerTarget == null)
                return;

            playerTarget.SendMovementSetCollisionHeight(playerTarget.GetCollisionHeight(), UpdateCollisionHeightReason.Force);
        }

        [AuraEffectHandler(AuraType.ModRequiredMountCapabilityFlags)]
        void HandleModRequiredMountCapabilityFlags(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player playerTarget = aurApp.GetTarget().ToPlayer();
            if (playerTarget == null)
                return;

            if (apply)
                playerTarget.SetRequiredMountCapabilityFlag((byte)GetMiscValue());
            else
            {
                int mountCapabilityFlags = 0;
                foreach (AuraEffect otherAura in playerTarget.GetAuraEffectsByType(GetAuraType()))
                    mountCapabilityFlags |= otherAura.GetMiscValue();

                playerTarget.ReplaceAllRequiredMountCapabilityFlags((byte)mountCapabilityFlags);
            }
        }

        [AuraEffectHandler(AuraType.SuppressItemPassiveEffectBySpellLabel)]
        void HandleSuppressItemPassiveEffectBySpellLabel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            List<Aura> suppressedAuras = new();
            foreach (var appliedAura in aurApp.GetTarget().GetOwnedAuras())
                if (appliedAura.Value.GetSpellInfo().HasLabel((uint)GetMiscValue()))
                    suppressedAuras.Add(appliedAura.Value);

            // Refresh applications
            foreach (Aura aura in suppressedAuras)
                aura.ApplyForTargets();
        }

        [AuraEffectHandler(AuraType.ForceBeathBar)]
        void HandleForceBreathBar(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player playerTarget = aurApp.GetTarget().ToPlayer();
            if (playerTarget == null)
                return;

            playerTarget.UpdatePositionData();
        }

        [AuraEffectHandler(AuraType.ActAsControlZone)]
        void HandleAuraActAsControlZone(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit auraOwner = aurApp.GetTarget();
            if (!apply)
            {
                auraOwner.RemoveGameObject(GetSpellInfo().Id, true);
                return;
            }

            GameObjectTemplate gameobjectTemplate = Global.ObjectMgr.GetGameObjectTemplate((uint)GetMiscValue());
            if (gameobjectTemplate == null)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HanldeAuraActAsControlZone: Spell {GetId()} [EffectIndex: {GetEffIndex()}] does not have an existing gameobject template.");
                return;
            }

            if (gameobjectTemplate.type != GameObjectTypes.ControlZone)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HanldeAuraActAsControlZone: Spell {GetId()} [EffectIndex: {GetEffIndex()}] has a gameobject template ({gameobjectTemplate.entry}) that is not a control zone.");
                return;
            }

            if (gameobjectTemplate.displayId != 0)
            {
                Log.outWarn(LogFilter.Spells, $"AuraEffect::HanldeAuraActAsControlZone: Spell {GetId()} [EffectIndex: {GetEffIndex()}] has a gameobject template ({gameobjectTemplate.entry}) that has a display id. Only invisible gameobjects are supported.");
                return;
            }

            GameObject controlZone = auraOwner.SummonGameObject(gameobjectTemplate.entry, auraOwner.GetPosition(), Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(aurApp.GetTarget().GetOrientation(), 0.0f, 0.0f)), TimeSpan.FromHours(24), GameObjectSummonType.TimedOrCorpseDespawn);
            if (controlZone != null)
                controlZone.SetSpellId(GetSpellInfo().Id);
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
