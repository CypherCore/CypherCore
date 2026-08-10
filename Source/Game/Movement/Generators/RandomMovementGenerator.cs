// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.v2;
using System;

namespace Game.Movement
{
    public class RandomMovementGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        PathGenerator _path;
        TimeTracker _timer;
        TimeTracker _duration;
        float? _speed;
        MovementWalkRunSpeedSelectionMode _speedSelectionMode;
        Position _reference;
        float _wanderDistance;
        uint _wanderSteps;

        public RandomMovementGenerator(float distance = 0.0f, TimeSpan? duration = null, float? speed = null, MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default, ActionResultSetter<MovementStopReason> scriptResult = null)
        {
            _timer = new TimeTracker();
            _speed = speed;
            _speedSelectionMode = speedSelectionMode;
            _reference = new();
            _wanderDistance = distance;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
            ScriptResult = scriptResult;

            if (duration.HasValue)
                _duration = new TimeTracker(duration.Value);
        }

        public override bool DoInitialize(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated | MovementGeneratorFlags.TimedPaused);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (!owner.IsAlive())
                return false;

            _reference = owner.GetPosition();

            // Retail seems to let a creature walk 2 up to 10 splines before triggering a pause
            _wanderSteps = RandomHelper.URand(2, 10);

            _timer.Reset(0);
            _path = null;
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
                return true;

            if (HasFlag(MovementGeneratorFlags.Finalized | MovementGeneratorFlags.Paused))
                return true;

            if (_duration != null)
            {
                _duration.Update(diff);
                if (_duration.Passed())
                {
                    RemoveFlag(MovementGeneratorFlags.Transitory);
                    AddFlag(MovementGeneratorFlags.InformEnabled);
                    return false;
                }
            }

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
                SetRandomLocation(owner);

            return true;
        }

        public override void DoDeactivate(T owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void DoFinalize(T owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
            {
                owner.ClearUnitState(UnitState.RoamingMove);
                owner.StopMoving();

                // TODO: Research if this modification is needed, which most likely isnt
                owner.SetWalk(false);
            }

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
            {
                SetScriptResult(MovementStopReason.Finished);
                if (owner.IsCreature() && owner.IsAIEnabled())
                    owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Random, 0);
            }
        }

        public override void Pause(uint timer)
        {
            if (timer != 0)
            {
                AddFlag(MovementGeneratorFlags.TimedPaused);
                _timer.Reset(timer);
                RemoveFlag(MovementGeneratorFlags.Paused);
            }
            else
            {
                AddFlag(MovementGeneratorFlags.Paused);
                RemoveFlag(MovementGeneratorFlags.TimedPaused);
            }
        }

        public override void Resume(uint overrideTimer)
        {
            if (overrideTimer != 0)
                _timer.Reset(overrideTimer);

            RemoveFlag(MovementGeneratorFlags.Paused);
        }

        void SetRandomLocation(T owner)
        {
            if (owner.HasUnitState(UnitState.NotMove | UnitState.LostControl) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                _path = null;
                return;
            }

            Position position = new(_reference);
            float distance = _wanderDistance > 0.1f ? RandomHelper.FRand(0.1f, _wanderDistance) : _wanderDistance;
            float angle = RandomHelper.FRand(0.0f, MathF.PI * 2);
            owner.MovePositionToFirstCollision(position, distance, angle);

            // Check if the destination is in LOS
            if (!owner.IsWithinLOS(position.GetPositionX(), position.GetPositionY(), position.GetPositionZ()))
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

            bool result = _path.CalculatePath(position.GetPositionX(), position.GetPositionY(), position.GetPositionZ());
            // PATHFIND_FARFROMPOLY shouldn't be checked as creatures in water are most likely far from poly
            if (!result || _path.GetPathType().HasFlag(PathType.NoPath) || _path.GetPathType().HasFlag(PathType.Shortcut))// || _path.GetPathType().HasFlag(PathType.FarFromPoly))
            {
                _timer.Reset(100);
                return;
            }

            if (_path.GetPathLength() < 0.1f)
            {
                // the path is too short for the spline system to be accepted. Let's try again soon.
                _timer.Reset(500);
                return;
            }

            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.TimedPaused);

            owner.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new(owner);
            init.MovebyPath(_path.GetPath());

            switch (_speedSelectionMode)
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

            if (_speed.HasValue)
                init.SetVelocity(_speed.Value);

            uint splineDuration = (uint)init.Launch();

            --_wanderSteps;
            if (_wanderSteps != 0) // Creature has yet to do steps before pausing
                _timer.Reset(splineDuration);
            else
            {
                // Creature has made all its steps, time for a little break
                _timer.Reset(splineDuration + RandomHelper.URand(4, 10) * Time.InMilliseconds); // Retails seems to use rounded numbers so we do as well
                _wanderSteps = RandomHelper.URand(2, 10);
            }

            // Call for creature group update
            if (owner.IsCreature())
                owner.ToCreature().SignalFormationMovement();
        }

        public override void UnitSpeedChanged() { AddFlag(MovementGeneratorFlags.SpeedUpdatePending); }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Random;
        }
    }
}
