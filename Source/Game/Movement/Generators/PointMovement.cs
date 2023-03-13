// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;

namespace Game.Movement
{
    public class PointMovementGenerator : MovementGenerator
    {
        uint _movementId;
        Position _destination;
        float? _speed;
        bool _generatePath;
        //! if set then unit will turn to specified _orient in provided _pos
        float? _finalOrient;
        Unit _faceTarget;
        SpellEffectExtraData _spellEffectExtra;
        MovementWalkRunSpeedSelectionMode _speedSelectionMode;
        float? _closeEnoughDistance;

        public PointMovementGenerator(uint id, float x, float y, float z, bool generatePath, float? speed = null, float? finalOrient = null, Unit faceTarget = null, SpellEffectExtraData spellEffectExtraData = null,
            MovementWalkRunSpeedSelectionMode speedSelectionMode = MovementWalkRunSpeedSelectionMode.Default, float? closeEnoughDistance = null)
        {
            _movementId = id;
            _destination = new Position(x, y, z);
            _speed = speed;
            _generatePath = generatePath;
            _finalOrient = finalOrient;
            _faceTarget = faceTarget;
            _spellEffectExtra = spellEffectExtraData;
            _speedSelectionMode = speedSelectionMode;
            _closeEnoughDistance = closeEnoughDistance;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public override void Initialize(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated);
            AddFlag(MovementGeneratorFlags.Initialized);

            if (_movementId == EventId.ChargePrepath)
            {
                owner.AddUnitState(UnitState.RoamingMove);
                return;
            }

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return;
            }

            owner.AddUnitState(UnitState.RoamingMove);

            MoveSplineInit init = new(owner);
            if (_generatePath)
            {
                PathGenerator path = new(owner);
                bool result = path.CalculatePath(_destination.posX, _destination.posY, _destination.posZ, false);
                if (result && !path.GetPathType().HasFlag(PathType.NoPath))
                {
                    if (_closeEnoughDistance.HasValue)
                        path.ShortenPathUntilDist(_destination, _closeEnoughDistance.Value);

                    init.MovebyPath(path.GetPath());
                    return;
                }
            }

            Position dest = _destination;
            if (_closeEnoughDistance.HasValue)
                owner.MovePosition(dest, Math.Min(_closeEnoughDistance.Value, dest.GetExactDist(owner)), MathF.PI + owner.GetRelativeAngle(dest));

            init.MoveTo(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false);

            if (_speed.HasValue)
                init.SetVelocity(_speed.Value);

            if (_faceTarget)
                init.SetFacing(_faceTarget);

            if (_spellEffectExtra != null)
                init.SetSpellEffectExtraData(_spellEffectExtra);

            if (_finalOrient.HasValue)
                init.SetFacing(_finalOrient.Value);

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

            init.Launch();

            // Call for creature group update
            Creature creature = owner.ToCreature();
            if (creature != null)
                creature.SignalFormationMovement();
        }

        public override void Reset(Unit owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            Initialize(owner);
        }

        public override bool Update(Unit owner, uint diff)
        {
            if (owner == null)
                return false;

            if (_movementId == EventId.ChargePrepath)
            {
                if (owner.MoveSpline.Finalized())
                {
                    AddFlag(MovementGeneratorFlags.InformEnabled);
                    return false;
                }
                return true;
            }

            if (owner.HasUnitState(UnitState.NotMove) || owner.IsMovementPreventedByCasting())
            {
                AddFlag(MovementGeneratorFlags.Interrupted);
                owner.StopMoving();
                return true;
            }

            if ((HasFlag(MovementGeneratorFlags.Interrupted) && owner.MoveSpline.Finalized()) || (HasFlag(MovementGeneratorFlags.SpeedUpdatePending) && !owner.MoveSpline.Finalized()))
            {
                RemoveFlag(MovementGeneratorFlags.Interrupted | MovementGeneratorFlags.SpeedUpdatePending);

                owner.AddUnitState(UnitState.RoamingMove);

                MoveSplineInit init = new(owner);
                init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ(), _generatePath);
                if (_speed.HasValue) // Default value for point motion type is 0.0, if 0.0 spline will use GetSpeed on unit
                    init.SetVelocity(_speed.Value);
                init.Launch();

                // Call for creature group update
                Creature creature = owner.ToCreature();
                if (creature != null)
                    creature.SignalFormationMovement();
            }

            if (owner.MoveSpline.Finalized())
            {
                RemoveFlag(MovementGeneratorFlags.Transitory);
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }
            return true;
        }

        public override void Deactivate(Unit owner)
        {
            AddFlag(MovementGeneratorFlags.Deactivated);
            owner.ClearUnitState(UnitState.RoamingMove);
        }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                MovementInform(owner);
        }

        public void MovementInform(Unit owner)
        {
            owner.ToCreature()?.GetAI()?.MovementInform(MovementGeneratorType.Point, _movementId);
        }

        public override void UnitSpeedChanged()
        {
            AddFlag(MovementGeneratorFlags.SpeedUpdatePending);
        }

        public uint GetId() { return _movementId; }
        
        public override MovementGeneratorType GetMovementGeneratorType()
        {
            return MovementGeneratorType.Point;
        }
    }

    public class AssistanceMovementGenerator : PointMovementGenerator
    {
        public AssistanceMovementGenerator(uint id, float x, float y, float z) : base(id, x, y, z, true) { }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled) && owner.IsCreature())
            {
                Creature ownerCreature = owner.ToCreature();
                ownerCreature.SetNoCallAssistance(false);
                ownerCreature.CallAssistance();
                if (ownerCreature.IsAlive())
                    ownerCreature.GetMotionMaster().MoveSeekAssistanceDistract(WorldConfig.GetUIntValue(WorldCfg.CreatureFamilyAssistanceDelay));
            }
        }

        public override MovementGeneratorType GetMovementGeneratorType() { return MovementGeneratorType.Assistance; }
    }
}
