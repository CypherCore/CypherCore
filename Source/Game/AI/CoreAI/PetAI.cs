// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Movement;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.AI
{
    public class PetAI : CreatureAI
    {
        List<ObjectGuid> _allySet = new();
        uint _updateAlliesTimer;

        public PetAI(Creature creature) : base(creature)
        {
            UpdateAllies();
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.IsAlive() || me.GetCharmInfo() == null)
                return;

            Unit owner = me.GetCharmerOrOwner();

            if (_updateAlliesTimer <= diff)
                // UpdateAllies self set update timer
                UpdateAllies();
            else
                _updateAlliesTimer -= diff;

            if (me.GetVictim() != null && me.GetVictim().IsAlive())
            {
                // is only necessary to stop casting, the pet must not exit combat
                if (me.GetCurrentSpell(CurrentSpellTypes.Channeled) == null && // ignore channeled spells (Pin, Seduction)
                    (me.GetVictim() != null && me.GetVictim().HasBreakableByDamageCrowdControlAura(me)))
                {
                    me.InterruptNonMeleeSpells(false);
                    return;
                }

                if (NeedToStop())
                {
                    Log.outTrace(LogFilter.ScriptsAi, $"PetAI::UpdateAI: AI stopped attacking {me.GetGUID()}");
                    StopAttack();
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
                    // All other cases (ie: defensive) - Targets are assigned by DamageTaken(), OwnerAttackedBy(), OwnerAttacked(), etc.
                    Unit nextTarget = SelectNextTarget(me.HasReactState(ReactStates.Aggressive));

                    if (nextTarget != null)
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
                List<Tuple<Unit, Spell>> targetSpellStore = new();

                for (byte i = 0; i < me.GetPetAutoSpellSize(); ++i)
                {
                    uint spellID = me.GetPetAutoSpellOnPos(i);
                    if (spellID == 0)
                        continue;

                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellID, me.GetMap().GetDifficultyID());
                    if (spellInfo == null)
                        continue;

                    if (me.GetSpellHistory().HasGlobalCooldown(spellInfo))
                        continue;

                    // check spell cooldown
                    if (!me.GetSpellHistory().IsReady(spellInfo))
                        continue;

                    if (spellInfo.IsPositive())
                    {
                        if (spellInfo.CanBeUsedInCombat(me))
                        {
                            // Check if we're in combat or commanded to attack
                            if (!me.IsInCombat() && !me.GetCharmInfo().IsCommandAttack())
                                continue;
                        }

                        Spell spell = new(me, spellInfo, TriggerCastFlags.None);
                        bool spellUsed = false;

                        // Some spells can target enemy or friendly (DK Ghoul's Leap)
                        // Check for enemy first (pet then owner)
                        Unit target = me.GetAttackerForHelper();
                        if (target == null && owner != null)
                            target = owner.GetAttackerForHelper();

                        if (target != null)
                        {
                            if (CanAttack(target) && spell.CanAutoCast(target))
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
                            foreach (var tar in _allySet)
                            {
                                Unit ally = Global.ObjAccessor.GetUnit(me, tar);

                                //only buff targets that are in combat, unless the spell can only be cast while out of combat
                                if (ally == null)
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
                    else if (me.GetVictim() != null && CanAttack(me.GetVictim()) && spellInfo.CanBeUsedInCombat(me))
                    {
                        Spell spell = new(me, spellInfo, TriggerCastFlags.None);
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

                    SpellCastTargets targets = new();
                    targets.SetUnitTarget(target);

                    spell.Prepare(targets);
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

        public override void KilledUnit(Unit victim)
        {
            // Called from Unit.Kill() in case where pet or owner kills something
            // if owner killed this victim, pet may still be attacking something else
            if (me.GetVictim() != null && me.GetVictim() != victim)
                return;

            // Clear target just in case. May help problem where health / focus / mana
            // regen gets stuck. Also resets attack command.
            // Can't use StopAttack() because that activates movement handlers and ignores
            // next target selection
            me.AttackStop();
            me.InterruptNonMeleeSpells(false);

            // Before returning to owner, see if there are more things to attack
            Unit nextTarget = SelectNextTarget(false);
            if (nextTarget != null)
                AttackStart(nextTarget);
            else
                HandleReturnMovement(); // Return
        }

        public override void AttackStart(Unit target)
        {
            // Overrides Unit.AttackStart to prevent pet from switching off its assigned target
            if (target == null || target == me)
                return;

            if (me.GetVictim() != null && me.GetVictim().IsAlive())
                return;

            _AttackStart(target);
        }

        public void _AttackStart(Unit target)
        {
            // Check all pet states to decide if we can attack this target
            if (!CanAttack(target))
                return;

            // Only chase if not commanded to stay or if stay but commanded to attack
            DoAttack(target, (!me.GetCharmInfo().HasCommandState(CommandStates.Stay) || me.GetCharmInfo().IsCommandAttack()));
        }

        public override void OwnerAttackedBy(Unit attacker)
        {
            // Called when owner takes damage. This function helps keep pets from running off
            //  simply due to owner gaining aggro.

            if (attacker == null || !me.IsAlive())
                return;

            // Passive pets don't do anything
            if (me.HasReactState(ReactStates.Passive))
                return;

            // Prevent pet from disengaging from current target
            if (me.GetVictim() != null && me.GetVictim().IsAlive())
                return;

            // Continue to evaluate and attack if necessary
            AttackStart(attacker);
        }

        public override void OwnerAttacked(Unit target)
        {
            // Called when owner attacks something. Allows defensive pets to know
            //  that they need to assist

            // Target might be null if called from spell with invalid cast targets
            if (target == null || !me.IsAlive())
                return;

            // Passive pets don't do anything
            if (me.HasReactState(ReactStates.Passive))
                return;

            // Prevent pet from disengaging from current target
            if (me.GetVictim() != null && me.GetVictim().IsAlive())
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
            Unit myAttacker = me.GetAttackerForHelper();
            if (myAttacker != null)
                if (!myAttacker.HasBreakableByDamageCrowdControlAura())
                    return myAttacker;

            // Not sure why we wouldn't have an owner but just in case...
            if (me.GetCharmerOrOwner() == null)
                return null;

            // Check owner attackers
            Unit ownerAttacker = me.GetCharmerOrOwner().GetAttackerForHelper();
            if (ownerAttacker != null)
                if (!ownerAttacker.HasBreakableByDamageCrowdControlAura())
                    return ownerAttacker;

            // Check owner victim
            // 3.0.2 - Pets now start attacking their owners victim in defensive mode as soon as the hunter does
            Unit ownerVictim = me.GetCharmerOrOwner().GetVictim();
            if (ownerVictim != null)
                return ownerVictim;

            // Neither pet or owner had a target and aggressive pets can pick any target
            // To prevent aggressive pets from chain selecting targets and running off, we
            //  only select a random target if certain conditions are met.
            if (me.HasReactState(ReactStates.Aggressive) && allowAutoSelect)
            {
                if (!me.GetCharmInfo().IsReturning() || me.GetCharmInfo().IsFollowing() || me.GetCharmInfo().IsAtStay())
                {
                    Unit nearTarget = me.SelectNearestHostileUnitInAggroRange(true, true);
                    if (nearTarget != null)
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

            if (me.GetCharmInfo() == null)
            {
                Log.outWarn(LogFilter.ScriptsAi, $"me.GetCharmInfo() is NULL in PetAI::HandleReturnMovement(). Debug info: {GetDebugInfo()}");
                return;
            }

            if (me.GetCharmInfo().HasCommandState(CommandStates.Stay))
            {
                if (!me.GetCharmInfo().IsAtStay() && !me.GetCharmInfo().IsReturning())
                {
                    // Return to previous position where stay was clicked
                    float x, y, z;

                    me.GetCharmInfo().GetStayPosition(out x, out y, out z);
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsReturning(true);

                    if (me.HasUnitState(UnitState.Chase))
                        me.GetMotionMaster().Remove(MovementGeneratorType.Chase);

                    me.GetMotionMaster().MovePoint((uint)me.GetGUID().GetCounter(), x, y, z);
                }
            }
            else // COMMAND_FOLLOW
            {
                if (!me.GetCharmInfo().IsFollowing() && !me.GetCharmInfo().IsReturning())
                {
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsReturning(true);

                    if (me.HasUnitState(UnitState.Chase))
                        me.GetMotionMaster().Remove(MovementGeneratorType.Chase);

                    me.GetMotionMaster().MoveFollow(me.GetCharmerOrOwner(), SharedConst.PetFollowDist, me.GetFollowAngle());
                }
            }

            me.RemoveUnitFlag(UnitFlags.PetInCombat); // on player pets, this flag indicates that we're actively going after a target - we're returning, so remove it
        }

        void DoAttack(Unit target, bool chase)
        {
            // Handles attack with or without chase and also resets flags
            // for next update / creature kill

            if (me.Attack(target, true))
            {
                me.SetUnitFlag(UnitFlags.PetInCombat); // on player pets, this flag indicates we're actively going after a target - that's what we're doing, so set it

                // Play sound to let the player know the pet is attacking something it picked on its own
                if (me.HasReactState(ReactStates.Aggressive) && !me.GetCharmInfo().IsCommandAttack())
                    me.SendPetAIReaction(me.GetGUID());

                if (chase)
                {
                    bool oldCmdAttack = me.GetCharmInfo().IsCommandAttack(); // This needs to be reset after other flags are cleared
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsCommandAttack(oldCmdAttack); // For passive pets commanded to attack so they will use spells

                    if (me.HasUnitState(UnitState.Follow))
                        me.GetMotionMaster().Remove(MovementGeneratorType.Follow);

                    // Pets with ranged attacks should not care about the chase angle at all.
                    float chaseDistance = me.GetPetChaseDistance();
                    float angle = chaseDistance == 0.0f ? MathF.PI : 0.0f;
                    float tolerance = chaseDistance == 0.0f ? MathFunctions.PiOver4 : (MathF.PI * 2);
                    me.GetMotionMaster().MoveChase(target, new ChaseRange(0.0f, chaseDistance), new ChaseAngle(angle, tolerance));
                }
                else
                {
                    ClearCharmInfoFlags();
                    me.GetCharmInfo().SetIsAtStay(true);

                    if (me.HasUnitState(UnitState.Follow))
                        me.GetMotionMaster().Remove(MovementGeneratorType.Follow);

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
                        me.GetMotionMaster().MoveIdle();
                    }
                    break;
                }
                case MovementGeneratorType.Follow:
                {
                    // If data is owner's GUIDLow then we've reached follow point,
                    // otherwise we're probably chasing a creature
                    if (me.GetCharmerOrOwner() != null && me.GetCharmInfo() != null && id == me.GetCharmerOrOwner().GetGUID().GetCounter() && me.GetCharmInfo().IsReturning())
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

        public bool CanAttack(Unit victim)
        {
            // Evaluates wether a pet can attack a specific target based on CommandState, ReactState and other flags
            // IMPORTANT: The order in which things are checked is important, be careful if you add or remove checks

            // Hmmm...
            if (victim == null) 
                return false;

            if (!victim.IsAlive())
            {
                // if target is invalid, pet should evade automaticly
                // Clear target to prevent getting stuck on dead targets
                //me.AttackStop();
                //me.InterruptNonMeleeSpells(false);
                return false;
            }

            if (me.GetCharmInfo() == null)
            {
                Log.outWarn(LogFilter.ScriptsAi, $"me.GetCharmInfo() is NULL in PetAI::CanAttack(). Debug info: {GetDebugInfo()}");
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
            if (me.GetVictim() != null && me.GetVictim() != victim)
            {
                // Check if our owner selected this target and clicked "attack"
                Unit ownerTarget;
                Player owner = me.GetCharmerOrOwner().ToPlayer();
                if (owner != null)
                    ownerTarget = owner.GetSelectedUnit();
                else
                    ownerTarget = me.GetCharmerOrOwner().GetVictim();

                if (ownerTarget != null && me.GetCharmInfo().IsCommandAttack())
                    return victim.GetGUID() == ownerTarget.GetGUID();
            }

            // Follow
            if (me.GetCharmInfo().HasCommandState(CommandStates.Follow))
                return !me.GetCharmInfo().IsReturning();

            // default, though we shouldn't ever get here
            return false;
        }

        public override void ReceiveEmote(Player player, TextEmotes emoteId)
        {
            if (me.GetOwnerGUID() != player.GetGUID())
                return;

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
                        me.HandleEmoteCommand(Emote.OneshotOmnicastGhoul);
                    break;
            }
        }

        bool NeedToStop()
        {
            // This is needed for charmed creatures, as once their target was reset other effects can trigger threat
            if (me.IsCharmed() && me.GetVictim() == me.GetCharmer())
                return true;

            // dont allow pets to follow targets far away from owner
            Unit owner = me.GetCharmerOrOwner();
            if (owner != null)
                if (owner.GetExactDist(me) >= (owner.GetVisibilityRange() - 10.0f))
                    return true;

            return !me.IsValidAttackTarget(me.GetVictim());
        }

        void StopAttack()
        {
            if (!me.IsAlive())
            {
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MoveIdle();
                me.CombatStop();
                return;
            }

            me.AttackStop();
            me.InterruptNonMeleeSpells(false);
            me.GetCharmInfo().SetIsCommandAttack(false);
            ClearCharmInfoFlags();
            HandleReturnMovement();
        }

        void UpdateAllies()
        {
            _updateAlliesTimer = 10 * Time.InMilliseconds;                 // update friendly targets every 10 seconds, lesser checks increase performance

            Unit owner = me.GetCharmerOrOwner();
            if (owner == null)
                return;

            Group group = null;
            Player player = owner.ToPlayer();
            if (player != null)
                group = player.GetGroup();

            // only pet and owner/not in group.ok
            if (_allySet.Count == 2 && group == null)
                return;

            // owner is in group; group members filled in already (no raid . subgroupcount = whole count)
            if (group != null && !group.IsRaidGroup() && _allySet.Count == (group.GetMembersCount() + 2))
                return;

            _allySet.Clear();
            _allySet.Add(me.GetGUID());
            if (group != null) // add group
            {
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player target = refe.GetSource();
                    if (target == null || !target.IsInMap(owner) || !group.SameSubGroup(owner.ToPlayer(), target))
                        continue;

                    if (target.GetGUID() == owner.GetGUID())
                        continue;

                    _allySet.Add(target.GetGUID());
                }
            }
            else // remove group
                _allySet.Add(owner.GetGUID());
        }

        public override void OnCharmed(bool isNew)
        {
            if (!me.IsPossessedByPlayer() && me.IsCharmed())
                me.GetMotionMaster().MoveFollow(me.GetCharmer(), SharedConst.PetFollowDist, me.GetFollowAngle());

            base.OnCharmed(isNew);
        }

        /// <summary>
        /// Quick access to set all flags to FALSE
        /// </summary>
        void ClearCharmInfoFlags()
        {
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

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            AttackStart(attacker);
        }

        public override void JustEnteredCombat(Unit who)
        {
            EngagementStart(who);
        }

        public override void JustExitedCombat()
        {
            EngagementOver();
        }

        // The following aren't used by the PetAI but need to be defined to override
        //  default CreatureAI functions which interfere with the PetAI
        public override void MoveInLineOfSight(Unit who) { }
        public override void MoveInLineOfSight_Safe(Unit who) { }
        public override void JustAppeared() { } // we will control following manually
        public override void EnterEvadeMode(EvadeReason why) { }
    }
}
