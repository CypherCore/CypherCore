// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Combat;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class CreatureAI : UnitAI
    {
        bool _isEngaged;
        bool _moveInLOSLocked;
        List<AreaBoundary> _boundary = new();
        bool _negateBoundary;

        protected new Creature me;

        protected EventMap _events = new();
        protected TaskScheduler _scheduler = new();
        protected InstanceScript _instanceScript;

        public CreatureAI(Creature _creature) : base(_creature)
        {
            me = _creature;
            _moveInLOSLocked = false;
        }

        public void Talk(uint id, WorldObject whisperTarget = null)
        {
            Global.CreatureTextMgr.SendChat(me, (byte)id, whisperTarget);
        }

        public override void OnCharmed(bool isNew)
        {
            if (isNew && !me.IsCharmed() && !me.LastCharmerGUID.IsEmpty())
            {
                if (!me.HasReactState(ReactStates.Passive))
                {
                    Unit lastCharmer = Global.ObjAccessor.GetUnit(me, me.LastCharmerGUID);
                    if (lastCharmer != null)
                        me.EngageWithTarget(lastCharmer);
                }

                me.LastCharmerGUID.Clear();
            }

            base.OnCharmed(isNew);
        }

        public void DoZoneInCombat(Creature creature = null)
        {
            if (!creature)
                creature = me;

            Map map = creature.GetMap();
            if (!map.IsDungeon()) // use IsDungeon instead of Instanceable, in case Battlegrounds will be instantiated
            {
                Log.outError(LogFilter.Server, "DoZoneInCombat call for map that isn't an instance (creature entry = {0})", creature.IsTypeId(TypeId.Unit) ? creature.ToCreature().GetEntry() : 0);
                return;
            }

            if (!map.HavePlayers())
                return;

            foreach (var player in map.GetPlayers())
            {
                if (player != null)
                {
                    if (!player.IsAlive() || !CombatManager.CanBeginCombat(creature, player))
                        continue;

                    creature.EngageWithTarget(player);
                    foreach (Unit pet in player.m_Controlled)
                        creature.EngageWithTarget(pet);

                    Unit vehicle = player.GetVehicleBase();
                    if (vehicle != null)
                        creature.EngageWithTarget(vehicle);
                }
            }
        }

        public virtual void MoveInLineOfSight_Safe(Unit who)
        {
            if (_moveInLOSLocked)
                return;

            _moveInLOSLocked = true;
            MoveInLineOfSight(who);
            _moveInLOSLocked = false;
        }

        public virtual void MoveInLineOfSight(Unit who)
        {
            if (me.IsEngaged())
                return;

            if (me.HasReactState(ReactStates.Aggressive) && me.CanStartAttack(who, false))
                me.EngageWithTarget(who);
        }

        void OnOwnerCombatInteraction(Unit target)
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
            me.GetMotionMaster().MoveDistract(5 * Time.InMilliseconds, me.GetAbsoluteAngle(who));
        }

        // adapted from logic in Spell:EffectSummonType
        public static bool ShouldFollowOnSpawn(SummonPropertiesRecord properties)
        {
            // Summons without SummonProperties are generally scripted summons that don't belong to any owner
            if (properties == null)
                return false;

            switch (properties.Control)
            {
                case SummonCategory.Pet:
                    return true;
                case SummonCategory.Wild:
                case SummonCategory.Ally:
                case SummonCategory.Unk:
                    if (properties.GetFlags().HasFlag(SummonPropertiesFlags.JoinSummonerSpawnGroup))
                        return true;
                    switch (properties.Title)
                    {
                        case SummonTitle.Pet:
                        case SummonTitle.Guardian:
                        case SummonTitle.Runeblade:
                        case SummonTitle.Minion:
                        case SummonTitle.Companion:
                            return true;
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        // Called when creature appears in the world (spawn, respawn, grid load etc...)
        public virtual void JustAppeared()
        {
            if (!IsEngaged())
            {
                TempSummon summon = me.ToTempSummon();
                if (summon != null)
                {
                    // Only apply this to specific types of summons
                    if (!summon.GetVehicle() && ShouldFollowOnSpawn(summon.m_Properties) && summon.CanFollowOwner())
                    {
                        Unit owner = summon.GetCharmerOrOwner();
                        if (owner != null)
                        {
                            summon.GetMotionMaster().Clear();
                            summon.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, summon.GetFollowAngle());
                        }
                    }
                }
            }
        }

        public override void JustEnteredCombat(Unit who)
        {
            if (!IsEngaged() && !me.CanHaveThreatList())
                EngagementStart(who);
        }

        // Called for reaction at stopping attack at no attackers or targets
        public virtual void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            if (!_EnterEvadeMode(why))
                return;

            Log.outDebug(LogFilter.Unit, $"CreatureAI::EnterEvadeMode: entering evade mode (why: {why}) ({me.GetGUID()})");

            if (me.GetVehicle() == null) // otherwise me will be in evade mode forever
            {
                Unit owner = me.GetCharmerOrOwner();
                if (owner != null)
                {
                    me.GetMotionMaster().Clear();
                    me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, me.GetFollowAngle());
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
        }

        public bool UpdateVictim()
        {
            if (!IsEngaged())
                return false;

            if (!me.IsAlive())
            {
                EngagementOver();
                return false;
            }

            if (!me.HasReactState(ReactStates.Passive))
            {
                Unit victim = me.SelectVictim();
                if (victim != null && victim != me.GetVictim())
                    AttackStart(victim);

                return me.GetVictim() != null;
            }
            else if (!me.IsInCombat())
            {
                EnterEvadeMode(EvadeReason.NoHostiles);
                return false;
            }
            else if (me.GetVictim() != null)
                me.AttackStop();

            return true;
        }

        public void EngagementStart(Unit who)
        {
            if (_isEngaged)
            {
                Log.outError(LogFilter.ScriptsAi, $"CreatureAI::EngagementStart called even though creature is already engaged. Creature debug info:\n{me.GetDebugInfo()}");
                return;
            }
            _isEngaged = true;

            me.AtEngage(who);
        }

        public void EngagementOver()
        {
            if (!_isEngaged)
            {
                Log.outDebug(LogFilter.ScriptsAi, $"CreatureAI::EngagementOver called even though creature is not currently engaged. Creature debug info:\n{me.GetDebugInfo()}");
                return;
            }
            _isEngaged = false;

            me.AtDisengage();
        }

        public bool _EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            if (me.IsInEvadeMode())
                return false;

            if (!me.IsAlive())
            {
                EngagementOver();
                return false;
            }

            me.RemoveAurasOnEvade();

            // sometimes bosses stuck in combat?
            me.CombatStop(true);
            me.SetTappedBy(null);
            me.ResetPlayerDamageReq();
            me.SetLastDamagedTime(0);
            me.SetCannotReachTarget(false);
            me.DoNotReacquireSpellFocusTarget();
            me.SetTarget(ObjectGuid.Empty);
            me.GetSpellHistory().ResetAllCooldowns();
            EngagementOver();

            return true;
        }

        public CypherStrings VisualizeBoundary(TimeSpan duration, Unit owner = null, bool fill = false)
        {
            if (!owner)
                return 0;

            if (_boundary.Empty())
                return CypherStrings.CreatureMovementNotBounded;

            List<KeyValuePair<int, int>> Q = new();
            List<KeyValuePair<int, int>> alreadyChecked = new();
            List<KeyValuePair<int, int>> outOfBounds = new();

            Position startPosition = owner.GetPosition();
            if (!IsInBoundary(startPosition)) // fall back to creature position
            {
                startPosition = me.GetPosition();
                if (!IsInBoundary(startPosition))
                {
                    startPosition = me.GetHomePosition();
                    if (!IsInBoundary(startPosition)) // fall back to creature home position
                        return CypherStrings.CreatureNoInteriorPointFound;
                }
            }
            float spawnZ = startPosition.GetPositionZ() + SharedConst.BoundaryVisualizeSpawnHeight;

            bool boundsWarning = false;
            Q.Add(new KeyValuePair<int, int>(0, 0));
            while (!Q.Empty())
            {
                var front = Q.First();
                bool hasOutOfBoundsNeighbor = false;
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
                        Position nextPos = new(startPosition.GetPositionX() + next.Key * SharedConst.BoundaryVisualizeStepSize, startPosition.GetPositionY() + next.Value * SharedConst.BoundaryVisualizeStepSize, startPosition.GetPositionZ());
                        if (IsInBoundary(nextPos))
                            Q.Add(next);
                        else
                        {
                            outOfBounds.Add(next);
                            hasOutOfBoundsNeighbor = true;
                        }
                        alreadyChecked.Add(next);
                    }
                    else if (outOfBounds.Contains(next))
                        hasOutOfBoundsNeighbor = true;
                }

                if (fill || hasOutOfBoundsNeighbor)
                {
                    var pos = new Position(startPosition.GetPositionX() + front.Key * SharedConst.BoundaryVisualizeStepSize, startPosition.GetPositionY() + front.Value * SharedConst.BoundaryVisualizeStepSize, spawnZ);
                    TempSummon point = owner.SummonCreature(SharedConst.BoundaryVisualizeCreature, pos, TempSummonType.TimedDespawn, duration);
                    if (point)
                    {
                        point.SetObjectScale(SharedConst.BoundaryVisualizeCreatureScale);
                        point.SetUnitFlag(UnitFlags.Stunned);
                        point.SetImmuneToAll(true);
                        if (!hasOutOfBoundsNeighbor)
                            point.SetUnitFlag(UnitFlags.Uninteractible);
                    }
                    Q.Remove(front);
                }
            }
            return boundsWarning ? CypherStrings.CreatureMovementMaybeUnbounded : 0;
        }

        public bool IsInBoundary(Position who = null)
        {
            if (_boundary == null)
                return true;

            if (who == null)
                who = me;

            return IsInBounds(_boundary, who) != _negateBoundary;
        }

        public virtual bool CheckInRoom()
        {
            if (IsInBoundary())
                return true;
            else
            {
                EnterEvadeMode(EvadeReason.Boundary);
                return false;
            }
        }

        public Creature DoSummon(uint entry, Position pos, TimeSpan despawnTime, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
        {
            return me.SummonCreature(entry, pos, summonType, despawnTime);
        }

        public Creature DoSummon(uint entry, WorldObject obj, float radius = 5.0f, TimeSpan despawnTime = default, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
        {
            Position pos = obj.GetRandomNearPosition(radius);
            return me.SummonCreature(entry, pos, summonType, despawnTime);
        }

        public Creature DoSummonFlyer(uint entry, WorldObject obj, float flightZ, float radius = 5.0f, TimeSpan despawnTime = default, TempSummonType summonType = TempSummonType.CorpseTimedDespawn)
        {
            Position pos = obj.GetRandomNearPosition(radius);
            pos.posZ += flightZ;
            return me.SummonCreature(entry, pos, summonType, despawnTime);
        }

        public static bool IsInBounds(List<AreaBoundary> boundary, Position pos)
        {
            foreach (AreaBoundary areaBoundary in boundary)
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

        // Called for reaction whenever a new non-offline unit is added to the threat list
        public virtual void JustStartedThreateningMe(Unit who)
        {
            if (!IsEngaged())
                EngagementStart(who);
        }

        // Called for reaction when initially engaged - this will always happen _after_ JustEnteredCombat
        public virtual void JustEngagedWith(Unit who) { }

        // Called when the creature is killed
        public virtual void JustDied(Unit killer) { }

        // Called when the creature kills a unit
        public virtual void KilledUnit(Unit victim) { }

        // Called when the creature summon successfully other creature
        public virtual void JustSummoned(Creature summon) { }
        public virtual void IsSummonedBy(WorldObject summoner) { }

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
        public virtual void SpellHit(WorldObject caster, SpellInfo spellInfo) { }

        // Called when spell hits a target
        public virtual void SpellHitTarget(WorldObject target, SpellInfo spellInfo) { }

        // Called when a spell finishes
        public virtual void OnSpellCast(SpellInfo spell) { }

        // Called when a spell fails
        public virtual void OnSpellFailed(SpellInfo spell) { }

        // Called when a spell starts
        public virtual void OnSpellStart(SpellInfo spell) { }

        // Called when a channeled spell finishes
        public virtual void OnChannelFinished(SpellInfo spell) { }

        // Should return true if the NPC is currently being escorted
        public virtual bool IsEscorted() { return false; }

        public virtual void MovementInform(MovementGeneratorType type, uint id) { }

        // Called at reaching home after evade
        public virtual void JustReachedHome() { }

        // Called at text emote receive from player
        public virtual void ReceiveEmote(Player player, TextEmotes emoteId) { }

        // Called when owner takes damage
        public virtual void OwnerAttackedBy(Unit attacker) { OnOwnerCombatInteraction(attacker); }

        // Called when owner attacks something
        public virtual void OwnerAttacked(Unit target) { OnOwnerCombatInteraction(target); }

        // called when the corpse of this creature gets removed
        public virtual void CorpseRemoved(long respawnDelay) { }

        /// == Gossip system ================================

        // Called when the dialog status between a player and the creature is requested.
        public virtual QuestGiverStatus? GetDialogStatus(Player player)
        {
            return null;
        }

        // Called when a player opens a gossip dialog with the creature.
        public virtual bool OnGossipHello(Player player) { return false; }

        // Called when a player selects a gossip item in the creature's gossip menu.
        public virtual bool OnGossipSelect(Player player, uint menuId, uint gossipListId) { return false; }

        // Called when a player selects a gossip with a code in the creature's gossip menu.
        public virtual bool OnGossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
        {
            return false;
        }

        // Called when a player accepts a quest from the creature.
        public virtual void OnQuestAccept(Player player, Quest quest) { }

        // Called when a player completes a quest and is rewarded, opt is the selected item's index or 0
        public virtual void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt) { }

        /// == Waypoints system =============================
        /// 
        public virtual void WaypointStarted(uint nodeId, uint pathId) { }

        public virtual void WaypointReached(uint nodeId, uint pathId) { }

        public virtual void WaypointPathEnded(uint nodeId, uint pathId) { }

        public virtual void PassengerBoarded(Unit passenger, sbyte seatId, bool apply) { }

        public virtual void OnSpellClick(Unit clicker, ref bool spellClickHandled) { }

        public virtual bool CanSeeAlways(WorldObject obj) { return false; }

        // Called when a player is charmed by the creature
        // If a PlayerAI* is returned, that AI is placed on the player instead of the default charm AI
        // Object destruction is handled by Unit::RemoveCharmedBy
        public virtual PlayerAI GetAIForCharmedPlayer(Player who) { return null; }

        public List<AreaBoundary> GetBoundary() { return _boundary; }

        public bool IsEngaged() { return _isEngaged; }
    }

    public class AISpellInfoType
    {
        public AISpellInfoType()
        {
            target = AITarget.Self;
            condition = AICondition.Combat;
            cooldown = TimeSpan.FromMilliseconds(SharedConst.AIDefaultCooldown);
        }

        public AITarget target;
        public AICondition condition;
        public TimeSpan cooldown;
        public TimeSpan realCooldown;
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
