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
using Game.Groups;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.AI
{
    public class PetAI : CreatureAI
    {
        public PetAI(Creature c) : base(c)
        {
            i_tracker = new TimeTracker(5000);

            UpdateAllies();
        }

        bool _needToStop()
        {
            // This is needed for charmed creatures, as once their target was reset other effects can trigger threat
            if (me.IsCharmed() && me.GetVictim() == me.GetCharmer())
                return true;

            // dont allow pets to follow targets far away from owner
            Unit owner = me.GetCharmerOrOwner();
            if (owner)
                if (owner.GetExactDist(me) >= (owner.GetVisibilityRange() - 10.0f))
                    return true;

            return !me.IsValidAttackTarget(me.GetVictim());
        }

        void _stopAttack()
        {
            if (!me.IsAlive())
            {
                Log.outDebug(LogFilter.Server, "Creature stoped attacking cuz his dead [{0}]", me.GetGUID().ToString());
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MoveIdle();
                me.CombatStop();
                me.getHostileRefManager().deleteReferences();

                return;
            }

            me.AttackStop();
            me.InterruptNonMeleeSpells(false);
            me.SendMeleeAttackStop(); // Should stop pet's attack button from flashing
            me.GetCharmInfo().SetIsCommandAttack(false);
            ClearCharmInfoFlags();
            HandleReturnMovement();
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.IsAlive() || me.GetCharmInfo() == null)
                return;

            Unit owner = me.GetCharmerOrOwner();

            if (m_updateAlliesTimer <= diff)
                // UpdateAllies self set update timer
                UpdateAllies();
            else
                m_updateAlliesTimer -= diff;

            if (me.GetVictim() && me.GetVictim().IsAlive())
            {
                // is only necessary to stop casting, the pet must not exit combat
                if (!me.GetCurrentSpell(CurrentSpellTypes.Channeled) && // ignore channeled spells (Pin, Seduction)
                    (me.GetVictim() && me.GetVictim().HasBreakableByDamageCrowdControlAura(me)))
                {
                    me.InterruptNonMeleeSpells(false);
                    return;
                }

                if (_needToStop())
                {
                    Log.outDebug(LogFilter.Server, "Pet AI stopped attacking [{0}]", me.GetGUID().ToString());
                    _stopAttack();
                    return;
                }

                // Check before attacking to prevent pets from leaving stay position
                if (me.GetCharmInfo().HasCommandState(CommandStates.Stay))
                {
                    if (me.GetCharmInfo().IsCommandAttack() || (me.GetCharmInfo().IsAtStay() && me.IsWithinMeleeRange(me.GetVictim())))
                        DoMeleeAttackIfReady();
                }
                else
                    DoMeleeAttackIfReady();
            }
            else
            {
                if (me.HasReactState(ReactStates.Aggressive) || me.GetCharmInfo().IsAtStay())
                {
                    // Every update we need to check targets only in certain cases
                    // Aggressive - Allow auto select if owner or pet don't have a target
                    // Stay - Only pick from pet or owner targets / attackers so targets won't run by
                    //   while chasing our owner. Don't do auto select.
                    // All other cases (ie: defensive) - Targets are assigned by AttackedBy(), OwnerAttackedBy(), OwnerAttacked(), etc.
                    Unit nextTarget = SelectNextTarget(me.HasReactState(ReactStates.Aggressive));

                    if (nextTarget)
                        AttackStart(nextTarget);
                    else
                        HandleReturnMovement();
                }
                else
                    HandleReturnMovement();
            }

            // Autocast (casted only in combat or persistent spells in any state)
            if (!me.HasUnitState(UnitState.Casting))
            {
                List<Tuple<Unit, Spell>> targetSpellStore = new List<Tuple<Unit, Spell>>();

                for (byte i = 0; i < me.GetPetAutoSpellSize(); ++i)
                {
                    uint spellID = me.GetPetAutoSpellOnPos(i);
                    if (spellID == 0)
                        continue;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellID);
                    if (spellInfo == null)
                        continue;

                    if (me.GetCharmInfo() != null && me.GetSpellHistory().HasGlobalCooldown(spellInfo))
                        continue;

                    // check spell cooldown
                    if (!me.GetSpellHistory().IsReady(spellInfo))
                        continue;

                    if (spellInfo.IsPositive())
                    {
                        if (spellInfo.CanBeUsedInCombat())
                        {
                            // Check if we're in combat or commanded to attack
                            if (!me.IsInCombat() && !me.GetCharmInfo().IsCommandAttack())
                                continue;
                        }

                        Spell spell = new Spell(me, spellInfo, TriggerCastFlags.None);
                        bool spellUsed = false;

                        // Some spells can target enemy or friendly (DK Ghoul's Leap)
                        // Check for enemy first (pet then owner)
                        Unit target = me.getAttackerForHelper();
                        if (!target && owner)
                            target = owner.getAttackerForHelper();

                        if (target)
                        {
                            if (CanAIAttack(target) && spell.CanAutoCast(target))
                            {
                                targetSpellStore.Add(Tuple.Create(target, spell));
                                spellUsed = true;
                            }
                        }

                        if (spellInfo.HasEffect(SpellEffectName.JumpDest))
                        {
                            if (!spellUsed)
                                spell.Dispose();
                            continue; // Pets must only jump to target
                        }

                        // No enemy, check friendly
                        if (!spellUsed)
                        {
                            foreach (var tar in m_AllySet)
                            {
                                Unit ally = Global.ObjAccessor.GetUnit(me, tar);

                                //only buff targets that are in combat, unless the spell can only be cast while out of combat
                                if (!ally)
                                    continue;

                                if (spell.CanAutoCast(ally))
                                {
                                    targetSpellStore.Add(Tuple.Create(ally, spell));
                                    spellUsed = true;
                                    break;
                                }
                            }
                        }

                        // No valid targets at all
                        if (!spellUsed)
                            spell.Dispose();
                    }
                    else if (me.GetVictim() && CanAIAttack(me.GetVictim()) && spellInfo.CanBeUsedInCombat())
                    {
                        Spell spell = new Spell(me, spellInfo, TriggerCastFlags.None);
                        if (spell.CanAutoCast(me.GetVictim()))
                            targetSpellStore.Add(Tuple.Create(me.GetVictim(), spell));
                        else
                            spell.Dispose();
                    }
                }

                //found units to cast on to
                if (!targetSpellStore.Empty())
                {
                    int index = RandomHelper.IRand(0, targetSpellStore.Count - 1);
                    var tss = targetSpellStore[index];

                    (Unit target, Spell spell) = tss;

                    targetSpellStore.RemoveAt(index);

                    SpellCastTargets targets = new SpellCastTargets();
                    targets.SetUnitTarget(target);

                    spell.prepare(targets);
                }

                // deleted cached Spell objects
                foreach (var pair in targetSpellStore)
                    pair.Item2.Dispose();
            }

            // Update speed as needed to prevent dropping too far behind and despawning
            me.UpdateSpeed(UnitMoveType.Run);
            me.UpdateSpeed(UnitMoveType.Walk);
            me.UpdateSpeed(UnitMoveType.Flight);
        }

        void UpdateAllies()
        {
            m_updateAlliesTimer = 10 * Time.InMilliseconds;                 // update friendly targets every 10 seconds, lesser checks increase performance

            Unit owner = me.GetCharmerOrOwner();
            if (!owner)
                return;

            Group group = null;
            Player player = owner.ToPlayer();
            if (player)
                group = player.GetGroup();

            //only pet and owner/not in group.ok
            if (m_AllySet.Count == 2 && !group)
                return;

            //owner is in group; group members filled in already (no raid . subgroupcount = whole count)
            if (group && !group.isRaidGroup() && m_AllySet.Count == (group.GetMembersCount() + 2))
                return;

            m_AllySet.Clear();
            m_AllySet.Add(me.GetGUID());
            if (group)                                              //add group
            {
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player Target = refe.GetSource();
                    if (!Target || !group.SameSubGroup(owner.ToPlayer(), Target))
                        continue;

                    if (Target.GetGUID() == owner.GetGUID())
                        continue;

                    m_AllySet.Add(Target.GetGUID());
                }
            }
            else                                                    //remove group
                m_AllySet.Add(owner.GetGUID());
        }

        public override void KilledUnit(Unit victim)
        {
            // Called from Unit.Kill() in case where pet or owner kills something
            // if owner killed this victim, pet may still be attacking something else
            if (me.GetVictim() && me.GetVictim() != victim)
                return;

            // Clear target just in case. May help problem where health / focus / mana
            // regen gets stuck. Also resets attack command.
            // Can't use _stopAttack() because that activates movement handlers and ignores
            // next target selection
            me.AttackStop();
            me.InterruptNonMeleeSpells(false);
            me.SendMeleeAttackStop();  // Stops the pet's 'Attack' button from flashing

            // Before returning to owner, see if there are more things to attack
            Unit nextTarget = SelectNextTarget(false);
            if (nextTarget)
                AttackStart(nextTarget);
            else
                HandleReturnMovement(); // Return
        }

        public override void AttackStart(Unit victim)
        {
            // Overrides Unit.AttackStart to correctly evaluate Pet states

            // Check all pet states to decide if we can attack this target
            if (!CanAIAttack(victim))
                return;

            // Only chase if not commanded to stay or if stay but commanded to attack
            DoAttack(victim, (!me.GetCharmInfo().HasCommandState(CommandStates.Stay) || me.GetCharmInfo().IsCommandAttack()));
        }

        public override void OwnerAttackedBy(Unit attacker)
        {
            // Called when owner takes damage. This function helps keep pets from running off
            //  simply due to owner gaining aggro.

            if (!attacker)
                return;

            // Passive pets don't do anything
            if (me.HasReactState(ReactStates.Passive))
                return;

            // Prevent pet from disengaging from current target
            if (me.GetVictim() && me.GetVictim().IsAlive())
                return;

            // Continue to evaluate and attack if necessary
            AttackStart(attacker);
        }

        public override void OwnerAttacked(Unit target)
        {
            // Called when owner attacks something. Allows defensive pets to know
            //  that they need to assist

            // Target might be null if called from spell with invalid cast targets
            if (!target)
                return;

            // Passive pets don't do anything
            if (me.HasReactState(ReactStates.Passive))
                return;

            // Prevent pet from disengaging from current target
            if (me.GetVictim() && me.GetVictim().IsAlive())
                return;

            // Continue to evaluate and attack if necessary
            AttackStart(target);
        }

        Unit SelectNextTarget(bool allowAutoSelect)
        {
            // Provides next target selection after current target death.
            // This function should only be called internally by the AI
            // Targets are not evaluated here for being valid targets, that is done in _CanAttack()
            // The parameter: allowAutoSelect lets us disable aggressive pet auto targeting for certain situations

            // Passive pets don't do next target selection
            if (me.HasReactState(ReactStates.Passive))
                return null;

            // Check pet attackers first so we don't drag a bunch of targets to the owner
            Unit myAttacker = me.getAttackerForHelper();
            if (myAttacker)
                if (!myAttacker.HasBreakableByDamageCrowdControlAura())
                    return myAttacker;

            // Not sure why we wouldn't have an owner but just in case...
            if (!me.GetCharmerOrOwner())
                return null;

            // Check owner attackers
            Unit ownerAttacker = me.GetCharmerOrOwner().getAttackerForHelper();
            if (ownerAttacker)
                if (!ownerAttacker.HasBreakableByDamageCrowdControlAura())
                    return ownerAttacker;

            // Check owner victim
            // 3.0.2 - Pets now start attacking their owners victim in defensive mode as soon as the hunter does
            Unit ownerVictim = me.GetCharmerOrOwner().GetVictim();
            if (ownerVictim)
                return ownerVictim;

            // Neither pet or owner had a target and aggressive pets can pick any target
            // To prevent aggressive pets from chain selecting targets and running off, we
            //  only select a random target if certain conditions are met.
            if (me.HasReactState(ReactStates.Aggressive) && allowAutoSelect)
            {
                if (!me.GetCharmInfo().IsReturning() || me.GetCharmInfo().IsFollowing() || me.GetCharmInfo().IsAtStay())
                {
                    Unit nearTarget = me.SelectNearestHostileUnitInAggroRange(true);
                    if (nearTarget)
                        return nearTarget;
                }
            }

            // Default - no valid targets
            return null;
        }

        void HandleReturnMovement()
        {
            // Handles moving the pet back to stay or owner

            // Prevent activating movement when under control of spells
            // such as "Eyes of the Beast"
            if (me.IsCharmed())
                return;

            if (me.GetCharmInfo().HasCommandState(CommandStates.Stay))
            {
                if (!me.GetCharmInfo().IsAtStay() && !me.GetCharmInfo().IsReturning())
                {
                    // Return to previous position where stay was clicked
                    float x, y, z;

                    me.GetCharmInfo().GetStayPosition(out x, out y, out z);
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsReturning(true);
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MovePoint(me.GetGUID().GetCounter(), x, y, z);
                }
            }
            else // COMMAND_FOLLOW
            {
                if (!me.GetCharmInfo().IsFollowing() && !me.GetCharmInfo().IsReturning())
                {
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsReturning(true);
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveFollow(me.GetCharmerOrOwner(), SharedConst.PetFollowDist, me.GetFollowAngle());
                }
            }
        }

        void DoAttack(Unit target, bool chase)
        {
            // Handles attack with or without chase and also resets flags
            // for next update / creature kill

            if (me.Attack(target, true))
            {
                Unit owner = me.GetOwner();
                if (owner)
                    owner.SetInCombatWith(target);

                // Play sound to let the player know the pet is attacking something it picked on its own
                if (me.HasReactState(ReactStates.Aggressive) && !me.GetCharmInfo().IsCommandAttack())
                    me.SendPetAIReaction(me.GetGUID());

                if (chase)
                {
                    bool oldCmdAttack = me.GetCharmInfo().IsCommandAttack(); // This needs to be reset after other flags are cleared
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsCommandAttack(oldCmdAttack); // For passive pets commanded to attack so they will use spells
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveChase(target, me.GetPetChaseDistance());
                }
                else
                {
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsAtStay(true);
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveIdle();
                }
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            // Receives notification when pet reaches stay or follow owner
            switch (type)
            {
                case MovementGeneratorType.Point:
                    {
                        // Pet is returning to where stay was clicked. data should be
                        // pet's GUIDLow since we set that as the waypoint ID
                        if (id == me.GetGUID().GetCounter() && me.GetCharmInfo().IsReturning())
                        {
                            ClearCharmInfoFlags();
                            me.GetCharmInfo().SetIsAtStay(true);
                            me.GetMotionMaster().Clear();
                            me.GetMotionMaster().MoveIdle();
                        }
                        break;
                    }
                case MovementGeneratorType.Follow:
                    {
                        // If data is owner's GUIDLow then we've reached follow point,
                        // otherwise we're probably chasing a creature
                        if (me.GetCharmerOrOwner() && me.GetCharmInfo() != null && id == me.GetCharmerOrOwner().GetGUID().GetCounter() && me.GetCharmInfo().IsReturning())
                        {
                            ClearCharmInfoFlags();
                            me.GetCharmInfo().SetIsFollowing(true);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public override bool CanAIAttack(Unit victim)
        {
            // Evaluates wether a pet can attack a specific target based on CommandState, ReactState and other flags
            // IMPORTANT: The order in which things are checked is important, be careful if you add or remove checks

            // Hmmm...
            if (!victim)
                return false;

            if (!victim.IsAlive())
            {
                // Clear target to prevent getting stuck on dead targets
                me.AttackStop();
                me.InterruptNonMeleeSpells(false);
                me.SendMeleeAttackStop();
                return false;
            }

            // Passive - passive pets can attack if told to
            if (me.HasReactState(ReactStates.Passive))
                return me.GetCharmInfo().IsCommandAttack();

            // CC - mobs under crowd control can be attacked if owner commanded
            if (victim.HasBreakableByDamageCrowdControlAura())
                return me.GetCharmInfo().IsCommandAttack();

            // Returning - pets ignore attacks only if owner clicked follow
            if (me.GetCharmInfo().IsReturning())
                return !me.GetCharmInfo().IsCommandFollow();

            // Stay - can attack if target is within range or commanded to
            if (me.GetCharmInfo().HasCommandState(CommandStates.Stay))
                return (me.IsWithinMeleeRange(victim) || me.GetCharmInfo().IsCommandAttack());

            //  Pets attacking something (or chasing) should only switch targets if owner tells them to
            if (me.GetVictim() && me.GetVictim() != victim)
            {
                // Check if our owner selected this target and clicked "attack"
                Unit ownerTarget;
                Player owner = me.GetCharmerOrOwner().ToPlayer();
                if (owner)
                    ownerTarget = owner.GetSelectedUnit();
                else
                    ownerTarget = me.GetCharmerOrOwner().GetVictim();

                if (ownerTarget && me.GetCharmInfo().IsCommandAttack())
                    return (victim.GetGUID() == ownerTarget.GetGUID());
            }

            // Follow
            if (me.GetCharmInfo().HasCommandState(CommandStates.Follow))
                return !me.GetCharmInfo().IsReturning();

            // default, though we shouldn't ever get here
            return false;
        }

        public override void ReceiveEmote(Player player, TextEmotes emoteId)
        {
            if (!me.GetOwnerGUID().IsEmpty() && me.GetOwnerGUID() == player.GetGUID())
                switch (emoteId)
                {
                    case TextEmotes.Cower:
                        if (me.IsPet() && me.ToPet().IsPetGhoul())
                            me.HandleEmoteCommand(Emote.OneshotOmnicastGhoul);
                        break;
                    case TextEmotes.Angry:
                        if (me.IsPet() && me.ToPet().IsPetGhoul())
                            me.HandleEmoteCommand(Emote.StateStun);
                        break;
                    case TextEmotes.Glare:
                        if (me.IsPet() && me.ToPet().IsPetGhoul())
                            me.HandleEmoteCommand(Emote.StateStun);
                        break;
                    case TextEmotes.Soothe:
                        if (me.IsPet() && me.ToPet().IsPetGhoul())
                            me.HandleEmoteCommand( Emote.OneshotOmnicastGhoul);
                        break;
                }
        }

        public override void OnCharmed(bool apply)
        {
            me.NeedChangeAI = true;
            me.IsAIEnabled = false;
        }

        void ClearCharmInfoFlags()
        {
            // Quick access to set all flags to FALSE

            CharmInfo ci = me.GetCharmInfo();

            if (ci != null)
            {
                ci.SetIsAtStay(false);
                ci.SetIsCommandAttack(false);
                ci.SetIsCommandFollow(false);
                ci.SetIsFollowing(false);
                ci.SetIsReturning(false);
            }
        }

        public override void AttackedBy(Unit attacker)
        {
            // Called when pet takes damage. This function helps keep pets from running off
            //  simply due to gaining aggro.

            if (!attacker)
                return;

            // Passive pets don't do anything
            if (me.HasReactState( ReactStates.Passive))
                return;

            // Prevent pet from disengaging from current target
            if (me.GetVictim() && me.GetVictim().IsAlive())
                return;

            // Continue to evaluate and attack if necessary
            AttackStart(attacker);
        }

        // The following aren't used by the PetAI but need to be defined to override
        //  default CreatureAI functions which interfere with the PetAI
        public override void MoveInLineOfSight(Unit who) { }
        public override void MoveInLineOfSight_Safe(Unit who) { }
        public override void EnterEvadeMode(EvadeReason why) { }

        TimeTracker i_tracker;
        List<ObjectGuid> m_AllySet = new List<ObjectGuid>();
        uint m_updateAlliesTimer;
    }
}
