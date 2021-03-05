﻿/*
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
using Game.Maps;
using Game.Scripting;
using System.Collections.Generic;
using System;

namespace Scripts.Pets
{
    namespace Mage
    {
        internal struct SpellIds
        {
            public const uint CloneMe = 45204;
            public const uint MastersThreatList = 58838;
            public const uint MageFrostBolt = 59638;
            public const uint MageFireBlast = 59637;
        }

        internal struct MiscConst
        {
            public const uint TimerMirrorImageInit = 0;
            public const uint TimerMirrorImageFrostBolt = 4000;
            public const uint TimerMirrorImageFireBlast = 6000;
        }

        [Script]
        internal class npc_pet_mage_mirror_image : CasterAI
        {
            public npc_pet_mage_mirror_image(Creature creature) : base(creature) { }

            private void Init()
            {
                var owner = me.GetCharmerOrOwner();

                var targets = new List<Unit>();
                var u_check = new AnyUnfriendlyUnitInObjectRangeCheck(me, me, 30.0f);
                var searcher = new UnitListSearcher(me, targets, u_check);
                Cell.VisitAllObjects(me, searcher, 40.0f);

                Unit highestThreatUnit = null;
                var highestThreat = 0.0f;
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
                        var triggers = unit.GetThreatManager().GetThreatList();
                        foreach (var reference in triggers)
                        {
                            // Try to find threat referenced to owner
                            if (reference.GetTarget() == owner)
                            {
                                // Check if best fit hostile unit hs lower threat than this current unit
                                if (highestThreat < reference.GetThreat())
                                {
                                    // If so, update best fit unit
                                    highestThreat = reference.GetThreat();
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

            private bool IsInThreatList(Unit target)
            {
                var owner = me.GetCharmerOrOwner();

                var targets = new List<Unit>();
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
                            var triggers = unit.GetThreatManager().GetThreatList();
                            foreach (var reference in triggers)
                            {
                                // Try to find threat referenced to owner
                                if (reference.GetTarget() == owner)
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
                var owner = me.GetOwner();
                if (!owner)
                    return;

                // here mirror image casts on summoner spell (not present in client dbc) 49866
                // here should be auras (not present in client dbc): 35657, 35658, 35659, 35660 selfcasted by mirror images (stats related?)
                // Clone Me!
                owner.CastSpell(me, SpellIds.CloneMe, false);
            }

            public override void EnterCombat(Unit victim)
            {
                if (me.GetVictim() && !me.GetVictim().HasBreakableByDamageCrowdControlAura(me))
                {
                    me.CastSpell(victim, SpellIds.MageFireBlast, false);
                    _scheduler.Schedule(TimeSpan.FromSeconds(0), task =>
                    {  
                        DoCastVictim(SpellIds.MageFrostBolt);
                        task.Repeat(TimeSpan.FromSeconds(4));
                    });
                    _scheduler.Schedule(TimeSpan.FromSeconds(6), task =>
                    {
                        DoCastVictim(SpellIds.MageFireBlast);
                        task.Repeat();
                    });
                }
                else
                    EnterEvadeMode(EvadeReason.Other);
            }

            public override void Reset()
            {
                _scheduler.CancelAll();
            }

            public override void UpdateAI(uint diff)
            {
                var owner = me.GetCharmerOrOwner();
                if (!owner)
                    return;

                var target = owner.GetAttackerForHelper();

                _scheduler.Update(diff);

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
                    var owner1 = me.GetCharmerOrOwner().ToPlayer();
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

                _events.ExecuteEvents(eventId =>
                {
                    if (eventId == SpellIds.MageFrostBolt)
                    {

                    }
                    else if (eventId == SpellIds.MageFireBlast)
                    {
                    }
                });
            }

            // Do not reload Creature templates on evade mode enter - prevent visual lost
            public override void EnterEvadeMode(EvadeReason why)
            {
                if (me.IsInEvadeMode() || !me.IsAlive())
                    return;

                var owner = me.GetCharmerOrOwner();

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
}
