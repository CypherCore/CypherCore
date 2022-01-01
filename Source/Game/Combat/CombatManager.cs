/*
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
using Game.AI;
using Game.Entities;
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
            if (a.IsFriendlyTo(b) || b.IsFriendlyTo(a))
                return false;
            Player playerA = a.GetCharmerOrOwnerPlayerOrPlayerItself();
            Player playerB = b.GetCharmerOrOwnerPlayerOrPlayerItself();
            // ...neither of the two units must be (owned by) a player with .gm on
            if ((playerA && playerA.IsGameMaster()) || (playerB && playerB.IsGameMaster()))
                return false;
            return true;
        }

        public void Update(uint tdiff)
        {
            foreach(var pair in _pvpRefs.ToList())
            {
                PvPCombatReference  refe = pair.Value;
                if (refe.first == _owner && !refe.Update(tdiff)) // only update if we're the first unit involved (otherwise double decrement)
                {
                    _pvpRefs.Remove(pair.Key);
                    refe.EndCombat(); // this will remove it from the other side
                }
            }
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
            if (!_pveRefs.Empty())
                return _pveRefs.First().Value.GetOther(_owner);

            foreach (var pair in _pvpRefs)
                if (!pair.Value.IsSuppressedFor(_owner))
                    return pair.Value.GetOther(_owner);

            return null;
        }

        public bool SetInCombatWith(Unit who)
        {
            // Are we already in combat? If yes, refresh pvp combat
            var pvpRefe = _pvpRefs.LookupByKey(who.GetGUID());
            if (pvpRefe != null)
            {
                pvpRefe.Refresh();
                return true;
            }
            else if (_pveRefs.ContainsKey(who.GetGUID()))
                return true;

            // Otherwise, check validity...
            if (!CombatManager.CanBeginCombat(_owner, who))
                return false;

            // ...then create new reference
            CombatReference refe;
            if (_owner.IsControlledByPlayer() && who.IsControlledByPlayer())
                refe = new PvPCombatReference(_owner, who);
            else
                refe = new CombatReference(_owner, who);

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
                if (!IsInCombatWith(refe.Key))
                {
                    Unit target = refe.Value.GetOther(who);
                    if ((_owner.IsImmuneToPC() && target.HasUnitFlag(UnitFlags.PlayerControlled)) ||
                        (_owner.IsImmuneToNPC() && !target.HasUnitFlag(UnitFlags.PlayerControlled)))
                        continue;
                    SetInCombatWith(target);
                }
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

        public void SuppressPvPCombat()
        {
            foreach (var pair in _pvpRefs)
                pair.Value.Suppress(_owner);

            if (UpdateOwnerCombatState())
            {
                UnitAI ownerAI = _owner.GetAI();
                if (ownerAI != null)
                    ownerAI.JustExitedCombat();
            }
        }

        public void EndAllPvECombat()
        {
            // cannot have threat without combat
            _owner.GetThreatManager().RemoveMeFromThreatLists();
            _owner.GetThreatManager().ClearAllThreat();
            while (!_pveRefs.Empty())
                _pveRefs.First().Value.EndCombat();
        }

        void EndAllPvPCombat()
        {
            while (!_pvpRefs.Empty())
                _pvpRefs.First().Value.EndCombat();
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
                _owner.AddUnitFlag(UnitFlags.InCombat);
                _owner.AtEnterCombat();
                if (_owner.IsCreature())
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


        Unit GetOwner() { return _owner; }

        public bool HasCombat() { return HasPvECombat() || HasPvPCombat(); }

        public bool HasPvECombat() { return !_pveRefs.Empty(); }

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

        public Unit GetOther(Unit me) { return (first == me) ? second : first; }
    }

    public class PvPCombatReference : CombatReference
    {
        public static uint PVP_COMBAT_TIMEOUT = 5 * Time.InMilliseconds;

        uint _combatTimer = PVP_COMBAT_TIMEOUT;
        bool _suppressFirst = false;
        bool _suppressSecond = false;

        public PvPCombatReference(Unit first, Unit second) : base(first, second, true) { }

        public bool Update(uint tdiff)
        {
            if (_combatTimer <= tdiff)
                return false;
            _combatTimer -= tdiff;
            return true;
        }

        public void Refresh()
        {
            _combatTimer = PVP_COMBAT_TIMEOUT;

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
    }
}
