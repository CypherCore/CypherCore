// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Combat
{
    public class CombatManager
    {
        Unit _owner;
        Dictionary<ObjectGuid, CombatReference> _pveRefs = new();
        Dictionary<ObjectGuid, PvPCombatReference> _pvpRefs = new();

        public CombatManager(Unit owner)
        {
            _owner = owner;
        }

        public static bool CanBeginCombat(Unit a, Unit b)
        {
            // Checks combat validity before initial reference creation.
            // For the combat to be valid...
            // ...the two units need to be different
            if (a == b)
                return false;
            // ...the two units need to be in the world
            if (!a.IsInWorld || !b.IsInWorld)
                return false;
            // ...the two units need to both be alive
            if (!a.IsAlive() || !b.IsAlive())
                return false;
            // ...the two units need to be on the same map
            if (a.GetMap() != b.GetMap())
                return false;
            // ...the two units need to be in the same phase
            if (!WorldObject.InSamePhase(a, b))
                return false;
            if (a.HasUnitState(UnitState.Evade) || b.HasUnitState(UnitState.Evade))
                return false;
            if (a.HasUnitState(UnitState.InFlight) || b.HasUnitState(UnitState.InFlight))
                return false;
            // ... both units must be allowed to enter combat
            if (a.IsCombatDisallowed() || b.IsCombatDisallowed())
                return false;
            if (a.IsFriendlyTo(b) || b.IsFriendlyTo(a))
                return false;
            Player playerA = a.GetCharmerOrOwnerPlayerOrPlayerItself();
            Player playerB = b.GetCharmerOrOwnerPlayerOrPlayerItself();
            // ...neither of the two units must be (owned by) a player with .gm on
            if ((playerA != null && playerA.IsGameMaster()) || (playerB != null && playerB.IsGameMaster()))
                return false;
            return true;
        }

        public void Update(uint tdiff)
        {
            foreach (var pair in _pvpRefs.ToList())
            {
                PvPCombatReference refe = pair.Value;
                if (refe.first == _owner && !refe.Update(tdiff)) // only update if we're the first unit involved (otherwise double decrement)
                {
                    _pvpRefs.Remove(pair.Key);
                    refe.EndCombat(); // this will remove it from the other side
                }
            }
        }

        public bool HasPvECombat()
        {
            foreach (var (_, refe) in _pveRefs)
                if (!refe.IsSuppressedFor(_owner))
                    return true;
            return false;
        }

        public bool HasPvECombatWithPlayers()
        {
            foreach (var reference in _pveRefs)
                if (!reference.Value.IsSuppressedFor(_owner) && reference.Value.GetOther(_owner).IsPlayer())
                    return true;

            return false;
        }

        public bool HasPvPCombat()
        {
            foreach (var pair in _pvpRefs)
                if (!pair.Value.IsSuppressedFor(_owner))
                    return true;

            return false;
        }

        public Unit GetAnyTarget()
        {
            foreach (var pair in _pveRefs)
                if (!pair.Value.IsSuppressedFor(_owner))
                    return pair.Value.GetOther(_owner);

            foreach (var pair in _pvpRefs)
                if (!pair.Value.IsSuppressedFor(_owner))
                    return pair.Value.GetOther(_owner);

            return null;
        }

        public bool SetInCombatWith(Unit who, bool addSecondUnitSuppressed = false)
        {
            // Are we already in combat? If yes, refresh pvp combat
            var existingPvpRef = _pvpRefs.LookupByKey(who.GetGUID());
            if (existingPvpRef != null)
            {
                existingPvpRef.RefreshTimer();
                existingPvpRef.Refresh();
                return true;
            }

            var existingPveRef = _pveRefs.LookupByKey(who.GetGUID());
            if (existingPveRef != null)
            {
                existingPveRef.Refresh();
                return true;
            }

            // Otherwise, check validity...
            if (!CanBeginCombat(_owner, who))
                return false;

            // ...then create new reference
            CombatReference refe;
            if (_owner.IsControlledByPlayer() && who.IsControlledByPlayer())
                refe = new PvPCombatReference(_owner, who);
            else
                refe = new CombatReference(_owner, who);

            if (addSecondUnitSuppressed)
                refe.Suppress(who);

            // ...and insert it into both managers
            PutReference(who.GetGUID(), refe);
            who.GetCombatManager().PutReference(_owner.GetGUID(), refe);

            // now, sequencing is important - first we update the combat state, which will set both units in combat and do non-AI combat start stuff
            bool needSelfAI = UpdateOwnerCombatState();
            bool needOtherAI = who.GetCombatManager().UpdateOwnerCombatState();

            // then, we finally notify the AI (if necessary) and let it safely do whatever it feels like
            if (needSelfAI)
                NotifyAICombat(_owner, who);
            if (needOtherAI)
                NotifyAICombat(who, _owner);

            return IsInCombatWith(who);
        }

        public bool IsInCombatWith(ObjectGuid guid)
        {
            return _pveRefs.ContainsKey(guid) || _pvpRefs.ContainsKey(guid);
        }

        public bool IsInCombatWith(Unit who)
        {
            return IsInCombatWith(who.GetGUID());
        }

        public void InheritCombatStatesFrom(Unit who)
        {
            CombatManager mgr = who.GetCombatManager();
            foreach (var refe in mgr._pveRefs)
            {
                if (!IsInCombatWith(refe.Key))
                {
                    Unit target = refe.Value.GetOther(who);
                    if ((_owner.IsImmuneToPC() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
                        (_owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
                        continue;
                    SetInCombatWith(target);
                }
            }
            foreach (var refe in mgr._pvpRefs)
            {
                Unit target = refe.Value.GetOther(who);
                if ((_owner.IsImmuneToPC() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
                    (_owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
                    continue;
                SetInCombatWith(target);
            }
        }

        public void EndCombatBeyondRange(float range, bool includingPvP)
        {
            foreach (var pair in _pveRefs.ToList())
            {
                CombatReference refe = pair.Value;
                if (!refe.first.IsWithinDistInMap(refe.second, range))
                {
                    _pveRefs.Remove(pair.Key);
                    refe.EndCombat();
                }
            }

            if (!includingPvP)
                return;

            foreach (var pair in _pvpRefs.ToList())
            {
                CombatReference refe = pair.Value;
                if (!refe.first.IsWithinDistInMap(refe.second, range))
                {
                    _pvpRefs.Remove(pair.Key);
                    refe.EndCombat();
                }
            }
        }

        public void SuppressPvPCombat(Func<Unit, bool> unitFilter = null)
        {
            foreach (var (_, combatRef) in _pvpRefs)
                if (unitFilter == null || unitFilter(combatRef.GetOther(_owner)))
                    combatRef.Suppress(_owner);

            if (UpdateOwnerCombatState())
            {
                UnitAI ownerAI = _owner.GetAI();
                if (ownerAI != null)
                    ownerAI.JustExitedCombat();
            }
        }

        public void EndAllPvECombat(Func<Unit, bool> unitFilter = null)
        {
            // cannot have threat without combat
            _owner.GetThreatManager().RemoveMeFromThreatLists(unitFilter);
            _owner.GetThreatManager().ClearAllThreat();

            List<CombatReference> combatReferencesToRemove = new();
            foreach (var (_, combatRef) in _pveRefs)
                if (unitFilter == null || unitFilter(combatRef.GetOther(_owner)))
                    combatReferencesToRemove.Add(combatRef);

            foreach (CombatReference combatRef in combatReferencesToRemove)
                combatRef.EndCombat();
        }

        public void RevalidateCombat()
        {
            foreach (var (guid, refe) in _pveRefs.ToList())
            {
                if (!CanBeginCombat(_owner, refe.GetOther(_owner)))
                {
                    _pveRefs.Remove(guid); // erase manually here to avoid iterator invalidation
                    refe.EndCombat();
                }
            }

            foreach (var (guid, refe) in _pvpRefs.ToList())
            {
                if (!CanBeginCombat(_owner, refe.GetOther(_owner)))
                {
                    _pvpRefs.Remove(guid); // erase manually here to avoid iterator invalidation
                    refe.EndCombat();
                }
            }
        }

        public void EndAllPvPCombat(Func<Unit, bool> unitFilter = null)
        {
            List<CombatReference> combatReferencesToRemove = new();
            foreach (var (_, combatRef) in _pvpRefs)
                if (unitFilter == null || unitFilter(combatRef.GetOther(_owner)))
                    combatReferencesToRemove.Add(combatRef);

            foreach (CombatReference combatRef in combatReferencesToRemove)
                combatRef.EndCombat();
        }

        void EndAllCombat(Func<Unit, bool> unitFilter = null)
        {
            EndAllPvECombat(unitFilter);
            EndAllPvPCombat(unitFilter);
        }

        public static void NotifyAICombat(Unit me, Unit other)
        {
            UnitAI ai = me.GetAI();
            if (ai != null)
                ai.JustEnteredCombat(other);
        }

        void PutReference(ObjectGuid guid, CombatReference refe)
        {
            if (refe._isPvP)
            {
                Cypher.Assert(!_pvpRefs.ContainsKey(guid), "Duplicate combat state detected!");
                _pvpRefs[guid] = (PvPCombatReference)refe;
            }
            else
            {
                Cypher.Assert(!_pveRefs.ContainsKey(guid), "Duplicate combat state detected!");
                _pveRefs[guid] = refe;
            }
        }

        public void PurgeReference(ObjectGuid guid, bool pvp)
        {
            if (pvp)
                _pvpRefs.Remove(guid);
            else
                _pveRefs.Remove(guid);
        }

        public bool UpdateOwnerCombatState()
        {
            bool combatState = HasCombat();
            if (combatState == _owner.IsInCombat())
                return false;

            if (combatState)
            {
                _owner.SetUnitFlag(UnitFlags.InCombat);
                _owner.AtEnterCombat();
                if (!_owner.IsCreature())
                    _owner.AtEngage(GetAnyTarget());
            }
            else
            {
                _owner.RemoveUnitFlag(UnitFlags.InCombat);
                _owner.AtExitCombat();
                if (!_owner.IsCreature())
                    _owner.AtDisengage();
            }

            Unit master = _owner.GetCharmerOrOwner();
            if (master != null)
                master.UpdatePetCombatState();

            return true;
        }

        public Unit GetOwner() { return _owner; }

        public bool HasCombat() { return HasPvECombat() || HasPvPCombat(); }

        public Dictionary<ObjectGuid, CombatReference> GetPvECombatRefs() { return _pveRefs; }

        public Dictionary<ObjectGuid, PvPCombatReference> GetPvPCombatRefs() { return _pvpRefs; }

        public void EndAllCombat()
        {
            EndAllPvECombat();
            EndAllPvPCombat();
        }
    }

    public class CombatReference
    {
        public Unit first;
        public Unit second;
        public bool _isPvP;

        bool _suppressFirst;
        bool _suppressSecond;

        public CombatReference(Unit a, Unit b, bool pvp = false)
        {
            first = a;
            second = b;
            _isPvP = pvp;
        }

        public void EndCombat()
        {
            // sequencing matters here - AI might do nasty stuff, so make sure refs are in a consistent state before you hand off!

            // first, get rid of any threat that still exists...
            first.GetThreatManager().ClearThreat(second);
            second.GetThreatManager().ClearThreat(first);

            // ...then, remove the references from both managers...
            first.GetCombatManager().PurgeReference(second.GetGUID(), _isPvP);
            second.GetCombatManager().PurgeReference(first.GetGUID(), _isPvP);

            // ...update the combat state, which will potentially remove IN_COMBAT...
            bool needFirstAI = first.GetCombatManager().UpdateOwnerCombatState();
            bool needSecondAI = second.GetCombatManager().UpdateOwnerCombatState();

            // ...and if that happened, also notify the AI of it...
            if (needFirstAI)
            {
                UnitAI firstAI = first.GetAI();
                if (firstAI != null)
                    firstAI.JustExitedCombat();
            }
            if (needSecondAI)
            {
                UnitAI secondAI = second.GetAI();
                if (secondAI != null)
                    secondAI.JustExitedCombat();
            }
        }

        public void Refresh()
        {
            bool needFirstAI = false, needSecondAI = false;
            if (_suppressFirst)
            {
                _suppressFirst = false;
                needFirstAI = first.GetCombatManager().UpdateOwnerCombatState();
            }
            if (_suppressSecond)
            {
                _suppressSecond = false;
                needSecondAI = second.GetCombatManager().UpdateOwnerCombatState();
            }

            if (needFirstAI)
                CombatManager.NotifyAICombat(first, second);
            if (needSecondAI)
                CombatManager.NotifyAICombat(second, first);
        }

        public void SuppressFor(Unit who)
        {
            Suppress(who);
            if (who.GetCombatManager().UpdateOwnerCombatState())
            {
                UnitAI ai = who.GetAI();
                if (ai != null)
                    ai.JustExitedCombat();
            }
        }

        // suppressed combat refs do not generate a combat state for one side of the relation
        // (used by: vanish, feign death)
        public bool IsSuppressedFor(Unit who) { return (who == first) ? _suppressFirst : _suppressSecond; }

        public void Suppress(Unit who)
        {
            if (who == first)
                _suppressFirst = true;
            else
                _suppressSecond = true;
        }

        public Unit GetOther(Unit me) { return (first == me) ? second : first; }
    }

    public class PvPCombatReference : CombatReference
    {
        public static uint PVP_COMBAT_TIMEOUT = 5 * Time.InMilliseconds;

        uint _combatTimer = PVP_COMBAT_TIMEOUT;


        public PvPCombatReference(Unit first, Unit second) : base(first, second, true) { }

        public bool Update(uint tdiff)
        {
            if (_combatTimer <= tdiff)
                return false;
            _combatTimer -= tdiff;
            return true;
        }

        public void RefreshTimer()
        {
            _combatTimer = PVP_COMBAT_TIMEOUT;
        }
    }
}
