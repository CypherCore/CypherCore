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
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System.Collections.Generic;

namespace Scripts.Pets
{
    struct PetMageConst
    {
        public const uint SpellCloneMe = 45204;
        public const uint SpellMastersThreatList = 58838;
        public const uint SpellMageFrostBolt = 59638;
        public const uint SpellMageFireBlast = 59637;


        public const uint TimerMirrorImageInit = 0;
        public const uint TimerMirrorImageFrostBolt = 4000;
        public const uint TimerMirrorImageFireBlast = 6000;
    }

    [Script]
    class npc_pet_mage_mirror_image : CasterAI
    {
        public npc_pet_mage_mirror_image(Creature creature) : base(creature) { }

        void Init()
        {
            Unit owner = me.GetCharmerOrOwner();

            List<Unit> targets = new List<Unit>();
            var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(me, me, 30.0f);
            var searcher = new UnitListSearcher(me, targets, u_check);
            Cell.VisitAllObjects(me, searcher, 40.0f);

            Unit highestThreatUnit = null;
            float highestThreat = 0.0f;
            Unit nearestPlayer = null;
            foreach (var unit in targets)
            {
                // Consider only units without CC
                if (!unit.HasBreakableByDamageCrowdControlAura(unit))
                {
                    // Take first found unit
                    if (!highestThreatUnit && !unit.IsTypeId(TypeId.Player))
                    {
                        highestThreatUnit = unit;
                        continue;
                    }
                    if (!nearestPlayer && unit.IsTypeId(TypeId.Player))
                    {
                        nearestPlayer = unit;
                        continue;
                    }
                    // else compare best fit unit with current unit
                    var triggers = unit.GetThreatManager().getThreatList();
                    foreach (var reference in triggers)
                    {
                        // Try to find threat referenced to owner
                        if (reference.getTarget() == owner)
                        {
                            // Check if best fit hostile unit hs lower threat than this current unit
                            if (highestThreat < reference.getThreat())
                            {
                                // If so, update best fit unit
                                highestThreat = reference.getThreat();
                                highestThreatUnit = unit;
                                break;
                            }
                        }
                    }
                    // In case no unit with threat was found so far, always check for nearest unit (only for players)
                    if (unit.IsTypeId(TypeId.Player))
                    {
                        // If this player is closer than the previous one, update it
                        if (me.GetDistance(unit.GetPosition()) < me.GetDistance(nearestPlayer.GetPosition()))
                            nearestPlayer = unit;
                    }
                }
            }
            // Prioritize units with threat referenced to owner
            if (highestThreat > 0.0f && highestThreatUnit)
                me.Attack(highestThreatUnit, false);
            // If there is no such target, try to attack nearest hostile unit if such exists
            else if (nearestPlayer)
                me.Attack(nearestPlayer, false);
        }

        bool IsInThreatList(Unit target)
        {
            Unit owner = me.GetCharmerOrOwner();

            List<Unit> targets = new List<Unit>();
            var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(me, me, 30.0f);
            var searcher = new UnitListSearcher(me, targets, u_check);
            Cell.VisitAllObjects(me, searcher, 40.0f);

            foreach (var unit in targets)
            {
                if (unit == target)
                {
                    // Consider only units without CC
                    if (!unit.HasBreakableByDamageCrowdControlAura(unit))
                    {
                        var triggers = unit.GetThreatManager().getThreatList();
                        foreach (var reference in triggers)
                        {
                            // Try to find threat referenced to owner
                            if (reference.getTarget() == owner)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();
            Unit owner = me.GetOwner();
            if (!owner)
                return;

            // here mirror image casts on summoner spell (not present in client dbc) 49866
            // here should be auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcasted by mirror images (stats related?)
            // Clone Me!
            owner.CastSpell(me, PetMageConst.SpellCloneMe, false);
        }

        public override void EnterCombat(Unit victim)
        {
            if (me.GetVictim() && !me.GetVictim().HasBreakableByDamageCrowdControlAura(me))
            {
                me.CastSpell(victim, PetMageConst.SpellMageFireBlast, false);
                _events.ScheduleEvent(PetMageConst.SpellMageFrostBolt, PetMageConst.TimerMirrorImageInit);
                _events.ScheduleEvent(PetMageConst.SpellMageFireBlast, PetMageConst.TimerMirrorImageFireBlast);
            }
            else
                EnterEvadeMode(EvadeReason.Other);
        }

        public override void Reset()
        {
            _events.Reset();
        }

        public override void UpdateAI(uint diff)
        {
            Unit owner = me.GetCharmerOrOwner();
            if (!owner)
                return;

            Unit target = owner.getAttackerForHelper();

            _events.Update(diff);

            // prevent CC interrupts by images
            if (me.GetVictim() && me.GetVictim().HasBreakableByDamageCrowdControlAura(me))
            {
                me.InterruptNonMeleeSpells(false);
                return;
            }

            if (me.HasUnitState(UnitState.Casting))
                return;

            // assign target if image doesnt have any or the target is not actual
            if (!target || me.GetVictim() != target)
            {
                Unit ownerTarget = null;
                Player owner1 = me.GetCharmerOrOwner().ToPlayer();
                if (owner1)
                    ownerTarget = owner1.GetSelectedUnit();

                // recognize which victim will be choosen
                if (ownerTarget && ownerTarget.IsTypeId(TypeId.Player))
                {
                    if (!ownerTarget.HasBreakableByDamageCrowdControlAura(ownerTarget))
                        me.Attack(ownerTarget, false);
                }
                else if (ownerTarget && !ownerTarget.IsTypeId(TypeId.Player) && IsInThreatList(ownerTarget))
                {
                    if (!ownerTarget.HasBreakableByDamageCrowdControlAura(ownerTarget))
                        me.Attack(ownerTarget, false);
                }
                else
                    Init();
            }

            _events.ExecuteEvents(spellId =>
            {
                if (spellId == PetMageConst.SpellMageFrostBolt)
                {
                    _events.ScheduleEvent(PetMageConst.SpellMageFrostBolt, PetMageConst.TimerMirrorImageFrostBolt);
                    DoCastVictim(spellId);
                }
                else if (spellId == PetMageConst.SpellMageFireBlast)
                {
                    DoCastVictim(spellId);
                    _events.ScheduleEvent(PetMageConst.SpellMageFireBlast, PetMageConst.TimerMirrorImageFireBlast);
                }
            });
        }

        // Do not reload Creature templates on evade mode enter - prevent visual lost
        public override void EnterEvadeMode(EvadeReason why)
        {
            if (me.IsInEvadeMode() || !me.IsAlive())
                return;

            Unit owner = me.GetCharmerOrOwner();

            me.CombatStop(true);
            if (owner && !me.HasUnitState(UnitState.Follow))
            {
                me.GetMotionMaster().Clear(false);
                me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, me.GetFollowAngle(), MovementSlot.Active);
            }
            Init();
        }
    }
}
