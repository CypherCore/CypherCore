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
using Framework.Dynamic;
using Game.Entities;
using Game.Maps;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class CreatureAI : UnitAI
    {
        public CreatureAI(Creature _creature) : base(_creature)
        {
            me = _creature;
            MoveInLineOfSight_locked = false;
        }

        public override void OnCharmed(bool apply)
        {
            if (apply)
            {
                me.NeedChangeAI = true;
                me.IsAIEnabled = false;
            }
        }

        public void Talk(uint id, WorldObject whisperTarget = null)
        {
            Global.CreatureTextMgr.SendChat(me, (byte)id, whisperTarget);
        }

        public void DoZoneInCombat(Creature creature = null, float maxRangeToNearestTarget = 250.0f)
        {
            if (!creature)
                creature = me;

            if (!creature.CanHaveThreatList())
                return;

            var map = creature.GetMap();
            if (!map.IsDungeon())                                  //use IsDungeon instead of Instanceable, in case Battlegrounds will be instantiated
            {
                Log.outError(LogFilter.Server, "DoZoneInCombat call for map that isn't an instance (creature entry = {0})", creature.IsTypeId(TypeId.Unit) ? creature.ToCreature().GetEntry() : 0);
                return;
            }

            if (!creature.HasReactState(ReactStates.Passive) && creature.GetVictim() == null)
            {
                var nearTarget = creature.SelectNearestTarget(maxRangeToNearestTarget);
                if (nearTarget != null)
                    creature.GetAI().AttackStart(nearTarget);
                else if (creature.IsSummon())
                {
                    var summoner = creature.ToTempSummon().GetSummoner();
                    if (summoner != null)
                    {
                        var target = summoner.GetAttackerForHelper();
                        if (target == null && summoner.CanHaveThreatList() && !summoner.GetThreatManager().IsThreatListEmpty())
                            target = summoner.GetThreatManager().GetHostilTarget();
                        if (target != null && (creature.IsFriendlyTo(summoner) || creature.IsHostileTo(target)))
                            creature.GetAI().AttackStart(target);
                    }
                }
            }

            // Intended duplicated check, the code above this should select a victim
            // If it can't find a suitable attack target then we should error out.
            if (!creature.HasReactState(ReactStates.Passive) && creature.GetVictim() == null)
            {
                Log.outError(LogFilter.Server, "DoZoneInCombat called for creature that has empty threat list (creature entry = {0})", creature.GetEntry());
                return;
            }

            var playerList = map.GetPlayers();
            if (playerList.Empty())
                return;

            foreach (var player in playerList)
            {
                if (player.IsGameMaster())
                    continue;

                if (player.IsAlive())
                {
                    creature.SetInCombatWith(player);
                    player.SetInCombatWith(creature);
                    creature.GetThreatManager().AddThreat(player, 0.0f, null, true, true);
                }
            }
        }

        public virtual void MoveInLineOfSight_Safe(Unit who)
        {
            if (MoveInLineOfSight_locked)
                return;

            MoveInLineOfSight_locked = true;
            MoveInLineOfSight(who);
            MoveInLineOfSight_locked = false;
        }

        public virtual void MoveInLineOfSight(Unit who)
        {
            if (me.IsEngaged())
                return;

            if (me.HasReactState(ReactStates.Aggressive) && me.CanStartAttack(who, false))
                me.EngageWithTarget(who);
        }

        private void _OnOwnerCombatInteraction(Unit target)
        {
            if (target == null || !me.IsAlive())
                return;

            if (!me.HasReactState(ReactStates.Passive) && me.CanStartAttack(target, true))
                me.EngageWithTarget(target);
        }

        // Distract creature, if player gets too close while stealthed/prowling
        public void TriggerAlert(Unit who)
        {
            // If there's no target, or target isn't a player do nothing
            if (!who || !who.IsTypeId(TypeId.Player))
                return;

            // If this unit isn't an NPC, is already distracted, is fighting, is confused, stunned or fleeing, do nothing
            if (!me.IsTypeId(TypeId.Unit) || me.IsEngaged() || me.HasUnitState(UnitState.Confused | UnitState.Stunned | UnitState.Fleeing | UnitState.Distracted))
                return;

            // Only alert for hostiles!
            if (me.IsCivilian() || me.HasReactState(ReactStates.Passive) || !me.IsHostileTo(who) || !me._IsTargetAcceptable(who))
                return;

            // Send alert sound (if any) for this creature
            me.SendAIReaction(AiReaction.Alert);

            // Face the unit (stealthed player) and set distracted state for 5 seconds
            me.GetMotionMaster().MoveDistract(5 * Time.InMilliseconds);
            me.StopMoving();
            me.SetFacingTo(me.GetAngle(who));
        }

        // Called for reaction at stopping attack at no attackers or targets
        public virtual void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            if (!_EnterEvadeMode(why))
                return;

            Log.outDebug( LogFilter.Unit, "Creature {0} enters evade mode.", me.GetEntry());

            if (me.GetVehicle() == null) // otherwise me will be in evade mode forever
            {
                var owner = me.GetCharmerOrOwner();
                if (owner != null)
                {
                    me.GetMotionMaster().Clear(false);
                    me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, me.GetFollowAngle(), MovementSlot.Active);
                }
                else
                {
                    // Required to prevent attacking creatures that are evading and cause them to reenter combat
                    // Does not apply to MoveFollow
                    me.AddUnitState(UnitState.Evade);
                    me.GetMotionMaster().MoveTargetedHome();
                }
            }

            Reset();

            if (me.IsVehicle()) // use the same sequence of addtoworld, aireset may remove all summons!
                me.GetVehicleKit().Reset(true);
        }

        private void SetGazeOn(Unit target)
        {
            if (me.IsValidAttackTarget(target) && target != me.GetVictim())
            {
                if (!me.IsFocusing(null, true))
                    AttackStart(target);
                me.SetReactState(ReactStates.Passive);
            }
        }

        public bool UpdateVictimWithGaze()
        {
            if (!me.IsEngaged())
                return false;

            if (me.HasReactState(ReactStates.Passive))
            {
                if (me.GetVictim() != null)
                    return true;
                else
                    me.SetReactState(ReactStates.Aggressive);
            }

            var victim = me.SelectVictim();
            if (victim != null)
            {
                if (!me.IsFocusing(null, true) && victim != me.GetVictim())
                    AttackStart(victim);
            }

            return me.GetVictim() != null;
        }

        public bool UpdateVictim()
        {
            if (!me.IsEngaged())
                return false;

            if (!me.HasReactState(ReactStates.Passive))
            {
                var victim = me.SelectVictim();
                if (victim != null)
                    if (!me.IsFocusing(null, true) && victim != me.GetVictim())
                        AttackStart(victim);

                return me.GetVictim() != null;
            }
            else if (me.GetThreatManager().IsThreatListEmpty())
            {
                EnterEvadeMode(EvadeReason.NoHostiles);
                return false;
            }

            return true;
        }

        public bool _EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            if (!me.IsAlive())
                return false;

            me.RemoveAurasOnEvade();

            // sometimes bosses stuck in combat?
            me.GetThreatManager().ClearAllThreat();
            me.CombatStop(true);
            me.SetLootRecipient(null);
            me.ResetPlayerDamageReq();
            me.SetLastDamagedTime(0);
            me.SetCannotReachTarget(false);
            me.DoNotReacquireTarget();

            if (me.IsInEvadeMode())
                return false;

            return true;
        }

        public CypherStrings VisualizeBoundary(int duration, Unit owner = null, bool fill = false)
        {
            if (!owner)
                return 0;

            if (_boundary.Empty())
                return CypherStrings.CreatureMovementNotBounded;

            var Q = new List<KeyValuePair<int, int>>();
            var alreadyChecked = new List<KeyValuePair<int, int>>();
            var outOfBounds = new List<KeyValuePair<int, int>>();

            var startPosition = owner.GetPosition();
            if (!CheckBoundary(startPosition)) // fall back to creature position
            {
                startPosition = me.GetPosition();
                if (!CheckBoundary(startPosition))
                {
                    startPosition = me.GetHomePosition();
                    if (!CheckBoundary(startPosition)) // fall back to creature home position
                        return CypherStrings.CreatureNoInteriorPointFound;
                }
            }
            var spawnZ = startPosition.GetPositionZ() + SharedConst.BoundaryVisualizeSpawnHeight;

            var boundsWarning = false;
            Q.Add(new KeyValuePair<int, int>(0, 0));
            while (!Q.Empty())
            {
                var front = Q.First();
                var hasOutOfBoundsNeighbor = false;
                foreach (var off in new List<KeyValuePair<int, int>>() { new KeyValuePair<int, int>(1, 0), new KeyValuePair<int, int>(0, 1), new KeyValuePair<int, int>(-1, 0), new KeyValuePair<int, int>(0, -1) })
                {
                    var next = new KeyValuePair<int, int>(front.Key + off.Key, front.Value + off.Value);
                    if (next.Key > SharedConst.BoundaryVisualizeFailsafeLimit || next.Key < -SharedConst.BoundaryVisualizeFailsafeLimit || next.Value > SharedConst.BoundaryVisualizeFailsafeLimit || next.Value < -SharedConst.BoundaryVisualizeFailsafeLimit)
                    {
                        boundsWarning = true;
                        continue;
                    }
                    if (!alreadyChecked.Contains(next)) // never check a coordinate twice
                    {
                        var nextPos = new Position(startPosition.GetPositionX() + next.Key * SharedConst.BoundaryVisualizeStepSize, startPosition.GetPositionY() + next.Value * SharedConst.BoundaryVisualizeStepSize, startPosition.GetPositionZ());
                        if (CheckBoundary(nextPos))
                            Q.Add(next);
                        else
                        {
                            outOfBounds.Add(next);
                            hasOutOfBoundsNeighbor = true;
                        }
                        alreadyChecked.Add(next);
                    }
                    else
                    {
                        if (outOfBounds.Contains(next))
                            hasOutOfBoundsNeighbor = true;
                    }
                }
                if (fill || hasOutOfBoundsNeighbor)
                {
                    var pos = new Position(startPosition.GetPositionX() + front.Key * SharedConst.BoundaryVisualizeStepSize, startPosition.GetPositionY() + front.Value * SharedConst.BoundaryVisualizeStepSize, spawnZ);
                    var point = owner.SummonCreature(SharedConst.BoundaryVisualizeCreature, pos, TempSummonType.TimedDespawn, (uint)(duration * Time.InMilliseconds));
                    if (point)
                    {
                        point.SetObjectScale(SharedConst.BoundaryVisualizeCreatureScale);
                        point.AddUnitFlag(UnitFlags.ImmuneToPc | UnitFlags.Stunned | UnitFlags.ImmuneToNpc);
                        if (!hasOutOfBoundsNeighbor)
                            point.AddUnitFlag(UnitFlags.NotSelectable);
                    }
                    Q.Remove(front);
                }
            }
            return boundsWarning ? CypherStrings.CreatureMovementMaybeUnbounded : 0;
        }

        public bool CheckBoundary(Position who = null)
        {
            if (_boundary == null)
                return true;

            if (who == null)
                who = me;

            return IsInBounds(_boundary, who) != _negateBoundary;
        }

        public bool CheckInRoom()
        {
            if (CheckBoundary())
                return true;
            else
            {
                EnterEvadeMode(EvadeReason.Boundary);
                return false;
            }
        }

        public Creature DoSummon(uint entry, Position pos, uint despawnTime = 30000, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
        {
            return me.SummonCreature(entry, pos, summonType, despawnTime);
        }

        public Creature DoSummon(uint entry, WorldObject obj, float radius = 5.0f, uint despawnTime = 30000, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
        {
            var pos = obj.GetRandomNearPosition(radius);
            return me.SummonCreature(entry, pos, summonType, despawnTime);
        }

        public Creature DoSummonFlyer(uint entry, WorldObject obj, float flightZ, float radius = 5.0f, uint despawnTime = 30000, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
        {
            var pos = obj.GetRandomNearPosition(radius);
            pos.posZ += flightZ;
            return me.SummonCreature(entry, pos, summonType, despawnTime);
        }

        public static bool IsInBounds(List<AreaBoundary> boundary, Position pos)
        {
            foreach (var areaBoundary in boundary)
                if (!areaBoundary.IsWithinBoundary(pos))
                    return false;

            return true;
        }
        
        public void SetBoundary(List<AreaBoundary> boundary, bool negateBoundaries = false)
        {
            _boundary = boundary;
            _negateBoundary = negateBoundaries;
            me.DoImmediateBoundaryCheck();
        }

        /// <summary>
        /// Called for reaction when initially engaged
        /// </summary>
        /// <param name="victim"></param>
        public virtual void EnterCombat(Unit victim) { }

        // Called when the creature is killed
        public virtual void JustDied(Unit killer) { }

        // Called when the creature kills a unit
        public virtual void KilledUnit(Unit victim) {}
        
        // Called when the creature summon successfully other creature
        public virtual void JustSummoned(Creature summon) { }
        public virtual void IsSummonedBy(Unit summoner) { }

        public virtual void SummonedCreatureDespawn(Creature summon) { }
        public virtual void SummonedCreatureDies(Creature summon, Unit killer) { }

        // Called when the creature successfully summons a gameobject
        public virtual void JustSummonedGameobject(GameObject gameobject) { }
        public virtual void SummonedGameobjectDespawn(GameObject gameobject) { }

        // Called when the creature successfully registers a dynamicobject
        public virtual void JustRegisteredDynObject(DynamicObject dynObject) { }
        public virtual void JustUnregisteredDynObject(DynamicObject dynObject) { }

        // Called when the creature successfully registers an areatrigger
        public virtual void JustRegisteredAreaTrigger(AreaTrigger areaTrigger) { }
        public virtual void JustUnregisteredAreaTrigger(AreaTrigger areaTrigger) { }

        // Called when hit by a spell
        public virtual void SpellHit(Unit caster, SpellInfo spell) {}

        // Called when spell hits a target
        public virtual void SpellHitTarget(Unit target, SpellInfo spell) {}
        
        public virtual bool IsEscorted() { return false; }

        // Called when creature appears in the world (spawn, respawn, grid load etc...)
        public virtual void JustAppeared() { }

        public virtual void MovementInform(MovementGeneratorType type, uint id) { }

        // Called when a spell cast gets interrupted
        public virtual void OnSpellCastInterrupt(SpellInfo spell) { }

        // Called when a spell cast has been successfully finished
        public virtual void OnSuccessfulSpellCast(SpellInfo spell) { }

        // Called at reaching home after evade
        public virtual void JustReachedHome() { }
        
        // Called at text emote receive from player
        public virtual void ReceiveEmote(Player player, TextEmotes emoteId) { }

        // Called when owner takes damage
        public virtual void OwnerAttackedBy(Unit attacker) { _OnOwnerCombatInteraction(attacker); }

        // Called when owner attacks something
        public virtual void OwnerAttacked(Unit target) { _OnOwnerCombatInteraction(target); }

        // called when the corpse of this creature gets removed
        public virtual void CorpseRemoved(long respawnDelay) {}

        public virtual void PassengerBoarded(Unit passenger, sbyte seatId, bool apply) { }

        public virtual void OnSpellClick(Unit clicker, ref bool result) { }

        public virtual bool CanSeeAlways(WorldObject obj) { return false; }

        // Called when a player is charmed by the creature
        // If a PlayerAI* is returned, that AI is placed on the player instead of the default charm AI
        // Object destruction is handled by Unit::RemoveCharmedBy
        public virtual PlayerAI GetAIForCharmedPlayer(Player who) { return null; }


        /// <summary>
        /// Should return true if the NPC is target of an escort quest
        /// If onlyIfActive is set, should return true only if the escort quest is currently active
        /// </summary>
        /// <param name="onlyIfActive"></param>
        /// <returns></returns>
        public virtual bool IsEscortNPC(bool onlyIfActive)  { return false;    }

        private List<AreaBoundary> GetBoundary() { return _boundary; }

        private bool MoveInLineOfSight_locked;
        protected new Creature me;
        private List<AreaBoundary> _boundary = new List<AreaBoundary>();
        private bool _negateBoundary;

        protected EventMap _events = new EventMap();
        protected TaskScheduler _scheduler = new TaskScheduler();
        protected InstanceScript _instance;
    }

    public struct AISpellInfoType
    {
        public AITarget target;
        public AICondition condition;
        public uint cooldown;
        public uint realCooldown;
        public float maxRange;

        public byte Targets;                                          // set of enum SelectTarget
        public byte Effects;                                          // set of enum SelectEffect
    }

    public enum AITarget
    {
        Self,
        Victim,
        Enemy,
        Ally,
        Buff,
        Debuff
    }

    public enum AICondition
    {
        Aggro,
        Combat,
        Die
    }
}
