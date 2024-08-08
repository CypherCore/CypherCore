// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.v2;
using System;

namespace Game.Movement
{
    public class IdleMovementGenerator : MovementGenerator
    {
        public IdleMovementGenerator()
        {
            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.Initialized;
            BaseUnitState = 0;
        }

        public override void Initialize(Unit owner)
        {
            owner.StopMoving();
        }

        public override void Reset(Unit owner)
        {
            owner.StopMoving();
        }

        public override bool Update(Unit owner, uint diff)
        {
            return true;
        }

        public override void Deactivate(Unit owner) { }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
        }

        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Idle;
        }
    }

    public class RotateMovementGenerator : MovementGenerator
    {
        static float MIN_ANGLE_DELTA_FOR_FACING_UPDATE = 0.05f;

        uint _id;
        RotateDirection _direction;
        TimeTracker _duration;
        float? _turnSpeed;         ///< radians per sec
        float? _totalTurnAngle;
        uint _diffSinceLastUpdate;

        public RotateMovementGenerator(uint id, RotateDirection direction, TimeSpan? duration, float? turnSpeed, float? totalTurnAngle, ActionResultSetter<MovementStopReason> scriptResult)
        {
            _id = id;
            _direction = direction;
            if (duration.HasValue)
                _duration = new TimeTracker(duration.Value);

            _turnSpeed = turnSpeed;
            _totalTurnAngle = totalTurnAngle;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Rotating;
            ScriptResult = scriptResult;
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            owner.StopMoving();

            /*
             *  TODO: This code should be handled somewhere else, like MovementInform
             *
             *  if (owner->GetVictim())
             *      owner->SetInFront(owner->GetVictim());
             *
             *  owner->AttackStop();
             */
        }

        public override void Reset(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.Deactivated);
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            _diffSinceLastUpdate += diff;

            float currentAngle = owner.GetOrientation();
            float angleDelta = _turnSpeed.GetValueOrDefault(owner.GetSpeed(UnitMoveType.TurnRate)) * ((float)_diffSinceLastUpdate / (float)Time.InMilliseconds);

            if (_duration != null)
                _duration.Update(diff);

            if (_totalTurnAngle.HasValue)
                _totalTurnAngle = _totalTurnAngle - angleDelta;

            bool expired = (_duration != null && _duration.Passed()) || (_totalTurnAngle.HasValue && _totalTurnAngle < 0.0f);

            if (angleDelta >= MIN_ANGLE_DELTA_FOR_FACING_UPDATE || expired)
            {
                float newAngle = Position.NormalizeOrientation(currentAngle + angleDelta * (_direction == RotateDirection.Left ? 1.0f : -1.0f));

                MoveSplineInit init = new(owner);
                init.MoveTo(owner.GetPosition(), false);
                if (!owner.GetTransGUID().IsEmpty())
                    init.DisableTransportPathTransformations();
                init.SetFacing(newAngle);
                init.Launch();

                _diffSinceLastUpdate = 0;
            }

            if (expired)
            {
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }

            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            if (movementInform)
            {
                SetScriptResult(MovementStopReason.Finished);
                if (owner.IsCreature())
                    owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Rotate, _id);
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Rotate; }
    }

    public class DistractMovementGenerator : MovementGenerator
    {
        public DistractMovementGenerator(uint timer, float orientation)
        {
            _timer = timer;
            _orientation = orientation;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Highest;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Distracted;
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            // Distracted creatures stand up if not standing
            if (!owner.IsStandState())
                owner.SetStandState(UnitStandStateType.Stand);

            MoveSplineInit init = new(owner);
            init.MoveTo(owner, false);
            if (!owner.GetTransGUID().IsEmpty())
                init.DisableTransportPathTransformations();

            init.SetFacing(_orientation);
            init.Launch();
        }

        public override void Reset(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.Deactivated);
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (owner == null)
                return false;

            if (diff > _timer)
            {
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }

            _timer -= diff;
            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);

            // TODO: This code should be handled somewhere else
            // If this is a creature, then return orientation to original position (for idle movement creatures)
            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled) && owner.IsCreature())
            {
                float angle = owner.ToCreature().GetHomePosition().GetOrientation();
                owner.SetFacingTo(angle);
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Distract; }

        uint _timer;
        float _orientation;
    }

    public class AssistanceDistractMovementGenerator : DistractMovementGenerator
    {
        public AssistanceDistractMovementGenerator(uint timer, float orientation) : base(timer, orientation)
        {
            Priority = MovementGeneratorPriority.Normal;
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            owner.ClearUnitState(UnitState.Distracted);
            owner.ToCreature().SetReactState(ReactStates.Aggressive);
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.AssistanceDistract; }
    }
}
