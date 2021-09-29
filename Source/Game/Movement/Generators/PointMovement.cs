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

namespace Game.Movement
{
    public class PointMovementGenerator<T> : MovementGeneratorMedium<T> where T : Unit
    {
        public PointMovementGenerator(uint id, float x, float y, float z, bool generatePath, float speed = 0.0f, float? finalOrient = null, Unit faceTarget = null, SpellEffectExtraData spellEffectExtraData = null)
        {
            _movementId = id;
            _destination = new Position(x, y, z);
            _speed = speed;
            _generatePath = generatePath;
            _finalOrient = finalOrient;
            _faceTarget = faceTarget;
            _spellEffectExtra = spellEffectExtraData;

            Mode = MovementGeneratorMode.Default;
            Priority = MovementGeneratorPriority.Normal;
            Flags = MovementGeneratorFlags.InitializationPending;
            BaseUnitState = UnitState.Roaming;
        }

        public override void DoInitialize(T owner)
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
            init.MoveTo(_destination.GetPositionX(), _destination.GetPositionY(), _destination.GetPositionZ(), _generatePath);
            if (_speed > 0.0f)
                init.SetVelocity(_speed);

            if (_faceTarget)
                init.SetFacing(_faceTarget);

            if (_spellEffectExtra != null)
                init.SetSpellEffectExtraData(_spellEffectExtra);

            if (_finalOrient.HasValue)
                init.SetFacing(_finalOrient.Value);

            init.Launch();

            // Call for creature group update
            Creature creature = owner.ToCreature();
            if (creature != null)
                creature.SignalFormationMovement(_destination, _movementId);
        }

        public override void DoReset(T owner)
        {
            RemoveFlag(MovementGeneratorFlags.Transitory | MovementGeneratorFlags.Deactivated);

            DoInitialize(owner);
        }

        public override bool DoUpdate(T owner, uint diff)
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
                if (_speed > 0.0f) // Default value for point motion type is 0.0, if 0.0 spline will use GetSpeed on unit
                    init.SetVelocity(_speed);
                init.Launch();

                // Call for creature group update
                Creature creature = owner.ToCreature();
                if (creature != null)
                    creature.SignalFormationMovement(_destination, _movementId);
            }

            if (owner.MoveSpline.Finalized())
            {
                RemoveFlag(MovementGeneratorFlags.Transitory);
                AddFlag(MovementGeneratorFlags.InformEnabled);
                return false;
            }
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
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
                MovementInform(owner);
        }

        public void MovementInform(T owner)
        {
            if (owner.IsTypeId(TypeId.Unit))
            {
                if (owner.ToCreature().GetAI() != null)
                    owner.ToCreature().GetAI().MovementInform(MovementGeneratorType.Point, _movementId);
            }
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

        uint _movementId;
        Position _destination;
        float _speed;
        bool _generatePath;
        //! if set then unit will turn to specified _orient in provided _pos
        float? _finalOrient;
        Unit _faceTarget;
        SpellEffectExtraData _spellEffectExtra;
    }

    public class AssistanceMovementGenerator : PointMovementGenerator<Creature>
    {
        public AssistanceMovementGenerator(uint id, float x, float y, float z) : base(id, x, y, z, true) { }

        public override void Finalize(Unit owner, bool active, bool movementInform)
        {
            AddFlag(MovementGeneratorFlags.Finalized);
            if (active)
                owner.ClearUnitState(UnitState.RoamingMove);

            if (movementInform && HasFlag(MovementGeneratorFlags.InformEnabled))
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
