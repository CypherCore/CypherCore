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
using System.Collections.Generic;
using System.Linq;

namespace Game.AI
{
    public class NpcEscortAI : ScriptedAI
    {
        public NpcEscortAI(Creature creature) : base(creature)
        {
            m_uiPlayerGUID = ObjectGuid.Empty;
            m_uiWPWaitTimer = 2500;
            m_uiPlayerCheckTimer = 1000;
            m_uiEscortState = EscortState.None;
            MaxPlayerDistance = 50;
            m_pQuestForEscort = null;
            m_bIsActiveAttacker = true;
            m_bIsRunning = false;
            m_bCanInstantRespawn = false;
            m_bCanReturnToStart = false;
            DespawnAtEnd = true;
            DespawnAtFar = true;
            ScriptWP = false;
            HasImmuneToNPCFlags = false;
        }

        public override void AttackStart(Unit target)
        {
            if (target == null)
                return;

            if (me.Attack(target, true))
            {
                if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                    me.GetMotionMaster().MovementExpired();

                if (IsCombatMovementAllowed())
                    me.GetMotionMaster().MoveChase(target);
            }
        }

        //see followerAI
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
            if (me.IsWithinDistInMap(who, GetMaxPlayerDistance()) && me.IsWithinLOSInMap(who))
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
            if (me.HasReactState(ReactStates.Aggressive) && !me.HasUnitState(UnitState.Stunned) && who.IsTargetableForAttack() && who.IsInAccessiblePlaceFor(me))
            {
                if (HasEscortState(EscortState.Escorting) && AssistPlayerInCombatAgainst(who))
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
            if (!HasEscortState(EscortState.Escorting) || m_uiPlayerGUID.IsEmpty() || m_pQuestForEscort == null)
                return;
            
            Player player = GetPlayerForEscort();
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
                                member.FailQuest(m_pQuestForEscort.Id);
                    }
                }
                else
                    player.FailQuest(m_pQuestForEscort.Id);
            }
        }

        public override void JustRespawned()
        {
            m_uiEscortState = EscortState.None;

            if (!IsCombatMovementAllowed())
                SetCombatMovement(true);

            //add a small delay before going to first waypoint, normal in near all cases
            m_uiWPWaitTimer = 2500;

            if (me.GetFaction() != me.GetCreatureTemplate().Faction)
                me.RestoreFaction();

            Reset();
        }

        void ReturnToLastPoint()
        {
            me.GetMotionMaster().MovePoint(0xFFFFFF, me.GetHomePosition());
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            me.RemoveAllAuras();
            me.DeleteThreatList();
            me.CombatStop(true);
            me.SetLootRecipient(null);

            if (HasEscortState(EscortState.Escorting))
            {
                AddEscortState(EscortState.Returning);
                ReturnToLastPoint();
                Log.outDebug(LogFilter.Scripts, "EscortAI has left combat and is now returning to last point");
            }
            else
            {
                me.GetMotionMaster().MoveTargetedHome();
                if (HasImmuneToNPCFlags)
                    me.AddUnitFlag(UnitFlags.ImmuneToNpc);
                Reset();
            }
        }

        bool IsPlayerOrGroupInRange()
        {
            Player player = GetPlayerForEscort();
            if (player)
            {
                Group group = player.GetGroup();
                if (group)
                {
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.Next())
                    {
                        Player member = groupRef.GetSource();
                        if (member)
                            if (me.IsWithinDistInMap(member, GetMaxPlayerDistance()))
                                return true;
                    }
                }
                else if (me.IsWithinDistInMap(player, GetMaxPlayerDistance()))
                    return true;
            }

            return false;
        }

        public override void UpdateAI(uint diff)
        {
            //Waypoint Updating
            if (HasEscortState(EscortState.Escorting) && !me.GetVictim() && m_uiWPWaitTimer != 0 && !HasEscortState(EscortState.Returning))
            {
                if (m_uiWPWaitTimer <= diff)
                {
                    //End of the line
                    if (WaypointList[CurrentWPIndex] == null)
                    {
                        m_uiWPWaitTimer = 0;

                        if (DespawnAtEnd)
                        {
                            Log.outDebug(LogFilter.Scripts, "EscortAI reached end of waypoints");

                            if (m_bCanReturnToStart)
                            {
                                float fRetX, fRetY, fRetZ;
                                me.GetRespawnPosition(out fRetX, out fRetY, out fRetZ);

                                me.GetMotionMaster().MovePoint(EscortPointIds.Home, fRetX, fRetY, fRetZ);

                                m_uiWPWaitTimer = 0;

                                Log.outDebug(LogFilter.Scripts, $"EscortAI are returning home to spawn location: {EscortPointIds.Home}, {fRetX}, {fRetY}, {fRetZ}");
                                return;
                            }

                            if (m_bCanInstantRespawn)
                            {
                                me.SetDeathState(DeathState.JustDied);
                                me.Respawn();
                            }
                            else
                                me.DespawnOrUnsummon();

                            return;
                        }
                        else
                        {
                            Log.outDebug(LogFilter.Scripts, "EscortAI reached end of waypoints with Despawn off");
                            return;
                        }

                    }

                    if (!HasEscortState(EscortState.Paused))
                    {
                        var currentWp = WaypointList[CurrentWPIndex];
                        me.GetMotionMaster().MovePoint(currentWp.Id, currentWp.X, currentWp.Y, currentWp.Z);
                        Log.outDebug(LogFilter.Scripts, $"EscortAI start waypoint {currentWp.Id} ({currentWp.X}, {currentWp.Y}, {currentWp.Z}).");

                        WaypointStart(currentWp.Id);

                        m_uiWPWaitTimer = 0;
                    }
                }
            }

            //Check if player or any member of his group is within range
            if (HasEscortState(EscortState.Escorting) && !m_uiPlayerGUID.IsEmpty() && !me.GetVictim() && !HasEscortState(EscortState.Returning))
            {
                if (m_uiPlayerCheckTimer <= diff)
                {
                    if (DespawnAtFar && !IsPlayerOrGroupInRange())
                    {
                        Log.outDebug(LogFilter.Scripts, "EscortAI failed because player/group was to far away or not found");

                        if (m_bCanInstantRespawn)
                        {
                            me.SetDeathState(DeathState.JustDied);
                            me.Respawn();
                        }
                        else
                            me.DespawnOrUnsummon();

                        return;
                    }

                    m_uiPlayerCheckTimer = 1000;
                }
                else
                    m_uiPlayerCheckTimer -= diff;
            }

            UpdateEscortAI(diff);
        }

        public virtual void UpdateEscortAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override void MovementInform(MovementGeneratorType moveType, uint pointId)
        {
            if (moveType != MovementGeneratorType.Point || !HasEscortState(EscortState.Escorting))
                return;

            //Combat start position reached, continue waypoint movement
            if (pointId == EscortPointIds.LastPoint)
            {
                Log.outDebug(LogFilter.Scripts, "EscortAI has returned to original position before combat");

                me.SetWalk(!m_bIsRunning);
                RemoveEscortState(EscortState.Returning);

                if (m_uiWPWaitTimer == 0)
                    m_uiWPWaitTimer = 1;
            }
            else if (pointId == EscortPointIds.Home)
            {
                Log.outDebug(LogFilter.Scripts, "EscortAI has returned to original home location and will continue from beginning of waypoint list.");

                CurrentWPIndex = 0;
                m_uiWPWaitTimer = 1;
            }
            else
            {
                var currentWp = WaypointList[CurrentWPIndex];
                //Make sure that we are still on the right waypoint
                if (currentWp.Id != pointId)
                {
                    Log.outError(LogFilter.Misc, $"TSCR ERROR: EscortAI reached waypoint out of order {pointId}, expected {currentWp.Id}, creature entry {me.GetEntry()}");
                    return;
                }

                Log.outDebug(LogFilter.Scripts, $"EscortAI Waypoint {currentWp.Id} reached");

                //Call WP function
                WaypointReached(currentWp.Id);

                m_uiWPWaitTimer = currentWp.WaitTimeMs + 1;

                ++CurrentWPIndex;
            }
        }

        public void AddWaypoint(uint id, float x, float y, float z, uint waitTime = 0)
        {
            Escort_Waypoint t = new Escort_Waypoint(id, x, y, z, waitTime);

            WaypointList.Add(t);

            ScriptWP = true;
        }

        void FillPointMovementListForCreature()
        {
            var movePoints = Global.ScriptMgr.GetPointMoveList(me.GetEntry());
            if (movePoints.Empty())
                return;

            foreach (var point in movePoints)
            {
                Escort_Waypoint wayPoint = new Escort_Waypoint(point.uiPointId, point.fX, point.fY, point.fZ, point.uiWaitTime);
                WaypointList.Add(wayPoint);
            }
        }

        public void SetRun(bool on = true)
        {
            if (on)
            {
                if (!m_bIsRunning)
                    me.SetWalk(false);
                else
                    Log.outDebug(LogFilter.Scripts, "EscortAI attempt to set run mode, but is already running.");
            }
            else
            {
                if (m_bIsRunning)
                    me.SetWalk(true);
                else
                    Log.outDebug(LogFilter.Scripts, "EscortAI attempt to set walk mode, but is already walking.");
            }

            m_bIsRunning = on;
        }

        /// todo get rid of this many variables passed in function.
        public void Start(bool isActiveAttacker = true, bool run = false, ObjectGuid playerGUID = default, Quest quest = null, bool instantRespawn = false, bool canLoopPath = false, bool resetWaypoints = true)
        {
            if (me.GetVictim())
            {
                Log.outError(LogFilter.Server, "TSCR ERROR: EscortAI (script: {0}, creature entry: {1}) attempts to Start while in combat", me.GetScriptName(), me.GetEntry());
                return;
            }

            if (HasEscortState(EscortState.Escorting))
            {
                Log.outError(LogFilter.Scripts, "EscortAI (script: {0}, creature entry: {1}) attempts to Start while already escorting", me.GetScriptName(), me.GetEntry());
                return;
            }

            if (!ScriptWP && resetWaypoints)
            {
                if (!WaypointList.Empty())
                    WaypointList.Clear();
                FillPointMovementListForCreature();
            }


            if (WaypointList.Empty())
            {
                Log.outError(LogFilter.Scripts, $"EscortAI (script: {me.GetScriptName()}, creature entry: {me.GetEntry()}) starts with 0 waypoints (possible missing entry in script_waypoint. Quest: {(quest != null ? quest.Id : 0)}).");
                return;
            }

            //set variables
            m_bIsActiveAttacker = isActiveAttacker;
            m_bIsRunning = run;

            m_uiPlayerGUID = playerGUID;
            m_pQuestForEscort = quest;

            m_bCanInstantRespawn = instantRespawn;
            m_bCanReturnToStart = canLoopPath;

            if (m_bCanReturnToStart && m_bCanInstantRespawn)
                Log.outDebug(LogFilter.Scripts, "EscortAI is set to return home after waypoint end and instant respawn at waypoint end. Creature will never despawn.");

            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
            {
                me.GetMotionMaster().MovementExpired();
                me.GetMotionMaster().MoveIdle();
                Log.outDebug(LogFilter.Scripts, "EscortAI start with WAYPOINT_MOTION_TYPE, changed to MoveIdle.");
            }

            //disable npcflags
            me.SetNpcFlags(NPCFlags.None);
            me.SetNpcFlags2(NPCFlags2.None);
            if (me.HasUnitFlag(UnitFlags.ImmuneToNpc))
            {
                HasImmuneToNPCFlags = true;
                me.RemoveUnitFlag(UnitFlags.ImmuneToNpc);
            }

            Log.outDebug(LogFilter.Scripts, $"EscortAI started. ActiveAttacker = {m_bIsActiveAttacker}, Run = {m_bIsRunning}, PlayerGUID = {m_uiPlayerGUID.ToString()}");

            CurrentWPIndex = 0;

            //Set initial speed
            if (m_bIsRunning)
                me.SetWalk(false);
            else
                me.SetWalk(true);

            AddEscortState(EscortState.Escorting);
        }

        public void SetEscortPaused(bool on)
        {
            if (!HasEscortState(EscortState.Escorting))
                return;

            if (on)
                AddEscortState(EscortState.Paused);
            else
                RemoveEscortState(EscortState.Paused);
        }

        bool SetNextWaypoint(uint pointId, float x, float y, float z, float orientation)
        {
            me.UpdatePosition(x, y, z, orientation);
            return SetNextWaypoint(pointId, false, true);
        }

        bool SetNextWaypoint(uint pointId, bool setPosition, bool resetWaypointsOnFail)
        {
            if (!WaypointList.Empty())
                WaypointList.Clear();

            FillPointMovementListForCreature();

            if (WaypointList.Empty())
                return false;

            int size = WaypointList.Count;
            Escort_Waypoint waypoint;
            do
            {
                waypoint = WaypointList.First();
                WaypointList.RemoveAt(0);
                if (waypoint.Id == pointId)
                {
                    if (setPosition)
                        me.UpdatePosition(waypoint.X, waypoint.Y, waypoint.Z, me.GetOrientation());

                    CurrentWPIndex = 0;
                    return true;
                }
            }
            while (!WaypointList.Empty());

            // we failed.
            // we reset the waypoints in the start; if we pulled any, reset it again
            if (resetWaypointsOnFail && size != WaypointList.Count)
            {
                if (!WaypointList.Empty())
                    WaypointList.Clear();

                FillPointMovementListForCreature();
            }

            return false;
        }

        bool GetWaypointPosition(uint pointId, ref float x, ref float y, ref float z)
        {
            var waypoints = Global.ScriptMgr.GetPointMoveList(me.GetEntry());
            if (waypoints == null)
                return false;

            foreach (var pointMove in waypoints)
            {
                if (pointMove.uiPointId == pointId)
                {
                    x = pointMove.fX;
                    y = pointMove.fY;
                    z = pointMove.fZ;
                    return true;
                }
            }

            return false;
        }

        public virtual void WaypointReached(uint pointId) { }
        public virtual void WaypointStart(uint pointId) { }

        public bool HasEscortState(EscortState escortState) { return m_uiEscortState.HasAnyFlag(escortState); }
        public override bool IsEscorted() { return m_uiEscortState.HasAnyFlag(EscortState.Escorting); }

        public void SetMaxPlayerDistance(float newMax) { MaxPlayerDistance = newMax; }
        public float GetMaxPlayerDistance() { return MaxPlayerDistance; }

        public void SetDespawnAtEnd(bool despawn) { DespawnAtEnd = despawn; }
        public void SetDespawnAtFar(bool despawn) { DespawnAtFar = despawn; }
        public bool GetAttack() { return m_bIsActiveAttacker; }//used in EnterEvadeMode override
        public void SetCanAttack(bool attack) { m_bIsActiveAttacker = attack; }
        public ObjectGuid GetEventStarterGUID() { return m_uiPlayerGUID; }

        public Player GetPlayerForEscort() { return Global.ObjAccessor.GetPlayer(me, m_uiPlayerGUID); }

        void AddEscortState(EscortState escortState) { m_uiEscortState |= escortState; }
        void RemoveEscortState(EscortState escortState) { m_uiEscortState &= ~escortState; }

        ObjectGuid m_uiPlayerGUID;
        uint m_uiWPWaitTimer;
        uint m_uiPlayerCheckTimer;
        EscortState m_uiEscortState;
        float MaxPlayerDistance;

        Quest m_pQuestForEscort;                     //generally passed in Start() when regular escort script.

        List<Escort_Waypoint> WaypointList = new List<Escort_Waypoint>();
        int CurrentWPIndex;

        bool m_bIsActiveAttacker;                           //obsolete, determined by faction.
        bool m_bIsRunning;                                  //all creatures are walking by default (has flag MOVEMENTFLAG_WALK)
        bool m_bCanInstantRespawn;                          //if creature should respawn instantly after escort over (if not, database respawntime are used)
        bool m_bCanReturnToStart;                           //if creature can walk same path (loop) without despawn. Not for regular escort quests.
        bool DespawnAtEnd;
        bool DespawnAtFar;
        bool ScriptWP;
        bool HasImmuneToNPCFlags;
    }

    public enum EscortState
    {
        None = 0x000,                        //nothing in progress
        Escorting = 0x001,                        //escort are in progress
        Returning = 0x002,                        //escort is returning after being in combat
        Paused = 0x004                         //will not proceed with waypoints before state is removed
    }

    class Escort_Waypoint
    {
        public Escort_Waypoint(uint id, float x, float y, float z, uint w)
        {
            Id = id;
            X = x;
            Y = y;
            Z = z;
            WaitTimeMs = w;
        }

        public uint Id;
        public float X;
        public float Y;
        public float Z;
        public uint WaitTimeMs;
    }

    struct EscortPointIds
    {
        public const uint LastPoint = 0xFFFFFF;
        public const uint Home = 0xFFFFFE;
    }
}
