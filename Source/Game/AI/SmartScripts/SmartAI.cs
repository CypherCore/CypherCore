// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class SmartAI : CreatureAI
    {
        const int SMART_ESCORT_MAX_PLAYER_DIST = 60;
        const int SMART_MAX_AID_DIST = SMART_ESCORT_MAX_PLAYER_DIST / 2;

        public uint EscortQuestID;

        SmartScript _script = new();

        bool _isCharmed;
        uint _followCreditType;
        uint _followArrivedTimer;
        uint _followCredit;
        uint _followArrivedEntry;
        ObjectGuid _followGuid;
        float _followDist;
        float _followAngle;

        SmartEscortState _escortState;
        uint _escortNPCFlags;
        uint _escortInvokerCheckTimer;
        uint _currentWaypointNode;
        bool _waypointReached;
        uint _waypointPauseTimer;
        bool _waypointPauseForced;
        bool _repeatWaypointPath;
        bool _OOCReached;
        bool _waypointPathEnded;

        bool _run;
        bool _evadeDisabled;
        bool _canCombatMove;
        uint _invincibilityHpLevel;

        uint _despawnTime;
        uint _despawnState;

        // Vehicle conditions
        bool _hasConditions;
        uint _conditionsTimer;

        // Gossip
        bool _gossipReturn;

        public SmartAI(Creature creature) : base(creature)
        {
            _escortInvokerCheckTimer = 1000;
            _run = true;
            _canCombatMove = true;

            _hasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, creature.GetEntry());
        }

        bool IsAIControlled()
        {
            return !_isCharmed;
        }

        public void StartPath(uint pathId = 0, bool repeat = false, Unit invoker = null, uint nodeId = 0)
        {
            if (HasEscortState(SmartEscortState.Escorting))
                StopPath();

            if (pathId == 0)
                return;

            WaypointPath path = LoadPath(pathId);
            if (path == null)
                return;

            _currentWaypointNode = nodeId;
            _waypointPathEnded = false;

            _repeatWaypointPath = repeat;

            // Do not use AddEscortState, removing everything from previous
            _escortState = SmartEscortState.Escorting;

            if (invoker != null && invoker.IsPlayer())
            {
                _escortNPCFlags = (uint)me.GetNpcFlags();
                me.ReplaceAllNpcFlags(NPCFlags.None);
            }

            me.GetMotionMaster().MovePath(path, _repeatWaypointPath);
        }

        WaypointPath LoadPath(uint entry)
        {
            if (HasEscortState(SmartEscortState.Escorting))
                return null;

            WaypointPath path = Global.WaypointMgr.GetPath(entry);
            if (path == null || path.nodes.Empty())
            {
                GetScript().SetPathId(0);
                return null;
            }

            GetScript().SetPathId(entry);
            return path;
        }

        public void PausePath(uint delay, bool forced)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
            {
                me.PauseMovement(delay, MovementSlot.Default, forced);
                if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
                {
                    var (nodeId, pathId) = me.GetCurrentWaypointInfo();
                    GetScript().ProcessEventsFor(SmartEvents.WaypointPaused, null, nodeId, pathId);
                }
                return;
            }

            if (HasEscortState(SmartEscortState.Paused))
            {
                Log.outError(LogFilter.Server, $"SmartAI.PausePath: Creature entry {me.GetEntry()} wanted to pause waypoint movement while already paused, ignoring.");
                return;
            }

            _waypointPauseTimer = delay;

            if (forced)
            {
                _waypointPauseForced = forced;
                SetRun(_run);
                me.PauseMovement();
                me.SetHomePosition(me.GetPosition());
            }
            else
                _waypointReached = false;

            AddEscortState(SmartEscortState.Paused);
            GetScript().ProcessEventsFor(SmartEvents.WaypointPaused, null, _currentWaypointNode, GetScript().GetPathId());
        }

        public bool CanResumePath()
        {
            if (!HasEscortState(SmartEscortState.Escorting))
            {
                // The whole resume logic doesn't support this case
                return false;
            }

            return HasEscortState(SmartEscortState.Paused);
        }

        public void StopPath(uint despawnTime = 0, uint quest = 0, bool fail = false)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
            {
                uint nodeId = 0;
                uint pathId = 0;
                if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
                    (nodeId, pathId) = me.GetCurrentWaypointInfo();

                if (_despawnState != 2)
                    SetDespawnTime(despawnTime);

                me.GetMotionMaster().MoveIdle();

                if (pathId != 0)
                    GetScript().ProcessEventsFor(SmartEvents.WaypointStopped, null, nodeId, pathId);

                if (!fail)
                {
                    if (pathId != 0)
                        GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, nodeId, pathId);
                    if (_despawnState == 1)
                        StartDespawn();
                }
                return;
            }

            if (quest != 0)
                EscortQuestID = quest;

            if (_despawnState != 2)
                SetDespawnTime(despawnTime);

            me.GetMotionMaster().MoveIdle();

            GetScript().ProcessEventsFor(SmartEvents.WaypointStopped, null, _currentWaypointNode, GetScript().GetPathId());

            EndPath(fail);
        }

        public void EndPath(bool fail = false)
        {
            RemoveEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning);

            _waypointPauseTimer = 0;

            if (_escortNPCFlags != 0)
            {
                me.ReplaceAllNpcFlags((NPCFlags)_escortNPCFlags);
                _escortNPCFlags = 0;
            }

            List<WorldObject> targets = GetScript().GetStoredTargetList(SharedConst.SmartEscortTargets, me);
            if (targets != null && EscortQuestID != 0)
            {
                if (targets.Count == 1 && GetScript().IsPlayer(targets.First()))
                {
                    Player player = targets.First().ToPlayer();
                    if (!fail && player.IsAtGroupRewardDistance(me) && player.GetCorpse() == null)
                        player.GroupEventHappens(EscortQuestID, me);

                    if (fail)
                        player.FailQuest(EscortQuestID);

                    Group group = player.GetGroup();
                    if (group != null)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                        {
                            Player groupGuy = groupRef.GetSource();
                            if (!groupGuy.IsInMap(player))
                                continue;

                            if (!fail && groupGuy.IsAtGroupRewardDistance(me) && groupGuy.GetCorpse() == null)
                                groupGuy.AreaExploredOrEventHappens(EscortQuestID);
                            else if (fail)
                                groupGuy.FailQuest(EscortQuestID);
                        }
                    }
                }
                else
                {
                    foreach (var obj in targets)
                    {
                        if (GetScript().IsPlayer(obj))
                        {
                            Player player = obj.ToPlayer();
                            if (!fail && player.IsAtGroupRewardDistance(me) && player.GetCorpse() == null)
                                player.AreaExploredOrEventHappens(EscortQuestID);
                            else if (fail)
                                player.FailQuest(EscortQuestID);
                        }
                    }
                }
            }

            // End Path events should be only processed if it was SUCCESSFUL stop or stop called by SMART_ACTION_WAYPOINT_STOP
            if (fail)
                return;

            uint pathid = GetScript().GetPathId();
            GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, _currentWaypointNode, pathid);

            if (_repeatWaypointPath)
            {
                if (IsAIControlled())
                    StartPath(GetScript().GetPathId(), _repeatWaypointPath);
            }
            else if (pathid == GetScript().GetPathId()) // if it's not the same pathid, our script wants to start another path; don't override it
                GetScript().SetPathId(0);

            if (_despawnState == 1)
                StartDespawn();
        }

        public void ResumePath()
        {
            GetScript().ProcessEventsFor(SmartEvents.WaypointResumed, null, _currentWaypointNode, GetScript().GetPathId());

            RemoveEscortState(SmartEscortState.Paused);

            _waypointPauseForced = false;
            _waypointReached = false;
            _waypointPauseTimer = 0;

            SetRun(_run);
            me.ResumeMovement();
        }

        void ReturnToLastOOCPos()
        {
            if (!IsAIControlled())
                return;

            me.SetWalk(false);
            me.GetMotionMaster().MovePoint(EventId.SmartEscortLastOCCPoint, me.GetHomePosition());
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.IsAlive())
            {
                if (IsEngaged())
                    EngagementOver();
                return;
            }

            CheckConditions(diff);

            bool hasVictim = UpdateVictim();

            GetScript().OnUpdate(diff);

            UpdatePath(diff);
            UpdateFollow(diff);
            UpdateDespawn(diff);

            if (!IsAIControlled())
                return;

            if (!hasVictim)
                return;
            
            DoMeleeAttackIfReady();
        }

        bool IsEscortInvokerInRange()
        {
            var targets = GetScript().GetStoredTargetList(SharedConst.SmartEscortTargets, me);
            if (targets != null)
            {
                float checkDist = me.GetInstanceScript() != null ? SMART_ESCORT_MAX_PLAYER_DIST * 2 : SMART_ESCORT_MAX_PLAYER_DIST;
                if (targets.Count == 1 && GetScript().IsPlayer(targets.First()))
                {
                    Player player = targets.First().ToPlayer();
                    if (me.GetDistance(player) <= checkDist)
                        return true;

                    Group group = player.GetGroup();
                    if (group != null)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                        {
                            Player groupGuy = groupRef.GetSource();
                            if (groupGuy.IsInMap(player) && me.GetDistance(groupGuy) <= checkDist)
                                return true;
                        }
                    }
                }
                else
                {
                    foreach (var obj in targets)
                    {
                        if (GetScript().IsPlayer(obj))
                        {
                            if (me.GetDistance(obj.ToPlayer()) <= checkDist)
                                return true;
                        }
                    }
                }

                // no valid target found
                return false;
            }

            // no player invoker was stored, just ignore range check
            return true;
        }

        public override void WaypointReached(uint nodeId, uint pathId)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
            {
                GetScript().ProcessEventsFor(SmartEvents.WaypointReached, null, nodeId, pathId);
                return;
            }

            _currentWaypointNode = nodeId;

            GetScript().ProcessEventsFor(SmartEvents.WaypointReached, null, _currentWaypointNode, pathId);

            if (_waypointPauseTimer != 0 && !_waypointPauseForced)
            {
                _waypointReached = true;
                me.PauseMovement();
                me.SetHomePosition(me.GetPosition());
            }
            else if (HasEscortState(SmartEscortState.Escorting) && me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
            {
                WaypointPath path = Global.WaypointMgr.GetPath(pathId);
                if (path != null && _currentWaypointNode == path.nodes.Last()?.id)
                    _waypointPathEnded = true;
                else
                    SetRun(_run);
            }
        }

        public override void WaypointPathEnded(uint nodeId, uint pathId)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
            {
                GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, nodeId, pathId);
                return;
            }
        }

        public override void MovementInform(MovementGeneratorType movementType, uint id)
        {
            if (movementType == MovementGeneratorType.Point && id == EventId.SmartEscortLastOCCPoint)
                me.ClearUnitState(UnitState.Evade);

            GetScript().ProcessEventsFor(SmartEvents.Movementinform, null, (uint)movementType, id);

            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (movementType != MovementGeneratorType.Point && id == EventId.SmartEscortLastOCCPoint)
                _OOCReached = true;
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            if (_evadeDisabled)
            {
                GetScript().ProcessEventsFor(SmartEvents.Evade);
                return;
            }

            if (!IsAIControlled())
            {
                me.AttackStop();
                return;
            }

            if (!_EnterEvadeMode())
                return;

            me.AddUnitState(UnitState.Evade);

            GetScript().ProcessEventsFor(SmartEvents.Evade); // must be after _EnterEvadeMode (spells, auras, ...)

            SetRun(_run);

            Unit owner = me.GetCharmerOrOwner();
            if (owner != null)
            {
                me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                me.ClearUnitState(UnitState.Evade);
            }
            else if (HasEscortState(SmartEscortState.Escorting))
            {
                AddEscortState(SmartEscortState.Returning);
                ReturnToLastOOCPos();
            }
            else
            {
                Unit target = !_followGuid.IsEmpty() ? Global.ObjAccessor.GetUnit(me, _followGuid) : null;
                if (target != null)
                {
                    me.GetMotionMaster().MoveFollow(target, _followDist, _followAngle);
                    // evade is not cleared in MoveFollow, so we can't keep it
                    me.ClearUnitState(UnitState.Evade);
                }
                else
                    me.GetMotionMaster().MoveTargetedHome();
            }

            if (!me.HasUnitState(UnitState.Evade))
                GetScript().OnReset();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who == null)
                return;

            GetScript().OnMoveInLineOfSight(who);

            if (!IsAIControlled())
                return;

            if (HasEscortState(SmartEscortState.Escorting) && AssistPlayerInCombatAgainst(who))
                return;

            base.MoveInLineOfSight(who);
        }

        bool AssistPlayerInCombatAgainst(Unit who)
        {
            if (me.HasReactState(ReactStates.Passive) || !IsAIControlled())
                return false;

            if (who == null || who.GetVictim() == null)
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureDifficulty().TypeFlags.HasFlag(CreatureTypeFlags.CanAssist))
                return false;

            //not a player
            if (who.GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself() == null)
                return false;

            if (!who.IsInAccessiblePlaceFor(me))
                return false;

            if (!CanAIAttack(who))
                return false;

            // we cannot attack in evade mode
            if (me.IsInEvadeMode())
                return false;

            // or if enemy is in evade mode
            if (who.IsCreature() && who.ToCreature().IsInEvadeMode())
                return false;

            if (!me.IsValidAssistTarget(who.GetVictim()))
                return false;

            //too far away and no free sight
            if (me.IsWithinDistInMap(who, SMART_MAX_AID_DIST) && me.IsWithinLOSInMap(who))
            {
                me.EngageWithTarget(who);
                return true;
            }

            return false;
        }

        public override void InitializeAI()
        {
            GetScript().OnInitialize(me);

            _despawnTime = 0;
            _despawnState = 0;
            _escortState = SmartEscortState.None;

            _followGuid.Clear();//do not reset follower on Reset(), we need it after combat evade
            _followDist = 0;
            _followAngle = 0;
            _followCredit = 0;
            _followArrivedTimer = 1000;
            _followArrivedEntry = 0;
            _followCreditType = 0;
        }

        public override void JustAppeared()
        {
            base.JustAppeared();

            if (me.IsDead())
                return;

            GetScript().ProcessEventsFor(SmartEvents.Respawn);
            GetScript().OnReset();
        }

        public override void JustReachedHome()
        {
            GetScript().OnReset();
            GetScript().ProcessEventsFor(SmartEvents.ReachedHome);

            CreatureGroup formation = me.GetFormation();
            if (formation == null || formation.GetLeader() == me || !formation.IsFormed())
            {
                if (me.GetMotionMaster().GetCurrentMovementGeneratorType(MovementSlot.Default) != MovementGeneratorType.Waypoint)
                    if (me.GetWaypointPath() != 0)
                        me.GetMotionMaster().MovePath(me.GetWaypointPath(), true);
                
                me.ResumeMovement();
            }
            else if (formation.IsFormed())
                me.GetMotionMaster().MoveIdle(); // wait the order of leader
        }

        public override void JustEngagedWith(Unit victim)
        {
            if (IsAIControlled())
                me.InterruptNonMeleeSpells(false); // must be before ProcessEvents

            GetScript().ProcessEventsFor(SmartEvents.Aggro, victim);
        }

        public override void JustDied(Unit killer)
        {
            if (HasEscortState(SmartEscortState.Escorting))
                EndPath(true);

            GetScript().ProcessEventsFor(SmartEvents.Death, killer);
        }

        public override void KilledUnit(Unit victim)
        {
            GetScript().ProcessEventsFor(SmartEvents.Kill, victim);
        }

        public override void JustSummoned(Creature summon)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonedUnit, summon);
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonedUnitDies, summon);
        }

        public override void AttackStart(Unit who)
        {
            // dont allow charmed npcs to act on their own
            if (!IsAIControlled())
            {
                if (who != null)
                    me.Attack(who, true);
                return;
            }

            if (who != null && me.Attack(who, true))
            {
                me.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);
                me.PauseMovement();

                if (_canCombatMove)
                {
                    SetRun(_run);
                    me.GetMotionMaster().MoveChase(who);
                }
            }
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHit, caster.ToUnit(), 0, 0, false, spellInfo, caster.ToGameObject());
        }
        
        public override void SpellHitTarget(WorldObject target, SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHitTarget, target.ToUnit(), 0, 0, false, spellInfo, target.ToGameObject());
        }

        public override void OnSpellCast(SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.OnSpellCast, null, 0, 0, false, spellInfo);
        }

        public override void OnSpellFailed(SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.OnSpellFailed, null, 0, 0, false, spellInfo);
        }

        public override void OnSpellStart(SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.OnSpellStart, null, 0, 0, false, spellInfo);
        }
        
        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            GetScript().ProcessEventsFor(SmartEvents.Damaged, attacker, damage);

            if (!IsAIControlled()) // don't allow players to use unkillable units
                return;

            if (_invincibilityHpLevel != 0 && (damage >= me.GetHealth() - _invincibilityHpLevel))
                damage = (uint)(me.GetHealth() - _invincibilityHpLevel);  // damage should not be nullified, because of player damage req.
        }

        public override void HealReceived(Unit by, uint addhealth)
        {
            GetScript().ProcessEventsFor(SmartEvents.ReceiveHeal, by, addhealth);
        }

        public override void ReceiveEmote(Player player, TextEmotes emoteId)
        {
            GetScript().ProcessEventsFor(SmartEvents.ReceiveEmote, player, (uint)emoteId);
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            GetScript().ProcessEventsFor(SmartEvents.JustSummoned, summoner.ToUnit(), 0, 0, false, null, summoner.ToGameObject());
        }

        public override void DamageDealt(Unit victim, ref uint damage, DamageEffectType damageType)
        {
            GetScript().ProcessEventsFor(SmartEvents.DamagedTarget, victim, damage);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonDespawned, summon, summon.GetEntry());
        }

        public override void CorpseRemoved(long respawnDelay)
        {
            GetScript().ProcessEventsFor(SmartEvents.CorpseRemoved, null, (uint)respawnDelay);
        }

        public override void OnDespawn()
        {
            GetScript().ProcessEventsFor(SmartEvents.OnDespawn);
        }

        public override void PassengerBoarded(Unit passenger, sbyte seatId, bool apply)
        {
            GetScript().ProcessEventsFor(apply ? SmartEvents.PassengerBoarded : SmartEvents.PassengerRemoved, passenger, (uint)seatId, 0, apply);
        }

        public override void OnCharmed(bool isNew)
        {
            bool charmed = me.IsCharmed();
            if (charmed) // do this before we change charmed state, as charmed state might prevent these things from processing
            {
                if (HasEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning))
                    EndPath(true);
            }

            _isCharmed = charmed;

            if (charmed && !me.IsPossessed() && !me.IsVehicle())
                me.GetMotionMaster().MoveFollow(me.GetCharmer(), SharedConst.PetFollowDist, me.GetFollowAngle());

            if (!charmed && !me.IsInEvadeMode())
            {
                if (_repeatWaypointPath)
                    StartPath(GetScript().GetPathId(), true);
                else
                    me.SetWalk(!_run);

                if (!me.LastCharmerGUID.IsEmpty())
                {
                    if (!me.HasReactState(ReactStates.Passive))
                    {
                        Unit lastCharmer = Global.ObjAccessor.GetUnit(me, me.LastCharmerGUID);
                        if (lastCharmer != null)
                            me.EngageWithTarget(lastCharmer);
                    }
                    me.LastCharmerGUID.Clear();

                    if (!me.IsInCombat())
                        EnterEvadeMode(EvadeReason.NoHostiles);
                }
            }

            GetScript().ProcessEventsFor(SmartEvents.Charmed, null, 0, 0, charmed);

            if (!GetScript().HasAnyEventWithFlag(SmartEventFlags.WhileCharmed)) // we can change AI if there are no events with this flag
                base.OnCharmed(isNew);
        }

        public override void DoAction(int param)
        {
            GetScript().ProcessEventsFor(SmartEvents.ActionDone, null, (uint)param);
        }

        public override uint GetData(uint id)
        {
            return 0;
        }

        public override void SetData(uint id, uint value) { SetData(id, value, null); }

        public void SetData(uint id, uint value, Unit invoker)
        {
            GetScript().ProcessEventsFor(SmartEvents.DataSet, invoker, id, value);
        }

        public override void SetGUID(ObjectGuid guid, int id) { }

        public override ObjectGuid GetGUID(int id)
        {
            return ObjectGuid.Empty;
        }

        public void SetRun(bool run)
        {
            me.SetWalk(!run);
            _run = run;
        }

        public void SetDisableGravity(bool disable = true)
        {
            me.SetDisableGravity(disable);
        }

        public void SetEvadeDisabled(bool disable)
        {
            _evadeDisabled = disable;
        }

        public override bool OnGossipHello(Player player)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player);
            return _gossipReturn;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId);
            return _gossipReturn;
        }

        public override bool OnGossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
        {
            return false;
        }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id);
        }

        public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
        {
            GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt);
        }

        public void SetCombatMove(bool on, bool stopMoving = false)
        {
            if (_canCombatMove == on)
                return;

            _canCombatMove = on;

            if (!IsAIControlled())
                return;

            if (me.IsEngaged())
            {
                if (on)
                {
                    if (!me.HasReactState(ReactStates.Passive) && me.GetVictim() != null && !me.GetMotionMaster().HasMovementGenerator(movement =>
                    {
                        return movement.GetMovementGeneratorType() == MovementGeneratorType.Chase && movement.Mode == MovementGeneratorMode.Default && movement.Priority == MovementGeneratorPriority.Normal;
                    }))
                    {
                        SetRun(_run);
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                    }
                }
                else
                {
                    var movement = me.GetMotionMaster().GetMovementGenerator(a => a.GetMovementGeneratorType() == MovementGeneratorType.Chase && a.Mode == MovementGeneratorMode.Default && a.Priority == MovementGeneratorPriority.Normal);
                    if (movement != null)
                    {
                        me.GetMotionMaster().Remove(movement);
                        if (stopMoving)
                            me.StopMoving();
                    }
                }
            }
        }

        public void SetFollow(Unit target, float dist, float angle, uint credit, uint end, uint creditType)
        {
            if (target == null)
            {
                StopFollow(false);
                return;
            }

            _followGuid = target.GetGUID();
            _followDist = dist;
            _followAngle = angle;
            _followArrivedTimer = 1000;
            _followCredit = credit;
            _followArrivedEntry = end;
            _followCreditType = creditType;
            SetRun(_run);
            me.GetMotionMaster().MoveFollow(target, _followDist, _followAngle);
        }

        public void StopFollow(bool complete)
        {
            _followGuid.Clear();
            _followDist = 0;
            _followAngle = 0;
            _followCredit = 0;
            _followArrivedTimer = 1000;
            _followArrivedEntry = 0;
            _followCreditType = 0;
            me.GetMotionMaster().Clear();
            me.StopMoving();
            me.GetMotionMaster().MoveIdle();

            if (!complete)
                return;

            Player player = Global.ObjAccessor.GetPlayer(me, _followGuid);
            if (player != null)
            {
                if (_followCreditType == 0)
                    player.RewardPlayerAndGroupAtEvent(_followCredit, me);
                else
                    player.GroupEventHappens(_followCredit, me);
            }

            SetDespawnTime(5000);
            StartDespawn();
            GetScript().ProcessEventsFor(SmartEvents.FollowCompleted, player);
        }

        public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker, uint startFromEventId = 0)
        {
            GetScript().SetTimedActionList(e, entry, invoker, startFromEventId);
        }

        public override void OnGameEvent(bool start, ushort eventId)
        {
            GetScript().ProcessEventsFor(start ? SmartEvents.GameEventStart : SmartEvents.GameEventEnd, null, eventId);
        }

        public override void OnSpellClick(Unit clicker, ref bool spellClickHandled)
        {
            if (!spellClickHandled)
                return;

            GetScript().ProcessEventsFor(SmartEvents.OnSpellclick, clicker);
        }

        void CheckConditions(uint diff)
        {
            if (!_hasConditions)
                return;

            if (_conditionsTimer <= diff)
            {
                Vehicle vehicleKit = me.GetVehicleKit();
                if (vehicleKit != null)
                {
                    foreach (var pair in vehicleKit.Seats)
                    {
                        Unit passenger = Global.ObjAccessor.GetUnit(me, pair.Value.Passenger.Guid);
                        if (passenger != null)
                        {
                            Player player = passenger.ToPlayer();
                            if (player != null)
                            {
                                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.CreatureTemplateVehicle, me.GetEntry(), player, me))
                                {
                                    player.ExitVehicle();
                                    return; // check other pessanger in next tick
                                }
                            }
                        }
                    }
                }

                _conditionsTimer = 1000;
            }
            else
                _conditionsTimer -= diff;
        }

        void UpdatePath(uint diff)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (_escortInvokerCheckTimer < diff)
            {
                if (!IsEscortInvokerInRange())
                {
                    StopPath(0, EscortQuestID, true);

                    // allow to properly hook out of range despawn action, which in most cases should perform the same operation as dying
                    GetScript().ProcessEventsFor(SmartEvents.Death, me);
                    me.DespawnOrUnsummon();
                    return;
                }
                _escortInvokerCheckTimer = 1000;
            }
            else
                _escortInvokerCheckTimer -= diff;

            // handle pause
            if (HasEscortState(SmartEscortState.Paused) && (_waypointReached || _waypointPauseForced))
            {
                // Resume only if there was a pause timer set
                if (_waypointPauseTimer != 0 && !me.IsInCombat() && !HasEscortState(SmartEscortState.Returning))
                {
                    if (_waypointPauseTimer <= diff)
                        ResumePath();
                    else
                        _waypointPauseTimer -= diff;
                }
            }
            else if (_waypointPathEnded) // end path
            {
                _waypointPathEnded = false;
                StopPath();
                return;
            }

            if (HasEscortState(SmartEscortState.Returning))
            {
                if (_OOCReached)//reached OOC WP
                {
                    _OOCReached = false;
                    RemoveEscortState(SmartEscortState.Returning);
                    if (!HasEscortState(SmartEscortState.Paused))
                        ResumePath();
                }
            }
        }

        void UpdateFollow(uint diff)
        {
            if (_followGuid.IsEmpty())
            {
                if (_followArrivedTimer < diff)
                {
                    if (me.FindNearestCreature(_followArrivedEntry, SharedConst.InteractionDistance, true) != null)
                    {
                        StopFollow(true);
                        return;
                    }

                    _followArrivedTimer = 1000;
                }
                else
                    _followArrivedTimer -= diff;
            }
        }

        void UpdateDespawn(uint diff)
        {
            if (_despawnState <= 1 || _despawnState > 3)
                return;

            if (_despawnTime < diff)
            {
                if (_despawnState == 2)
                {
                    me.SetVisible(false);
                    _despawnTime = 5000;
                    _despawnState++;
                }
                else
                    me.DespawnOrUnsummon();
            }
            else
                _despawnTime -= diff;
        }

        public override void Reset()
        {
            if (!HasEscortState(SmartEscortState.Escorting))//dont mess up escort movement after combat
                SetRun(_run);
            GetScript().OnReset();
        }

        public bool HasEscortState(SmartEscortState escortState) { return (_escortState & escortState) != 0; }
        public void AddEscortState(SmartEscortState escortState) { _escortState |= escortState; }
        public void RemoveEscortState(SmartEscortState escortState) { _escortState &= ~escortState; }

        public bool CanCombatMove() { return _canCombatMove; }

        public SmartScript GetScript() { return _script; }

        public void SetInvincibilityHpLevel(uint level) { _invincibilityHpLevel = level; }

        public void SetDespawnTime(uint t, uint r = 0)
        {
            _despawnTime = t;
            _despawnState = t != 0 ? 1 : 0u;
        }

        public void StartDespawn() { _despawnState = 2; }

        public void SetWPPauseTimer(uint time) { _waypointPauseTimer = time; }

        public void SetGossipReturn(bool val) { _gossipReturn = val; }
    }

    public class SmartGameObjectAI : GameObjectAI
    {
        SmartScript _script = new();

        // Gossip
        bool _gossipReturn;

        public SmartGameObjectAI(GameObject go) : base(go) { }

        public override void UpdateAI(uint diff)
        {
            GetScript().OnUpdate(diff);
        }

        public override void InitializeAI()
        {
            GetScript().OnInitialize(me);

            // do not call respawn event if go is not spawned
            if (me.IsSpawned())
                GetScript().ProcessEventsFor(SmartEvents.Respawn);
        }

        public override void Reset()
        {
            GetScript().OnReset();
        }

        public override bool OnGossipHello(Player player)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 0, 0, false, null, me);
            return _gossipReturn;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId, false, null, me);
            return _gossipReturn;
        }

        public override bool OnGossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
        {
            return false;
        }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id, 0, false, null, me);
        }

        public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
        {
            GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt, false, null, me);
        }

        public override bool OnReportUse(Player player)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 1, 0, false, null, me);
            return _gossipReturn;
        }

        public override void Destroyed(WorldObject attacker, uint eventId)
        {
            GetScript().ProcessEventsFor(SmartEvents.Death, attacker != null ? attacker.ToUnit() : null, eventId, 0, false, null, me);
        }

        public override void SetData(uint id, uint value) { SetData(id, value, null); }
        
        public void SetData(uint id, uint value, Unit invoker)
        {
            GetScript().ProcessEventsFor(SmartEvents.DataSet, invoker, id, value);
        }

        public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
        {
            GetScript().SetTimedActionList(e, entry, invoker);
        }

        public override void OnGameEvent(bool start, ushort eventId)
        {
            GetScript().ProcessEventsFor(start ? SmartEvents.GameEventStart : SmartEvents.GameEventEnd, null, eventId);
        }

        public override void OnLootStateChanged(uint state, Unit unit)
        {
            GetScript().ProcessEventsFor(SmartEvents.GoLootStateChanged, unit, state);
        }

        public override void EventInform(uint eventId)
        {
            GetScript().ProcessEventsFor(SmartEvents.GoEventInform, null, eventId);
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHit, caster.ToUnit(), 0, 0, false, spellInfo);
        }

        public override void JustSummoned(Creature creature)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonedUnit, creature);
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonedUnitDies, summon);
        }

        public override void SummonedCreatureDespawn(Creature unit)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonDespawned, unit, unit.GetEntry());
        }

        public void SetGossipReturn(bool val) { _gossipReturn = val; }

        public SmartScript GetScript() { return _script; }
    }

    public class SmartAreaTriggerAI : AreaTriggerAI
    {
        SmartScript _script = new();

        public SmartAreaTriggerAI(AreaTrigger areaTrigger) : base(areaTrigger) { }

        public override void OnInitialize()
        {
            GetScript().OnInitialize(at);
        }

        public override void OnUpdate(uint diff)
        {
            GetScript().OnUpdate(diff);
        }

        public override void OnUnitEnter(Unit unit)
        {
            GetScript().ProcessEventsFor(SmartEvents.AreatriggerOntrigger, unit);
        }

        public void SetTimedActionList(SmartScriptHolder e, uint entry, Unit invoker)
        {
            GetScript().SetTimedActionList(e, entry, invoker);
        }

        public SmartScript GetScript() { return _script; }
    }

    public enum SmartEscortState
    {
        None = 0x00,                        //nothing in progress
        Escorting = 0x01,                        //escort is in progress
        Returning = 0x02,                        //escort is returning after being in combat
        Paused = 0x04                         //will not proceed with waypoints before state is removed
    }
}
