// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Spells
{
    public class AuraApplication
    {
        private readonly Aura _base;
        private readonly byte _slot;                 // Aura Slot on unit
        private readonly Unit _target;
        private uint _effectMask;
        private uint _effectsToApply; // Used only at spell hit to determine which effect should be applied
        private AuraFlags _flags;     // Aura info flag
        private bool _needClientUpdate;
        private AuraRemoveMode _removeMode; // Store info for know remove aura reason

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

            // Try find Slot for aura
            byte slot = 0;

            // Lookup for Auras already applied from spell
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
                Log.outDebug(LogFilter.Spells, "Aura: {0} Effect: {1} put to unit visible Auras Slot: {2}", GetBase().GetId(), GetEffectMask(), slot);
            }
            else
            {
                Log.outError(LogFilter.Spells, "Aura: {0} Effect: {1} could not find empty unit visible Slot", GetBase().GetId(), GetEffectMask());
            }


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

        public void UpdateApplyEffectMask(uint newEffMask, bool canHandleNewEffects)
        {
            if (_effectsToApply == newEffMask)
                return;

            uint removeEffMask = (_effectsToApply ^ newEffMask) & (~newEffMask);
            uint addEffMask = (_effectsToApply ^ newEffMask) & (~_effectsToApply);

            // quick check, removes application completely
            if (removeEffMask == _effectsToApply &&
                addEffMask == 0)
            {
                _target._UnapplyAura(this, AuraRemoveMode.Default);

                return;
            }

            // update real effects only if they were applied already
            for (uint i = 0; i < SpellConst.MaxEffects; ++i)
                if (HasEffect(i) &&
                    (removeEffMask & (1 << (int)i)) != 0)
                    _HandleEffect(i, false);

            _effectsToApply = newEffMask;

            if (canHandleNewEffects)
                for (uint i = 0; i < SpellConst.MaxEffects; ++i)
                    if ((addEffMask & (1 << (int)i)) != 0)
                        _HandleEffect(i, true);
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

            auraInfo.AuraData = new AuraDataInfo();

            Aura aura = GetBase();

            AuraDataInfo auraData = auraInfo.AuraData;
            auraData.CastID = aura.GetCastId();
            auraData.SpellID = (int)aura.GetId();
            auraData.Visual = aura.GetSpellVisual();
            auraData.Flags = GetFlags();

            if (aura.GetAuraType() != AuraObjectType.DynObj &&
                aura.GetMaxDuration() > 0 &&
                !aura.GetSpellInfo().HasAttribute(SpellAttr5.DoNotDisplayDuration))
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
                auraData.CastUnit = ObjectGuid.Empty; // optional _data is filled in, but cast unit contains empty Guid in packet
            else if (!auraData.Flags.HasFlag(AuraFlags.NoCaster))
                auraData.CastUnit = aura.GetCasterGUID();

            if (auraData.Flags.HasFlag(AuraFlags.Duration))
            {
                auraData.Duration = aura.GetMaxDuration();
                auraData.Remaining = aura.GetDuration();
            }

            if (auraData.Flags.HasFlag(AuraFlags.Scalable))
            {
                bool hasEstimatedAmounts = false;

                foreach (AuraEffect effect in GetBase().GetAuraEffects())
                    if (effect != null &&
                        HasEffect(effect.GetEffIndex())) // Not all of aura's effects have to be applied on every Target
                    {
                        auraData.Points.Add(effect.GetAmount());

                        if (effect.GetEstimatedAmount().HasValue)
                            hasEstimatedAmounts = true;
                    }

                if (hasEstimatedAmounts)
                    // When sending EstimatedPoints all effects (at least up to the last one that uses GetEstimatedAmount) must have proper value in packet
                    foreach (AuraEffect effect in GetBase().GetAuraEffects())
                        if (effect != null &&
                            HasEffect(effect.GetEffIndex())) // Not all of aura's effects have to be applied on every Target
                            auraData.EstimatedPoints.Add(effect.GetEstimatedAmount().GetValueOrDefault(effect.GetAmount()));
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

        public Unit GetTarget()
        {
            return _target;
        }

        public Aura GetBase()
        {
            return _base;
        }

        public byte GetSlot()
        {
            return _slot;
        }

        public AuraFlags GetFlags()
        {
            return _flags;
        }

        public uint GetEffectMask()
        {
            return _effectMask;
        }

        public bool HasEffect(uint effect)
        {
            Cypher.Assert(effect < SpellConst.MaxEffects);

            return Convert.ToBoolean(_effectMask & (1 << (int)effect));
        }

        public bool IsPositive()
        {
            return _flags.HasAnyFlag(AuraFlags.Positive);
        }

        public uint GetEffectsToApply()
        {
            return _effectsToApply;
        }

        public void SetRemoveMode(AuraRemoveMode mode)
        {
            _removeMode = mode;
        }

        public AuraRemoveMode GetRemoveMode()
        {
            return _removeMode;
        }

        public bool HasRemoveMode()
        {
            return _removeMode != 0;
        }

        public bool IsNeedClientUpdate()
        {
            return _needClientUpdate;
        }

        private void _InitFlags(Unit caster, uint effMask)
        {
            // mark as selfcasted if needed
            _flags |= (GetBase().GetCasterGUID() == GetTarget().GetGUID()) ? AuraFlags.NoCaster : AuraFlags.None;

            // aura is casted by self or an enemy
            // one negative effect and we know aura is negative
            if (IsSelfcasted() ||
                caster == null ||
                !caster.IsFriendlyTo(GetTarget()))
            {
                bool negativeFound = false;

                foreach (var spellEffectInfo in GetBase().GetSpellInfo().GetEffects())
                    if (((1 << (int)spellEffectInfo.EffectIndex) & effMask) != 0 &&
                        !GetBase().GetSpellInfo().IsPositiveEffect(spellEffectInfo.EffectIndex))
                    {
                        negativeFound = true;

                        break;
                    }

                _flags |= negativeFound ? AuraFlags.Negative : AuraFlags.Positive;
            }
            // aura is casted by friend
            // one positive effect and we know aura is positive
            else
            {
                bool positiveFound = false;

                foreach (var spellEffectInfo in GetBase().GetSpellInfo().GetEffects())
                    if (((1 << (int)spellEffectInfo.EffectIndex) & effMask) != 0 &&
                        GetBase().GetSpellInfo().IsPositiveEffect(spellEffectInfo.EffectIndex))
                    {
                        positiveFound = true;

                        break;
                    }

                _flags |= positiveFound ? AuraFlags.Positive : AuraFlags.Negative;
            }

            bool effectNeedsAmount(AuraEffect effect)
            {
                return effect != null && (GetEffectsToApply() & (1 << (int)effect.GetEffIndex())) != 0 && Aura.EffectTypeNeedsSendingAmount(effect.GetAuraType());
            }

            if (GetBase().GetSpellInfo().HasAttribute(SpellAttr8.AuraSendAmount) ||
                GetBase().GetAuraEffects().Any(effectNeedsAmount))
                _flags |= AuraFlags.Scalable;
        }

        private bool IsSelfcasted()
        {
            return _flags.HasAnyFlag(AuraFlags.NoCaster);
        }
    }
}