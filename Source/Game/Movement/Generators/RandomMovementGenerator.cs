// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Movement
{
    public class RandomMovementGenerator : MovementGeneratorMedium<Creature>
    {
        PathGenerator _path;
        TimeTracker _timer;
        TimeTracker _duration;
        Position _reference;
        float _wanderDistance;
        uint _wanderSteps;

        public RandomMovementGenerator(float spawnDist = 0.0f, TimeSpan? duration = null)
        {
            _timer = new TimeTracker();
            _reference = new();
            _wanderDistance = spawnDist;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
            if (duration.HasValue)
                _duration = new TimeTracker(duration.Value);
        }

        public override void DoInitialize(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated | MovementGeneratorFlags.Paused);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (owner == null || !owner.IsAlive())
                return;

            _reference = owner.GetPosition();
            owner.StopMoving();

            if (_wanderDistance == 0f)
                _wanderDistance = owner.GetWanderDistance();

            // Retail seems to let a creature walk 2 up to 10 splines before triggering a pause
            _wanderSteps = RandomHelper.URand(2, 10);

            _timer.Reset(0);
            _path = null;
        }

        public override void DoReset(Creature owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);
            DoInitialize(owner);
        }

        public override bool DoUpdate(Creature owner, uint diff)
        {
            if (owner == null || !owner.IsAlive())
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
                owner.StopMoving();

                // TODO: Research if this modification is needed, which most likely isnt
                owner.SetWalk(false);
            }

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                if (owner.IsAIEnabled())
                    owner.GetAI().MovementInform(MovementGeneratorType.Random, 0);
        }

        public override void Pause(uint timer = 0)
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

        public override void Resume(uint overrideTimer = 0)
        {
            if (overrideTimer != 0)
                _timer.Reset(overrideTimer);

            RemoveFlag(MovementGeneratorFlags.Paused);
        }

        void SetRandomLocation(Creature owner)
        {
            if (owner == null)
                return;

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

            bool walk = true;
            switch (owner.GetMovementTemplate().GetRandom())
            {
                case CreatureRandomMovementType.CanRun:
                    walk = owner.IsWalking();
                    break;
                case CreatureRandomMovementType.AlwaysRun:
                    walk = false;
                    break;
                default:
                    break;
            }

            MoveSplineInit init = new(owner);
            init.MovebyPath(_path.GetPath());
            init.SetWalk(walk);
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
            owner.SignalFormationMovement();
        }

        public override void UnitSpeedChanged() { AddFlag(MovementGeneratorFlags.SpeedUpdatePending); }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Random;
        }
    }
}
