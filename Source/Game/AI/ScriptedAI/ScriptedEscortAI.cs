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
using Game.Movement;
using System;
using System.Collections.Generic;

namespace Game.AI
{
    public class EscortAI : ScriptedAI
    {
        public EscortAI(Creature creature) : base(creature)
        {
            _pauseTimer = 2500;
            _playerCheckTimer = 1000;
            _maxPlayerDistance = 50;
            _activeAttacker = true;
            _despawnAtEnd = true;
            _despawnAtFar = true;

            _path = new WaypointPath();
        }

        public Player GetPlayerForEscort()
        {
            return Global.ObjAccessor.GetPlayer(me, _playerGUID);
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

            if (me.HasReactState(ReactStates.Passive))
                return false;

            //experimental (unknown) flag not present
            if (!me.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CanAssist))
                return false;

            //not a player
            if (!who.GetVictim().GetCharmerOrOwnerPlayerOrPlayerItself())
                return false;

            //never attack friendly
            if (me.IsValidAssistTarget(who.GetVictim()))
                return false;

            //too far away and no free sight?
            if (me.IsWithinDistInMap(who, GetMaxPlayerDistance()) && me.IsWithinLOSInMap(who))
            {
                me.EngageWithTarget(who);
                return true;
            }

            return false;
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (who == null)
                return;

            if (HasEscortState(EscortState.Escorting) && AssistPlayerInCombatAgainst(who))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            if (!HasEscortState(EscortState.Escorting) || _playerGUID.IsEmpty() || _escortQuest == null)
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
                                member.FailQuest(_escortQuest.Id);
                    }
                }
                else
                    player.FailQuest(_escortQuest.Id);
            }
        }

        public override void JustAppeared()
        {
            _escortState = EscortState.None;

            if (!IsCombatMovementAllowed())
                SetCombatMovement(true);

            //add a small delay before going to first waypoint, normal in near all cases
            _pauseTimer = 2000;

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
            me.GetThreatManager().ClearAllThreat();
            me.CombatStop(true);
            me.SetLootRecipient(null);

            if (HasEscortState(EscortState.Escorting))
            {
                AddEscortState(EscortState.Returning);
                ReturnToLastPoint();
                Log.outDebug(LogFilter.Scripts, "EscortAI.EnterEvadeMode has left combat and is now returning to last point");
            }
            else
            {
                me.GetMotionMaster().MoveTargetedHome();
                if (_hasImmuneToNPCFlags)
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
            if (HasEscortState(EscortState.Escorting) && !me.IsEngaged() && !HasEscortState(EscortState.Returning))
            {
                if (_pauseTimer <= diff)
                {
                    if (!HasEscortState(EscortState.Paused))
                    {
                        _pauseTimer = 0;

                        if (_ended)
                        {
                            _ended = false;
                            me.GetMotionMaster().MoveIdle();

                            if (_despawnAtEnd)
                            {
                                Log.outDebug(LogFilter.Scripts, "EscortAI.UpdateAI: reached end of waypoints, despawning at end");
                                if (_returnToStart)
                                {
                                    Position respawnPosition = new Position();
                                    float orientation;
                                    me.GetRespawnPosition(out respawnPosition.posX, out respawnPosition.posY, out respawnPosition.posZ, out orientation);
                                    respawnPosition.SetOrientation(orientation);
                                    me.GetMotionMaster().MovePoint(EscortPointIds.Home, respawnPosition);
                                    Log.outDebug(LogFilter.Scripts, $"EscortAI.UpdateAI: returning to spawn location: {respawnPosition}");
                                }
                                else if (_instantRespawn)
                                    me.Respawn();
                                else
                                    me.DespawnOrUnsummon();
                            }

                            Log.outDebug(LogFilter.Scripts, "EscortAI.UpdateAI: reached end of waypoints");
                            RemoveEscortState(EscortState.Escorting);
                            return;
                        }

                        if (!_started)
                        {
                            _started = true;
                            me.GetMotionMaster().MovePath(_path, false);
                        }
                        else if (_resume)
                        {
                            _resume = false;
                            IMovementGenerator movementGenerator = me.GetMotionMaster().GetMotionSlot(MovementSlot.Idle);
                            if (movementGenerator != null)
                                movementGenerator.Resume(0);
                        }
                    }
                }
                else
                    _pauseTimer -= diff;
            }


            //Check if player or any member of his group is within range
            if (_despawnAtFar && HasEscortState(EscortState.Escorting) && !_playerGUID.IsEmpty() && !me.GetVictim() && !HasEscortState(EscortState.Returning))
            {
                if (_playerCheckTimer <= diff)
                {
                    if (!IsPlayerOrGroupInRange())
                    {
                        Log.outDebug(LogFilter.Scripts, "EscortAI failed because player/group was to far away or not found");

                        bool isEscort = false;
                        CreatureData creatureData = me.GetCreatureData();
                        if (creatureData != null)
                            isEscort = (WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && creatureData.spawnGroupData.flags.HasAnyFlag(SpawnGroupFlags.EscortQuestNpc));

                        if (_instantRespawn && !isEscort)
                            me.DespawnOrUnsummon(0, TimeSpan.FromSeconds(1));
                        else if (_instantRespawn && isEscort)
                            me.GetMap().RemoveRespawnTime(SpawnObjectType.Creature, me.GetSpawnId(), true);
                        else
                            me.DespawnOrUnsummon();

                        return;
                    }

                    _playerCheckTimer = 1000;
                }
                else
                    _playerCheckTimer -= diff;
            }

            UpdateEscortAI(diff);
        }

        public virtual void UpdateEscortAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();
        }

        public override void MovementInform(MovementGeneratorType moveType, uint Id)
        {
            // no action allowed if there is no escort
            if (!HasEscortState(EscortState.Escorting))
                return;

            //Combat start position reached, continue waypoint movement
            if (moveType == MovementGeneratorType.Point)
            {
                if (_pauseTimer == 0)
                    _pauseTimer = 2000;

                if (Id == EscortPointIds.LastPoint)
                {
                    Log.outDebug(LogFilter.Scripts, "EscortAI.MovementInform has returned to original position before combat");

                    me.SetWalk(!_running);
                    RemoveEscortState(EscortState.Returning);

                }
                else if (Id == EscortPointIds.Home)
                {
                    Log.outDebug(LogFilter.Scripts, "EscortAI.MovementInform: returned to home location and restarting waypoint path");
                    _started = false;
                }
            }
            else if (moveType == MovementGeneratorType.Waypoint)
            {
                Cypher.Assert(Id < _path.nodes.Count, $"EscortAI.MovementInform: referenced movement id ({Id}) points to non-existing node in loaded path");
                WaypointNode waypoint = _path.nodes[(int)Id];

                Log.outDebug(LogFilter.Scripts, $"EscortAI.MovementInform: waypoint node {waypoint.id} reached");

                // last point
                if (Id == _path.nodes.Count - 1)
                {
                    _started = false;
                    _ended = true;
                    _pauseTimer = 1000;
                }
            }
        }

        public void AddWaypoint(uint id, float x, float y, float z, float orientation = 0, uint waitTime = 0)
        {
            GridDefines.NormalizeMapCoord(ref x);
            GridDefines.NormalizeMapCoord(ref y);

            WaypointNode waypoint = new WaypointNode();
            waypoint.id = id;
            waypoint.x = x;
            waypoint.y = y;
            waypoint.z = z;
            waypoint.orientation = orientation;
            waypoint.moveType = _running ? WaypointMoveType.Run : WaypointMoveType.Walk;
            waypoint.delay = waitTime;
            waypoint.eventId = 0;
            waypoint.eventChance = 100;
            _path.nodes.Add(waypoint);

            _manualPath = true;
        }

        void FillPointMovementListForCreature()
        {
            WaypointPath path = Global.WaypointMgr.GetPath(me.GetEntry());
            if (path == null)
                return;

            foreach (WaypointNode value in path.nodes)
            {
                WaypointNode node = value;
                GridDefines.NormalizeMapCoord(ref node.x);
                GridDefines.NormalizeMapCoord(ref node.y);
                node.moveType = _running ? WaypointMoveType.Run : WaypointMoveType.Walk;

                _path.nodes.Add(node);
            }
        }

        public void SetRun(bool on = true)
        {
            if (on && !_running)
                me.SetWalk(false);
            else if (!on && _running)
                me.SetWalk(true);

            _running = on;
        }

        /// todo get rid of this many variables passed in function.
        public void Start(bool isActiveAttacker = true, bool run = false, ObjectGuid playerGUID = default, Quest quest = null, bool instantRespawn = false, bool canLoopPath = false, bool resetWaypoints = true)
        {
            // Queue respawn from the point it starts
            Map map = me.GetMap();
            if (map != null)
            {
                CreatureData cdata = me.GetCreatureData();
                if (cdata != null)
                {
                    SpawnGroupTemplateData groupdata = cdata.spawnGroupData;
                    if (groupdata != null)
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.RespawnDynamicEscortNpc) && groupdata.flags.HasAnyFlag(SpawnGroupFlags.EscortQuestNpc) && map.GetCreatureRespawnTime(me.GetSpawnId()) == 0)
                        {
                            me.SetRespawnTime(me.GetRespawnDelay());
                            me.SaveRespawnTime();
                        }
                    }
                }
            }

            if (me.GetVictim())
            {
                Log.outError(LogFilter.Scripts, $"EscortAI.Start: (script: {me.GetScriptName()}, creature entry: {me.GetEntry()}) attempts to Start while in combat");
                return;
            }

            if (HasEscortState(EscortState.Escorting))
            {
                Log.outError(LogFilter.Scripts, $"EscortAI.Start: (script: {me.GetScriptName()}, creature entry: {me.GetEntry()}) attempts to Start while already escorting");
                return;
            }

            if (!_manualPath && resetWaypoints)
                FillPointMovementListForCreature();

            if (_path.nodes.Empty())
            {
                Log.outError(LogFilter.Scripts, $"EscortAI.Start: (script: {me.GetScriptName()}, creature entry: {me.GetEntry()}) starts with 0 waypoints (possible missing entry in script_waypoint. Quest: {(quest != null ? quest.Id : 0)}).");
                return;
            }

            // set variables
            _activeAttacker = isActiveAttacker;
            _running = run;
            _playerGUID = playerGUID;
            _escortQuest = quest;
            _instantRespawn = instantRespawn;
            _returnToStart = canLoopPath;

            if (_returnToStart && _instantRespawn)
                Log.outError(LogFilter.Scripts, $"EscortAI.Start: (script: {me.GetScriptName()}, creature entry: {me.GetEntry()}) is set to return home after waypoint end and instant respawn at waypoint end. Creature will never despawn.");

            me.GetMotionMaster().MoveIdle();
            me.GetMotionMaster().Clear(MovementSlot.Active);

            //disable npcflags
            me.SetNpcFlags(NPCFlags.None);
            me.SetNpcFlags2(NPCFlags2.None);
            if (me.HasUnitFlag(UnitFlags.ImmuneToNpc))
            {
                _hasImmuneToNPCFlags = true;
                me.RemoveUnitFlag(UnitFlags.ImmuneToNpc);
            }

            Log.outDebug(LogFilter.Scripts, $"EscortAI.Start: (script: {me.GetScriptName()}, creature entry: {me.GetEntry()}) started with {_path.nodes.Count} waypoints. ActiveAttacker = {_activeAttacker}, Run = {_running}, Player = {_playerGUID}");

            // set initial speed
            me.SetWalk(!_running);

            _started = false;
            AddEscortState(EscortState.Escorting);
        }

        public void SetEscortPaused(bool on)
        {
            if (!HasEscortState(EscortState.Escorting))
                return;

            if (on)
            {
                AddEscortState(EscortState.Paused);
                IMovementGenerator movementGenerator = me.GetMotionMaster().GetMotionSlot(MovementSlot.Idle);
                if (movementGenerator != null)
                    movementGenerator.Pause(0);
            }
            else
            {
                RemoveEscortState(EscortState.Paused);
                _resume = true;
            }
        }

        public override bool IsEscortNPC(bool onlyIfActive)
        {
            if (!onlyIfActive)
                return true;

            if (!GetEventStarterGUID().IsEmpty())
                return true;

            return false;
        }

        void SetPauseTimer(uint Timer) { _pauseTimer = Timer; }

        public bool HasEscortState(EscortState escortState) { return (_escortState & escortState) != 0; }
        public override bool IsEscorted() { return _escortState.HasAnyFlag(EscortState.Escorting); }

        void SetMaxPlayerDistance(float newMax) { _maxPlayerDistance = newMax; }
        float GetMaxPlayerDistance() { return _maxPlayerDistance; }

        public void SetDespawnAtEnd(bool despawn) { _despawnAtEnd = despawn; }
        public void SetDespawnAtFar(bool despawn) { _despawnAtFar = despawn; }

        bool GetAttack() { return _activeAttacker; } // used in EnterEvadeMode override
        void SetCanAttack(bool attack) { _activeAttacker = attack; }

        ObjectGuid GetEventStarterGUID() { return _playerGUID; }

        void AddEscortState(EscortState escortState) { _escortState |= escortState; }
        void RemoveEscortState(EscortState escortState) { _escortState &= ~escortState; }

        ObjectGuid _playerGUID;
        uint _pauseTimer;
        uint _playerCheckTimer;
        EscortState _escortState;
        float _maxPlayerDistance;

        Quest _escortQuest; //generally passed in Start() when regular escort script.

        WaypointPath _path;

        bool _activeAttacker;      // obsolete, determined by faction.
        bool _running;             // all creatures are walking by default (has flag MOVEMENTFLAG_WALK)
        bool _instantRespawn;      // if creature should respawn instantly after escort over (if not, database respawntime are used)
        bool _returnToStart;       // if creature can walk same path (loop) without despawn. Not for regular escort quests.
        bool _despawnAtEnd;
        bool _despawnAtFar;
        bool _manualPath;
        bool _hasImmuneToNPCFlags;
        bool _started;
        bool _ended;
        bool _resume;
    }

    public enum EscortState
    {
        None = 0x00,                        //nothing in progress
        Escorting = 0x01,                        //escort are in progress
        Returning = 0x02,                        //escort is returning after being in combat
        Paused = 0x04                         //will not proceed with waypoints before state is removed
    }

    struct EscortPointIds
    {
        public const uint LastPoint = 0xFFFFFF;
        public const uint Home = 0xFFFFFE;
    }
}
