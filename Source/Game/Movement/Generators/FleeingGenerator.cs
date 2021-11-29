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
using System;

namespace Game.Movement
{
    public class FleeingGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public const float MIN_QUIET_DISTANCE = 28.0f;
        public const float MAX_QUIET_DISTANCE = 43.0f;

        public FleeingGenerator(ObjectGuid fright)
        {
            _fleeTargetGUID = fright;
            _timer = new TimeTracker();

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Highest;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Fleeing;
        }

        public override void DoInitialize(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (owner == null || !owner.IsAlive())
                return;

            // TODO: UNIT_FIELD_FLAGS should not be handled by generators
            owner.AddUnitFlag(UnitFlags.Fleeing);
            _path = null;
            SetTargetLocation(owner);
        }

        public override void DoReset(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
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

        public override void DoDeactivate(T owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.FleeingMove);
        }

        public override void DoFinalize(T owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            if (active)
            {
                if (owner.IsPlayer())
                {
                    owner.RemoveUnitFlag(UnitFlags.Fleeing);
                    owner.ClearUnitState(UnitState.FleeingMove);
                    owner.StopMoving();
                }
                else
                {
                    owner.RemoveUnitFlag(UnitFlags.Fleeing);
                    owner.ClearUnitState(UnitState.FleeingMove);
                    if (owner.GetVictim() != null)
                        owner.SetTarget(owner.GetVictim().GetGUID());
                }
            }
        }

        void SetTargetLocation(T owner)
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

            Position destination = new (owner.GetPosition());
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
            if (!result || _path.GetPathType().HasAnyFlag(PathType.NoPath) || _path.GetPathType().HasAnyFlag(PathType.Shortcut))
            {
                _timer.Reset(100);
                return;
            }

            owner.AddUnitState(UnitState.FleeingMove);

            MoveSplineInit init = new(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(false);
            int traveltime = init.Launch();
            _timer.Reset(traveltime + RandomHelper.URand(800, 1500));
        }

        void GetPoint(T owner, Position position)
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

    public class TimedFleeingGenerator : FleeingGenerator<Creature>
    {
        public TimedFleeingGenerator(ObjectGuid fright, uint time) : base(fright)
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

            return DoUpdate(owner.ToCreature(), diff);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (!active)
                return;

            owner.RemoveUnitFlag(UnitFlags.Fleeing);
            Unit victim = owner.GetVictim();
            if (victim != null)
            {
                if (owner.IsAlive())
                {
                    owner.AttackStop();
                    owner.ToCreature().GetAI().AttackStart(victim);
                }
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.TimedFleeing;
        }

        TimeTracker _totalFleeTime;
    }
}
