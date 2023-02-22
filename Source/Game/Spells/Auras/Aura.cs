﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Dynamic;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static Game.Maps.InstanceScriptDataReader;

namespace Game.Spells
{
    public enum AuraObjectType
    {
        Unit,
        DynObj
    }

    public enum AuraRemoveMode
    {
        None = 0,
        Default = 1,       // scripted remove, remove by stack with aura with different ids and sc aura remove
        Interrupt,
        Cancel,
        EnemySpell,       // dispel and absorb aura destroy
        Expire,            // aura duration has ended
        Death
    }

    [Flags]
    public enum AuraFlags
    {
        None = 0x00,
        NoCaster = 0x01,
        Positive = 0x02,
        Duration = 0x04,
        Scalable = 0x08,
        Negative = 0x10,
        Unk20 = 0x20,
        Unk40 = 0x40,
        Unk80 = 0x80,
        MawPower = 0x100
    }

    public class AuraApplication
    {
        Unit _target;
        Aura _base;
        AuraRemoveMode _removeMode;                  // Store info for know remove aura reason
        byte _slot;                                   // Aura slot on unit
        AuraFlags _flags;                                  // Aura info flag
        uint _effectsToApply;                         // Used only at spell hit to determine which effect should be applied
        bool _needClientUpdate;
        uint _effectMask;

        public Guid Guid { get; } = Guid.NewGuid();

        public AuraApplication(Unit target, Unit caster, Aura aura, uint effMask)
        {
            _target = target;
            _base = aura;
            _removeMode = AuraRemoveMode.None;
            _slot = SpellConst.MaxAuras;
            _flags = AuraFlags.None;
            _effectsToApply = effMask;
            _needClientUpdate = false;

            Cypher.Assert(GetTarget() != null && GetBase() != null);

            // Try find slot for aura
            byte slot = 0;
            // Lookup for auras already applied from spell
            foreach (AuraApplication visibleAura in GetTarget().GetVisibleAuras())
            {
                if (slot < visibleAura.GetSlot())
                    break;

                ++slot;
            }

            // Register Visible Aura
            if (slot < SpellConst.MaxAuras)
            {
                _slot = slot;
                GetTarget().SetVisibleAura(this);
                _needClientUpdate = true;
                Log.outDebug(LogFilter.Spells, "Aura: {0} Effect: {1} put to unit visible auras slot: {2}", GetBase().GetId(), GetEffectMask(), slot);
            }
            else
                Log.outError(LogFilter.Spells, "Aura: {0} Effect: {1} could not find empty unit visible slot", GetBase().GetId(), GetEffectMask());


            _InitFlags(caster, effMask);
        }

        public void _Remove()
        {
            // update for out of range group members
            if (GetSlot() < SpellConst.MaxAuras)
            {
                GetTarget().RemoveVisibleAura(this);
                ClientUpdate(true);
            }
        }

        void _InitFlags(Unit caster, uint effMask)
        {
            // mark as selfcasted if needed
            _flags |= (GetBase().GetCasterGUID() == GetTarget().GetGUID()) ? AuraFlags.NoCaster : AuraFlags.None;

            // aura is casted by self or an enemy
            // one negative effect and we know aura is negative
            if (IsSelfcasted() || caster == null || !caster.IsFriendlyTo(GetTarget()))
            {
                bool negativeFound = false;
                foreach (var spellEffectInfo in GetBase().GetSpellInfo().GetEffects())
                {
                    if (((1 << spellEffectInfo.EffectIndex) & effMask) != 0 && !GetBase().GetSpellInfo().IsPositiveEffect(spellEffectInfo.EffectIndex))
                    {
                        negativeFound = true;
                        break;
                    }
                }
                _flags |= negativeFound ? AuraFlags.Negative : AuraFlags.Positive;
            }
            // aura is casted by friend
            // one positive effect and we know aura is positive
            else
            {
                bool positiveFound = false;
                foreach (var spellEffectInfo in GetBase().GetSpellInfo().GetEffects())
                {
                    if (((1 << spellEffectInfo.EffectIndex) & effMask) != 0 && GetBase().GetSpellInfo().IsPositiveEffect(spellEffectInfo.EffectIndex))
                    {
                        positiveFound = true;
                        break;
                    }
                }
                _flags |= positiveFound ? AuraFlags.Positive : AuraFlags.Negative;
            }

            bool effectNeedsAmount(KeyValuePair<int, AuraEffect> effect) => (GetEffectsToApply() & (1 << effect.Value.GetEffIndex())) != 0 && Aura.EffectTypeNeedsSendingAmount(effect.Value.GetAuraType());

            if (GetBase().GetSpellInfo().HasAttribute(SpellAttr8.AuraSendAmount) || GetBase().GetAuraEffects().Any(effectNeedsAmount))
                _flags |= AuraFlags.Scalable;
        }

        public void _HandleEffect(int effIndex, bool apply)
        {
            AuraEffect aurEff = GetBase().GetEffect(effIndex);
            if (aurEff == null)
            {
                Log.outError(LogFilter.Spells, "Aura {0} has no effect at effectIndex {1} but _HandleEffect was called", GetBase().GetSpellInfo().Id, effIndex);
                return;
            }
            Cypher.Assert(aurEff != null);
            Cypher.Assert(HasEffect(effIndex) == (!apply));
            Cypher.Assert(Convert.ToBoolean((1 << effIndex) & _effectsToApply));
            Log.outDebug(LogFilter.Spells, "AuraApplication._HandleEffect: {0}, apply: {1}: amount: {2}", aurEff.GetAuraType(), apply, aurEff.GetAmount());

            if (apply)
            {
                Cypher.Assert(!Convert.ToBoolean(_effectMask & (1 << effIndex)));
                _effectMask |= (uint)(1 << effIndex);
                aurEff.HandleEffect(this, AuraEffectHandleModes.Real, true);
                EffectIndexs.Add(effIndex);
            }
            else
            {
                Cypher.Assert(Convert.ToBoolean(_effectMask & (1 << effIndex)));
                _effectMask &= ~(uint)(1 << effIndex);
                aurEff.HandleEffect(this, AuraEffectHandleModes.Real, false);
                EffectIndexs.Remove(effIndex);
            }

            SetNeedClientUpdate();
        }

        public void UpdateApplyEffectMask(uint newEffMask, bool canHandleNewEffects)
        {
            if (_effectsToApply == newEffMask)
                return;

            uint removeEffMask = (_effectsToApply ^ newEffMask) & (~newEffMask);
            uint addEffMask = (_effectsToApply ^ newEffMask) & (~_effectsToApply);

            // quick check, removes application completely
            if (removeEffMask == _effectsToApply && addEffMask == 0)
            {
                _target._UnapplyAura(this, AuraRemoveMode.Default);
                return;
            }

            // update real effects only if they were applied already

            foreach (var eff in GetBase().GetAuraEffects())
            {
                if (HasEffect(eff.Key) && (removeEffMask & (1 << eff.Key)) != 0)
                    _HandleEffect(eff.Key, false);

                if (canHandleNewEffects)
                    if ((addEffMask & (1 << eff.Key)) != 0)
                        _HandleEffect(eff.Key, true);
            }

            _effectsToApply = newEffMask;
        }

        public void SetNeedClientUpdate()
        {
            if (_needClientUpdate || GetRemoveMode() != AuraRemoveMode.None)
                return;

            _needClientUpdate = true;
            _target.SetVisibleAuraUpdate(this);
        }

        public void BuildUpdatePacket(ref AuraInfo auraInfo, bool remove)
        {
            Cypher.Assert(_target.HasVisibleAura(this) != remove);

            auraInfo.Slot = GetSlot();
            if (remove)
                return;

            auraInfo.AuraData = new();

            Aura aura = GetBase();

            AuraDataInfo auraData = auraInfo.AuraData;
            auraData.CastID = aura.GetCastId();
            auraData.SpellID = (int)aura.GetId();
            auraData.Visual = aura.GetSpellVisual();
            auraData.Flags = GetFlags();
            if (aura.GetAuraType() != AuraObjectType.DynObj && aura.GetMaxDuration() > 0 && !aura.GetSpellInfo().HasAttribute(SpellAttr5.DoNotDisplayDuration))
                auraData.Flags |= AuraFlags.Duration;

            auraData.ActiveFlags = GetEffectMask();
            if (!aura.GetSpellInfo().HasAttribute(SpellAttr11.ScalesWithItemLevel))
                auraData.CastLevel = aura.GetCasterLevel();
            else
                auraData.CastLevel = (ushort)aura.GetCastItemLevel();

            // send stack amount for aura which could be stacked (never 0 - causes incorrect display) or charges
            // stack amount has priority over charges (checked on retail with spell 50262)
            auraData.Applications = aura.IsUsingStacks() ? aura.GetStackAmount() : aura.GetCharges();
            if (!aura.GetCasterGUID().IsUnit())
                auraData.CastUnit = ObjectGuid.Empty; // optional data is filled in, but cast unit contains empty guid in packet
            else if (!auraData.Flags.HasFlag(AuraFlags.NoCaster))
                auraData.CastUnit = aura.GetCasterGUID();

            if (auraData.Flags.HasFlag(AuraFlags.Duration))
            {
                auraData.Duration = aura.GetMaxDuration();
                auraData.Remaining = aura.GetDuration();
            }

            if (!auraData.Flags.HasFlag(AuraFlags.Scalable))
                return;

            bool hasEstimatedAmounts = false;
            foreach (var effect in GetBase().GetAuraEffects())
            {
                if (!HasEffect(effect.Value.GetEffIndex()))
                    continue;

                auraData.Points.Add(effect.Value.GetAmount());
                if (effect.Value.GetEstimatedAmount().HasValue)
                    hasEstimatedAmounts = true;
            }

            if (hasEstimatedAmounts)
                foreach (var effect in GetBase().GetAuraEffects())
                {
                    if (HasEffect(effect.Value.GetEffIndex()))
                        auraData.EstimatedPoints.Add(effect.Value.GetEstimatedAmount().GetValueOrDefault(effect.Value.GetAmount()));
                }
        }

        public void ClientUpdate(bool remove = false)
        {
            _needClientUpdate = false;

            AuraUpdate update = new();
            update.UpdateAll = false;
            update.UnitGUID = GetTarget().GetGUID();

            AuraInfo auraInfo = new();
            BuildUpdatePacket(ref auraInfo, remove);
            update.Auras.Add(auraInfo);

            _target.SendMessageToSet(update, true);
        }

        public string GetDebugInfo()
        {
            return $"Base: {(GetBase() != null ? GetBase().GetDebugInfo() : "NULL")}\nTarget: {(GetTarget() != null ? GetTarget().GetDebugInfo() : "NULL")}";
        }
        
        public Unit GetTarget() { return _target; }
        public Aura GetBase() { return _base; }

        public byte GetSlot() { return _slot; }
        public AuraFlags GetFlags() { return _flags; }
        public uint GetEffectMask() { return _effectMask; }
        public HashSet<int> EffectIndexs { get; } = new HashSet<int>();
        public bool HasEffect(int effect)
        {
            return Convert.ToBoolean(_effectMask & (1 << effect));
        }
        public bool IsPositive() { return _flags.HasAnyFlag(AuraFlags.Positive); }
        bool IsSelfcasted() { return _flags.HasAnyFlag(AuraFlags.NoCaster); }
        public uint GetEffectsToApply() { return _effectsToApply; }

        public void SetRemoveMode(AuraRemoveMode mode) { _removeMode = mode; }
        public AuraRemoveMode GetRemoveMode() { return _removeMode; }
        public bool HasRemoveMode() { return _removeMode != 0; }

        public bool IsNeedClientUpdate() { return _needClientUpdate; }
    }

    public class Aura
    {
        const int UPDATE_TARGET_MAP_INTERVAL = 500;
        public Guid Guid { get; } = Guid.NewGuid();

        public Aura(AuraCreateInfo createInfo)
        {
            m_spellInfo = createInfo._spellInfo;
            m_castDifficulty = createInfo._castDifficulty;
            m_castId = createInfo._castId;
            m_casterGuid = createInfo.CasterGUID;
            m_castItemGuid = createInfo.CastItemGUID;
            m_castItemId = createInfo.CastItemId;
            m_castItemLevel = createInfo.CastItemLevel;
            m_spellVisual = new SpellCastVisual(createInfo.Caster ? createInfo.Caster.GetCastSpellXSpellVisualId(createInfo._spellInfo) : createInfo._spellInfo.GetSpellXSpellVisualId(), 0);
            m_applyTime = GameTime.GetGameTime();
            m_owner = createInfo._owner;
            m_timeCla = 0;
            m_updateTargetMapInterval = 0;
            m_casterLevel = createInfo.Caster ? createInfo.Caster.GetLevel() : m_spellInfo.SpellLevel;
            m_procCharges = 0;
            m_stackAmount = 1;
            m_isRemoved = false;
            m_isSingleTarget = false;
            m_isUsingCharges = false;
            m_lastProcAttemptTime = (DateTime.Now - TimeSpan.FromSeconds(10));
            m_lastProcSuccessTime = (DateTime.Now - TimeSpan.FromSeconds(120));

            foreach (SpellPowerRecord power in m_spellInfo.PowerCosts)
                if (power != null && (power.ManaPerSecond != 0 || power.PowerPctPerSecond > 0.0f))
                    m_periodicCosts.Add(power);

            if (!m_periodicCosts.Empty())
                m_timeCla = 1 * Time.InMilliseconds;

            m_maxDuration = CalcMaxDuration(createInfo.Caster);
            m_duration = m_maxDuration;
            m_procCharges = CalcMaxCharges(createInfo.Caster);
            m_isUsingCharges = m_procCharges != 0;
            // m_casterLevel = cast item level/caster level, caster level should be saved to db, confirmed with sniffs
        }

        public T GetScript<T>() where T : AuraScript
        {
            return (T)GetScriptByType(typeof(T));
        }

        public AuraScript GetScriptByType(Type type)
        {
            foreach (var auraScript in m_loadedScripts)
                if (auraScript.GetType() == type)
                    return auraScript;

            return null;
        }

        public void _InitEffects(uint effMask, Unit caster, Dictionary<int, double> baseAmount)
        {
            // shouldn't be in constructor - functions in AuraEffect.AuraEffect use polymorphism
            _effects = new();

            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if ((effMask & (1 << spellEffectInfo.EffectIndex)) != 0)
                    _effects[spellEffectInfo.EffectIndex] = new AuraEffect(this, spellEffectInfo, baseAmount != null ? baseAmount[spellEffectInfo.EffectIndex] : null, caster);
            }
        }

        public virtual void Dispose()
        {
            // unload scripts
            foreach (var itr in m_loadedScripts.ToList())
                itr._Unload();

            Cypher.Assert(m_applications.Empty());
            _DeleteRemovedApplications();
        }

        public Unit GetCaster()
        {
            if (m_owner.GetGUID() == m_casterGuid)
                return GetUnitOwner();

            return Global.ObjAccessor.GetUnit(m_owner, m_casterGuid);
        }

        WorldObject GetWorldObjectCaster()
        {
            if (GetCasterGUID().IsUnit())
                return GetCaster();

            return Global.ObjAccessor.GetWorldObject(GetOwner(), GetCasterGUID());
        }

        public AuraObjectType GetAuraType()
        {
            return (m_owner.GetTypeId() == TypeId.DynamicObject) ? AuraObjectType.DynObj : AuraObjectType.Unit;
        }

        public virtual void _ApplyForTarget(Unit target, Unit caster, AuraApplication auraApp)
        {
            Cypher.Assert(target != null);
            Cypher.Assert(auraApp != null);
            // aura mustn't be already applied on target
            //Cypher.Assert(!IsAppliedOnTarget(target.GetGUID()) && "Aura._ApplyForTarget: aura musn't be already applied on target");

            m_applications[target.GetGUID()] = auraApp;

            // set infinity cooldown state for spells
            if (caster != null && caster.IsTypeId(TypeId.Player))
            {
                if (m_spellInfo.IsCooldownStartedOnEvent())
                {
                    Item castItem = !m_castItemGuid.IsEmpty() ? caster.ToPlayer().GetItemByGuid(m_castItemGuid) : null;
                    caster.GetSpellHistory().StartCooldown(m_spellInfo, castItem != null ? castItem.GetEntry() : 0, null, true);
                }
            }
        }
        public virtual void _UnapplyForTarget(Unit target, Unit caster, AuraApplication auraApp)
        {
            Cypher.Assert(target != null);
            Cypher.Assert(auraApp.HasRemoveMode());
            Cypher.Assert(auraApp != null);

            var app = m_applications.LookupByKey(target.GetGUID());

            // @todo Figure out why this happens
            if (app == null)
            {
                Log.outError(LogFilter.Spells, "Aura._UnapplyForTarget, target: {0}, caster: {1}, spell: {2} was not found in owners application map!",
                target.GetGUID().ToString(), caster ? caster.GetGUID().ToString() : "", auraApp.GetBase().GetSpellInfo().Id);
                Cypher.Assert(false);
            }

            // aura has to be already applied
            Cypher.Assert(app == auraApp);
            m_applications.Remove(target.GetGUID());

            _removedApplications.Add(auraApp);

            // reset cooldown state for spells
            if (caster != null && GetSpellInfo().IsCooldownStartedOnEvent())
                // note: item based cooldowns and cooldown spell mods with charges ignored (unknown existed cases)
                caster.GetSpellHistory().SendCooldownEvent(GetSpellInfo());
        }

        // removes aura from all targets
        // and marks aura as removed
        public void _Remove(AuraRemoveMode removeMode)
        {
            Cypher.Assert(!m_isRemoved);
            m_isRemoved = true;
            foreach (var pair in m_applications.ToList())
            {
                AuraApplication aurApp = pair.Value;
                Unit target = aurApp.GetTarget();
                target._UnapplyAura(aurApp, removeMode);
            }

            if (m_dropEvent != null)
            {
                m_dropEvent.ScheduleAbort();
                m_dropEvent = null;
            }
        }

        public void UpdateTargetMap(Unit caster, bool apply = true)
        {
            if (IsRemoved())
                return;

            m_updateTargetMapInterval = UPDATE_TARGET_MAP_INTERVAL;

            // fill up to date target list
            //       target, effMask
            Dictionary<Unit, uint> targets = new();

            FillTargetMap(ref targets, caster);

            List<Unit> targetsToRemove = new();

            // mark all auras as ready to remove
            foreach (var app in m_applications)
            {
                // not found in current area - remove the aura
                if (!targets.TryGetValue(app.Value.GetTarget(), out uint existing))
                    targetsToRemove.Add(app.Value.GetTarget());
                else
                {
                    // needs readding - remove now, will be applied in next update cycle
                    // (dbcs do not have auras which apply on same type of targets but have different radius, so this is not really needed)
                    if (app.Value.GetTarget().IsImmunedToSpell(GetSpellInfo(), caster, true) || !CanBeAppliedOn(app.Value.GetTarget()))
                    {
                        targetsToRemove.Add(app.Value.GetTarget());
                        continue;
                    }

                    // check target immunities (for existing targets)
                    foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
                        if (app.Value.GetTarget().IsImmunedToSpellEffect(GetSpellInfo(), spellEffectInfo, caster, true))
                            existing &= ~(uint)(1 << spellEffectInfo.EffectIndex);

                    targets[app.Value.GetTarget()] = existing;

                    // needs to add/remove effects from application, don't remove from map so it gets updated
                    if (app.Value.GetEffectMask() != existing)
                        continue;

                    // nothing to do - aura already applied
                    // remove from auras to register list
                    targets.Remove(app.Value.GetTarget());
                }
            }
            // register auras for units
            foreach (var unit in targets.Keys.ToList())
            {
                bool addUnit = true;
                // check target immunities
                AuraApplication aurApp = GetApplicationOfTarget(unit.GetGUID());
                if (aurApp == null)
                {
                    // check target immunities (for new targets)
                    foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
                        if (unit.IsImmunedToSpellEffect(GetSpellInfo(), spellEffectInfo, caster))
                            targets[unit] &= ~(uint)(1 << spellEffectInfo.EffectIndex);

                    if (targets[unit] == 0 || unit.IsImmunedToSpell(GetSpellInfo(), caster) || !CanBeAppliedOn(unit))
                        addUnit = false;
                }

                if (addUnit && !unit.IsHighestExclusiveAura(this, true))
                    addUnit = false;

                // Dynobj auras don't hit flying targets
                if (GetAuraType() == AuraObjectType.DynObj && unit.IsInFlight())
                    addUnit = false;

                // Do not apply aura if it cannot stack with existing auras
                if (addUnit)
                {
                    // Allow to remove by stack when aura is going to be applied on owner
                    if (unit != GetOwner())
                    {
                        // check if not stacking aura already on target
                        // this one prevents unwanted usefull buff loss because of stacking and prevents overriding auras periodicaly by 2 near area aura owners
                        foreach (var iter in unit.GetAppliedAuras())
                        {
                            Aura aura = iter.GetBase();
                            if (!CanStackWith(aura))
                            {
                                addUnit = false;
                                break;
                            }
                        }
                    }
                    
                }

                if (!addUnit)
                    targets.Remove(unit);
                else
                {
                    // owner has to be in world, or effect has to be applied to self
                    if (!m_owner.IsSelfOrInSameMap(unit))
                    {
                        // @todo There is a crash caused by shadowfiend load addon
                        Log.outFatal(LogFilter.Spells, "Aura {0}: Owner {1} (map {2}) is not in the same map as target {3} (map {4}).", GetSpellInfo().Id,
                            m_owner.GetName(), m_owner.IsInWorld ? (int)m_owner.GetMap().GetId() : -1,
                            unit.GetName(), unit.IsInWorld ? (int)unit.GetMap().GetId() : -1);
                    }

                    if (aurApp != null)
                    {
                        aurApp.UpdateApplyEffectMask(targets[unit], true); // aura is already applied, this means we need to update effects of current application
                        targets.Remove(unit);
                    }
                    else
                        unit._CreateAuraApplication(this, targets[unit]);
                }
            }

            // remove auras from units no longer needing them
            foreach (var unit in targetsToRemove)
            {
                AuraApplication aurApp = GetApplicationOfTarget(unit.GetGUID());
                if (aurApp != null)
                    unit._UnapplyAura(aurApp, AuraRemoveMode.Default);
            }

            if (!apply)
                return;

            // apply aura effects for units
            foreach (var pair in targets)
            {
                AuraApplication aurApp = GetApplicationOfTarget(pair.Key.GetGUID());
                if (aurApp != null)
                {
                    // owner has to be in world, or effect has to be applied to self
                    Cypher.Assert((!m_owner.IsInWorld && m_owner == pair.Key) || m_owner.IsInMap(pair.Key));
                    pair.Key._ApplyAura(aurApp, pair.Value);
                }
            }
        }

        // targets have to be registered and not have effect applied yet to use this function
        public void _ApplyEffectForTargets(int effIndex)
        {
            // prepare list of aura targets
            List<Unit> targetList = new();
            foreach (var app in m_applications.Values)
            {
                if (Convert.ToBoolean(app.GetEffectsToApply() & (1 << effIndex)) && !app.HasEffect(effIndex))
                    targetList.Add(app.GetTarget());
            }

            // apply effect to targets
            foreach (var unit in targetList)
            {
                if (GetApplicationOfTarget(unit.GetGUID()) != null)
                {
                    // owner has to be in world, or effect has to be applied to self
                    Cypher.Assert((!GetOwner().IsInWorld && GetOwner() == unit) || GetOwner().IsInMap(unit));
                    unit._ApplyAuraEffect(this, effIndex);
                }
            }
        }

        public void UpdateOwner(uint diff, WorldObject owner)
        {
            Cypher.Assert(owner == m_owner);

            Unit caster = GetCaster();
            // Apply spellmods for channeled auras
            // used for example when triggered spell of spell:10 is modded
            Spell modSpell = null;
            Player modOwner = null;
            if (caster != null)
            {
                modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                {
                    modSpell = modOwner.FindCurrentSpellBySpellId(GetId());
                    if (modSpell != null)
                        modOwner.SetSpellModTakingSpell(modSpell, true);
                }
            }

            Update(diff, caster);

            if (m_updateTargetMapInterval <= diff)
                UpdateTargetMap(caster);
            else
                m_updateTargetMapInterval -= (int)diff;

            // update aura effects
            foreach (var effect in GetAuraEffects())
                effect.Value.Update(diff, caster);

            // remove spellmods after effects update
            if (modSpell != null)
                modOwner.SetSpellModTakingSpell(modSpell, false);

            _DeleteRemovedApplications();
        }

        void Update(uint diff, Unit caster)
        {
            ForEachAuraScript<IAuraOnUpdate>(u => u.AuraOnUpdate(diff));

            if (m_duration > 0)
            {
                m_duration -= (int)diff;
                if (m_duration < 0)
                    m_duration = 0;

                // handle manaPerSecond/manaPerSecondPerLevel
                if (m_timeCla != 0)
                {
                    if (m_timeCla > diff)
                        m_timeCla -= (int)diff;
                    else if (caster != null && (caster == GetOwner() || !GetSpellInfo().HasAttribute(SpellAttr2.NoTargetPerSecondCosts)))
                    {
                        if (!m_periodicCosts.Empty())
                        {
                            m_timeCla += (int)(1000 - diff);

                            foreach (SpellPowerRecord power in m_periodicCosts)
                            {
                                if (power.RequiredAuraSpellID != 0 && !caster.HasAura(power.RequiredAuraSpellID))
                                    continue;

                                int manaPerSecond = (int)power.ManaPerSecond;
                                if (power.PowerType != PowerType.Health)
                                    manaPerSecond += MathFunctions.CalculatePct(caster.GetMaxPower(power.PowerType), power.PowerPctPerSecond);
                                else
                                    manaPerSecond += (int)MathFunctions.CalculatePct(caster.GetMaxHealth(), power.PowerPctPerSecond);

                                if (manaPerSecond != 0)
                                {
                                    if (power.PowerType == PowerType.Health)
                                    {
                                        if ((int)caster.GetHealth() > manaPerSecond)
                                            caster.ModifyHealth(-manaPerSecond);
                                        else
                                            Remove();
                                    }
                                    else if (caster.GetPower(power.PowerType) >= manaPerSecond)
                                        caster.ModifyPower(power.PowerType, -manaPerSecond);
                                    else
                                        Remove();
                                }
                            }
                        }
                    }
                }
            }
        }

        public int CalcMaxDuration(Unit caster)
        {
            return CalcMaxDuration(GetSpellInfo(), caster);
        }

        public static int CalcMaxDuration(SpellInfo spellInfo, WorldObject caster)
        {
            Player modOwner = null;
            int maxDuration;

            if (caster != null)
            {
                modOwner = caster.GetSpellModOwner();
                maxDuration = caster.CalcSpellDuration(spellInfo);
            }
            else
                maxDuration = spellInfo.GetDuration();

            if (spellInfo.IsPassive() && spellInfo.DurationEntry == null)
                maxDuration = -1;

            // IsPermanent() checks max duration (which we are supposed to calculate here)
            if (maxDuration != -1 && modOwner != null)
                modOwner.ApplySpellMod(spellInfo, SpellModOp.Duration, ref maxDuration);

            return maxDuration;
        }

        public void SetDuration(double duration, bool withMods = false)
        {
            SetDuration((int)duration, withMods);
        }

        public void SetDuration(int duration, bool withMods = false)
        {
            if (withMods)
            {
                Unit caster = GetCaster();
                if (caster)
                {
                    Player modOwner = caster.GetSpellModOwner();
                    if (modOwner)
                        modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.Duration, ref duration);
                }
            }

            m_duration = duration;
            SetNeedClientUpdateForTargets();
        }

        /// <summary>
        ///     Adds the given duration to the auras duration.
        /// </summary>
        public void ModDuration(int duration, bool withMods = false)
        {
            SetDuration(GetDuration() + duration, withMods);
        }

        public void ModDuration(double duration, bool withMods = false)
        {
            SetDuration((int)duration, withMods);
        }

        public void RefreshDuration(bool withMods = false)
        {
            Unit caster = GetCaster();
            if (withMods && caster)
            {
                int duration = m_spellInfo.GetMaxDuration();
                // Calculate duration of periodics affected by haste.
                if (m_spellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
                    duration = (int)(duration * caster.m_unitData.ModCastingSpeed);

                SetMaxDuration(duration);
                SetDuration(duration);
            }
            else
                SetDuration(GetMaxDuration());

            if (!m_periodicCosts.Empty())
                m_timeCla = 1 * Time.InMilliseconds;

            // also reset periodic counters
            foreach (var aurEff in GetAuraEffects())
                aurEff.Value.ResetTicks();
        }

        void RefreshTimers(bool resetPeriodicTimer)
        {
            m_maxDuration = CalcMaxDuration();
            if (m_spellInfo.HasAttribute(SpellAttr8.DontResetPeriodicTimer))
            {
                int minPeriod = m_maxDuration;

                foreach (var aurEff in GetAuraEffects())
                {
                    int period = aurEff.Value.GetPeriod();
                    if (period != 0)
                        minPeriod = Math.Min(period, minPeriod);
                }

                // If only one tick remaining, roll it over into new duration
                if (GetDuration() <= minPeriod)
                {
                    m_maxDuration += GetDuration();
                    resetPeriodicTimer = false;
                }
            }

            RefreshDuration();
            Unit caster = GetCaster();
            foreach (var aurEff in GetAuraEffects())
                aurEff.Value.CalculatePeriodic(caster, resetPeriodicTimer, false);
        }

        public void SetCharges(int charges)
        {
            if (m_procCharges == charges)
                return;
            m_procCharges = (byte)charges;
            m_isUsingCharges = m_procCharges != 0;
            SetNeedClientUpdateForTargets();
        }

        public bool ModCharges(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (IsUsingCharges())
            {
                int charges = m_procCharges + num;
                int maxCharges = CalcMaxCharges();

                // limit charges (only on charges increase, charges may be changed manually)
                if ((num > 0) && (charges > maxCharges))
                    charges = maxCharges;
                // we're out of charges, remove
                else if (charges <= 0)
                {
                    Remove(removeMode);
                    return true;
                }

                SetCharges((byte)charges);
            }
            return false;
        }

        public void ModChargesDelayed(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            m_dropEvent = null;
            ModCharges(num, removeMode);
        }

        public void DropChargeDelayed(uint delay, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            // aura is already during delayed charge drop
            if (m_dropEvent != null)
                return;

            // only units have events
            Unit owner = m_owner.ToUnit();
            if (!owner)
                return;

            m_dropEvent = new ChargeDropEvent(this, removeMode);
            owner.m_Events.AddEvent(m_dropEvent, owner.m_Events.CalculateTime(TimeSpan.FromMilliseconds(delay)));
        }

        public void SetStackAmount(byte stackAmount)
        {
            m_stackAmount = stackAmount;
            Unit caster = GetCaster();

            List<AuraApplication> applications = GetApplicationList();
            foreach (var aurApp in applications)
                if (!aurApp.HasRemoveMode())
                    HandleAuraSpecificMods(aurApp, caster, false, true);

            foreach (var aurEff in GetAuraEffects())
                aurEff.Value.ChangeAmount(aurEff.Value.CalculateAmount(caster), false, true);

            foreach (var aurApp in applications)
                if (!aurApp.HasRemoveMode())
                    HandleAuraSpecificMods(aurApp, caster, true, true);

            SetNeedClientUpdateForTargets();
        }

        public bool IsUsingStacks()
        {
            return m_spellInfo.StackAmount > 0;
        }

        public uint CalcMaxStackAmount()
        {
            uint maxStackAmount = m_spellInfo.StackAmount;
            Unit caster = GetCaster();
            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(m_spellInfo, SpellModOp.MaxAuraStacks, ref maxStackAmount);
            }
            return maxStackAmount;
        }

        public bool ModStackAmount(double num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
        {
            return ModStackAmount((int)num, removeMode, resetPeriodicTimer);
        }

        public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
        {
            int stackAmount = m_stackAmount + num;
            uint maxStackAmount = CalcMaxStackAmount();

            // limit the stack amount (only on stack increase, stack amount may be changed manually)
            if ((num > 0) && (stackAmount > maxStackAmount))
            {
                // not stackable aura - set stack amount to 1
                if (m_spellInfo.StackAmount == 0)
                    stackAmount = 1;
                else
                    stackAmount = (int)m_spellInfo.StackAmount;
            }
            // we're out of stacks, remove
            else if (stackAmount <= 0)
            {
                Remove(removeMode);
                return true;
            }

            bool refresh = stackAmount >= GetStackAmount() && (m_spellInfo.StackAmount != 0 || (!m_spellInfo.HasAttribute(SpellAttr1.AuraUnique) && !m_spellInfo.HasAttribute(SpellAttr5.AuraUniquePerCaster)));

            // Update stack amount
            SetStackAmount((byte)stackAmount);

            if (refresh)
            {
                RefreshTimers(resetPeriodicTimer);

                // reset charges
                SetCharges(CalcMaxCharges());
            }
            SetNeedClientUpdateForTargets();
            return false;
        }

        public bool HasMoreThanOneEffectForType(AuraType auraType)
        {
            uint count = 0;
            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if (HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.ApplyAuraName == auraType)
                    ++count;
            }

            return count > 1;
        }

        public bool IsArea()
        {
            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if (HasEffect(spellEffectInfo.EffectIndex) && spellEffectInfo.IsAreaAuraEffect())
                    return true;
            }
            return false;
        }

        public bool IsPassive()
        {
            return m_spellInfo.IsPassive();
        }

        public bool IsDeathPersistent()
        {
            return GetSpellInfo().IsDeathPersistent();
        }

        public bool IsRemovedOnShapeLost(Unit target)
        {
            return GetCasterGUID() == target.GetGUID()
                && m_spellInfo.Stances != 0
                && !m_spellInfo.HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm)
                && !m_spellInfo.HasAttribute(SpellAttr0.NotShapeshifted);
        }
        
        public bool CanBeSaved()
        {
            if (IsPassive())
                return false;

            if (GetSpellInfo().IsChanneled())
                return false;

            // Check if aura is single target, not only spell info
            if (GetCasterGUID() != GetOwner().GetGUID())
            {
                // owner == caster for area auras, check for possible bad data in DB
                foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
                {
                    if (!spellEffectInfo.IsEffect())
                        continue;

                    if (spellEffectInfo.IsTargetingArea() || spellEffectInfo.IsAreaAuraEffect())
                        return false;
                }

                if (IsSingleTarget() || GetSpellInfo().IsSingleTarget())
                    return false;
            }

            if (GetSpellInfo().HasAttribute(SpellCustomAttributes.AuraCannotBeSaved))
                return false;

            // don't save auras removed by proc system
            if (IsUsingCharges() && GetCharges() == 0)
                return false;

            // don't save permanent auras triggered by items, they'll be recasted on login if necessary
            if (!GetCastItemGUID().IsEmpty() && IsPermanent())
                return false;

            return true;
        }

        public bool IsSingleTargetWith(Aura aura)
        {
            // Same spell?
            if (GetSpellInfo().IsRankOf(aura.GetSpellInfo()))
                return true;

            SpellSpecificType spec = GetSpellInfo().GetSpellSpecific();
            // spell with single target specific types
            switch (spec)
            {
                case SpellSpecificType.MagePolymorph:
                    if (aura.GetSpellInfo().GetSpellSpecific() == spec)
                        return true;
                    break;
                default:
                    break;
            }

            return false;
        }

        public void UnregisterSingleTarget()
        {
            Cypher.Assert(m_isSingleTarget);
            Unit caster = GetCaster();
            Cypher.Assert(caster != null);
            caster.GetSingleCastAuras().Remove(this);
            SetIsSingleTarget(false);
        }

        public int CalcDispelChance(Unit auraTarget, bool offensive)
        {
            // we assume that aura dispel chance is 100% on start
            // need formula for level difference based chance
            int resistChance = 0;

            // Apply dispel mod from aura caster
            Unit caster = GetCaster();
            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.DispelResistance, ref resistChance);
            }

            resistChance = resistChance < 0 ? 0 : resistChance;
            resistChance = resistChance > 100 ? 100 : resistChance;
            return 100 - resistChance;
        }

        public AuraKey GenerateKey(out uint recalculateMask)
        {
            AuraKey key = new(GetCasterGUID(), GetCastItemGUID(), GetId(), 0);
            recalculateMask = 0;
            foreach(var aurEff in _effects)
            {
                key.EffectMask |= 1u << aurEff.Key;
                if (aurEff.Value.CanBeRecalculated())
                    recalculateMask |= 1u << aurEff.Key;
            }

            return key;
        }

        public void SetLoadedState(int maxduration, int duration, int charges, byte stackamount, uint recalculateMask, Dictionary<int, double> amount)
        {
            m_maxDuration = maxduration;
            m_duration = duration;
            m_procCharges = (byte)charges;
            m_isUsingCharges = m_procCharges != 0;
            m_stackAmount = stackamount;
            Unit caster = GetCaster();
            foreach (var effect in GetAuraEffects())
            {
                effect.Value.SetAmount(amount[effect.Value.GetEffIndex()]);
                effect.Value.SetCanBeRecalculated(Convert.ToBoolean(recalculateMask & (1 << effect.Value.GetEffIndex())));
                effect.Value.CalculatePeriodic(caster, false, true);
                effect.Value.CalculateSpellMod();
                effect.Value.RecalculateAmount(caster);
            }
        }

        public bool HasEffectType(AuraType type)
        {
            foreach (var eff in GetAuraEffects())
                if (eff.Value.GetAuraType() == type)
                    return true;

            return false;
        }

        public static bool EffectTypeNeedsSendingAmount(AuraType type)
        {
            switch (type)
            {
                case AuraType.OverrideActionbarSpells:
                case AuraType.OverrideActionbarSpellsTriggered:
                case AuraType.ModSpellCategoryCooldown:
                case AuraType.ModMaxCharges:
                case AuraType.ChargeRecoveryMod:
                case AuraType.ChargeRecoveryMultiplier:
                    return true;
                default:
                    break;
            }

            return false;
        }

        public void RecalculateAmountOfEffects()
        {
            Cypher.Assert(!IsRemoved());
            Unit caster = GetCaster();
            foreach (var effect in GetAuraEffects())
                if (!IsRemoved())
                    effect.Value.RecalculateAmount(caster);
        }

        public void HandleAllEffects(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            Cypher.Assert(!IsRemoved());
            foreach (var effect in GetAuraEffects())
                if (!IsRemoved())
                    effect.Value.HandleEffect(aurApp, mode, apply);
        }

        public List<AuraApplication> GetApplicationList()
        {
            var applicationList = new List<AuraApplication>();
            foreach (var aurApp in m_applications.Values)
            {
                if (aurApp.GetEffectMask() != 0)
                    applicationList.Add(aurApp);
            }

            return applicationList;
        }

        public void SetNeedClientUpdateForTargets()
        {
            foreach (var app in m_applications.Values)
                app.SetNeedClientUpdate();
        }

        // trigger effects on real aura apply/remove
        public void HandleAuraSpecificMods(AuraApplication aurApp, Unit caster, bool apply, bool onReapply)
        {
            Unit target = aurApp.GetTarget();
            AuraRemoveMode removeMode = aurApp.GetRemoveMode();
            // handle spell_area table
            var saBounds = Global.SpellMgr.GetSpellAreaForAuraMapBounds(GetId());
            if (saBounds != null)
            {
                uint zone, area;
                target.GetZoneAndAreaId(out zone, out area);

                foreach (var spellArea in saBounds)
                {
                    // some auras remove at aura remove
                    if (spellArea.flags.HasAnyFlag(SpellAreaFlag.AutoRemove) && !spellArea.IsFitToRequirements((Player)target, zone, area))
                        target.RemoveAura(spellArea.spellId);
                    // some auras applied at aura apply
                    else if (spellArea.flags.HasAnyFlag(SpellAreaFlag.AutoCast))
                    {
                        if (!target.HasAura(spellArea.spellId))
                            target.CastSpell(target, spellArea.spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(GetCastId()));
                    }
                }
            }

            // handle spell_linked_spell table
            if (!onReapply)
            {
                // apply linked auras
                if (apply)
                {
                    var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, GetId());
                    if (spellTriggered != null)
                    {
                        foreach (var spell in spellTriggered)
                        {
                            if (spell < 0)
                                target.ApplySpellImmune(GetId(), SpellImmunity.Id, (uint)-spell, true);
                            else if (caster != null)
                                caster.AddAura((uint)spell, target);
                        }
                    }
                }
                else
                {
                    // remove linked auras
                    var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Remove, GetId());
                    if (spellTriggered != null)
                    {
                        foreach (var spell in spellTriggered)
                        {
                            if (spell < 0)
                                target.RemoveAura((uint)-spell);
                            else if (removeMode != AuraRemoveMode.Death)
                                target.CastSpell(target, (uint)spell, new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                    .SetOriginalCaster(GetCasterGUID())
                                    .SetOriginalCastId(GetCastId()));
                        }
                    }
                    spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, GetId());
                    if (spellTriggered != null)
                    {
                        foreach (var id in spellTriggered)
                        {
                            if (id < 0)
                                target.ApplySpellImmune(GetId(), SpellImmunity.Id, (uint)-id, false);
                            else
                                target.RemoveAura((uint)id, GetCasterGUID(), removeMode);
                        }
                    }
                }
            }
            else if (apply)
            {
                // modify stack amount of linked auras
                var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Aura, GetId());
                if (spellTriggered != null)
                {
                    foreach (var id in spellTriggered)
                    {
                        if (id > 0)
                        {
                            Aura triggeredAura = target.GetAura((uint)id, GetCasterGUID());
                            if (triggeredAura != null)
                                triggeredAura.ModStackAmount(GetStackAmount() - triggeredAura.GetStackAmount());
                        }
                    }
                }
            }

            // mods at aura apply
            if (apply)
            {
                switch (GetSpellInfo().SpellFamilyName)
                {
                    case SpellFamilyNames.Generic:
                        switch (GetId())
                        {
                            case 33572: // Gronn Lord's Grasp, becomes stoned
                                if (GetStackAmount() >= 5 && !target.HasAura(33652))
                                    target.CastSpell(target, 33652, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(GetCastId()));
                                break;
                            case 50836: //Petrifying Grip, becomes stoned
                                if (GetStackAmount() >= 5 && !target.HasAura(50812))
                                    target.CastSpell(target, 50812, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(GetCastId()));
                                break;
                            case 60970: // Heroic Fury (remove Intercept cooldown)
                                if (target.IsTypeId(TypeId.Player))
                                    target.GetSpellHistory().ResetCooldown(20252, true);
                                break;
                        }
                        break;
                    case SpellFamilyNames.Druid:
                        if (caster == null)
                            break;
                        // Rejuvenation
                        if (GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x10u) && GetEffect(0) != null)
                        {
                            // Druid T8 Restoration 4P Bonus
                            if (caster.HasAura(64760))
                            {
                                CastSpellExtraArgs args = new(GetEffect(0));
                                args.AddSpellMod(SpellValueMod.BasePoint0, GetEffect(0).GetAmount());
                                caster.CastSpell(target, 64801, args);
                            }
                        }
                        break;
                }
            }
            // mods at aura remove
            else
            {
                switch (GetSpellInfo().SpellFamilyName)
                {
                    case SpellFamilyNames.Mage:
                        switch (GetId())
                        {
                            case 66: // Invisibility
                                if (removeMode != AuraRemoveMode.Expire)
                                    break;
                                target.CastSpell(target, 32612, new CastSpellExtraArgs(GetEffect(1)));
                                break;
                            default:
                                break;
                        }
                        break;
                    case SpellFamilyNames.Priest:
                        if (caster == null)
                            break;
                        // Power word: shield
                        if (removeMode == AuraRemoveMode.EnemySpell && GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x00000001u))
                        {
                            // Rapture
                            Aura aura = caster.GetAuraOfRankedSpell(47535);
                            if (aura != null)
                            {
                                // check cooldown
                                if (caster.IsTypeId(TypeId.Player))
                                {
                                    if (caster.GetSpellHistory().HasCooldown(aura.GetSpellInfo()))
                                    {
                                        // This additional check is needed to add a minimal delay before cooldown in in effect
                                        // to allow all bubbles broken by a single damage source proc mana return
                                        if (caster.GetSpellHistory().GetRemainingCooldown(aura.GetSpellInfo()) <= TimeSpan.FromSeconds(11))
                                            break;
                                    }
                                    else    // and add if needed
                                        caster.GetSpellHistory().AddCooldown(aura.GetId(), 0, TimeSpan.FromSeconds(12));
                                }

                                // effect on caster
                                AuraEffect aurEff = aura.GetEffect(0);
                                if (aurEff != null)
                                {
                                    double multiplier = aurEff.GetAmount();
                                    CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                                    args.SetOriginalCastId(GetCastId());
                                    args.AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(caster.GetMaxPower(PowerType.Mana), multiplier));
                                    caster.CastSpell(caster, 47755, args);
                                }
                            }
                        }
                        break;
                    case SpellFamilyNames.Rogue:
                        // Remove Vanish on stealth remove
                        if (GetId() == 1784)
                            target.RemoveAurasWithFamily(SpellFamilyNames.Rogue, new FlagArray128(0x0000800, 0, 0), target.GetGUID());
                        break;
                }
            }

            // mods at aura apply or remove
            switch (GetSpellInfo().SpellFamilyName)
            {
                case SpellFamilyNames.Hunter:
                    switch (GetId())
                    {
                        case 19574: // Bestial Wrath
                            // The Beast Within cast on owner if talent present
                            Unit owner = target.GetOwner();
                            if (owner != null)
                            {
                                // Search talent
                                if (owner.HasAura(34692))
                                {
                                    if (apply)
                                        owner.CastSpell(owner, 34471, new CastSpellExtraArgs(GetEffect(0)));
                                    else
                                        owner.RemoveAura(34471);
                                }
                            }
                            break;
                    }
                    break;
                case SpellFamilyNames.Paladin:
                    switch (GetId())
                    {
                        case 31821:
                            // Aura Mastery Triggered Spell Handler
                            // If apply Concentration Aura . trigger . apply Aura Mastery Immunity
                            // If remove Concentration Aura . trigger . remove Aura Mastery Immunity
                            // If remove Aura Mastery . trigger . remove Aura Mastery Immunity
                            // Do effects only on aura owner
                            if (GetCasterGUID() != target.GetGUID())
                                break;

                            if (apply)
                            {
                                if ((GetSpellInfo().Id == 31821 && target.HasAura(19746, GetCasterGUID())) || (GetSpellInfo().Id == 19746 && target.HasAura(31821)))
                                    target.CastSpell(target, 64364, new CastSpellExtraArgs(true));
                            }
                            else
                                target.RemoveAurasDueToSpell(64364, GetCasterGUID());
                            break;
                        case 31842: // Divine Favor
                            // Item - Paladin T10 Holy 2P Bonus
                            if (target.HasAura(70755))
                            {
                                if (apply)
                                    target.CastSpell(target, 71166, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCastId(GetCastId()));
                                else
                                    target.RemoveAura(71166);
                            }
                            break;
                    }
                    break;
                case SpellFamilyNames.Warlock:
                    // Drain Soul - If the target is at or below 25% health, Drain Soul causes four times the normal damage
                    if (GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x00004000u))
                    {
                        if (caster == null)
                            break;
                        if (apply)
                        {
                            if (target != caster && !target.HealthAbovePct(25))
                                caster.CastSpell(caster, 100001, new CastSpellExtraArgs(true));
                        }
                        else
                        {
                            if (target != caster)
                                caster.RemoveAura(GetId());
                            else
                                caster.RemoveAura(100001);
                        }
                    }
                    break;
            }
        }

        bool CanBeAppliedOn(Unit target)
        {
            foreach (uint label in GetSpellInfo().Labels)
                if (target.HasAuraTypeWithMiscvalue(AuraType.SuppressItemPassiveEffectBySpellLabel, (int)label))
                    return false;

            // unit not in world or during remove from world
            if (!target.IsInWorld || target.IsDuringRemoveFromWorld())
            {
                // area auras mustn't be applied
                if (GetOwner() != target)
                    return false;
                // not selfcasted single target auras mustn't be applied
                if (GetCasterGUID() != GetOwner().GetGUID() && GetSpellInfo().IsSingleTarget())
                    return false;
                return true;
            }
            else
                return CheckAreaTarget(target);
        }

        bool CheckAreaTarget(Unit target)
        {
            return CallScriptCheckAreaTargetHandlers(target);
        }

        public bool CanStackWith(Aura existingAura)
        {
            // Can stack with self
            if (this == existingAura)
                return true;

            bool sameCaster = GetCasterGUID() == existingAura.GetCasterGUID();
            SpellInfo existingSpellInfo = existingAura.GetSpellInfo();

            // Dynobj auras do not stack when they come from the same spell cast by the same caster
            if (GetAuraType() == AuraObjectType.DynObj || existingAura.GetAuraType() == AuraObjectType.DynObj)
            {
                if (sameCaster && m_spellInfo.Id == existingSpellInfo.Id)
                    return false;
                return true;
            }

            // passive auras don't stack with another rank of the spell cast by same caster
            if (IsPassive() && sameCaster && (m_spellInfo.IsDifferentRankOf(existingSpellInfo) || (m_spellInfo.Id == existingSpellInfo.Id && m_castItemGuid.IsEmpty())))
                return false;

            foreach (var spellEffectInfo in existingSpellInfo.GetEffects())
            {
                // prevent remove triggering aura by triggered aura
                if (spellEffectInfo.TriggerSpell == GetId())
                    return true;
            }

            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                // prevent remove triggered aura by triggering aura refresh
                if (spellEffectInfo.TriggerSpell == existingAura.GetId())
                    return true;
            }

            // check spell specific stack rules
            if (m_spellInfo.IsAuraExclusiveBySpecificWith(existingSpellInfo)
                || (sameCaster && m_spellInfo.IsAuraExclusiveBySpecificPerCasterWith(existingSpellInfo)))
                return false;

            // check spell group stack rules
            switch (Global.SpellMgr.CheckSpellGroupStackRules(m_spellInfo, existingSpellInfo))
            {
                case SpellGroupStackRule.Exclusive:
                case SpellGroupStackRule.ExclusiveHighest: // if it reaches this point, existing aura is lower/equal
                    return false;
                case SpellGroupStackRule.ExclusiveFromSameCaster:
                    if (sameCaster)
                        return false;
                    break;
                case SpellGroupStackRule.Default:
                case SpellGroupStackRule.ExclusiveSameEffect:
                default:
                    break;
            }

            if (m_spellInfo.SpellFamilyName != existingSpellInfo.SpellFamilyName)
                return true;

            if (!sameCaster)
            {
                // Channeled auras can stack if not forbidden by db or aura type
                if (existingAura.GetSpellInfo().IsChanneled())
                    return true;

                if (m_spellInfo.HasAttribute(SpellAttr3.DotStackingRule))
                    return true;

                // check same periodic auras
                bool hasPeriodicNonAreaEffect(SpellInfo spellInfo)
                {
                    foreach (var spellEffectInfo in spellInfo.GetEffects())
                    {
                        switch (spellEffectInfo.ApplyAuraName)
                        {
                            // DOT or HOT from different casters will stack
                            case AuraType.PeriodicDamage:
                            case AuraType.PeriodicDummy:
                            case AuraType.PeriodicHeal:
                            case AuraType.PeriodicTriggerSpell:
                            case AuraType.PeriodicEnergize:
                            case AuraType.PeriodicManaLeech:
                            case AuraType.PeriodicLeech:
                            case AuraType.PowerBurn:
                            case AuraType.ObsModPower:
                            case AuraType.ObsModHealth:
                            case AuraType.PeriodicTriggerSpellWithValue:
                            {
                                // periodic auras which target areas are not allowed to stack this way (replenishment for example)
                                if (spellEffectInfo.IsTargetingArea())
                                    return false;

                                return true;
                            }
                            default:
                                break;
                        }
                    }
                    return false;
                }

                if (hasPeriodicNonAreaEffect(m_spellInfo) && hasPeriodicNonAreaEffect(existingSpellInfo))
                    return true;
            }

            if (HasEffectType(AuraType.ControlVehicle) && existingAura.HasEffectType(AuraType.ControlVehicle))
            {
                Vehicle veh = null;
                if (GetOwner().ToUnit())
                    veh = GetOwner().ToUnit().GetVehicleKit();

                if (!veh)           // We should probably just let it stack. Vehicle system will prevent undefined behaviour later
                    return true;

                if (veh.GetAvailableSeatCount() == 0)
                    return false;   // No empty seat available

                return true; // Empty seat available (skip rest)
            }

            if (HasEffectType(AuraType.ShowConfirmationPrompt) || HasEffectType(AuraType.ShowConfirmationPromptWithDifficulty))
                if (existingAura.HasEffectType(AuraType.ShowConfirmationPrompt) || existingAura.HasEffectType(AuraType.ShowConfirmationPromptWithDifficulty))
                    return false;

            // spell of same spell rank chain
            if (m_spellInfo.IsRankOf(existingSpellInfo))
            {
                // don't allow passive area auras to stack
                if (m_spellInfo.IsMultiSlotAura() && !IsArea())
                    return true;
                if (!GetCastItemGUID().IsEmpty() && !existingAura.GetCastItemGUID().IsEmpty())
                    if (GetCastItemGUID() != existingAura.GetCastItemGUID() && m_spellInfo.HasAttribute(SpellCustomAttributes.EnchantProc))
                        return true;
                // same spell with same caster should not stack
                return false;
            }

            return true;
        }

        public bool IsProcOnCooldown(DateTime now)
        {
            return m_procCooldown > now;
        }

        public void AddProcCooldown(SpellProcEntry procEntry, DateTime now)
        {
            // cooldowns should be added to the whole aura (see 51698 area aura)
            int procCooldown = (int)procEntry.Cooldown;
            Unit caster = GetCaster();
            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.ProcCooldown, ref procCooldown);
            }

            m_procCooldown = now + TimeSpan.FromMilliseconds(procCooldown);
        }

        public void ResetProcCooldown()
        {
            m_procCooldown = DateTime.Now;
        }

        public void PrepareProcToTrigger(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
        {
            bool prepare = CallScriptPrepareProcHandlers(aurApp, eventInfo);
            if (!prepare)
                return;

            SpellProcEntry procEntry = Global.SpellMgr.GetSpellProcEntry(GetSpellInfo());
            Cypher.Assert(procEntry != null);

            PrepareProcChargeDrop(procEntry, eventInfo);

            // cooldowns should be added to the whole aura (see 51698 area aura)
            AddProcCooldown(procEntry, now);

            SetLastProcSuccessTime(now);
        }

        public void PrepareProcChargeDrop(SpellProcEntry procEntry, ProcEventInfo eventInfo)
        {
            // take one charge, aura expiration will be handled in Aura.TriggerProcOnEvent (if needed)
            if (!procEntry.AttributesMask.HasAnyFlag(ProcAttributes.UseStacksForCharges) && IsUsingCharges() && (eventInfo.GetSpellInfo() == null || !eventInfo.GetSpellInfo().HasAttribute(SpellAttr6.DoNotConsumeResources)))
            {
                --m_procCharges;
                SetNeedClientUpdateForTargets();
            }
        }

        public void ConsumeProcCharges(SpellProcEntry procEntry)
        {
            // Remove aura if we've used last charge to proc
            if (procEntry.AttributesMask.HasFlag(ProcAttributes.UseStacksForCharges))
            {
                ModStackAmount(-1);
            }
            else if (IsUsingCharges())
            {
                if (GetCharges() == 0)
                    Remove();
            }
        }

        public uint GetProcEffectMask(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
        {
            SpellProcEntry procEntry = Global.SpellMgr.GetSpellProcEntry(GetSpellInfo());
            // only auras with spell proc entry can trigger proc
            if (procEntry == null)
                return 0;

            // check spell triggering us
            Spell spell = eventInfo.GetProcSpell();
            if (spell)
            {
                // Do not allow auras to proc from effect triggered from itself
                if (spell.IsTriggeredByAura(m_spellInfo))
                    return 0;

                // check if aura can proc when spell is triggered (exception for hunter auto shot & wands)
                if (!GetSpellInfo().HasAttribute(SpellAttr3.CanProcFromProcs) && !procEntry.AttributesMask.HasFlag(ProcAttributes.TriggeredCanProc) && !eventInfo.GetTypeMask().HasFlag(ProcFlags.AutoAttackMask))
                    if (spell.IsTriggered() && !spell.GetSpellInfo().HasAttribute(SpellAttr3.NotAProc))
                        return 0;

                if (spell.m_CastItem != null && procEntry.AttributesMask.HasFlag(ProcAttributes.CantProcFromItemCast))
                    return 0;

                if (spell.GetSpellInfo().HasAttribute(SpellAttr4.SuppressWeaponProcs) && GetSpellInfo().HasAttribute(SpellAttr6.AuraIsWeaponProc))
                    return 0;

                if (GetSpellInfo().HasAttribute(SpellAttr12.OnlyProcFromClassAbilities) && !spell.GetSpellInfo().HasAttribute(SpellAttr13.AllowClassAbilityProcs))
                    return 0;
            }

            // check don't break stealth attr present
            if (m_spellInfo.HasAura(AuraType.ModStealth))
            {
                SpellInfo eventSpellInfo = eventInfo.GetSpellInfo();
                if (eventSpellInfo != null)
                    if (eventSpellInfo.HasAttribute(SpellCustomAttributes.DontBreakStealth))
                        return 0;
            }

            // check if we have charges to proc with
            if (IsUsingCharges())
            {
                if (GetCharges() == 0)
                    return 0;

                if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReqSpellmod))
                {
                    Spell eventSpell = eventInfo.GetProcSpell();
                    if (eventSpell != null)
                        if (!eventSpell.m_appliedMods.Contains(this))
                            return 0;
                }
            }

            // check proc cooldown
            if (IsProcOnCooldown(now))
                return 0;

            // do checks against db data

            if (!SpellManager.CanSpellTriggerProcOnEvent(procEntry, eventInfo))
                return 0;

            // do checks using conditions table
            if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.SpellProc, GetId(), eventInfo.GetActor(), eventInfo.GetActionTarget()))
                return 0;

            // AuraScript Hook
            bool check = CallScriptCheckProcHandlers(aurApp, eventInfo);
            if (!check)
                return 0;

            // At least one effect has to pass checks to proc aura
            uint procEffectMask = aurApp.GetEffectMask();
            foreach (var aurEff in GetAuraEffects())
                if ((procEffectMask & (1u << aurEff.Key)) != 0)
                    if ((procEntry.DisableEffectsMask & (1u << aurEff.Key)) != 0 || !aurEff.Value.CheckEffectProc(aurApp, eventInfo))
                        procEffectMask &= ~(1u << aurEff.Key);

            if (procEffectMask == 0)
                return 0;

            // @todo
            // do allow additional requirements for procs
            // this is needed because this is the last moment in which you can prevent aura charge drop on proc
            // and possibly a way to prevent default checks (if there're going to be any)

            // Check if current equipment meets aura requirements
            // do that only for passive spells
            // @todo this needs to be unified for all kinds of auras
            Unit target = aurApp.GetTarget();
            if (IsPassive() && target.IsPlayer() && GetSpellInfo().EquippedItemClass != ItemClass.None)
            {
                if (!GetSpellInfo().HasAttribute(SpellAttr3.NoProcEquipRequirement))
                {
                    Item item = null;
                    if (GetSpellInfo().EquippedItemClass == ItemClass.Weapon)
                    {
                        if (target.ToPlayer().IsInFeralForm())
                            return 0;

                        DamageInfo damageInfo = eventInfo.GetDamageInfo();
                        if (damageInfo != null)
                        {
                            if (damageInfo.GetAttackType() != WeaponAttackType.OffAttack)
                                item = target.ToPlayer().GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                            else
                                item = target.ToPlayer().GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                        }
                    }
                    else if (GetSpellInfo().EquippedItemClass == ItemClass.Armor)
                    {
                        // Check if player is wearing shield
                        item = target.ToPlayer().GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                    }

                    if (!item || item.IsBroken() || !item.IsFitToSpellRequirements(GetSpellInfo()))
                        return 0;
                }
            }

            if (m_spellInfo.HasAttribute(SpellAttr3.OnlyProcOutdoors))
                if (!target.IsOutdoors())
                    return 0;

            if (m_spellInfo.HasAttribute(SpellAttr3.OnlyProcOnCaster))
                if (target.GetGUID() != GetCasterGUID())
                    return 0;

            if (!m_spellInfo.HasAttribute(SpellAttr4.AllowProcWhileSitting))
                if (!target.IsStandState())
                    return 0;

            bool success = RandomHelper.randChance(CalcProcChance(procEntry, eventInfo));

            SetLastProcAttemptTime(now);

            if (success)
                return procEffectMask;

            return 0;
        }

        double CalcProcChance(SpellProcEntry procEntry, ProcEventInfo eventInfo)
        {
            double chance = procEntry.Chance;
            // calculate chances depending on unit with caster's data
            // so talents modifying chances and judgements will have properly calculated proc chance
            Unit caster = GetCaster();
            if (caster != null)
            {
                // calculate ppm chance if present and we're using weapon
                if (eventInfo.GetDamageInfo() != null && procEntry.ProcsPerMinute != 0)
                {
                    uint WeaponSpeed = caster.GetBaseAttackTime(eventInfo.GetDamageInfo().GetAttackType());
                    chance = caster.GetPPMProcChance(WeaponSpeed, procEntry.ProcsPerMinute, GetSpellInfo());
                }

                if (GetSpellInfo().ProcBasePPM > 0.0f)
                    chance = CalcPPMProcChance(caster);

                // apply chance modifer aura, applies also to ppm chance (see improved judgement of light spell)
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.ProcChance, ref chance);
            }

            // proc chance is reduced by an additional 3.333% per level past 60
            if (procEntry.AttributesMask.HasAnyFlag(ProcAttributes.ReduceProc60) && eventInfo.GetActor().GetLevel() > 60)
                chance = Math.Max(0.0f, (1.0f - ((eventInfo.GetActor().GetLevel() - 60) * 1.0f / 30.0f)) * chance);

            return chance;
        }

        public void TriggerProcOnEvent(uint procEffectMask, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool prevented = CallScriptProcHandlers(aurApp, eventInfo);
            if (!prevented)
            {
                foreach (var aurEff in GetAuraEffects())
                {
                    if (!Convert.ToBoolean(procEffectMask & (1 << aurEff.Key)))
                        continue;

                    // OnEffectProc / AfterEffectProc hooks handled in AuraEffect.HandleProc()
                    if (aurApp.HasEffect(aurEff.Key))
                        aurEff.Value.HandleProc(aurApp, eventInfo);
                }

                CallScriptAfterProcHandlers(aurApp, eventInfo);
            }

            ConsumeProcCharges(Global.SpellMgr.GetSpellProcEntry(GetSpellInfo()));
        }

        public double CalcPPMProcChance(Unit actor)
        {
            // Formula see http://us.battle.net/wow/en/forum/topic/8197741003#1
            double ppm = m_spellInfo.CalcProcPPM(actor, GetCastItemLevel());
            double averageProcInterval = 60.0f / ppm;

            var currentTime = GameTime.Now();
            float secondsSinceLastAttempt = Math.Min((float)(currentTime - m_lastProcAttemptTime).TotalSeconds, 10.0f);
            float secondsSinceLastProc = Math.Min((float)(currentTime - m_lastProcSuccessTime).TotalSeconds, 1000.0f);

            double chance = Math.Max(1.0f, 1.0f + ((secondsSinceLastProc / averageProcInterval - 1.5f) * 3.0f)) * ppm * secondsSinceLastAttempt / 60.0f;
            MathFunctions.RoundToInterval(ref chance, 0.0f, 1.0f);
            return chance * 100.0f;
        }

        void _DeleteRemovedApplications()
        {
            _removedApplications.Clear();
        }

        public void LoadScripts()
        {
            m_loadedScripts = Global.ScriptMgr.CreateAuraScripts(m_spellInfo.Id, this);
            foreach (var script in m_loadedScripts)
            {
                Log.outDebug(LogFilter.Spells, "Aura.LoadScripts: Script `{0}` for aura `{1}` is loaded now", script._GetScriptName(), m_spellInfo.Id);
                script.Register();

                if (script is IAuraScript)
                {
                    foreach (var iFace in script.GetType().GetInterfaces())
                    {
                        if (iFace.Name == nameof(IBaseSpellScript) || iFace.Name == nameof(IAuraScript))
                            continue;

                        if (!m_auraScriptsByType.TryGetValue(iFace, out var spellScripts))
                        {
                            spellScripts = new List<IAuraScript>();
                            m_auraScriptsByType[iFace] = spellScripts;
                        }

                        spellScripts.Add(script);
                        RegisterSpellEffectHandler(script);
                    }
                }
            }
        }

        private void RegisterSpellEffectHandler(AuraScript script)
        {
            if (script is IHasAuraEffects hse)
                foreach (var effect in hse.AuraEffects)
                    if (effect is IAuraEffectHandler se)
                    {
                        uint mask = 0;
                        if (se.EffectIndex == SpellConst.EffectAll || se.EffectIndex == SpellConst.EffectFirstFound)
                        {
                            foreach (var aurEff in GetAuraEffects())
                            {
                                if (se.EffectIndex == SpellConst.EffectFirstFound && mask != 0)
                                    break;

                                if (CheckAuraEffectHandler(se, aurEff.Key))
                                    AddAuraEffect(aurEff.Key, script, se);
                            }
                        }
                        else
                        {
                            if (CheckAuraEffectHandler(se, se.EffectIndex))
                                AddAuraEffect(se.EffectIndex, script, se);
                        }

                    }
        }

        private bool CheckAuraEffectHandler(IAuraEffectHandler ae, int effIndex)
        {
            if (m_spellInfo.GetEffects().Count <= effIndex)
                return false;

            SpellEffectInfo spellEffectInfo = m_spellInfo.GetEffect(effIndex);
            if (spellEffectInfo.ApplyAuraName == 0 && ae.AuraType == 0)
                return true;

            if (spellEffectInfo.ApplyAuraName == 0)
                return false;

            return ae.AuraType == AuraType.Any || spellEffectInfo.ApplyAuraName == ae.AuraType;
        }

        static List<IAuraScript> _dummy = new();
        static List<(IAuraScript, IAuraEffectHandler)> _dummyAuraEffects = new();
        readonly Dictionary<Type, List<IAuraScript>> m_auraScriptsByType = new Dictionary<Type, List<IAuraScript>>();
        Dictionary<int, Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>> _effectHandlers = new Dictionary<int, Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>>();

        public List<IAuraScript> GetAuraScripts<T>() where T : IAuraScript
        {
            if (m_auraScriptsByType.TryGetValue(typeof(T), out List<IAuraScript> scripts))
                return scripts;

            return _dummy;
        }

        public void ForEachAuraScript<T>(Action<T> action) where T : IAuraScript
        {
            foreach(T script in GetAuraScripts<T>())
                action.Invoke(script);
        }

        private void AddAuraEffect(int index, IAuraScript script, IAuraEffectHandler effect)
        {
            if (!_effectHandlers.TryGetValue(index, out var effecTypes))
            {
                effecTypes = new Dictionary<AuraScriptHookType, List<(IAuraScript, IAuraEffectHandler)>>();
                _effectHandlers.Add(index, effecTypes);
            }

            if (!effecTypes.TryGetValue(effect.HookType, out var effects))
            {
                effects = new List<(IAuraScript, IAuraEffectHandler)>();
                effecTypes.Add(effect.HookType, effects);
            }

            effects.Add((script, effect));
        }

        public List<(IAuraScript, IAuraEffectHandler)> GetEffectScripts(AuraScriptHookType h, int index)
        {
            if (_effectHandlers.TryGetValue(index, out var effDict) &&
                effDict.TryGetValue(h, out List<(IAuraScript, IAuraEffectHandler)> scripts))
                return scripts;

            return _dummyAuraEffects;
        }

        public bool UsesScriptType<T>()
        {
            return m_auraScriptsByType.ContainsKey(typeof(T));
        }    

        public virtual void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default) 
        {
            ForEachAuraScript<IAuraOnRemove>(a => a.Remove());
        }

        #region CallScripts

        bool CallScriptCheckAreaTargetHandlers(Unit target)
        {
            bool result = true;
            foreach (IAuraCheckAreaTarget auraScript in GetAuraScripts<IAuraCheckAreaTarget>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckAreaTarget);

                result &= auraScript.CheckAreaTarget(target);

                auraScript._FinishScriptCall();
            }
            return result;
        }

        public void CallScriptDispel(DispelInfo dispelInfo)
        {
            foreach (IAuraOnDispel auraScript in GetAuraScripts<IAuraOnDispel>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.Dispel);

                auraScript.OnDispel(dispelInfo);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptAfterDispel(DispelInfo dispelInfo)
        {
            foreach (IAfterAuraDispel auraScript in GetAuraScripts<IAfterAuraDispel>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.AfterDispel);

                auraScript.HandleDispel(dispelInfo);

                auraScript._FinishScriptCall();
            }
        }

        public bool CallScriptEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            bool preventDefault = false;
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectApply, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectApply, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }

            return preventDefault;
        }

        public bool CallScriptEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            bool preventDefault = false;
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectRemove, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectRemove, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }
            return preventDefault;
        }

        public void CallScriptAfterEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterApply, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterApply, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptAfterEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterRemove, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterRemove, aurApp);

                ((IAuraApplyHandler)auraScript.Item2).Apply(aurEff, mode);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public bool CallScriptEffectPeriodicHandlers(AuraEffect aurEff, AuraApplication aurApp)
        {
            bool preventDefault = false;
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectPeriodic, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectPeriodic, aurApp);

                ((IAuraPeriodic)auraScript.Item2).HandlePeriodic(aurEff);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }

            return preventDefault;
        }

        public void CallScriptEffectUpdatePeriodicHandlers(AuraEffect aurEff)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectUpdatePeriodic, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectUpdatePeriodic);

                ((IAuraUpdatePeriodic)auraScript.Item2).UpdatePeriodic(aurEff);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcAmountHandlers(AuraEffect aurEff, ref double amount, ref bool canBeRecalculated)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcAmount, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcAmount);

                ((IAuraCalcAmount)auraScript.Item2).HandleCalcAmount(aurEff, ref amount, ref canBeRecalculated);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcPeriodicHandlers(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcPeriodic, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcPeriodic);

                ((IAuraCalcPeriodic)auraScript.Item2).CalcPeriodic(aurEff, ref isPeriodic, ref amplitude);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcSpellModHandlers(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectCalcSpellmod, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcSpellmod);

                ((IAuraCalcSpellMod)auraScript.Item2).CalcSpellMod(aurEff, ref spellMod);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcCritChanceHandlers(AuraEffect aurEff, AuraApplication aurApp, Unit victim, ref double critChance)
        {
            foreach (var loadedScript in GetEffectScripts(AuraScriptHookType.EffectCalcCritChance, aurEff.GetEffIndex()))
            {
                loadedScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectCalcCritChance, aurApp);

                ((IAuraCalcCritChance)loadedScript.Item2).CalcCritChance(aurEff, victim, ref critChance);

                loadedScript.Item1._FinishScriptCall();
            }
        }
        
        public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount, ref bool defaultPrevented)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAbsorb, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

                ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, ref absorbAmount);

                defaultPrevented = auraScript.Item1._IsDefaultActionPrevented();
                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterAbsorb, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorb, aurApp);

                ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, ref absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, HealInfo healInfo, ref double absorbAmount, ref bool defaultPrevented)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAbsorbHeal, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

                ((IAuraEffectAbsorbHeal)auraScript.Item2).HandleAbsorb(aurEff, healInfo, ref absorbAmount);

                defaultPrevented = auraScript.Item1._IsDefaultActionPrevented();
                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, HealInfo healInfo, ref double absorbAmount)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterAbsorb, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorbHeal, aurApp);

                ((IAuraEffectAbsorbHeal)auraScript.Item2).HandleAbsorb(aurEff, healInfo, ref absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
        }
        
        public void CallScriptEffectManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount, ref bool defaultPrevented)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectManaShield, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectManaShield, aurApp);

                ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, ref absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectAfterManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double absorbAmount)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterManaShield, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterManaShield, aurApp);

                ((IAuraEffectAbsorb)auraScript.Item2).HandleAbsorb(aurEff, dmgInfo, ref absorbAmount);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEffectSplitHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref double splitAmount)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectSplit, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectSplit, aurApp);

                ((IAuraSplitHandler)auraScript.Item2).Split(aurEff, dmgInfo, ref splitAmount);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public void CallScriptEnterLeaveCombatHandlers(AuraApplication aurApp, bool isNowInCombat)
        {
            foreach (IAuraEnterLeaveCombat loadedScript in GetAuraScripts<IAuraEnterLeaveCombat>())
            {
                loadedScript._PrepareScriptCall(AuraScriptHookType.EnterLeaveCombat, aurApp);

                loadedScript.EnterLeaveCombat(isNowInCombat);

                loadedScript._FinishScriptCall();
            }
        }
        
        public bool CallScriptCheckProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool result = true;
            foreach (IAuraCheckProc auraScript in GetAuraScripts<IAuraCheckProc>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckProc, aurApp);

                result &= auraScript.CheckProc(eventInfo);

                auraScript._FinishScriptCall();
            }

            return result;
        }

        public bool CallScriptPrepareProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool prepare = true;
            foreach (IAuraPrepareProc auraScript in GetAuraScripts<IAuraPrepareProc>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.PrepareProc, aurApp);

                auraScript.DoPrepareProc(eventInfo);

                if (prepare)
                    prepare = !auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }

            return prepare;
        }

        public bool CallScriptProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool handled = false;
            foreach (IAuraOnProc auraScript in GetAuraScripts<IAuraOnProc>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.Proc, aurApp);

                auraScript.OnProc(eventInfo);

                handled |= auraScript._IsDefaultActionPrevented();
                auraScript._FinishScriptCall();
            }

            return handled;
        }

        public void CallScriptAfterProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            foreach (IAuraAfterProc auraScript in GetAuraScripts<IAuraAfterProc>())
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.AfterProc, aurApp);

                auraScript.AfterProc(eventInfo);

                auraScript._FinishScriptCall();
            }
        }

        public bool CallScriptCheckEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool result = true;
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.CheckEffectProc, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.CheckEffectProc, aurApp);

                result &= ((IAuraCheckEffectProc)auraScript.Item2).CheckProc(aurEff, eventInfo);

                auraScript.Item1._FinishScriptCall();
            }

            return result;
        }

        public bool CallScriptEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool preventDefault = false;
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectProc, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectProc, aurApp);

                ((IAuraEffectProcHandler)auraScript.Item2).HandleProc(aurEff, eventInfo);

                if (!preventDefault)
                    preventDefault = auraScript.Item1._IsDefaultActionPrevented();

                auraScript.Item1._FinishScriptCall();
            }
            return preventDefault;
        }

        public void CallScriptAfterEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            foreach (var auraScript in GetEffectScripts(AuraScriptHookType.EffectAfterProc, aurEff.GetEffIndex()))
            {
                auraScript.Item1._PrepareScriptCall(AuraScriptHookType.EffectAfterProc, aurApp);

                ((IAuraEffectProcHandler)auraScript.Item2).HandleProc(aurEff, eventInfo);

                auraScript.Item1._FinishScriptCall();
            }
        }

        public virtual string GetDebugInfo()
        {
            return $"Id: {GetId()} Name: '{GetSpellInfo().SpellName[Global.WorldMgr.GetDefaultDbcLocale()]}' Caster: {GetCasterGUID()}\nOwner: {(GetOwner() != null ? GetOwner().GetDebugInfo() : "NULL")}";
        }
        #endregion

        public SpellInfo GetSpellInfo() { return m_spellInfo; }
        public uint GetId() { return m_spellInfo.Id; }
        public Difficulty GetCastDifficulty() { return m_castDifficulty; }
        public ObjectGuid GetCastId() { return m_castId; }
        public ObjectGuid GetCasterGUID() { return m_casterGuid; }
        public ObjectGuid GetCastItemGUID() { return m_castItemGuid; }
        public uint GetCastItemId() { return m_castItemId; }
        public int GetCastItemLevel() { return m_castItemLevel; }
        public SpellCastVisual GetSpellVisual() { return m_spellVisual; }
        public WorldObject GetOwner() { return m_owner; }
        public Unit GetUnitOwner()
        {
            Cypher.Assert(GetAuraType() == AuraObjectType.Unit);
            return m_owner.ToUnit();
        }
        public DynamicObject GetDynobjOwner()
        {
            Cypher.Assert(GetAuraType() == AuraObjectType.DynObj);
            return m_owner.ToDynamicObject();
        }
        
        public void SetCastItemGUID(ObjectGuid guid)
        {
            m_castItemGuid = guid;
        }

        public void SetCastItemId(uint id)
        {
            m_castItemId = id;
        }

        public void SetCastItemLevel(int level)
        {
            m_castItemLevel = level;
        }

        public void _RegisterForTargets()
        {
            Unit caster = GetCaster();
            UpdateTargetMap(caster, false);
        }
        public void ApplyForTargets()
        {
            Unit caster = GetCaster();
            UpdateTargetMap(caster, true);
        }

        public long GetApplyTime() { return m_applyTime; }
        public int GetMaxDuration() { return m_maxDuration; }
        public void SetMaxDuration(double duration) { SetMaxDuration((int)duration); }
        public void SetMaxDuration(int duration) { m_maxDuration = duration; }
        public int CalcMaxDuration() { return CalcMaxDuration(GetCaster()); }
        public int GetDuration() { return m_duration; }
        public bool IsExpired() { return GetDuration() == 0 && m_dropEvent == null; }
        public bool IsPermanent() { return m_maxDuration == -1; }

        public byte GetCharges() { return m_procCharges; }
        public byte CalcMaxCharges() { return CalcMaxCharges(GetCaster()); }
        byte CalcMaxCharges(Unit caster)
        {
            uint maxProcCharges = m_spellInfo.ProcCharges;
            var procEntry = Global.SpellMgr.GetSpellProcEntry(GetSpellInfo());
            if (procEntry != null)
                maxProcCharges = procEntry.Charges;

            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetSpellInfo(), SpellModOp.ProcCharges, ref maxProcCharges);
            }
            return (byte)maxProcCharges;
        }
        public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default) { return ModCharges(-1, removeMode); }

        public byte GetStackAmount() { return m_stackAmount; }
        public byte GetCasterLevel() { return (byte)m_casterLevel; }

        public bool IsRemoved() { return m_isRemoved; }

        public bool IsSingleTarget() { return m_isSingleTarget; }
        public void SetIsSingleTarget(bool val) { m_isSingleTarget = val; }

        public bool HasEffect(int index)
        {
            return GetEffect(index) != null;
        }
        public AuraEffect GetEffect(int index)
        {
            if (_effects.TryGetValue(index, out var val))
                return val;

            return null;
        }
        public uint GetEffectMask()
        {
            uint effMask = 0;
            foreach (var aurEff in GetAuraEffects())
                effMask |= (uint)(1 << aurEff.Value.GetEffIndex());

            return effMask;
        }

        public Dictionary<ObjectGuid, AuraApplication> GetApplicationMap() { return m_applications; }
        public AuraApplication GetApplicationOfTarget(ObjectGuid guid) { return m_applications.LookupByKey(guid); }
        public bool IsAppliedOnTarget(ObjectGuid guid) { return m_applications.ContainsKey(guid); }

        public bool IsUsingCharges() { return m_isUsingCharges; }
        public void SetUsingCharges(bool val) { m_isUsingCharges = val; }

        public virtual void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster) { }

        public Dictionary<int, AuraEffect> GetAuraEffects() { return _effects; }

        public void SetLastProcAttemptTime(DateTime lastProcAttemptTime) { m_lastProcAttemptTime = lastProcAttemptTime; }
        public void SetLastProcSuccessTime(DateTime lastProcSuccessTime) { m_lastProcSuccessTime = lastProcSuccessTime; }

        public UnitAura ToUnitAura() { if (GetAuraType() == AuraObjectType.Unit) return (UnitAura)this; else return null; }

        public DynObjAura ToDynObjAura() { if (GetAuraType() == AuraObjectType.DynObj) return (DynObjAura)this; else return null; }

        //Static Methods
        public static uint BuildEffectMaskForOwner(SpellInfo spellProto, uint availableEffectMask, WorldObject owner)
        {
            Cypher.Assert(spellProto != null);
            Cypher.Assert(owner != null);
            uint effMask = 0;
            switch (owner.GetTypeId())
            {
                case TypeId.Unit:
                case TypeId.Player:
                    foreach (var spellEffectInfo in spellProto.GetEffects())
                    {
                        if (spellEffectInfo.IsUnitOwnedAuraEffect())
                            effMask |= (1u << spellEffectInfo.EffectIndex);
                    }
                    break;
                case TypeId.DynamicObject:
                    foreach (var spellEffectInfo in spellProto.GetEffects())
                    {
                        if (spellEffectInfo.Effect == SpellEffectName.PersistentAreaAura)
                            effMask |= (1u << spellEffectInfo.EffectIndex);
                    }
                    break;
                default:
                    break;
            }
            return (effMask & availableEffectMask);
        }
        public static Aura TryRefreshStackOrCreate(AuraCreateInfo createInfo, bool updateEffectMask = true)
        {
            Cypher.Assert(createInfo.Caster != null || !createInfo.CasterGUID.IsEmpty());

            createInfo.IsRefresh = false;

            createInfo._auraEffectMask = BuildEffectMaskForOwner(createInfo.GetSpellInfo(), createInfo.GetAuraEffectMask(), createInfo.GetOwner());
            createInfo._targetEffectMask &= createInfo._auraEffectMask;

            uint effMask = createInfo._auraEffectMask;
            if (createInfo._targetEffectMask != 0)
                effMask = createInfo._targetEffectMask;

            if (effMask == 0)
                return null;

            Aura foundAura = createInfo.GetOwner().ToUnit()._TryStackingOrRefreshingExistingAura(createInfo);
            if (foundAura != null)
            {
                // we've here aura, which script triggered removal after modding stack amount
                // check the state here, so we won't create new Aura object
                if (foundAura.IsRemoved())
                    return null;

                createInfo.IsRefresh = true;

                // add owner
                Unit unit = createInfo.GetOwner().ToUnit();

                // check effmask on owner application (if existing)
                if (updateEffectMask)
                {
                    AuraApplication aurApp = foundAura.GetApplicationOfTarget(unit.GetGUID());
                    if (aurApp != null)
                        aurApp.UpdateApplyEffectMask(effMask, false);
                }

                return foundAura;
            }
            else
                return Create(createInfo);
        }

        public static Aura TryCreate(AuraCreateInfo createInfo)
        {
            uint effMask = createInfo._auraEffectMask;
            if (createInfo._targetEffectMask != 0)
                effMask = createInfo._targetEffectMask;

            effMask = BuildEffectMaskForOwner(createInfo.GetSpellInfo(), effMask, createInfo.GetOwner());
            if (effMask == 0)
                return null;

            return Create(createInfo);
        }

        public static Aura Create(AuraCreateInfo createInfo)
        {
            // try to get caster of aura
            if (!createInfo.CasterGUID.IsEmpty())
            {
                if (createInfo.CasterGUID.IsUnit())
                {
                    if (createInfo._owner.GetGUID() == createInfo.CasterGUID)
                        createInfo.Caster = createInfo._owner.ToUnit();
                    else
                        createInfo.Caster = Global.ObjAccessor.GetUnit(createInfo._owner, createInfo.CasterGUID);
                }
            }
            else if (createInfo.Caster != null)
                createInfo.CasterGUID = createInfo.Caster.GetGUID();

            // check if aura can be owned by owner
            if (createInfo.GetOwner().IsTypeMask(TypeMask.Unit))
                if (!createInfo.GetOwner().IsInWorld || createInfo.GetOwner().ToUnit().IsDuringRemoveFromWorld())
                    // owner not in world so don't allow to own not self casted single target auras
                    if (createInfo.CasterGUID != createInfo.GetOwner().GetGUID() && createInfo.GetSpellInfo().IsSingleTarget())
                        return null;

            Aura aura;
            switch (createInfo.GetOwner().GetTypeId())
            {
                case TypeId.Unit:
                case TypeId.Player:
                    aura = new UnitAura(createInfo);

                    // aura can be removed in Unit::_AddAura call
                    if (aura.IsRemoved())
                        return null;

                    // add owner
                    uint effMask = createInfo._auraEffectMask;
                    if (createInfo._targetEffectMask != 0)
                        effMask = createInfo._targetEffectMask;

                    effMask = BuildEffectMaskForOwner(createInfo.GetSpellInfo(), effMask, createInfo.GetOwner());
                    Cypher.Assert(effMask != 0);

                    Unit unit = createInfo.GetOwner().ToUnit();
                    aura.ToUnitAura().AddStaticApplication(unit, effMask);
                    break;
                case TypeId.DynamicObject:
                    createInfo._auraEffectMask = BuildEffectMaskForOwner(createInfo.GetSpellInfo(), createInfo._auraEffectMask, createInfo.GetOwner());
                    Cypher.Assert(createInfo._auraEffectMask != 0);

                    aura = new DynObjAura(createInfo);
                    break;
                default:
                    Cypher.Assert(false);
                    return null;
            }

            // scripts, etc.
            if (aura.IsRemoved())
                return null;

            return aura;
        }

        #region Fields
        List<AuraScript> m_loadedScripts = new();
        SpellInfo m_spellInfo;
        Difficulty m_castDifficulty;
        ObjectGuid m_castId;
        ObjectGuid m_casterGuid;
        ObjectGuid m_castItemGuid;
        uint m_castItemId;
        int m_castItemLevel;
        SpellCastVisual m_spellVisual;
        long m_applyTime;
        WorldObject m_owner;

        int m_maxDuration;                                // Max aura duration
        int m_duration;                                   // Current time
        int m_timeCla;                                    // Timer for power per sec calcultion
        List<SpellPowerRecord> m_periodicCosts = new();// Periodic costs
        int m_updateTargetMapInterval;                    // Timer for UpdateTargetMapOfEffect

        uint m_casterLevel;                          // Aura level (store caster level for correct show level dep amount)
        byte m_procCharges;                                // Aura charges (0 for infinite)
        byte m_stackAmount;                                // Aura stack amount

        //might need to be arrays still
        Dictionary<int, AuraEffect> _effects;
        Dictionary<ObjectGuid, AuraApplication> m_applications = new();

        bool m_isRemoved;
        bool m_isSingleTarget;                        // true if it's a single target spell and registered at caster - can change at spell steal for example
        bool m_isUsingCharges;

        ChargeDropEvent m_dropEvent;

        DateTime m_procCooldown;
        DateTime m_lastProcAttemptTime;
        DateTime m_lastProcSuccessTime;

        List<AuraApplication> _removedApplications = new();
        #endregion
    }

    public class UnitAura : Aura
    {
        public UnitAura(AuraCreateInfo createInfo) : base(createInfo)
        {
            m_AuraDRGroup = DiminishingGroup.None;
            LoadScripts();
            _InitEffects(createInfo._auraEffectMask, createInfo.Caster, createInfo.BaseAmount);
            GetUnitOwner()._AddAura(this, createInfo.Caster);
        }

        public override void _ApplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
        {
            base._ApplyForTarget(target, caster, aurApp);

            // register aura diminishing on apply
            if (m_AuraDRGroup != DiminishingGroup.None)
                target.ApplyDiminishingAura(m_AuraDRGroup, true);
        }
        public override void _UnapplyForTarget(Unit target, Unit caster, AuraApplication aurApp)
        {
            base._UnapplyForTarget(target, caster, aurApp);

            // unregister aura diminishing (and store last time)
            if (m_AuraDRGroup != DiminishingGroup.None)
                target.ApplyDiminishingAura(m_AuraDRGroup, false);
        }
        public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (IsRemoved())
                return;
            GetUnitOwner().RemoveOwnedAura(this, removeMode);
            base.Remove(removeMode);
        }

        public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
        {
            Unit refe = caster;
            if (refe == null)
                refe = GetUnitOwner();

            // add non area aura targets
            // static applications go through spell system first, so we assume they meet conditions
            foreach (var targetPair in _staticApplications)
            {
                Unit target = Global.ObjAccessor.GetUnit(GetUnitOwner(), targetPair.Key);
                if (target == null && targetPair.Key == GetUnitOwner().GetGUID())
                    target = GetUnitOwner();

                if (target)
                    targets.Add(target, targetPair.Value);
            }

            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if (!HasEffect(spellEffectInfo.EffectIndex))
                    continue;

                // area auras only
                if (spellEffectInfo.Effect == SpellEffectName.ApplyAura)
                    continue;

                // skip area update if owner is not in world!
                if (!GetUnitOwner().IsInWorld)
                    continue;

                if (GetUnitOwner().HasUnitState(UnitState.Isolated))
                    continue;

                List<Unit> units = new();
                var condList = spellEffectInfo.ImplicitTargetConditions;

                float radius = spellEffectInfo.CalcRadius(refe);
                float extraSearchRadius = 0.0f;

                SpellTargetCheckTypes selectionType = SpellTargetCheckTypes.Default;
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.ApplyAreaAuraParty:
                    case SpellEffectName.ApplyAreaAuraPartyNonrandom:
                        selectionType = SpellTargetCheckTypes.Party;
                        break;
                    case SpellEffectName.ApplyAreaAuraRaid:
                        selectionType = SpellTargetCheckTypes.Raid;
                        break;
                    case SpellEffectName.ApplyAreaAuraFriend:
                        selectionType = SpellTargetCheckTypes.Ally;
                        break;
                    case SpellEffectName.ApplyAreaAuraEnemy:
                        selectionType = SpellTargetCheckTypes.Enemy;
                        extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
                        break;
                    case SpellEffectName.ApplyAreaAuraPet:
                        if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(GetUnitOwner(), refe, condList))
                            units.Add(GetUnitOwner());
                        goto case SpellEffectName.ApplyAreaAuraOwner;
                    /* fallthrough */
                    case SpellEffectName.ApplyAreaAuraOwner:
                    {
                        Unit owner = GetUnitOwner().GetCharmerOrOwner();
                        if (owner != null)
                            if (GetUnitOwner().IsWithinDistInMap(owner, radius))
                                if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(owner, refe, condList))
                                    units.Add(owner);
                        break;
                    }
                    case SpellEffectName.ApplyAuraOnPet:
                    {
                        Unit pet = Global.ObjAccessor.GetUnit(GetUnitOwner(), GetUnitOwner().GetPetGUID());
                        if (pet  != null)
                            if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(pet, refe, condList))
                                units.Add(pet);
                        break;
                    }
                    case SpellEffectName.ApplyAreaAuraSummons:
                    {
                        if (condList == null || Global.ConditionMgr.IsObjectMeetToConditions(GetUnitOwner(), refe, condList))
                            units.Add(GetUnitOwner());
                        selectionType = SpellTargetCheckTypes.Summoned;
                        break;
                    }
                }

                if (selectionType != SpellTargetCheckTypes.Default)
                {
                    WorldObjectSpellAreaTargetCheck check = new(radius, GetUnitOwner(), refe, GetUnitOwner(), GetSpellInfo(), selectionType, condList, SpellTargetObjectTypes.Unit);
                    UnitListSearcher searcher = new(GetUnitOwner(), units, check, GridType.All);
                    Cell.VisitGrid(GetUnitOwner(), searcher, radius + extraSearchRadius);

                    // by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for auras it is not acceptable
                    units.RemoveAll(unit => !unit.IsSelfOrInSameMap(GetUnitOwner()));
                }

                foreach (Unit unit in units)
                {
                    if (!targets.ContainsKey(unit))
                        targets[unit] = 0;

                    targets[unit] |= 1u << spellEffectInfo.EffectIndex;
                }
            }
        }

        public void AddStaticApplication(Unit target, uint effMask)
        {
            // only valid for non-area auras
            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if ((effMask & (1u << spellEffectInfo.EffectIndex)) != 0 && !spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                    effMask &= ~(1u << spellEffectInfo.EffectIndex);
            }

            if (effMask == 0)
                return;

            if (!_staticApplications.ContainsKey(target.GetGUID()))
                _staticApplications[target.GetGUID()] = 0;

            _staticApplications[target.GetGUID()] |= effMask;
        }

        // Allow Apply Aura Handler to modify and access m_AuraDRGroup
        public void SetDiminishGroup(DiminishingGroup group) { m_AuraDRGroup = group; }
        public DiminishingGroup GetDiminishGroup() { return m_AuraDRGroup; }

        DiminishingGroup m_AuraDRGroup;              // Diminishing
        Dictionary<ObjectGuid, uint> _staticApplications = new(); // non-area auras
    }

    public class DynObjAura : Aura
    {
        public DynObjAura(AuraCreateInfo createInfo) : base(createInfo)
        {
            LoadScripts();
            Cypher.Assert(GetDynobjOwner() != null);
            Cypher.Assert(GetDynobjOwner().IsInWorld);
            Cypher.Assert(GetDynobjOwner().GetMap() == createInfo.Caster.GetMap());
            _InitEffects(createInfo._auraEffectMask, createInfo.Caster, createInfo.BaseAmount);
            GetDynobjOwner().SetAura(this);

        }

        public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (IsRemoved())
                return;
            _Remove(removeMode);
            base.Remove(removeMode);
        }

        public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
        {
            Unit dynObjOwnerCaster = GetDynobjOwner().GetCaster();
            float radius = GetDynobjOwner().GetRadius();

            foreach (var spellEffectInfo in GetSpellInfo().GetEffects())
            {
                if (!HasEffect(spellEffectInfo.EffectIndex))
                    continue;

                // we can't use effect type like area auras to determine check type, check targets
                SpellTargetCheckTypes selectionType = spellEffectInfo.TargetA.GetCheckType();
                if (spellEffectInfo.TargetB.GetReferenceType() == SpellTargetReferenceTypes.Dest)
                    selectionType = spellEffectInfo.TargetB.GetCheckType();

                List<Unit> targetList = new();
                var condList = spellEffectInfo.ImplicitTargetConditions;

                WorldObjectSpellAreaTargetCheck check = new(radius, GetDynobjOwner(), dynObjOwnerCaster, dynObjOwnerCaster, GetSpellInfo(), selectionType, condList, SpellTargetObjectTypes.Unit);
                UnitListSearcher searcher = new(GetDynobjOwner(), targetList, check, GridType.All);
                Cell.VisitGrid(GetDynobjOwner(), searcher, radius);

                // by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for auras it is not acceptable
                targetList.RemoveAll(unit => !unit.IsSelfOrInSameMap(GetDynobjOwner()));

                foreach (var unit in targetList)
                {
                    if (!targets.ContainsKey(unit))
                        targets[unit] = 0;

                    targets[unit] |= 1u << spellEffectInfo.EffectIndex;
                }
            }
        }
    }

    public class ChargeDropEvent : BasicEvent
    {
        public ChargeDropEvent(Aura aura, AuraRemoveMode mode)
        {
            _base = aura;
            _mode = mode;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            // _base is always valid (look in Aura._Remove())
            _base.ModChargesDelayed(-1, _mode);
            return true;
        }

        Aura _base;
        AuraRemoveMode _mode;
    }

    public class AuraKey : IEquatable<AuraKey>
    {
        public AuraKey(ObjectGuid caster, ObjectGuid item, uint spellId, uint effectMask)
        {
            Caster = caster;
            Item = item;
            SpellId = spellId;
            EffectMask = effectMask;
        }

        public static bool operator ==(AuraKey first, AuraKey other)
        {
            if (ReferenceEquals(first, other))
                return true;

            if (ReferenceEquals(first, null) || ReferenceEquals(other, null))
                return false;

            return first.Equals(other);
        }

        public static bool operator !=(AuraKey first, AuraKey other)
        {
            return !(first == other);
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is AuraKey && Equals((AuraKey)obj);
        }

        public bool Equals(AuraKey other)
        {
            return other.Caster == Caster && other.Item == Item && other.SpellId == SpellId && other.EffectMask == EffectMask;
        }

        public override int GetHashCode()
        {
            return new { Caster, Item, SpellId, EffectMask }.GetHashCode();
        }

        public ObjectGuid Caster;
        public ObjectGuid Item;
        public uint SpellId;
        public uint EffectMask;
    }

    public class AuraLoadEffectInfo
    {
        public Dictionary<int, double> Amounts = new();
        public Dictionary<int, double> BaseAmounts = new();
    }

    public class AuraCreateInfo
    {
        public ObjectGuid CasterGUID;
        public Unit Caster;
        public Dictionary<int, double> BaseAmount;
        public ObjectGuid CastItemGUID;
        public uint CastItemId = 0;
        public int CastItemLevel = -1;
        public bool IsRefresh;
        public bool ResetPeriodicTimer = true;

        internal ObjectGuid _castId;
        internal SpellInfo _spellInfo;
        internal Difficulty _castDifficulty;
        internal uint _auraEffectMask;
        internal WorldObject _owner;

        internal uint _targetEffectMask;

        public AuraCreateInfo(ObjectGuid castId, SpellInfo spellInfo, Difficulty castDifficulty, uint auraEffMask, WorldObject owner)
        {
            _castId = castId;
            _spellInfo = spellInfo;
            _castDifficulty = castDifficulty;
            _auraEffectMask = auraEffMask;
            _owner = owner;

            Cypher.Assert(spellInfo != null);
            Cypher.Assert(auraEffMask != 0);
            Cypher.Assert(owner != null);

            Cypher.Assert(auraEffMask <= SpellConst.MaxEffectMask);
        }

        public void SetCasterGUID(ObjectGuid guid) { CasterGUID = guid; }
        public void SetCaster(Unit caster) { Caster = caster; }
        public void SetBaseAmount(Dictionary<int, double> bp) { BaseAmount = bp; }
        public void SetCastItem(ObjectGuid guid, uint itemId, int itemLevel) { CastItemGUID = guid; CastItemId = itemId; CastItemLevel = itemLevel; }
        public void SetPeriodicReset(bool reset) { ResetPeriodicTimer = reset; }
        public void SetOwnerEffectMask(uint effMask) { _targetEffectMask = effMask; }
        public void SetAuraEffectMask(uint effMask) { _auraEffectMask = effMask; }

        public SpellInfo GetSpellInfo() { return _spellInfo; }
        public uint GetAuraEffectMask() { return _auraEffectMask; }
        public WorldObject GetOwner() { return _owner; }
    }
}
