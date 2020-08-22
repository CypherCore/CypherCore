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
using Game.Entities;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Game.Combat
{
    public class ThreatManager
    {
        public ThreatManager(Unit owner)
        {
            currentVictim = null;
            Owner = owner;
            updateTimer = ThreatUpdateInternal;
            threatContainer = new ThreatContainer();
            threatOfflineContainer = new ThreatContainer();
        }

        const int ThreatUpdateInternal = 1 * Time.InMilliseconds;

        public void ForwardThreatForAssistingMe(Unit victim, float amount, SpellInfo spell, bool ignoreModifiers = false, bool ignoreRedirection = false)
        {
            GetOwner().GetHostileRefManager().ThreatAssist(victim, amount, spell);
        }

        public void AddThreat(Unit victim, float amount, SpellInfo spell, bool ignoreModifiers = false, bool ignoreRedirection = false)
        {
            if (!Owner.CanHaveThreatList() || Owner.HasUnitState(UnitState.Evade))
                return;

            Owner.SetInCombatWith(victim);
            victim.SetInCombatWith(Owner);
            AddThreat(victim, amount, spell != null ? spell.GetSchoolMask() : victim.GetMeleeDamageSchoolMask(), spell);
        }

        public void ClearAllThreat()
        {
            if (Owner.CanHaveThreatList(true) && !IsThreatListEmpty())
                Owner.SendClearThreatList();
            ClearReferences();
        }
        
        public void ClearReferences()
        {
            threatContainer.ClearReferences();
            threatOfflineContainer.ClearReferences();
            currentVictim = null;
            updateTimer = ThreatUpdateInternal;
        }

        public void AddThreat(Unit victim, float threat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal, SpellInfo threatSpell = null)
        {
            if (!IsValidProcess(victim, Owner, threatSpell))
                return;

            DoAddThreat(victim, CalcThreat(victim, Owner, threat, schoolMask, threatSpell));
        }

        public void DoAddThreat(Unit victim, float threat)
        {
            uint redirectThreadPct = victim.GetRedirectThreatPercent();
            Unit redirectTarget = victim.GetRedirectThreatTarget();

            // If victim is personnal spawn, redirect all aggro to summoner
            TempSummon tempSummonVictim = victim.ToTempSummon();
            if (tempSummonVictim)
            {
                if (tempSummonVictim.IsVisibleBySummonerOnly())
                {
                    // Personnal Spawns from same summoner can aggro each other
                    if (!GetOwner().ToTempSummon() ||
                        !GetOwner().ToTempSummon().IsVisibleBySummonerOnly() ||
                        tempSummonVictim.GetSummonerGUID() != GetOwner().ToTempSummon().GetSummonerGUID())
                    {
                        redirectThreadPct = 100;
                        redirectTarget = tempSummonVictim.GetSummoner();
                    }
                }
            }

            // must check > 0.0f, otherwise dead loop
            if (threat > 0.0f && redirectThreadPct != 0)
            {
                if (redirectTarget != null)
                {
                    float redirectThreat = MathFunctions.CalculatePct(threat, redirectThreadPct);
                    threat -= redirectThreat;
                    if (IsValidProcess(redirectTarget, GetOwner()))
                        AddThreat(redirectTarget, redirectThreat);
                }
            }

            AddThreat(victim, threat);
        }

        void AddThreat(Unit victim, float threat)
        {
            var reff = threatContainer.AddThreat(victim, threat);
            // Ref is not in the online refs, search the offline refs next
            if (reff == null)
                reff = threatOfflineContainer.AddThreat(victim, threat);

            if (reff == null) // there was no ref => create a new one
            {
                bool isFirst = threatContainer.Empty();

                // threat has to be 0 here
                var hostileRef = new HostileReference(victim, this, 0);
                threatContainer.AddReference(hostileRef);
                hostileRef.AddThreat(threat); // now we add the real threat
                if (victim.IsTypeId(TypeId.Player) && victim.ToPlayer().IsGameMaster())
                    hostileRef.SetOnlineOfflineState(false); // GM is always offline
                else if (isFirst)
                    SetCurrentVictim(hostileRef);
            }
        }

        public void ModifyThreatByPercent(Unit victim, int percent)
        {
            threatContainer.ModifyThreatByPercent(victim, percent);
        }

        public Unit GetHostilTarget()
        {
            threatContainer.Update();
            HostileReference nextVictim = threatContainer.SelectNextVictim(GetOwner().ToCreature(), getCurrentVictim());
            SetCurrentVictim(nextVictim);
            return GetCurrentVictim() != null ? GetCurrentVictim() : null;
        }

        public float GetThreat(Unit victim, bool alsoSearchOfflineList = false)
        {
            float threat = 0.0f;
            HostileReference refe = threatContainer.GetReferenceByTarget(victim);
            if (refe == null && alsoSearchOfflineList)
                refe = threatOfflineContainer.GetReferenceByTarget(victim);
            if (refe != null)
                threat = refe.GetThreat();
            return threat;
        }

        void TauntApply(Unit taunter)
        {
            HostileReference refe = threatContainer.GetReferenceByTarget(taunter);
            if (GetCurrentVictim() != null && refe != null && (refe.GetThreat() < getCurrentVictim().GetThreat()))
            {
                if (refe.GetTempThreatModifier() == 0.0f) // Ok, temp threat is unused
                    refe.SetTempThreat(getCurrentVictim().GetThreat());
            }
        }

        void TauntFadeOut(Unit taunter)
        {
            HostileReference refe = threatContainer.GetReferenceByTarget(taunter);
            if (refe != null)
                refe.ResetTempThreat();
        }

        public void SetCurrentVictim(HostileReference pHostileReference)
        {
            if (pHostileReference != null && pHostileReference != currentVictim)
            {
                Owner.SendChangeCurrentVictim(pHostileReference);
            }
            currentVictim = pHostileReference;
        }

        public void ProcessThreatEvent(ThreatRefStatusChangeEvent threatRefStatusChangeEvent)
        {
            threatRefStatusChangeEvent.SetThreatManager(this);     // now we can set the threat manager

            HostileReference hostilRef = threatRefStatusChangeEvent.GetReference();

            switch (threatRefStatusChangeEvent.GetEventType())
            {
                case UnitEventTypes.ThreatRefThreatChange:
                    if ((getCurrentVictim() == hostilRef && threatRefStatusChangeEvent.GetFValue() < 0.0f) ||
                        (getCurrentVictim() != hostilRef && threatRefStatusChangeEvent.GetFValue() > 0.0f))
                        SetDirty(true);                             // the order in the threat list might have changed
                    break;
                case UnitEventTypes.ThreatRefOnlineStatus:
                    if (!hostilRef.IsOnline())
                    {
                        if (hostilRef == getCurrentVictim())
                        {
                            SetCurrentVictim(null);
                            SetDirty(true);
                        }
                        Owner.SendRemoveFromThreatList(hostilRef);
                        threatContainer.Remove(hostilRef);
                        threatOfflineContainer.AddReference(hostilRef);
                    }
                    else
                    {
                        if (GetCurrentVictim() != null && hostilRef.GetThreat() > (1.1f * getCurrentVictim().GetThreat()))
                            SetDirty(true);
                        threatContainer.AddReference(hostilRef);
                        threatOfflineContainer.Remove(hostilRef);
                    }
                    break;
                case UnitEventTypes.ThreatRefRemoveFromList:
                    if (hostilRef == getCurrentVictim())
                    {
                        SetCurrentVictim(null);
                        SetDirty(true);
                    }
                    Owner.SendRemoveFromThreatList(hostilRef);
                    if (hostilRef.IsOnline())
                        threatContainer.Remove(hostilRef);
                    else
                        threatOfflineContainer.Remove(hostilRef);
                    break;
            }
        }

        public bool IsNeedUpdateToClient(uint time)
        {
            if (IsThreatListEmpty())
                return false;

            if (time >= updateTimer)
            {
                updateTimer = ThreatUpdateInternal;
                return true;
            }
            updateTimer -= time;
            return false;
        }

        // Reset all aggro without modifying the threatlist.
        void ResetAllAggro()
        {
            var threatList = threatContainer.threatList;
            if (threatList.Empty())
                return;

            foreach (var refe in threatList)
                refe.SetThreat(0);

            SetDirty(true);
        }
        public bool IsThreatListEmpty()
        {
            return threatContainer.Empty();
        }
        public bool IsThreatListsEmpty()
        {
            return threatContainer.Empty() && threatOfflineContainer.Empty();
        }

        public HostileReference getCurrentVictim() { return currentVictim; }
        
        public Unit GetOwner()
        {
            return Owner;
        }

        void SetDirty(bool isDirty)
        {
            threatContainer.SetDirty(isDirty);
        }

        public List<HostileReference> GetThreatList() { return threatContainer.GetThreatList(); }
        public List<HostileReference> GetOfflineThreatList() { return threatOfflineContainer.GetThreatList(); }
        public ThreatContainer GetOnlineContainer() { return threatContainer; }

        public Unit SelectVictim() { return GetHostilTarget(); }
        public Unit GetCurrentVictim() 
        {
            var refe = getCurrentVictim();
            if (refe != null)
                return refe.GetTarget();
            else 
                return null;
        }
        public bool IsThreatListEmpty(bool includeOffline = false) { return includeOffline ? IsThreatListsEmpty() : IsThreatListEmpty(); }
        public bool IsThreatenedBy(Unit who, bool includeOffline = false) { return FindReference(who, includeOffline) != null; }
        public int GetThreatListSize() { return threatContainer.threatList.Count; }
        public Unit GetAnyTarget()
        {
            var list = GetThreatList();
            if (!list.Empty())
                return list[0].GetTarget();

            return null;
        }
        public void ResetThreat(Unit who) 
        {
            var refe = FindReference(who, true);
            if (refe != null)
                refe.SetThreat(0.0f);

        }
        public void ResetAllThreat() { ResetAllAggro(); }

        HostileReference FindReference(Unit who, bool includeOffline)
        {
            var refe = threatContainer.GetReferenceByTarget(who);
            if (refe != null)
                return refe;

            if (includeOffline)
            {
                var offlineRefe = threatOfflineContainer.GetReferenceByTarget(who);
                if (offlineRefe != null)
                    return offlineRefe;
            }

            return null;
        }

    // The hatingUnit is not used yet
    public static float CalcThreat(Unit hatedUnit, Unit hatingUnit, float threat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal, SpellInfo threatSpell = null)
        {
            if (threatSpell != null)
            {
                var threatEntry = Global.SpellMgr.GetSpellThreatEntry(threatSpell.Id);
                if (threatEntry != null)
                    if (threatEntry.pctMod != 1.0f)
                        threat *= threatEntry.pctMod;

                // Energize is not affected by Mods
                foreach (SpellEffectInfo effect in threatSpell.GetEffects())
                    if (effect != null && (effect.Effect == SpellEffectName.Energize || effect.ApplyAuraName == AuraType.PeriodicEnergize))
                        return threat;

                Player modOwner = hatedUnit.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(threatSpell.Id, SpellModOp.Threat, ref threat);
            }

            return hatedUnit.ApplyTotalThreatModifier(threat, schoolMask);
        }

        public static bool IsValidProcess(Unit hatedUnit, Unit hatingUnit, SpellInfo threatSpell = null)
        {
            //function deals with adding threat and adding players and pets into ThreatList
            //mobs, NPCs, guards have ThreatList and HateOfflineList
            //players and pets have only InHateListOf
            //HateOfflineList is used co contain unattackable victims (in-flight, in-water, GM etc.)

            if (hatedUnit == null || hatingUnit == null)
                return false;

            // not to self
            if (hatedUnit == hatingUnit)
                return false;

            // not to GM
            if (hatedUnit.IsTypeId(TypeId.Player) && hatedUnit.ToPlayer().IsGameMaster())
                return false;

            // not to dead and not for dead
            if (!hatedUnit.IsAlive() || !hatingUnit.IsAlive())
                return false;

            // not in same map or phase
            if (!hatedUnit.IsInMap(hatingUnit) || !hatedUnit.IsInPhase(hatingUnit))
                return false;

            // spell not causing threat
            if (threatSpell != null && threatSpell.HasAttribute(SpellAttr1.NoThreat))
                return false;

            Cypher.Assert(hatingUnit.IsTypeId(TypeId.Unit));

            return true;
        }

        Unit Owner;
        HostileReference currentVictim;
        uint updateTimer;
        ThreatContainer threatContainer;
        ThreatContainer threatOfflineContainer;
    }

    public class ThreatContainer
    {
        public ThreatContainer()
        {
            threatList = new List<HostileReference>();
            iDirty = false;
        }

        public void ClearReferences()
        {
            foreach (var reff in threatList)
            {
                reff.Unlink();
            }

            threatList.Clear();
        }

        public HostileReference GetReferenceByTarget(Unit victim)
        {
            if (victim == null)
                return null;

            ObjectGuid guid = victim.GetGUID();
            foreach (var reff in threatList)
            {
                if (reff != null && reff.GetUnitGuid() == guid)
                    return reff;
            }

            return null;
        }

        public HostileReference AddThreat(Unit victim, float threat)
        {
            var reff = GetReferenceByTarget(victim);
            if (reff != null)
                reff.AddThreat(threat);
            return reff;
        }

        public void ModifyThreatByPercent(Unit victim, int percent)
        {
            HostileReference refe = GetReferenceByTarget(victim);
            if (refe != null)
                refe.AddThreatPercent(percent);
        }

        public void Update()
        {
            if (iDirty && threatList.Count > 1)
                threatList = threatList.OrderByDescending(p => p.GetThreat()).ToList();

            iDirty = false;
        }

        public HostileReference SelectNextVictim(Creature attacker, HostileReference currentVictim)
        {
            HostileReference currentRef = null;
            bool found = false;
            bool noPriorityTargetFound = false;

            for (var i = 0; i < threatList.Count; i++)
            {
                if (found)
                    break;

                currentRef = threatList[i];

                Unit target = currentRef.GetTarget();
                Cypher.Assert(target);                                     // if the ref has status online the target must be there !

                // some units are prefered in comparison to others
                if (!noPriorityTargetFound && (target.IsImmunedToDamage(attacker.GetMeleeDamageSchoolMask()) || target.HasNegativeAuraWithInterruptFlag(SpellAuraInterruptFlags.TakeDamage)))
                {
                    if (i != threatList.Count - 1)
                    {
                        // current victim is a second choice target, so don't compare threat with it below
                        if (currentRef == currentVictim)
                            currentVictim = null;
                        continue;
                    }
                    else
                    {
                        // if we reached to this point, everyone in the threatlist is a second choice target. In such a situation the target with the highest threat should be attacked.
                        noPriorityTargetFound = true;
                        i = 0;
                        continue;
                    }
                }

                if (attacker.CanCreatureAttack(target))           // skip non attackable currently targets
                {
                    if (currentVictim != null)                              // select 1.3/1.1 better target in comparison current target
                    {
                        // list sorted and and we check current target, then this is best case
                        if (currentVictim == currentRef || currentRef.GetThreat() <= 1.1f * currentVictim.GetThreat())
                        {
                            if (currentVictim != currentRef && attacker.CanCreatureAttack(currentVictim.GetTarget()))
                                currentRef = currentVictim;            // for second case, if currentvictim is attackable

                            found = true;
                            break;
                        }

                        if (currentRef.GetThreat() > 1.3f * currentVictim.GetThreat() ||
                            (currentRef.GetThreat() > 1.1f * currentVictim.GetThreat() &&
                            attacker.IsWithinMeleeRange(target)))
                        {                                           //implement 110% threat rule for targets in melee range
                            found = true;                           //and 130% rule for targets in ranged distances
                            break;                                  //for selecting alive targets
                        }
                    }
                    else                                            // select any
                    {
                        found = true;
                        break;
                    }
                }
            }
            if (!found)
                currentRef = null;

            return currentRef;
        }

        public void SetDirty(bool isDirty)
        {
            iDirty = isDirty;
        }

        bool IsDirty()
        {
            return iDirty;
        }

        public bool Empty()
        {
            return threatList.Empty();
        }
        public HostileReference GetMostHated()
        {
            return threatList.Count == 0 ? null : threatList[0];
        }

        public void Remove(HostileReference hostileRef)
        {
            threatList.Remove(hostileRef);
        }
        public void AddReference(HostileReference hostileRef)
        {
            threatList.Add(hostileRef);
        }

        public List<HostileReference> GetThreatList() { return threatList; }

        public List<HostileReference> threatList { get; set; }
        bool iDirty;
    }
}
