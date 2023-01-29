// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Game.Entities;

namespace Game.Combat
{
    public class ThreatReference : IComparable<ThreatReference>
    {
        private readonly Creature _owner;
        private readonly Unit _victim;
        private float _baseAmount;
        private TauntState _taunted;

        public ThreatReference(ThreatManager mgr, Unit victim)
        {
            _owner = mgr.Owner as Creature;
            Mgr = mgr;
            _victim = victim;
            Online = OnlineState.Offline;
        }

        public ThreatManager Mgr { get; set; }
        public OnlineState Online { get; set; }
        public int TempModifier { get; set; } // Temporary effects (Auras with SPELL_AURA_MOD_TOTAL_THREAT) - set from victim's threatmanager in ThreatManager::UpdateMyTempModifiers

        public int CompareTo(ThreatReference other)
        {
            return ThreatManager.CompareReferencesLT(this, other, 1.0f) ? 1 : -1;
        }

        public void AddThreat(float amount)
        {
            if (amount == 0.0f)
                return;

            _baseAmount = Math.Max(_baseAmount + amount, 0.0f);
            ListNotifyChanged();
            Mgr.NeedClientUpdate = true;
        }

        public void ScaleThreat(float factor)
        {
            if (factor == 1.0f)
                return;

            _baseAmount *= factor;
            ListNotifyChanged();
            Mgr.NeedClientUpdate = true;
        }

        public void UpdateOffline()
        {
            bool shouldBeOffline = ShouldBeOffline();

            if (shouldBeOffline == IsOffline())
                return;

            if (shouldBeOffline)
            {
                Online = OnlineState.Offline;
                ListNotifyChanged();
                Mgr.SendRemoveToClients(_victim);
            }
            else
            {
                Online = ShouldBeSuppressed() ? OnlineState.Suppressed : OnlineState.Online;
                ListNotifyChanged();
                Mgr.RegisterForAIUpdate(this);
            }
        }

        public static bool FlagsAllowFighting(Unit a, Unit b)
        {
            if (a.IsCreature() &&
                a.ToCreature().IsTrigger())
                return false;

            if (a.HasUnitFlag(UnitFlags.PlayerControlled))
            {
                if (b.HasUnitFlag(UnitFlags.ImmuneToPc))
                    return false;
            }
            else
            {
                if (b.HasUnitFlag(UnitFlags.ImmuneToNpc))
                    return false;
            }

            return true;
        }

        public bool ShouldBeOffline()
        {
            if (!_owner.CanSeeOrDetect(_victim))
                return true;

            if (!_owner._IsTargetAcceptable(_victim) ||
                !_owner.CanCreatureAttack(_victim))
                return true;

            if (!FlagsAllowFighting(_owner, _victim) ||
                !FlagsAllowFighting(_victim, _owner))
                return true;

            return false;
        }

        public bool ShouldBeSuppressed()
        {
            if (IsTaunting()) // a taunting victim can never be suppressed
                return false;

            if (_victim.IsImmunedToDamage(_owner.GetMeleeDamageSchoolMask()))
                return true;

            if (_victim.HasAuraType(AuraType.ModConfuse))
                return true;

            if (_victim.HasBreakableByDamageAuraType(AuraType.ModStun))
                return true;

            return false;
        }

        public void UpdateTauntState(TauntState state = TauntState.None)
        {
            // Check for SPELL_AURA_MOD_DETAUNT (applied from owner to victim)
            if (state < TauntState.Taunt &&
                _victim.HasAuraTypeWithCaster(AuraType.ModDetaunt, _owner.GetGUID()))
                state = TauntState.Detaunt;

            if (state == _taunted)
                return;

            Extensions.Swap(ref state, ref _taunted);

            ListNotifyChanged();
            Mgr.NeedClientUpdate = true;
        }

        public void ClearThreat()
        {
            Mgr.ClearThreat(this);
        }

        public void UnregisterAndFree()
        {
            _owner.GetThreatManager().PurgeThreatListRef(_victim.GetGUID());
            _victim.GetThreatManager().PurgeThreatenedByMeRef(_owner.GetGUID());
        }

        public Creature GetOwner()
        {
            return _owner;
        }

        public Unit GetVictim()
        {
            return _victim;
        }

        public float GetThreat()
        {
            return Math.Max(_baseAmount + (float)TempModifier, 0.0f);
        }

        public OnlineState GetOnlineState()
        {
            return Online;
        }

        public bool IsOnline()
        {
            return Online >= OnlineState.Online;
        }

        public bool IsAvailable()
        {
            return Online > OnlineState.Offline;
        }

        public bool IsSuppressed()
        {
            return Online == OnlineState.Suppressed;
        }

        public bool IsOffline()
        {
            return Online <= OnlineState.Offline;
        }

        public TauntState GetTauntState()
        {
            return IsTaunting() ? TauntState.Taunt : _taunted;
        }

        public bool IsTaunting()
        {
            return _taunted >= TauntState.Taunt;
        }

        public bool IsDetaunted()
        {
            return _taunted == TauntState.Detaunt;
        }

        public void SetThreat(float amount)
        {
            _baseAmount = amount;
            ListNotifyChanged();
        }

        public void ModifyThreatByPercent(int percent)
        {
            if (percent != 0)
                ScaleThreat(0.01f * (100f + percent));
        }

        public void ListNotifyChanged()
        {
            Mgr.ListNotifyChanged();
        }
    }
}