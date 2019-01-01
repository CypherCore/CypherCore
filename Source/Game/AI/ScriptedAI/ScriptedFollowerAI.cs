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
using System;

namespace Game.AI
{
    enum eFollowState
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
        public FollowerAI(Creature creature) : base(creature)
        {
            m_uiUpdateFollowTimer = 2500;
            m_uiFollowState = eFollowState.None;
            m_pQuestForFollow = null;
        }

        public override void AttackStart(Unit who)
        {
            if (!who)
                return;

            if (me.Attack(who, true))
            {
                me.AddThreat(who, 0.0f);
                me.SetInCombatWith(who);
                who.SetInCombatWith(me);

                if (me.HasUnitState(UnitState.Follow))
                    me.ClearUnitState(UnitState.Follow);

                if (IsCombatMovementAllowed())
                    me.GetMotionMaster().MoveChase(who);
            }
        }

        //This part provides assistance to a player that are attacked by who, even if out of normal aggro range
        //It will cause me to attack who that are attacking _any_ player (which has been confirmed may happen also on offi)
        //The flag (type_flag) is unconfirmed, but used here for further research and is a good candidate.
        bool AssistPlayerInCombatAgainst(Unit who)
        {
            if (!who || !who.GetVictim())
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
                return false;

            //not a player
            if (!who.GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself())
                return false;

            //never attack friendly
            if (me.IsFriendlyTo(who))
                return false;

            //too far away and no free sight?
            if (me.IsWithinDistInMap(who, 100.0f) && me.IsWithinLOSInMap(who))
            {
                //already fighting someone?
                if (!me.GetVictim())
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

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasReactState(ReactStates.Aggressive) && !me.HasUnitState(UnitState.Stunned) && who.isTargetableForAttack() && who.isInAccessiblePlaceFor(me))
            {
                if (HasFollowState(eFollowState.Inprogress) && AssistPlayerInCombatAgainst(who))
                    return;

                if (!me.CanFly() && me.GetDistanceZ(who) > SharedConst.CreatureAttackRangeZ)
                    return;

                if (me.IsHostileTo(who))
                {
                    float fAttackRadius = me.GetAttackDistance(who);
                    if (me.IsWithinDistInMap(who, fAttackRadius) && me.IsWithinLOSInMap(who))
                    {
                        if (!me.GetVictim())
                        {
                            // Clear distracted state on combat
                            if (me.HasUnitState(UnitState.Distracted))
                            {
                                me.ClearUnitState(UnitState.Distracted);
                                me.GetMotionMaster().Clear();
                            }

                            AttackStart(who);
                        }
                        else if (me.GetMap().IsDungeon())
                        {
                            who.SetInCombatWith(me);
                            me.AddThreat(who, 0.0f);
                        }
                    }
                }
            }
        }

        public override void JustDied(Unit killer)
        {
            if (!HasFollowState(eFollowState.Inprogress) || m_uiLeaderGUID.IsEmpty() || m_pQuestForFollow == null)
                return;

            // @todo need a better check for quests with time limit.
            Player player = GetLeaderForFollower();
            if (player)
            {
                Group group = player.GetGroup();
                if (group)
                {
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
                    {
                        Player member = groupRef.GetSource();
                        if (member)
                            member.FailQuest(m_pQuestForFollow.Id);
                    }
                }
                else
                    player.FailQuest(m_pQuestForFollow.Id);
            }
        }

        public override void JustRespawned()
        {
            m_uiFollowState = eFollowState.None;

            if (!IsCombatMovementAllowed())
                SetCombatMovement(true);

            if (me.getFaction() != me.GetCreatureTemplate().Faction)
                me.SetFaction(me.GetCreatureTemplate().Faction);

            Reset();
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            me.RemoveAllAuras();
            me.DeleteThreatList();
            me.CombatStop(true);
            me.SetLootRecipient(null);

            if (HasFollowState(eFollowState.Inprogress))
            {
                Log.outDebug(LogFilter.Scripts, "FollowerAI left combat, returning to CombatStartPosition.");

                if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Chase)
                {
                    float fPosX, fPosY, fPosZ;
                    me.GetPosition(out fPosX, out fPosY, out fPosZ);
                    me.GetMotionMaster().MovePoint(0xFFFFFF, fPosX, fPosY, fPosZ);
                }
            }
            else
            {
                if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Chase)
                    me.GetMotionMaster().MoveTargetedHome();
            }

            Reset();
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (HasFollowState(eFollowState.Inprogress) && !me.GetVictim())
            {
                if (m_uiUpdateFollowTimer <= uiDiff)
                {
                    if (HasFollowState(eFollowState.Complete) && !HasFollowState(eFollowState.PostEvent))
                    {
                        Log.outDebug(LogFilter.Scripts, "FollowerAI is set completed, despawns.");
                        me.DespawnOrUnsummon();
                        return;
                    }

                    bool bIsMaxRangeExceeded = true;

                    Player player = GetLeaderForFollower();
                    if (player)
                    {
                        if (HasFollowState(eFollowState.Returning))
                        {
                            Log.outDebug(LogFilter.Scripts, "FollowerAI is returning to leader.");

                            RemoveFollowState(eFollowState.Returning);
                            me.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
                            return;
                        }

                        Group group = player.GetGroup();
                        if (group)
                        {
                            for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
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

                    m_uiUpdateFollowTimer = 1000;
                }
                else
                    m_uiUpdateFollowTimer -= uiDiff;
            }

            UpdateFollowerAI(uiDiff);
        }

        void UpdateFollowerAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override void MovementInform(MovementGeneratorType motionType, uint pointId)
        {
            if (motionType != MovementGeneratorType.Point || !HasFollowState(eFollowState.Inprogress))
                return;

            if (pointId == 0xFFFFFF)
            {
                if (GetLeaderForFollower())
                {
                    if (!HasFollowState(eFollowState.Paused))
                        AddFollowState(eFollowState.Returning);
                }
                else
                    me.DespawnOrUnsummon();
            }
        }

        void StartFollow(Player player, uint factionForFollower = 0, Quest quest = null)
        {
            if (me.GetVictim())
            {
                Log.outDebug(LogFilter.Scripts, "FollowerAI attempt to StartFollow while in combat.");
                return;
            }

            if (HasFollowState(eFollowState.Inprogress))
            {
                Log.outError(LogFilter.Scenario, "FollowerAI attempt to StartFollow while already following.");
                return;
            }

            //set variables
            m_uiLeaderGUID = player.GetGUID();

            if (factionForFollower != 0)
                me.SetFaction(factionForFollower);

            m_pQuestForFollow = quest;

            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
            {
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MoveIdle();
                Log.outDebug(LogFilter.Scripts, "FollowerAI start with WAYPOINT_MOTION_TYPE, set to MoveIdle.");
            }

            me.SetUInt64Value(UnitFields.NpcFlags, (uint)NPCFlags.None);

            AddFollowState(eFollowState.Inprogress);

            me.GetMotionMaster().MoveFollow(player, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);

            Log.outDebug(LogFilter.Scripts, "FollowerAI start follow {0} ({1})", player.GetName(), m_uiLeaderGUID.ToString());
        }

        Player GetLeaderForFollower()
        {
            Player player = Global.ObjAccessor.GetPlayer(me, m_uiLeaderGUID);
            if (player)
            {
                if (player.IsAlive())
                    return player;
                else
                {
                    Group group = player.GetGroup();
                    if (group)
                    {
                        for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
                        {
                            Player member = groupRef.GetSource();

                            if (member && member.IsAlive() && me.IsWithinDistInMap(member, 100.0f))
                            {
                                Log.outDebug(LogFilter.Scripts, "FollowerAI GetLeader changed and returned new leader.");
                                m_uiLeaderGUID = member.GetGUID();
                                return member;
                            }
                        }
                    }
                }
            }

            Log.outDebug(LogFilter.Scripts, "FollowerAI GetLeader can not find suitable leader.");
            return null;
        }

        void SetFollowComplete(bool bWithEndEvent = false)
        {
            if (me.HasUnitState(UnitState.Follow))
            {
                me.ClearUnitState(UnitState.Follow);

                me.StopMoving();
                me.GetMotionMaster().Clear();
                me.GetMotionMaster().MoveIdle();
            }

            if (bWithEndEvent)
                AddFollowState(eFollowState.PostEvent);
            else
            {
                if (HasFollowState(eFollowState.PostEvent))
                    RemoveFollowState(eFollowState.PostEvent);
            }

            AddFollowState(eFollowState.Complete);
        }

        bool HasFollowState(eFollowState uiFollowState) { return (m_uiFollowState & uiFollowState) != 0; }

        void AddFollowState(eFollowState uiFollowState) { m_uiFollowState |= uiFollowState; }
        void RemoveFollowState(eFollowState uiFollowState) { m_uiFollowState &= ~uiFollowState; }

        ObjectGuid m_uiLeaderGUID;
        uint m_uiUpdateFollowTimer;
        eFollowState m_uiFollowState;

        Quest m_pQuestForFollow;                     //normally we have a quest
    }
}
