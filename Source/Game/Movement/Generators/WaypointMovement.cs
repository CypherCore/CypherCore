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
using Framework.GameMath;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Movement
{
    public class WaypointMovementGenerator : MovementGeneratorMedium<Creature>
    {
        public WaypointMovementGenerator(uint pathId = 0, bool repeating = true)
        {
            _nextMoveTime = new TimeTrackerSmall(0);
            _pathId = pathId;
            _repeating = repeating;
            _loadedFromDB = true;
        }

        public WaypointMovementGenerator(WaypointPath path, bool repeating = true)
        {
            _nextMoveTime = new TimeTrackerSmall(0);
            _repeating = repeating;            
            _path = path;
        }

        void LoadPath(Creature creature)
        {
            if (_loadedFromDB)
            {
                if (_pathId == 0)
                    _pathId = creature.GetWaypointPath();

                _path = Global.WaypointMgr.GetPath(_pathId);
            }

            if (_path == null)
            {
                // No path id found for entry
                Log.outError(LogFilter.Sql, $"WaypointMovementGenerator.LoadPath: creature {creature.GetName()} ({creature.GetGUID()} DB GUID: {creature.GetSpawnId()}) doesn't have waypoint path id: {_pathId}");
                return;
            }

            _nextMoveTime.Reset(1000);
        }

        public override void DoInitialize(Creature creature)
        {
            _done = false;
            LoadPath(creature);
        }

        public override void DoFinalize(Creature creature)
        {
            creature.ClearUnitState(UnitState.Roaming | UnitState.RoamingMove);
            creature.SetWalk(false);
        }

        public override void DoReset(Creature creature)
        {
            if (!_done && CanMove(creature))
                StartMoveNow(creature);
            else if (_done)
            {
                // mimic IdleMovementGenerator
                if (!creature.IsStopped())
                    creature.StopMoving();
            }
        }

        void OnArrived(Creature creature)
        {
            if (_path == null || _path.nodes.Empty())
                return;

            WaypointNode waypoint = _path.nodes.ElementAt((int)_currentNode);
            if (waypoint.delay != 0)
            {
                creature.ClearUnitState(UnitState.RoamingMove);
                _nextMoveTime.Reset((int)waypoint.delay);
            }

            if (waypoint.eventId != 0 && RandomHelper.URand(0, 99) < waypoint.eventChance)
            {
                Log.outDebug(LogFilter.MapsScript, $"Creature movement start script {waypoint.eventId} at point {_currentNode} for {creature.GetGUID()}.");
                creature.ClearUnitState(UnitState.RoamingMove);
                creature.GetMap().ScriptsStart(ScriptsType.Waypoint, waypoint.eventId, creature, null);
            }

            // inform AI
            if (creature.IsAIEnabled)
            {
                creature.GetAI().MovementInform(MovementGeneratorType.Waypoint, (uint)_currentNode);

                Cypher.Assert(_currentNode < _path.nodes.Count, $"WaypointMovementGenerator.OnArrived: tried to reference a node id ({_currentNode}) which is not included in path ({_path.id})");
                creature.GetAI().WaypointReached(_path.nodes[_currentNode].id, _path.id);
            }

            creature.UpdateWaypointID((uint)_currentNode);
        }

        bool StartMove(Creature creature)
        {
            if (!creature || !creature.IsAlive())
                return true;

            if (_done || _path == null || _path.nodes.Empty())
                return true;

            // if the owner is the leader of its formation, check members status
            if (creature.IsFormationLeader() && !creature.IsFormationLeaderMoveAllowed())
            {
                _nextMoveTime.Reset(1000);
                return true;
            }

            bool transportPath = creature.GetTransport() != null;

            if (_isArrivalDone)
            {
                if ((_currentNode == _path.nodes.Count - 1) && !_repeating) // If that's our last waypoint
                {
                    WaypointNode lastWaypoint = _path.nodes.ElementAt(_currentNode);

                    float x = lastWaypoint.x;
                    float y = lastWaypoint.y;
                    float z = lastWaypoint.z;
                    float o = creature.GetOrientation();

                    if (!transportPath)
                        creature.SetHomePosition(x, y, z, o);
                    else
                    {
                        Transport trans = creature.GetTransport();
                        if (trans)
                        {
                            o -= trans.GetOrientation();
                            creature.SetTransportHomePosition(x, y, z, o);
                            trans.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                            creature.SetHomePosition(x, y, z, o);
                        }
                        else
                            transportPath = false;
                        // else if (vehicle) - this should never happen, vehicle offsets are const
                    }
                    _done = true;
                    return true;
                }

                _currentNode = (_currentNode + 1) % _path.nodes.Count;

                // inform AI
                if (creature.IsAIEnabled)
                {
                    Cypher.Assert(_currentNode < _path.nodes.Count, $"WaypointMovementGenerator.StartMove: tried to reference a node id ({_currentNode}) which is not included in path ({_path.id})");
                    creature.GetAI().WaypointStarted(_path.nodes[(int)_currentNode].id, _path.id);
                }
            }

            WaypointNode waypoint = _path.nodes.ElementAt(_currentNode);
            Position formationDest = new Position(waypoint.x, waypoint.y, waypoint.z, (waypoint.orientation != 0 && waypoint.delay != 0) ? waypoint.orientation : 0.0f);

            _isArrivalDone = false;
            _recalculateSpeed = false;

            creature.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new MoveSplineInit(creature);

            //! If creature is on transport, we assume waypoints set in DB are already transport offsets
            if (transportPath)
            {
                init.DisableTransportPathTransformations();
                ITransport trans = creature.GetDirectTransport();
                if (trans != null)
                {
                    float orientation = formationDest.GetOrientation();
                    trans.CalculatePassengerPosition(ref formationDest.posX, ref formationDest.posY, ref formationDest.posZ, ref orientation);
                    formationDest.SetOrientation(orientation);
                }
            }

            //! Do not use formationDest here, MoveTo requires transport offsets due to DisableTransportPathTransformations() call
            //! but formationDest contains global coordinates
            init.MoveTo(waypoint.x, waypoint.y, waypoint.z);

            //! Accepts angles such as 0.00001 and -0.00001, 0 must be ignored, default value in waypoint table
            if (waypoint.orientation != 0 && waypoint.delay != 0)
                init.SetFacing(waypoint.orientation);

            switch (waypoint.moveType)
            {
                case WaypointMoveType.Land:
                    init.SetAnimation(AnimType.ToGround);
                    break;
                case WaypointMoveType.Takeoff:
                    init.SetAnimation(AnimType.ToFly);
                    break;
                case WaypointMoveType.Run:
                    init.SetWalk(false);
                    break;
                case WaypointMoveType.Walk:
                    init.SetWalk(true);
                    break;
            }

            init.Launch();

            // inform formation
            creature.SignalFormationMovement(formationDest, waypoint.id, waypoint.moveType, (waypoint.orientation != 0 && waypoint.delay != 0) ? true : false);
            return true;
        }

        public override bool DoUpdate(Creature creature, uint diff)
        {
            if (!creature || !creature.IsAlive())
                return true;

            if (_done || _path == null || _path.nodes.Empty())
                return true;

            if (_stalled || creature.HasUnitState(UnitState.NotMove) || creature.IsMovementPreventedByCasting())
            {
                creature.StopMoving();
                return true;
            }

            if (!_nextMoveTime.Passed())
            {
                _nextMoveTime.Update((int)diff);
                if (_nextMoveTime.Passed())
                    return StartMoveNow(creature);
            }
            else
            {
                // Set home position at place on waypoint movement.
                if (creature.GetTransGUID().IsEmpty())
                    creature.SetHomePosition(creature.GetPosition());

                if (creature.MoveSpline.Finalized())
                {
                    OnArrived(creature);
                    _isArrivalDone = true;

                    if (_nextMoveTime.Passed())
                        return StartMove(creature);
                }
                else if (_recalculateSpeed)
                {
                    if (_nextMoveTime.Passed())
                        StartMove(creature);
                }
            }

            return true;
        }

        void MovementInform(Creature creature)
        {
            if (creature.IsAIEnabled)
                creature.GetAI().MovementInform(MovementGeneratorType.Waypoint, (uint)_currentNode);
        }

        public override bool GetResetPosition(Unit u, out float x, out float y, out float z)
        {
            x = y = z = 0;
            // prevent a crash at empty waypoint path.
            // prevent a crash at empty waypoint path.
            if (_path == null || _path.nodes.Empty())
                return false;

            WaypointNode waypoint = _path.nodes.ElementAt(_currentNode);

            x = waypoint.x;
            y = waypoint.y;
            z = waypoint.z;
            return true;
        }

        public override void Pause(uint timer = 0)
        {
            _stalled = timer != 0 ? false : true;
            _nextMoveTime.Reset(timer != 0 ? (int)timer : 1);
        }

        public override void Resume(uint overrideTimer = 0)
        {
            _stalled = false;
            if (overrideTimer != 0)
                _nextMoveTime.Reset((int)overrideTimer);
        }

        bool CanMove(Creature creature)
        {
            return _nextMoveTime.Passed() && !creature.HasUnitState(UnitState.NotMove) && !creature.IsMovementPreventedByCasting();
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Waypoint; }

        public override void UnitSpeedChanged() { _recalculateSpeed = true; }

        bool StartMoveNow(Creature creature)
        {
            _nextMoveTime.Reset(0);
            return StartMove(creature);
        }

        TimeTrackerSmall _nextMoveTime;
        bool _recalculateSpeed;
        bool _isArrivalDone;
        uint _pathId;
        bool _repeating;
        bool _loadedFromDB;
        bool _stalled;
        bool _done;

        WaypointPath _path;
        int _currentNode;
    }

    public class FlightPathMovementGenerator : MovementGeneratorMedium<Player>
    {
        uint GetPathAtMapEnd()
        {
            if (_currentNode >= _path.Count)
                return (uint)_path.Count;

            uint curMapId = _path[_currentNode].ContinentID;
            for (int i = _currentNode; i < _path.Count; ++i)
            {
                if (_path[i].ContinentID != curMapId)
                    return (uint)i;
            }

            return (uint)_path.Count;
        }

        bool IsNodeIncludedInShortenedPath(TaxiPathNodeRecord p1, TaxiPathNodeRecord p2)
        {
            return p1.ContinentID != p2.ContinentID || Math.Pow(p1.Loc.X - p2.Loc.X, 2) + Math.Pow(p1.Loc.Y - p2.Loc.Y, 2) > (40.0f * 40.0f);
        }
        
        public void LoadPath(Player player, uint startNode = 0)
        {
            _path.Clear();
            _currentNode = (int)startNode;
            _pointsForPathSwitch.Clear();
            var taxi = player.m_taxi.GetPath();
            float discount = player.GetReputationPriceDiscount(player.m_taxi.GetFlightMasterFactionTemplate());

            for (int src = 0, dst = 1; dst < taxi.Count; src = dst++)
            {
                uint path, cost;
                Global.ObjectMgr.GetTaxiPath(taxi[src], taxi[dst], out path, out cost);
                if (path > CliDB.TaxiPathNodesByPath.Keys.Max())
                    return;

                var nodes = CliDB.TaxiPathNodesByPath[path];
                if (!nodes.Empty())
                {
                    TaxiPathNodeRecord start = nodes[0];
                    TaxiPathNodeRecord end = nodes[nodes.Length - 1];
                    bool passedPreviousSegmentProximityCheck = false;
                    for (uint i = 0; i < nodes.Length; ++i)
                    {
                        if (passedPreviousSegmentProximityCheck || src == 0 || _path.Empty() || IsNodeIncludedInShortenedPath(_path.Last(), nodes[i]))
                        {
                            if ((src == 0 || (IsNodeIncludedInShortenedPath(start, nodes[i]) && i >= 2)) &&
                                (dst == taxi.Count - 1 || (IsNodeIncludedInShortenedPath(end, nodes[i]) && i < nodes.Length - 1)))
                            {
                                passedPreviousSegmentProximityCheck = true;
                                _path.Add(nodes[i]);
                            }
                        }
                        else
                        {
                            _path.RemoveAt(_path.Count - 1);
                            _pointsForPathSwitch[_pointsForPathSwitch.Count - 1].PathIndex -= 1;
                        }
                    }
                }

                _pointsForPathSwitch.Add(new TaxiNodeChangeInfo((uint)(_path.Count - 1), (long)Math.Ceiling(cost * discount)));
            }
        }

        public override void DoInitialize(Player owner)
        {
            Reset(owner);
            InitEndGridInfo();
        }

        public override void DoFinalize(Player owner)
        {
            // remove flag to prevent send object build movement packets for flight state and crash (movement generator already not at top of stack)
            owner.ClearUnitState(UnitState.InFlight);

            owner.Dismount();
            owner.RemoveUnitFlag(UnitFlags.RemoveClientControl | UnitFlags.TaxiFlight);

            if (owner.m_taxi.Empty())
            {
                owner.GetHostileRefManager().SetOnlineOfflineState(true);
                // update z position to ground and orientation for landing point
                // this prevent cheating with landing  point at lags
                // when client side flight end early in comparison server side
                owner.StopMoving();
                owner.SetFallInformation(0, owner.GetPositionZ());
            }

            owner.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
            owner.RestoreDisplayId();
        }

        public override void DoReset(Player owner)
        {
            owner.GetHostileRefManager().SetOnlineOfflineState(false);
            owner.AddUnitState(UnitState.InFlight);
            owner.AddUnitFlag(UnitFlags.RemoveClientControl | UnitFlags.TaxiFlight);

            MoveSplineInit init = new MoveSplineInit(owner);
            uint end = GetPathAtMapEnd();
            init.args.path = new Vector3[end];
            for (int i = (int)GetCurrentNode(); i != end; ++i)
            {
                Vector3 vertice = new Vector3(_path[i].Loc.X, _path[i].Loc.Y, _path[i].Loc.Z);
                init.args.path[i] = vertice;
            }
            init.SetFirstPointId((int)GetCurrentNode());
            init.SetFly();
            init.SetSmooth();
            init.SetUncompressed();
            init.SetWalk(true);
            init.SetVelocity(30.0f);
            init.Launch();
        }

        public override bool DoUpdate(Player player, uint time_diff)
        {
            uint pointId = (uint)player.MoveSpline.CurrentPathIdx();
            if (pointId > _currentNode)
            {
                bool departureEvent = true;
                do
                {
                    DoEventIfAny(player, _path[_currentNode], departureEvent);
                    while (!_pointsForPathSwitch.Empty() && _pointsForPathSwitch[0].PathIndex <= _currentNode)
                    {
                        _pointsForPathSwitch.RemoveAt(0);
                        player.m_taxi.NextTaxiDestination();
                        if (!_pointsForPathSwitch.Empty())
                        {
                            player.UpdateCriteria(CriteriaTypes.GoldSpentForTravelling, (uint)_pointsForPathSwitch[0].Cost);
                            player.ModifyMoney(-_pointsForPathSwitch[0].Cost);
                        }
                    }

                    if (pointId == _currentNode)
                        break;

                    if (_currentNode == _preloadTargetNode)
                        PreloadEndGrid();
                    _currentNode += (departureEvent ? 1 : 0);
                    departureEvent = !departureEvent;
                }
                while (true);
            }

            return _currentNode < (_path.Count - 1);
        }

        public void SetCurrentNodeAfterTeleport()
        {
            if (_path.Empty() || _currentNode >= _path.Count)
                return;

            uint map0 = _path[_currentNode].ContinentID;
            for (int i = _currentNode + 1; i < _path.Count; ++i)
            {
                if (_path[i].ContinentID != map0)
                {
                    _currentNode = i;
                    return;
                }
            }

        }

        void DoEventIfAny(Player player, TaxiPathNodeRecord node, bool departure)
        {
            uint eventid = departure ? node.DepartureEventID : node.ArrivalEventID;
            if (eventid != 0)
            {
                Log.outDebug(LogFilter.Scripts, "Taxi {0} event {1} of node {2} of path {3} for player {4}", departure ? "departure" : "arrival", eventid, node.NodeIndex, node.PathID, player.GetName());
                player.GetMap().ScriptsStart(ScriptsType.Event, eventid, player, player);
            }
        }

        bool GetResetPos(Player player, out float x, out float y, out float z)
        {
            TaxiPathNodeRecord node = _path[_currentNode];
            x = node.Loc.X;
            y = node.Loc.Y;
            z = node.Loc.Z;
            return true;
        }

        void InitEndGridInfo()
        {
            int nodeCount = _path.Count;        //! Number of nodes in path.
            _endMapId = _path[nodeCount - 1].ContinentID; //! MapId of last node
            _preloadTargetNode = (uint)nodeCount - 3;
            _endGridX = _path[nodeCount - 1].Loc.X;
            _endGridY = _path[nodeCount - 1].Loc.Y;
        }

        void PreloadEndGrid()
        {
            // used to preload the final grid where the flightmaster is
            Map endMap = Global.MapMgr.FindBaseNonInstanceMap(_endMapId);

            // Load the grid
            if (endMap != null)
            {
                Log.outInfo(LogFilter.Server, "Preloading grid ({0}, {1}) for map {2} at node index {3}/{4}", _endGridX, _endGridY, _endMapId, _preloadTargetNode, _path.Count - 1);
                endMap.LoadGrid(_endGridX, _endGridY);
            }
            else
                Log.outInfo(LogFilter.Server, "Unable to determine map to preload flightmaster grid");
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Flight; }

        public List<TaxiPathNodeRecord> GetPath() { return _path; }

        bool HasArrived() { return (_currentNode >= _path.Count); }

        public void SkipCurrentNode() { ++_currentNode; }

        public uint GetCurrentNode() { return (uint)_currentNode; }

        float _endGridX;                //! X coord of last node location
        float _endGridY;                //! Y coord of last node location
        uint _endMapId;               //! map Id of last node location
        uint _preloadTargetNode;      //! node index where preloading starts

        List<TaxiPathNodeRecord> _path = new List<TaxiPathNodeRecord>();
        int _currentNode;
        List<TaxiNodeChangeInfo> _pointsForPathSwitch = new List<TaxiNodeChangeInfo>();    //! node indexes and costs where TaxiPath changes

        class TaxiNodeChangeInfo
        {
            public TaxiNodeChangeInfo(uint pathIndex, long cost)
            {
                PathIndex = pathIndex;
                Cost = cost;
            }

            public uint PathIndex;
            public long Cost;
        }
    }
}
