// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Movement
{
    public class FormationMovementGenerator : MovementGeneratorMedium<Creature>
    {
        AbstractFollower _abstractFollower;

        static uint FORMATION_MOVEMENT_INTERVAL = 1200; // sniffed (3 batch update cycles)
        float _range;
        float _angle;
        uint _point1;
        uint _point2;
        uint _lastLeaderSplineID;
        bool _hasPredictedDestination;

        Position _lastLeaderPosition;
        TimeTracker _nextMoveTimer = new();

        public FormationMovementGenerator(Unit leader, float range, float angle, uint point1, uint point2)
        {
            _abstractFollower = new(leader);
            _range = range;
            _angle = angle;
            _point1 = point1;
            _point2 = point2;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.FollowFormation;
        }

        public override void DoInitialize(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return;
            }

            _nextMoveTimer.Reset(0);
        }

        public override void DoReset(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            DoInitialize(owner);
        }

        public override bool DoUpdate(Creature owner, uint diff)
        {
            Unit target = _abstractFollower.GetTarget();

            if (owner == null || target == null)
                return false;

            // Owner cannot move. Reset all fields and wait for next action
            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                _nextMoveTimer.Reset(0);
                _hasPredictedDestination = false;
                return true;
            }

            // Update home position
            // If target is not moving and destination has been predicted and if we are on the same spline, we stop as well
            if (target.MoveSpline.Finalized() && target.MoveSpline.GetId() == _lastLeaderSplineID && _hasPredictedDestination)
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                _nextMoveTimer.Reset(0);
                _hasPredictedDestination = false;
                return true;
            }

            if (!owner.MoveSpline.Finalized())
                owner.SetHomePosition(owner.GetPosition());

            // Formation leader has launched a new spline, launch a new one for our member as well
            // This action does not reset the regular movement launch cycle interval
            if (!target.MoveSpline.Finalized() && target.MoveSpline.GetId() != _lastLeaderSplineID)
            {
                // Update formation angle
                if (_point1 != 0 && target.IsCreature())
                {
                    CreatureGroup formation = target.ToCreature().GetFormation();
                    if (formation != null)
                    {
                        Creature leader = formation.GetLeader();
                        if (leader != null)
                        {
                            uint currentWaypoint = leader.GetCurrentWaypointInfo().nodeId;
                            if (currentWaypoint == _point1 || currentWaypoint == _point2)
                                _angle = MathF.PI * 2 - _angle;
                        }
                    }
                }

                LaunchMovement(owner, target);
                _lastLeaderSplineID = target.MoveSpline.GetId();
                return true;
            }

            _nextMoveTimer.Update(diff);
            if (_nextMoveTimer.Passed())
            {
                _nextMoveTimer.Reset(FORMATION_MOVEMENT_INTERVAL);

                // Our leader has a different position than on our last check, launch movement.
                if (_lastLeaderPosition != target.GetPosition())
                {
                    LaunchMovement(owner, target);
                    return true;
                }
            }

            // We have reached our destination before launching a new movement. Alling facing with leader
            if (owner.HasUnitState(UnitState.FollowFormationMove) && owner.MoveSpline.Finalized())
            {
                owner.ClearUnitState(UnitState.FollowFormationMove);
                owner.SetFacingTo(target.GetOrientation());
                MovementInform(owner);
            }

            return true;
        }

        void LaunchMovement(Creature owner, Unit target)
        {
            float relativeAngle = 0.0f;

            // Determine our relative angle to our current spline destination point
            if (!target.MoveSpline.Finalized())
                relativeAngle = target.GetRelativeAngle(new Position(target.MoveSpline.CurrentDestination()));

            // Destination calculation
            /*
                According to sniff data, formation members have a periodic move interal of 1,2s.
                Each of these splines has a exact duration of 1650ms +- 1ms when no pathfinding is involved.
                To get a representative result like that we have to predict our formation leader's path
                and apply our formation shape based on that destination.
            */
            Position dest = new Position(target.GetPosition());
            float velocity = 0.0f;

            // Formation leader is moving. Predict our destination
            if (!target.MoveSpline.Finalized())
            {
                // Pick up leader's spline velocity
                velocity = target.MoveSpline.velocity;

                // Calculate travel distance to get a 1650ms result
                float travelDist = velocity * 1.65f;

                // Move destination ahead...
                target.MovePositionToFirstCollision(dest, travelDist, relativeAngle);
                // ... and apply formation shape
                target.MovePositionToFirstCollision(dest, _range, _angle + relativeAngle);

                float distance = owner.GetExactDist(dest);

                // Calculate catchup speed mod (Limit to a maximum of 50% of our original velocity
                float velocityMod = Math.Min(distance / travelDist, 1.5f);

                // Now we will always stay synch with our leader
                velocity *= velocityMod;
                _hasPredictedDestination = true;
            }
            else
            {
                // Formation leader is not moving. Just apply the base formation shape on his position.
                target.MovePositionToFirstCollision(dest, _range, _angle + relativeAngle);
                _hasPredictedDestination = false;
            }

            // Leader is not moving, so just pick up his default walk speed
            if (velocity == 0.0f)
                velocity = target.GetSpeed(UnitMoveType.Walk);

            MoveSplineInit init = new(owner);
            init.MoveTo(dest);
            init.SetVelocity(velocity);
            init.Launch();

            _lastLeaderPosition = new Position(target.GetPosition());
            owner.AddUnitState(UnitState.FollowFormationMove);
            RemoveFlag(MovementGeneratorFlags.Interrupted);
        }

        public override void DoDeactivate(Creature owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.FollowFormationMove);
        }

        public override void DoFinalize(Creature owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.FollowFormationMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                MovementInform(owner);
        }

        public override void UnitSpeedChanged()
        {
            AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Formation;
        }

        void MovementInform(Creature owner)
        {
            if (owner.GetAI() != null)
                owner.GetAI().MovementInform(MovementGeneratorType.Formation, 0);
        }
    }
}
