// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using System;

namespace Game.AI
{
    enum FollowState
    {
        None = 0x00,
        Inprogress = 0x01,                    //must always have this state for any follow
        Paused = 0x02,                        //disables following
        Complete = 0x04,                      //follow is completed and may end
        PreEvent = 0x08,                      //not implemented (allow pre event to run, before follow is initiated)
        PostEvent = 0x10                      //can be set at complete and allow post event to run
    }

    class FollowerAI : ScriptedAI
    {
        ObjectGuid _leaderGUID;
        uint _updateFollowTimer;
        FollowState _followState;
        uint _questForFollow;

        public FollowerAI(Creature creature) : base(creature)
        {
            _updateFollowTimer = 2500;
            _followState = FollowState.None;
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (HasFollowState(FollowState.Inprogress) && !ShouldAssistPlayerInCombatAgainst(who))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            if (!HasFollowState(FollowState.Inprogress) || _leaderGUID.IsEmpty() || _questForFollow == 0)
                return;

            // @todo need a better check for quests with time limit.
            Player player = GetLeaderForFollower();
            if (player != null)
            {
                Group group = player.GetGroup();
                if (group != null)
                {
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                    {
                        Player member = groupRef.GetSource();
                        if (member != null)
                            if (member.IsInMap(player))
                                member.FailQuest(_questForFollow);
                    }
                }
                else
                    player.FailQuest(_questForFollow);
            }
        }

        public override void JustReachedHome()
        {
            if (!HasFollowState(FollowState.Inprogress))
                return;

            Player player = GetLeaderForFollower();
            if (player != null)
            {
                if (HasFollowState(FollowState.Paused))
                    return;
                me.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
            }
            else
                me.DespawnOrUnsummon();
        }

        public override void OwnerAttackedBy(Unit attacker)
        {
            if (!me.HasReactState(ReactStates.Passive) && ShouldAssistPlayerInCombatAgainst(attacker))
                me.EngageWithTarget(attacker);
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (HasFollowState(FollowState.Inprogress) && !me.IsEngaged())
            {
                if (_updateFollowTimer <= uiDiff)
                {
                    if (HasFollowState(FollowState.Complete) && !HasFollowState(FollowState.PostEvent))
                    {
                        Log.outDebug(LogFilter.ScriptsAi, $"FollowerAI::UpdateAI: is set completed, despawns. ({me.GetGUID()})");
                        me.DespawnOrUnsummon();
                        return;
                    }

                    bool maxRangeExceeded = true;
                    bool questAbandoned = (_questForFollow != 0);

                    Player player = GetLeaderForFollower();
                    if (player != null)
                    {
                        Group group = player.GetGroup();
                        if (group != null)
                        {
                            for (GroupReference groupRef = group.GetFirstMember(); groupRef != null && (maxRangeExceeded || questAbandoned); groupRef = groupRef.Next())
                            {
                                Player member = groupRef.GetSource();
                                if (member == null)
                                    continue;

                                if (maxRangeExceeded && me.IsWithinDistInMap(member, 100.0f))
                                    maxRangeExceeded = false;

                                if (questAbandoned)
                                {
                                    QuestStatus status = member.GetQuestStatus(_questForFollow);
                                    if ((status == QuestStatus.Complete) || (status == QuestStatus.Incomplete))
                                        questAbandoned = false;
                                }
                            }
                        }
                        else
                        {
                            if (me.IsWithinDistInMap(player, 100.0f))
                                maxRangeExceeded = false;

                            if (questAbandoned)
                            {
                                QuestStatus status = player.GetQuestStatus(_questForFollow);
                                if ((status == QuestStatus.Complete) || (status == QuestStatus.Incomplete))
                                    questAbandoned = false;
                            }
                        }
                    }

                    if (maxRangeExceeded || questAbandoned)
                    {
                        Log.outDebug(LogFilter.ScriptsAi, $"FollowerAI::UpdateAI: failed because player/group was to far away or not found ({me.GetGUID()})");
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
            CreatureData cdata = me.GetCreatureData();
            if (cdata != null)
            {
                if (WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && cdata.spawnGroupData.flags.HasFlag(SpawnGroupFlags.EscortQuestNpc))
                    me.SaveRespawnTime(me.GetRespawnDelay());
            }

            if (me.IsEngaged())
            {
                Log.outDebug(LogFilter.Scripts, $"FollowerAI::StartFollow: attempt to StartFollow while in combat. ({me.GetGUID()})");
                return;
            }

            if (HasFollowState(FollowState.Inprogress))
            {
                Log.outError(LogFilter.Scenario, $"FollowerAI::StartFollow: attempt to StartFollow while already following. ({me.GetGUID()})");
                return;
            }

            //set variables
            _leaderGUID = player.GetGUID();

            if (factionForFollower != 0)
                me.SetFaction(factionForFollower);

            _questForFollow = quest.Id;

            me.GetMotionMaster().Clear(MovementGeneratorPriority.Normal);
            me.PauseMovement();

            me.ReplaceAllNpcFlags(NPCFlags.None);
            me.ReplaceAllNpcFlags2(NPCFlags2.None);

            AddFollowState(FollowState.Inprogress);

            me.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);

            Log.outDebug(LogFilter.Scripts, $"FollowerAI::StartFollow: start follow {player.GetName()} - {_leaderGUID} ({me.GetGUID()})");
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
            if (player != null)
            {
                if (player.IsAlive())
                    return player;
                else
                {
                    Group group = player.GetGroup();
                    if (group != null)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                        {
                            Player member = groupRef.GetSource();
                            if (member != null && me.IsWithinDistInMap(member, 100.0f) && member.IsAlive())
                            {
                                Log.outDebug(LogFilter.Scripts, $"FollowerAI::GetLeaderForFollower: GetLeader changed and returned new leader. ({me.GetGUID()})");
                                _leaderGUID = member.GetGUID();
                                return member;
                            }
                        }
                    }
                }
            }

            Log.outDebug(LogFilter.Scripts, $"FollowerAI::GetLeaderForFollower: GetLeader can not find suitable leader. ({me.GetGUID()})");
            return null;
        }

        //This part provides assistance to a player that are attacked by who, even if out of normal aggro range
        //It will cause me to attack who that are attacking _any_ player (which has been confirmed may happen also on offi)
        //The flag (type_flag) is unconfirmed, but used here for further research and is a good candidate.
        bool ShouldAssistPlayerInCombatAgainst(Unit who)
        {
            if (who == null || who.GetVictim() == null)
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureDifficulty().TypeFlags.HasFlag(CreatureTypeFlags.CanAssist))
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
            if (!me.IsWithinDistInMap(who, 100.0f) || !me.IsWithinLOSInMap(who))
                return false;

            return true;
        }

        public override bool IsEscorted() { return HasFollowState(FollowState.Inprogress); }

        bool HasFollowState(FollowState uiFollowState) { return (_followState & uiFollowState) != 0; }

        void AddFollowState(FollowState uiFollowState) { _followState |= uiFollowState; }

        void RemoveFollowState(FollowState uiFollowState) { _followState &= ~uiFollowState; }
    }
}
