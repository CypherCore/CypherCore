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
using System.Collections.Generic;
using System.Linq;
using Game.Movement;
using Game.Maps;

namespace Game.AI
{
    public class npc_escortAI : ScriptedAI
    {
        public npc_escortAI(Creature creature) : base(creature)
        {
            m_uiPlayerGUID = ObjectGuid.Empty;
            m_uiWPWaitTimer = 1000;
            m_uiPlayerCheckTimer = 0;
            m_uiEscortState = eEscortState.None;
            MaxPlayerDistance = 50;
            LastWP = 0;
            m_pQuestForEscort = null;
            m_bIsActiveAttacker = true;
            m_bIsRunning = false;
            m_bCanInstantRespawn = false;
            m_bCanReturnToStart = false;
            DespawnAtEnd = true;
            DespawnAtFar = true;
            ScriptWP = false;
            HasImmuneToNPCFlags = false;
            m_bStarted = false;
            m_bEnded = false;
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
            if (me.GetVictim())
                return;

            if (me.HasReactState(ReactStates.Aggressive) && !me.HasUnitState(UnitState.Stunned) && who.isTargetableForAttack() && who.isInAccessiblePlaceFor(me))
                if (HasEscortState(eEscortState.Escorting) && AssistPlayerInCombatAgainst(who))
                    return;

            if (me.CanStartAttack(who, false))
                AttackStart(who);
        }

        public override void JustDied(Unit killer)
        {
            if (!HasEscortState(eEscortState.Escorting) || m_uiPlayerGUID.IsEmpty() || m_pQuestForEscort == null)
                return;
            
            Player player = GetPlayerForEscort();
            if (player)
            {
                Group group = player.GetGroup();
                if (group)
                {
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
                    {
                        Player member = groupRef.GetSource();
                        if (member)
                            member.FailQuest(m_pQuestForEscort.Id);
                    }
                }
                else
                    player.FailQuest(m_pQuestForEscort.Id);
            }
        }

        public override void JustRespawned()
        {
            RemoveEscortState(eEscortState.Escorting | eEscortState.Returning | eEscortState.Paused);

            if (!IsCombatMovementAllowed())
                SetCombatMovement(true);

            //add a small delay before going to first waypoint, normal in near all cases
            m_uiWPWaitTimer = 1000;

            if (me.getFaction() != me.GetCreatureTemplate().Faction)
                me.RestoreFaction();

            Reset();
        }

        void ReturnToLastPoint()
        {
            me.SetWalk(false);
            me.GetMotionMaster().MovePoint(0xFFFFFF, me.GetHomePosition());
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            me.RemoveAllAuras();
            me.DeleteThreatList();
            me.CombatStop(true);
            me.SetLootRecipient(null);

            if (HasEscortState(eEscortState.Escorting))
            {
                AddEscortState(eEscortState.Returning);
                ReturnToLastPoint();
                Log.outDebug(LogFilter.Scripts, "EscortAI has left combat and is now returning to last point");
            }
            else
            {
                me.GetMotionMaster().MoveTargetedHome();
                if (HasImmuneToNPCFlags)
                    me.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc);
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
                    for (GroupReference groupRef = group.GetFirstMember(); groupRef != null; groupRef = groupRef.next())
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
            if (HasEscortState(eEscortState.Escorting) && !me.GetVictim() && m_uiWPWaitTimer != 0 && !HasEscortState(eEscortState.Returning))
            {
                if (m_uiWPWaitTimer <= diff)
                {
                    if (!HasEscortState(eEscortState.Paused))
                    {
                        m_uiWPWaitTimer = 0;

                        if (m_bEnded)
                        {
                            me.StopMoving();
                            me.GetMotionMaster().Clear(false);
                            me.GetMotionMaster().MoveIdle();

                            m_bEnded = false;

                            if (DespawnAtEnd)
                            {
                                Log.outDebug(LogFilter.Scripts, "EscortAI reached end of waypoints");

                                if (m_bCanReturnToStart)
                                {
                                    float fRetX, fRetY, fRetZ;
                                    me.GetRespawnPosition(out fRetX, out fRetY, out fRetZ);

                                    me.GetMotionMaster().MovePoint(EscortPointIds.Home, fRetX, fRetY, fRetZ);

                                    Log.outDebug(LogFilter.Scripts, $"EscortAI are returning home to spawn location: {EscortPointIds.Home}, {fRetX}, {fRetY}, {fRetZ}");
                                }
                                else if (m_bCanInstantRespawn)
                                {
                                    me.setDeathState(DeathState.JustDied);
                                    me.Respawn();
                                }
                                else
                                    me.DespawnOrUnsummon();
                            }
                            else
                                Log.outDebug(LogFilter.Scripts, "EscortAI reached end of waypoints with Despawn off");

                            RemoveEscortState(eEscortState.Escorting);
                            return;
                        }

                        if (!m_bStarted)
                        {
                            m_bStarted = true;
                            me.GetMotionMaster().MovePath(_path, false);
                        }
                        else
                        {
                            WaypointMovementGenerator move = (WaypointMovementGenerator)me.GetMotionMaster().top();
                            if (move != null)
                                WaypointStart(move.GetCurrentNode());
                        }
                    }
                }
                else
                    m_uiWPWaitTimer -= diff;
            }

            //Check if player or any member of his group is within range
            if (HasEscortState(eEscortState.Escorting) && !m_uiPlayerGUID.IsEmpty() && !me.GetVictim() && !HasEscortState(eEscortState.Returning))
            {
                m_uiPlayerCheckTimer += diff;
                if (m_uiPlayerCheckTimer > 1000)
                {
                    if (DespawnAtFar && !IsPlayerOrGroupInRange())
                    {
                        if (m_bCanInstantRespawn)
                        {
                            me.setDeathState(DeathState.JustDied);
                            me.Respawn();
                        }
                        else
                            me.DespawnOrUnsummon();

                        return;
                    }

                    m_uiPlayerCheckTimer = 0;
                }
            }

            UpdateEscortAI(diff);
        }

        void UpdateEscortAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override void MovementInform(MovementGeneratorType moveType, uint pointId)
        {
            // no action allowed if there is no escort
            if (!HasEscortState(eEscortState.Escorting))
                return;

            if (moveType == MovementGeneratorType.Point)
            {
                if (m_uiWPWaitTimer == 0)
                    m_uiWPWaitTimer = 1;

                //Combat start position reached, continue waypoint movement
                if (pointId == EscortPointIds.LastPoint)
                {
                    Log.outDebug(LogFilter.Scripts, "EscortAI has returned to original position before combat");

                    me.SetWalk(!m_bIsRunning);
                    RemoveEscortState(eEscortState.Returning);
                }
                else if (pointId == EscortPointIds.Home)
                {
                    Log.outDebug(LogFilter.Scripts, "EscortAI has returned to original home location and will continue from beginning of waypoint list.");

                    m_bStarted = false;
                }
            }
            else if (moveType == MovementGeneratorType.Waypoint)
            {
                //Call WP function
                WaypointReached(pointId);

                //End of the line
                if (LastWP != 0 && LastWP == pointId)
                {
                    LastWP = 0;

                    m_bStarted = false;
                    m_bEnded = true;

                    m_uiWPWaitTimer = 50;

                    return;
                }

                Log.outDebug(LogFilter.Scripts, $"EscortAI Waypoint {pointId} reached");

                WaypointMovementGenerator move = (WaypointMovementGenerator)me.GetMotionMaster().top();
                if (move != null)
                    m_uiWPWaitTimer = (uint)move.GetTrackerTimer().GetExpiry();

                //Call WP start function
                if (m_uiWPWaitTimer == 0 && !HasEscortState(eEscortState.Paused) && move != null)
                    WaypointStart(move.GetCurrentNode());

                if (m_bIsRunning)
                    me.SetWalk(false);
                else
                    me.SetWalk(true);
            }
        }

        public void AddWaypoint(uint id, float x, float y, float z, uint waitTime = 0)
        {
            GridDefines.NormalizeMapCoord(ref x);
            GridDefines.NormalizeMapCoord(ref y);

            WaypointNode wp = new WaypointNode();
            wp.id = id;
            wp.x = x;
            wp.y = y;
            wp.z = z;
            wp.orientation = 0.0f;
            wp.moveType = m_bIsRunning ? WaypointMoveType.Run : WaypointMoveType.Walk;
            wp.delay = waitTime;
            wp.eventId = 0;
            wp.eventChance = 100;

            _path.nodes.Add(wp);

            LastWP = id;

            ScriptWP = true;
        }

        void FillPointMovementListForCreature()
        {
            var movePoints = Global.ScriptMgr.GetPointMoveList(me.GetEntry());
            if (movePoints.Empty())
                return;

            LastWP = movePoints.Last().uiPointId;

            foreach (var point in movePoints)
            {
                float x = point.fX;
                float y = point.fY;
                float z = point.fZ;

                GridDefines.NormalizeMapCoord(ref x);
                GridDefines.NormalizeMapCoord(ref y);

                WaypointNode wp = new WaypointNode();
                wp.id = point.uiPointId;
                wp.x = x;
                wp.y = y;
                wp.z = z;
                wp.orientation = 0.0f;
                wp.moveType = m_bIsRunning ? WaypointMoveType.Run : WaypointMoveType.Walk;
                wp.delay = point.uiWaitTime;
                wp.eventId = 0;
                wp.eventChance = 100;

                _path.nodes.Add(wp);
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
        public void Start(bool isActiveAttacker = true, bool run = false, ObjectGuid playerGUID = default(ObjectGuid), Quest quest = null, bool instantRespawn = false, bool canLoopPath = false, bool resetWaypoints = true)
        {
            if (me.GetVictim())
            {
                Log.outError(LogFilter.Server, "TSCR ERROR: EscortAI (script: {0}, creature entry: {1}) attempts to Start while in combat", me.GetScriptName(), me.GetEntry());
                return;
            }

            if (HasEscortState(eEscortState.Escorting))
            {
                Log.outError(LogFilter.Scripts, "EscortAI (script: {0}, creature entry: {1}) attempts to Start while already escorting", me.GetScriptName(), me.GetEntry());
                return;
            }

            //set variables
            m_bIsActiveAttacker = isActiveAttacker;
            m_bIsRunning = run;

            m_uiPlayerGUID = playerGUID;
            m_pQuestForEscort = quest;

            m_bCanInstantRespawn = instantRespawn;
            m_bCanReturnToStart = canLoopPath;

            if (!ScriptWP && resetWaypoints) // sd2 never adds wp in script, but tc does
                FillPointMovementListForCreature();

            if (m_bCanReturnToStart && m_bCanInstantRespawn)
                Log.outDebug(LogFilter.Scripts, "EscortAI is set to return home after waypoint end and instant respawn at waypoint end. Creature will never despawn.");

            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Waypoint)
            {
                me.StopMoving();
                me.GetMotionMaster().Clear(false);
                me.GetMotionMaster().MoveIdle();
                Log.outDebug(LogFilter.Scripts, "EscortAI start with WAYPOINT_MOTION_TYPE, changed to MoveIdle.");
            }

            //disable npcflags
            me.SetUInt64Value(UnitFields.NpcFlags, (ulong)NPCFlags.None);
            if (me.HasFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc))
            {
                HasImmuneToNPCFlags = true;
                me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToNpc);
            }

            Log.outDebug(LogFilter.Scripts, $"EscortAI started. ActiveAttacker = {m_bIsActiveAttacker}, Run = {m_bIsRunning}, PlayerGUID = {m_uiPlayerGUID.ToString()}");

            //Set initial speed
            if (m_bIsRunning)
                me.SetWalk(false);
            else
                me.SetWalk(true);

            m_bStarted = false;

            AddEscortState(eEscortState.Escorting);
        }

        public void SetEscortPaused(bool on)
        {
            if (!HasEscortState(eEscortState.Escorting))
                return;

            if (on)
            {
                AddEscortState(eEscortState.Paused);
                me.StopMoving();
            }
            else
            {
                RemoveEscortState(eEscortState.Paused);
                WaypointMovementGenerator move = (WaypointMovementGenerator)me.GetMotionMaster().top();
                if (move != null)
                    move.GetTrackerTimer().Reset(1);
            }
        }

        public virtual void WaypointReached(uint pointId) { }
        public virtual void WaypointStart(uint pointId) { }

        public bool HasEscortState(eEscortState escortState) { return m_uiEscortState.HasAnyFlag(escortState); }
        public override bool IsEscorted() { return m_uiEscortState.HasAnyFlag(eEscortState.Escorting); }

        public void SetMaxPlayerDistance(float newMax) { MaxPlayerDistance = newMax; }
        public float GetMaxPlayerDistance() { return MaxPlayerDistance; }

        public void SetDespawnAtEnd(bool despawn) { DespawnAtEnd = despawn; }
        public void SetDespawnAtFar(bool despawn) { DespawnAtFar = despawn; }
        public bool GetAttack() { return m_bIsActiveAttacker; }//used in EnterEvadeMode override
        public void SetCanAttack(bool attack) { m_bIsActiveAttacker = attack; }
        public ObjectGuid GetEventStarterGUID() { return m_uiPlayerGUID; }
        public void SetWaitTimer(uint Timer) { m_uiWPWaitTimer = Timer; }

        public Player GetPlayerForEscort() { return Global.ObjAccessor.GetPlayer(me, m_uiPlayerGUID); }

        void AddEscortState(eEscortState escortState) { m_uiEscortState |= escortState; }
        void RemoveEscortState(eEscortState escortState) { m_uiEscortState &= ~escortState; }

        ObjectGuid m_uiPlayerGUID;
        uint m_uiWPWaitTimer;
        uint m_uiPlayerCheckTimer;
        eEscortState m_uiEscortState;
        float MaxPlayerDistance;
        uint LastWP;

        WaypointPath _path = new WaypointPath();

        Quest m_pQuestForEscort;                     //generally passed in Start() when regular escort script.

        bool m_bIsActiveAttacker;                           //obsolete, determined by faction.
        bool m_bIsRunning;                                  //all creatures are walking by default (has flag MOVEMENTFLAG_WALK)
        bool m_bCanInstantRespawn;                          //if creature should respawn instantly after escort over (if not, database respawntime are used)
        bool m_bCanReturnToStart;                           //if creature can walk same path (loop) without despawn. Not for regular escort quests.
        bool DespawnAtEnd;
        bool DespawnAtFar;
        bool ScriptWP;
        bool HasImmuneToNPCFlags;
        bool m_bStarted;
        bool m_bEnded;
    }

    public enum eEscortState
    {
        None = 0x000,                        //nothing in progress
        Escorting = 0x001,                        //escort are in progress
        Returning = 0x002,                        //escort is returning after being in combat
        Paused = 0x004                         //will not proceed with waypoints before state is removed
    }

    struct EscortPointIds
    {
        public const uint LastPoint = 0xFFFFFF;
        public const uint Home = 0xFFFFFE;
    }
}
