// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Movement
{
    public class FlightPathMovementGenerator : MovementGeneratorMedium<Player>
    {
        public FlightPathMovementGenerator()
        {
            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Highest;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.InFlight;
        }

        public override void DoInitialize(Player owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            DoReset(owner);
            InitEndGridInfo();
        }

        public override void DoReset(Player owner)
        {
            RemoveFlag(MovementGeneratorFlags.Deactivated);

            owner.CombatStopWithPets();
            owner.SetUnitFlag(UnitFlags.RemoveClientControl | UnitFlags.OnTaxi);

            uint end = GetPathAtMapEnd();
            uint currentNodeId = GetCurrentNode();

            if (currentNodeId == end)
            {
                Log.outDebug(LogFilter.Movement, $"FlightPathMovementGenerator::DoReset: trying to start a flypath from the end point. {owner.GetDebugInfo()}");
                return;
            }

            MoveSplineInit init = new(owner);
            // Providing a starting vertex since the taxi paths do not provide such
            init.Path().Add(new Vector3(owner.GetPositionX(), owner.GetPositionY(), owner.GetPositionZ()));
            for (int i = (int)currentNodeId; i != (uint)end; ++i)
            {
                Vector3 vertice = new(_path[i].Loc.X, _path[i].Loc.Y, _path[i].Loc.Z);
                init.Path().Add(vertice);
            }
            
            init.SetFirstPointId((int)GetCurrentNode());
            init.SetFly();
            init.SetSmooth();
            init.SetUncompressed();
            init.SetWalk(true);
            init.SetVelocity(30.0f);
            init.Launch();
        }

        public override bool DoUpdate(Player owner, uint diff)
        {
            if (owner == null)
                return false;

            // skipping the first spline path point because it's our starting point and not a taxi path point
            uint pointId = (uint)(owner.MoveSpline.CurrentPathIdx() <= 0 ? 0 : owner.MoveSpline.CurrentPathIdx() - 1);
            if (pointId > _currentNode && _currentNode < _path.Count - 1)
            {
                bool departureEvent = true;
                do
                {
                    Cypher.Assert(_currentNode < _path.Count, $"Point Id: {pointId}\n{owner.GetDebugInfo()}");

                    DoEventIfAny(owner, _path[_currentNode], departureEvent);
                    while (!_pointsForPathSwitch.Empty() && _pointsForPathSwitch[0].PathIndex <= _currentNode)
                    {
                        _pointsForPathSwitch.RemoveAt(0);
                        owner.m_taxi.NextTaxiDestination();
                        if (!_pointsForPathSwitch.Empty())
                        {
                            owner.UpdateCriteria(CriteriaType.MoneySpentOnTaxis, (uint)_pointsForPathSwitch[0].Cost);
                            owner.ModifyMoney(-_pointsForPathSwitch[0].Cost);
                        }
                    }

                    if (pointId == _currentNode)
                        break;

                    if (_currentNode == _preloadTargetNode)
                        PreloadEndGrid(owner);

                    _currentNode += (departureEvent ? 1 : 0);
                    departureEvent = !departureEvent;
                }
                while (_currentNode < _path.Count - 1);
            }

            if (_currentNode >= (_path.Count - 1))
            {
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }
            return true;
        }

        public override void DoDeactivate(Player owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
        }

        public override void DoFinalize(Player owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (!active)
                return;

            uint taxiNodeId = owner.m_taxi.GetTaxiDestination();
            owner.m_taxi.ClearTaxiDestinations();
            owner.Dismount();
            owner.RemoveUnitFlag(UnitFlags.RemoveClientControl | UnitFlags.OnTaxi);

            if (owner.m_taxi.Empty())
            {
                // update z position to ground and orientation for landing point
                // this prevent cheating with landing  point at lags
                // when client side flight end early in comparison server side
                owner.StopMoving();
                // When the player reaches the last flight point, teleport to destination taxi node location
                var node = CliDB.TaxiNodesStorage.LookupByKey(taxiNodeId);
                if (node != null)
                {
                    owner.SetFallInformation(0, node.Pos.Z);
                    owner.TeleportTo(node.ContinentID, node.Pos.X, node.Pos.Y, node.Pos.Z, owner.GetOrientation());
                }
            }

            owner.RemovePlayerFlag(PlayerFlags.TaxiBenchmark);
        }

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
                if (path >= CliDB.TaxiPathNodesByPath.Keys.Max())
                    return;

                var nodes = CliDB.TaxiPathNodesByPath[path];
                if (!nodes.Empty())
                {
                    TaxiPathNodeRecord start = nodes[0];
                    TaxiPathNodeRecord end = nodes[^1];
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
                            _pointsForPathSwitch[^1].PathIndex -= 1;
                        }
                    }
                }

                _pointsForPathSwitch.Add(new TaxiNodeChangeInfo((uint)(_path.Count - 1), (long)Math.Ceiling(cost * discount)));
            }
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

        void DoEventIfAny(Player owner, TaxiPathNodeRecord node, bool departure)
        {
            Cypher.Assert(node != null, owner.GetDebugInfo());

            uint eventid = departure ? node.DepartureEventID : node.ArrivalEventID;
            if (eventid != 0)
            {
                Log.outDebug(LogFilter.MapsScript, $"FlightPathMovementGenerator::DoEventIfAny: taxi {(departure ? "departure" : "arrival")} event {eventid} of node {node.NodeIndex} of path {node.PathID} for player {owner.GetName()}");
                GameEvents.Trigger(eventid, owner, owner);
            }
        }

        void InitEndGridInfo()
        {
            int nodeCount = _path.Count;        //! Number of nodes in path.
            _endMapId = _path[nodeCount - 1].ContinentID; //! MapId of last node

            if (nodeCount < 3)
                _preloadTargetNode = 0;
            else
                _preloadTargetNode = (uint)nodeCount - 3;

            while (_path[(int)_preloadTargetNode].ContinentID != _endMapId)
                ++_preloadTargetNode;

            _endGridX = _path[nodeCount - 1].Loc.X;
            _endGridY = _path[nodeCount - 1].Loc.Y;
        }

        void PreloadEndGrid(Player owner)
        {
            // Used to preload the final grid where the flightmaster is
            Map endMap = owner.GetMap();

            // Load the grid
            if (endMap != null)
            {
                Log.outDebug(LogFilter.Server, "FlightPathMovementGenerator::PreloadEndGrid: Preloading grid ({0}, {1}) for map {2} at node index {3}/{4}", _endGridX, _endGridY, _endMapId, _preloadTargetNode, _path.Count - 1);
                endMap.LoadGrid(_endGridX, _endGridY);
            }
            else
                Log.outDebug(LogFilter.Server, "FlightPathMovementGenerator::PreloadEndGrid: Unable to determine map to preload flightmaster grid");
        }

        uint GetPathId(int index)
        {
            if (index >= _path.Count)
                return 0;

            return _path[index].PathID;
        }
        
        public override string GetDebugInfo()
        {
            return $"Current Node: {GetCurrentNode()}\n{base.GetDebugInfo()}\nStart Path Id: {GetPathId(0)} Path Size: {_path.Count} HasArrived: {HasArrived()} End Grid X: {_endGridX} " +
                $"End Grid Y: {_endGridY} End Map Id: {_endMapId} Preloaded Target Node: {_preloadTargetNode}";
        }
        
        public override bool GetResetPosition(Unit u, out float x, out float y, out float z)
        {
            var node = _path[_currentNode];
            x = node.Loc.X;
            y = node.Loc.Y;
            z = node.Loc.Z;
            return true;
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Flight; }

        public List<TaxiPathNodeRecord> GetPath() { return _path; }

        bool HasArrived() { return _currentNode >= _path.Count; }

        public void SkipCurrentNode() { ++_currentNode; }

        public uint GetCurrentNode() { return (uint)_currentNode; }

        float _endGridX;                //! X coord of last node location
        float _endGridY;                //! Y coord of last node location
        uint _endMapId;               //! map Id of last node location
        uint _preloadTargetNode;      //! node index where preloading starts

        List<TaxiPathNodeRecord> _path = new();
        int _currentNode;
        List<TaxiNodeChangeInfo> _pointsForPathSwitch = new();    //! node indexes and costs where TaxiPath changes

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
