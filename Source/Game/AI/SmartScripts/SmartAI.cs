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

        public SmartAI(Creature creature) : base(creature)
        {
            _escortInvokerCheckTimer = 1000;
            mRun = true;
            mCanAutoAttack = true;
            mCanCombatMove = true;

            mHasConditions = Global.ConditionMgr.HasConditionsForNotGroupedEntry(ConditionSourceType.CreatureTemplateVehicle, creature.GetEntry());
        }

        bool IsAIControlled()
        {
            return !mIsCharmed;
        }

        void UpdateDespawn(uint diff)
        {
            if (mDespawnState <= 1 || mDespawnState > 3)
                return;

            if (mDespawnTime < diff)
            {
                if (mDespawnState == 2)
                {
                    me.SetVisible(false);
                    mDespawnTime = 5000;
                    mDespawnState++;
                }
                else
                    me.DespawnOrUnsummon(0, TimeSpan.FromSeconds(mRespawnTime));
            }
            else
                mDespawnTime -= diff;
        }

        public void StartPath(bool run = false, uint pathId = 0, bool repeat = false, Unit invoker = null, uint nodeId = 1)
        {
            if (me.IsInCombat())// no wp movement in combat
            {
                Log.outError(LogFilter.Server, $"SmartAI.StartPath: Creature entry {me.GetEntry()} wanted to start waypoint movement while in combat, ignoring.");
                return;
            }

            if (HasEscortState(SmartEscortState.Escorting))
                StopPath();

            if (pathId != 0)
            {
                if (!LoadPath(pathId))
                    return;
            }

            if (_path.nodes.Empty())
                return;

            _currentWaypointNode = nodeId;
            _waypointPathEnded = false;

            _repeatWaypointPath = repeat;

            // Do not use AddEscortState, removing everything from previous
            _escortState = SmartEscortState.Escorting;

            if (invoker && invoker.IsPlayer())
            {
                _escortNPCFlags = me.m_unitData.NpcFlags[0];
                me.SetNpcFlags((NPCFlags)0);
            }

            GetScript().ProcessEventsFor(SmartEvents.WaypointStart, null, _currentWaypointNode, GetScript().GetPathId());

            me.GetMotionMaster().MovePath(_path, _repeatWaypointPath);
        }

        bool LoadPath(uint entry)
        {
            if (HasEscortState(SmartEscortState.Escorting))
                return false;

            WaypointPath path = Global.SmartAIMgr.GetPath(entry);
            if (path == null || path.nodes.Empty())
            {
                GetScript().SetPathId(0);
                return false;
            }

            _path.id = path.id;
            _path.nodes = path.nodes;
            foreach (WaypointNode waypoint in _path.nodes)
            {
                GridDefines.NormalizeMapCoord(ref waypoint.x);
                GridDefines.NormalizeMapCoord(ref waypoint.y);
                waypoint.moveType = mRun ? WaypointMoveType.Run : WaypointMoveType.Walk;
            }

            GetScript().SetPathId(entry);
            return true;
        }

        public void PausePath(uint delay, bool forced)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (HasEscortState(SmartEscortState.Paused))
            {
                Log.outError(LogFilter.Server, $"SmartAI.PausePath: Creature entry {me.GetEntry()} wanted to pause waypoint movement while already paused, ignoring.");
                return;
            }

            _waypointPauseTimer = delay;

            if (forced)
            {
                _waypointPauseForced = forced;
                SetRun(mRun);
                me.PauseMovement();
                me.SetHomePosition(me.GetPosition());
            }
            else
                _waypointReached = false;

            AddEscortState(SmartEscortState.Paused);
            GetScript().ProcessEventsFor(SmartEvents.WaypointPaused, null, _currentWaypointNode, GetScript().GetPathId());
        }

        public void StopPath(uint despawnTime = 0, uint quest = 0, bool fail = false)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (quest != 0)
                mEscortQuestID = quest;

            if (mDespawnState != 2)
                SetDespawnTime(despawnTime);

            me.GetMotionMaster().MoveIdle();

            GetScript().ProcessEventsFor(SmartEvents.WaypointStopped, null, _currentWaypointNode, GetScript().GetPathId());

            EndPath(fail);
        }

        public void EndPath(bool fail = false)
        {
            RemoveEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning);
            _path.nodes.Clear();
            _waypointPauseTimer = 0;

            if (_escortNPCFlags != 0)
            {
                me.SetNpcFlags((NPCFlags)_escortNPCFlags);
                _escortNPCFlags = 0;
            }

            List<WorldObject> targets = GetScript().GetStoredTargetList(SharedConst.SmartEscortTargets, me);
            if (targets != null && mEscortQuestID != 0)
            {
                if (targets.Count == 1 && GetScript().IsPlayer(targets.First()))
                {
                    Player player = targets.First().ToPlayer();
                    if (!fail && player.IsAtGroupRewardDistance(me) && player.GetCorpse() == null)
                        player.GroupEventHappens(mEscortQuestID, me);

                    if (fail)
                        player.FailQuest(mEscortQuestID);

                    Group group = player.GetGroup();
                    if (group)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                        {
                            Player groupGuy = groupRef.GetSource();
                            if (!groupGuy.IsInMap(player))
                                continue;

                            if (!fail && groupGuy.IsAtGroupRewardDistance(me) && !groupGuy.GetCorpse())
                                groupGuy.AreaExploredOrEventHappens(mEscortQuestID);
                            else if (fail)
                                groupGuy.FailQuest(mEscortQuestID);
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
                                player.AreaExploredOrEventHappens(mEscortQuestID);
                            else if (fail)
                                player.FailQuest(mEscortQuestID);
                        }
                    }
                }
            }

            // End Path events should be only processed if it was SUCCESSFUL stop or stop called by SMART_ACTION_WAYPOINT_STOP
            if (fail)
                return;

            GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, _currentWaypointNode, GetScript().GetPathId());

            if (_repeatWaypointPath)
            {
                if (IsAIControlled())
                    StartPath(mRun, GetScript().GetPathId(), _repeatWaypointPath);
            }
            else
                GetScript().SetPathId(0);

            if (mDespawnState == 1)
                StartDespawn();
        }

        public void ResumePath()
        {
            GetScript().ProcessEventsFor(SmartEvents.WaypointResumed, null, _currentWaypointNode, GetScript().GetPathId());

            RemoveEscortState(SmartEscortState.Paused);

            _waypointPauseForced = false;
            _waypointReached = false;
            _waypointPauseTimer = 0;

            SetRun(mRun);
            me.ResumeMovement();
        }

        void ReturnToLastOOCPos()
        {
            if (!IsAIControlled())
                return;

            me.SetWalk(false);
            me.GetMotionMaster().MovePoint(EventId.SmartEscortLastOCCPoint, me.GetHomePosition());
        }

        void UpdatePath(uint diff)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (_escortInvokerCheckTimer < diff)
            {
                if (!IsEscortInvokerInRange())
                {
                    StopPath(0, mEscortQuestID, true);

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
                if (_waypointPauseTimer < diff)
                {
                    if (!me.IsInCombat() && !HasEscortState(SmartEscortState.Returning))
                        ResumePath();
                }
                else
                    _waypointPauseTimer -= diff;
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

        public override void UpdateAI(uint diff)
        {
            CheckConditions(diff);
            GetScript().OnUpdate(diff);
            UpdatePath(diff);
            UpdateDespawn(diff);

            if (!mFollowGuid.IsEmpty())
            {
                if (mFollowArrivedTimer < diff)
                {
                    if (me.FindNearestCreature(mFollowArrivedEntry, SharedConst.InteractionDistance, true))
                    {
                        StopFollow(true);
                        return;
                    }

                    mFollowArrivedTimer = 1000;
                }
                else
                    mFollowArrivedTimer -= diff;
            }

            if (!IsAIControlled())
                return;

            if (!UpdateVictim())
                return;

            if (mCanAutoAttack)
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
                    if (group)
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

        public override void WaypointStarted(uint nodeId, uint pathId)
        {

        }

        public override void WaypointReached(uint nodeId, uint pathId)
        {
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
                if (_currentWaypointNode == _path.nodes.Count)
                    _waypointPathEnded = true;
                else
                    SetRun(mRun);
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
            if (mEvadeDisabled)
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

            GetScript().ProcessEventsFor(SmartEvents.Evade);//must be after aura clear so we can cast spells from db

            SetRun(mRun);
            if (HasEscortState(SmartEscortState.Escorting))
            {
                AddEscortState(SmartEscortState.Returning);
                ReturnToLastOOCPos();
            }
            else
            {
                Unit target = !mFollowGuid.IsEmpty() ? Global.ObjAccessor.GetUnit(me, mFollowGuid) : null;
                Unit owner = me.GetCharmerOrOwner();

                if (target)
                {
                    me.GetMotionMaster().MoveFollow(target, mFollowDist, mFollowAngle);
                    // evade is not cleared in MoveFollow, so we can't keep it
                    me.ClearUnitState(UnitState.Evade);
                }
                else if (owner)
                {
                    me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
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

        public override bool CanAIAttack(Unit victim)
        {
            return !me.HasReactState(ReactStates.Passive);
        }

        bool AssistPlayerInCombatAgainst(Unit who)
        {
            if (me.HasReactState(ReactStates.Passive) || !IsAIControlled())
                return false;

            if (who == null || who.GetVictim() == null)
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
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

        public override void JustAppeared()
        {
            mDespawnTime = 0;
            mRespawnTime = 0;
            mDespawnState = 0;
            _escortState = SmartEscortState.None;
            me.SetVisible(true);
            if (me.GetFaction() != me.GetCreatureTemplate().Faction)
                me.RestoreFaction();
            mJustReset = true;
            JustReachedHome();
            GetScript().ProcessEventsFor(SmartEvents.Respawn);
            mFollowGuid.Clear();//do not reset follower on Reset(), we need it after combat evade
            mFollowDist = 0;
            mFollowAngle = 0;
            mFollowCredit = 0;
            mFollowArrivedTimer = 1000;
            mFollowArrivedEntry = 0;
            mFollowCreditType = 0;
        }

        public override void JustReachedHome()
        {
            GetScript().OnReset();
            if (!mJustReset)
            {
                GetScript().ProcessEventsFor(SmartEvents.ReachedHome);

                CreatureGroup formation = me.GetFormation();
                if (formation == null || formation.GetLeader() == me || !formation.IsFormed())
                {
                    if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Idle && me.GetWaypointPath() != 0)
                        me.GetMotionMaster().MovePath(me.GetWaypointPath(), true);
                    else
                        me.ResumeMovement();
                }
                else if (formation.IsFormed())
                    me.GetMotionMaster().MoveIdle(); // wait the order of leader
            }

            mJustReset = false;
        }

        public override void EnterCombat(Unit victim)
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

        public override void AttackStart(Unit who)
        {
            // dont allow charmed npcs to act on their own
            if (!IsAIControlled())
            {
                if (who != null)
                    me.Attack(who, mCanAutoAttack);
                return;
            }

            if (who != null && me.Attack(who, mCanAutoAttack))
            {
                me.GetMotionMaster().Clear(MovementSlot.Active);
                me.PauseMovement();

                if (mCanCombatMove)
                {
                    SetRun(mRun);
                    me.GetMotionMaster().MoveChase(who);
                }
            }
        }

        public override void SpellHit(Unit caster, SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHit, caster, 0, 0, false, spellInfo);
        }

        public override void SpellHitTarget(Unit target, SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHitTarget, target, 0, 0, false, spellInfo);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            GetScript().ProcessEventsFor(SmartEvents.Damaged, attacker, damage);

            if (!IsAIControlled()) // don't allow players to use unkillable units
                return;

            if (mInvincibilityHpLevel != 0 && (damage >= me.GetHealth() - mInvincibilityHpLevel))
                damage = (uint)(me.GetHealth() - mInvincibilityHpLevel);  // damage should not be nullified, because of player damage req.
        }

        public override void HealReceived(Unit by, uint addhealth)
        {
            GetScript().ProcessEventsFor(SmartEvents.ReceiveHeal, by, addhealth);
        }

        public override void ReceiveEmote(Player player, TextEmotes emoteId)
        {
            GetScript().ProcessEventsFor(SmartEvents.ReceiveEmote, player, (uint)emoteId);
        }

        public override void IsSummonedBy(Unit summoner)
        {
            GetScript().ProcessEventsFor(SmartEvents.JustSummoned, summoner);
        }

        public override void DamageDealt(Unit victim, ref uint damage, DamageEffectType damageType)
        {
            GetScript().ProcessEventsFor(SmartEvents.DamagedTarget, victim, damage);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            GetScript().ProcessEventsFor(SmartEvents.SummonDespawned, summon);
        }

        public override void CorpseRemoved(long respawnDelay)
        {
            GetScript().ProcessEventsFor(SmartEvents.CorpseRemoved, null, (uint)respawnDelay);
        }

        public override void PassengerBoarded(Unit passenger, sbyte seatId, bool apply)
        {
            GetScript().ProcessEventsFor(apply ? SmartEvents.PassengerBoarded : SmartEvents.PassengerRemoved, passenger, (uint)seatId, 0, apply);
        }

        public override void InitializeAI()
        {
            mScript.OnInitialize(me);

            if (!me.IsDead())
            {
                GetScript().OnReset();
                GetScript().ProcessEventsFor(SmartEvents.Respawn);
            }
        }

        public override void OnCharmed(bool apply)
        {
            if (apply) // do this before we change charmed state, as charmed state might prevent these things from processing
            {
                if (HasEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning))
                    EndPath(true);
            }

            mIsCharmed = apply;

            if (!apply && !me.IsInEvadeMode())
            {
                if (_repeatWaypointPath)
                    StartPath(mRun, GetScript().GetPathId(), true);
                else
                    me.SetWalk(!mRun);

                Unit charmer = me.GetCharmer();
                if (charmer)
                    AttackStart(charmer);
            }

            GetScript().ProcessEventsFor(SmartEvents.Charmed, null, 0, 0, apply);
        }

        public override void DoAction(int param)
        {
            GetScript().ProcessEventsFor(SmartEvents.ActionDone, null, (uint)param);
        }

        public override uint GetData(uint id)
        {
            return 0;
        }

        public override void SetData(uint id, uint value)
        {
            GetScript().ProcessEventsFor(SmartEvents.DataSet, null, id, value);
        }

        public override void SetGUID(ObjectGuid guid, int id) { }

        public override ObjectGuid GetGUID(int id)
        {
            return ObjectGuid.Empty;
        }

        public void SetRun(bool run)
        {
            me.SetWalk(!run);
            mRun = run;
        }

        public void SetDisableGravity(bool disable = true)
        {
            me.SetDisableGravity(disable);
        }

        public void SetCanFly(bool fly = true)
        {
            me.SetCanFly(fly);
        }

        public void SetSwim(bool swim)
        {
            me.SetSwim(swim);
        }

        public void SetEvadeDisabled(bool disable)
        {
            mEvadeDisabled = disable;
        }

        public override bool GossipHello(Player player)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player);
            return _gossipReturn;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId);
            return _gossipReturn;
        }

        public override bool GossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
        {
            return false;
        }

        public override void QuestAccept(Player player, Quest quest)
        {
            GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id);
        }

        public override void QuestReward(Player player, Quest quest, uint opt)
        {
            GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt);
        }

        public void SetCombatMove(bool on)
        {
            if (mCanCombatMove == on)
                return;

            mCanCombatMove = on;

            if (!IsAIControlled())
                return;

            if (me.IsEngaged())
            {
                if (on && !me.HasReactState(ReactStates.Passive) && me.GetVictim() && me.GetMotionMaster().GetMotionSlotType(MovementSlot.Active) == MovementGeneratorType.Max)
                {
                    SetRun(mRun);
                    me.GetMotionMaster().MoveChase(me.GetVictim());
                }
                else if (!on && me.GetMotionMaster().GetMotionSlotType(MovementSlot.Active) == MovementGeneratorType.Chase)
                    me.GetMotionMaster().Clear(MovementSlot.Active);
            }
        }

        public void SetFollow(Unit target, float dist, float angle, uint credit, uint end, uint creditType)
        {
            if (target == null)
            {
                StopFollow(false);
                return;
            }

            mFollowGuid = target.GetGUID();
            mFollowDist = dist;
            mFollowAngle = angle;
            mFollowArrivedTimer = 1000;
            mFollowCredit = credit;
            mFollowArrivedEntry = end;
            mFollowCreditType = creditType;
            SetRun(mRun);
            me.GetMotionMaster().MoveFollow(target, mFollowDist, mFollowAngle);
        }

        public void StopFollow(bool complete)
        {
            mFollowGuid.Clear();
            mFollowDist = 0;
            mFollowAngle = 0;
            mFollowCredit = 0;
            mFollowArrivedTimer = 1000;
            mFollowArrivedEntry = 0;
            mFollowCreditType = 0;
            me.StopMoving();
            me.GetMotionMaster().MoveIdle();

            if (!complete)
                return;

            Player player = Global.ObjAccessor.GetPlayer(me, mFollowGuid);
            if (player != null)
            {
                if (mFollowCreditType == 0)
                    player.RewardPlayerAndGroupAtEvent(mFollowCredit, me);
                else
                    player.GroupEventHappens(mFollowCredit, me);
            }

            SetDespawnTime(5000);
            StartDespawn();
            GetScript().ProcessEventsFor(SmartEvents.FollowCompleted);
        }

        public void SetScript9(SmartScriptHolder e, uint entry, Unit invoker)
        {
            if (invoker != null)
                GetScript().mLastInvoker = invoker.GetGUID();
            GetScript().SetScript9(e, entry);
        }

        public override void OnGameEvent(bool start, ushort eventId)
        {
            GetScript().ProcessEventsFor(start ? SmartEvents.GameEventStart : SmartEvents.GameEventEnd, null, eventId);
        }

        public override void OnSpellClick(Unit clicker, ref bool result)
        {
            if (!result)
                return;

            GetScript().ProcessEventsFor(SmartEvents.OnSpellclick, clicker);
        }

        void CheckConditions(uint diff)
        {
            if (!mHasConditions)
                return;

            if (mConditionsTimer <= diff)
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

                mConditionsTimer = 1000;
            }
            else
                mConditionsTimer -= diff;
        }

        public override void Reset()
        {
            if (!HasEscortState(SmartEscortState.Escorting))//dont mess up escort movement after combat
                SetRun(mRun);
            GetScript().OnReset();
        }

        public bool HasEscortState(SmartEscortState uiEscortState) { return (_escortState & uiEscortState) != 0; }
        public void AddEscortState(SmartEscortState uiEscortState) { _escortState |= uiEscortState; }
        public void RemoveEscortState(SmartEscortState uiEscortState) { _escortState &= ~uiEscortState; }
        public void SetAutoAttack(bool on) { mCanAutoAttack = on; }

        public bool CanCombatMove() { return mCanCombatMove; }

        public SmartScript GetScript() { return mScript; }

        public void SetInvincibilityHpLevel(uint level) { mInvincibilityHpLevel = level; }

        public void SetDespawnTime(uint t, uint r = 0)
        {
            mDespawnTime = t;
            mRespawnTime = r;
            mDespawnState = t != 0 ? 1 : 0u;
        }

        public void StartDespawn() { mDespawnState = 2; }

        public void SetWPPauseTimer(uint time) { _waypointPauseTimer = time; }

        public void SetGossipReturn(bool val) { _gossipReturn = val; }

        public uint mEscortQuestID;

        SmartScript mScript = new SmartScript();

        bool mIsCharmed;
        uint mFollowCreditType;
        uint mFollowArrivedTimer;
        uint mFollowCredit;
        uint mFollowArrivedEntry;
        ObjectGuid mFollowGuid;
        float mFollowDist;
        float mFollowAngle;

        SmartEscortState _escortState;
        uint _escortNPCFlags;
        uint _escortInvokerCheckTimer;
        WaypointPath _path;
        uint _currentWaypointNode;
        bool _waypointReached;
        uint _waypointPauseTimer;
        bool _waypointPauseForced;
        bool _repeatWaypointPath;
        bool _OOCReached;
        bool _waypointPathEnded;

        bool mRun;
        bool mEvadeDisabled;
        bool mCanAutoAttack;
        bool mCanCombatMove;
        uint mInvincibilityHpLevel;

        uint mDespawnTime;
        uint mRespawnTime;
        uint mDespawnState;
        bool mJustReset;

        // Vehicle conditions
        bool mHasConditions;
        uint mConditionsTimer;

        // Gossip
        bool _gossipReturn;
    }

    public class SmartGameObjectAI : GameObjectAI
    {
        public SmartGameObjectAI(GameObject g) : base(g)
        {
            mScript = new SmartScript();
        }

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
            // call respawn event on reset
            GetScript().ProcessEventsFor(SmartEvents.Respawn);

            GetScript().OnReset();
        }

        public override bool GossipHello(Player player)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 0, 0, false, null, me);
            return _gossipReturn;
        }

        public override bool OnReportUse(Player player)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 1, 0, false, null, me);
            return _gossipReturn;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            _gossipReturn = false;
            GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId, false, null, me);
            return _gossipReturn;
        }

        public override bool GossipSelectCode(Player player, uint menuId, uint gossipListId, string code)
        {
            return false;
        }

        public override void QuestAccept(Player player, Quest quest)
        {
            GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id, 0, false, null, me);
        }

        public override void QuestReward(Player player, Quest quest, uint opt)
        {
            GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt, false, null, me);
        }

        public override uint GetDialogStatus(Player player)
        {
            return 100;
        }

        public override void Destroyed(Player player, uint eventId)
        {
            GetScript().ProcessEventsFor(SmartEvents.Death, player, eventId, 0, false, null, me);
        }

        public override void SetData(uint id, uint value)
        {
            GetScript().ProcessEventsFor(SmartEvents.DataSet, null, id, value);
        }

        public void SetScript9(SmartScriptHolder e, uint entry, Unit invoker)
        {
            if (invoker != null)
                GetScript().mLastInvoker = invoker.GetGUID();
            GetScript().SetScript9(e, entry);
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

        public override void SpellHit(Unit unit, SpellInfo spellInfo)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHit, unit, 0, 0, false, spellInfo);
        }

        public void SetGossipReturn(bool val) { _gossipReturn = val; }

        public SmartScript GetScript() { return mScript; }

        SmartScript mScript;

        // Gossip
        bool _gossipReturn;
    }

    public enum SmartEscortState
    {
        None = 0x00,                        //nothing in progress
        Escorting = 0x01,                        //escort is in progress
        Returning = 0x02,                        //escort is returning after being in combat
        Paused = 0x04                         //will not proceed with waypoints before state is removed
    }
}
