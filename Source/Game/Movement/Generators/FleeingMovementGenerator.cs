// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using System;

namespace Game.Movement
{
    public class FleeingMovementGenerator : MovementGenerator
    {
        public const float MIN_QUIET_DISTANCE = 28.0f;
        public const float MAX_QUIET_DISTANCE = 43.0f;

        public FleeingMovementGenerator(ObjectGuid fright)
        {
            _fleeTargetGUID = fright;
            _timer = new TimeTracker();

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Highest;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Fleeing;
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (owner == null || !owner.IsAlive())
                return;

            _path = null;
            SetTargetLocation(owner);
        }

        public override void Reset(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (owner == null || !owner.IsAlive())
                return false;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                _path = null;
                return true;
            }
            else
                RemoveFlag(MovementGeneratorFlags.Interrupted);

            _timer.Update(diff);
            if ((HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()) || (_timer.Passed() && owner.MoveSpline.Finalized()))
            {
                RemoveFlag(MovementGeneratorFlags.Transitory);
                SetTargetLocation(owner);
            }

            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.FleeingMove);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            if (active)
            {
                owner.ClearUnitState(UnitState.FleeingMove);

                if (owner.IsCreature())
                {
                    Unit victim = owner.GetVictim();
                    if (victim != null)
                        owner.SetTarget(victim.GetGUID());
                }
                else if (owner.IsPlayer())
                    owner.StopMoving();
            }
        }

        void SetTargetLocation(Unit owner)
        {
            if (owner == null || !owner.IsAlive())
                return;

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                _path = null;
                return;
            }

            Position destination = new(owner.GetPosition());
            GetPoint(owner, destination);

            // Add LOS check for target point
            if (!owner.IsWithinLOS(destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ()))
            {
                _timer.Reset(200);
                return;
            }

            if (_path == null)
            {
                _path = new PathGenerator(owner);
                _path.SetPathLengthLimit(30.0f);
            }

            bool result = _path.CalculatePath(destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ());
            if (!result || _path.GetPathType().HasFlag(PathType.NoPath) || _path.GetPathType().HasFlag(PathType.Shortcut) || _path.GetPathType().HasFlag(PathType.FarFromPoly))
            {
                _timer.Reset(100);
                return;
            }

            owner.AddUnitState(UnitState.FleeingMove);

            MoveSplineInit init = new(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(false);
            uint traveltime = (uint)init.Launch();
            _timer.Reset(traveltime + RandomHelper.URand(800, 1500));
        }

        void GetPoint(Unit owner, Position position)
        {
            float casterDistance, casterAngle;
            Unit fleeTarget = Global.ObjAccessor.GetUnit(owner, _fleeTargetGUID);
            if (fleeTarget != null)
            {
                casterDistance = fleeTarget.GetDistance(owner);
                if (casterDistance > 0.2f)
                    casterAngle = fleeTarget.GetAbsoluteAngle(owner);
                else
                    casterAngle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
            }
            else
            {
                casterDistance = 0.0f;
                casterAngle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
            }

            float distance, angle;
            if (casterDistance < MIN_QUIET_DISTANCE)
            {
                distance = RandomHelper.FRand(0.4f, 1.3f) * (MIN_QUIET_DISTANCE - casterDistance);
                angle = casterAngle + RandomHelper.FRand(-MathF.PI / 8.0f, MathF.PI / 8.0f);
            }
            else if (casterDistance > MAX_QUIET_DISTANCE)
            {
                distance = RandomHelper.FRand(0.4f, 1.0f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
                angle = -casterAngle + RandomHelper.FRand(-MathF.PI / 4.0f, MathF.PI / 4.0f);
            }
            else    // we are inside quiet range
            {
                distance = RandomHelper.FRand(0.6f, 1.2f) * (MAX_QUIET_DISTANCE - MIN_QUIET_DISTANCE);
                angle = RandomHelper.FRand(0.0f, 2.0f * MathF.PI);
            }

            owner.MovePositionToFirstCollision(position, distance, angle);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Fleeing;
        }

        public override void UnitSpeedChanged()
        {
            AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
        }

        PathGenerator _path;
        ObjectGuid _fleeTargetGUID;
        TimeTracker _timer;
    }

    public class TimedFleeingMovementGenerator : FleeingMovementGenerator
    {
        public TimedFleeingMovementGenerator(ObjectGuid fright, TimeSpan time) : base(fright)
        {
            _totalFleeTime = new TimeTracker(time);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (owner == null || !owner.IsAlive())
                return false;

            _totalFleeTime.Update(diff);
            if (_totalFleeTime.Passed())
                return false;

            return base.Update(owner.ToCreature(), diff);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (!active)
                return;

            owner.StopMoving();
            if (owner.IsCreature() && owner.IsAlive())
            {
                Unit victim = owner.GetVictim();
                if (victim != null)
                {
                    owner.AttackStop();
                    owner.GetAI().AttackStart(victim);
                }
            }

            if (movementInform)
            {
                Creature ownerCreature = owner.ToCreature();
                CreatureAI ai = ownerCreature != null ? ownerCreature.GetAI() : null;
                if (ai != null)
                    ai.MovementInform(MovementGeneratorType.TimedFleeing, 0);
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.TimedFleeing;
        }

        TimeTracker _totalFleeTime;
    }
}
