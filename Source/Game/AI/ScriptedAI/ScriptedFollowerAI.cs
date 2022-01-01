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
using System;

namespace Game.AI
{
    enum FollowState
    {
        None = 0x000,
        Inprogress = 0x001,                    //must always have this state for any follow
        Returning = 0x002,                     //when returning to combat start after being in combat
        Paused = 0x004,                        //disables following
        Complete = 0x008,                      //follow is completed and may end
        PreEvent = 0x010,                      //not implemented (allow pre event to run, before follow is initiated)
        PostEvent = 0x020                      //can be set at complete and allow post event to run
    }

    class FollowerAI : ScriptedAI
    {
        ObjectGuid _leaderGUID;
        uint _updateFollowTimer;
        FollowState _followState;

        Quest _questForFollow;                     //normally we have a quest

        public FollowerAI(Creature creature) : base(creature)
        {
            _updateFollowTimer = 2500;
            _followState = FollowState.None;
            _questForFollow = null;
        }

        public override void MovementInform(MovementGeneratorType motionType, uint pointId)
        {
            if (motionType != MovementGeneratorType.Point || !HasFollowState(FollowState.Inprogress))
                return;

            if (pointId == 0xFFFFFF)
            {
                if (GetLeaderForFollower())
                {
                    if (!HasFollowState(FollowState.Paused))
                        AddFollowState(FollowState.Returning);
                }
                else
                    me.DespawnOrUnsummon();
            }
        }

        public override void AttackStart(Unit who)
        {
            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            // TODO: what in the world is this?
            if (me.HasReactState(ReactStates.Aggressive) && !me.HasUnitState(UnitState.Stunned) && who.IsTargetableForAttack() && who.IsInAccessiblePlaceFor(me))
                return;

            if (HasFollowState(FollowState.Inprogress) && AssistPlayerInCombatAgainst(who))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            if (!HasFollowState(FollowState.Inprogress) || _leaderGUID.IsEmpty() || _questForFollow == null)
                return;

            // @todo need a better check for quests with time limit.
            Player player = GetLeaderForFollower();
            if (player)
            {
                Group group = player.GetGroup();
                if (group)
                {
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                    {
                        Player member = groupRef.GetSource();
                        if (member)
                            if (member.IsInMap(player))
                                member.FailQuest(_questForFollow.Id);
                    }
                }
                else
                    player.FailQuest(_questForFollow.Id);
            }
        }

        public override void JustAppeared()
        {
            _followState = FollowState.None;

            if (!IsCombatMovementAllowed())
                SetCombatMovement(true);

            if (me.GetFaction() != me.GetCreatureTemplate().Faction)
                me.SetFaction(me.GetCreatureTemplate().Faction);

            Reset();
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!me.IsAlive())
            {
                EngagementOver();
                return;
            }

            me.RemoveAllAuras();
            me.CombatStop(true);
            me.SetLootRecipient(null);
            me.SetCannotReachTarget(false);
            me.DoNotReacquireTarget();

            EngagementOver();

            if (HasFollowState(FollowState.Inprogress))
            {
                Log.outDebug(LogFilter.Scripts, "FollowerAI left combat, returning to CombatStartPosition.");

                if (me.HasUnitState(UnitState.Chase))
                    me.GetMotionMaster().Remove(MovementGeneratorType.Chase);
            }
            else
                me.GetMotionMaster().MoveTargetedHome();

            Reset();
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (HasFollowState(FollowState.Inprogress) && !me.GetVictim())
            {
                if (_updateFollowTimer <= uiDiff)
                {
                    if (HasFollowState(FollowState.Complete) && !HasFollowState(FollowState.PostEvent))
                    {
                        Log.outDebug(LogFilter.Scripts, "FollowerAI is set completed, despawns.");
                        me.DespawnOrUnsummon();
                        return;
                    }

                    bool bIsMaxRangeExceeded = true;

                    Player player = GetLeaderForFollower();
                    if (player)
                    {
                        if (HasFollowState(FollowState.Returning))
                        {
                            Log.outDebug(LogFilter.Scripts, "FollowerAI is returning to leader.");

                            RemoveFollowState(FollowState.Returning);
                            me.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            return;
                        }

                        Group group = player.GetGroup();
                        if (group)
                        {
                            for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                            {
                                Player member = groupRef.GetSource();
                                if (member && me.IsWithinDistInMap(member, 100.0f))
                                {
                                    bIsMaxRangeExceeded = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (me.IsWithinDistInMap(player, 100.0f))
                                bIsMaxRangeExceeded = false;
                        }
                    }

                    if (bIsMaxRangeExceeded)
                    {
                        Log.outDebug(LogFilter.Scripts, "FollowerAI failed because player/group was to far away or not found");
                        me.DespawnOrUnsummon();
                        return;
                    }

                    _updateFollowTimer = 1000;
                }
                else
                    _updateFollowTimer -= uiDiff;
            }

            UpdateFollowerAI(uiDiff);
        }

        void UpdateFollowerAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public void StartFollow(Player player, uint factionForFollower = 0, Quest quest = null)
        {
            if (me.GetVictim())
            {
                Log.outDebug(LogFilter.Scripts, "FollowerAI attempt to StartFollow while in combat.");
                return;
            }

            if (HasFollowState(FollowState.Inprogress))
            {
                Log.outError(LogFilter.Scenario, "FollowerAI attempt to StartFollow while already following.");
                return;
            }

            //set variables
            _leaderGUID = player.GetGUID();

            if (factionForFollower != 0)
                me.SetFaction(factionForFollower);

            _questForFollow = quest;

            me.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);
            me.PauseMovement();

            me.SetNpcFlags(NPCFlags.None);
            me.SetNpcFlags2(NPCFlags2.None);

            AddFollowState(FollowState.Inprogress);

            me.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);

            Log.outDebug(LogFilter.Scripts, "FollowerAI start follow {0} ({1})", player.GetName(), _leaderGUID.ToString());
        }

        public void SetFollowPaused(bool paused)
        {
            if (!HasFollowState(FollowState.Inprogress) || HasFollowState(FollowState.Complete))
                return;

            if (paused)
            {
                AddFollowState(FollowState.Paused);

                if (me.HasUnitState(UnitState.Follow))
                    me.GetMotionMaster().Remove(MovementGeneratorType.Follow);
            }
            else
            {
                RemoveFollowState(FollowState.Paused);

                Player leader = GetLeaderForFollower();
                if (leader != null)
                    me.GetMotionMaster().MoveFollow(leader, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
            }
        }

        public void SetFollowComplete(bool withEndEvent = false)
        {
            if (me.HasUnitState(UnitState.Follow))
                me.GetMotionMaster().Remove(MovementGeneratorType.Follow);

            if (withEndEvent)
                AddFollowState(FollowState.PostEvent);
            else
            {
                if (HasFollowState(FollowState.PostEvent))
                    RemoveFollowState(FollowState.PostEvent);
            }

            AddFollowState(FollowState.Complete);
        }

        Player GetLeaderForFollower()
        {
            Player player = Global.ObjAccessor.GetPlayer(me, _leaderGUID);
            if (player)
            {
                if (player.IsAlive())
                    return player;
                else
                {
                    Group group = player.GetGroup();
                    if (group)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                        {
                            Player member = groupRef.GetSource();
                            if (member && me.IsWithinDistInMap(member, 100.0f) && member.IsAlive())
                            {
                                Log.outDebug(LogFilter.Scripts, "FollowerAI GetLeader changed and returned new leader.");
                                _leaderGUID = member.GetGUID();
                                return member;
                            }
                        }
                    }
                }
            }

            Log.outDebug(LogFilter.Scripts, "FollowerAI GetLeader can not find suitable leader.");
            return null;
        }

        //This part provides assistance to a player that are attacked by who, even if out of normal aggro range
        //It will cause me to attack who that are attacking _any_ player (which has been confirmed may happen also on offi)
        //The flag (type_flag) is unconfirmed, but used here for further research and is a good candidate.
        bool AssistPlayerInCombatAgainst(Unit who)
        {
            if (!who || !who.GetVictim())
                return false;

            if (me.HasReactState(ReactStates.Passive))
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
                return false;

            //not a player
            if (!who.GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself())
                return false;

            if (!who.IsInAccessiblePlaceFor(me))
                return false;

            if (!CanAIAttack(who))
                return false;

            // we cannot attack in evade mode
            if (me.IsInEvadeMode())
                return false;

            // or if enemy is in evade mode
            if (who.GetTypeId() == TypeId.Unit && who.ToCreature().IsInEvadeMode())
                return false;

            //never attack friendly
            if (me.IsFriendlyTo(who))
                return false;

            //too far away and no free sight?
            if (me.IsWithinDistInMap(who, 100.0f) && me.IsWithinLOSInMap(who))
            {
                me.EngageWithTarget(who);
                return true;
            }

            return false;
        }

        bool HasFollowState(FollowState uiFollowState) { return (_followState & uiFollowState) != 0; }

        void AddFollowState(FollowState uiFollowState) { _followState |= uiFollowState; }
        void RemoveFollowState(FollowState uiFollowState) { _followState &= ~uiFollowState; }
    }
}
