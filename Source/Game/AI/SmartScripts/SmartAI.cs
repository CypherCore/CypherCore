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
using Game.Maps;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class SmartAI : CreatureAI
    {
        const int SMART_ESCORT_MAX_PLAYER_DIST = 50;
        const int SMART_MAX_AID_DIST = SMART_ESCORT_MAX_PLAYER_DIST / 2;

        public SmartAI(Creature creature) : base(creature)
        {
            mIsCharmed = false;
            // copy script to local (protection for table reload)

            mEscortState = SmartEscortState.None;
            mCurrentWPID = 0;//first wp id is 1 !!
            mWPReached = false;
            mWPPauseTimer = 0;
            mOOCReached = false;
            mEscortNPCFlags = 0;

            mCanRepeatPath = false;

            // Spawn in run mode
            mRun = true;
            m_Ended = false;

            mCanAutoAttack = true;
            mCanCombatMove = true;

            mForcedPaused = false;
            mEscortQuestID = 0;

            mDespawnTime = 0;
            mDespawnState = 0;

            mEscortInvokerCheckTimer = 1000;
            mFollowGuid = ObjectGuid.Empty;
            mFollowDist = 0;
            mFollowAngle = 0;
            mFollowCredit = 0;
            mFollowArrivedEntry = 0;
            mFollowCreditType = 0;
            mInvincibilityHpLevel = 0;
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

        public override void Reset()
        {
            if (!HasEscortState(SmartEscortState.Escorting))//dont mess up escort movement after combat
                SetRun(mRun);
            GetScript().OnReset();
        }

        public void StartPath(bool run = false, uint path = 0, bool repeat = false, Unit invoker = null)
        {
            if (me.IsInCombat())// no wp movement in combat
            {
                Log.outError(LogFilter.Server, "SmartAI.StartPath: Creature entry {0} wanted to start waypoint movement while in combat, ignoring.", me.GetEntry());
                return;
            }

            if (HasEscortState(SmartEscortState.Escorting))
                StopPath();

            SetRun(run);

            if (path != 0)
                if (!LoadPath(path))
                    return;

            if (_path.nodes.Empty())
                return;

            mCurrentWPID = 1;
            m_Ended = false;

            // Do not use AddEscortState, removing everything from previous cycle
            mEscortState = SmartEscortState.Escorting;
            mCanRepeatPath = repeat;

            if (invoker && invoker.GetTypeId() == TypeId.Player)
            {
                mEscortNPCFlags = me.GetUInt32Value(UnitFields.NpcFlags);
                me.SetFlag(UnitFields.NpcFlags, 0);
            }

            GetScript().ProcessEventsFor(SmartEvents.WaypointStart, null, mCurrentWPID, GetScript().GetPathId());

            me.GetMotionMaster().MovePath(_path, mCanRepeatPath);
        }

        bool LoadPath(uint entry)
        {
            if (HasEscortState(SmartEscortState.Escorting))
                return false;

            var path = Global.SmartAIMgr.GetPath(entry);
            if (path.Empty())
            {
                GetScript().SetPathId(0);
                return false;
            }

            foreach (WayPoint waypoint in path)
            {
                float x = waypoint.x;
                float y = waypoint.y;
                float z = waypoint.z;

                GridDefines.NormalizeMapCoord(ref x);
                GridDefines.NormalizeMapCoord(ref y);

                WaypointNode wp = new WaypointNode();
                wp.id = waypoint.id;
                wp.x = x;
                wp.y = y;
                wp.z = z;
                wp.orientation = 0.0f;
                wp.moveType = mRun ? WaypointMoveType.Run : WaypointMoveType.Walk;
                wp.delay = 0;
                wp.eventId = 0;
                wp.eventChance = 100;

                _path.nodes.Add(wp);
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
                Log.outError(LogFilter.Server, $"SmartAI.PausePath: Creature entry {me.GetEntry()} wanted to pause waypoint (current waypoint: {mCurrentWPID}) movement while already paused, ignoring.");
                return;
            }

            AddEscortState(SmartEscortState.Paused);
            mWPPauseTimer = delay;
            if (forced && !mWPReached)
            {
                mForcedPaused = forced;
                SetRun(mRun);
                me.StopMoving();
            }
            GetScript().ProcessEventsFor(SmartEvents.WaypointPaused, null, mCurrentWPID, GetScript().GetPathId());
        }

        public void StopPath(uint DespawnTime = 0, uint quest = 0, bool fail = false)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (quest != 0)
                mEscortQuestID = quest;

            if (mDespawnState != 2)
                SetDespawnTime(DespawnTime);

            me.StopMoving();
            me.GetMotionMaster().MovementExpired(false);
            me.GetMotionMaster().MoveIdle();
            GetScript().ProcessEventsFor(SmartEvents.WaypointStopped, null, mCurrentWPID, GetScript().GetPathId());
            EndPath(fail);
        }

        public void EndPath(bool fail = false)
        {
            RemoveEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning);
            _path.nodes.Clear();
            mWPPauseTimer = 0;

            if (mEscortNPCFlags != 0)
            {
                me.SetFlag(UnitFields.NpcFlags, mEscortNPCFlags);
                mEscortNPCFlags = 0;
            }

            List<WorldObject> targets = GetScript().GetTargetList(SharedConst.SmartEscortTargets);
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
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
                        {
                            Player groupGuy = groupRef.GetSource();

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

            GetScript().ProcessEventsFor(SmartEvents.WaypointEnded, null, mCurrentWPID, GetScript().GetPathId());

            if (mCanRepeatPath)
            {
                if (IsAIControlled())
                    StartPath(mRun, GetScript().GetPathId(), mCanRepeatPath);
            }
            else
                GetScript().SetPathId(0);

            if (mDespawnState == 1)
                StartDespawn();
        }

        public void ResumePath()
        {
            GetScript().ProcessEventsFor(SmartEvents.WaypointResumed, null, mCurrentWPID, GetScript().GetPathId());
            RemoveEscortState(SmartEscortState.Paused);
            mForcedPaused = false;
            mWPReached = false;
            mWPPauseTimer = 0;
            SetRun(mRun);

            WaypointMovementGenerator move = (WaypointMovementGenerator)me.GetMotionMaster().top();
            if (move != null)
                move.GetTrackerTimer().Reset(1);
        }

        void ReturnToLastOOCPos()
        {
            if (!IsAIControlled())
                return;

            me.SetWalk(false);
            float x, y, z, o;
            me.GetHomePosition(out x, out y, out z, out o);
            me.GetMotionMaster().MovePoint(EventId.SmartEscortLastOCCPoint, x, y, z);
        }

        void UpdatePath(uint diff)
        {
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (mEscortInvokerCheckTimer < diff)
            {
                if (!IsEscortInvokerInRange())
                {
                    StopPath(0, mEscortQuestID, true);

                    // allow to properly hook out of range despawn action, which in most cases should perform the same operation as dying
                    GetScript().ProcessEventsFor(SmartEvents.Death, me);
                    me.DespawnOrUnsummon(1);
                    return;
                }
                mEscortInvokerCheckTimer = 1000;
            }
            else
                mEscortInvokerCheckTimer -= diff;

            // handle pause
            if (HasEscortState(SmartEscortState.Paused))
            {
                if (mWPPauseTimer <= diff)
                {
                    if (!me.IsInCombat() && !HasEscortState(SmartEscortState.Returning) && (mWPReached || mForcedPaused))
                        ResumePath();
                }
                else
                    mWPPauseTimer -= diff;
            }
            else if (m_Ended) // end path
            {
                m_Ended = false;
                StopPath();
                return;
            }

            if (HasEscortState(SmartEscortState.Returning))
            {
                if (mOOCReached)//reached OOC WP
                {
                    mOOCReached = false;
                    RemoveEscortState(SmartEscortState.Returning);
                    if (!HasEscortState(SmartEscortState.Paused))
                        ResumePath();
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            GetScript().OnUpdate(diff);
            UpdatePath(diff);
            UpdateDespawn(diff);

            UpdateFollow(diff);

            if (!IsAIControlled())
                return;

            if (!UpdateVictim())
                return;

            if (mCanAutoAttack)
                DoMeleeAttackIfReady();
        }

        bool IsEscortInvokerInRange()
        {
            var targets = GetScript().GetTargetList(SharedConst.SmartEscortTargets);
            if (targets != null)
            {
                if (targets.Count == 1 && GetScript().IsPlayer(targets.First()))
                {
                    Player player = targets.First().ToPlayer();
                    if (me.GetDistance(player) <= SMART_ESCORT_MAX_PLAYER_DIST)
                        return true;

                    Group group = player.GetGroup();
                    if (group)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
                        {
                            Player groupGuy = groupRef.GetSource();

                            if (me.GetDistance(groupGuy) <= SMART_ESCORT_MAX_PLAYER_DIST)
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
                            if (me.GetDistance(obj.ToPlayer()) <= SMART_ESCORT_MAX_PLAYER_DIST)
                                return true;
                        }
                    }
                }
            }
            return true;//escort targets were not set, ignore range check
        }

        void MovepointReached(uint id)
        {
            // override the id, path can be resumed any time and counter will reset
            // mCurrentWPID holds proper id

            // both point movement and escort generator can enter this function
            if (id == EventId.SmartEscortLastOCCPoint)
            {
                mOOCReached = true;
                return;
            }

            mCurrentWPID = id + 1; // in SmartAI increase by 1

            mWPReached = true;
            GetScript().ProcessEventsFor(SmartEvents.WaypointReached, null, mCurrentWPID, GetScript().GetPathId());

            if (HasEscortState(SmartEscortState.Paused))
                me.StopMoving();
            else if (HasEscortState(SmartEscortState.Escorting) && me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
            {
                mWPReached = false;
                if (mCurrentWPID == _path.nodes.Count)
                    m_Ended = true;
                else
                    SetRun(mRun);
            }
        }

        public override void MovementInform(MovementGeneratorType MovementType, uint Data)
        {
            if (MovementType == MovementGeneratorType.Point && Data == EventId.SmartEscortLastOCCPoint)
                me.ClearUnitState(UnitState.Evade);

            GetScript().ProcessEventsFor(SmartEvents.Movementinform, null, (uint)MovementType, Data);
            if (!HasEscortState(SmartEscortState.Escorting))
                return;

            if (MovementType == MovementGeneratorType.Waypoint || (MovementType == MovementGeneratorType.Point && Data == EventId.SmartEscortLastOCCPoint))
                MovepointReached(Data);
        }

        void RemoveAuras()
        {
            //fixme: duplicated logic in CreatureAI._EnterEvadeMode (could use RemoveAllAurasExceptType)
            foreach (var pair in me.GetAppliedAuras())
            {
                Aura aura = pair.Value.GetBase();
                if (!aura.IsPassive() && !aura.HasEffectType(AuraType.ControlVehicle) && !aura.HasEffectType(AuraType.CloneCaster) && aura.GetCasterGUID() != me.GetGUID())
                    me.RemoveAura(pair);
            }
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            if (mEvadeDisabled)
            {
                GetScript().ProcessEventsFor(SmartEvents.Evade);
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
                    GetScript().OnReset();
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

            if (AssistPlayerInCombatAgainst(who))
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

            //never attack friendly
            if (me.IsFriendlyTo(who))
                return false;

            //too far away and no free sight?
            if (me.IsWithinDistInMap(who, SMART_MAX_AID_DIST) && me.IsWithinLOSInMap(who))
            {
                //already fighting someone?
                if (me.GetVictim() == null)
                {
                    AttackStart(who);
                    return true;
                }
                else
                {
                    who.SetInCombatWith(me);
                    me.AddThreat(who, 0.0f);
                    return true;
                }
            }

            return false;
        }

        public override void JustRespawned()
        {
            mDespawnTime = 0;
            mRespawnTime = 0;
            mDespawnState = 0;
            mEscortState = SmartEscortState.None;
            me.SetVisible(true);
            if (me.getFaction() != me.GetCreatureTemplate().Faction)
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

                if (!UpdateVictim() && me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Idle && me.GetWaypointPath() != 0)
                    me.GetMotionMaster().MovePath(me.GetWaypointPath(), true);
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
            GetScript().ProcessEventsFor(SmartEvents.Death, killer);
            if (HasEscortState(SmartEscortState.Escorting))
                EndPath(true);
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
            if (me.HasFlag(UnitFields.Flags, UnitFlags.PlayerControlled))
            {
                if (who && mCanAutoAttack)
                    me.Attack(who, true);
                return;
            }

            if (who && me.Attack(who, me.IsWithinMeleeRange(who)))
            {
                if (mCanCombatMove)
                {
                    SetRun(mRun);

                    MovementGeneratorType type = me.GetMotionMaster().GetMotionSlotType(MovementSlot.Active);
                    if (type == MovementGeneratorType.Waypoint || type == MovementGeneratorType.Point)
                        me.StopMoving();

                    me.GetMotionMaster().MoveChase(who);
                }
            }
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellHit, caster, 0, 0, false, spell);
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            GetScript().ProcessEventsFor(SmartEvents.SpellhitTarget, target, 0, 0, false, spell);
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
                mJustReset = true;
            JustReachedHome();
            GetScript().ProcessEventsFor(SmartEvents.Respawn);
        }

        public override void OnCharmed(bool apply)
        {
            if (apply) // do this before we change charmed state, as charmed state might prevent these things from processing
            {
                if (HasEscortState(SmartEscortState.Escorting | SmartEscortState.Paused | SmartEscortState.Returning))
                    EndPath(true);
                me.StopMoving();
            }
            mIsCharmed = apply;

            if (!apply && !me.IsInEvadeMode())
            {
                if (mCanRepeatPath)
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

        public void SetFly(bool fly)
        {
            me.SetDisableGravity(fly);
        }

        public void SetSwim(bool swim)
        {
            me.SetSwim(swim);
        }

        public void SetEvadeDisabled(bool disable)
        {
            mEvadeDisabled = disable;
        }

        public override void sGossipHello(Player player)
        {
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player);
        }

        public override void sGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, menuId, gossipListId);
        }

        public override void sGossipSelectCode(Player player, uint menuId, uint gossipListId, string code) { }

        public override void sQuestAccept(Player player, Quest quest)
        {
            GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id);
        }

        public override void sQuestReward(Player player, Quest quest, uint opt)
        {
            GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt);
        }

        public override bool sOnDummyEffect(Unit caster, uint spellId, int effIndex)
        {
            GetScript().ProcessEventsFor(SmartEvents.DummyEffect, caster, spellId, (uint)effIndex);
            return true;
        }

        public void SetCombatMove(bool on)
        {
            if (mCanCombatMove == on)
                return;

            mCanCombatMove = on;
            if (!IsAIControlled())
                return;

            if (!HasEscortState(SmartEscortState.Escorting))
            {
                if (on && me.GetVictim() != null)
                {
                    if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Idle)
                    {
                        SetRun(mRun);
                        me.GetMotionMaster().MoveChase(me.GetVictim());
                        me.CastStop();
                    }
                }
                else
                {
                    me.StopMoving();
                    if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Chase)
                        me.GetMotionMaster().Clear(false);
                    me.GetMotionMaster().MoveIdle();
                }
            }
        }

        public void SetFollow(Unit target, float dist, float angle, uint credit, uint end, uint creditType)
        {
            if (target == null)
                return;

            mFollowGuid = target.GetGUID();
            mFollowDist = dist >= 0.0f ? dist : SharedConst.PetFollowDist;
            mFollowAngle = angle >= 0.0f ? angle : me.GetFollowAngle();
            mFollowArrivedTimer = 1000;
            mFollowCredit = credit;
            mFollowArrivedEntry = end;
            mFollowCreditType = creditType;
            SetRun(mRun);
            me.GetMotionMaster().MoveFollow(target, mFollowDist, mFollowAngle);
        }

        public void SetScript9(SmartScriptHolder e, uint entry, Unit invoker)
        {
            if (invoker != null)
                GetScript().mLastInvoker = invoker.GetGUID();
            GetScript().SetScript9(e, entry);
        }

        public override void sOnGameEvent(bool start, ushort eventId)
        {
            GetScript().ProcessEventsFor(start ? SmartEvents.GameEventStart : SmartEvents.GameEventEnd, null, eventId);
        }

        public override void OnSpellClick(Unit clicker, ref bool result)
        {
            if (!result)
                return;

            GetScript().ProcessEventsFor(SmartEvents.OnSpellclick, clicker);
        }

        public void UpdateFollow(uint diff)
        {
            if (!mFollowGuid.IsEmpty())
            {
                if (mFollowArrivedTimer < diff)
                {
                    if (me.FindNearestCreature(mFollowArrivedEntry, SharedConst.InteractionDistance, true) != null)
                    {
                        Player player = Global.ObjAccessor.GetPlayer(me, mFollowGuid);
                        if (player != null)
                        {
                            if (mFollowCreditType == 0)
                                player.RewardPlayerAndGroupAtEvent(mFollowCredit, me);
                            else
                                player.GroupEventHappens(mFollowCredit, me);
                        }
                        mFollowGuid.Clear();
                        mFollowDist = 0;
                        mFollowAngle = 0;
                        mFollowCredit = 0;
                        mFollowArrivedTimer = 1000;
                        mFollowArrivedEntry = 0;
                        mFollowCreditType = 0;
                        SetDespawnTime(5000);
                        me.StopMoving();
                        me.GetMotionMaster().MoveIdle();
                        StartDespawn();
                        GetScript().ProcessEventsFor(SmartEvents.FollowCompleted);
                        return;
                    }
                    mFollowArrivedTimer = 1000;
                }
                else mFollowArrivedTimer -= diff;
            }
        }

        bool HasEscortState(SmartEscortState uiEscortState) { return mEscortState.HasAnyFlag(uiEscortState); }
        void AddEscortState(SmartEscortState uiEscortState) { mEscortState |= uiEscortState; }
        void RemoveEscortState(SmartEscortState uiEscortState) { mEscortState &= ~uiEscortState; }
        public void SetAutoAttack(bool on) { mCanAutoAttack = on; }
        public bool CanCombatMove() { return mCanCombatMove; }

        public SmartScript GetScript() { return mScript; }

        public void SetInvincibilityHpLevel(uint level) { mInvincibilityHpLevel = level; }

        public void SetDespawnTime(uint t, uint r = 0)
        {
            mDespawnTime = t;
            mRespawnTime = r;
            mDespawnState = (uint)(t != 0 ? 1 : 0);
        }

        public void StartDespawn() { mDespawnState = 2; }

        bool mIsCharmed;
        uint mFollowCreditType;
        uint mFollowArrivedTimer;
        uint mFollowCredit;
        uint mFollowArrivedEntry;
        ObjectGuid mFollowGuid;
        float mFollowDist;
        float mFollowAngle;

        SmartScript mScript = new SmartScript();
        SmartEscortState mEscortState;
        uint mCurrentWPID;
        bool mWPReached;
        bool mOOCReached;
        bool m_Ended;
        uint mWPPauseTimer;
        uint mEscortNPCFlags;
        bool mCanRepeatPath;
        bool mRun;
        bool mEvadeDisabled;
        bool mCanAutoAttack;
        bool mCanCombatMove;
        bool mForcedPaused;
        uint mInvincibilityHpLevel;

        WaypointPath _path = new WaypointPath();
        uint mDespawnTime;
        uint mRespawnTime;
        uint mDespawnState;

        public uint mEscortQuestID;

        uint mEscortInvokerCheckTimer;
        bool mJustReset;
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
            GetScript().OnInitialize(go);
            GetScript().ProcessEventsFor(SmartEvents.Respawn);
        }

        public override void Reset()
        {
            GetScript().OnReset();
        }

        public override bool GossipHello(Player player, bool isUse)
        {
            Log.outDebug(LogFilter.ScriptsAi, "SmartGameObjectAI.GossipHello");
            GetScript().ProcessEventsFor(SmartEvents.GossipHello, player, 0, 0, false, null, go);
            return false;
        }

        public override bool GossipSelect(Player player, uint sender, uint action)
        {
            GetScript().ProcessEventsFor(SmartEvents.GossipSelect, player, sender, action, false, null, go);
            return false;
        }

        public override bool GossipSelectCode(Player player, uint sender, uint action, string code)
        {
            return false;
        }

        public override bool QuestAccept(Player player, Quest quest)
        {
            GetScript().ProcessEventsFor(SmartEvents.AcceptedQuest, player, quest.Id, 0, false, null, go);
            return false;
        }

        public override bool QuestReward(Player player, Quest quest, uint opt)
        {
            GetScript().ProcessEventsFor(SmartEvents.RewardQuest, player, quest.Id, opt, false, null, go);
            return false;
        }

        public override uint GetDialogStatus(Player player)
        {
            return 100;
        }

        public override void Destroyed(Player player, uint eventId)
        {
            GetScript().ProcessEventsFor(SmartEvents.Death, player, eventId, 0, false, null, go);
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

        public override void OnStateChanged(uint state, Unit unit)
        {
            GetScript().ProcessEventsFor(SmartEvents.GoStateChanged, unit, state);
        }

        public override void EventInform(uint eventId)
        {
            GetScript().ProcessEventsFor(SmartEvents.GoEventInform, null, eventId);
        }

        public SmartScript GetScript() { return mScript; }

        SmartScript mScript;
    }

    public enum SmartEscortState
    {
        None = 0x00,                        //nothing in progress
        Escorting = 0x01,                        //escort is in progress
        Returning = 0x02,                        //escort is returning after being in combat
        Paused = 0x04                         //will not proceed with waypoints before state is removed
    }
}
