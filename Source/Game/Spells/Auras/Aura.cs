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
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Network.Packets;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

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
    public enum AuraFlags
    {
        None = 0x00,
        NoCaster = 0x01,
        Positive = 0x02,
        Duration = 0x04,
        Scalable = 0x08,
        Negative = 0x10,
        Unk20 = 0x20
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
                foreach (SpellEffectInfo effect in GetBase().GetSpellEffectInfos())
                {
                    if (effect != null && (Convert.ToBoolean((1 << (int)effect.EffectIndex) & effMask) && !GetBase().GetSpellInfo().IsPositiveEffect(effect.EffectIndex)))
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
                foreach (SpellEffectInfo effect in GetBase().GetSpellEffectInfos())
                {
                    if (effect != null && (Convert.ToBoolean((1 << (int)effect.EffectIndex) & effMask) && GetBase().GetSpellInfo().IsPositiveEffect(effect.EffectIndex)))
                    {
                        positiveFound = true;
                        break;
                    }
                }
                _flags |= positiveFound ? AuraFlags.Positive : AuraFlags.Negative;
            }

            if (GetBase().GetSpellInfo().HasAttribute(SpellAttr8.AuraSendAmount) ||
                GetBase().HasEffectType(AuraType.ModSpellCategoryCooldown) ||
                GetBase().HasEffectType(AuraType.ModMaxCharges) ||
                GetBase().HasEffectType(AuraType.ChargeRecoveryMod) ||
                GetBase().HasEffectType(AuraType.ChargeRecoveryMultiplier))
                _flags |= AuraFlags.Scalable;
        }

        public void _HandleEffect(uint effIndex, bool apply)
        {
            AuraEffect aurEff = GetBase().GetEffect(effIndex);
            if (aurEff == null)
            {
                Log.outError(LogFilter.Spells, "Aura {0} has no effect at effectIndex {1} but _HandleEffect was called", GetBase().GetSpellInfo().Id, effIndex);
                return;
            }
            Cypher.Assert(aurEff != null);
            Cypher.Assert(HasEffect(effIndex) == (!apply));
            Cypher.Assert(Convert.ToBoolean((1 << (int)effIndex) & _effectsToApply));
            Log.outDebug(LogFilter.Spells, "AuraApplication._HandleEffect: {0}, apply: {1}: amount: {2}", aurEff.GetAuraType(), apply, aurEff.GetAmount());

            if (apply)
            {
                Cypher.Assert(!Convert.ToBoolean(_effectMask & (1 << (int)effIndex)));
                _effectMask |= (uint)(1 << (int)effIndex);
                aurEff.HandleEffect(this, AuraEffectHandleModes.Real, true);
            }
            else
            {
                Cypher.Assert(Convert.ToBoolean(_effectMask & (1 << (int)effIndex)));
                _effectMask &= ~(uint)(1 << (int)effIndex);
                aurEff.HandleEffect(this, AuraEffectHandleModes.Real, false);
            }
            SetNeedClientUpdate();
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

            auraInfo.AuraData.HasValue = true;

            Aura aura = GetBase();

            AuraDataInfo auraData = auraInfo.AuraData.Value;
            auraData.CastID = aura.GetCastGUID();
            auraData.SpellID = (int)aura.GetId();
            auraData.SpellXSpellVisualID = (int)aura.GetSpellXSpellVisualId();
            auraData.Flags = GetFlags();
            if (aura.GetMaxDuration() > 0 && !aura.GetSpellInfo().HasAttribute(SpellAttr5.HideDuration))
                auraData.Flags |= AuraFlags.Duration;

            auraData.ActiveFlags = GetEffectMask();
            if (!aura.GetSpellInfo().HasAttribute(SpellAttr11.ScalesWithItemLevel))
                auraData.CastLevel = aura.GetCasterLevel();
            else
                auraData.CastLevel = (ushort)aura.GetCastItemLevel();

            // send stack amount for aura which could be stacked (never 0 - causes incorrect display) or charges
            // stack amount has priority over charges (checked on retail with spell 50262)
            auraData.Applications = aura.GetSpellInfo().StackAmount != 0 ? aura.GetStackAmount() : aura.GetCharges();
            if (!auraData.Flags.HasAnyFlag(AuraFlags.NoCaster))
                auraData.CastUnit.Set(aura.GetCasterGUID());

            if (auraData.Flags.HasAnyFlag(AuraFlags.Duration))
            {
                auraData.Duration.Set(aura.GetMaxDuration());
                auraData.Remaining.Set(aura.GetDuration());
            }

            if (auraData.Flags.HasAnyFlag(AuraFlags.Scalable))
            {
                auraData.Points = new float[GetBase().GetAuraEffects().Length];
                foreach (AuraEffect effect in GetBase().GetAuraEffects())
                    if (effect != null && HasEffect(effect.GetEffIndex()))       // Not all of aura's effects have to be applied on every target
                        auraData.Points[effect.GetEffIndex()] = effect.GetAmount();
            }
        }

        public void ClientUpdate(bool remove = false)
        {
            _needClientUpdate = false;

            AuraUpdate update = new AuraUpdate();
            update.UpdateAll = false;
            update.UnitGUID = GetTarget().GetGUID();

            AuraInfo auraInfo = new AuraInfo();
            BuildUpdatePacket(ref auraInfo, remove);
            update.Auras.Add(auraInfo);

            _target.SendMessageToSet(update, true);
        }

        public Unit GetTarget() { return _target; }
        public Aura GetBase() { return _base; }

        public byte GetSlot() { return _slot; }
        public AuraFlags GetFlags() { return _flags; }
        public uint GetEffectMask() { return _effectMask; }
        public bool HasEffect(uint effect)
        {
            Cypher.Assert(effect < SpellConst.MaxEffects);
            return Convert.ToBoolean(_effectMask & (1 << (int)effect));
        }
        public bool IsPositive() { return _flags.HasAnyFlag(AuraFlags.Positive); }
        bool IsSelfcasted() { return !_flags.HasAnyFlag(AuraFlags.NoCaster); }
        public uint GetEffectsToApply() { return _effectsToApply; }

        public void SetRemoveMode(AuraRemoveMode mode) { _removeMode = mode; }
        public AuraRemoveMode GetRemoveMode() { return _removeMode; }
        public bool HasRemoveMode() { return _removeMode != 0; }

        public bool IsNeedClientUpdate() { return _needClientUpdate; }
    }

    public class Aura
    {
        const int UPDATE_TARGET_MAP_INTERVAL = 500;

        public Aura(SpellInfo spellproto, ObjectGuid castId, WorldObject owner, Unit caster, Item castItem, ObjectGuid casterGUID, ObjectGuid castItemGuid, int castItemLevel)
        {
            m_spellInfo = spellproto;
            m_castGuid = castId;
            m_casterGuid = !casterGUID.IsEmpty() ? casterGUID : caster.GetGUID();
            m_castItemGuid = castItem != null ? castItem.GetGUID() : castItemGuid;
            m_castItemLevel = castItemLevel;
            m_spellXSpellVisualId = caster ? caster.GetCastSpellXSpellVisualId(spellproto) : spellproto.GetSpellXSpellVisualId();
            m_applyTime = Time.UnixTime;
            m_owner = owner;
            m_timeCla = 0;
            m_updateTargetMapInterval = 0;
            m_casterLevel = caster != null ? caster.getLevel() : m_spellInfo.SpellLevel;
            m_procCharges = 0;
            m_stackAmount = 1;
            m_isRemoved = false;
            m_isSingleTarget = false;
            m_isUsingCharges = false;
            m_lastProcAttemptTime = (DateTime.Now - TimeSpan.FromSeconds(10));
            m_lastProcSuccessTime = (DateTime.Now - TimeSpan.FromSeconds(120));

            var powers = Global.DB2Mgr.GetSpellPowers(GetId(), caster ? caster.GetMap().GetDifficultyID() : Difficulty.None);
            foreach (var power in powers)
                if (power.ManaPerSecond != 0 || power.PowerPctPerSecond > 0.0f)
                    m_periodicCosts.Add(power);

            if (!m_periodicCosts.Empty())
                m_timeCla = 1 * Time.InMilliseconds;

            m_maxDuration = CalcMaxDuration(caster);
            m_duration = m_maxDuration;
            m_procCharges = CalcMaxCharges(caster);
            m_isUsingCharges = m_procCharges != 0;
            // m_casterLevel = cast item level/caster level, caster level should be saved to db, confirmed with sniffs
        }

        public T GetScript<T>(string scriptName) where T : AuraScript
        {
            return (T)GetScriptByName(scriptName);
        }

        public AuraScript GetScriptByName(string scriptName)
        {
            foreach (var auraScript in m_loadedScripts)
                if (auraScript._GetScriptName().Equals(scriptName))
                    return auraScript;
            return null;
        }

        public void _InitEffects(uint effMask, Unit caster, int[] baseAmount)
        {
            // shouldn't be in constructor - functions in AuraEffect.AuraEffect use polymorphism
            _spellEffectInfos = m_spellInfo.GetEffectsForDifficulty(GetOwner().GetMap().GetDifficultyID());

            _effects = new AuraEffect[GetSpellEffectInfos().Length];

            foreach (SpellEffectInfo effect in GetSpellEffectInfos())
            {
                if (effect != null && Convert.ToBoolean(effMask & (1 << (int)effect.EffectIndex)))
                    _effects[effect.EffectIndex] = new AuraEffect(this, effect.EffectIndex, baseAmount != null ? baseAmount[effect.EffectIndex] : (int?)null, caster);
            }
        }

        public Unit GetCaster()
        {
            if (m_owner.GetGUID() == m_casterGuid)
                return GetUnitOwner();
            AuraApplication aurApp = GetApplicationOfTarget(m_casterGuid);
            if (aurApp != null)
                return aurApp.GetTarget();

            return Global.ObjAccessor.GetUnit(m_owner, m_casterGuid);
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
                if (m_spellInfo.HasAttribute(SpellAttr0.DisabledWhileActive))
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

            m_removedApplications.Add(auraApp);

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

        void UpdateTargetMap(Unit caster, bool apply = true)
        {
            if (IsRemoved())
                return;

            m_updateTargetMapInterval = UPDATE_TARGET_MAP_INTERVAL;

            // fill up to date target list
            //       target, effMask
            Dictionary<Unit, uint> targets = new Dictionary<Unit, uint>();

            FillTargetMap(ref targets, caster);

            List<Unit> targetsToRemove = new List<Unit>();

            // mark all auras as ready to remove
            foreach (var app in m_applications)
            {
                var existing = targets.FirstOrDefault(p => p.Key == app.Value.GetTarget());
                // not found in current area - remove the aura
                if (existing.Key == null)
                    targetsToRemove.Add(app.Value.GetTarget());
                else
                {
                    // needs readding - remove now, will be applied in next update cycle
                    // (dbcs do not have auras which apply on same type of targets but have different radius, so this is not really needed)
                    if (app.Value.GetEffectMask() != existing.Value || !CanBeAppliedOn(existing.Key))
                        targetsToRemove.Add(app.Value.GetTarget());
                    // nothing todo - aura already applied
                    // remove from auras to register list
                    targets.Remove(existing.Key);
                }
            }
            // register auras for units
            foreach (var unit in targets.Keys.ToList())
            {
                // aura mustn't be already applied on target
                AuraApplication aurApp = GetApplicationOfTarget(unit.GetGUID());
                if (aurApp != null)
                {
                    // the core created 2 different units with same guid
                    // this is a major failue, which i can't fix right now
                    // let's remove one unit from aura list
                    // this may cause area aura "bouncing" between 2 units after each update
                    // but because we know the reason of a crash we can remove the assertion for now
                    if (aurApp.GetTarget() != unit)
                    {
                        // remove from auras to register list
                        targets.Remove(unit);
                        continue;
                    }
                    else
                    {
                        // ok, we have one unit twice in target map (impossible, but...)
                        Cypher.Assert(false);
                    }
                }

                var value = targets[unit];

                bool addUnit = true;
                // check target immunities
                for (byte effIndex = 0; effIndex < SpellConst.MaxEffects; ++effIndex)
                {
                    if (unit.IsImmunedToSpellEffect(GetSpellInfo(), effIndex, caster))
                        value &= (byte)~(1 << effIndex);
                }
                if (value == 0 || unit.IsImmunedToSpell(GetSpellInfo(), caster)
                    || !CanBeAppliedOn(unit))
                    addUnit = false;

                if (addUnit && !unit.IsHighestExclusiveAura(this, true))
                    addUnit = false;

                if (addUnit)
                {
                    // persistent area aura does not hit flying targets
                    if (GetAuraType() == AuraObjectType.DynObj)
                    {
                        if (unit.IsInFlight())
                            addUnit = false;
                    }
                    // unit auras can not stack with each other
                    else // (GetAuraType() == UNIT_AURA_TYPE)
                    {
                        // Allow to remove by stack when aura is going to be applied on owner
                        if (unit != m_owner)
                        {
                            // check if not stacking aura already on target
                            // this one prevents unwanted usefull buff loss because of stacking and prevents overriding auras periodicaly by 2 near area aura owners
                            foreach (var iter in unit.GetAppliedAuras())
                            {
                                Aura aura = iter.Value.GetBase();
                                if (!CanStackWith(aura))
                                {
                                    addUnit = false;
                                    break;
                                }
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
                    unit._CreateAuraApplication(this, value);
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
        public void _ApplyEffectForTargets(uint effIndex)
        {
            // prepare list of aura targets
            List<Unit> targetList = new List<Unit>();
            foreach (var app in m_applications.Values)
            {
                if (Convert.ToBoolean(app.GetEffectsToApply() & (1 << (int)effIndex)) && !app.HasEffect(effIndex))
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
            foreach (AuraEffect effect in GetAuraEffects())
                if (effect != null)
                    effect.Update(diff, caster);

            // remove spellmods after effects update
            if (modSpell != null)
                modOwner.SetSpellModTakingSpell(modSpell, false);

            _DeleteRemovedApplications();
        }

        void Update(uint diff, Unit caster)
        {
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
                    else if (caster != null)
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

        int CalcMaxDuration(Unit caster)
        {
            Player modOwner = null;
            int maxDuration;

            if (caster != null)
            {
                modOwner = caster.GetSpellModOwner();
                maxDuration = caster.CalcSpellDuration(m_spellInfo);
            }
            else
                maxDuration = m_spellInfo.GetDuration();

            if (IsPassive() && m_spellInfo.DurationEntry == null)
                maxDuration = -1;

            // IsPermanent() checks max duration (which we are supposed to calculate here)
            if (maxDuration != -1 && modOwner != null)
                modOwner.ApplySpellMod(GetId(), SpellModOp.Duration, ref maxDuration);
            return maxDuration;
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
                        modOwner.ApplySpellMod(GetId(), SpellModOp.Duration, ref duration);
                }
            }

            m_duration = duration;
            SetNeedClientUpdateForTargets();
        }

        public void RefreshDuration(bool withMods = false)
        {
            Unit caster = GetCaster();
            if (withMods && caster)
            {
                int duration = m_spellInfo.GetMaxDuration();
                // Calculate duration of periodics affected by haste.
                if (caster.HasAuraTypeWithAffectMask(AuraType.PeriodicHaste, m_spellInfo) || m_spellInfo.HasAttribute(SpellAttr5.HasteAffectDuration))
                    duration = (int)(duration * caster.GetFloatValue(UnitFields.ModCastSpeed));

                SetMaxDuration(duration);
                SetDuration(duration);
            }
            else
                SetDuration(GetMaxDuration());

            if (!m_periodicCosts.Empty())
                m_timeCla = 1 * Time.InMilliseconds;
        }

        void RefreshTimers(bool resetPeriodicTimer)
        {
            m_maxDuration = CalcMaxDuration();
            if (m_spellInfo.HasAttribute(SpellAttr8.DontResetPeriodicTimer))
            {
                int minPeriod = m_maxDuration;
                for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                {
                    AuraEffect eff = GetEffect(i);
                    if (eff != null)
                    {
                        int period = eff.GetPeriod();
                        if (period != 0)
                            minPeriod = Math.Min(period, minPeriod);
                    }
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
            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
            {
                AuraEffect aurEff = GetEffect(i);
                if (aurEff != null)
                    aurEff.CalculatePeriodic(caster, resetPeriodicTimer, false);
            }
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
            owner.m_Events.AddEvent(m_dropEvent, owner.m_Events.CalculateTime(delay));
        }

        public void SetStackAmount(byte stackAmount)
        {
            m_stackAmount = stackAmount;
            Unit caster = GetCaster();

            List<AuraApplication> applications = GetApplicationList();
            foreach (var appt in applications)
                if (!appt.HasRemoveMode())
                    HandleAuraSpecificMods(appt, caster, false, true);

            foreach (AuraEffect effect in GetAuraEffects())
                if (effect != null)
                    effect.ChangeAmount(effect.CalculateAmount(caster), false, true);

            foreach (var app in applications)
            {
                if (!app.HasRemoveMode())
                {
                    HandleAuraSpecificPeriodics(app, caster);
                    HandleAuraSpecificMods(app, caster, true, true);
                }
            }

            SetNeedClientUpdateForTargets();
        }

        public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default, bool resetPeriodicTimer = true)
        {
            int stackAmount = m_stackAmount + num;

            // limit the stack amount (only on stack increase, stack amount may be changed manually)
            if ((num > 0) && (stackAmount > (int)m_spellInfo.StackAmount))
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

            bool refresh = stackAmount >= GetStackAmount();

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
            foreach (SpellEffectInfo effect in GetSpellEffectInfos())
            {
                if (effect != null && HasEffect(effect.EffectIndex) && effect.ApplyAuraName == auraType)
                    ++count;
            }

            return count > 1;
        }

        public bool IsArea()
        {
            foreach (SpellEffectInfo effect in GetSpellEffectInfos())
            {
                if (effect != null && HasEffect(effect.EffectIndex) && effect.IsAreaAuraEffect())
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

        public bool CanBeSaved()
        {
            if (IsPassive())
                return false;

            if (GetCasterGUID() != GetOwner().GetGUID())
                if (GetSpellInfo().IsSingleTarget())
                    return false;

            // No point in saving this, since the stable dialog can't be open on aura load anyway.
            if (HasEffectType(AuraType.OpenStable))
                return false;

            // Can't save vehicle auras, it requires both caster & target to be in world
            if (HasEffectType(AuraType.ControlVehicle))
                return false;

            // Incanter's Absorbtion - considering the minimal duration and problems with aura stacking
            // we skip saving this aura
            // Also for some reason other auras put as MultiSlot crash core on keeping them after restart,
            // so put here only these for which you are sure they get removed
            switch (GetId())
            {
                case 44413: // Incanter's Absorption
                case 40075: // Fel Flak Fire
                case 55849: // Power Spark
                    return false;
            }

            // When a druid logins, he doesnt have either eclipse power, nor the marker auras, nor the eclipse buffs. Dont save them.
            if (GetId() == 67483 || GetId() == 67484 || GetId() == 48517 || GetId() == 48518)
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
                case SpellSpecificType.Judgement:
                case SpellSpecificType.MagePolymorph:
                    if (aura.GetSpellInfo().GetSpellSpecific() == spec)
                        return true;
                    break;
                default:
                    break;
            }

            if (HasEffectType(AuraType.ControlVehicle) && aura.HasEffectType(AuraType.ControlVehicle))
                return true;

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
                    modOwner.ApplySpellMod(GetId(), SpellModOp.ResistDispelChance, ref resistChance);
            }

            // Dispel resistance from target SPELL_AURA_MOD_DISPEL_RESIST
            // Only affects offensive dispels
            if (offensive && auraTarget != null)
                resistChance += auraTarget.GetTotalAuraModifier(AuraType.ModDispelResist);

            resistChance = resistChance < 0 ? 0 : resistChance;
            resistChance = resistChance > 100 ? 100 : resistChance;
            return 100 - resistChance;
        }

        public AuraKey GenerateKey(out uint recalculateMask)
        {
            AuraKey key = new AuraKey(GetCasterGUID(), GetCastItemGUID(), GetId(), 0);
            recalculateMask = 0;
            for (int i = 0; i < _effects.Length; ++i)
            {
                AuraEffect effect = _effects[i];
                if (effect != null)
                {
                    key.EffectMask |= 1u << i;
                    if (effect.CanBeRecalculated())
                        recalculateMask |= 1u << i;
                }
            }

            return key;
        }

        public void SetLoadedState(int maxduration, int duration, int charges, byte stackamount, uint recalculateMask, int[] amount)
        {
            m_maxDuration = maxduration;
            m_duration = duration;
            m_procCharges = (byte)charges;
            m_isUsingCharges = m_procCharges != 0;
            m_stackAmount = stackamount;
            Unit caster = GetCaster();
            foreach (AuraEffect effect in GetAuraEffects())
            {
                if (effect == null)
                    continue;

                effect.SetAmount(amount[effect.GetEffIndex()]);
                effect.SetCanBeRecalculated(Convert.ToBoolean(recalculateMask & (1 << effect.GetEffIndex())));
                effect.CalculatePeriodic(caster, false, true);
                effect.CalculateSpellMod();
                effect.RecalculateAmount(caster);
            }
        }

        public bool HasEffectType(AuraType type)
        {
            foreach (var eff in GetAuraEffects())
                if (eff != null && HasEffect(eff.GetEffIndex()) && eff.GetAuraType() == type)
                    return true;

            return false;
        }

        public void RecalculateAmountOfEffects()
        {
            Cypher.Assert(!IsRemoved());
            Unit caster = GetCaster();
            foreach (AuraEffect effect in GetAuraEffects())
                if (effect != null && !IsRemoved())
                    effect.RecalculateAmount(caster);
        }

        public void HandleAllEffects(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            Cypher.Assert(!IsRemoved());
            foreach (AuraEffect effect in GetAuraEffects())
                if (effect != null && !IsRemoved())
                    effect.HandleEffect(aurApp, mode, apply);
        }

        public List<AuraApplication> GetApplicationList()
        {
            var applicationList = new List<AuraApplication>();
            foreach (var app in m_applications.Values)
            {
                if (app.GetEffectMask() != 0)
                    applicationList.Add(app);
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
                        target.RemoveAurasDueToSpell(spellArea.spellId);
                    // some auras applied at aura apply
                    else if (spellArea.flags.HasAnyFlag(SpellAreaFlag.AutoCast))
                    {
                        if (!target.HasAura(spellArea.spellId))
                            target.CastSpell(target, spellArea.spellId, true);
                    }
                }
            }

            // handle spell_linked_spell table
            if (!onReapply)
            {
                // apply linked auras
                if (apply)
                {
                    var spellTriggered = Global.SpellMgr.GetSpellLinked((int)GetId() + (int)SpellLinkedType.Aura);
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
                    var spellTriggered = Global.SpellMgr.GetSpellLinked(-(int)GetId());
                    if (spellTriggered != null)
                    {
                        foreach (var spell in spellTriggered)
                        {
                            if (spell < 0)
                                target.RemoveAurasDueToSpell((uint)-spell);
                            else if (removeMode != AuraRemoveMode.Death)
                                target.CastSpell(target, (uint)spell, true, null, null, GetCasterGUID());
                        }
                    }
                    spellTriggered = Global.SpellMgr.GetSpellLinked((int)GetId() + (int)SpellLinkedType.Aura);
                    if (spellTriggered != null)
                    {
                        foreach (var id in spellTriggered)
                        {
                            if (id < 0)
                                target.ApplySpellImmune(GetId(), SpellImmunity.Id, (uint)-id, false);
                            else
                                target.RemoveAura((uint)id, GetCasterGUID(), 0, removeMode);
                        }
                    }
                }
            }
            else if (apply)
            {
                // modify stack amount of linked auras
                var spellTriggered = Global.SpellMgr.GetSpellLinked((int)GetId() + (int)SpellLinkedType.Aura);
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
                            case 32474: // Buffeting Winds of Susurrus
                                if (target.IsTypeId(TypeId.Player))
                                    target.ToPlayer().ActivateTaxiPathTo(506, GetId());
                                break;
                            case 33572: // Gronn Lord's Grasp, becomes stoned
                                if (GetStackAmount() >= 5 && !target.HasAura(33652))
                                    target.CastSpell(target, 33652, true);
                                break;
                            case 50836: //Petrifying Grip, becomes stoned
                                if (GetStackAmount() >= 5 && !target.HasAura(50812))
                                    target.CastSpell(target, 50812, true);
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
                                int heal = GetEffect(0).GetAmount();
                                caster.CastCustomSpell(target, 64801, heal, 0, 0, true, null, GetEffect(0));
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
                                target.CastSpell(target, 32612, true, null, GetEffect(1));
                                target.CombatStop();
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
                                    if (caster.GetSpellHistory().HasCooldown(aura.GetId()))
                                    {
                                        // This additional check is needed to add a minimal delay before cooldown in in effect
                                        // to allow all bubbles broken by a single damage source proc mana return
                                        if (caster.GetSpellHistory().GetRemainingCooldown(aura.GetSpellInfo()) <= 11 * Time.InMilliseconds)
                                            break;
                                    }
                                    else    // and add if needed
                                        caster.GetSpellHistory().AddCooldown(aura.GetId(), 0, TimeSpan.FromSeconds(12));
                                }

                                // effect on caster
                                AuraEffect aurEff = aura.GetEffect(0);
                                if (aurEff != null)
                                {
                                    float multiplier = aurEff.GetAmount();
                                    int basepoints0 = MathFunctions.CalculatePct(caster.GetMaxPower(PowerType.Mana), multiplier);
                                    caster.CastCustomSpell(caster, 47755, basepoints0, 0, 0, true);
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
                case SpellFamilyNames.Rogue:
                    // Stealth
                    if (GetSpellInfo().SpellFamilyFlags[0].HasAnyFlag(0x00400000u))
                    {
                        // Master of subtlety
                        AuraEffect aurEff = target.GetAuraEffect(31223, 0);
                        if (aurEff != null)
                        {
                            if (!apply)
                                target.CastSpell(target, 31666, true);
                            else
                            {
                                int basepoints0 = aurEff.GetAmount();
                                target.CastCustomSpell(target, 31665, basepoints0, 0, 0, true);
                            }
                        }
                        break;
                    }
                    break;
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
                                        owner.CastSpell(owner, 34471, true, null, GetEffect(0));
                                    else
                                        owner.RemoveAurasDueToSpell(34471);
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
                                    target.CastSpell(target, 64364, true);
                            }
                            else
                                target.RemoveAurasDueToSpell(64364, GetCasterGUID());
                            break;
                        case 31842: // Divine Favor
                            // Item - Paladin T10 Holy 2P Bonus
                            if (target.HasAura(70755))
                            {
                                if (apply)
                                    target.CastSpell(target, 71166, true);
                                else
                                    target.RemoveAurasDueToSpell(71166);
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
                                caster.CastSpell(caster, 100001, true);
                        }
                        else
                        {
                            if (target != caster)
                                caster.RemoveAurasDueToSpell(GetId());
                            else
                                caster.RemoveAurasDueToSpell(100001);
                        }
                    }
                    break;
            }
        }

        public void HandleAuraSpecificPeriodics(AuraApplication aurApp, Unit caster)
        {
            Unit target = aurApp.GetTarget();

            if (!caster || aurApp.HasRemoveMode())
                return;

            foreach (AuraEffect effect in GetAuraEffects())
            {
                if (effect == null || effect.IsAreaAuraEffect() || effect.IsEffect(SpellEffectName.PersistentAreaAura))
                    continue;

                switch (effect.GetSpellEffectInfo().ApplyAuraName)
                {
                    case AuraType.PeriodicDamage:
                    case AuraType.PeriodicDamagePercent:
                    case AuraType.PeriodicLeech:
                        {
                            // ignore non positive values (can be result apply spellmods to aura damage
                            uint damage = (uint)Math.Max(effect.GetAmount(), 0);

                            // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
                            Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);

                            effect.SetDonePct(caster.SpellDamagePctDone(target, m_spellInfo, DamageEffectType.DOT)); // Calculate done percentage first!
                            effect.SetDamage((int)(caster.SpellDamageBonusDone(target, m_spellInfo, damage, DamageEffectType.DOT, effect.GetSpellEffectInfo(), GetStackAmount()) * effect.GetDonePct()));
                            effect.SetCritChance(caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()));
                            break;
                        }
                    case AuraType.PeriodicHeal:
                    case AuraType.ObsModHealth:
                        {
                            // ignore non positive values (can be result apply spellmods to aura damage
                            uint damage = (uint)Math.Max(effect.GetAmount(), 0);

                            // Script Hook For HandlePeriodicDamageAurasTick -- Allow scripts to change the Damage pre class mitigation calculations
                            Global.ScriptMgr.ModifyPeriodicDamageAurasTick(target, caster, ref damage);

                            effect.SetDonePct(caster.SpellHealingPctDone(target, m_spellInfo)); // Calculate done percentage first!
                            effect.SetDamage((int)(caster.SpellHealingBonusDone(target, m_spellInfo, damage, DamageEffectType.DOT, effect.GetSpellEffectInfo(), GetStackAmount()) * effect.GetDonePct()));
                            effect.SetCritChance(caster.GetUnitSpellCriticalChance(target, m_spellInfo, m_spellInfo.GetSchoolMask()));
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        bool CanBeAppliedOn(Unit target)
        {
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

            // Dynobj auras always stack
            if (GetAuraType() == AuraObjectType.DynObj || existingAura.GetAuraType() == AuraObjectType.DynObj)
                return true;

            SpellInfo existingSpellInfo = existingAura.GetSpellInfo();
            bool sameCaster = GetCasterGUID() == existingAura.GetCasterGUID();

            // passive auras don't stack with another rank of the spell cast by same caster
            if (IsPassive() && sameCaster && (m_spellInfo.IsDifferentRankOf(existingSpellInfo) || (m_spellInfo.Id == existingSpellInfo.Id && m_castItemGuid.IsEmpty())))
                return false;

            foreach (SpellEffectInfo effect in existingAura.GetSpellEffectInfos())
            {
                // prevent remove triggering aura by triggered aura
                if (effect != null && effect.TriggerSpell == GetId())
                    return true;
            }

            foreach (SpellEffectInfo effect in GetSpellEffectInfos())
            {
                // prevent remove triggered aura by triggering aura refresh
                if (effect != null && effect.TriggerSpell == existingAura.GetId())
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

                if (m_spellInfo.HasAttribute(SpellAttr3.StackForDiffCasters))
                    return true;

                // check same periodic auras
                for (byte i = 0; i < SpellConst.MaxEffects; i++)
                {
                    SpellEffectInfo effect = GetSpellEffectInfo(i);
                    if (effect == null)
                        continue;

                    switch (effect.ApplyAuraName)
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
                            SpellEffectInfo existingEffect = GetSpellEffectInfo(i);
                            // periodic auras which target areas are not allowed to stack this way (replenishment for example)
                            if (effect.IsTargetingArea() || (existingEffect != null && existingEffect.IsTargetingArea()))
                                break;
                            return true;
                        default:
                            break;
                    }
                }
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

        public void AddProcCooldown(DateTime cooldownEnd)
        {
            m_procCooldown = cooldownEnd;
        }

        public void PrepareProcToTrigger(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
        {
            bool prepare = CallScriptPrepareProcHandlers(aurApp, eventInfo);
            if (!prepare)
                return;

            // take one charge, aura expiration will be handled in Aura.TriggerProcOnEvent (if needed)
            if (IsUsingCharges())
            {
                --m_procCharges;
                SetNeedClientUpdateForTargets();
            }

            SpellProcEntry procEntry = Global.SpellMgr.GetSpellProcEntry(GetId());

            Cypher.Assert(procEntry != null);

            // cooldowns should be added to the whole aura (see 51698 area aura)
            AddProcCooldown(now + TimeSpan.FromMilliseconds(procEntry.Cooldown));

            SetLastProcSuccessTime(now);
        }

        public uint IsProcTriggeredOnEvent(AuraApplication aurApp, ProcEventInfo eventInfo, DateTime now)
        {
            SpellProcEntry procEntry = Global.SpellMgr.GetSpellProcEntry(GetId());
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
                if (spell.IsTriggered() && !procEntry.AttributesMask.HasAnyFlag(ProcAttributes.TriggeredCanProc) && !eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.AutoAttackMask))
                    if (!GetSpellInfo().HasAttribute(SpellAttr3.CanProcWithTriggered))
                        return 0;
            }

            // check don't break stealth attr present
            if (m_spellInfo.HasAura(Difficulty.None, AuraType.ModStealth))
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
            uint procEffectMask = 0;
            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                if (aurApp.HasEffect(i))
                    if (GetEffect(i).CheckEffectProc(aurApp, eventInfo))
                        procEffectMask |= (1u << i);

            if (procEffectMask == 0)
                return 0;

            // @todo
            // do allow additional requirements for procs
            // this is needed because this is the last moment in which you can prevent aura charge drop on proc
            // and possibly a way to prevent default checks (if there're going to be any)

            // Aura added by spell can't trigger from self (prevent drop charges/do triggers)
            // But except periodic and kill triggers (can triggered from self)
            SpellInfo spellInfo = eventInfo.GetSpellInfo();
            if (spellInfo != null)
                if (spellInfo.Id == GetId() && !eventInfo.GetTypeMask().HasAnyFlag(ProcFlags.TakenPeriodic | ProcFlags.Kill))
                    return 0;

            // Check if current equipment meets aura requirements
            // do that only for passive spells
            // @todo this needs to be unified for all kinds of auras
            Unit target = aurApp.GetTarget();
            if (IsPassive() && target.IsTypeId(TypeId.Player))
            {
                if (GetSpellInfo().EquippedItemClass == ItemClass.Weapon)
                {
                    if (target.ToPlayer().IsInFeralForm())
                        return 0;

                    DamageInfo damageInfo = eventInfo.GetDamageInfo();
                    if (damageInfo != null)
                    {
                        WeaponAttackType attType = damageInfo.GetAttackType();
                        Item item = null;
                        if (attType == WeaponAttackType.BaseAttack || attType == WeaponAttackType.RangedAttack)
                            item = target.ToPlayer().GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);
                        else if (attType == WeaponAttackType.OffAttack)
                            item = target.ToPlayer().GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

                        if (item == null || item.IsBroken() || item.GetTemplate().GetClass() != ItemClass.Weapon || !Convert.ToBoolean((1 << (int)item.GetTemplate().GetSubClass()) & GetSpellInfo().EquippedItemSubClassMask))
                            return 0;
                    }
                }
                else if (GetSpellInfo().EquippedItemClass == ItemClass.Armor)
                {
                    // Check if player is wearing shield
                    Item item = target.ToPlayer().GetUseableItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);
                    if (item == null || item.IsBroken() || item.GetTemplate().GetClass() != ItemClass.Armor || !Convert.ToBoolean((1 << (int)item.GetTemplate().GetSubClass()) & GetSpellInfo().EquippedItemSubClassMask))
                        return 0;
                }
            }

            bool success = RandomHelper.randChance(CalcProcChance(procEntry, eventInfo));

            SetLastProcAttemptTime(now);

            if (success)
                return procEffectMask;

            return 0;
        }

        float CalcProcChance(SpellProcEntry procEntry, ProcEventInfo eventInfo)
        {
            float chance = procEntry.Chance;
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
                    modOwner.ApplySpellMod(GetId(), SpellModOp.ChanceOfSuccess, ref chance);
            }
            return chance;
        }

        public void TriggerProcOnEvent(uint procEffectMask, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool prevented = CallScriptProcHandlers(aurApp, eventInfo);
            if (!prevented)
            {
                for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                {
                    if (!Convert.ToBoolean(procEffectMask & (1 << i)))
                        continue;

                    // OnEffectProc / AfterEffectProc hooks handled in AuraEffect.HandleProc()
                    if (aurApp.HasEffect(i))
                        GetEffect(i).HandleProc(aurApp, eventInfo);
                }

                CallScriptAfterProcHandlers(aurApp, eventInfo);
            }

            // Remove aura if we've used last charge to proc
            if (IsUsingCharges() && GetCharges() == 0)
                Remove();
        }

        public float CalcPPMProcChance(Unit actor)
        {
            // Formula see http://us.battle.net/wow/en/forum/topic/8197741003#1
            float ppm = m_spellInfo.CalcProcPPM(actor, m_castItemLevel);
            float averageProcInterval = 60.0f / ppm;

            var currentTime = DateTime.Now;
            float secondsSinceLastAttempt = Math.Min((float)(currentTime - m_lastProcAttemptTime).TotalSeconds, 10.0f);
            float secondsSinceLastProc = Math.Min((float)(currentTime - m_lastProcSuccessTime).TotalSeconds, 1000.0f);

            float chance = Math.Max(1.0f, 1.0f + ((secondsSinceLastProc / averageProcInterval - 1.5f) * 3.0f)) * ppm * secondsSinceLastAttempt / 60.0f;
            MathFunctions.RoundToInterval(ref chance, 0.0f, 1.0f);
            return chance * 100.0f;
        }

        void _DeleteRemovedApplications()
        {
            m_removedApplications.Clear();
        }

        public void LoadScripts()
        {
            m_loadedScripts = Global.ScriptMgr.CreateAuraScripts(m_spellInfo.Id, this);
            foreach (var script in m_loadedScripts)
            {
                Log.outDebug(LogFilter.Spells, "Aura.LoadScripts: Script `{0}` for aura `{1}` is loaded now", script._GetScriptName(), m_spellInfo.Id);
                script.Register();
            }
        }

        public virtual void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default) { }
        #region CallScripts

        bool CallScriptCheckAreaTargetHandlers(Unit target)
        {
            bool result = true;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckAreaTarget);

                foreach (var hook in auraScript.DoCheckAreaTarget)
                    result &= hook.Call(target);

                auraScript._FinishScriptCall();
            }
            return result;
        }

        public void CallScriptDispel(DispelInfo dispelInfo)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.Dispel);

                foreach (var hook in auraScript.OnDispel)
                    hook.Call(dispelInfo);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptAfterDispel(DispelInfo dispelInfo)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.AfterDispel);

                foreach (var hook in auraScript.AfterDispel)
                    hook.Call(dispelInfo);

                auraScript._FinishScriptCall();
            }
        }

        public bool CallScriptEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            bool preventDefault = false;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectApply, aurApp);

                foreach (var eff in auraScript.OnEffectApply)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, mode);

                if (!preventDefault)
                    preventDefault = auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }

            return preventDefault;
        }

        public bool CallScriptEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            bool preventDefault = false;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectRemove, aurApp);

                foreach (var eff in auraScript.OnEffectRemove)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, mode);

                if (!preventDefault)
                    preventDefault = auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }
            return preventDefault;
        }

        public void CallScriptAfterEffectApplyHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectAfterApply, aurApp);

                foreach (var eff in auraScript.AfterEffectApply)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, mode);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptAfterEffectRemoveHandlers(AuraEffect aurEff, AuraApplication aurApp, AuraEffectHandleModes mode)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectAfterRemove, aurApp);

                foreach (var eff in auraScript.AfterEffectRemove)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, mode);

                auraScript._FinishScriptCall();
            }
        }

        public bool CallScriptEffectPeriodicHandlers(AuraEffect aurEff, AuraApplication aurApp)
        {
            bool preventDefault = false;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectPeriodic, aurApp);

                foreach (var eff in auraScript.OnEffectPeriodic)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff);

                if (!preventDefault)
                    preventDefault = auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }

            return preventDefault;
        }

        public void CallScriptEffectUpdatePeriodicHandlers(AuraEffect aurEff)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectUpdatePeriodic);

                foreach (var eff in auraScript.OnEffectUpdatePeriodic)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcAmountHandlers(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectCalcAmount);
                foreach (var eff in auraScript.DoEffectCalcAmount)
                {
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, ref amount, ref canBeRecalculated);
                }

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcPeriodicHandlers(AuraEffect aurEff, ref bool isPeriodic, ref int amplitude)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectCalcPeriodic);

                foreach (var eff in auraScript.DoEffectCalcPeriodic)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, ref isPeriodic, ref amplitude);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectCalcSpellModHandlers(AuraEffect aurEff, ref SpellModifier spellMod)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectCalcSpellmod);

                foreach (var eff in auraScript.DoEffectCalcSpellMod)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, ref spellMod);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref uint absorbAmount, ref bool defaultPrevented)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectAbsorb, aurApp);

                foreach (var eff in auraScript.OnEffectAbsorb)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, dmgInfo, ref absorbAmount);

                defaultPrevented = auraScript._IsDefaultActionPrevented();
                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectAfterAbsorbHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectAfterAbsorb, aurApp);

                foreach (var eff in auraScript.AfterEffectAbsorb)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, dmgInfo, ref absorbAmount);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref uint absorbAmount, ref bool defaultPrevented)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectManaShield, aurApp);

                foreach (var eff in auraScript.OnEffectManaShield)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, dmgInfo, ref absorbAmount);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectAfterManaShieldHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, ref uint absorbAmount)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectAfterManaShield, aurApp);

                foreach (var eff in auraScript.AfterEffectManaShield)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, dmgInfo, ref absorbAmount);

                auraScript._FinishScriptCall();
            }
        }

        public void CallScriptEffectSplitHandlers(AuraEffect aurEff, AuraApplication aurApp, DamageInfo dmgInfo, uint splitAmount)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectSplit, aurApp);

                foreach (var eff in auraScript.OnEffectSplit)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, dmgInfo, splitAmount);

                auraScript._FinishScriptCall();
            }
        }

        public bool CallScriptCheckProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool result = true;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckProc, aurApp);

                foreach (var hook in auraScript.DoCheckProc)
                    result &= hook.Call(eventInfo);

                auraScript._FinishScriptCall();
            }

            return result;
        }

        public bool CallScriptPrepareProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool prepare = true;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.PrepareProc, aurApp);

                foreach (var eff in auraScript.DoPrepareProc)
                    eff.Call(eventInfo);

                if (prepare)
                    prepare = !auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }

            return prepare;
        }

        public bool CallScriptProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool handled = false;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.Proc, aurApp);

                foreach (var hook in auraScript.OnProc)
                    hook.Call(eventInfo);

                handled |= auraScript._IsDefaultActionPrevented();
                auraScript._FinishScriptCall();
            }

            return handled;
        }

        public void CallScriptAfterProcHandlers(AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.AfterProc, aurApp);

                foreach (var hook in auraScript.AfterProc)
                    hook.Call(eventInfo);

                auraScript._FinishScriptCall();
            }
        }

        public bool CallScriptCheckEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool result = true;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.CheckEffectProc, aurApp);

                foreach (var hook in auraScript.DoCheckEffectProc)
                    if (hook.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        result &= hook.Call(aurEff, eventInfo);

                auraScript._FinishScriptCall();
            }

            return result;
        }

        public bool CallScriptEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            bool preventDefault = false;
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectProc, aurApp);

                foreach (var eff in auraScript.OnEffectProc)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, eventInfo);

                if (!preventDefault)
                    preventDefault = auraScript._IsDefaultActionPrevented();

                auraScript._FinishScriptCall();
            }
            return preventDefault;
        }

        public void CallScriptAfterEffectProcHandlers(AuraEffect aurEff, AuraApplication aurApp, ProcEventInfo eventInfo)
        {
            foreach (var auraScript in m_loadedScripts)
            {
                auraScript._PrepareScriptCall(AuraScriptHookType.EffectAfterProc, aurApp);

                foreach (var eff in auraScript.AfterEffectProc)
                    if (eff.IsEffectAffected(m_spellInfo, aurEff.GetEffIndex()))
                        eff.Call(aurEff, eventInfo);

                auraScript._FinishScriptCall();
            }
        }

        #endregion

        public SpellInfo GetSpellInfo() { return m_spellInfo; }
        public uint GetId() { return m_spellInfo.Id; }
        public ObjectGuid GetCastGUID() { return m_castGuid; }
        public ObjectGuid GetCasterGUID() { return m_casterGuid; }
        public ObjectGuid GetCastItemGUID() { return m_castItemGuid; }
        public int GetCastItemLevel() { return m_castItemLevel; }
        public uint GetSpellXSpellVisualId() { return m_spellXSpellVisualId; }
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
            var procEntry = Global.SpellMgr.GetSpellProcEntry(GetId());
            if (procEntry != null)
                maxProcCharges = procEntry.Charges;

            if (caster != null)
            {
                Player modOwner = caster.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(GetId(), SpellModOp.Charges, ref maxProcCharges);
            }
            return (byte)maxProcCharges;
        }
        public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default) { return ModCharges(-1, removeMode); }

        public byte GetStackAmount() { return m_stackAmount; }
        public byte GetCasterLevel() { return (byte)m_casterLevel; }

        public bool IsRemovedOnShapeLost(Unit target)
        {
            return GetCasterGUID() == target.GetGUID()
                    && m_spellInfo.Stances != 0
                    && !m_spellInfo.HasAttribute(SpellAttr2.NotNeedShapeshift)
                    && !m_spellInfo.HasAttribute(SpellAttr0.NotShapeshift);
        }
        public bool IsRemoved() { return m_isRemoved; }

        public bool IsSingleTarget() { return m_isSingleTarget; }
        public void SetIsSingleTarget(bool val) { m_isSingleTarget = val; }

        public bool HasEffect(uint index)
        {
            return GetEffect(index) != null;
        }
        public AuraEffect GetEffect(uint index)
        {
            if (index >= _effects.Length)
                return null;

            return _effects[index];
        }
        public uint GetEffectMask()
        {
            uint effMask = 0;
            foreach (AuraEffect effect in GetAuraEffects())
                if (effect != null)
                    effMask |= (uint)(1 << effect.GetEffIndex());
            return effMask;
        }

        public Dictionary<ObjectGuid, AuraApplication> GetApplicationMap() { return m_applications; }
        public AuraApplication GetApplicationOfTarget(ObjectGuid guid) { return m_applications.LookupByKey(guid); }
        public bool IsAppliedOnTarget(ObjectGuid guid) { return m_applications.ContainsKey(guid); }

        public bool IsUsingCharges() { return m_isUsingCharges; }
        public void SetUsingCharges(bool val) { m_isUsingCharges = val; }

        public virtual void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster) { }

        public AuraEffect[] GetAuraEffects() { return _effects; }

        public SpellEffectInfo[] GetSpellEffectInfos() { return _spellEffectInfos; }
        public SpellEffectInfo GetSpellEffectInfo(uint index)
        {
            if (index >= _spellEffectInfos.Length)
                return null;

            return _spellEffectInfos[index];
        }

        public void SetLastProcAttemptTime(DateTime lastProcAttemptTime) { m_lastProcAttemptTime = lastProcAttemptTime; }
        public void SetLastProcSuccessTime(DateTime lastProcSuccessTime) { m_lastProcSuccessTime = lastProcSuccessTime; }

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
                    foreach (SpellEffectInfo effect in spellProto.GetEffectsForDifficulty(owner.GetMap().GetDifficultyID()))
                    {
                        if (effect != null && effect.IsUnitOwnedAuraEffect())
                            effMask |= (uint)(1 << (int)effect.EffectIndex);
                    }
                    break;
                case TypeId.DynamicObject:
                    foreach (SpellEffectInfo effect in spellProto.GetEffectsForDifficulty(owner.GetMap().GetDifficultyID()))
                    {
                        if (effect != null && effect.Effect == SpellEffectName.PersistentAreaAura)
                            effMask |= (uint)(1 << (int)effect.EffectIndex);
                    }
                    break;
                default:
                    break;
            }
            return (effMask & availableEffectMask);
        }
        public static Aura TryRefreshStackOrCreate(SpellInfo spellproto, ObjectGuid castId, uint tryEffMask, WorldObject owner, Unit caster, int[] baseAmount = null, Item castItem = null, ObjectGuid casterGUID = default(ObjectGuid), bool resetPeriodicTimer = true, ObjectGuid castItemGuid = default(ObjectGuid), int castItemLevel = -1)
        {
            bool throwway;
            return TryRefreshStackOrCreate(spellproto, castId, tryEffMask, owner, caster, out throwway, baseAmount, castItem, casterGUID, resetPeriodicTimer, castItemGuid, castItemLevel);
        }
        public static Aura TryRefreshStackOrCreate(SpellInfo spellproto, ObjectGuid castId, uint tryEffMask, WorldObject owner, Unit caster, out bool refresh, int[] baseAmount, Item castItem = null, ObjectGuid casterGUID = default(ObjectGuid), bool resetPeriodicTimer = true, ObjectGuid castItemGuid = default(ObjectGuid), int castItemLevel = -1)
        {
            Cypher.Assert(spellproto != null);
            Cypher.Assert(owner != null);
            Cypher.Assert(caster || !casterGUID.IsEmpty());
            Cypher.Assert(tryEffMask <= SpellConst.MaxEffectMask);
            refresh = false;

            uint effMask = BuildEffectMaskForOwner(spellproto, tryEffMask, owner);
            if (effMask == 0)
                return null;
            Aura foundAura = owner.ToUnit()._TryStackingOrRefreshingExistingAura(spellproto, effMask, caster, baseAmount, castItem, casterGUID, resetPeriodicTimer, castItemGuid, castItemLevel);
            if (foundAura != null)
            {
                // we've here aura, which script triggered removal after modding stack amount
                // check the state here, so we won't create new Aura object
                if (foundAura.IsRemoved())
                    return null;

                refresh = true;
                return foundAura;
            }
            else
                return Create(spellproto, castId, effMask, owner, caster, baseAmount, castItem, casterGUID, castItemGuid, castItemLevel);
        }
        public static Aura TryCreate(SpellInfo spellproto, ObjectGuid castId, uint tryEffMask, WorldObject owner, Unit caster, int[] baseAmount, Item castItem = null, ObjectGuid casterGUID = default(ObjectGuid), ObjectGuid castItemGuid = default(ObjectGuid), int castItemLevel = -1)
        {
            Cypher.Assert(spellproto != null);
            Cypher.Assert(owner != null);
            Cypher.Assert(caster != null || !casterGUID.IsEmpty());
            Cypher.Assert(tryEffMask <= SpellConst.MaxEffectMask);
            uint effMask = BuildEffectMaskForOwner(spellproto, tryEffMask, owner);
            if (effMask == 0)
                return null;
            return Create(spellproto, castId, effMask, owner, caster, baseAmount, castItem, casterGUID, castItemGuid, castItemLevel);
        }
        public static Aura Create(SpellInfo spellproto, ObjectGuid castId, uint effMask, WorldObject owner, Unit caster, int[] baseAmount, Item castItem, ObjectGuid casterGUID, ObjectGuid castItemGuid, int castItemLevel)
        {
            Cypher.Assert(effMask != 0);
            Cypher.Assert(spellproto != null);
            Cypher.Assert(owner != null);
            Cypher.Assert(caster != null || !casterGUID.IsEmpty());
            Cypher.Assert(effMask <= SpellConst.MaxEffectMask);
            // try to get caster of aura
            if (!casterGUID.IsEmpty())
            {
                if (owner.GetGUID() == casterGUID)
                    caster = owner.ToUnit();
                else
                    caster = Global.ObjAccessor.GetUnit(owner, casterGUID);
            }
            else
                casterGUID = caster.GetGUID();

            // check if aura can be owned by owner
            if (owner.isTypeMask(TypeMask.Unit))
                if (!owner.IsInWorld || owner.ToUnit().IsDuringRemoveFromWorld())
                    // owner not in world so don't allow to own not self casted single target auras
                    if (casterGUID != owner.GetGUID() && spellproto.IsSingleTarget())
                        return null;

            Aura aura = null;
            switch (owner.GetTypeId())
            {
                case TypeId.Unit:
                case TypeId.Player:
                    aura = new UnitAura(spellproto, castId, effMask, owner, caster, baseAmount, castItem, casterGUID, castItemGuid, castItemLevel);
                    break;
                case TypeId.DynamicObject:
                    aura = new DynObjAura(spellproto, castId, effMask, owner, caster, baseAmount, castItem, casterGUID, castItemGuid, castItemLevel);
                    break;
                default:
                    Cypher.Assert(false);
                    return null;
            }
            // aura can be removed in Unit:_AddAura call
            if (aura.IsRemoved())
                return null;
            return aura;
        }

        #region Fields
        List<AuraScript> m_loadedScripts = new List<AuraScript>();
        SpellInfo m_spellInfo;
        ObjectGuid m_castGuid;
        ObjectGuid m_casterGuid;
        ObjectGuid m_castItemGuid;
        int m_castItemLevel;
        uint m_spellXSpellVisualId;
        long m_applyTime;
        WorldObject m_owner;

        int m_maxDuration;                                // Max aura duration
        int m_duration;                                   // Current time
        int m_timeCla;                                    // Timer for power per sec calcultion
        List<SpellPowerRecord> m_periodicCosts = new List<SpellPowerRecord>();// Periodic costs
        int m_updateTargetMapInterval;                    // Timer for UpdateTargetMapOfEffect

        uint m_casterLevel;                          // Aura level (store caster level for correct show level dep amount)
        byte m_procCharges;                                // Aura charges (0 for infinite)
        byte m_stackAmount;                                // Aura stack amount

        //might need to be arrays still
        SpellEffectInfo[] _spellEffectInfos;
        AuraEffect[] _effects;
        Dictionary<ObjectGuid, AuraApplication> m_applications = new Dictionary<ObjectGuid, AuraApplication>();

        bool m_isRemoved;
        bool m_isSingleTarget;                        // true if it's a single target spell and registered at caster - can change at spell steal for example
        bool m_isUsingCharges;

        ChargeDropEvent m_dropEvent;

        DateTime m_procCooldown;
        DateTime m_lastProcAttemptTime;
        DateTime m_lastProcSuccessTime;

        List<AuraApplication> m_removedApplications = new List<AuraApplication>();
        #endregion
    }

    public class UnitAura : Aura
    {
        public UnitAura(SpellInfo spellproto, ObjectGuid castId, uint effMask, WorldObject owner, Unit caster, int[] baseAmount, Item castItem, ObjectGuid casterGUID, ObjectGuid castItemGuid, int castItemLevel)
            : base(spellproto, castId, owner, caster, castItem, casterGUID, castItemGuid, castItemLevel)
        {
            m_AuraDRGroup = DiminishingGroup.None;
            LoadScripts();
            _InitEffects(effMask, caster, baseAmount);
            GetUnitOwner()._AddAura(this, caster);
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
        }

        public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
        {
            foreach (SpellEffectInfo effect in GetSpellEffectInfos())
            {
                if (effect == null || !HasEffect(effect.EffectIndex))
                    continue;

                List<Unit> targetList = new List<Unit>();
                // non-area aura
                if (effect.Effect == SpellEffectName.ApplyAura)
                {
                    targetList.Add(GetUnitOwner());
                }
                else
                {
                    float radius = effect.CalcRadius(caster);

                    if (!GetUnitOwner().HasUnitState(UnitState.Isolated))
                    {
                        switch (effect.Effect)
                        {
                            case SpellEffectName.ApplyAreaAuraParty:
                            case SpellEffectName.ApplyAreaAuraRaid:
                                {
                                    targetList.Add(GetUnitOwner());
                                    var u_check = new AnyGroupedUnitInObjectRangeCheck(GetUnitOwner(), GetUnitOwner(), radius, effect.Effect == SpellEffectName.ApplyAreaAuraRaid);
                                    var searcher = new UnitListSearcher(GetUnitOwner(), targetList, u_check);
                                    Cell.VisitAllObjects(GetUnitOwner(), searcher, radius);
                                    break;
                                }
                            case SpellEffectName.ApplyAreaAuraFriend:
                                {
                                    targetList.Add(GetUnitOwner());
                                    var u_check = new AnyFriendlyUnitInObjectRangeCheck(GetUnitOwner(), GetUnitOwner(), radius);
                                    var searcher = new UnitListSearcher(GetUnitOwner(), targetList, u_check);
                                    Cell.VisitAllObjects(GetUnitOwner(), searcher, radius);
                                    break;
                                }
                            case SpellEffectName.ApplyAreaAuraEnemy:
                                {
                                    var u_check = new AnyAoETargetUnitInObjectRangeCheck(GetUnitOwner(), GetUnitOwner(), radius);
                                    var searcher = new UnitListSearcher(GetUnitOwner(), targetList, u_check);
                                    Cell.VisitAllObjects(GetUnitOwner(), searcher, radius);
                                    break;
                                }
                            case SpellEffectName.ApplyAreaAuraPet:
                                targetList.Add(GetUnitOwner());
                                goto case SpellEffectName.ApplyAreaAuraOwner;
                            case SpellEffectName.ApplyAreaAuraOwner:
                                {
                                    Unit owner = GetUnitOwner().GetCharmerOrOwner();
                                    if (owner != null)
                                        if (GetUnitOwner().IsWithinDistInMap(owner, radius))
                                            targetList.Add(owner);
                                    break;
                                }
                        }
                    }
                }

                foreach (var unit in targetList)
                {
                    if (targets.ContainsKey(unit))
                        targets[unit] |= (byte)(1 << (int)effect.EffectIndex);
                    else
                        targets[unit] = (byte)(1 << (int)effect.EffectIndex);
                }
            }
        }

        // Allow Apply Aura Handler to modify and access m_AuraDRGroup
        public void SetDiminishGroup(DiminishingGroup group) { m_AuraDRGroup = group; }
        public DiminishingGroup GetDiminishGroup() { return m_AuraDRGroup; }

        DiminishingGroup m_AuraDRGroup;              // Diminishing
    }

    public class DynObjAura : Aura
    {
        public DynObjAura(SpellInfo spellproto, ObjectGuid castId, uint effMask, WorldObject owner, Unit caster, int[] baseAmount, Item castItem, ObjectGuid casterGUID, ObjectGuid castItemGuid, int castItemLevel)
            : base(spellproto, castId, owner, caster, castItem, casterGUID, castItemGuid, castItemLevel)
        {
            LoadScripts();
            Cypher.Assert(GetDynobjOwner() != null);
            Cypher.Assert(GetDynobjOwner().IsInWorld);
            Cypher.Assert(GetDynobjOwner().GetMap() == caster.GetMap());
            _InitEffects(effMask, caster, baseAmount);
            GetDynobjOwner().SetAura(this);

        }

        public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            if (IsRemoved())
                return;
            _Remove(removeMode);
        }

        public override void FillTargetMap(ref Dictionary<Unit, uint> targets, Unit caster)
        {
            Unit dynObjOwnerCaster = GetDynobjOwner().GetCaster();
            float radius = GetDynobjOwner().GetRadius();

            foreach (SpellEffectInfo effect in GetSpellEffectInfos())
            {
                if (effect == null || !HasEffect(effect.EffectIndex))
                    continue;

                List<Unit> targetList = new List<Unit>();
                if (effect.TargetB.GetTarget() == Targets.DestDynobjAlly || effect.TargetB.GetTarget() == Targets.UnitDestAreaAlly)
                {
                    var u_check = new AnyFriendlyUnitInObjectRangeCheck(GetDynobjOwner(), dynObjOwnerCaster, radius, GetSpellInfo().HasAttribute(SpellAttr3.OnlyTargetPlayers));
                    var searcher = new UnitListSearcher(GetDynobjOwner(), targetList, u_check);
                    Cell.VisitAllObjects(GetDynobjOwner(), searcher, radius);
                }
                else
                {
                    var u_check = new AnyAoETargetUnitInObjectRangeCheck(GetDynobjOwner(), dynObjOwnerCaster, radius);
                    var searcher = new UnitListSearcher(GetDynobjOwner(), targetList, u_check);
                    Cell.VisitAllObjects(GetDynobjOwner(), searcher, radius);
                }

                foreach (var unit in targetList)
                {
                    if (targets.ContainsKey(unit))
                        targets[unit] |= (byte)(1 << (int)effect.EffectIndex);
                    else
                        targets[unit] = (byte)(1 << (int)effect.EffectIndex);
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

            if ((object)first == null || (object)other == null)
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
        public int[] Amounts = new int[SpellConst.MaxEffects];
        public int[] BaseAmounts = new int[SpellConst.MaxEffects];
    }
}
