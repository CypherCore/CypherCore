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
using Game.Entities;
using Game.Spells;

namespace Game.Combat
{
    public class HostileRefManager : RefManager<Unit, ThreatManager>
    {
        Unit Owner;

        public HostileRefManager(Unit owner)
        {
            Owner = owner;
        }

        Unit getOwner() { return Owner; }

        // send threat to all my haters for the victim
        // The victim is then hated by them as well
        // use for buffs and healing threat functionality
        public void threatAssist(Unit victim, float baseThreat, SpellInfo threatSpell = null)
        {
            float threat = ThreatManager.calcThreat(victim, Owner, baseThreat, (threatSpell != null ? threatSpell.GetSchoolMask() : SpellSchoolMask.Normal), threatSpell);
            threat /= GetSize();

            HostileReference refe = getFirst();
            while (refe != null)
            {
                if (ThreatManager.isValidProcess(victim, refe.GetSource().GetOwner(), threatSpell))
                    refe.GetSource().doAddThreat(victim, threat);

                refe = refe.next();
            }
        }

        public void addTempThreat(float threat, bool apply)
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                if (apply)
                {
                    if (refe.getTempThreatModifier() == 0.0f)
                        refe.addTempThreat(threat);
                }
                else
                    refe.resetTempThreat();

                refe = refe.next();
            }
        }

        void addThreatPercent(int percent)
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                refe.addThreatPercent(percent);
                refe = refe.next();
            }
        }

        // The references are not needed anymore
        // tell the source to remove them from the list and free the mem
        public void deleteReferences()
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.next();
                refe.removeReference();
                refe = nextRef;
            }
        }

        // Remove specific faction references
        public void deleteReferencesForFaction(uint faction)
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.next();
                if (refe.GetSource().GetOwner().GetFactionTemplateEntry().Faction == faction)
                {
                    refe.removeReference();
                }
                refe = nextRef;
            }
        }

        // delete all references out of specified range
        public void deleteReferencesOutOfRange(float range)
        {
            HostileReference refe = getFirst();
            range = range * range;
            while (refe != null)
            {
                HostileReference nextRef = refe.next();
                Unit owner = refe.GetSource().GetOwner();
                if (!owner.isActiveObject() && owner.GetExactDist2dSq(getOwner()) > range)
                {
                    refe.removeReference();
                }
                refe = nextRef;
            }
        }

        public new HostileReference getFirst() { return ((HostileReference)base.getFirst()); }

        public void updateThreatTables()
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                refe.updateOnlineStatus();
                refe = refe.next();
            }
        }

        public void setOnlineOfflineState(bool isOnline)
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                refe.setOnlineOfflineState(isOnline);
                refe = refe.next();
            }
        }

        // set state for one reference, defined by Unit
        public void setOnlineOfflineState(Unit creature, bool isOnline)
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.next();
                if (refe.GetSource().GetOwner() == creature)
                {
                    refe.setOnlineOfflineState(isOnline);
                    break;
                }
                refe = nextRef;
            }
        }

        // delete one reference, defined by Unit
        public void deleteReference(Unit creature)
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.next();
                if (refe.GetSource().GetOwner() == creature)
                {
                    refe.removeReference();
                    break;
                }
                refe = nextRef;
            }
        }

        public void UpdateVisibility()
        {
            HostileReference refe = getFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.next();
                if (!refe.GetSource().GetOwner().CanSeeOrDetect(getOwner()))
                {
                    nextRef = refe.next();
                    refe.removeReference();
                }
                refe = nextRef;
            }
        }
    }

    public class HostileReference : Reference<Unit, ThreatManager>
    {
        public HostileReference(Unit refUnit, ThreatManager threatManager, float threat)
        {
            iThreat = threat;
            iTempThreatModifier = 0.0f;
            link(refUnit, threatManager);
            iUnitGuid = refUnit.GetGUID();
            iOnline = true;
            iAccessible = true;
        }

        public override void targetObjectBuildLink()
        {
            getTarget().addHatedBy(this);
        }
        public override void targetObjectDestroyLink()
        {
            getTarget().removeHatedBy(this);
        }
        public override void sourceObjectDestroyLink()
        {
            setOnlineOfflineState(false);
        }

        void fireStatusChanged(ThreatRefStatusChangeEvent threatRefStatusChangeEvent)
        {
            if (GetSource() != null)
                GetSource().processThreatEvent(threatRefStatusChangeEvent);
        }

        public void addThreat(float modThreat)
        {
            if (modThreat == 0.0f)
                return;

            iThreat += modThreat;

            // the threat is changed. Source and target unit have to be available
            // if the link was cut before relink it again
            if (!isOnline())
                updateOnlineStatus();

            ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefThreatChange, this, modThreat);
            fireStatusChanged(Event);

            if (isValid() && modThreat > 0.0f)
            {
                Unit victimOwner = getTarget().GetCharmerOrOwner();
                if (victimOwner != null && victimOwner.IsAlive())
                    GetSource().addThreat(victimOwner, 0.0f);     // create a threat to the owner of a pet, if the pet attacks
            }
        }

        public void addThreatPercent(int percent)
        {
            addThreat(MathFunctions.CalculatePct(iThreat, percent));
        }

        // check, if source can reach target and set the status
        public void updateOnlineStatus()
        {
            bool online = false;
            bool accessible = false;

            if (!isValid())
            {
                Unit target = Global.ObjAccessor.GetUnit(getSourceUnit(), getUnitGuid());
                if (target != null)
                    link(target, GetSource());
            }

            // only check for online status if
            // ref is valid
            // target is no player or not gamemaster
            // target is not in flight
            if (isValid()
                && (getTarget().IsTypeId(TypeId.Player) || !getTarget().ToPlayer().IsGameMaster())
                && !getTarget().HasUnitState(UnitState.InFlight)
                && getTarget().IsInMap(getSourceUnit())
                && getTarget().IsInPhase(getSourceUnit())
                )
            {
                Creature creature = getSourceUnit().ToCreature();
                online = getTarget().isInAccessiblePlaceFor(creature);
                if (!online)
                {
                    if (creature.IsWithinCombatRange(getTarget(), creature.m_CombatDistance))
                        online = true;                              // not accessible but stays online
                }
                else
                    accessible = true;
            }
            setAccessibleState(accessible);
            setOnlineOfflineState(online);
        }

        public void setOnlineOfflineState(bool isOnline)
        {
            if (iOnline != isOnline)
            {
                iOnline = isOnline;
                if (!iOnline)
                    setAccessibleState(false);                      // if not online that not accessable as well

                ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefOnlineStatus, this);
                fireStatusChanged(Event);
            }
        }

        void setAccessibleState(bool isAccessible)
        {
            if (iAccessible != isAccessible)
            {
                iAccessible = isAccessible;

                ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefAccessibleStatus, this);
                fireStatusChanged(Event);
            }
        }

        // reference is not needed anymore. realy delete it !
        public void removeReference()
        {
            invalidate();

            ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefRemoveFromList, this);
            fireStatusChanged(Event);
        }

        Unit getSourceUnit()
        {
            return GetSource().GetOwner();
        }

        public void setThreat(float threat)
        {
            addThreat(threat - iThreat);
        }

        public float getThreat()
        {
            return iThreat + iTempThreatModifier;
        }

        public bool isOnline()
        {
            return iOnline;
        }

        // The Unit might be in water and the creature can not enter the water, but has range attack
        // in this case online = true, but accessible = false
        bool isAccessible()
        {
            return iAccessible;
        }

        // used for temporary setting a threat and reducing it later again.
        // the threat modification is stored
        public void setTempThreat(float threat)
        {
            addTempThreat(threat - iTempThreatModifier);
        }

        public void addTempThreat(float threat)
        {
            if (threat == 0.0f)
                return;

            iTempThreatModifier += threat;

            ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefThreatChange, this, threat);
            fireStatusChanged(Event);
        }

        public void resetTempThreat()
        {
            addTempThreat(-iTempThreatModifier);
        }

        public float getTempThreatModifier()
        {
            return iTempThreatModifier;
        }

        public ObjectGuid getUnitGuid()
        {
            return iUnitGuid;
        }

        public new HostileReference next() { return (HostileReference)base.next(); }

        float iThreat;
        float iTempThreatModifier;                          // used for SPELL_AURA_MOD_TOTAL_THREAT
        ObjectGuid iUnitGuid;
        bool iOnline;
        bool iAccessible;
    }
}
