// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting.v2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Movement
{
    public class WaypointMovementGenerator : MovementGeneratorMedium<Creature>
    {
        uint _pathId;
        WaypointPath _path;
        int _currentNode;

        TimeTracker _duration;
        float? _speed;
        MovementWalkRunSpeedSelectionMode _speedSelectionMode;
        (TimeSpan min, TimeSpan max)? _waitTimeRangeAtPathEnd;
        float? _wanderDistanceAtPathEnds;
        bool? _followPathBackwardsFromEndToStart;
        bool? _exactSplinePath;
        bool _repeating;
        bool _generatePath;

        TimeTracker _moveTimer;
        TimeTracker _nextMoveTime;
        List<int> _waypointTransitionSplinePoints = new();
        int _waypointTransitionSplinePointsIndex;
        bool _isReturningToStart;

        static TimeSpan SEND_NEXT_POINT_EARLY_DELTA = TimeSpan.FromMilliseconds(1500);

        public WaypointMovementGenerator(uint pathId = 0, bool repeating = true, TimeSpan? duration = null, float? speed = null, MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default,
            (TimeSpan min, TimeSpan max)? waitTimeRangeAtPathEnd = null, float? wanderDistanceAtPathEnds = null, bool? followPathBackwardsFromEndToStart = null, bool? exactSplinePath = null, bool generatePath = true, ActionResultSetter<MovementStopReason> scriptResult = null)
        {
            _nextMoveTime = new TimeTracker(0);
            _pathId = pathId;
            _repeating = repeating;
            _speed = speed;
            _speedSelectionMode = speedSelectionMode;
            _waitTimeRangeAtPathEnd = waitTimeRangeAtPathEnd;
            _wanderDistanceAtPathEnds = wanderDistanceAtPathEnds;
            _followPathBackwardsFromEndToStart = followPathBackwardsFromEndToStart;
            _exactSplinePath = exactSplinePath;
            _generatePath = generatePath;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
            ScriptResult = scriptResult;

            if (duration.HasValue)
                _duration = new(duration.Value);

            _path.BuildSegments();
        }

        public WaypointMovementGenerator(WaypointPath path, bool repeating = true, TimeSpan? duration = null, float? speed = null, MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default,
            (TimeSpan min, TimeSpan max)? waitTimeRangeAtPathEnd = null, float? wanderDistanceAtPathEnds = null, bool? followPathBackwardsFromEndToStart = null, bool? exactSplinePath = null, bool generatePath = true, ActionResultSetter<MovementStopReason> scriptResult = null)
        {
            _nextMoveTime = new TimeTracker(0);
            _repeating = repeating;
            _path = path;
            _speed = speed;
            _speedSelectionMode = speedSelectionMode;
            _waitTimeRangeAtPathEnd = waitTimeRangeAtPathEnd;
            _wanderDistanceAtPathEnds = wanderDistanceAtPathEnds;
            _followPathBackwardsFromEndToStart = followPathBackwardsFromEndToStart;
            _exactSplinePath = exactSplinePath;
            _generatePath = generatePath;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
            ScriptResult = scriptResult;

            if (duration.HasValue)
                _duration = new(duration.Value);
        }

        public override void Pause(uint timer)
        {
            if (timer != 0)
            {
                // Don't try to paused an already paused generator
                if (HasFlag(MovementGeneratorFlags.Paused))
                    return;

                AddFlag(MovementGeneratorFlags.TimedPaused);
                _nextMoveTime.Reset(timer);
                RemoveFlag(MovementGeneratorFlags.Paused);
            }
            else
            {
                AddFlag(MovementGeneratorFlags.Paused);
                _nextMoveTime.Reset(1); // Needed so that Update does not behave as if node was reached
                RemoveFlag(MovementGeneratorFlags.TimedPaused);
            }
        }

        public override void Resume(uint overrideTimer)
        {
            if (overrideTimer != 0)
                _nextMoveTime.Reset(overrideTimer);

            if (_nextMoveTime.Passed())
                _nextMoveTime.Reset(1); // Needed so that Update does not behave as if node was reached

            RemoveFlag(MovementGeneratorFlags.Paused);
        }

        public override bool GetResetPosition(Unit owner, out float x, out float y, out float z)
        {
            x = y = z = 0;

            // prevent a crash at empty waypoint path.
            if (_path == null || _path.Nodes.Empty())
                return false;

            Cypher.Assert(_currentNode < _path.Nodes.Count, $"WaypointMovementGenerator::GetResetPosition: tried to reference a node id ({_currentNode}) which is not included in path ({_path.Id})");
            WaypointNode waypoint = _path.Nodes.ElementAt(_currentNode);

            x = waypoint.X;
            y = waypoint.Y;
            z = waypoint.Z;
            return true;
        }

        public override void DoInitialize(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            if (IsLoadedFromDB())
            {
                if (_pathId == 0)
                    _pathId = owner.GetWaypointPathId();

                _path = Global.WaypointMgr.GetPath(_pathId);
            }

            if (_path == null)
            {
                Log.outError(LogFilter.Sql, $"WaypointMovementGenerator::DoInitialize: couldn't load path for creature ({owner.GetGUID()}) (_pathId: {_pathId})");
                return;
            }

            if (_path.Nodes.Count == 1)
                _repeating = false;

            owner.StopMoving();

            _nextMoveTime.Reset(1000);
        }

        public override void DoReset(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            owner.StopMoving();

            if (!HasFlag(MovementGeneratorFlags.Finalized) && _nextMoveTime.Passed())
                _nextMoveTime.Reset(1); // Needed so that Update does not behave as if node was reached
        }

        public override bool DoUpdate(Creature owner, uint diff)
        {
            if (owner == null || !owner.IsAlive())
                return true;

            if (HasFlag(MovementGeneratorFlags.Finalized | MovementGeneratorFlags.Paused))
                return true;

            if (_path == null || _path.Nodes.Empty())
                return true;

            if (_duration != null)
            {
                _duration.Update(diff);
                if (_duration.Passed())
                {
                    RemoveFlag(MovementGeneratorFlags.Transitory);
                    AddFlag(MovementGeneratorFlags.InformEnabled);
                    AddFlag(MovementGeneratorFlags.Finalized);
                    owner.UpdateCurrentWaypointInfo(0, 0);
                    SetScriptResult(MovementStopReason.Finished);
                    return false;
                }
            }

            if (owner.HasUnitState(UnitState.NotMove | UnitState.LostControl) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return true;
            }

            if (HasFlag(MovementGeneratorFlags.Interrupted))
            {
                /*
                 *  relaunch only if
                 *  - has a tiner? -> was it interrupted while not waiting aka moving? need to check both:
                 *      -> has a timer - is it because its waiting to start next node?
                 *      -> has a timer - is it because something set it while moving (like timed pause)?
                 *
                 *  - doesnt have a timer? -> is movement valid?
                 *
                 *  TODO: ((_nextMoveTime.Passed() && VALID_MOVEMENT) || (!_nextMoveTime.Passed() && !HasFlag(MOVEMENTGENERATOR_FLAG_INFORM_ENABLED)))
                 */
                if (HasFlag(MovementGeneratorFlags.Initialized) && (_nextMoveTime.Passed() || !HasFlag(MovementGeneratorFlags.InformEnabled)))
                {
                    StartMove(owner, true);
                    return true;
                }

                RemoveFlag(MovementGeneratorFlags.Interrupted);
            }

            // if it's moving
            if (!UpdateMoveTimer(diff) && !owner.MoveSpline.Finalized())
            {
                // set home position at place (every MotionMaster::UpdateMotion)
                if (owner.GetTransGUID().IsEmpty())
                    owner.SetHomePosition(owner.GetPosition());

                // handle switching points in continuous segments
                if (IsExactSplinePath())
                {
                    if (_waypointTransitionSplinePointsIndex < _waypointTransitionSplinePoints.Count && owner.MoveSpline.CurrentPathIdx() >= _waypointTransitionSplinePoints[_waypointTransitionSplinePointsIndex])
                    {
                        OnArrived(owner);
                        ++_waypointTransitionSplinePointsIndex;
                        if (ComputeNextNode())
                        {
                            CreatureAI ai = owner.GetAI();
                            if (ai != null)
                                ai.WaypointStarted(_path.Nodes[_currentNode].Id, _path.Id);
                        }
                    }
                }

                // relaunch movement if its speed has changed
                if (HasFlag(MovementGeneratorFlags.SpeedUpdatePending))
                    StartMove(owner, true);
            }
            else if (!_nextMoveTime.Passed()) // it's not moving, is there a timer?
            {
                if (UpdateWaitTimer(diff))
                {
                    if (!HasFlag(MovementGeneratorFlags.Initialized)) // initial movement call
                    {
                        StartMove(owner);
                        return true;
                    }
                    else if (!HasFlag(MovementGeneratorFlags.InformEnabled)) // timer set before node was reached, resume now
                    {
                        StartMove(owner, true);
                        return true;
                    }
                }
                else
                    return true; // keep waiting
            }
            else // not moving, no timer
            {
                if (HasFlag(MovementGeneratorFlags.Initialized) && !HasFlag(MovementGeneratorFlags.InformEnabled))
                {
                    OnArrived(owner); // hooks and wait timer reset (if necessary)
                    AddFlag(MovementGeneratorFlags.InformEnabled); // signals to future StartMove that it reached a node
                }

                if (_nextMoveTime.Passed()) // OnArrived might have set a timer
                    StartMove(owner); // check path status, get next point and move if necessary & can
            }

            return true;
        }

        public override void DoDeactivate(Creature owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void DoFinalize(Creature owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
            {
                owner.ClearUnitState(UnitState.RoamingMove);

                // TODO: Research if this modification is needed, which most likely isnt
                owner.SetWalk(false);
            }

            if (movementInform)
                SetScriptResult(MovementStopReason.Finished);
        }

        public void MovementInform(Creature owner)
        {
            WaypointNode waypoint = _path.Nodes.ElementAt(_currentNode);
            CreatureAI ai = owner.GetAI();
            if (ai != null)
            {
                ai.MovementInform(MovementGeneratorType.Waypoint, waypoint.Id);
                ai.WaypointReached(waypoint.Id, _path.Id);
            }
        }

        void OnArrived(Creature owner)
        {
            if (_path == null || _path.Nodes.Empty())
                return;

            Cypher.Assert(_currentNode < _path.Nodes.Count, $"WaypointMovementGenerator.OnArrived: tried to reference a node id ({_currentNode}) which is not included in path ({_path.Id})");
            WaypointNode waypoint = _path.Nodes.ElementAt(_currentNode);

            if (waypoint.Delay != TimeSpan.Zero)
            {
                owner.ClearUnitState(UnitState.RoamingMove);
                _nextMoveTime.Reset(waypoint.Delay);
            }

            if (_waitTimeRangeAtPathEnd.HasValue && IsFollowingPathBackwardsFromEndToStart()
                && ((_isReturningToStart && _currentNode == 0) || (!_isReturningToStart && _currentNode == _path.Nodes.Count - 1)))
            {
                owner.ClearUnitState(UnitState.RoamingMove);
                TimeSpan waitTime = RandomHelper.RandTime(_waitTimeRangeAtPathEnd.Value.min, _waitTimeRangeAtPathEnd.Value.max);
                if (_duration != null)
                    _duration.Update(waitTime); // count the random movement time as part of waypoing movement action

                if (_wanderDistanceAtPathEnds.HasValue)
                    owner.GetMotionMaster().MoveRandom(_wanderDistanceAtPathEnds.Value, waitTime, MovementSlot.Active);
                else
                    _nextMoveTime.Reset(waitTime);
            }

            MovementInform(owner);

            owner.UpdateCurrentWaypointInfo(waypoint.Id, _path.Id);
        }

        void StartMove(Creature owner, bool relaunch = false)
        {
            // sanity checks
            if (owner == null || !owner.IsAlive() || HasFlag(MovementGeneratorFlags.Finalized) || (relaunch && (HasFlag(MovementGeneratorFlags.InformEnabled) || !HasFlag(MovementGeneratorFlags.Initialized))))
                return;

            if (_path == null || _path.Nodes.Empty())
                return;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting() || (owner.IsFormationLeader() && !owner.IsFormationLeaderMoveAllowed())) // if cannot move OR cannot move because of formation
            {
                _nextMoveTime.Reset(1000); // delay 1s
                return;
            }

            bool transportPath = !owner.GetTransGUID().IsEmpty();

            int previousNode = _currentNode;
            if (HasFlag(MovementGeneratorFlags.InformEnabled) && HasFlag(MovementGeneratorFlags.Initialized))
            {
                if (ComputeNextNode())
                {
                    Cypher.Assert(_currentNode < _path.Nodes.Count, $"WaypointMovementGenerator.StartMove: tried to reference a node id ({_currentNode}) which is not included in path ({_path.Id})");
                    // inform AI
                    CreatureAI ai = owner.GetAI();
                    if (ai != null)
                        ai.WaypointStarted(_path.Nodes[_currentNode].Id, _path.Id);
                }
                else
                {
                    WaypointNode currentWaypoint = _path.Nodes[_currentNode];
                    float x = currentWaypoint.X;
                    float y = currentWaypoint.Y;
                    float z = currentWaypoint.Z;
                    float o = owner.GetOrientation();

                    if (!transportPath)
                        owner.SetHomePosition(x, y, z, o);
                    else
                    {
                        ITransport trans = owner.GetTransport();
                        if (trans != null)
                        {
                            o -= trans.GetTransportOrientation();
                            owner.SetTransportHomePosition(x, y, z, o);
                            trans.CalculatePassengerPosition(ref x, ref y, ref z, ref o);
                            owner.SetHomePosition(x, y, z, o);
                        }
                        // else if (vehicle != null) - this should never happen, vehicle offsets are const
                    }

                    AddFlag(MovementGeneratorFlags.Finalized);
                    owner.UpdateCurrentWaypointInfo(0, 0);

                    // inform AI
                    CreatureAI ai = owner.GetAI();
                    if (ai != null)
                        ai.WaypointPathEnded(currentWaypoint.Id, _path.Id);

                    SetScriptResult(MovementStopReason.Finished);
                    return;
                }
            }
            else if (!HasFlag(MovementGeneratorFlags.Initialized))
            {
                AddFlag(MovementGeneratorFlags.Initialized);

                // inform AI
                CreatureAI ai = owner.GetAI();
                if (ai != null)
                    ai.WaypointStarted(_path.Nodes[_currentNode].Id, _path.Id);
            }

            Cypher.Assert(_currentNode < _path.Nodes.Count, $"WaypointMovementGenerator.StartMove: tried to reference a node id ({_currentNode}) which is not included in path ({_path.Id})");
            WaypointNode lastWaypointForSegment = _path.Nodes[_currentNode];

            bool isCyclic = IsCyclic();
            List<Vector3> points = new();

            if (IsExactSplinePath())
                CreateMergedPath(owner, _path, previousNode, _currentNode, _isReturningToStart, false, isCyclic, points, _waypointTransitionSplinePoints, ref lastWaypointForSegment);
            else
                CreateSingularPointPath(owner, _path, _currentNode, _generatePath, points, _waypointTransitionSplinePoints);

            _waypointTransitionSplinePointsIndex = 0;

            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.InformEnabled | MovementGeneratorFlags.TimedPaused);

            owner.AddUnitState(UnitState.RoamingMove);

            if (isCyclic)
            {
                bool isFirstCycle = relaunch || owner.MoveSpline.Finalized() || !owner.MoveSpline.IsCyclic();
                if (!isFirstCycle)
                {
                    for (var i = 0; i < _waypointTransitionSplinePoints.Count; ++i)
                        --_waypointTransitionSplinePoints[i];

                    // cyclic paths are using identical duration to first cycle with EnterCycle
                    _moveTimer.Reset(TimeSpan.FromMilliseconds(owner.MoveSpline.Duration()));
                    return;
                }
            }

            MoveSplineInit init = new(owner);

            //! If creature is on transport, we assume waypoints set in DB are already transport offsets
            if (transportPath)
                init.DisableTransportPathTransformations();

            init.MovebyPath(points.ToArray());

            if (lastWaypointForSegment.Orientation.HasValue
                && (lastWaypointForSegment.Delay != TimeSpan.Zero || (_isReturningToStart ? _currentNode == 0 : _currentNode == _path.Nodes.Count - 1)))
                init.SetFacing(lastWaypointForSegment.Orientation.Value);

            switch (_path.MoveType)
            {
                case WaypointMoveType.Land:
                    init.SetAnimation(AnimTier.Ground);
                    init.SetFly();
                    break;
                case WaypointMoveType.Takeoff:
                    init.SetAnimation(AnimTier.Fly);
                    init.SetFly();
                    break;
                case WaypointMoveType.Run:
                    init.SetWalk(false);
                    break;
                case WaypointMoveType.Walk:
                    init.SetWalk(true);
                    break;
            }

            switch (_speedSelectionMode) // overrides move type from each waypoint if set
            {
                case MovementWalkRunSpeedSelectionMode.Default:
                    break;
                case MovementWalkRunSpeedSelectionMode.ForceRun:
                    init.SetWalk(false);
                    break;
                case MovementWalkRunSpeedSelectionMode.ForceWalk:
                    init.SetWalk(true);
                    break;
                default:
                    break;
            }

            if (_path.Velocity.HasValue && !_speed.HasValue)
                _speed = _path.Velocity;

            if (_speed.HasValue)
                init.SetVelocity(_speed.Value);

            if (isCyclic)
                init.SetCyclic();

            if (IsExactSplinePath() && points.Count > 2 && owner.CanFly())
                init.SetSmooth();

            TimeSpan duration = TimeSpan.FromMilliseconds(init.Launch());

            if (!IsExactSplinePath()
                && duration > 2 * SEND_NEXT_POINT_EARLY_DELTA
                && lastWaypointForSegment.Delay == TimeSpan.Zero
                && _path.Nodes.Count > 2
                // don't cut movement short at ends of path if its not a looping path or if it can be traversed backwards
                && ((_currentNode != 0 && _currentNode != _path.Nodes.Count - 1) || (!IsFollowingPathBackwardsFromEndToStart() && _repeating)))
                duration -= SEND_NEXT_POINT_EARLY_DELTA;

            _moveTimer.Reset(duration);

            // inform formation
            owner.SignalFormationMovement();
        }

        bool ComputeNextNode()
        {
            if ((_currentNode == _path.Nodes.Count - 1) && !_repeating)
                return false;

            if (!IsFollowingPathBackwardsFromEndToStart() || _path.Nodes.Count < 2)
                _currentNode = (_currentNode + 1) % _path.Nodes.Count;
            else
            {
                if (!_isReturningToStart)
                {
                    if (++_currentNode >= _path.Nodes.Count)
                    {
                        _currentNode -= 2;
                        _isReturningToStart = true;
                    }
                }
                else
                {
                    if (_currentNode-- == 0)
                    {
                        _currentNode = 1;
                        _isReturningToStart = false;
                    }
                }
            }
            return true;
        }

        bool IsFollowingPathBackwardsFromEndToStart()
        {
            if (_followPathBackwardsFromEndToStart.HasValue)
                return _followPathBackwardsFromEndToStart.Value;

            return _path.Flags.HasFlag(WaypointPathFlags.FollowPathBackwardsFromEndToStart);
        }

        bool IsExactSplinePath()
        {
            if (_exactSplinePath.HasValue)
                return _exactSplinePath.Value;

            return _path.Flags.HasFlag(WaypointPathFlags.ExactSplinePath);
        }

        bool IsCyclic()
        {
            return !IsFollowingPathBackwardsFromEndToStart()
                && IsExactSplinePath()
                && _repeating
                && _path.ContinuousSegments.Count == 1;
        }

        public override string GetDebugInfo()
        {
            return $"Current Node: {_currentNode}\n{base.GetDebugInfo()}";
        }

        bool UpdateMoveTimer(uint diff) { return UpdateTimer(_moveTimer, diff); }

        bool UpdateWaitTimer(uint diff) { return UpdateTimer(_nextMoveTime, diff); }

        bool UpdateTimer(TimeTracker timer, uint diff)
        {
            timer.Update(diff);
            if (timer.Passed())
            {
                timer.Reset(0);
                return true;
            }
            return false;
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Waypoint; }

        public override void UnitSpeedChanged() { AddFlag(MovementGeneratorFlags.SpeedUpdatePending); }

        bool IsLoadedFromDB() { return _path != null; }

        void CreateSingularPointPath(Unit owner, WaypointPath path, int currentNode, bool generatePath, List<Vector3> points, List<int> waypointTransitionSplinePoints)
        {
            WaypointNode waypoint = path.Nodes[currentNode];
            points.Add(new Vector3(owner.GetPositionX(), owner.GetPositionY(), owner.GetPositionZ()));

            if (generatePath)
            {
                PathGenerator generator = new(owner);
                bool result = generator.CalculatePath(waypoint.X, waypoint.Y, waypoint.Z);
                if (result && (generator.GetPathType() & PathType.NoPath) == 0)
                    points.AddRange(generator.GetPath()[1..]);
                else
                    points.Add(new Vector3(waypoint.X, waypoint.Y, waypoint.Z));
            }
            else
                points.Add(new Vector3(waypoint.X, waypoint.Y, waypoint.Z));

            waypointTransitionSplinePoints.Add(points.Count - 1);
        }

        void CreateMergedPath(Unit owner, WaypointPath path, int previousNode, int currentNode, bool isReturningToStart, bool generatePath, bool isCyclic, List<Vector3> points, List<int> waypointTransitionSplinePoints, ref WaypointNode lastWaypointOnPath)
        {
            var segment = new Func<List<WaypointNode>>(() =>
            {
                // find the continuous segment that our destination waypoint is on
                var segmentItr = path.ContinuousSegments.Find(segmentRange =>
                {
                    bool isInSegmentRange(int node) => node >= segmentRange.First && node < segmentRange.First + segmentRange.Last;
                    return isInSegmentRange(currentNode) && isInSegmentRange(previousNode);
                });

                // handle path returning directly from last point to first
                if (segmentItr == null)
                {
                    if (currentNode != 0 || previousNode != path.Nodes.Count - 1)
                        return path.Nodes[currentNode..1];

                    segmentItr = path.ContinuousSegments[0];
                }

                if (!isReturningToStart)
                    return path.Nodes[currentNode..(segmentItr.Last - (currentNode - segmentItr.First))];

                return path.Nodes[segmentItr.First..(currentNode - segmentItr.First + 1)];
            })();

            lastWaypointOnPath = !isReturningToStart ? segment.Last() : segment.First();

            waypointTransitionSplinePoints.Clear();

            void fillPath(List<WaypointNode> list)
            {
                PathGenerator generator = null;
                if (_generatePath)
                    generator = new(owner);

                Position source = owner.GetPosition();
                points.Add(new Vector3(source.GetPositionX(), source.GetPositionY(), source.GetPositionZ()));

                foreach (var node in list)
                {
                    if (generator != null)
                    {
                        bool result = generator.CalculatePath(source.GetPositionX(), source.GetPositionY(), source.GetPositionZ(), node.X, node.Y, node.Z);
                        if (result && (generator.GetPathType() & PathType.NoPath) == 0)
                            points.AddRange(generator.GetPath()[1..]);
                        else
                            generator = null; // when path generation to a waypoint fails, add all remaining points without pathfinding (preserve legacy behavior of MoveSplineInit::MoveTo)
                    }

                    if (generator == null)
                        points.Add(new Vector3(node.X, node.Y, node.Z));

                    _waypointTransitionSplinePoints.Add(points.Count - 1);

                    source.Relocate(node.X, node.Y, node.Z);
                }
            };

            if (isCyclic)
            {
                // create new cyclic path starting at current node
                List<WaypointNode> cyclicPath = path.Nodes;
                fillPath(cyclicPath[currentNode..].Concat(cyclicPath[0..currentNode]).ToList());
                return;
            }

            if (!isReturningToStart)
                fillPath(segment);
            else
                fillPath(segment[^0..]);
        }
    }
}
