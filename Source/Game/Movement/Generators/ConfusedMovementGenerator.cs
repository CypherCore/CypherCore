// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Movement
{
    public class ConfusedMovementGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public ConfusedMovementGenerator()
        {
            _timer = new TimeTracker();
            _reference = new();

            Priority = MovementGeneratorPriority.Highest;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Confused;
        }

        public override bool DoInitialize(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (!owner.IsAlive())
                return false;

            // TODO: UNIT_FIELD_FLAGS should not be handled by generators
            owner.SetUnitFlag(UnitFlags.Confused);

            _timer.Reset(0);
            _reference = owner.GetPosition();
            _path = null;
            SetTargetLocation(owner);
            return true;
        }

        public override bool DoReset(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            return DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
        {
            if (!owner.IsAlive())
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

            // waiting for next move
            _timer.Update(diff);
            if ((HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()) || (_timer.Passed() && owner.MoveSpline.Finalized()))
            {
                RemoveFlag(MovementGeneratorFlags.Transitory);
                SetTargetLocation(owner);
            }

            return true;
        }

        public override void DoDeactivate(T owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.ConfusedMove);
        }

        public override void DoFinalize(T owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            if (active)
            {
                owner.RemoveUnitFlag(UnitFlags.Confused);
                owner.ClearUnitState(UnitState.ConfusedMove);

                if (owner.IsCreature())
                {
                    if (owner.GetVictim() != null)
                        owner.SetTarget(owner.GetVictim().GetGUID());
                }
                else if (owner.IsPlayer())
                    owner.StopMoving();
            }
        }

        void SetTargetLocation(T owner)
        {
            if (owner == null)
                return;

            Position destination = new(_reference);
            float distance = 4.0f * RandomHelper.FRand(0.0f, 1.0f) - 2.0f;
            float angle = RandomHelper.FRand(0.0f, 1.0f) * MathF.PI * 2.0f;
            owner.MovePositionToFirstCollision(destination, distance, angle);

            // Check if the destination is in LOS
            if (!owner.IsWithinLOS(destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ()))
            {
                // Retry later on
                _timer.Reset(200);
                return;
            }

            if (_path == null)
            {
                _path = new PathGenerator(owner);
                _path.SetPathLengthLimit(30.0f);
            }

            bool result = _path.CalculatePath(destination.GetPositionX(), destination.GetPositionY(), destination.GetPositionZ());
            if (!result || (_path.GetPathType().HasFlag(PathType.NoPath))
                || (_path.GetPathType().HasFlag(PathType.Shortcut))
                || (_path.GetPathType().HasFlag(PathType.FarFromPoly)))
            {
                _timer.Reset(100);
                return;
            }

            owner.AddUnitState(UnitState.ConfusedMove);

            MoveSplineInit init = new(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(true);
            int traveltime = init.Launch();
            _timer.Reset((uint)(traveltime + RandomHelper.URand(800, 1500)));
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Confused;
        }

        public override void UnitSpeedChanged() { AddFlag(MovementGeneratorFlags.SpeedUpdatePending); }

        PathGenerator _path;
        TimeTracker _timer;
        Position _reference;
    }
}
