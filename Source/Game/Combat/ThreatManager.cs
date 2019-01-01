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

        public void clearReferences()
        {
            threatContainer.clearReferences();
            threatOfflineContainer.clearReferences();
            currentVictim = null;
            updateTimer = ThreatUpdateInternal;
        }

        public void addThreat(Unit victim, float threat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal, SpellInfo threatSpell = null)
        {
            if (!isValidProcess(victim, Owner, threatSpell))
                return;

            doAddThreat(victim, calcThreat(victim, Owner, threat, schoolMask, threatSpell));
        }

        public void doAddThreat(Unit victim, float threat)
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
                    _addThreat(redirectTarget, redirectThreat);
                }
            }

            _addThreat(victim, threat);
        }

        void _addThreat(Unit victim, float threat)
        {
            var reff = threatContainer.addThreat(victim, threat);
            // Ref is not in the online refs, search the offline refs next
            if (reff == null)
                reff = threatOfflineContainer.addThreat(victim, threat);

            if (reff == null) // there was no ref => create a new one
            {
                bool isFirst = threatContainer.empty();

                // threat has to be 0 here
                var hostileRef = new HostileReference(victim, this, 0);
                threatContainer.addReference(hostileRef);
                hostileRef.addThreat(threat); // now we add the real threat
                if (victim.IsTypeId(TypeId.Player) && victim.ToPlayer().IsGameMaster())
                    hostileRef.setOnlineOfflineState(false); // GM is always offline
                else if (isFirst)
                    setCurrentVictim(hostileRef);
            }
        }

        public void modifyThreatPercent(Unit victim, int percent)
        {
            threatContainer.modifyThreatPercent(victim, percent);
        }

        public Unit getHostilTarget()
        {
            threatContainer.update();
            HostileReference nextVictim = threatContainer.selectNextVictim(GetOwner().ToCreature(), getCurrentVictim());
            setCurrentVictim(nextVictim);
            return getCurrentVictim() != null ? getCurrentVictim().getTarget() : null;
        }

        public float getThreat(Unit victim, bool alsoSearchOfflineList = false)
        {
            float threat = 0.0f;
            HostileReference refe = threatContainer.getReferenceByTarget(victim);
            if (refe == null && alsoSearchOfflineList)
                refe = threatOfflineContainer.getReferenceByTarget(victim);
            if (refe != null)
                threat = refe.getThreat();
            return threat;
        }

        void tauntApply(Unit taunter)
        {
            HostileReference refe = threatContainer.getReferenceByTarget(taunter);
            if (getCurrentVictim() != null && refe != null && (refe.getThreat() < getCurrentVictim().getThreat()))
            {
                if (refe.getTempThreatModifier() == 0.0f) // Ok, temp threat is unused
                    refe.setTempThreat(getCurrentVictim().getThreat());
            }
        }

        void tauntFadeOut(Unit taunter)
        {
            HostileReference refe = threatContainer.getReferenceByTarget(taunter);
            if (refe != null)
                refe.resetTempThreat();
        }

        public void setCurrentVictim(HostileReference pHostileReference)
        {
            if (pHostileReference != null && pHostileReference != currentVictim)
            {
                Owner.SendChangeCurrentVictim(pHostileReference);
            }
            currentVictim = pHostileReference;
        }

        public void processThreatEvent(ThreatRefStatusChangeEvent threatRefStatusChangeEvent)
        {
            threatRefStatusChangeEvent.setThreatManager(this);     // now we can set the threat manager

            HostileReference hostilRef = threatRefStatusChangeEvent.getReference();

            switch (threatRefStatusChangeEvent.getType())
            {
                case UnitEventTypes.ThreatRefThreatChange:
                    if ((getCurrentVictim() == hostilRef && threatRefStatusChangeEvent.getFValue() < 0.0f) ||
                        (getCurrentVictim() != hostilRef && threatRefStatusChangeEvent.getFValue() > 0.0f))
                        setDirty(true);                             // the order in the threat list might have changed
                    break;
                case UnitEventTypes.ThreatRefOnlineStatus:
                    if (!hostilRef.isOnline())
                    {
                        if (hostilRef == getCurrentVictim())
                        {
                            setCurrentVictim(null);
                            setDirty(true);
                        }
                        Owner.SendRemoveFromThreatList(hostilRef);
                        threatContainer.remove(hostilRef);
                        threatOfflineContainer.addReference(hostilRef);
                    }
                    else
                    {
                        if (getCurrentVictim() != null && hostilRef.getThreat() > (1.1f * getCurrentVictim().getThreat()))
                            setDirty(true);
                        threatContainer.addReference(hostilRef);
                        threatOfflineContainer.remove(hostilRef);
                    }
                    break;
                case UnitEventTypes.ThreatRefRemoveFromList:
                    if (hostilRef == getCurrentVictim())
                    {
                        setCurrentVictim(null);
                        setDirty(true);
                    }
                    Owner.SendRemoveFromThreatList(hostilRef);
                    if (hostilRef.isOnline())
                        threatContainer.remove(hostilRef);
                    else
                        threatOfflineContainer.remove(hostilRef);
                    break;
            }
        }


        public bool isNeedUpdateToClient(uint time)
        {
            if (isThreatListEmpty())
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
        void resetAllAggro()
        {
            var threatList = threatContainer.threatList;
            if (threatList.Empty())
                return;

            foreach (var refe in threatList)
                refe.setThreat(0);

            setDirty(true);
        }
        public bool isThreatListEmpty()
        {
            return threatContainer.empty();
        }

        public HostileReference getCurrentVictim()
        {
            return currentVictim;
        }

        public Unit GetOwner()
        {
            return Owner;
        }

        void setDirty(bool isDirty)
        {
            threatContainer.setDirty(isDirty);
        }

        public List<HostileReference> getThreatList() { return threatContainer.getThreatList(); }
        public List<HostileReference> getOfflineThreatList() { return threatOfflineContainer.getThreatList(); }
        public ThreatContainer getOnlineContainer() { return threatContainer; }

        // The hatingUnit is not used yet
        public static float calcThreat(Unit hatedUnit, Unit hatingUnit, float threat, SpellSchoolMask schoolMask = SpellSchoolMask.Normal, SpellInfo threatSpell = null)
        {
            if (threatSpell != null)
            {
                var threatEntry = Global.SpellMgr.GetSpellThreatEntry(threatSpell.Id);
                if (threatEntry != null)
                    if (threatEntry.pctMod != 1.0f)
                        threat *= threatEntry.pctMod;

                // Energize is not affected by Mods
                foreach (SpellEffectInfo effect in threatSpell.GetEffectsForDifficulty(hatedUnit.GetMap().GetDifficultyID()))
                    if (effect != null && (effect.Effect == SpellEffectName.Energize || effect.ApplyAuraName == AuraType.PeriodicEnergize))
                        return threat;

                Player modOwner = hatedUnit.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(threatSpell.Id, SpellModOp.Threat, ref threat);
            }

            return hatedUnit.ApplyTotalThreatModifier(threat, schoolMask);
        }

        public static bool isValidProcess(Unit hatedUnit, Unit hatingUnit, SpellInfo threatSpell)
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

        public void clearReferences()
        {
            foreach (var reff in threatList)
            {
                reff.unlink();
            }

            threatList.Clear();
        }

        public HostileReference getReferenceByTarget(Unit victim)
        {
            if (victim == null)
                return null;

            ObjectGuid guid = victim.GetGUID();
            foreach (var reff in threatList)
            {
                if (reff != null && reff.getUnitGuid() == guid)
                    return reff;
            }

            return null;
        }

        public HostileReference addThreat(Unit victim, float threat)
        {
            var reff = getReferenceByTarget(victim);
            if (reff != null)
                reff.addThreat(threat);
            return reff;
        }

        public void modifyThreatPercent(Unit victim, int percent)
        {
            HostileReference refe = getReferenceByTarget(victim);
            if (refe != null)
                refe.addThreatPercent(percent);
        }

        public void update()
        {
            if (iDirty && threatList.Count > 1)
                threatList = threatList.OrderByDescending(p => p.getThreat()).ToList();

            iDirty = false;
        }

        public HostileReference selectNextVictim(Creature attacker, HostileReference currentVictim)
        {
            HostileReference currentRef = null;
            bool found = false;
            bool noPriorityTargetFound = false;

            for (var i = 0; i < threatList.Count; i++)
            {
                if (found)
                    break;

                currentRef = threatList[i];

                Unit target = currentRef.getTarget();
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
                        if (currentVictim == currentRef || currentRef.getThreat() <= 1.1f * currentVictim.getThreat())
                        {
                            if (currentVictim != currentRef && attacker.CanCreatureAttack(currentVictim.getTarget()))
                                currentRef = currentVictim;            // for second case, if currentvictim is attackable

                            found = true;
                            break;
                        }

                        if (currentRef.getThreat() > 1.3f * currentVictim.getThreat() ||
                            (currentRef.getThreat() > 1.1f * currentVictim.getThreat() &&
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

        public void setDirty(bool isDirty)
        {
            iDirty = isDirty;
        }

        bool isDirty()
        {
            return iDirty;
        }

        public bool empty()
        {
            return threatList.Empty();
        }
        public HostileReference getMostHated()
        {
            return threatList.Count == 0 ? null : threatList[0];
        }

        public void remove(HostileReference hostileRef)
        {
            threatList.Remove(hostileRef);
        }
        public void addReference(HostileReference hostileRef)
        {
            threatList.Add(hostileRef);
        }

        public List<HostileReference> getThreatList() { return threatList; }

        public List<HostileReference> threatList { get; set; }
        bool iDirty;
    }
}
