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

        Unit GetOwner() { return Owner; }

        // send threat to all my haters for the victim
        // The victim is then hated by them as well
        // use for buffs and healing threat functionality
        public void ThreatAssist(Unit victim, float baseThreat, SpellInfo threatSpell = null)
        {
            float threat = ThreatManager.CalcThreat(victim, Owner, baseThreat, (threatSpell != null ? threatSpell.GetSchoolMask() : SpellSchoolMask.Normal), threatSpell);
            threat /= GetSize();

            HostileReference refe = GetFirst();
            while (refe != null)
            {
                if (ThreatManager.IsValidProcess(victim, refe.GetSource().GetOwner(), threatSpell))
                    refe.GetSource().DoAddThreat(victim, threat);

                refe = refe.Next();
            }
        }

        public void AddTempThreat(float threat, bool apply)
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                if (apply)
                {
                    if (refe.GetTempThreatModifier() == 0.0f)
                        refe.AddTempThreat(threat);
                }
                else
                    refe.ResetTempThreat();

                refe = refe.Next();
            }
        }

        void AddThreatPercent(int percent)
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                refe.AddThreatPercent(percent);
                refe = refe.Next();
            }
        }

        // The references are not needed anymore
        // tell the source to remove them from the list and free the mem
        public void DeleteReferences()
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.Next();
                refe.RemoveReference();
                refe = nextRef;
            }
        }

        // Remove specific faction references
        public void DeleteReferencesForFaction(uint faction)
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.Next();
                if (refe.GetSource().GetOwner().GetFactionTemplateEntry().Faction == faction)
                {
                    refe.RemoveReference();
                }
                refe = nextRef;
            }
        }

        // delete all references out of specified range
        public void DeleteReferencesOutOfRange(float range)
        {
            HostileReference refe = GetFirst();
            range = range * range;
            while (refe != null)
            {
                HostileReference nextRef = refe.Next();
                Unit owner = refe.GetSource().GetOwner();
                if (!owner.IsActiveObject() && owner.GetExactDist2dSq(GetOwner()) > range)
                {
                    refe.RemoveReference();
                }
                refe = nextRef;
            }
        }

        public new HostileReference GetFirst() { return (HostileReference)base.GetFirst(); }

        public void UpdateThreatTables()
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                refe.UpdateOnlineStatus();
                refe = refe.Next();
            }
        }

        public void SetOnlineOfflineState(bool isOnline)
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                refe.SetOnlineOfflineState(isOnline);
                refe = refe.Next();
            }
        }

        // set state for one reference, defined by Unit
        public void SetOnlineOfflineState(Unit creature, bool isOnline)
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.Next();
                if (refe.GetSource().GetOwner() == creature)
                {
                    refe.SetOnlineOfflineState(isOnline);
                    break;
                }
                refe = nextRef;
            }
        }

        // delete one reference, defined by Unit
        public void DeleteReference(Unit creature)
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.Next();
                if (refe.GetSource().GetOwner() == creature)
                {
                    refe.RemoveReference();
                    break;
                }
                refe = nextRef;
            }
        }

        public void UpdateVisibility()
        {
            HostileReference refe = GetFirst();
            while (refe != null)
            {
                HostileReference nextRef = refe.Next();
                if (!refe.GetSource().GetOwner().CanSeeOrDetect(GetOwner()))
                {
                    nextRef = refe.Next();
                    refe.RemoveReference();
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
            Link(refUnit, threatManager);
            iUnitGuid = refUnit.GetGUID();
            iOnline = true;
            iAccessible = true;
        }

        public override void TargetObjectBuildLink()
        {
            GetTarget().AddHatedBy(this);
        }

        public override void TargetObjectDestroyLink()
        {
            GetTarget().RemoveHatedBy(this);
        }

        public override void SourceObjectDestroyLink()
        {
            SetOnlineOfflineState(false);
        }

        void FireStatusChanged(ThreatRefStatusChangeEvent threatRefStatusChangeEvent)
        {
            if (GetSource() != null)
                GetSource().ProcessThreatEvent(threatRefStatusChangeEvent);
        }

        public void AddThreat(float modThreat)
        {
            if (modThreat == 0.0f)
                return;

            iThreat += modThreat;

            // the threat is changed. Source and target unit have to be available
            // if the link was cut before relink it again
            if (!IsOnline())
                UpdateOnlineStatus();

            ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefThreatChange, this, modThreat);
            FireStatusChanged(Event);

            if (IsValid() && modThreat > 0.0f)
            {
                Unit victimOwner = GetTarget().GetCharmerOrOwner();
                if (victimOwner != null && victimOwner.IsAlive())
                    GetSource().AddThreat(victimOwner, 0.0f);     // create a threat to the owner of a pet, if the pet attacks
            }
        }

        public void AddThreatPercent(int percent)
        {
            AddThreat(MathFunctions.CalculatePct(iThreat, percent));
        }

        // check, if source can reach target and set the status
        public void UpdateOnlineStatus()
        {
            bool online = false;
            bool accessible = false;

            if (!IsValid())
            {
                Unit target = Global.ObjAccessor.GetUnit(GetSourceUnit(), GetUnitGuid());
                if (target != null)
                    Link(target, GetSource());
            }

            // only check for online status if
            // ref is valid
            // target is no player or not gamemaster
            // target is not in flight
            if (IsValid()
                && (GetTarget().IsTypeId(TypeId.Player) || !GetTarget().ToPlayer().IsGameMaster())
                && !GetTarget().HasUnitState(UnitState.InFlight)
                && GetTarget().IsInMap(GetSourceUnit())
                && GetTarget().IsInPhase(GetSourceUnit())
                )
            {
                Creature creature = GetSourceUnit().ToCreature();
                online = GetTarget().IsInAccessiblePlaceFor(creature);
                if (!online)
                {
                    if (creature.IsWithinCombatRange(GetTarget(), creature.m_CombatDistance))
                        online = true;                              // not accessible but stays online
                }
                else
                    accessible = true;
            }
            SetAccessibleState(accessible);
            SetOnlineOfflineState(online);
        }

        public void SetOnlineOfflineState(bool isOnline)
        {
            if (iOnline != isOnline)
            {
                iOnline = isOnline;
                if (!iOnline)
                    SetAccessibleState(false);                      // if not online that not accessable as well

                ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefOnlineStatus, this);
                FireStatusChanged(Event);
            }
        }

        void SetAccessibleState(bool isAccessible)
        {
            if (iAccessible != isAccessible)
            {
                iAccessible = isAccessible;

                ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefAccessibleStatus, this);
                FireStatusChanged(Event);
            }
        }

        // reference is not needed anymore. realy delete it !
        public void RemoveReference()
        {
            Invalidate();

            ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefRemoveFromList, this);
            FireStatusChanged(Event);
        }

        Unit GetSourceUnit()
        {
            return GetSource().GetOwner();
        }

        public void SetThreat(float threat)
        {
            AddThreat(threat - iThreat);
        }

        public float GetThreat()
        {
            return iThreat + iTempThreatModifier;
        }

        public bool IsOnline()
        {
            return iOnline;
        }

        // The Unit might be in water and the creature can not enter the water, but has range attack
        // in this case online = true, but accessible = false
        bool IsAccessible()
        {
            return iAccessible;
        }

        // used for temporary setting a threat and reducing it later again.
        // the threat modification is stored
        public void SetTempThreat(float threat)
        {
            AddTempThreat(threat - iTempThreatModifier);
        }

        public void AddTempThreat(float threat)
        {
            if (threat == 0.0f)
                return;

            iTempThreatModifier += threat;

            ThreatRefStatusChangeEvent Event = new ThreatRefStatusChangeEvent(UnitEventTypes.ThreatRefThreatChange, this, threat);
            FireStatusChanged(Event);
        }

        public void ResetTempThreat()
        {
            AddTempThreat(-iTempThreatModifier);
        }

        public float GetTempThreatModifier()
        {
            return iTempThreatModifier;
        }

        public ObjectGuid GetUnitGuid()
        {
            return iUnitGuid;
        }

        public new HostileReference Next() { return (HostileReference)base.Next(); }

        float iThreat;
        float iTempThreatModifier;                          // used for SPELL_AURA_MOD_TOTAL_THREAT
        ObjectGuid iUnitGuid;
        bool iOnline;
        bool iAccessible;
    }
}