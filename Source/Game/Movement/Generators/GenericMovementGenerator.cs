// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.v2;
using System;

namespace Game.Movement
{
    class GenericMovementGenerator : MovementGenerator
    {
        Action<MoveSplineInit> _splineInit;
        MovementGeneratorType _type;
        uint _pointId;
        TimeTracker _duration;
        bool _durationTracksSpline;

        uint _arrivalSpellId;
        ObjectGuid _arrivalSpellTargetGuid;

        public GenericMovementGenerator(Action<MoveSplineInit> initializer, MovementGeneratorType type, uint id, GenericMovementGeneratorArgs args = default)
        {
            _splineInit = initializer;
            _type = type;
            _pointId = id;
            _durationTracksSpline = true;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
            if (args.ArrivalSpellId.HasValue)
                _arrivalSpellId = args.ArrivalSpellId.Value;
            if (args.ArrivalSpellTarget.HasValue)
                _arrivalSpellTargetGuid = args.ArrivalSpellTarget.Value;
            if (args.Duration.HasValue)
            {
                _duration = new(args.Duration.Value);
                _durationTracksSpline = false;
            }

            ScriptResult = args.ScriptResult;
        }

        public override void Initialize(Unit owner)
        {
            if (HasFlag(MovementGeneratorFlags.Deactivated) && !HasFlag(MovementGeneratorFlags.InitializationPending)) // Resume spline is not supported
            {
                RemoveFlag(MovementGeneratorFlags.Deactivated);
                AddFlag(MovementGeneratorFlags.Finalized);
                return;
            }

            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            MoveSplineInit init = new(owner);
            _splineInit(init);
            int duration = init.Launch();
            if (_durationTracksSpline)
                _duration = new((uint)duration);
        }

        public override void Reset(Unit owner)
        {
            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (owner == null || HasFlag(MovementGeneratorFlags.Finalized))
                return false;

            // Cyclic splines never expire, so update the duration only if it's not cyclic
            _duration?.Update(diff);

            if ((_duration != null && _duration.Passed()) || owner.MoveSpline.Finalized())
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

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                MovementInform(owner);
        }

        void MovementInform(Unit owner)
        {
            if (_arrivalSpellId != 0)
                owner.CastSpell(Global.ObjAccessor.GetUnit(owner, _arrivalSpellTargetGuid), _arrivalSpellId, true);

            SetScriptResult(MovementStopReason.Finished);

            Creature creature = owner.ToCreature();
            if (creature != null && creature.GetAI() != null)
                creature.GetAI().MovementInform(_type, _pointId);
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return _type; }
    }

    struct GenericMovementGeneratorArgs
    {
        public uint? ArrivalSpellId;
        public ObjectGuid? ArrivalSpellTarget;
        public TimeSpan? Duration;
        public ActionResultSetter<MovementStopReason> ScriptResult;
    }
}
